using UnityEngine;

public class PlaneController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;


    [Header("Movement Limits")]
    [SerializeField] private float xLimit = 5f;
    [SerializeField] private float yLimit = 3f;


    [Header("Plane Rotation")]
    [SerializeField] private float maxRoll = 25f;
    [SerializeField] private float maxPitch = 15f;
    [SerializeField] private float rotationSpeed = 5f;


    [Header("Wing Rotation")]
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;

    [SerializeField] private float wingRollAmount = 15f;
    [SerializeField] private float wingPitchAmount = 8f;
    [SerializeField] private float wingRotationSpeed = 5f;


    public Vector2 Input { get; private set; }


    private Quaternion initialRotation;

    private Quaternion leftWingInitialRotation;
    private Quaternion rightWingInitialRotation;

    private bool canControl;


    private void Awake()
    {
        // Plane initial rotation
        initialRotation = transform.rotation;


        // Wings initial local rotations
        if (leftWing != null)
        {
            leftWingInitialRotation =
                leftWing.localRotation;
        }


        if (rightWing != null)
        {
            rightWingInitialRotation =
                rightWing.localRotation;
        }


        canControl = false;
    }


    private void OnEnable()
    {
        GameEvents.GameStarted += EnableControl;
        GameEvents.GameOver += DisableControl;
    }


    private void OnDisable()
    {
        GameEvents.GameStarted -= EnableControl;
        GameEvents.GameOver -= DisableControl;
    }


    private void Update()
    {
        if (!canControl)
            return;


        ReadInput();

        Move();

        ClampPosition();

        RotatePlane();

        RotateWings();
    }


    private void EnableControl()
    {
        canControl = true;
    }


    private void DisableControl()
    {
        canControl = false;

        Input = Vector2.zero;
    }


    private void ReadInput()
    {
        Input = new Vector2(
            UnityEngine.Input.GetAxisRaw("Horizontal"),
            UnityEngine.Input.GetAxisRaw("Vertical")
        );


        Input =
            Vector2.ClampMagnitude(
                Input,
                1f
            );
    }


    private void Move()
    {
        Vector3 movement =
            new Vector3(
                Input.x,
                Input.y,
                0f
            );


        transform.position +=
            movement *
            moveSpeed *
            Time.deltaTime;
    }


    private void ClampPosition()
    {
        Vector3 position =
            transform.position;


        position.x =
            Mathf.Clamp(
                position.x,
                -xLimit,
                xLimit
            );


        position.y =
            Mathf.Clamp(
                position.y,
                -yLimit,
                yLimit
            );


        transform.position = position;
    }


    private void RotatePlane()
    {
        float targetRoll =
            -Input.x * maxRoll;


        float targetPitch =
            -Input.y * maxPitch;


        Quaternion targetRotation =
            initialRotation *
            Quaternion.Euler(
                targetPitch,
                0f,
                targetRoll
            );


        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    private void RotateWings()
    {
        if (leftWing == null && rightWing == null)
            return;
        
        float horizontalRotation =
            -Input.x * wingRollAmount;
        
        float verticalRotation =
            -Input.y * wingPitchAmount;
        
        float leftRotation =
            verticalRotation + horizontalRotation;


        float rightRotation =
            verticalRotation - horizontalRotation;


        if (leftWing != null)
        {
            Quaternion targetRotation =
                leftWingInitialRotation *
                Quaternion.Euler(
                    leftRotation,
                    0f,
                    0f
                );


            leftWing.localRotation =
                Quaternion.Lerp(
                    leftWing.localRotation,
                    targetRotation,
                    wingRotationSpeed *
                    Time.deltaTime
                );
        }


        if (rightWing != null)
        {
            Quaternion targetRotation =
                rightWingInitialRotation *
                Quaternion.Euler(
                    rightRotation,
                    0f,
                    0f
                );


            rightWing.localRotation =
                Quaternion.Lerp(
                    rightWing.localRotation,
                    targetRotation,
                    wingRotationSpeed *
                    Time.deltaTime
                );
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class PlaneController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float touchSensitivity = 0.015f;
    [SerializeField] private float movementSmoothness = 10f;


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

    private Vector3 targetPosition;

    private Vector2 lastTouchPosition;
    private bool isTouching;


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


        targetPosition =
            transform.position;

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

        targetPosition =
            transform.position;

        Input = Vector2.zero;

        isTouching = false;
    }


    private void DisableControl()
    {
        canControl = false;

        Input = Vector2.zero;

        isTouching = false;
    }


    private void ReadInput()
    {
        if (Touchscreen.current == null)
        {
            Input = Vector2.zero;
            return;
        }

        var touch =
            Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            isTouching = true;

            lastTouchPosition =
                touch.position.ReadValue();

            Input = Vector2.zero;

            return;
        }

        if (touch.press.isPressed && isTouching)
        {
            Vector2 currentTouchPosition =
                touch.position.ReadValue();

            Vector2 delta =
                currentTouchPosition -
                lastTouchPosition;

            lastTouchPosition =
                currentTouchPosition;

            Input =
                Vector2.ClampMagnitude(
                    delta * 0.05f,
                    1f
                );

            Vector3 movement =
                new Vector3(
                    delta.x,
                    delta.y,
                    0f
                ) *
                touchSensitivity;

            targetPosition += movement;
        }

        if (touch.press.wasReleasedThisFrame)
        {
            isTouching = false;

            Input = Vector2.zero;
        }
    }


    private void Move()
    {
        targetPosition.x =
            Mathf.Clamp(
                targetPosition.x,
                -xLimit,
                xLimit
            );


        targetPosition.y =
            Mathf.Clamp(
                targetPosition.y,
                -yLimit,
                yLimit
            );


        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                movementSmoothness *
                Time.deltaTime
            );
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


        transform.position =
            position;
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
            verticalRotation +
            horizontalRotation;


        float rightRotation =
            verticalRotation -
            horizontalRotation;


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
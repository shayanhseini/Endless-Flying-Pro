using UnityEngine;
using UnityEngine.InputSystem;
using DroneVision;

public class PlaneController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
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


    [Header("Final Auto Pilot")]
    [SerializeField] private float autoPilotSpeed = 20f;
    [SerializeField] private float autoPilotRotationSpeed = 4f;
    [SerializeField] private PlaneCameraController planeCameraController;
    
    private bool isAutoPilot;
    private Transform finalTarget;


    public Vector2 Input { get; private set; }


    private Quaternion initialRotation;

    private Quaternion leftWingInitialRotation;
    private Quaternion rightWingInitialRotation;

    private bool canControl;

    private Vector3 targetPosition;

    
    private void Awake()
    {
        initialRotation =
            transform.rotation;


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

        isAutoPilot = false;
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
        if (isAutoPilot)
        {
            AutoPilot();

            return;
        }


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

        isAutoPilot = false;

        finalTarget = null;


        targetPosition =
            transform.position;


        Input =
            Vector2.zero;
    }


    private void DisableControl()
    {
        canControl = false;

        isAutoPilot = false;

        finalTarget = null;

        Input =
            Vector2.zero;
    }


    public void StartFinalAutoPilot(Transform target)
    {
        if (target == null)
            return;


        canControl = false;

        isAutoPilot = true;

        finalTarget = target;

        Input = Vector2.zero;


        if (planeCameraController != null)
        {
            planeCameraController.StartCinematicFollow(
                transform
            );
        }
    }
    
    private void AutoPilot()
    {
        if (finalTarget == null)
            return;
        
        Vector3 direction =
            finalTarget.position -
            transform.position;


        if (direction.sqrMagnitude <= 0.001f)
            return;


        direction.Normalize();


        transform.position +=
            direction *
            autoPilotSpeed *
            Time.deltaTime;


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                autoPilotRotationSpeed *
                Time.deltaTime
            );
    }


    private void ReadInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            // A / D
            if (Keyboard.current.aKey.isPressed)
                horizontal -= 1f;

            if (Keyboard.current.dKey.isPressed)
                horizontal += 1f;

            // S / W
            if (Keyboard.current.sKey.isPressed)
                vertical -= 1f;

            if (Keyboard.current.wKey.isPressed)
                vertical += 1f;
        }

        // Drone Vision integration (webcam body control)
        if (DroneVisionInput.Instance != null)
        {
            horizontal += DroneVisionInput.Instance.Horizontal;
            vertical += DroneVisionInput.Instance.Vertical;
        }

        Input =
            new Vector2(
                horizontal,
                vertical
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
            ) *
            moveSpeed *
            Time.deltaTime;
        
        targetPosition +=
            movement;
        
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
            -Input.x *
            maxRoll;
        
        float targetPitch =
            -Input.y *
            maxPitch;
        
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
        if (leftWing == null &&
            rightWing == null)
            return;
        
        float horizontalRotation =
            -Input.x *
            wingRollAmount;
        
        float verticalRotation =
            -Input.y *
            wingPitchAmount;
        
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
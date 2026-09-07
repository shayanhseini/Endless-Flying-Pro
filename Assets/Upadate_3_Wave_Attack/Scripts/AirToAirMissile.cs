using UnityEngine;

public class AirToAirMissile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 30f;


    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float maxTurnSpeed = 180f;


    [Header("Guidance")]
    [SerializeField, Range(0f, 1f)]
    private float guidancePercent = 0.9f;


    [Header("Prediction")]
    [SerializeField] private float maxPredictionTime = 0.5f;


    [Header("Arming")]
    [SerializeField, Tooltip("مدت زمانی که بعد از شلیک، برخورد (OnTriggerEnter) نادیده گرفته میشه تا موشک بتونه از بدنه‌ی شلیک‌کننده فاصله بگیره")]
    private float armDelay = 0.15f;


    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;


    [Header("Initial Transform")]
    [SerializeField]
    private Vector3 initialLocalPosition =
        new Vector3(
            -4.069999f,
            -0.5499902f,
            -5.369999f
        );

    [SerializeField]
    private Vector3 initialLocalRotation =
        new Vector3(
            0f,
            -120f,
            0f
        );

    [Header("Effects")]
    [SerializeField] private GameObject moveSoundEffect;

    private Rigidbody rb;

    private Transform target;

    private Vector3 targetLastPosition;
    private Vector3 targetVelocity;

    private Vector3 launchPosition;
    private Vector3 lockedDirection;

    private float initialDistance;
    private float currentTurnSpeed;

    private bool isLocked;
    private bool isMoving;

    private float armTimer;
    private bool isArmed;


    private void Awake()
    {
        // مهم: موشک کاملا دستی با transform حرکت می‌کنه، نه با فیزیک.
        // اینجا مجبورش می‌کنیم Kinematic باشه و گرانش نداشته باشه،
        // حتی اگه رو پریفب اشتباه تنظیم شده باشه - چون گرانش رو Rigidbody
        // غیر Kinematic دقیقا همون چیزیه که باعث میشه موشک بره سمت زمین.
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }


    private void OnEnable()
    {
        isMoving = false;
        currentTurnSpeed = 0f;

        armTimer = 0f;
        isArmed = false;

        if (moveSoundEffect != null)
            moveSoundEffect.SetActive(false);

        if (debugLogging)
            Debug.Log($"[AirToAirMissile] OnEnable - {gameObject.name}", this);
    }


    public void SetTarget(Transform target)
    {
        if (target == null)
            return;


        this.target = target;


        launchPosition =
            transform.position;


        initialDistance =
            Vector3.Distance(
                launchPosition,
                target.position
            );


        targetLastPosition =
            target.position;


        targetVelocity =
            Vector3.zero;


        currentTurnSpeed = 0f;

        isLocked = false;
        isMoving = true;

        armTimer = 0f;
        isArmed = false;

        // مهم: به‌جای اینکه از جهت فعلی (که ممکنه بخاطر چرخش نقطه‌ی
        // اسپاون رو مدل، اشتباه/رو به پایین باشه) آروم‌آروم بچرخیم سمت
        // تارگت، همون لحظه‌ی شلیک مستقیم رو به سمت تارگت قفل می‌کنیم.
        // این باعث میشه موشک دیگه بی‌دلیل سمت زمین نره.
        Vector3 initialDirection = target.position - transform.position;

        if (initialDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(initialDirection.normalized);

        if (moveSoundEffect != null)
            moveSoundEffect.SetActive(true);

        if (debugLogging)
            Debug.Log($"[AirToAirMissile] SetTarget - target: {target.name}, isMoving: {isMoving}, launchPos: {launchPosition}, initialDistance: {initialDistance}", this);
    }


    public void StopAndReset()
    {
        isMoving = false;

        target = null;

        targetVelocity =
            Vector3.zero;

        currentTurnSpeed = 0f;

        isLocked = false;

        initialDistance = 0f;

        armTimer = 0f;
        isArmed = false;


        transform.localPosition =
            initialLocalPosition;


        transform.localRotation =
            Quaternion.Euler(
                initialLocalRotation
            );


        lockedDirection =
            transform.forward;


        gameObject.SetActive(true);
    }


    private void Update()
    {
        if (!isMoving)
            return;


        if (!isArmed)
        {
            armTimer += Time.deltaTime;

            if (armTimer >= armDelay)
            {
                isArmed = true;

                if (debugLogging)
                    Debug.Log($"[AirToAirMissile] Armed - {gameObject.name}, pos: {transform.position}", this);
            }
        }


        if (isLocked)
        {
            MoveStraight();
            return;
        }


        FollowTarget();
    }


    private void FollowTarget()
    {
        if (target == null)
        {
            LockDirection();
            return;
        }

        float traveledDistance =
            Vector3.Distance(
                launchPosition,
                transform.position
            );


        float progress =
            initialDistance > 0.001f
                ? traveledDistance /
                  initialDistance
                : 1f;

        if (progress >= guidancePercent)
        {
            LockDirection();
            return;
        }


        UpdateTargetVelocity();


        Vector3 predictedPosition =
            GetPredictedPosition();


        Vector3 direction =
            predictedPosition -
            transform.position;


        if (direction.sqrMagnitude <= 0.001f)
        {
            MoveForward();
            return;
        }


        direction.Normalize();


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        float angle =
            Quaternion.Angle(
                transform.rotation,
                targetRotation
            );

        float targetTurnSpeed =
            Mathf.Lerp(
                0f,
                maxTurnSpeed,
                Mathf.Clamp01(
                    angle / 90f
                )
            );

        currentTurnSpeed =
            Mathf.Lerp(
                currentTurnSpeed,
                targetTurnSpeed,
                rotationSpeed *
                Time.deltaTime
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                currentTurnSpeed *
                Time.deltaTime
            );

        MoveForward();
    }


    private void UpdateTargetVelocity()
    {
        if (Time.deltaTime <= 0f)
            return;


        Vector3 currentPosition =
            target.position;


        targetVelocity =
            (currentPosition -
             targetLastPosition) /
            Time.deltaTime;


        targetLastPosition =
            currentPosition;
    }


    private Vector3 GetPredictedPosition()
    {
        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );


        float predictionTime =
            distance / speed;


        predictionTime =
            Mathf.Clamp(
                predictionTime,
                0f,
                maxPredictionTime
            );


        return target.position +
               targetVelocity *
               predictionTime;
    }


    private void MoveForward()
    {
        transform.position +=
            transform.forward *
            speed *
            Time.deltaTime;
    }


    private void LockDirection()
    {
        lockedDirection =
            transform.forward.normalized;

        isLocked = true;
    }


    private void MoveStraight()
    {
        transform.position +=
            lockedDirection *
            speed *
            Time.deltaTime;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!isArmed)
        {
            if (debugLogging)
                Debug.Log($"[AirToAirMissile] OnTriggerEnter with {other.name} ignored - not armed yet", this);

            return;
        }


        if (!other.CompareTag("Player"))
        {
            if (debugLogging)
                Debug.Log($"[AirToAirMissile] OnTriggerEnter with {other.name} ignored - tag is '{other.tag}', not 'Player'", this);

            return;
        }


        PlaneInteraction plane =
            other.GetComponent<PlaneInteraction>();


        if (plane == null)
        {
            if (debugLogging)
                Debug.Log($"[AirToAirMissile] OnTriggerEnter with {other.name} ignored - has 'Player' tag but no PlaneInteraction component", this);

            return;
        }


        if (debugLogging)
            Debug.Log($"[AirToAirMissile] Hit confirmed on {other.name}", this);


        plane.HitObstacle();


        isMoving = false;
        target = null;


        if (moveSoundEffect != null)
            moveSoundEffect.SetActive(false);

        gameObject.SetActive(false);
    }
}
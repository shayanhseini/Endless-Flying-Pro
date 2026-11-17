using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Missile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 30f;


    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float maxTurnSpeed = 180f;


    [Header("Guidance")]
    [SerializeField, Range(0f, 1f)]
    [FormerlySerializedAs("guidancePercent")]
    private float terminalGuidanceStart = 0.9f;

    [SerializeField, Range(0.1f, 1f)]
    private float terminalTurnMultiplier = 0.22f;

    [SerializeField] private float velocitySmoothing = 12f;

    [SerializeField] private float missDisableDelay = 0.75f;


    [Header("Prediction")]
    [SerializeField] private float maxPredictionTime = 0.5f;


    [Header("Initial Transform")]
    [SerializeField] private Vector3 initialLocalPosition =
        new Vector3(
            -4.069999f,
            -0.5499902f,
            -5.369999f
        );

    [SerializeField] private Vector3 initialLocalRotation =
        new Vector3(
            0f,
            -120f,
            0f
        );

    [Header("Effects")]
    [SerializeField] private GameObject moveSoundEffect;

    private Transform target;
    private Collider targetCollider;

    private Vector3 targetLastPosition;
    private Vector3 targetVelocity;

    private Vector3 launchPosition;
    private Vector3 lockedDirection;

    private float initialDistance;
    private float currentTurnSpeed;

    private bool isLocked;
    private bool isMoving;
    private bool isWaitingToDisable;

    private float baseRotationSpeed;
    private float baseMaxTurnSpeed;
    private float baseMaxPredictionTime;
    private bool baseValuesCaptured;

    private void Awake()
    {
        CaptureBaseValues();
    }

    private void CaptureBaseValues()
    {
        if (baseValuesCaptured) return;
        baseRotationSpeed = rotationSpeed;
        baseMaxTurnSpeed = maxTurnSpeed;
        baseMaxPredictionTime = maxPredictionTime;
        baseValuesCaptured = true;
    }

    public void ApplyDifficultyModifiers(float turnSpeedMultiplier, float predictionMultiplier)
    {
        CaptureBaseValues();
        rotationSpeed = baseRotationSpeed * turnSpeedMultiplier;
        maxTurnSpeed = baseMaxTurnSpeed * turnSpeedMultiplier;
        maxPredictionTime = baseMaxPredictionTime * predictionMultiplier;
    }


    private void OnEnable()
    {
        CaptureBaseValues();
        isMoving = false;
        isWaitingToDisable = false;
        currentTurnSpeed = 0f;
        
        if (moveSoundEffect != null)
            moveSoundEffect.SetActive(false);
    }


    public void SetTarget(Transform target)
    {
        if (target == null)
            return;

        StopAllCoroutines();
        isWaitingToDisable = false;

        this.target = target;
        targetCollider = target.GetComponentInChildren<Collider>();


        launchPosition =
            transform.position;


        initialDistance =
            Vector3.Distance(
                launchPosition,
                GetTargetPosition()
            );


        targetLastPosition =
            GetTargetPosition();


        targetVelocity =
            Vector3.zero;


        currentTurnSpeed = 0f;

        isLocked = false;
        isMoving = true;
        
        moveSoundEffect.SetActive(true);
    }


    public void StopAndReset()
    {
        CaptureBaseValues();
        StopAllCoroutines();
        isMoving = false;
        isWaitingToDisable = false;

        target = null;
        targetCollider = null;

        targetVelocity =
            Vector3.zero;

        currentTurnSpeed = 0f;

        isLocked = false;

        initialDistance = 0f;


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
        
        UpdateTargetVelocity();

        Vector3 targetPosition = GetTargetPosition();
        Vector3 toTarget = targetPosition - transform.position;
        float dodgePhaseStart =
            Mathf.Min(terminalGuidanceStart, 0.82f);

        bool passedTarget =
            progress >= dodgePhaseStart &&
            Vector3.Dot(transform.forward, toTarget) <= 0f;

        if (passedTarget)
        {
            LockDirection();

            if (!isWaitingToDisable)
            {
                isWaitingToDisable = true;
                StartCoroutine(DisableAfterMissDelay());
            }

            return;
        }

        Vector3 aimPosition =
            progress >= dodgePhaseStart
                ? targetPosition
                : GetPredictedPosition(targetPosition);


        Vector3 direction =
            aimPosition -
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
                maxTurnSpeed * 0.35f,
                maxTurnSpeed,
                Mathf.Clamp01(
                    angle / 90f
                )
            );

        if (progress >= dodgePhaseStart)
            targetTurnSpeed *= terminalTurnMultiplier;
        
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
            GetTargetPosition();

        Vector3 measuredVelocity =
            (currentPosition -
             targetLastPosition) /
            Time.deltaTime;

        float smoothing =
            1f - Mathf.Exp(
                -velocitySmoothing *
                Time.deltaTime
            );

        targetVelocity =
            Vector3.Lerp(
                targetVelocity,
                measuredVelocity,
                smoothing
            );


        targetLastPosition =
            currentPosition;
    }


    private Vector3 GetPredictedPosition(
        Vector3 targetPosition)
    {
        Vector3 toTarget =
            targetPosition - transform.position;

        float speedSquared = speed * speed;
        float velocitySquared = targetVelocity.sqrMagnitude;

        float a = velocitySquared - speedSquared;
        float b = 2f * Vector3.Dot(toTarget, targetVelocity);
        float c = toTarget.sqrMagnitude;

        float predictionTime = 0f;

        if (Mathf.Abs(a) < 0.001f)
        {
            if (Mathf.Abs(b) > 0.001f)
                predictionTime = -c / b;
        }
        else
        {
            float discriminant = b * b - 4f * a * c;

            if (discriminant >= 0f)
            {
                float sqrtDiscriminant = Mathf.Sqrt(discriminant);
                float firstTime = (-b - sqrtDiscriminant) / (2f * a);
                float secondTime = (-b + sqrtDiscriminant) / (2f * a);

                predictionTime = GetSmallestPositiveTime(
                    firstTime,
                    secondTime
                );
            }
        }


        if (predictionTime <= 0f)
            predictionTime = toTarget.magnitude / Mathf.Max(speed, 0.1f);


        predictionTime =
            Mathf.Clamp(
                predictionTime,
                0f,
                maxPredictionTime
            );


        return targetPosition +
               targetVelocity *
               predictionTime;
    }


    private float GetSmallestPositiveTime(
        float firstTime,
        float secondTime)
    {
        bool firstIsValid = firstTime > 0f;
        bool secondIsValid = secondTime > 0f;

        if (firstIsValid && secondIsValid)
            return Mathf.Min(firstTime, secondTime);

        if (firstIsValid)
            return firstTime;

        return secondIsValid ? secondTime : 0f;
    }


    private Vector3 GetTargetPosition()
    {
        return targetCollider != null
            ? targetCollider.bounds.center
            : target.position;
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
        if (!other.CompareTag("Player"))
            return;


        PlaneInteraction plane =
            other.GetComponent<PlaneInteraction>();


        if (plane == null)
            return;


        plane.HitObstacle();


        DeactivateMissile();
    }


    private void DeactivateMissile()
    {
        isMoving = false;
        isLocked = false;
        isWaitingToDisable = false;
        target = null;
        targetCollider = null;
        targetVelocity = Vector3.zero;

        if (moveSoundEffect != null)
            moveSoundEffect.SetActive(false);

        gameObject.SetActive(false);
    }


    private IEnumerator DisableAfterMissDelay()
    {
        yield return new WaitForSeconds(missDisableDelay);
        DeactivateMissile();
    }
}

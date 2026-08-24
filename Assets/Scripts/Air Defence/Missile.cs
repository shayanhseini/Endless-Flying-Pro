using UnityEngine;

public class Missile : MonoBehaviour
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

    private Vector3 targetLastPosition;
    private Vector3 targetVelocity;

    private Vector3 launchPosition;
    private Vector3 lockedDirection;

    private float initialDistance;
    private float currentTurnSpeed;

    private bool isLocked;
    private bool isMoving;


    private void OnEnable()
    {
        isMoving = false;
        currentTurnSpeed = 0f;
        
        moveSoundEffect.SetActive(false);
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
        
        moveSoundEffect.SetActive(true);
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
        if (!other.CompareTag("Player"))
            return;


        PlaneInteraction plane =
            other.GetComponent<PlaneInteraction>();


        if (plane == null)
            return;


        plane.HitObstacle();


        isMoving = false;
        target = null;

        
        moveSoundEffect.SetActive(false);
        gameObject.SetActive(false);
    }
}
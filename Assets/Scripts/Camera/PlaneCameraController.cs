using System.Collections;
using UnityEngine;

public class PlaneCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneController planeController;


    [Header("Rotation")]
    [SerializeField] private float pitchAmount = 5f;
    [SerializeField] private float rollAmount = 3f;
    [SerializeField] private float rotationSmoothness = 5f;


    [Header("Vertical Bob")]
    [SerializeField] private float bobAmount = 0.15f;
    [SerializeField] private float bobSpeed = 1.5f;


    [Header("FOV")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float speedFOV = 68f;
    [SerializeField] private float fovTransitionDuration = 0.25f;


    [Header("Cinematic Follow")]
    [SerializeField] private float cinematicFollowDistance = 8f;
    [SerializeField] private float cinematicHeight = 2f;
    [SerializeField] private float cinematicPositionSmoothness = 3f;
    [SerializeField] private float cinematicRotationSmoothness = 4f;
    [SerializeField] private float cinematicLookAhead = 3f;


    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private Coroutine fovCoroutine;

    private Transform cinematicTarget;
    private bool isCinematic;
    private Vector3 cinematicVelocity;


    private void Awake()
    {
        initialRotation =
            transform.localRotation;

        initialPosition =
            transform.localPosition;


        if (targetCamera == null)
        {
            targetCamera =
                GetComponent<Camera>();
        }


        if (targetCamera != null)
        {
            targetCamera.fieldOfView =
                normalFOV;
        }


        isCinematic = false;
        cinematicTarget = null;
    }


    private void LateUpdate()
    {
        if (isCinematic)
        {
            CinematicFollow();

            return;
        }


        RotateCamera();

        BobCamera();
    }


    private void RotateCamera()
    {
        Vector2 input =
            planeController.Input;


        float targetPitch =
            -input.y * pitchAmount;

        float targetRoll =
            -input.x * rollAmount;


        Quaternion targetRotation =
            initialRotation *
            Quaternion.Euler(
                targetPitch,
                0f,
                targetRoll
            );


        transform.localRotation =
            Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                rotationSmoothness *
                Time.deltaTime
            );
    }


    private void BobCamera()
    {
        float offset =
            Mathf.Sin(
                Time.time * bobSpeed
            ) * bobAmount;


        Vector3 targetPosition =
            initialPosition;


        targetPosition.y += offset;


        transform.localPosition =
            targetPosition;
    }


    // =========================================================
    // Cinematic
    // =========================================================

    public void StartCinematicFollow(Transform target)
    {
        if (target == null)
            return;

        cinematicTarget = target;
        isCinematic = true;
        cinematicVelocity = Vector3.zero;

        // Position camera immediately at desired follow position to eliminate any initial catch-up drag/jerk
        Vector3 desiredPosition =
            cinematicTarget.position
            - cinematicTarget.forward * cinematicFollowDistance
            + Vector3.up * cinematicHeight;
        transform.position = desiredPosition;

        Vector3 lookPosition =
            cinematicTarget.position
            + cinematicTarget.forward * cinematicLookAhead;
        Vector3 direction = lookPosition - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }


    public void StopCinematicFollow()
    {
        isCinematic = false;
        cinematicTarget = null;
        cinematicVelocity = Vector3.zero;
    }


    private void CinematicFollow()
    {
        if (cinematicTarget == null)
            return;

        Vector3 desiredPosition =
            cinematicTarget.position
            - cinematicTarget.forward * cinematicFollowDistance
            + Vector3.up * cinematicHeight;

        // Framerate-independent critically damped smooth follow (zero rubber-banding / zero stutter)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref cinematicVelocity,
            0.05f
        );

        Vector3 lookPosition =
            cinematicTarget.position
            + cinematicTarget.forward * cinematicLookAhead;

        Vector3 direction = lookPosition - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Framerate-independent responsive rotation damping
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-16f * Time.deltaTime)
        );
    }


    // =========================================================
    // Gameplay Setup
    // =========================================================

    public void SetInitialTransform(
        Vector3 position,
        Quaternion rotation)
    {
        initialPosition =
            transform.parent != null
                ? transform.parent.InverseTransformPoint(position)
                : position;


        initialRotation =
            transform.parent != null
                ? Quaternion.Inverse(
                      transform.parent.rotation
                  ) * rotation
                : rotation;
    }


    // =========================================================
    // FOV
    // =========================================================

    public void SetSpeedFOV(bool active)
    {
        if (targetCamera == null)
            return;


        float targetFOV =
            active
                ? speedFOV
                : normalFOV;


        if (fovCoroutine != null)
        {
            StopCoroutine(fovCoroutine);
        }


        fovCoroutine =
            StartCoroutine(
                ChangeFOV(targetFOV)
            );
    }


    private IEnumerator ChangeFOV(
        float targetFOV)
    {
        float startFOV =
            targetCamera.fieldOfView;


        float elapsed = 0f;


        while (elapsed < fovTransitionDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fovTransitionDuration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            targetCamera.fieldOfView =
                Mathf.Lerp(
                    startFOV,
                    targetFOV,
                    t
                );


            yield return null;
        }


        targetCamera.fieldOfView =
            targetFOV;


        fovCoroutine = null;
    }
}
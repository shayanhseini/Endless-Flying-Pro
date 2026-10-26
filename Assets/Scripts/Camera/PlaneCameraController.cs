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

    [Header("Crash Follow")]
    [Min(0f)]
    [SerializeField] private float impactShakeDuration = 0.45f;
    [Min(0f)]
    [SerializeField] private float impactShakePosition = 0.3f;
    [Min(0f)]
    [SerializeField] private float impactShakeRotation = 2.5f;
    [Tooltip("Point in front of the plane that the camera looks toward.")]
    [SerializeField] private Vector3 crashLookOffset = new(0f, 0.4f, 2.5f);
    [Min(0.1f)]
    [SerializeField] private float crashRotationSmoothness = 30f;
    [Min(0.05f)]
    [Tooltip("Time used to move the camera smoothly into its behind-the-plane follow position.")]
    [SerializeField] private float crashRepositionDuration = 0.85f;


    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private Coroutine fovCoroutine;
    private Coroutine impactShakeCoroutine;
    private bool crashFollowing;
    private bool impactShaking;
    private float capturedBehindDistance;
    private float capturedFollowHeight;
    private float followTransitionElapsed;
    private Vector3 followTransitionStartPosition;
    private Quaternion followTransitionStartRotation;


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
    }

    private void OnEnable()
    {
        GameEvents.CrashLandingStarted += BeginCrashFollow;
        GameEvents.VictoryOrbitStarted += BeginVictoryFollow;
    }

    private void OnDisable()
    {
        GameEvents.CrashLandingStarted -= BeginCrashFollow;
        GameEvents.VictoryOrbitStarted -= BeginVictoryFollow;
    }


    private void LateUpdate()
    {
        if (impactShaking)
            return;

        if (crashFollowing)
        {
            FollowCrashingPlane();
            return;
        }

        RotateCamera();
        BobCamera();
    }

    private void BeginCrashFollow()
    {
        CaptureCurrentFollowOffset();
        crashFollowing = false;

        if (impactShakeCoroutine != null)
            StopCoroutine(impactShakeCoroutine);

        impactShakeCoroutine = StartCoroutine(ShakeThenFollow());
    }

    private void BeginVictoryFollow()
    {
        CaptureCurrentFollowOffset();
        impactShaking = false;
        BeginFollowTransition();
    }

    private void CaptureCurrentFollowOffset()
    {
        if (planeController == null)
            return;

        Transform plane = planeController.transform;
        Vector3 planeToCamera = transform.position - plane.position;
        capturedFollowHeight = Vector3.Dot(planeToCamera, Vector3.up);
        Vector3 horizontalOffset = Vector3.ProjectOnPlane(planeToCamera, Vector3.up);
        capturedBehindDistance = Mathf.Max(0.1f, horizontalOffset.magnitude);
    }

    private IEnumerator ShakeThenFollow()
    {
        impactShaking = true;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < impactShakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, impactShakeDuration));
            Vector3 positionOffset = Random.insideUnitSphere * impactShakePosition * strength;
            Vector3 rotationOffset = Random.insideUnitSphere * impactShakeRotation * strength;

            transform.position = startPosition + positionOffset;
            transform.rotation = startRotation * Quaternion.Euler(rotationOffset);
            yield return null;
        }

        transform.SetPositionAndRotation(startPosition, startRotation);
        impactShaking = false;
        BeginFollowTransition();
        impactShakeCoroutine = null;
    }

    private void BeginFollowTransition()
    {
        followTransitionElapsed = 0f;
        followTransitionStartPosition = transform.position;
        followTransitionStartRotation = transform.rotation;
        crashFollowing = true;
    }

    private void FollowCrashingPlane()
    {
        if (planeController == null)
            return;

        Transform plane = planeController.transform;
        Vector3 targetPosition = plane.position -
            plane.forward * capturedBehindDistance +
            Vector3.up * capturedFollowHeight;
        Vector3 lookPosition = plane.position +
            plane.forward * crashLookOffset.z +
            Vector3.up * crashLookOffset.y;
        float rotationBlend = 1f - Mathf.Exp(-crashRotationSmoothness * Time.deltaTime);

        Vector3 lookDirection = lookPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up);

            if (followTransitionElapsed < crashRepositionDuration)
            {
                followTransitionElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(
                    followTransitionElapsed / crashRepositionDuration);
                progress = Mathf.SmoothStep(0f, 1f, progress);
                transform.position = Vector3.Lerp(
                    followTransitionStartPosition,
                    targetPosition,
                    progress);
                transform.rotation = Quaternion.Slerp(
                    followTransitionStartRotation,
                    targetRotation,
                    progress);
                return;
            }

            transform.position = targetPosition;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationBlend);
        }
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

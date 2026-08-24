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


    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private Coroutine fovCoroutine;


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


    private void LateUpdate()
    {
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
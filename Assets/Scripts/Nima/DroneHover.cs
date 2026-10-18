using UnityEngine;

public sealed class DroneHover : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private float hoverAmount = 0.2f;
    [SerializeField] private float hoverFrequency = 1.5f;

    [Header("Tilt")]
    [SerializeField] private float tiltAmount = 3f;
    [SerializeField] private float tiltSpeed = 2f;

    [Header("Randomization")]
    [SerializeField] private float randomStartDelay = 1f;

   
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private float randomPhase;
    private float startTime;

    private void Awake()
    {
    
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        randomPhase = Random.Range(0f, Mathf.PI * 2f);
        startTime = Time.time + Random.Range(0f, randomStartDelay);
    }

    private void LateUpdate()
    {
        if (Time.time < startTime)
            return;

        float time = Time.time - startTime;

        // Hover
        float verticalOffset =
            Mathf.Sin(time * hoverFrequency + randomPhase) * hoverAmount;

        transform.localPosition =
            initialLocalPosition + Vector3.up * verticalOffset;

        // Tilt
        float tiltTime = time + randomPhase;

        float tiltX =
            Mathf.Sin(tiltTime * tiltSpeed) * tiltAmount;

        float tiltZ =
            Mathf.Cos(tiltTime * tiltSpeed * 0.8f) * tiltAmount;

        transform.localRotation =
            initialLocalRotation *
            Quaternion.Euler(tiltX, 0f, tiltZ);
    }
}
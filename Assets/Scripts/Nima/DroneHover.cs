using UnityEngine;

[RequireComponent(typeof(DroneTargetMover))]
public sealed class DroneHover : MonoBehaviour
{
    [SerializeField] private float hoverAmount = 0.2f;
    [SerializeField] private float hoverFrequency = 1.5f;

    [SerializeField] private float tiltAmount = 3f;
    [SerializeField] private float tiltSpeed = 2f;

    private DroneTargetMover targetMover;
    private Quaternion initialRotation;

    private void Awake()
    {
        targetMover = GetComponent<DroneTargetMover>();
        initialRotation = transform.rotation;
    }

    private void LateUpdate()               // LateUpdate مهمه: بعد از Update اجرا میشه
    {
        float time = Time.time;

        float verticalOffset = Mathf.Sin(time * hoverFrequency) * hoverAmount;

        // موقعیت پایه رو از BasePosition منطقیِ DroneTargetMover می‌گیریم،
        // نه از transform.position فعلی (که خودش شامل offset فریم قبل بود)
        transform.position = targetMover.BasePosition + Vector3.up * verticalOffset;

        // تیلت
        float tiltX = Mathf.Sin(time * tiltSpeed) * tiltAmount;
        float tiltZ = Mathf.Cos(time * tiltSpeed * 0.8f) * tiltAmount;

        transform.rotation = initialRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
    }
}
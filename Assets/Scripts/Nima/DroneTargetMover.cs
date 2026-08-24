using UnityEngine;

public sealed class DroneTargetMover : MonoBehaviour
{
    [SerializeField] private GameObject droneObject;
    [SerializeField] private Transform[] droneTargets;

    [SerializeField] private float moveSpeed = 3f;

    private int currentTargetIndex;

    // موقعیت منطقی/base که فقط این اسکریپت تغییرش می‌ده.
    // DroneHover باید offset بصری‌اش رو روی همین اعمال کنه، نه روی transform.position مستقیم،
    // وگرنه یک feedback loop بین دو اسکریپت ایجاد میشه.
    public Vector3 BasePosition { get; private set; }

    private void Awake()
    {
        if (droneObject != null)
            BasePosition = droneObject.transform.position;
    }

    private void Update()
    {
        if (droneObject == null || droneTargets == null || droneTargets.Length < 2)
            return;

        Transform target = droneTargets[currentTargetIndex];

        BasePosition = Vector3.MoveTowards(
            BasePosition,
            target.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(BasePosition, target.position) < 0.05f)
        {
            currentTargetIndex++;

            if (currentTargetIndex >= droneTargets.Length)
                currentTargetIndex = 0;
        }
    }
}
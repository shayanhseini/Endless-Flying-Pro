using System.Collections;
using UnityEngine;

public class FighterMissileLauncher : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField, Tooltip("زاویه باز شدن درب چپ دور محور X (مثلا -70)")]
    private float leftDoorOpenAngleX = -70f;
    [SerializeField, Tooltip("زاویه باز شدن درب راست دور محور X (مثلا 70)")]
    private float rightDoorOpenAngleX = 70f;
    [SerializeField] private float doorOpenDuration = 0.6f;
    [SerializeField] private AnimationCurve doorOpenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Missile Spawn Points")]
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private Transform rightSpawnPoint;

    [Header("Missile")]
    [SerializeField, Tooltip("پریفب موشک - هر پریفبی که کامپوننت AirToAirMissile داشته باشه")]
    private GameObject missilePrefab;
    [SerializeField, Tooltip("چون این آبجکت یه پریفبه، نمیشه مستقیم رفرنس یه آبجکت تو صحنه رو بهش داد. به جاش با این تگ موقع اجرا دنبال تارگت می‌گرده")]
    private string targetTag = "Player";

    [Header("Timing")]
    [SerializeField] private float initialDelay = 1.5f;
    [SerializeField] private float fireInterval = 3f;
    [SerializeField, Tooltip("مقدار +/- رندوم که به فاصله شلیک‌ها اضافه میشه تا طبیعی‌تر به نظر برسه")]
    private float fireIntervalRandomness = 0.3f;

    [Header("Effects")]
    [SerializeField, Tooltip("مادامی که این لانچر فعاله، این آبجکت هم فعال می‌مونه و با غیرفعال شدن لانچر خاموش میشه")]
    private GameObject activeEffectObject;

    private Coroutine sequenceRoutine;
    private bool useLeftSpawnNext = true;

    private Quaternion leftDoorClosedRotation;
    private Quaternion rightDoorClosedRotation;

    private Transform target;

    private void OnEnable()
    {
        if (leftDoor != null)
            leftDoorClosedRotation = leftDoor.localRotation;

        if (rightDoor != null)
            rightDoorClosedRotation = rightDoor.localRotation;

        TryFindTarget();

        if (activeEffectObject != null)
            activeEffectObject.SetActive(true);

        sequenceRoutine = StartCoroutine(RunSequence());
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (activeEffectObject != null)
            activeEffectObject.SetActive(false);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void TryFindTarget()
    {
        if (target != null)
            return;

        GameObject found = GameObject.FindGameObjectWithTag(targetTag);

        if (found != null)
            target = found.transform;
    }

    private IEnumerator RunSequence()
    {
        yield return StartCoroutine(OpenDoors());

        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            try
            {
                FireMissile();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FighterMissileLauncher] خطا حین شلیک، ولی چرخه ادامه پیدا می‌کنه: {e}", this);
            }

            float randomOffset = Random.Range(-fireIntervalRandomness, fireIntervalRandomness);
            float waitTime = Mathf.Max(0.1f, fireInterval + randomOffset);

            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator OpenDoors()
    {
        float elapsed = 0f;

        Quaternion leftOpen = leftDoorClosedRotation * Quaternion.Euler(leftDoorOpenAngleX, 0f, 0f);
        Quaternion rightOpen = rightDoorClosedRotation * Quaternion.Euler(rightDoorOpenAngleX, 0f, 0f);

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = doorOpenCurve.Evaluate(Mathf.Clamp01(elapsed / doorOpenDuration));

            if (leftDoor != null)
                leftDoor.localRotation = Quaternion.Slerp(leftDoorClosedRotation, leftOpen, t);

            if (rightDoor != null)
                rightDoor.localRotation = Quaternion.Slerp(rightDoorClosedRotation, rightOpen, t);

            yield return null;
        }

        if (leftDoor != null)
            leftDoor.localRotation = leftOpen;

        if (rightDoor != null)
            rightDoor.localRotation = rightOpen;
    }

    private void FireMissile()
    {
        TryFindTarget();

        if (missilePrefab == null || target == null)
        {
            Debug.LogWarning("موشک شلیک نشد: پریفب موشک یا تارگت پیدا نشد.", this);
            return;
        }

        Transform spawnPoint = useLeftSpawnNext ? leftSpawnPoint : rightSpawnPoint;
        useLeftSpawnNext = !useLeftSpawnNext;

        if (spawnPoint == null)
            return;

        GameObject missileObject = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
        AirToAirMissile missile = missileObject.GetComponent<AirToAirMissile>();

        if (missile == null)
        {
            Debug.LogWarning("پریفب موشک کامپوننت AirToAirMissile نداره!", missileObject);
            return;
        }

        missileObject.SetActive(true);
        missile.SetTarget(target);
    }
}
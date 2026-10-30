using UnityEngine;
using System.Linq;
using System.Collections;

public class Obstacle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private GameObject[] asteroidVariants;

    [Header("Asteroid Rotation")]
    [SerializeField] private float minRotationSpeed = 20f;
    [SerializeField] private float maxRotationSpeed = 55f;

    [Header("Random Size")]
    [SerializeField] private float minScale = 3f;
    [SerializeField] private float maxScale = 4f;
    [Min(0.05f)]
    [SerializeField] private float appearDuration = 0.45f;

    [Header("Player Tracking / Homing Settings")]
    [Tooltip("Enable missile-like tracking behavior where a portion of rocks drift/aim towards the spaceship.")]
    [SerializeField] private bool enableHoming = true;

    [Tooltip("Percentage chance for a rock to track the spaceship (0.5 = 50% of rocks).")]
    [Range(0f, 1f)]
    [SerializeField] private float homingChance = 0.5f;

    [Tooltip("Minimum lateral (X/Y) speed towards the spaceship.")]
    [SerializeField] private float minLateralSpeed = 1.8f;

    [Tooltip("Maximum lateral (X/Y) speed towards the spaceship.")]
    [SerializeField] private float maxLateralSpeed = 3.2f;

    [Tooltip("Minimum extra forward speed along -Z towards the spaceship.")]
    [SerializeField] private float minExtraForwardSpeed = 1.0f;

    [Tooltip("Maximum extra forward speed along -Z towards the spaceship.")]
    [SerializeField] private float maxExtraForwardSpeed = 3.0f;

    [Tooltip("Distance in front of spaceship where tracking locks, giving the player a fair dodge window.")]
    [SerializeField] private float guidanceLockDistance = 8.0f;

    [Tooltip("Random aiming spread around the player (prevents all rocks aiming at the exact same point).")]
    [SerializeField] private float aimSpread = 1.5f;

    [Tooltip("Delay in seconds after rock spawns before homing begins.")]
    [SerializeField] private float homingStartDelay = 0.45f;

    [Tooltip("Whether to maintain drift velocity after guidance locks.")]
    [SerializeField] private bool continueDriftAfterLock = true;

    private Collider[] activeColliders = System.Array.Empty<Collider>();
    private Transform activeVisual;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private bool hitProcessed;
    private Coroutine scaleCoroutine;
    private static int nextVariantIndex = -1;

    // Position reset caching
    private Vector3 initialLocalPosition;
    private bool initialPositionCached;

    // Tracking runtime state
    private static Transform cachedPlayerTransform;
    private bool isHoming;
    private bool isGuidanceLocked;
    private float homingTimer;
    private float currentExtraZSpeed;
    private float currentLateralSpeed;
    private Vector2 currentAimOffset;
    private Vector2 lastLateralDirection;

    private void Awake()
    {
        CacheInitialPosition();
        CacheChildVariantsIfNeeded();
        InstallCollisionRelays();
    }

    private void CacheInitialPosition()
    {
        if (!initialPositionCached)
        {
            initialLocalPosition = transform.localPosition;
            initialPositionCached = true;
        }
    }

    private void OnEnable()
    {
        GameEvents.VictorySequenceStarted += BeginVictoryExit;
        GameEvents.GameOver += StopHoming;
        GameEvents.CrashLandingStarted += StopHoming;
        GameEvents.VictoryOrbitStarted += StopHoming;

        hitProcessed = false;

        // Reset to original chunk slot position
        CacheInitialPosition();
        transform.localPosition = initialLocalPosition;

        SelectRandomVariant();

        float randomScale = Random.Range(minScale, maxScale);
        Vector3 targetScale = Vector3.one * randomScale;
        transform.localScale = Vector3.zero;

        SetActiveVariantColliders(false);
        scaleCoroutine = StartCoroutine(ScaleRoutine(
            Vector3.zero,
            targetScale,
            appearDuration,
            false));

        rotationAxis = Random.onUnitSphere;
        rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);

        if (activeVisual != null)
            activeVisual.localRotation = Random.rotation;

        // Initialize homing behavior
        InitializeHoming();
    }

    private void OnDisable()
    {
        GameEvents.VictorySequenceStarted -= BeginVictoryExit;
        GameEvents.GameOver -= StopHoming;
        GameEvents.CrashLandingStarted -= StopHoming;
        GameEvents.VictoryOrbitStarted -= StopHoming;

        scaleCoroutine = null;
        isHoming = false;
    }

    private void InitializeHoming()
    {
        if (enableHoming && Random.value < homingChance)
        {
            isHoming = true;
            isGuidanceLocked = false;
            homingTimer = 0f;
            currentExtraZSpeed = Random.Range(minExtraForwardSpeed, maxExtraForwardSpeed);
            currentLateralSpeed = Random.Range(minLateralSpeed, maxLateralSpeed);
            currentAimOffset = Random.insideUnitCircle * aimSpread;
            lastLateralDirection = Vector2.zero;

            if (cachedPlayerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    cachedPlayerTransform = playerObj.transform;
            }
        }
        else
        {
            isHoming = false;
        }
    }

    private void StopHoming()
    {
        isHoming = false;
    }

    private void BeginVictoryExit(float duration)
    {
        isHoming = false;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        SetActiveVariantColliders(false);
        scaleCoroutine = StartCoroutine(ScaleRoutine(
            transform.localScale,
            Vector3.zero,
            duration,
            true));
    }

    private IEnumerator ScaleRoutine(
        Vector3 startScale,
        Vector3 endScale,
        float transitionDuration,
        bool keepCollidersDisabled)
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / transitionDuration));
            transform.localScale = Vector3.LerpUnclamped(
                startScale,
                endScale,
                progress);
            yield return null;
        }

        transform.localScale = endScale;
        if (!keepCollidersDisabled)
            SetActiveVariantColliders(true);
        scaleCoroutine = null;
    }

    private void Update()
    {
        if (activeVisual != null)
        {
            activeVisual.Rotate(
                rotationAxis,
                rotationSpeed * Time.deltaTime,
                Space.Self
            );
        }

        UpdateHomingMovement();
    }

    private void UpdateHomingMovement()
    {
        if (!isHoming || hitProcessed)
            return;

        if (cachedPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                cachedPlayerTransform = playerObj.transform;
            else
                return;
        }

        homingTimer += Time.deltaTime;
        if (homingTimer < homingStartDelay)
            return;

        float zDistance = transform.position.z - cachedPlayerTransform.position.z;

        // If the obstacle has already passed the player, stop tracking
        if (zDistance <= 0f)
        {
            isHoming = false;
            return;
        }

        // Apply extra forward velocity towards player along -Z
        if (currentExtraZSpeed > 0f)
        {
            transform.localPosition += Vector3.back * (currentExtraZSpeed * Time.deltaTime);
        }

        // Active guidance towards player's X and Y
        if (zDistance > guidanceLockDistance && !isGuidanceLocked)
        {
            Vector3 playerPos = cachedPlayerTransform.position;
            Vector3 targetPos = playerPos + new Vector3(currentAimOffset.x, currentAimOffset.y, 0f);

            float currentX = transform.position.x;
            float currentY = transform.position.y;

            Vector2 diff = new Vector2(targetPos.x - currentX, targetPos.y - currentY);
            if (diff.sqrMagnitude > 0.04f)
            {
                Vector2 dir = diff.normalized;
                Vector2 step = dir * (currentLateralSpeed * Time.deltaTime);

                if (step.sqrMagnitude > diff.sqrMagnitude)
                    step = diff;

                transform.localPosition += new Vector3(step.x, step.y, 0f);
                lastLateralDirection = dir;
            }
        }
        else
        {
            // Within dodge window: lock guidance and maintain drift
            isGuidanceLocked = true;
            if (continueDriftAfterLock && lastLateralDirection.sqrMagnitude > 0.01f)
            {
                Vector2 drift = lastLateralDirection * (currentLateralSpeed * 0.75f * Time.deltaTime);
                transform.localPosition += new Vector3(drift.x, drift.y, 0f);
            }
        }
    }

    private void SelectRandomVariant()
    {
        activeVisual = null;
        activeColliders = System.Array.Empty<Collider>();

        CacheChildVariantsIfNeeded();

        // Pooled obstacles can retain collider state from their previous use.
        // Disable every variant collider before choosing the next visual.
        foreach (GameObject variant in asteroidVariants)
        {
            if (variant == null)
                continue;

            foreach (Collider variantCollider in
                     variant.GetComponentsInChildren<Collider>(true))
            {
                variantCollider.enabled = false;
            }
        }

        if (asteroidVariants == null || asteroidVariants.Length == 0)
        {
            if (box != null)
            {
                box.SetActive(true);
                activeVisual = box.transform;
                activeColliders = box.GetComponentsInChildren<Collider>(true);
            }

            return;
        }

        foreach (GameObject variant in asteroidVariants)
        {
            if (variant != null)
                variant.SetActive(false);
        }

        if (nextVariantIndex < 0)
            nextVariantIndex = Random.Range(0, asteroidVariants.Length);

        int selectedIndex = nextVariantIndex % asteroidVariants.Length;
        nextVariantIndex = (nextVariantIndex + 1) % asteroidVariants.Length;
        GameObject selected = asteroidVariants[selectedIndex];
        if (selected == null)
            return;

        selected.SetActive(true);
        box = selected;
        activeVisual = selected.transform;
        activeColliders = selected.GetComponentsInChildren<Collider>(true);
    }

    private void CacheChildVariantsIfNeeded()
    {
        if (asteroidVariants != null && asteroidVariants.Length > 0)
            return;

        asteroidVariants = System.Array.FindAll(
            GetComponentsInChildren<Transform>(true),
            child => child != transform && child.name.StartsWith("AsteroidVariant_"))
            .Select(child => child.gameObject)
            .ToArray();
    }

    private void InstallCollisionRelays()
    {
        foreach (GameObject variant in asteroidVariants)
        {
            if (variant == null)
                continue;

            Collider[] colliders = variant.GetComponentsInChildren<Collider>(true);
            foreach (Collider variantCollider in colliders)
            {
                ObstacleCollisionRelay relay =
                    variantCollider.GetComponent<ObstacleCollisionRelay>();

                if (relay == null)
                    relay = variantCollider.gameObject.AddComponent<ObstacleCollisionRelay>();

                relay.Initialize(this);
            }
        }
    }

    private void SetActiveVariantColliders(bool enabled)
    {
        foreach (Collider variantCollider in activeColliders)
        {
            if (variantCollider != null)
                variantCollider.enabled = enabled;
        }
    }

    public void HandleVariantTrigger(Collider sourceCollider, Collider other)
    {
        if (hitProcessed)
            return;

        if (!IsActiveVariantCollider(sourceCollider) || !HasActiveVisual())
            return;

        PlaneInteraction plane = other.GetComponentInParent<PlaneInteraction>();

        if (plane == null)
            return;

        hitProcessed = true;
        isHoming = false;
        plane.HitObstacle();
        SetActiveVariantColliders(false);
        gameObject.SetActive(false);
    }

    private bool IsActiveVariantCollider(Collider sourceCollider)
    {
        if (sourceCollider == null || !sourceCollider.enabled || activeVisual == null)
            return false;

        Transform sourceTransform = sourceCollider.transform;
        return sourceTransform == activeVisual || sourceTransform.IsChildOf(activeVisual);
    }

    private bool HasActiveVisual()
    {
        if (activeVisual == null || !activeVisual.gameObject.activeInHierarchy)
            return false;

        Renderer[] renderers = activeVisual.GetComponentsInChildren<Renderer>(false);
        return System.Array.Exists(
            renderers,
            renderer => renderer != null && renderer.enabled);
    }
}

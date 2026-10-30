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

    [Header("Ballistic Drift Towards Player (Natural Space Trajectory)")]
    [Tooltip("Enable realistic ballistic trajectory where a random portion of rocks aim towards the spaceship upon appearance without tracking during flight.")]
    [SerializeField] private bool enableBallisticTrajectory = true;

    [Tooltip("Percentage chance for an asteroid to launch on an incoming trajectory towards the spaceship (0.55 = 55% of rocks).")]
    [Range(0f, 1f)]
    [SerializeField] private float trajectoryChance = 0.55f;

    [Tooltip("Minimum distance in front of player (along Z) at which an incoming asteroid begins its dive.")]
    [SerializeField] private float minTriggerDistance = 50f;

    [Tooltip("Maximum distance in front of player (along Z) at which an incoming asteroid begins its dive.")]
    [SerializeField] private float maxTriggerDistance = 70f;

    [Tooltip("Maximum lateral (X/Y) drift speed in world units per second.")]
    [SerializeField] private float maxLateralSpeed = 6.0f;

    [Tooltip("Minimum extra forward speed along -Z towards the spaceship.")]
    [SerializeField] private float minExtraForwardSpeed = 4.0f;

    [Tooltip("Maximum extra forward speed along -Z towards the spaceship.")]
    [SerializeField] private float maxExtraForwardSpeed = 7.0f;

    [Tooltip("Random horizontal aim spread around the player corridor (X axis).")]
    [SerializeField] private float aimSpreadX = 0.5f;

    [Tooltip("Random vertical aim spread around the player corridor (Y axis).")]
    [SerializeField] private float aimSpreadY = 0.4f;

    private Collider[] activeColliders = System.Array.Empty<Collider>();
    private Transform activeVisual;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private bool hitProcessed;
    private Coroutine scaleCoroutine;
    private static int nextVariantIndex = -1;

    // Position reset caching across chunk recycles
    private Vector3 initialLocalPosition;
    private bool initialPositionCached;

    // Ballistic trajectory runtime state
    private static Transform cachedPlayerTransform;
    private static WorldMovementManager cachedWorldMovement;
    private bool isBallistic;
    private bool trajectoryCalculated;
    private float currentTriggerDistance;
    private Vector2 ballisticVelocity;
    private float currentExtraZSpeed;

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
        GameEvents.GameOver += StopBallistic;
        GameEvents.CrashLandingStarted += StopBallistic;
        GameEvents.VictoryOrbitStarted += StopBallistic;

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

        // Initialize ballistic trajectory behavior
        InitializeBallistic();
    }

    private void OnDisable()
    {
        GameEvents.VictorySequenceStarted -= BeginVictoryExit;
        GameEvents.GameOver -= StopBallistic;
        GameEvents.CrashLandingStarted -= StopBallistic;
        GameEvents.VictoryOrbitStarted -= StopBallistic;

        scaleCoroutine = null;
        isBallistic = false;
    }

    private void InitializeBallistic()
    {
        trajectoryCalculated = false;
        ballisticVelocity = Vector2.zero;
        currentExtraZSpeed = 0f;

        if (enableBallisticTrajectory && Random.value < trajectoryChance)
        {
            isBallistic = true;
            currentExtraZSpeed = Random.Range(minExtraForwardSpeed, maxExtraForwardSpeed);
            currentTriggerDistance = Random.Range(minTriggerDistance, maxTriggerDistance);
            EnsureReferences();
        }
        else
        {
            isBallistic = false;
        }
    }

    private static void EnsureReferences()
    {
        if (cachedPlayerTransform == null)
        {
            PlaneController planeCtrl = FindFirstObjectByType<PlaneController>();
            if (planeCtrl != null)
                cachedPlayerTransform = planeCtrl.transform;
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    cachedPlayerTransform = playerObj.transform;
            }
        }

        if (cachedWorldMovement == null)
        {
            cachedWorldMovement = FindFirstObjectByType<WorldMovementManager>();
        }
    }

    private void StopBallistic()
    {
        isBallistic = false;
    }

    private void BeginVictoryExit(float duration)
    {
        isBallistic = false;

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

        UpdateBallisticMovement();
    }

    private void UpdateBallisticMovement()
    {
        if (!isBallistic || hitProcessed)
            return;

        EnsureReferences();

        if (cachedWorldMovement != null && !cachedWorldMovement.IsMovementEnabled)
            return;

        if (cachedPlayerTransform == null)
            return;

        float deltaZ = transform.position.z - cachedPlayerTransform.position.z;

        // If the rock has already crossed the player's plane, stop ballistic motion
        if (deltaZ <= 0.5f)
        {
            isBallistic = false;
            return;
        }

        // Before locking trajectory, wait until the rock enters the engagement / visual zone!
        if (!trajectoryCalculated)
        {
            // If still too far in the distance, wait and let chunk bring it closer naturally
            if (deltaZ > currentTriggerDistance)
                return;

            // If too close (behind reaction window, e.g. < 12m), don't perform sudden unfair dive
            if (deltaZ < 12f)
            {
                isBallistic = false;
                return;
            }

            // Ensure scale-in animation is completed so the rock is fully visible
            if (scaleCoroutine != null)
                return;

            // LOCK TRAJECTORY ONCE at this precise moment
            trajectoryCalculated = true;

            float worldSpeed = (cachedWorldMovement != null && cachedWorldMovement.WorldSpeed > 0.1f)
                ? cachedWorldMovement.WorldSpeed
                : 10f;

            float totalZSpeed = worldSpeed + currentExtraZSpeed;
            float travelTime = deltaZ / totalZSpeed;

            if (travelTime <= 0.05f)
            {
                isBallistic = false;
                return;
            }

            // Target position: player's CURRENT position at this second + tight spread
            Vector3 playerPos = cachedPlayerTransform.position;
            float targetX = playerPos.x + Random.Range(-aimSpreadX, aimSpreadX);
            float targetY = playerPos.y + Random.Range(-aimSpreadY, aimSpreadY);

            // Keep target within player's reachable flight zone [-4.5, 4.5] and [-3.2, 3.2]
            targetX = Mathf.Clamp(targetX, -4.5f, 4.5f);
            targetY = Mathf.Clamp(targetY, -3.2f, 3.2f);

            // World velocity required to intercept target position
            float velX = (targetX - transform.position.x) / travelTime;
            float velY = (targetY - transform.position.y) / travelTime;

            Vector2 lateral = new Vector2(velX, velY);
            if (lateral.magnitude > maxLateralSpeed)
                lateral = lateral.normalized * maxLateralSpeed;

            ballisticVelocity = lateral;

            // Visual feedback: spin slightly faster as it hurtles towards the player
            rotationSpeed *= 1.35f;
        }

        // Apply constant ballistic motion in WORLD SPACE (completely immune to parent scale distortion!)
        Vector3 worldDisplacement = new Vector3(
            ballisticVelocity.x,
            ballisticVelocity.y,
            -currentExtraZSpeed
        ) * Time.deltaTime;

        transform.position += worldDisplacement;
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
        isBallistic = false;
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

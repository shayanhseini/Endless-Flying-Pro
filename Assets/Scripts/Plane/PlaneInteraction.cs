using System.Collections;
using UnityEngine;

public class PlaneInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShieldController shield;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameAudioController gameAudioController;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffect;

    [Header("Atmosphere Orbit")]
    [Min(0f)]
    [Tooltip("Short pause used for the impact camera shake before flying toward the planet.")]
    [SerializeField] private float impactPauseDuration = 0.45f;
    [Min(1f)]
    [Tooltip("Maximum world-units-per-second speed reached during the middle of the approach.")]
    [SerializeField] private float atmosphereApproachSpeed = 100f;
    [Min(1f)]
    [Tooltip("Far approaches raise their middle speed so this approximate travel time is not exceeded too much.")]
    [SerializeField] private float distanceSpeedReferenceTime = 5f;
    [Min(1.01f)]
    [Tooltip("Orbit radius relative to the visible radius of the planet.")]
    [SerializeField] private float atmosphereRadiusMultiplier = 1.15f;
    [Min(0.1f)]
    [Tooltip("How long the plane drifts through the planet's atmosphere.")]
    [UnityEngine.Serialization.FormerlySerializedAs("orbitDuration")]
    [SerializeField] private float atmosphereDriftDuration = 0f;
    [Min(0f)]
    [Tooltip("How shortly before reaching the atmosphere the screen starts fading to black.")]
    [SerializeField] private float restartFadeLeadTime = 0.5f;
    [Min(0.01f)]
    [Tooltip("Slow linear drifting speed after entering the atmosphere.")]
    [SerializeField] private float atmosphereDriftSpeed = 3f;
    [Range(1f, 4f)]
    [Tooltip("Entry speed relative to the regular atmosphere drift speed.")]
    [SerializeField] private float atmosphereEntrySpeedMultiplier = 1.35f;
    [Min(0.1f)]
    [Tooltip("Time used to smoothly reduce entry speed to the regular atmosphere drift speed.")]
    [UnityEngine.Serialization.FormerlySerializedAs("orbitSlowdownDuration")]
    [SerializeField] private float atmosphereSlowdownDuration = 3f;
    private bool atmosphereSequenceActive;
    private Quaternion gameplayRotation;

    private void Awake()
    {
        gameplayRotation = transform.rotation;
    }
    
    public void HitObstacle()
    {
        GameEvents.RaisePlaneHit();

        PlayHitEffect();
        gameAudioController.PlayExplosionSound(this.transform);

        if (shield != null && shield.IsActive)
        {
            shield.Consume();
            return;
        }
        
        DestroyPlane();
    }


    private void PlayHitEffect()
    {
        if (hitEffect == null)
            return;

        hitEffect.SetActive(false);

        hitEffect.transform.position =
            transform.position;

        hitEffect.SetActive(true);
    }

    

    private void DestroyPlane()
    {
        if (atmosphereSequenceActive || gameManager == null)
            return;

        transform.rotation = gameplayRotation;
        if (!gameManager.BeginCrashLanding())
            return;

        atmosphereSequenceActive = true;
        DisablePlaneColliders();

        PlanetSequenceManager sequence = FindFirstObjectByType<PlanetSequenceManager>();
        Transform planet = sequence != null ? sequence.PrepareCrashLanding() : null;
        StartCoroutine(AtmosphereOrbitRoutine(planet, false, impactPauseDuration));
    }

    public void BeginVictoryOrbit(Transform planet)
    {
        if (atmosphereSequenceActive || gameManager == null)
            return;

        transform.rotation = gameplayRotation;
        if (!gameManager.BeginVictoryOrbit())
            return;

        atmosphereSequenceActive = true;
        DisablePlaneColliders();
        StartCoroutine(AtmosphereOrbitRoutine(planet, true, 0f));
    }

    private void DisablePlaneColliders()
    {
        Collider[] planeColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider planeCollider in planeColliders)
            planeCollider.enabled = false;
    }

    private IEnumerator AtmosphereOrbitRoutine(
        Transform planet,
        bool victory,
        float initialDelay)
    {
        WorldMovementManager movement = FindFirstObjectByType<WorldMovementManager>();
        float initialFlightSpeed = movement != null
            ? Mathf.Max(0.1f, movement.WorldSpeed)
            : 10f;

        float delayElapsed = 0f;
        while (delayElapsed < initialDelay)
        {
            float delta = Time.deltaTime;
            delayElapsed += delta;
            transform.position += transform.forward * initialFlightSpeed * delta;
            transform.rotation = gameplayRotation;
            yield return null;
        }

        if (planet == null)
        {
            yield return new WaitForSeconds(1f);
            FinishAtmosphereSequence(victory);
            yield break;
        }

        Bounds planetBounds = CalculatePlanetBounds(planet);
        Vector3 planetCenter = planetBounds.center;
        float visibleRadius = Mathf.Max(
            planetBounds.extents.x,
            planetBounds.extents.y,
            planetBounds.extents.z);
        float atmosphereRadius = visibleRadius * atmosphereRadiusMultiplier;
        Vector3 startPosition = transform.position;
        transform.rotation = gameplayRotation;
        Vector3 entryDirection = (startPosition - planetCenter).normalized;
        if (entryDirection.sqrMagnitude < 0.001f)
            entryDirection = Vector3.back;
        Vector3 atmosphereEntry = planetCenter + entryDirection * atmosphereRadius;
        Vector3 travelDirection = (atmosphereEntry - startPosition).normalized;
        Vector3 orbitAxis = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(entryDirection, orbitAxis)) > 0.95f)
            orbitAxis = Vector3.right;
        Vector3 entryTangent = Vector3.Cross(orbitAxis, entryDirection).normalized;
        float approachDistance = Vector3.Distance(startPosition, atmosphereEntry);
        Vector3 firstControlPoint = startPosition +
            travelDirection * approachDistance * 0.35f -
            entryTangent * approachDistance * 0.22f;
        float desiredEntryLinearSpeed = atmosphereDriftSpeed * atmosphereEntrySpeedMultiplier;
        float entryControlDistance = Mathf.Clamp(
            desiredEntryLinearSpeed * 2f,
            approachDistance * 0.05f,
            approachDistance * 0.25f);
        Vector3 secondControlPoint = atmosphereEntry - entryTangent * entryControlDistance;
        Quaternion atmosphereRotation = Quaternion.LookRotation(entryTangent, Vector3.up);
        float pathLength = EstimateCubicBezierLength(
            startPosition,
            firstControlPoint,
            secondControlPoint,
            atmosphereEntry);
        float distanceBlend = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(150f, 800f, pathLength));
        float closeApproachSpeed = Mathf.Max(initialFlightSpeed * 1.5f, 40f);
        float closeAwareBaseSpeed = Mathf.Lerp(
            closeApproachSpeed,
            atmosphereApproachSpeed,
            distanceBlend);
        float farDistanceSpeed =
            pathLength / Mathf.Max(1f, distanceSpeedReferenceTime) * 1.8f;
        float distanceAdjustedMaximumSpeed = Mathf.Max(
            closeAwareBaseSpeed,
            farDistanceSpeed);
        float distanceAlongPath = 0f;
        bool dangerCleared = false;
        bool restartFadeStarted = false;

        while (distanceAlongPath < pathLength && planet != null)
        {
            float progress = Mathf.Clamp01(distanceAlongPath / Mathf.Max(0.01f, pathLength));
            if (!dangerCleared && progress >= 0.55f)
            {
                dangerCleared = true;
                GameEvents.RaiseAtmosphereReached();
            }

            float currentSpeed = CalculateApproachSpeed(
                progress,
                initialFlightSpeed,
                distanceAdjustedMaximumSpeed,
                desiredEntryLinearSpeed);
            float remainingDistance = Mathf.Max(0f, pathLength - distanceAlongPath);
            float estimatedTimeRemaining = remainingDistance / Mathf.Max(0.01f, currentSpeed);
            if (!victory && !restartFadeStarted &&
                estimatedTimeRemaining <= restartFadeLeadTime)
            {
                restartFadeStarted = true;
                gameManager.BeginRestartFade();
            }

            distanceAlongPath += currentSpeed * Time.deltaTime;
            progress = Mathf.Clamp01(distanceAlongPath / Mathf.Max(0.01f, pathLength));
            transform.position = CubicBezier(
                startPosition,
                firstControlPoint,
                secondControlPoint,
                atmosphereEntry,
                progress);

            Vector3 pathDirection = CubicBezierTangent(
                startPosition,
                firstControlPoint,
                secondControlPoint,
                atmosphereEntry,
                progress);
            Quaternion movementRotation = Quaternion.LookRotation(pathDirection, Vector3.up);
            float rotationBlend = 1f - Mathf.Exp(-8f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                movementRotation,
                rotationBlend);

            yield return null;
        }

        if (planet == null)
        {
            FinishAtmosphereSequence(victory);
            yield break;
        }

        if (!dangerCleared)
            GameEvents.RaiseAtmosphereReached();

        if (!victory && !restartFadeStarted)
            gameManager.BeginRestartFade();

        Quaternion fixedAtmosphereRotation = atmosphereRotation;
        transform.SetPositionAndRotation(atmosphereEntry, fixedAtmosphereRotation);
        Vector3 driftDirection = entryTangent;
        float entryDriftSpeed = atmosphereDriftSpeed * atmosphereEntrySpeedMultiplier;
        float elapsed = 0f;
        while (elapsed < atmosphereDriftDuration && planet != null)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            float slowdownProgress = Mathf.Clamp01(
                elapsed / Mathf.Max(0.01f, atmosphereSlowdownDuration));
            slowdownProgress = Mathf.SmoothStep(0f, 1f, slowdownProgress);
            float currentDriftSpeed = Mathf.Lerp(
                entryDriftSpeed,
                atmosphereDriftSpeed,
                slowdownProgress);
            transform.position += driftDirection * currentDriftSpeed * delta;
            transform.rotation = fixedAtmosphereRotation;
            yield return null;
        }

        FinishAtmosphereSequence(victory);
    }

    private void FinishAtmosphereSequence(bool victory)
    {
        if (victory)
            gameManager.WinGame();
        else
            gameManager.RestartGame();
    }

    private static float CalculateApproachSpeed(
        float progress,
        float initialSpeed,
        float maximumSpeed,
        float entrySpeed)
    {
        maximumSpeed = Mathf.Max(initialSpeed, maximumSpeed);

        if (progress < 0.35f)
        {
            float acceleration = Mathf.SmoothStep(0f, 1f, progress / 0.35f);
            return Mathf.Lerp(initialSpeed, maximumSpeed, acceleration);
        }

        if (progress < 0.55f)
            return maximumSpeed;

        float slowdown = Mathf.InverseLerp(0.55f, 1f, progress);
        slowdown = Mathf.SmoothStep(0f, 1f, slowdown);
        return Mathf.Lerp(maximumSpeed, entrySpeed, slowdown);
    }

    private static float EstimateCubicBezierLength(
        Vector3 start,
        Vector3 controlA,
        Vector3 controlB,
        Vector3 end)
    {
        const int sampleCount = 32;
        float length = 0f;
        Vector3 previous = start;

        for (int i = 1; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            Vector3 current = CubicBezier(start, controlA, controlB, end, t);
            length += Vector3.Distance(previous, current);
            previous = current;
        }

        return Mathf.Max(0.01f, length);
    }

    private static Vector3 CubicBezier(
        Vector3 start,
        Vector3 controlA,
        Vector3 controlB,
        Vector3 end,
        float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * inverse * start +
               3f * inverse * inverse * t * controlA +
               3f * inverse * t * t * controlB +
               t * t * t * end;
    }

    private static Vector3 CubicBezierTangent(
        Vector3 start,
        Vector3 controlA,
        Vector3 controlB,
        Vector3 end,
        float t)
    {
        float inverse = 1f - t;
        Vector3 tangent =
            3f * inverse * inverse * (controlA - start) +
            6f * inverse * t * (controlB - controlA) +
            3f * t * t * (end - controlB);

        return tangent.sqrMagnitude > 0.0001f
            ? tangent.normalized
            : Vector3.forward;
    }

    private static Bounds CalculatePlanetBounds(Transform planet)
    {
        Renderer[] renderers = planet.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(planet.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}

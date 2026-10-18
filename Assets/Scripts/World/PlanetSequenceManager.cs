using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the Solar System planets in order while the existing world movement
/// continues to drive the illusion of forward flight. After Neptune the route
/// loops back to Mercury, keeping the current version endless.
/// </summary>
public class PlanetSequenceManager : MonoBehaviour
{
    private sealed class ActivePlanet
    {
        public Transform Transform;
        public Vector3 InitialScale;
        public Vector3 TargetScale;
        public Vector3 RotationAxis;
        public float RotationSpeed;
    }

    [Serializable]
    private class PlanetDefinition
    {
        public string displayName;
        public GameObject prefab;
    }

    private static readonly string[] DefaultNames =
    {
        "Mercury", "Venus", "Earth", "Mars",
        "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    // Sidereal rotation speed relative to Earth. Negative values are retrograde.
    private static readonly float[] RealRotationSpeedMultipliers =
    {
        0.017f, -0.0041f, 1f, 0.972f,
        2.413f, 2.245f, -1.389f, 1.485f
    };

    // Axial tilt in degrees. Retrograde direction is handled by rotation speed.
    private static readonly float[] AxialTilts =
    {
        0.034f, 2.64f, 23.44f, 25.19f,
        3.13f, 26.73f, 82.23f, 28.32f
    };

    [Header("Planet Assets")]
    [SerializeField] private PlanetDefinition[] planets = Array.Empty<PlanetDefinition>();

    [Header("Timing")]
    [Min(1f)]
    [SerializeField] private float secondsBetweenPlanets = 30f;
    [Min(1f)]
    [SerializeField] private float planetTravelDuration = 30f;
    [SerializeField] private bool spawnFirstPlanetImmediately = true;

    [Header("Placement")]
    [Min(1f)]
    [SerializeField] private float spawnDistance = 2000f;
    [Range(0.01f, 1f)]
    [SerializeField] private float initialScaleMultiplier = 0.05f;
    [Range(0.1f, 1f)]
    [SerializeField] private float scaleGrowthJourneyFraction = 0.75f;
    [SerializeField] private float leftSideX = -250f;
    [SerializeField] private float rightSideX = 250f;
    [Min(1f)]
    [SerializeField] private float minimumPlanetScale = 100f;
    [Min(1f)]
    [SerializeField] private float minimumMercuryToPlaneRatio = 20f;
    [Min(1f)]
    [SerializeField] private float referencePlanetSize = 150f;
    [Min(1f)]
    [SerializeField] private float referenceSideDistance = 250f;
    [SerializeField] private Vector2 verticalRange = new(8f, 28f);
    [Min(0f)]
    [SerializeField] private float despawnDistanceBehindPlayer = 300f;
    [Min(0.01f)]
    [SerializeField] private float earthRotationDegreesPerSecond = 12f;
    [Min(0f)]
    [SerializeField] private float minimumVisibleRotationSpeed = 1.5f;

    private readonly List<ActivePlanet> activePlanets = new();
    private Transform player;
    private WorldMovementManager worldMovement;
    private float elapsed;
    private int nextPlanetIndex;
    private bool initialized;
    private float minimumVisualPlanetScale;

    public void Initialize(Transform playerTransform, WorldMovementManager movementManager)
    {
        if (initialized)
            return;

        player = playerTransform;
        worldMovement = movementManager;
        EnsureDefinitions();
        minimumVisualPlanetScale = Mathf.Max(
            minimumPlanetScale,
            GetVisualSize(player) * minimumMercuryToPlaneRatio);
        elapsed = spawnFirstPlanetImmediately ? secondsBetweenPlanets : 0f;
        initialized = true;
    }

    private void Awake()
    {
        EnsureDefinitions();
    }

    private void Update()
    {
        if (!initialized || worldMovement == null || player == null)
            return;

        if (!worldMovement.IsMovementEnabled)
            return;

        MoveActivePlanets();

        elapsed += Time.deltaTime;
        if (elapsed < secondsBetweenPlanets)
            return;

        elapsed -= secondsBetweenPlanets;
        SpawnNextPlanet();
    }

    private void MoveActivePlanets()
    {
        float approachSpeed = spawnDistance / Mathf.Max(1f, planetTravelDuration);
        Vector3 movement = Vector3.back * approachSpeed * Time.deltaTime;

        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            ActivePlanet activePlanet = activePlanets[i];
            Transform planet = activePlanet.Transform;
            if (planet == null)
            {
                activePlanets.RemoveAt(i);
                continue;
            }

            planet.position += movement;
            planet.Rotate(
                activePlanet.RotationAxis,
                activePlanet.RotationSpeed * Time.deltaTime,
                Space.Self);

            float distanceAhead = planet.position.z - player.position.z;
            float distanceTravelled = spawnDistance - distanceAhead;
            float growthDistance = spawnDistance * scaleGrowthJourneyFraction;
            float scaleProgress = Mathf.Clamp01(
                distanceTravelled / Mathf.Max(1f, growthDistance));
            planet.localScale = Vector3.Lerp(
                activePlanet.InitialScale,
                activePlanet.TargetScale,
                scaleProgress);

            if (planet.position.z < player.position.z - despawnDistanceBehindPlayer)
            {
                Destroy(planet.gameObject);
                activePlanets.RemoveAt(i);
            }
        }
    }

    private void SpawnNextPlanet()
    {
        if (planets.Length == 0)
            return;

        PlanetDefinition definition = planets[nextPlanetIndex];
        if (definition.prefab == null)
            return;

        GameObject instance = Instantiate(definition.prefab, transform);

        instance.name = $"Planet_{nextPlanetIndex + 1:00}_{definition.displayName}";
        instance.SetActive(true);
        DisableColliders(instance);

        float prefabSize = LargestScaleComponent(instance.transform.localScale);
        float visualScaleMultiplier = prefabSize > Mathf.Epsilon
            ? minimumVisualPlanetScale / minimumPlanetScale
            : 1f;
        Vector3 targetScale = EnsureMinimumScale(
            instance.transform.localScale * visualScaleMultiplier,
            minimumVisualPlanetScale);
        float planetSize = LargestScaleComponent(targetScale);
        float distanceMultiplier = Mathf.Max(1f, planetSize / referencePlanetSize);
        float sideDistance = referenceSideDistance * distanceMultiplier;
        float x = nextPlanetIndex % 2 == 0
            ? -Mathf.Max(Mathf.Abs(leftSideX), sideDistance)
            : Mathf.Max(Mathf.Abs(rightSideX), sideDistance);
        float y = UnityEngine.Random.Range(verticalRange.x, verticalRange.y);
        instance.transform.position = player.position + new Vector3(x, y, spawnDistance);
        Vector3 initialScale = targetScale * initialScaleMultiplier;
        instance.transform.localScale = initialScale;

        activePlanets.Add(new ActivePlanet
        {
            Transform = instance.transform,
            InitialScale = initialScale,
            TargetScale = targetScale,
            RotationAxis = Quaternion.AngleAxis(
                AxialTilts[nextPlanetIndex],
                Vector3.forward) * Vector3.up,
            RotationSpeed = GetVisibleRotationSpeed(nextPlanetIndex)
        });

        Debug.Log($"Current planet: {definition.displayName}");
        nextPlanetIndex = (nextPlanetIndex + 1) % planets.Length;
    }

    private static Vector3 EnsureMinimumScale(Vector3 scale, float minimum)
    {
        float largest = LargestScaleComponent(scale);
        if (largest >= minimum || largest <= Mathf.Epsilon)
            return scale;

        return scale * (minimum / largest);
    }

    private static float LargestScaleComponent(Vector3 scale)
    {
        return Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z));
    }

    private static float GetVisualSize(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return LargestScaleComponent(target.lossyScale);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    }

    private float GetVisibleRotationSpeed(int planetIndex)
    {
        float realMultiplier = RealRotationSpeedMultipliers[planetIndex];
        float compressedMultiplier = Mathf.Sign(realMultiplier) *
            Mathf.Sqrt(Mathf.Abs(realMultiplier));
        float speed = earthRotationDegreesPerSecond * compressedMultiplier;

        if (Mathf.Abs(speed) >= minimumVisibleRotationSpeed)
            return speed;

        return Mathf.Sign(speed) * minimumVisibleRotationSpeed;
    }

    private void EnsureDefinitions()
    {
        if (planets != null && planets.Length == DefaultNames.Length)
            return;

        planets = new PlanetDefinition[DefaultNames.Length];
        for (int i = 0; i < planets.Length; i++)
        {
            planets[i] = new PlanetDefinition
            {
                displayName = DefaultNames[i]
            };
        }
    }

    private static void DisableColliders(GameObject planet)
    {
        Collider[] colliders = planet.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = false;
    }

}

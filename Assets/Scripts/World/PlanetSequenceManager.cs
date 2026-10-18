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

    [Header("Planet Assets")]
    [SerializeField] private PlanetDefinition[] planets = Array.Empty<PlanetDefinition>();

    [Header("Timing")]
    [Min(1f)]
    [SerializeField] private float secondsBetweenPlanets = 30f;
    [SerializeField] private bool spawnFirstPlanetImmediately = true;

    [Header("Placement")]
    [Min(1f)]
    [SerializeField] private float spawnDistance = 1500f;
    [Min(0.001f)]
    [SerializeField] private float initialScaleMultiplier = 0.05f;
    [Min(0f)]
    [SerializeField] private float fullScaleDistance = 350f;
    [SerializeField] private float leftSideX = -180f;
    [SerializeField] private float rightSideX = 180f;
    [Min(1f)]
    [SerializeField] private float minimumPlanetScale = 100f;
    [Min(1f)]
    [SerializeField] private float referencePlanetSize = 150f;
    [Min(1f)]
    [SerializeField] private float referenceSideDistance = 180f;
    [SerializeField] private Vector2 verticalRange = new(8f, 28f);
    [Min(0f)]
    [SerializeField] private float despawnDistanceBehindPlayer = 300f;
    [SerializeField] private Vector3 rotationPerSecond = new(0f, 5f, 0f);

    private readonly List<ActivePlanet> activePlanets = new();
    private Transform player;
    private WorldMovementManager worldMovement;
    private float elapsed;
    private int nextPlanetIndex;
    private bool initialized;

    public void Initialize(Transform playerTransform, WorldMovementManager movementManager)
    {
        if (initialized)
            return;

        player = playerTransform;
        worldMovement = movementManager;
        EnsureDefinitions();
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
        float approachSpeed = spawnDistance / Mathf.Max(1f, secondsBetweenPlanets);
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
            planet.Rotate(rotationPerSecond * Time.deltaTime, Space.Self);

            float distanceAhead = planet.position.z - player.position.z;
            float scaleProgress = 1f - Mathf.InverseLerp(
                fullScaleDistance,
                spawnDistance,
                distanceAhead
            );
            scaleProgress = Mathf.SmoothStep(0f, 1f, scaleProgress);
            planet.localScale = Vector3.Lerp(
                activePlanet.InitialScale,
                activePlanet.TargetScale,
                scaleProgress
            );

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

        Vector3 targetScale = EnsureMinimumScale(
            instance.transform.localScale,
            minimumPlanetScale);
        float planetSize = LargestScaleComponent(targetScale);
        float distanceMultiplier = Mathf.Max(1f, planetSize / referencePlanetSize);
        float sideDistance = referenceSideDistance * distanceMultiplier;
        float x = nextPlanetIndex % 2 == 0
            ? -Mathf.Max(Mathf.Abs(leftSideX), sideDistance)
            : Mathf.Max(Mathf.Abs(rightSideX), sideDistance);
        float y = UnityEngine.Random.Range(verticalRange.x, verticalRange.y);
        instance.transform.position = player.position + new Vector3(x, y, spawnDistance);
        Vector3 initialScale = EnsureMinimumScale(
            targetScale * initialScaleMultiplier,
            minimumPlanetScale);
        instance.transform.localScale = initialScale;

        activePlanets.Add(new ActivePlanet
        {
            Transform = instance.transform,
            InitialScale = initialScale,
            TargetScale = targetScale
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

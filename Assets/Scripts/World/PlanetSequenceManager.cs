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
        public Vector3 TargetScale;
    }

    [Serializable]
    private class PlanetDefinition
    {
        public string displayName;
        public GameObject prefab;
        public Color fallbackColor = Color.white;
        [Min(0.1f)] public float scale = 1f;
    }

    private static readonly string[] DefaultNames =
    {
        "Mercury", "Venus", "Earth", "Mars",
        "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    private static readonly Color[] DefaultColors =
    {
        new(0.55f, 0.52f, 0.48f),
        new(0.85f, 0.62f, 0.30f),
        new(0.18f, 0.42f, 0.85f),
        new(0.72f, 0.24f, 0.12f),
        new(0.76f, 0.60f, 0.42f),
        new(0.82f, 0.73f, 0.50f),
        new(0.45f, 0.82f, 0.86f),
        new(0.20f, 0.34f, 0.82f)
    };

    [Header("Planet Assets")]
    [Tooltip("The current sample planet. It is cloned when a planet-specific prefab is not assigned.")]
    [SerializeField] private GameObject referencePlanet;
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
    [SerializeField] private Vector2 horizontalRange = new(-35f, 35f);
    [SerializeField] private Vector2 verticalRange = new(8f, 28f);
    [SerializeField] private bool useReferenceSideOffset = true;
    [Min(0f)]
    [SerializeField] private float despawnDistanceBehindPlayer = 300f;
    [SerializeField] private Vector3 rotationPerSecond = new(0f, 5f, 0f);

    private readonly List<ActivePlanet> activePlanets = new();
    private Transform player;
    private WorldMovementManager worldMovement;
    private float elapsed;
    private int nextPlanetIndex;
    private bool initialized;
    private Vector2 referenceSideOffset;
    private bool hasReferenceSideOffset;

    public void Initialize(Transform playerTransform, WorldMovementManager movementManager)
    {
        if (initialized)
            return;

        player = playerTransform;
        worldMovement = movementManager;
        EnsureDefinitions();
        FindSceneReferenceIfNeeded();

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
        Vector3 movement = Vector3.back * worldMovement.WorldSpeed * Time.deltaTime;

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
                activePlanet.TargetScale * initialScaleMultiplier,
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
        GameObject source = definition.prefab != null ? definition.prefab : referencePlanet;
        GameObject instance = source != null
            ? Instantiate(source, transform)
            : CreateFallbackPlanet(definition);

        instance.name = $"Planet_{nextPlanetIndex + 1:00}_{definition.displayName}";
        instance.SetActive(true);
        DisableColliders(instance);

        float x = hasReferenceSideOffset && useReferenceSideOffset
            ? referenceSideOffset.x
            : UnityEngine.Random.Range(horizontalRange.x, horizontalRange.y);
        float y = hasReferenceSideOffset && useReferenceSideOffset
            ? referenceSideOffset.y
            : UnityEngine.Random.Range(verticalRange.x, verticalRange.y);
        instance.transform.position = player.position + new Vector3(x, y, spawnDistance);
        instance.transform.localScale *= Mathf.Max(0.1f, definition.scale);

        Vector3 targetScale = instance.transform.localScale;
        instance.transform.localScale = targetScale * initialScaleMultiplier;

        activePlanets.Add(new ActivePlanet
        {
            Transform = instance.transform,
            TargetScale = targetScale
        });

        Debug.Log($"Current planet: {definition.displayName}");
        nextPlanetIndex = (nextPlanetIndex + 1) % planets.Length;
    }

    private GameObject CreateFallbackPlanet(PlanetDefinition definition)
    {
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.transform.SetParent(transform);

        Renderer renderer = planet.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new(renderer.sharedMaterial);
            material.color = definition.fallbackColor;
            renderer.material = material;
        }

        return planet;
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
                displayName = DefaultNames[i],
                fallbackColor = DefaultColors[i],
                scale = 1f
            };
        }
    }

    private void FindSceneReferenceIfNeeded()
    {
        if (referencePlanet != null)
            return;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform candidate in sceneTransforms)
        {
            if (candidate == transform || candidate == player)
                continue;

            string candidateName = candidate.name.ToLowerInvariant();
            if (!LooksLikePlanetReference(candidateName))
                continue;

            referencePlanet = candidate.gameObject;
            Vector3 offset = referencePlanet.transform.position - player.position;
            referenceSideOffset = new Vector2(offset.x, offset.y);
            hasReferenceSideOffset = true;
            referencePlanet.SetActive(false);
            break;
        }
    }

    private static bool LooksLikePlanetReference(string candidateName)
    {
        if (candidateName.Contains("planet") ||
            candidateName.Contains("سیاره") ||
            candidateName == "sample")
            return true;

        foreach (string planetName in DefaultNames)
        {
            if (candidateName.Contains(planetName.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static void DisableColliders(GameObject planet)
    {
        Collider[] colliders = planet.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = false;
    }

}

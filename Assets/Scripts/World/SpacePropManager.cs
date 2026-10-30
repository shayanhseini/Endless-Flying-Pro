using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns random visual decorative space objects (satellites, orbital stations, deep-space probes)
/// along the flight route with authentic zero-gravity floating and tumbling behavior.
/// Keeps props in side corridors outside the gameplay obstacle lane, and dynamically ensures
/// props are spawned strictly on the side OPPOSITE to passing planets to prevent visual clipping.
/// </summary>
public class SpacePropManager : MonoBehaviour
{
    [Header("Prop Prefabs")]
    [Tooltip("Prefabs to spawn. If empty, automatically loads from Resources/SpaceProps.")]
    [SerializeField] private GameObject[] propPrefabs;

    [Header("Spawn Interval")]
    [SerializeField] private float minSpawnDelay = 9f;
    [SerializeField] private float maxSpawnDelay = 20f;
    [SerializeField] private float initialSpawnDelay = 5f;

    [Header("Spawn Position Ahead")]
    [SerializeField] private float spawnDistanceAheadMin = 260f;
    [SerializeField] private float spawnDistanceAheadMax = 420f;

    [Header("Corridor Placement")]
    [Tooltip("Minimum distance from center lane (X=0) to keep clear of obstacles.")]
    [SerializeField] private float lateralMinDistance = 35f;
    [Tooltip("Maximum distance from center lane for scenic vista props (kept well clear of planet bounds).")]
    [SerializeField] private float lateralMaxDistance = 75f;
    [SerializeField] private float altitudeMin = -20f;
    [SerializeField] private float altitudeMax = 42f;

    [Header("Limits & Scale")]
    [SerializeField] private int maxActiveProps = 6;
    [SerializeField] private Vector2 randomScaleMultiplier = new Vector2(0.9f, 1.2f);

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private WorldMovementManager worldMovement;
    [SerializeField] private PlanetSequenceManager planetSequence;

    private readonly List<GameObject> activePropInstances = new List<GameObject>();
    private float nextSpawnTime;
    private bool isRunning;
    private int lastPrefabIndex = -1;

    public void Initialize(Transform playerTransform, WorldMovementManager movementManager, PlanetSequenceManager sequenceManager = null)
    {
        player = playerTransform;
        worldMovement = movementManager;
        if (sequenceManager != null)
            planetSequence = sequenceManager;
    }

    private void Awake()
    {
        EnsureReferences();
        LoadPrefabsIfNeeded();
    }

    private void OnEnable()
    {
        GameEvents.GameStarted += StartSpawning;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.GameWon += HandleGameWon;
        GameEvents.CrashLandingStarted += HandleCrash;
        GameEvents.PlanetIntroductionStarted += HandlePlanetIntroductionStarted;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= StartSpawning;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.GameWon -= HandleGameWon;
        GameEvents.CrashLandingStarted -= HandleCrash;
        GameEvents.PlanetIntroductionStarted -= HandlePlanetIntroductionStarted;
    }

    private void HandlePlanetIntroductionStarted(string planetName, string desc, float duration, bool onLeft, int planetIndex)
    {
        // When approaching or introducing the Sun (index 8), immediately clear all artificial space props
        if (planetIndex == 8 || (planetSequence != null && planetSequence.IsSunApproachingOrActive))
        {
            ClearAllProps();
        }
    }

    private void Start()
    {
        EnsureReferences();
        if (worldMovement != null && worldMovement.IsMovementEnabled)
        {
            StartSpawning();
        }
    }

    private void EnsureReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (worldMovement == null)
        {
            worldMovement = Object.FindFirstObjectByType<WorldMovementManager>();
        }

        if (planetSequence == null)
        {
            planetSequence = GetComponent<PlanetSequenceManager>();
            if (planetSequence == null)
                planetSequence = Object.FindFirstObjectByType<PlanetSequenceManager>();
        }
    }

    private void LoadPrefabsIfNeeded()
    {
        if (propPrefabs == null || propPrefabs.Length == 0)
        {
            GameObject[] loaded = Resources.LoadAll<GameObject>("SpaceProps");
            if (loaded != null && loaded.Length > 0)
            {
                propPrefabs = loaded;
            }
        }
    }

    private void StartSpawning()
    {
        isRunning = true;
        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    private void HandleGameOver()
    {
        isRunning = false;
        ClearAllProps();
    }

    private void HandleGameWon()
    {
        isRunning = false;
    }

    private void HandleCrash()
    {
        // Keep existing props floating gracefully during crash sequence
    }

    private void Update()
    {
        CleanDestroyedProps();
        AvoidActivePlanetIntersections();

        // Near the Sun: suppress spawning and immediately clear all existing artificial satellites
        if (planetSequence != null && planetSequence.IsSunApproachingOrActive)
        {
            if (activePropInstances.Count > 0)
            {
                ClearAllProps();
            }
            return;
        }

        if (!isRunning) return;
        if (worldMovement == null || !worldMovement.IsMovementEnabled) return;
        if (player == null) return;

        if (Time.time >= nextSpawnTime)
        {
            if (activePropInstances.Count < maxActiveProps)
            {
                SpawnRandomProp();
            }

            nextSpawnTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
        }
    }

    /// <summary>
    /// Despawns any floating prop that enters the spatial clearance zone of a passing active planet
    /// to completely prevent props appearing inside or through planet atmospheres.
    /// </summary>
    private void AvoidActivePlanetIntersections()
    {
        if (planetSequence == null) return;

        if (planetSequence.TryGetActivePlanet(out Transform activePlanet, out float planetRadius, out bool planetOnLeft))
        {
            Vector3 planetPos = activePlanet.position;
            float safeClearanceDistance = planetRadius + 45f;
            for (int i = activePropInstances.Count - 1; i >= 0; i--)
            {
                GameObject prop = activePropInstances[i];
                if (prop == null) continue;

                Vector3 propPos = prop.transform.position;
                bool propOnSameSide = (propPos.x < 0f && planetPos.x < 0f) || (propPos.x > 0f && planetPos.x > 0f);
                if (propOnSameSide)
                {
                    float dz = Mathf.Abs(propPos.z - planetPos.z);
                    if (dz < safeClearanceDistance)
                    {
                        Destroy(prop);
                        activePropInstances.RemoveAt(i);
                    }
                }
            }
        }
    }

    private void SpawnRandomProp()
    {
        // Never spawn man-made satellites in the searing proximity of the Sun
        if (planetSequence != null && planetSequence.IsSunApproachingOrActive)
            return;

        LoadPrefabsIfNeeded();
        if (propPrefabs == null || propPrefabs.Length == 0) return;

        // Pick a prefab different from the previous one for visual variety
        int index = Random.Range(0, propPrefabs.Length);
        if (propPrefabs.Length > 1 && index == lastPrefabIndex)
        {
            index = (index + 1) % propPrefabs.Length;
        }
        lastPrefabIndex = index;

        GameObject prefab = propPrefabs[index];
        if (prefab == null) return;

        // Dynamic side placement: always spawn strictly on the OPPOSITE side of any active or upcoming planet
        bool spawnOnLeft;
        if (planetSequence != null && planetSequence.TryGetActivePlanet(out Transform activePlanet, out float planetRadius, out bool planetOnLeft))
        {
            spawnOnLeft = !planetOnLeft;
        }
        else if (planetSequence != null)
        {
            spawnOnLeft = !planetSequence.NextPlanetWillBeOnLeft;
        }
        else
        {
            spawnOnLeft = Random.value > 0.5f;
        }

        float lateralDist = Random.Range(lateralMinDistance, lateralMaxDistance);
        float x = spawnOnLeft ? -lateralDist : lateralDist;
        float y = Random.Range(altitudeMin, altitudeMax);
        float z = player.position.z + Random.Range(spawnDistanceAheadMin, spawnDistanceAheadMax);

        Vector3 spawnPos = new Vector3(x, y, z);
        Quaternion spawnRot = Random.rotation;

        GameObject instance = Instantiate(prefab, spawnPos, spawnRot, transform);
        instance.name = $"ScenicProp_{prefab.name}_{Time.frameCount}";

        // Subtle random scale variation
        float scaleMult = Random.Range(randomScaleMultiplier.x, randomScaleMultiplier.y);
        instance.transform.localScale = prefab.transform.localScale * scaleMult;

        // Attach & configure FloatingSpaceProp component
        FloatingSpaceProp floater = instance.GetComponent<FloatingSpaceProp>();
        if (floater == null)
        {
            floater = instance.AddComponent<FloatingSpaceProp>();
        }
        floater.Initialize(player, worldMovement);

        activePropInstances.Add(instance);
    }

    private void CleanDestroyedProps()
    {
        for (int i = activePropInstances.Count - 1; i >= 0; i--)
        {
            if (activePropInstances[i] == null)
            {
                activePropInstances.RemoveAt(i);
            }
        }
    }

    public void ClearAllProps()
    {
        for (int i = activePropInstances.Count - 1; i >= 0; i--)
        {
            if (activePropInstances[i] != null)
            {
                Destroy(activePropInstances[i]);
            }
        }
        activePropInstances.Clear();
    }
}

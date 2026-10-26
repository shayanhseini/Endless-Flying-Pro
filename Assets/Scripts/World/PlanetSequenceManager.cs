using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the Solar System planets in order while obstacles keep moving.
/// Each introduction is followed by a silent gameplay gap; completing Neptune
/// finishes the run.
/// </summary>
public class PlanetSequenceManager : MonoBehaviour
{
    private enum SequencePhase
    {
        Waiting,
        InitialDelay,
        Introduction,
        Gap,
        Complete
    }

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
        [TextArea(3, 8)] public string description;
        public GameObject prefab;
    }

    private static readonly string[] DefaultNames =
    {
        "Mercury", "Venus", "Earth", "Mars",
        "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    private static readonly string[] DefaultDescriptions =
    {
        "Mercury is the smallest and closest planet to the Sun.",
        "Venus is the hottest planet and is covered by thick clouds.",
        "Earth is our home and the only known planet with life.",
        "Mars is the red planet and has the largest volcano in the Solar System.",
        "Jupiter is the largest planet and has a giant storm called the Great Red Spot.",
        "Saturn is famous for its wide system of icy rings.",
        "Uranus rotates on its side and is an ice giant.",
        "Neptune is the farthest planet and has extremely powerful winds."
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

    [Header("Narration Sequence")]
    [Min(0f)]
    [Tooltip("Minimum delay after gameplay starts before the first planet introduction begins.")]
    [SerializeField] private float initialNarrationDelayMin = 3f;
    [Min(0f)]
    [Tooltip("Maximum delay after gameplay starts before the first planet introduction begins.")]
    [SerializeField] private float initialNarrationDelayMax = 5f;
    [Min(0.1f)]
    [Tooltip("Used when a planet does not have a narration clip yet.")]
    [SerializeField] private float fallbackIntroductionDuration = 5f;
    [Min(1f)]
    [Tooltip("Silent gameplay time between the end of one introduction and the next planet.")]
    [SerializeField] private float timeBetweenPlanets = 10f;
    [SerializeField] private AudioClip[] narrationClips = new AudioClip[8];
    [SerializeField] private AudioSource narrationSource;

    [Header("Travel")]
    [Min(1f)]
    [SerializeField] private float planetTravelDuration = 30f;

    [Header("Placement")]
    [Min(1f)]
    [SerializeField] private float spawnDistance = 2000f;

    [Header("Distant Appearance")]
    [Range(0f, 1f)]
    [Tooltip("Scale used when a planet first appears, relative to its prefab scale.")]
    [SerializeField] private float initialScaleMultiplier = 0.2f;
    [Range(0.05f, 1f)]
    [Tooltip("Portion of the journey used to smoothly reach the prefab scale.")]
    [SerializeField] private float scaleGrowthJourneyFraction = 0.25f;

    [Header("Horizontal Placement")]
    [Tooltip("Horizontal offset where a left-side planet first appears in the distance.")]
    [SerializeField] private float leftSideX = -200f;
    [Tooltip("Horizontal offset where a right-side planet first appears in the distance.")]
    [SerializeField] private float rightSideX = 200f;
    [Header("Vertical Placement")]
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
    private GameManager gameManager;
    private SequencePhase phase = SequencePhase.Waiting;
    private float phaseTimeRemaining;
    private int nextPlanetIndex;
    private bool initialized;
    private bool currentPlanetOnLeft;
    private Coroutine narrationFadeCoroutine;

    private static int sessionCheckpointPlanetIndex;

    public bool ObstaclesEnabled => true;

    public void Initialize(
        Transform playerTransform,
        WorldMovementManager movementManager,
        GameManager owningGameManager)
    {
        if (initialized)
            return;

        player = playerTransform;
        worldMovement = movementManager;
        gameManager = owningGameManager;
        EnsureDefinitions();
        EnsureNarrationSource();
        initialized = true;
    }

    private void Awake()
    {
        EnsureDefinitions();
        EnsureNarrationSource();
    }

    private void OnEnable()
    {
        GameEvents.GameStarted += BeginSequence;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= BeginSequence;
    }

    private void Update()
    {
        if (!initialized || worldMovement == null || player == null)
            return;

        if (!worldMovement.IsMovementEnabled)
            return;

        MoveActivePlanets();

        if (phase != SequencePhase.InitialDelay &&
            phase != SequencePhase.Introduction &&
            phase != SequencePhase.Gap)
            return;

        phaseTimeRemaining = Mathf.Max(0f, phaseTimeRemaining - Time.deltaTime);
        if (phaseTimeRemaining > 0f)
            return;

        if (phase == SequencePhase.InitialDelay)
            StartFirstIntroduction();
        else if (phase == SequencePhase.Introduction)
            BeginGap();
        else
            CompleteCurrentPlanet();
    }

    private void BeginSequence()
    {
        if (!initialized || phase != SequencePhase.Waiting)
            return;

        nextPlanetIndex = Mathf.Clamp(
            sessionCheckpointPlanetIndex,
            0,
            Mathf.Max(0, planets.Length - 1));
        phase = SequencePhase.InitialDelay;

        float minimumDelay = Mathf.Max(0f, initialNarrationDelayMin);
        float maximumDelay = Mathf.Max(minimumDelay, initialNarrationDelayMax);
        phaseTimeRemaining = minimumDelay;
    }

    private void StartFirstIntroduction()
    {
        SpawnNextPlanet();
        BeginIntroductionPhase();
    }

    private void BeginIntroductionPhase()
    {
        phase = SequencePhase.Introduction;
        AudioClip clip = GetNarrationClip(nextPlanetIndex);
        phaseTimeRemaining = clip != null
            ? Mathf.Max(0.1f, clip.length)
            : fallbackIntroductionDuration;

        if (clip != null)
        {
            narrationSource.clip = clip;
            narrationSource.Play();
        }

        GameEvents.RaisePlanetIntroductionStarted(
            planets[nextPlanetIndex].displayName,
            planets[nextPlanetIndex].description,
            phaseTimeRemaining,
            currentPlanetOnLeft);
    }

    public Transform PrepareCrashLanding()
    {
        sessionCheckpointPlanetIndex = Mathf.Clamp(
            nextPlanetIndex,
            0,
            Mathf.Max(0, planets.Length - 1));

        phase = SequencePhase.Complete;
        GameEvents.RaisePlanetIntroductionEnded();
        FadeOutNarration();

        if (activePlanets.Count == 0)
            SpawnNextPlanet();

        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            if (activePlanets[i].Transform != null)
                return activePlanets[i].Transform;
        }

        return null;
    }

    private void FadeOutNarration()
    {
        if (narrationSource == null || !narrationSource.isPlaying)
            return;

        if (narrationFadeCoroutine != null)
            StopCoroutine(narrationFadeCoroutine);

        narrationFadeCoroutine = StartCoroutine(FadeNarrationRoutine());
    }

    private System.Collections.IEnumerator FadeNarrationRoutine()
    {
        float startVolume = narrationSource.volume;
        float elapsed = 0f;

        while (elapsed < 0.75f)
        {
            elapsed += Time.deltaTime;
            narrationSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / 0.75f);
            yield return null;
        }

        narrationSource.Stop();
        narrationSource.volume = startVolume;
        narrationFadeCoroutine = null;
    }

    private void BeginGap()
    {
        if (narrationSource != null)
            narrationSource.Stop();

        phase = SequencePhase.Gap;
        phaseTimeRemaining = timeBetweenPlanets;
        GameEvents.RaisePlanetIntroductionEnded();
    }

    private void CompleteCurrentPlanet()
    {
        nextPlanetIndex++;
        if (nextPlanetIndex >= planets.Length)
        {
            phase = SequencePhase.Complete;
            GameEvents.RaisePlanetIntroductionEnded();
            FadeOutNarration();

            PlaneInteraction planeInteraction = player != null
                ? player.GetComponent<PlaneInteraction>()
                : null;

            if (planeInteraction != null)
                planeInteraction.BeginVictoryOrbit(GetLatestActivePlanet());
            else
                gameManager.WinGame();
            return;
        }

        SpawnNextPlanet();
        BeginIntroductionPhase();
    }

    private Transform GetLatestActivePlanet()
    {
        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            if (activePlanets[i].Transform != null)
                return activePlanets[i].Transform;
        }

        return null;
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
            float growthProgress = Mathf.Clamp01(
                distanceTravelled / Mathf.Max(1f, growthDistance));
            float easedProgress = Mathf.SmoothStep(0f, 1f, growthProgress);
            planet.localScale = Vector3.LerpUnclamped(
                activePlanet.InitialScale,
                activePlanet.TargetScale,
                easedProgress);

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

        currentPlanetOnLeft = nextPlanetIndex % 2 == 0;
        float spawnX = currentPlanetOnLeft
            ? -Mathf.Abs(leftSideX)
            : Mathf.Abs(rightSideX);
        float y = UnityEngine.Random.Range(verticalRange.x, verticalRange.y);
        instance.transform.position = player.position + new Vector3(spawnX, y, spawnDistance);
        Vector3 targetScale = instance.transform.localScale;
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
    }

    private AudioClip GetNarrationClip(int index)
    {
        if (narrationClips == null || index < 0 || index >= narrationClips.Length)
            return null;

        return narrationClips[index];
    }

    private void EnsureNarrationSource()
    {
        if (narrationSource != null)
            return;

        narrationSource = GetComponent<AudioSource>();
        if (narrationSource == null)
            narrationSource = gameObject.AddComponent<AudioSource>();

        narrationSource.playOnAwake = false;
        narrationSource.loop = false;
        narrationSource.spatialBlend = 0f;
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
                displayName = DefaultNames[i],
                description = DefaultDescriptions[i]
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

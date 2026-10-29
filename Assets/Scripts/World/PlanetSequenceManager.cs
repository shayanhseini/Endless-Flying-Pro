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
        OpeningNarration,
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
        public float MovementSpeed;
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
        "Neptune", "Uranus", "Saturn", "Jupiter",
        "Mars", "Earth", "Venus", "Mercury",
        "Sun"
    };

    private static readonly string[] DefaultDescriptions =
    {
        "نِپتون، دورترین سیاره از خورشید! رنگ آبی عمیقش به خاطر ترکیبات موجود در جوشه! بادهایی با سرعت باورنکردنی، بیش از دو هزار کیلومتر بر ساعت! یک سال نپتون، برابر با ۱۶۵ سال زمینی!",
        "اُورانوس، هفتمین سیاره از خورشید و یکی از سردترین سیاره‌های منظومه شمسی! رنگ آبی‌ـ‌سبزش به خاطر گاز متانه! نکته عجیبش؟ تقریباً به پهلو می‌چرخه، انگار روی مدارش غلت می‌زنه!",
        "زُحَل، ششمین سیاره از خورشید و معروف به حلقه‌های زیبای اطرافش! حلقه‌هاش از تکه‌های یخ و سنگ ساخته شدن! زحل یک غول گازیه و چگالیش حتی از آب هم کمتره!",
        "مُشتَری، بزرگ‌ترین سیاره منظومه شمسی! طوفانی عظیم به نام لکه سرخ بزرگ روی سطحش، که صدها ساله ادامه داره! بیش از ۹۰ قمر شناخته‌شده و حلقه‌هایی کم‌رنگ در اطرافش!",
        "مریخ به سیاره سرخ مشهور است و بزرگ‌ترین آتشفشان منظومه شمسی در آن قرار دارد.",
        "زمین، سومین سیاره از خورشیده و تنها سیاره‌ایه که می‌دونیم زندگی روی اون وجود داره! بیشتر سطح زمین رو آب پوشونده، برای همین بهش سیاره آبی می‌گن. زمین یک قمر به نام ماه داره و تقریباً هر ۳۶۵ روز، یک بار به دور خورشید می‌چرخه!",
        "زُهره، دومین سیاره از خورشیده و داغ‌ترین سیاره منظومه شمسیه! جوّی غلیظ و پر از گازهای سمی داره! اندازه‌ش تقریباً مثل زمینه، اما دماش اون‌قدر بالاست که حتی سرب هم ذوب می‌شه!",
        "عَطارُد، نزدیک‌ترین سیاره به خورشیده و کوچک‌ترین سیاره منظومه شمسیه! سطحش پر از دهانه‌های برخورده و شباهت زیادی به ماه داره. یک سال عَطارُد فقط ۸۸ روز زمینی طول می‌کشه!",
        "خورشید، ستاره سوزان و قلب تپنده منظومه شمسی! این غول آتشین عظیم، گرما و نور تمام سیارات را تأمین می‌کند. دمای مرکز آن بیش از ۱۵ میلیون درجه است و گدازه‌ها و فوران‌های خورشیدی پیوسته از سطحش به فضا زبانه می‌کشند!"
    };

    // Sidereal rotation speed relative to Earth. Negative values are retrograde.
    private static readonly float[] RealRotationSpeedMultipliers =
    {
        1.485f, -1.389f, 2.245f, 2.413f,
        0.972f, 1f, -0.0041f, 0.017f,
        0.04f
    };

    // Axial tilt in degrees. Retrograde direction is handled by rotation speed.
    private static readonly float[] AxialTilts =
    {
        28.32f, 82.23f, 26.73f, 3.13f,
        25.19f, 23.44f, 2.64f, 0.034f,
        7.25f
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
    [SerializeField] private AudioClip[] narrationClips = new AudioClip[9];
    [SerializeField] private AudioSource narrationSource;

    [Header("First Launch Narration")]
    [Tooltip("Played once per application run, before the first planet appears.")]
    [SerializeField] private AudioClip startNarrationClip;
    [Min(0f)]
    [SerializeField] private float startNarrationDelay = 2f;
    [Min(0f)]
    [SerializeField] private float firstPlanetDelayAfterStartNarration = 3f;

    [Header("Victory Sequence")]
    [Tooltip("Played after Neptune is completed. Assign the success narration here.")]
    [SerializeField] private AudioClip victoryNarrationClip;
    [Min(0f)]
    [SerializeField] private float victoryCountdownDelay = 2f;
    [Min(0.05f)]
    [SerializeField] private float obstacleExitDuration = 0.45f;
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
    [Min(1f)]
    [Tooltip("Absolute horizontal distance used by Jupiter.")]
    [SerializeField] private float jupiterSideX = 720f;
    [Min(1f)]
    [Tooltip("Extra side distance used by Saturn so it stays clear of the flight lane.")]
    [UnityEngine.Serialization.FormerlySerializedAs("jupiterSaturnSideMultiplier")]
    [SerializeField] private float saturnSideMultiplier = 1.5f;
    [Min(1f)]
    [Tooltip("Absolute horizontal distance used by the Sun so the spaceship flies safely past its massive corona.")]
    [SerializeField] private float sunSideX = 820f;

    [Header("Debug & Sequence Control")]
    [Tooltip("If set >= 0, sequence begins at this planet index (e.g. 0 for Neptune, 8 for Sun). Set to -1 for standard sequence starting from Neptune.")]
    [SerializeField] private int debugStartPlanetIndex = -1;
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
    private float currentPlanetCycleDuration;
    private float currentPlanetCycleElapsed;
    private int nextPlanetIndex;
    private bool initialized;
    private bool currentPlanetOnLeft;
    private Coroutine narrationFadeCoroutine;
    private Coroutine openingNarrationCoroutine;

    private static int sessionCheckpointPlanetIndex;
    private static bool openingNarrationPlayedThisSession;

    public bool ObstaclesEnabled =>
        phase == SequencePhase.Introduction || phase == SequencePhase.Gap;

    public float ObstacleExitDuration => obstacleExitDuration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetSessionState()
    {
        sessionCheckpointPlanetIndex = 0;
        openingNarrationPlayedThisSession = false;
    }

    private static void ResetOpeningNarrationState()
    {
        ResetSessionState();
    }

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

        if (phase == SequencePhase.Introduction || phase == SequencePhase.Gap)
        {
            currentPlanetCycleElapsed += Time.deltaTime;
            float progress = currentPlanetCycleDuration > 0f
                ? Mathf.Clamp01(currentPlanetCycleElapsed / currentPlanetCycleDuration)
                : 1f;
            GameEvents.RaisePlanetProgressChanged(nextPlanetIndex, progress);
        }

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

        int startIndex = debugStartPlanetIndex >= 0
            ? debugStartPlanetIndex
            : sessionCheckpointPlanetIndex;

        nextPlanetIndex = Mathf.Clamp(
            startIndex,
            0,
            Mathf.Max(0, planets.Length - 1));

        if (debugStartPlanetIndex >= 0)
        {
            openingNarrationPlayedThisSession = true;
        }

        if (!openingNarrationPlayedThisSession)
        {
            openingNarrationPlayedThisSession = true;
            phase = SequencePhase.OpeningNarration;
            openingNarrationCoroutine = StartCoroutine(
                FirstLaunchNarrationRoutine());
            return;
        }

        phase = SequencePhase.InitialDelay;

        float minimumDelay = Mathf.Max(0f, initialNarrationDelayMin);
        float maximumDelay = Mathf.Max(minimumDelay, initialNarrationDelayMax);
        phaseTimeRemaining = minimumDelay;
    }

    private System.Collections.IEnumerator FirstLaunchNarrationRoutine()
    {
        yield return WaitForPlayingSeconds(startNarrationDelay);

        if (narrationSource != null && startNarrationClip != null)
        {
            narrationSource.Stop();
            narrationSource.clip = startNarrationClip;
            narrationSource.Play();
            while (narrationSource.isPlaying)
                yield return null;
        }

        yield return WaitForPlayingSeconds(firstPlanetDelayAfterStartNarration);

        openingNarrationCoroutine = null;
        StartFirstIntroduction();
    }

    private System.Collections.IEnumerator WaitForPlayingSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (gameManager == null || gameManager.State == GameState.Playing)
                elapsed += Time.deltaTime;

            yield return null;
        }
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
        currentPlanetCycleDuration = phaseTimeRemaining + timeBetweenPlanets;
        currentPlanetCycleElapsed = 0f;
        GameEvents.RaisePlanetProgressChanged(nextPlanetIndex, 0f);

        if (clip != null)
        {
            narrationSource.clip = clip;
            narrationSource.Play();
        }

        string pName = !string.IsNullOrEmpty(planets[nextPlanetIndex].displayName)
            ? planets[nextPlanetIndex].displayName
            : (nextPlanetIndex < DefaultNames.Length ? DefaultNames[nextPlanetIndex] : "");
        string pDesc = !string.IsNullOrEmpty(planets[nextPlanetIndex].description)
            ? planets[nextPlanetIndex].description
            : (nextPlanetIndex < DefaultDescriptions.Length ? DefaultDescriptions[nextPlanetIndex] : "");

        GameEvents.RaisePlanetIntroductionStarted(
            pName,
            pDesc,
            phaseTimeRemaining,
            currentPlanetOnLeft,
            nextPlanetIndex);
    }

    public void PrepareCrashRecovery()
    {
        sessionCheckpointPlanetIndex = Mathf.Clamp(
            nextPlanetIndex,
            0,
            Mathf.Max(0, planets.Length - 1));

        GameEvents.RaisePlanetIntroductionEnded();
        FadeOutNarration();
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

            if (narrationFadeCoroutine != null)
            {
                StopCoroutine(narrationFadeCoroutine);
                narrationFadeCoroutine = null;
            }

            if (narrationSource != null)
            {
                narrationSource.Stop();
                if (victoryNarrationClip != null)
                {
                    narrationSource.clip = victoryNarrationClip;
                    narrationSource.Play();
                }
            }

            if (gameManager != null)
            {
                gameManager.StartVictorySequence();
            }
            return;
        }

        SpawnNextPlanet();
        BeginIntroductionPhase();
    }

    private System.Collections.IEnumerator VictorySequenceRoutine()
    {
        if (gameManager == null ||
            !gameManager.BeginVictorySequence(obstacleExitDuration))
            yield break;

        sessionCheckpointPlanetIndex = 0;

        if (narrationFadeCoroutine != null)
        {
            StopCoroutine(narrationFadeCoroutine);
            narrationFadeCoroutine = null;
        }

        if (narrationSource != null)
        {
            narrationSource.Stop();
            if (victoryNarrationClip != null)
            {
                narrationSource.clip = victoryNarrationClip;
                narrationSource.Play();
                while (narrationSource.isPlaying)
                    yield return null;
            }
        }

        if (victoryCountdownDelay > 0f)
            yield return new WaitForSeconds(victoryCountdownDelay);

        gameManager.StartRoundCountdown(true);
    }

    public void RestartFromFirstPlanet()
    {
        if (narrationFadeCoroutine != null)
        {
            StopCoroutine(narrationFadeCoroutine);
            narrationFadeCoroutine = null;
        }

        if (narrationSource != null)
        {
            narrationSource.Stop();
            narrationSource.volume = 1f;
        }

        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            if (activePlanets[i].Transform != null)
                Destroy(activePlanets[i].Transform.gameObject);
        }

        activePlanets.Clear();
        sessionCheckpointPlanetIndex = 0;
        nextPlanetIndex = debugStartPlanetIndex >= 0 ? debugStartPlanetIndex : 0;
        SpawnNextPlanet();
        BeginIntroductionPhase();
    }

    private void MoveActivePlanets()
    {
        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            ActivePlanet activePlanet = activePlanets[i];
            Transform planet = activePlanet.Transform;
            if (planet == null)
            {
                activePlanets.RemoveAt(i);
                continue;
            }

            planet.position += Vector3.back * activePlanet.MovementSpeed * Time.deltaTime;
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
        if (nextPlanetIndex == 2)
            spawnX *= saturnSideMultiplier;
        else if (nextPlanetIndex == 3)
            spawnX = currentPlanetOnLeft
                ? -Mathf.Abs(jupiterSideX)
                : Mathf.Abs(jupiterSideX);
        else if (nextPlanetIndex == 8)
            spawnX = currentPlanetOnLeft
                ? -Mathf.Abs(sunSideX)
                : Mathf.Abs(sunSideX);
        float y = UnityEngine.Random.Range(verticalRange.x, verticalRange.y);
        instance.transform.position = player.position + new Vector3(spawnX, y, spawnDistance);
        Vector3 targetScale = instance.transform.localScale;
        Vector3 initialScale = targetScale * initialScaleMultiplier;
        instance.transform.localScale = initialScale;
        AudioClip narrationClip = GetNarrationClip(nextPlanetIndex);
        float introductionDuration = narrationClip != null
            ? Mathf.Max(0.1f, narrationClip.length)
            : fallbackIntroductionDuration;
        float completePassDuration = Mathf.Max(
            0.1f,
            introductionDuration + timeBetweenPlanets - 0.25f);
        float movementSpeed =
            (spawnDistance + despawnDistanceBehindPlayer) / completePassDuration;

        activePlanets.Add(new ActivePlanet
        {
            Transform = instance.transform,
            InitialScale = initialScale,
            TargetScale = targetScale,
            RotationAxis = Quaternion.AngleAxis(
                AxialTilts[nextPlanetIndex],
                Vector3.forward) * Vector3.up,
            RotationSpeed = GetVisibleRotationSpeed(nextPlanetIndex),
            MovementSpeed = movementSpeed
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
        if (narrationSource == null)
            narrationSource = GetComponent<AudioSource>();
        if (narrationSource == null)
            narrationSource = gameObject.AddComponent<AudioSource>();

        narrationSource.playOnAwake = false;
        narrationSource.loop = false;
        narrationSource.spatialBlend = 0f;
        narrationSource.priority = 0;
        narrationSource.volume = 1f;
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
        {
            collider.enabled = false;
            Destroy(collider);
        }
    }

}

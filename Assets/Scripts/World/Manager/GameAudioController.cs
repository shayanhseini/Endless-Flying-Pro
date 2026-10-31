using System.Collections;
using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    [Header("Shield")]
    [SerializeField] private AudioSource shieldSource;
    [SerializeField] private AudioClip shieldPickupClip;
    [SerializeField] private AudioClip shieldDeActiveClip;
    
    [Header("Speed Boost SFX")]
    [SerializeField] private AudioSource speedBoostSource;
    [SerializeField] private AudioClip speedBoostActiveClip;
    [SerializeField] private AudioClip speedBoostDeActiveClip;

    [Header("Speed Boost Engine Roar")]
    [SerializeField] private AudioSource boostEngineSource;
    [SerializeField] private AudioClip boostEngineClip;
    [SerializeField, Range(0f, 1f)] private float boostEngineTargetVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float duckedBoostEngineVolume = 0.12f;
    [SerializeField] private float boostEngineFadeInDuration = 0.35f;
    [SerializeField] private float boostEngineFadeOutDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float engineVolumeDuringBoost = 0.08f;
    [SerializeField, Range(0f, 1f)] private float windVolumeDuringBoost = 0.06f;

    [Header("UI")]
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioClip uiButtonClip;
    
    [Header("Explosion")]
    [SerializeField] private AudioSource expSound;
    [SerializeField] private AudioClip[] explosionClips;
    
    [Header("Launch Missile")]
    [SerializeField] private AudioSource dangerSource;

    [Header("Narration Ducking & Ambient Sounds")]
    [SerializeField] private AudioSource narrationSource;
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioSource windSource;
    [SerializeField, Range(0f, 1f)] private float normalEngineVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float duckedEngineVolume = 0.10f;
    [SerializeField, Range(0f, 1f)] private float normalWindVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float duckedWindVolume = 0.06f;
    [SerializeField] private float duckFadeDuration = 0.45f;

    private Coroutine ambientDuckCoroutine;
    private Coroutine boostFadeCoroutine;
    private bool isNarrationDucked;
    private bool isBoostActive;
    private bool wasNarrationSourcePlaying;

    private void Awake()
    {
        EnsureBoostEngineSource();
        EnsureNarrationSource();
    }

    private void Start()
    {
        FindAmbientSources();
    }

    private void Update()
    {
        EnsureNarrationSource();
        if (narrationSource != null)
        {
            bool isPlaying = narrationSource.isPlaying;
            if (isPlaying != wasNarrationSourcePlaying)
            {
                wasNarrationSourcePlaying = isPlaying;
                UpdateAudioMixing();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (boostEngineClip == null)
        {
            boostEngineClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/SFX/Boost_Engine-Sound.wav");
        }
    }
#endif

    private void OnEnable()
    {
        GameEvents.ShieldActivated += PlayShieldSound;
        GameEvents.ShieldDeactivated += PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated += HandleSpeedBoostActivated;
        GameEvents.SpeedBoostDeactivated += HandleSpeedBoostDeactivated;
        GameEvents.LaunchMissile += PlayDangerSound;
        GameEvents.CrashLandingStarted += HandleCrashLandingStarted;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.GameWon += HandleGameWon;
        GameEvents.AtmosphereReached += StopDangerSound;
        GameEvents.RoundCountdownCompleted += StopDangerSound;
        GameEvents.PlanetIntroductionStarted += HandleNarrationStarted;
        GameEvents.PlanetIntroductionEnded += HandleNarrationEnded;
    }

    private void OnDisable()
    {
        GameEvents.ShieldActivated -= PlayShieldSound;
        GameEvents.ShieldDeactivated -= PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated -= HandleSpeedBoostActivated;
        GameEvents.SpeedBoostDeactivated -= HandleSpeedBoostDeactivated;
        GameEvents.LaunchMissile -= PlayDangerSound;
        GameEvents.CrashLandingStarted -= HandleCrashLandingStarted;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.GameWon -= HandleGameWon;
        GameEvents.AtmosphereReached -= StopDangerSound;
        GameEvents.RoundCountdownCompleted -= StopDangerSound;
        GameEvents.PlanetIntroductionStarted -= HandleNarrationStarted;
        GameEvents.PlanetIntroductionEnded -= HandleNarrationEnded;

        StopBoostEngineSound(immediate: true);
    }

    private void HandleSpeedBoostActivated()
    {
        PlaySpeedBoostActivateSound();
        StartBoostEngineSound();
    }

    private void HandleSpeedBoostDeactivated()
    {
        PlaySpeedBoostDeActiveSound();
        StopBoostEngineSound(immediate: false);
    }

    private void HandleCrashLandingStarted()
    {
        StopDangerSound();
        HandleNarrationEnded();
        StopBoostEngineSound(immediate: true);
    }

    private void HandleGameOver()
    {
        StopDangerSound();
        StopBoostEngineSound(immediate: true);
    }

    private void HandleGameWon()
    {
        StopDangerSound();
        StopBoostEngineSound(immediate: false);
    }

    private void HandleNarrationStarted(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft,
        int planetIndex)
    {
        PlayPlanetReachedSound(planetName, description, duration, planetOnLeft, planetIndex);
        DuckAmbientSounds(true);
    }

    private void HandleNarrationEnded()
    {
        DuckAmbientSounds(false);
    }

    private void FindAmbientSources()
    {
        if (engineSource != null && windSource != null)
            return;

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource s in sources)
        {
            if (engineSource == null && s.gameObject.name == "Engine Sound")
                engineSource = s;
            else if (windSource == null && s.gameObject.name == "Wind Sound")
                windSource = s;
        }

        if (engineSource != null && !isNarrationDucked && !isBoostActive)
            engineSource.volume = normalEngineVolume;
        if (windSource != null && !isNarrationDucked && !isBoostActive)
            windSource.volume = normalWindVolume;
    }

    private void EnsureNarrationSource()
    {
        if (narrationSource != null)
            return;

        Transform child = transform.Find("Naration AudioSource");
        if (child != null && child.TryGetComponent<AudioSource>(out var src))
        {
            narrationSource = src;
            return;
        }

        AudioSource[] sources = transform.parent != null
            ? transform.parent.GetComponentsInChildren<AudioSource>(true)
            : FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioSource source in sources)
        {
            if (source.gameObject.name == "Naration AudioSource")
            {
                narrationSource = source;
                return;
            }
        }
    }

    public bool IsNarrationActive()
    {
        if (isNarrationDucked)
            return true;
        if (narrationSource != null && narrationSource.isPlaying)
            return true;
        return false;
    }

    public void DuckAmbientSounds(bool duck)
    {
        isNarrationDucked = duck;
        UpdateAudioMixing();
    }

    private void UpdateAudioMixing(float customFadeDuration = -1f)
    {
        FindAmbientSources();
        EnsureNarrationSource();

        bool narrationActive = IsNarrationActive();
        float duration = customFadeDuration >= 0f ? customFadeDuration : duckFadeDuration;

        // 1. Ambient Sounds (Engine & Wind)
        float targetEngine;
        float targetWind;

        if (narrationActive)
        {
            targetEngine = duckedEngineVolume;
            targetWind = duckedWindVolume;
        }
        else if (isBoostActive)
        {
            targetEngine = engineVolumeDuringBoost;
            targetWind = windVolumeDuringBoost;
        }
        else
        {
            targetEngine = normalEngineVolume;
            targetWind = normalWindVolume;
        }

        if (ambientDuckCoroutine != null)
            StopCoroutine(ambientDuckCoroutine);

        ambientDuckCoroutine = StartCoroutine(DuckAmbientRoutine(targetEngine, targetWind, duration));

        // 2. Boost Engine Sound (ducks when narration is active)
        if (isBoostActive && boostEngineSource != null && boostEngineSource.isPlaying)
        {
            float targetBoost = narrationActive ? duckedBoostEngineVolume : boostEngineTargetVolume;
            SetBoostEngineTargetVolume(targetBoost, duration, stopOnComplete: false);
        }
    }

    private IEnumerator DuckAmbientRoutine(float targetEngine, float targetWind, float duration)
    {
        float startEngine = engineSource != null ? engineSource.volume : targetEngine;
        float startWind = windSource != null ? windSource.volume : targetWind;

        if (duration <= 0f)
        {
            if (engineSource != null) engineSource.volume = targetEngine;
            if (windSource != null) windSource.volume = targetWind;
            ambientDuckCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (engineSource != null)
                engineSource.volume = Mathf.Lerp(startEngine, targetEngine, t);
            if (windSource != null)
                windSource.volume = Mathf.Lerp(startWind, targetWind, t);
            yield return null;
        }

        if (engineSource != null)
            engineSource.volume = targetEngine;
        if (windSource != null)
            windSource.volume = targetWind;

        ambientDuckCoroutine = null;
    }

    public void StartBoostEngineSound()
    {
        EnsureBoostEngineSource();
        if (boostEngineSource == null)
            return;

        if (boostEngineClip != null && boostEngineSource.clip != boostEngineClip)
        {
            boostEngineSource.clip = boostEngineClip;
        }

        if (boostEngineSource.clip == null)
        {
#if UNITY_EDITOR
            boostEngineClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/SFX/Boost_Engine-Sound.wav");
            boostEngineSource.clip = boostEngineClip;
#endif
            if (boostEngineSource.clip == null)
                return;
        }

        boostEngineSource.loop = true;

        if (!boostEngineSource.isPlaying)
        {
            boostEngineSource.volume = 0f;
            boostEngineSource.Play();
        }

        isBoostActive = true;

        float targetBoost = IsNarrationActive() ? duckedBoostEngineVolume : boostEngineTargetVolume;
        SetBoostEngineTargetVolume(targetBoost, boostEngineFadeInDuration, stopOnComplete: false);

        UpdateAudioMixing(boostEngineFadeInDuration);
    }

    public void StopBoostEngineSound(bool immediate = false)
    {
        isBoostActive = false;

        if (boostFadeCoroutine != null)
        {
            StopCoroutine(boostFadeCoroutine);
            boostFadeCoroutine = null;
        }

        if (boostEngineSource != null)
        {
            if (immediate)
            {
                boostEngineSource.Stop();
                boostEngineSource.volume = 0f;
            }
            else if (boostEngineSource.isPlaying)
            {
                SetBoostEngineTargetVolume(0f, boostEngineFadeOutDuration, stopOnComplete: true);
            }
        }

        UpdateAudioMixing(boostEngineFadeOutDuration);
    }

    private void SetBoostEngineTargetVolume(float targetVolume, float duration, bool stopOnComplete = false)
    {
        if (boostEngineSource == null)
            return;

        if (boostFadeCoroutine != null)
            StopCoroutine(boostFadeCoroutine);

        boostFadeCoroutine = StartCoroutine(FadeBoostEngineRoutine(targetVolume, duration, stopOnComplete));
    }

    private IEnumerator FadeBoostEngineRoutine(float targetVolume, float duration, bool stopOnComplete)
    {
        if (boostEngineSource == null) yield break;

        float startVolume = boostEngineSource.volume;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            boostEngineSource.volume = targetVolume;
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                boostEngineSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }
            boostEngineSource.volume = targetVolume;
        }

        if (stopOnComplete && targetVolume <= 0.001f)
        {
            boostEngineSource.Stop();
        }

        boostFadeCoroutine = null;
    }

    private void EnsureBoostEngineSource()
    {
        if (boostEngineSource != null)
            return;

        Transform child = transform.Find("Boost Engine");
        if (child != null && child.TryGetComponent<AudioSource>(out var existing))
        {
            boostEngineSource = existing;
            ConfigureBoostEngineSource();
            return;
        }

        if (transform.parent != null)
        {
            Transform sibling = transform.parent.Find("Boost Engine");
            if (sibling != null && sibling.TryGetComponent<AudioSource>(out var existingSibling))
            {
                boostEngineSource = existingSibling;
                ConfigureBoostEngineSource();
                return;
            }
        }

        GameObject go = new GameObject("Boost Engine");
        go.transform.SetParent(transform, false);
        boostEngineSource = go.AddComponent<AudioSource>();
        ConfigureBoostEngineSource();
    }

    private void ConfigureBoostEngineSource()
    {
        if (boostEngineSource == null) return;
        boostEngineSource.playOnAwake = false;
        boostEngineSource.loop = true;
        boostEngineSource.spatialBlend = 0f;
        boostEngineSource.volume = 0f;
        if (boostEngineClip != null)
            boostEngineSource.clip = boostEngineClip;
    }

    private void PlayShieldSound()
    {
        if (shieldSource != null && shieldPickupClip != null)
            shieldSource.PlayOneShot(shieldPickupClip);
    }

    private void PlayShieldDeActiveSound()
    {
        if (shieldSource != null && shieldDeActiveClip != null)
            shieldSource.PlayOneShot(shieldDeActiveClip);
    }

    private void PlaySpeedBoostActivateSound()
    {
        if (speedBoostSource != null && speedBoostActiveClip != null)
        {
            speedBoostSource.volume = IsNarrationActive() ? 0.35f : 0.65f;
            speedBoostSource.PlayOneShot(speedBoostActiveClip);
        }
    }

    private void PlaySpeedBoostDeActiveSound()
    {
        if (speedBoostSource != null && speedBoostDeActiveClip != null)
        {
            speedBoostSource.volume = IsNarrationActive() ? 0.35f : 0.65f;
            speedBoostSource.PlayOneShot(speedBoostDeActiveClip);
        }
    }
    
    public void PlayButtonSound()
    {
        if (uiSource != null && uiButtonClip != null)
            uiSource.PlayOneShot(uiButtonClip);
    }

    private void PlayPlanetReachedSound(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft,
        int planetIndex)
    {
        PlayButtonSound();
    }
    
    public void PlayExplosionSound(Transform target)
    {
        if (expSound == null || explosionClips == null || explosionClips.Length == 0)
            return;
        
        expSound.transform.position = target.position;
        int index = Random.Range(0, explosionClips.Length);
        if (explosionClips[index] != null)
            expSound.PlayOneShot(explosionClips[index]);
    }

    public void PlayDangerSound()
    {
        EnsureDangerSource();
        if (dangerSource == null)
            return;

        dangerSource.gameObject.SetActive(true);
        dangerSource.Play();
    }

    private void StopDangerSound()
    {
        if (dangerSource != null)
        {
            dangerSource.Stop();
            dangerSource.gameObject.SetActive(false);
        }
    }

    private void EnsureDangerSource()
    {
        if (dangerSource != null)
            return;

        AudioSource[] sources = transform.parent != null
            ? transform.parent.GetComponentsInChildren<AudioSource>(true)
            : FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioSource source in sources)
        {
            if (source.gameObject.name == "Danger")
            {
                dangerSource = source;
                return;
            }
        }
    }
}

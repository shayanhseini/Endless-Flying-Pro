using System.Collections;
using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    [Header("Shield")]
    [SerializeField] private AudioSource shieldSource;
    [SerializeField] private AudioClip shieldPickupClip;
    [SerializeField] private AudioClip shieldDeActiveClip;
    
    [Header("Speed Boost")]
    [SerializeField] private AudioSource speedBoostSource;
    [SerializeField] private AudioClip speedBoostActiveClip;
    [SerializeField] private AudioClip speedBoostDeActiveClip;
    
    [Header("UI")]
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioClip uiButtonClip;
    
    [Header("Explosion")]
    [SerializeField] private AudioSource expSound;
    [SerializeField] private AudioClip[] explosionClips;
    
    [Header("Launch Missile")]
    [SerializeField] private AudioSource dangerSource;

    [Header("Narration Ducking & Ambient Sounds")]
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioSource windSource;
    [SerializeField, Range(0f, 1f)] private float normalEngineVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float duckedEngineVolume = 0.10f;
    [SerializeField, Range(0f, 1f)] private float normalWindVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float duckedWindVolume = 0.06f;
    [SerializeField] private float duckFadeDuration = 0.45f;

    private Coroutine ambientDuckCoroutine;
    private bool isAmbientDucked;

    private void Start()
    {
        FindAmbientSources();
    }

    private void OnEnable()
    {
        GameEvents.ShieldActivated += PlayShieldSound;
        GameEvents.ShieldDeactivated += PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated += PlaySpeedBoostActivateSound;
        GameEvents.SpeedBoostDeactivated += PlaySpeedBoostDeActiveSound;
        GameEvents.LaunchMissile += PlayDangerSound;
        GameEvents.CrashLandingStarted += StopDangerSound;
        GameEvents.GameOver += StopDangerSound;
        GameEvents.AtmosphereReached += StopDangerSound;
        GameEvents.RoundCountdownCompleted += StopDangerSound;
        GameEvents.PlanetIntroductionStarted += HandleNarrationStarted;
        GameEvents.PlanetIntroductionEnded += HandleNarrationEnded;
        GameEvents.CrashLandingStarted += HandleNarrationEnded;
    }

    private void OnDisable()
    {
        GameEvents.ShieldActivated -= PlayShieldSound;
        GameEvents.ShieldDeactivated -= PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated -= PlaySpeedBoostActivateSound;
        GameEvents.SpeedBoostDeactivated -= PlaySpeedBoostDeActiveSound;
        GameEvents.LaunchMissile -= PlayDangerSound;
        GameEvents.CrashLandingStarted -= StopDangerSound;
        GameEvents.GameOver -= StopDangerSound;
        GameEvents.AtmosphereReached -= StopDangerSound;
        GameEvents.RoundCountdownCompleted -= StopDangerSound;
        GameEvents.PlanetIntroductionStarted -= HandleNarrationStarted;
        GameEvents.PlanetIntroductionEnded -= HandleNarrationEnded;
        GameEvents.CrashLandingStarted -= HandleNarrationEnded;
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

        if (engineSource != null && !isAmbientDucked)
            engineSource.volume = normalEngineVolume;
        if (windSource != null && !isAmbientDucked)
            windSource.volume = normalWindVolume;
    }

    public void DuckAmbientSounds(bool duck)
    {
        FindAmbientSources();
        if (isAmbientDucked == duck)
            return;

        isAmbientDucked = duck;
        if (ambientDuckCoroutine != null)
            StopCoroutine(ambientDuckCoroutine);

        ambientDuckCoroutine = StartCoroutine(DuckAmbientRoutine(duck));
    }

    private IEnumerator DuckAmbientRoutine(bool duck)
    {
        float targetEngine = duck ? duckedEngineVolume : normalEngineVolume;
        float targetWind = duck ? duckedWindVolume : normalWindVolume;

        float startEngine = engineSource != null ? engineSource.volume : targetEngine;
        float startWind = windSource != null ? windSource.volume : targetWind;

        float elapsed = 0f;
        while (elapsed < duckFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duckFadeDuration));
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
            speedBoostSource.PlayOneShot(speedBoostActiveClip);
    }

    private void PlaySpeedBoostDeActiveSound()
    {
        if (speedBoostSource != null && speedBoostDeActiveClip != null)
            speedBoostSource.PlayOneShot(speedBoostDeActiveClip);
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

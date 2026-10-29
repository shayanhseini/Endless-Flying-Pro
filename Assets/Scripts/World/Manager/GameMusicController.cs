using System.Collections;
using UnityEngine;

public class GameMusicController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource waitingSource;
    [SerializeField] private AudioSource gameSource;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float waitingVolume = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float gameVolume = 0.3f;

    [Header("Narration Ducking")]
    [SerializeField, Range(0f, 1f)]
    private float duckedGameVolume = 0.08f;
    [SerializeField] private float duckFadeDuration = 0.5f;

    [Header("Crossfade")]
    [SerializeField] private float fadeDuration = 2f;

    private Coroutine crossfadeCoroutine;
    private Coroutine duckCoroutine;
    private bool isDucked;

    private void Awake()
    {
        waitingSource.volume = waitingVolume;
        waitingSource.loop = true;
        waitingSource.Play();

        gameSource.volume = 0f;
        gameSource.loop = true;
        gameSource.Play();
    }

    private void OnEnable()
    {
        GameEvents.GameStarted += SwitchToGameMusic;
        GameEvents.GameOver += SwitchToWaitingMusic;
        GameEvents.PlanetIntroductionStarted += HandleIntroductionStarted;
        GameEvents.PlanetIntroductionEnded += HandleIntroductionEnded;
        GameEvents.CrashLandingStarted += HandleIntroductionEnded;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= SwitchToGameMusic;
        GameEvents.GameOver -= SwitchToWaitingMusic;
        GameEvents.PlanetIntroductionStarted -= HandleIntroductionStarted;
        GameEvents.PlanetIntroductionEnded -= HandleIntroductionEnded;
        GameEvents.CrashLandingStarted -= HandleIntroductionEnded;
    }

    private void HandleIntroductionStarted(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft,
        int planetIndex)
    {
        DuckMusic(true);
    }

    private void HandleIntroductionEnded()
    {
        DuckMusic(false);
    }

    public void DuckMusic(bool duck)
    {
        if (isDucked == duck)
            return;

        isDucked = duck;
        if (crossfadeCoroutine != null)
            return;

        if (duckCoroutine != null)
            StopCoroutine(duckCoroutine);

        float target = duck ? duckedGameVolume : gameVolume;
        duckCoroutine = StartCoroutine(FadeVolumeRoutine(gameSource, target, duckFadeDuration));
    }

    private IEnumerator FadeVolumeRoutine(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
            yield break;

        float start = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            source.volume = Mathf.Lerp(start, targetVolume, t);
            yield return null;
        }

        source.volume = targetVolume;
        duckCoroutine = null;
    }

    private void SwitchToGameMusic()
    {
        isDucked = false;
        Crossfade(
            waitingSource,
            gameSource,
            waitingVolume,
            gameVolume
        );
    }

    private void SwitchToWaitingMusic()
    {
        isDucked = false;
        Crossfade(
            gameSource,
            waitingSource,
            gameVolume,
            waitingVolume
        );
    }

    private void Crossfade(
        AudioSource from,
        AudioSource to,
        float fromVolume,
        float toVolume)
    {
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine =
            StartCoroutine(
                CrossfadeCoroutine(
                    from,
                    to,
                    fromVolume,
                    toVolume
                )
            );
    }

    private IEnumerator CrossfadeCoroutine(
        AudioSource from,
        AudioSource to,
        float fromVolume,
        float toVolume)
    {
        if (!to.isPlaying)
            to.Play();

        float startFromVolume = from.volume;
        float startToVolume = to.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            from.volume = Mathf.Lerp(startFromVolume, 0f, t);
            to.volume = Mathf.Lerp(startToVolume, toVolume, t);

            yield return null;
        }

        from.volume = 0f;
        to.volume = toVolume;
        from.Stop();
        crossfadeCoroutine = null;
    }
}

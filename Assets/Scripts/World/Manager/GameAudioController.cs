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
        GameEvents.PlanetIntroductionStarted += PlayPlanetReachedSound;
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
        GameEvents.PlanetIntroductionStarted -= PlayPlanetReachedSound;
    }

    private void PlayShieldSound()
    {
        shieldSource.PlayOneShot(shieldPickupClip);
    }

    private void PlayShieldDeActiveSound()
    {
        shieldSource.PlayOneShot(shieldDeActiveClip);
    }

    private void PlaySpeedBoostActivateSound()
    {
        speedBoostSource.PlayOneShot(speedBoostActiveClip);
    }

    private void PlaySpeedBoostDeActiveSound()
    {
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
        if (expSound == null)
            return;
        
        expSound.transform.position =
            target.position;

        int index = Random.Range(0, explosionClips.Length);
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

using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    [Header("Coin")]
    [SerializeField] private AudioSource coinSource;
    [SerializeField] private AudioClip coinClip;
    
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


    private void Awake()
    {
        PreloadAllClips();
    }

    private void PreloadAllClips()
    {
        if (explosionClips != null)
        {
            foreach (var clip in explosionClips)
            {
                if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
                    clip.LoadAudioData();
            }
        }

        if (coinClip != null && coinClip.loadState != AudioDataLoadState.Loaded) coinClip.LoadAudioData();
        if (shieldPickupClip != null && shieldPickupClip.loadState != AudioDataLoadState.Loaded) shieldPickupClip.LoadAudioData();
        if (shieldDeActiveClip != null && shieldDeActiveClip.loadState != AudioDataLoadState.Loaded) shieldDeActiveClip.LoadAudioData();
        if (speedBoostActiveClip != null && speedBoostActiveClip.loadState != AudioDataLoadState.Loaded) speedBoostActiveClip.LoadAudioData();
        if (speedBoostDeActiveClip != null && speedBoostDeActiveClip.loadState != AudioDataLoadState.Loaded) speedBoostDeActiveClip.LoadAudioData();
        if (uiButtonClip != null && uiButtonClip.loadState != AudioDataLoadState.Loaded) uiButtonClip.LoadAudioData();
    }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.ShieldActivated += PlayShieldSound;
        GameEvents.ShieldDeactivated += PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated += PlaySpeedBoostActivateSound;
        GameEvents.SpeedBoostDeactivated += PlaySpeedBoostDeActiveSound;
        GameEvents.LaunchMissile += PlayDangerSound;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.ShieldActivated -= PlayShieldSound;
        GameEvents.ShieldDeactivated -= PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated -= PlaySpeedBoostActivateSound;
        GameEvents.SpeedBoostDeactivated -= PlaySpeedBoostDeActiveSound;
        GameEvents.LaunchMissile -= PlayDangerSound;
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
    
    private void OnScoreChanged(int score)
    {
        coinSource.PlayOneShot(coinClip);
    }
    
    public void PlayButtonSound()
    {
        uiSource.PlayOneShot(uiButtonClip);
    }
    
    public void PlayExplosionSound(Transform target)
    {
        if (target == null)
            return;

        PlayExplosionSound(target.position);
    }

    public void PlayExplosionSound(Vector3 position)
    {
        if (expSound == null ||
            explosionClips == null ||
            explosionClips.Length == 0)
            return;

        expSound.transform.position =
            position;

        int index = Random.Range(0, explosionClips.Length);
        AudioClip clip = explosionClips[index];
        if (clip != null)
        {
            if (clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();

            expSound.PlayOneShot(clip);
        }
    }

    public void PlayDangerSound()
    {
        dangerSource.Play();
    }
}

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
        expSound.PlayOneShot(explosionClips[index]);
    }

    public void PlayDangerSound()
    {
        dangerSource.Play();
    }
}

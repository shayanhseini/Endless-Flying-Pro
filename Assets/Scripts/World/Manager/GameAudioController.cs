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


    private void OnEnable()
    {
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.ShieldActivated += PlayShieldSound;
        GameEvents.ShieldDeactivated += PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated += PlaySpeedBoostActivateSound;
        GameEvents.SpeedBoostDeactivated += PlaySpeedBoostDeActiveSound;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.ShieldActivated -= PlayShieldSound;
        GameEvents.ShieldDeactivated -= PlayShieldDeActiveSound;
        GameEvents.SpeedBoostActivated -= PlaySpeedBoostActivateSound;
        GameEvents.SpeedBoostDeactivated -= PlaySpeedBoostDeActiveSound;
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
}
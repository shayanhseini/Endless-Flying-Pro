using System;
using UnityEngine;

public class PlaneInteraction : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private ShieldController shield;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameAudioController gameAudioController;

    [Header("Hit Effect")] [SerializeField]
    private GameObject hitEffect;

    [Header("Invulnerability Grace Period")]
    [Tooltip("Immunity window after shield is consumed so multi-collider triggers or simultaneous missiles do not kill the player.")]
    [SerializeField] private float shieldBreakGracePeriod = 0.8f;
    private float invulnerableUntilTime = -1f;

    public bool IsInvulnerable => Time.time < invulnerableUntilTime;

    
    public void HitObstacle(bool win = false)
    {
        if (win)
        {
            GameEvents.RaisePlaneHit();
            PlayHitEffect();
            if (gameAudioController != null)
                gameAudioController.PlayExplosionSound(this.transform);
            gameManager.Victory();
            return;
        }

        // If plane is in post-shield break grace period, ignore subsequent hits
        if (IsInvulnerable)
            return;

        if (shield != null &&
            shield.IsActive)
        {
            // Grant grace period immediately so concurrent collider triggers or simultaneous hits do not kill
            invulnerableUntilTime = Time.time + shieldBreakGracePeriod;

            GameEvents.RaisePlaneHit();
            PlayHitEffect();
            if (gameAudioController != null)
                gameAudioController.PlayExplosionSound(this.transform);

            shield.Consume();
            return;
        }

        GameEvents.RaisePlaneHit();
        PlayHitEffect();
        if (gameAudioController != null)
            gameAudioController.PlayExplosionSound(this.transform);

        gameManager.GameOver();
    }


    private void PlayHitEffect()
    {
        if (hitEffect == null)
            return;


        hitEffect.SetActive(false);


        hitEffect.transform.position =
            transform.position;


        hitEffect.SetActive(true);
    }

    private void DisableShield()
    {
        if (shield == null)
            return;

        shield.ForceDeactivate();
    }
}
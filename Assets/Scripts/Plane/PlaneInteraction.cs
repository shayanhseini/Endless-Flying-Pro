using System.Collections;
using UnityEngine;

public class PlaneInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShieldController shield;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameAudioController gameAudioController;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffect;

    [Header("Recovery")]
    [Min(0f)]
    [SerializeField] private float colliderEnableDelay = 0.5f;

    private bool recoveryActive;

    private void OnEnable()
    {
        GameEvents.RoundCountdownCompleted += HandleCountdownCompleted;
    }

    private void OnDisable()
    {
        GameEvents.RoundCountdownCompleted -= HandleCountdownCompleted;
    }

    public void HitObstacle()
    {
        if (recoveryActive || gameManager == null ||
            gameManager.State != GameState.Playing)
            return;

        GameEvents.RaisePlaneHit();
        PlayHitEffect();

        if (gameAudioController != null)
            gameAudioController.PlayExplosionSound(transform);

        if (shield != null && shield.IsActive)
        {
            shield.Consume();
            return;
        }

        BeginRecovery();
    }

    private void PlayHitEffect()
    {
        if (hitEffect == null)
            return;

        hitEffect.SetActive(false);
        hitEffect.transform.position = transform.position;
        hitEffect.SetActive(true);
    }

    private void BeginRecovery()
    {
        if (gameManager == null || !gameManager.BeginCrashLanding())
            return;

        recoveryActive = true;
        SetPlaneCollidersEnabled(false);

        PlanetSequenceManager sequence = FindFirstObjectByType<PlanetSequenceManager>();
        if (sequence != null)
            sequence.PrepareCrashRecovery();

        gameManager.StartRoundCountdown(false);
    }

    private void HandleCountdownCompleted()
    {
        if (!recoveryActive)
            return;

        recoveryActive = false;
        StartCoroutine(EnableCollidersAfterDelay());
    }

    private IEnumerator EnableCollidersAfterDelay()
    {
        if (colliderEnableDelay > 0f)
            yield return new WaitForSeconds(colliderEnableDelay);

        SetPlaneCollidersEnabled(true);
    }

    private void SetPlaneCollidersEnabled(bool enabled)
    {
        Collider[] planeColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider planeCollider in planeColliders)
            planeCollider.enabled = enabled;
    }
}

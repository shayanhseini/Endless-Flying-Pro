using System.Collections;
using UnityEngine;

public class SpeedPowerUpController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldMovementManager worldMovement;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlaneCameraController planeCameraController;


    [Header("Settings")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private int bonusScore = 50;


    [Header("Speed Transition")]
    [SerializeField] private float speedTransitionDuration = 0.25f;


    [Header("Speed Particle")]
    [SerializeField] private ParticleSystem speedParticle;
    [SerializeField] private float particleMultiplier = 2f;


    private float initialParticleRate;

    private Coroutine powerUpCoroutine;


    public bool IsActive { get; private set; }


    private void Awake()
    {
        if (speedParticle != null)
        {
            var emission = speedParticle.emission;

            initialParticleRate =
                emission.rateOverTime.constant;
        }
    }


    public void Activate()
    {
        if (powerUpCoroutine != null)
            StopCoroutine(powerUpCoroutine);


        powerUpCoroutine =
            StartCoroutine(
                PowerUpRoutine()
            );
        GameEvents.RaiseSpeedBoostActivated();
    }


    private IEnumerator PowerUpRoutine()
    {
        IsActive = true;


        // Particle ×2
        SetParticleRate(
            particleMultiplier
        );


        // افزایش FOV
        if (planeCameraController != null)
        {
            planeCameraController.SetSpeedFOV(true);
        }


        // افزایش سرعت
        yield return StartCoroutine(
            ChangeSpeedMultiplier(
                worldMovement.SpeedMultiplier,
                speedMultiplier
            )
        );


        // مدت Power-up
        yield return new WaitForSeconds(
            duration
        );

        GameEvents.RaiseSpeedBoostDeactivated();

        // برگشت Particle
        SetParticleRate(1f);


        // برگشت FOV
        if (planeCameraController != null)
        {
            planeCameraController.SetSpeedFOV(false);
        }


        // برگشت سرعت
        yield return StartCoroutine(
            ChangeSpeedMultiplier(
                worldMovement.SpeedMultiplier,
                1f
            )
        );


        IsActive = false;

        powerUpCoroutine = null;


        // پاداش پایان Power-up
        gameManager.AddScore(
            bonusScore
        );
        
        
    }


    private IEnumerator ChangeSpeedMultiplier(
        float from,
        float to)
    {
        float elapsed = 0f;


        while (elapsed < speedTransitionDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    speedTransitionDuration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            float multiplier =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );


            worldMovement.SetSpeedMultiplier(
                multiplier
            );


            yield return null;
        }


        worldMovement.SetSpeedMultiplier(to);
    }


    private void SetParticleRate(
        float multiplier)
    {
        if (speedParticle == null)
            return;


        var emission =
            speedParticle.emission;


        emission.rateOverTime =
            initialParticleRate *
            multiplier;
    }
}
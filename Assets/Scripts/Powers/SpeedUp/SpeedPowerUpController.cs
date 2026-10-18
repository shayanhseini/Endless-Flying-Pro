using System.Collections;
using UnityEngine;

public class SpeedPowerUpController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldMovementManager worldMovement;
    [SerializeField] private PlaneCameraController planeCameraController;


    [Header("Settings")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private float speedMultiplier = 1.5f;


    [Header("Speed Transition")]
    [SerializeField] private float speedTransitionDuration = 0.25f;


    [Header("Speed Particle")]
    [SerializeField] private ParticleSystem speedParticle;
    [SerializeField] private float particleMultiplier = 2f;


    private float initialParticleRate;

    private float remainingTime;

    private Coroutine powerUpCoroutine;
    private Coroutine speedTransitionCoroutine;


    public bool IsActive { get; private set; }


    private void Awake()
    {
        if (speedParticle != null)
        {
            var emission =
                speedParticle.emission;

            initialParticleRate =
                emission.rateOverTime.constant;
        }
    }


    public void Activate()
    {
        // اگر PowerUp از قبل فعال است،
        // فقط زمان آن را Refresh کن.
        if (IsActive)
        {
            remainingTime = duration;

            return;
        }


        IsActive = true;

        remainingTime = duration;


        // Particle
        SetParticleRate(
            particleMultiplier
        );


        // FOV
        if (planeCameraController != null)
        {
            planeCameraController.SetSpeedFOV(true);
        }


        // Event فقط یک بار
        GameEvents.RaiseSpeedBoostActivated();


        // اگر Transition قبلی وجود دارد متوقفش کن
        if (speedTransitionCoroutine != null)
        {
            StopCoroutine(
                speedTransitionCoroutine
            );
        }


        speedTransitionCoroutine =
            StartCoroutine(
                ChangeSpeedMultiplier(
                    worldMovement.SpeedMultiplier,
                    speedMultiplier
                )
            );


        // مدیریت زمان
        if (powerUpCoroutine != null)
        {
            StopCoroutine(
                powerUpCoroutine
            );
        }


        powerUpCoroutine =
            StartCoroutine(
                PowerUpRoutine()
            );
    }


    private IEnumerator PowerUpRoutine()
    {
        while (remainingTime > 0f)
        {
            remainingTime -=
                Time.deltaTime;

            yield return null;
        }


        // PowerUp تمام شده
        IsActive = false;


        powerUpCoroutine = null;


        // Event فقط یک بار
        GameEvents.RaiseSpeedBoostDeactivated();


        // Particle
        SetParticleRate(1f);


        // FOV
        if (planeCameraController != null)
        {
            planeCameraController.SetSpeedFOV(false);
        }


        // برگشت سرعت
        if (speedTransitionCoroutine != null)
        {
            StopCoroutine(
                speedTransitionCoroutine
            );
        }


        speedTransitionCoroutine =
            StartCoroutine(
                ChangeSpeedMultiplier(
                    worldMovement.SpeedMultiplier,
                    1f
                )
            );


    }


    private IEnumerator ChangeSpeedMultiplier(
        float from,
        float to)
    {
        float elapsed = 0f;


        while (
            elapsed <
            speedTransitionDuration)
        {
            elapsed +=
                Time.deltaTime;


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


        worldMovement.SetSpeedMultiplier(
            to
        );


        speedTransitionCoroutine = null;
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

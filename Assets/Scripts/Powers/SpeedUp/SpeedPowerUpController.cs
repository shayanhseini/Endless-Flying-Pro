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

    [Header("Engine Flame Boost")]
    [SerializeField] private EngineFlameBoostController engineFlameBoost;

    [Header("Speed Tunnel Effect")]
    [SerializeField] private SpeedTunnelController speedTunnel;

    private float initialParticleRate;
    private float remainingTime;
    private Coroutine powerUpCoroutine;
    private Coroutine speedTransitionCoroutine;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        if (speedParticle != null)
        {
            var emission = speedParticle.emission;
            initialParticleRate = emission.rateOverTime.constant;
        }

        if (engineFlameBoost == null)
        {
            engineFlameBoost = GetComponent<EngineFlameBoostController>();
            if (engineFlameBoost == null)
                engineFlameBoost = GetComponentInChildren<EngineFlameBoostController>(true);
            if (engineFlameBoost == null && transform.parent != null)
                engineFlameBoost = transform.parent.GetComponentInChildren<EngineFlameBoostController>(true);
        }

        if (speedTunnel == null)
        {
            speedTunnel = FindFirstObjectByType<SpeedTunnelController>();
        }
    }

    private void OnDisable()
    {
        if (IsActive)
        {
            IsActive = false;
            if (engineFlameBoost != null)
                engineFlameBoost.ResetToNormalImmediate();
            if (speedTunnel != null)
                speedTunnel.SetTunnelActive(false, immediate: true);
            if (worldMovement != null)
                worldMovement.SetSpeedMultiplier(1f);
        }
    }

    public void Activate()
    {
        // اگر PowerUp از قبل فعال است،
        // فقط زمان آن را Refresh کن و صدا و افکت را مجدداً اعمال کن
        if (IsActive)
        {
            remainingTime = duration;

            // صدایش دوباره پخش شود تا بازیکن متوجه دریافت مجدد شود
            GameEvents.RaiseSpeedBoostActivated();

            if (engineFlameBoost != null)
                engineFlameBoost.SetBoostActive(true);

            if (speedTunnel != null)
                speedTunnel.SetTunnelActive(true);

            return;
        }

        IsActive = true;
        remainingTime = duration;

        // Particle
        SetParticleRate(particleMultiplier);

        // FOV
        if (planeCameraController != null)
        {
            planeCameraController.SetSpeedFOV(true);
        }

        // Engine Flame Boost
        if (engineFlameBoost != null)
        {
            engineFlameBoost.SetBoostActive(true);
        }

        // Speed Tunnel
        if (speedTunnel != null)
        {
            speedTunnel.SetTunnelActive(true);
        }

        // Event
        GameEvents.RaiseSpeedBoostActivated();

        // اگر Transition قبلی وجود دارد متوقفش کن
        if (speedTransitionCoroutine != null)
        {
            StopCoroutine(speedTransitionCoroutine);
        }

        speedTransitionCoroutine = StartCoroutine(
            ChangeSpeedMultiplier(
                worldMovement.SpeedMultiplier,
                speedMultiplier
            )
        );

        // مدیریت زمان
        if (powerUpCoroutine != null)
        {
            StopCoroutine(powerUpCoroutine);
        }

        powerUpCoroutine = StartCoroutine(PowerUpRoutine());
    }

    private IEnumerator PowerUpRoutine()
    {
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        // PowerUp تمام شده
        IsActive = false;
        powerUpCoroutine = null;

        // Engine Flame Boost
        if (engineFlameBoost != null)
        {
            engineFlameBoost.SetBoostActive(false);
        }

        // Speed Tunnel
        if (speedTunnel != null)
        {
            speedTunnel.SetTunnelActive(false);
        }

        // Event
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
            StopCoroutine(speedTransitionCoroutine);
        }

        speedTransitionCoroutine = StartCoroutine(
            ChangeSpeedMultiplier(
                worldMovement.SpeedMultiplier,
                1f
            )
        );
    }

    private IEnumerator ChangeSpeedMultiplier(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < speedTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / speedTransitionDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float multiplier = Mathf.Lerp(from, to, t);
            worldMovement.SetSpeedMultiplier(multiplier);

            yield return null;
        }

        worldMovement.SetSpeedMultiplier(to);
        speedTransitionCoroutine = null;
    }

    private void SetParticleRate(float multiplier)
    {
        if (speedParticle == null)
            return;

        var emission = speedParticle.emission;
        emission.rateOverTime = initialParticleRate * multiplier;
    }
}

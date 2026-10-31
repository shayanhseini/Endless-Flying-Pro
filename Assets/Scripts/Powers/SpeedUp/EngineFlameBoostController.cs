using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the spaceship engine flames during Speed Boost.
/// Transitions thruster particles to a roaring, dense, vibrant orange afterburner
/// with increased length, scale, emission, and dynamic lighting,
/// then smoothly returns to the normal blue flame when the boost ends.
/// </summary>
public class EngineFlameBoostController : MonoBehaviour
{
    [System.Serializable]
    private class ThrusterData
    {
        public Transform transform;
        public Vector3 baseLocalScale;
        public ParticleSystem particleSystem;
        public ParticleSystemRenderer renderer;
        public float baseEmissionRate;
        public float baseStartSpeed;
        public float baseStartSize;
    }

    [Header("References")]
    [Tooltip("The parent container holding the FlameThrower engine instances (e.g. 'Flame').")]
    [SerializeField] private Transform flameContainer;

    [Header("Materials")]
    [Tooltip("Normal engine flame material (blue).")]
    [SerializeField] private Material normalMaterial;

    [Tooltip("Boosted engine flame material (orange).")]
    [SerializeField] private Material boostMaterial;

    [Header("Boost Tuning")]
    [Tooltip("Smooth transition duration in seconds.")]
    [SerializeField] private float transitionDuration = 0.35f;

    [Tooltip("Multiplier for flame width and height (X and Y scale).")]
    [SerializeField] private float scaleMultiplier = 1.45f;

    [Tooltip("Multiplier for flame length (Z scale) shooting backwards.")]
    [SerializeField] private float lengthMultiplier = 2.4f;

    [Tooltip("Multiplier for particle emission rate (density of fire).")]
    [SerializeField] private float emissionMultiplier = 2.8f;

    [Tooltip("Multiplier for particle ejection speed.")]
    [SerializeField] private float speedMultiplier = 1.8f;

    [Tooltip("Multiplier for individual fireball particle size.")]
    [SerializeField] private float sizeMultiplier = 1.5f;

    [Header("Dynamic Engine Glow")]
    [SerializeField] private bool enableDynamicLight = true;
    [SerializeField] private Color normalLightColor = new Color(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color boostLightColor = new Color(1f, 0.45f, 0.08f, 1f);
    [SerializeField] private float normalLightIntensity = 2.5f;
    [SerializeField] private float boostLightIntensity = 9f;
    [SerializeField] private float normalLightRange = 5f;
    [SerializeField] private float boostLightRange = 10f;

    private readonly List<ThrusterData> thrusters = new List<ThrusterData>();
    private Light engineLight;
    private Coroutine transitionCoroutine;
    private float currentBoostProgress;
    private bool isBoosted;

    public bool IsBoosted => isBoosted;

    private void Awake()
    {
        InitializeThrusters();
        SetupDynamicLight();
    }

    private void OnEnable()
    {
        GameEvents.SpeedBoostActivated += HandleSpeedBoostActivated;
        GameEvents.SpeedBoostDeactivated += HandleSpeedBoostDeactivated;
        GameEvents.GameOver += HandleReset;
    }

    private void OnDisable()
    {
        GameEvents.SpeedBoostActivated -= HandleSpeedBoostActivated;
        GameEvents.SpeedBoostDeactivated -= HandleSpeedBoostDeactivated;
        GameEvents.GameOver -= HandleReset;

        ResetToNormalImmediate();
    }

    public void InitializeThrusters()
    {
        if (thrusters.Count > 0)
            return;

        if (flameContainer == null)
        {
            flameContainer = transform.Find("Flame");
            if (flameContainer == null)
            {
                Transform[] allChildren = GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (t.name == "Flame")
                    {
                        flameContainer = t;
                        break;
                    }
                }
            }
        }

        if (flameContainer == null)
            return;

        // Collect all child flame instances
        for (int i = 0; i < flameContainer.childCount; i++)
        {
            Transform child = flameContainer.GetChild(i);
            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            ParticleSystemRenderer rend = child.GetComponent<ParticleSystemRenderer>();

            if (ps != null && rend != null)
            {
                var main = ps.main;
                var emission = ps.emission;

                thrusters.Add(new ThrusterData
                {
                    transform = child,
                    baseLocalScale = child.localScale,
                    particleSystem = ps,
                    renderer = rend,
                    baseEmissionRate = emission.rateOverTime.constant,
                    baseStartSpeed = main.startSpeed.constant,
                    baseStartSize = main.startSize.constant
                });

                if (normalMaterial == null && rend.sharedMaterial != null)
                {
                    normalMaterial = rend.sharedMaterial;
                }
            }
        }
    }

    private void SetupDynamicLight()
    {
        if (!enableDynamicLight)
            return;

        if (engineLight == null && flameContainer != null)
        {
            engineLight = flameContainer.GetComponent<Light>();
            if (engineLight == null)
            {
                engineLight = flameContainer.gameObject.AddComponent<Light>();
                engineLight.type = LightType.Point;
                engineLight.shadows = LightShadows.None;
                engineLight.renderMode = LightRenderMode.ForcePixel;
            }
        }

        if (engineLight != null)
        {
            engineLight.color = normalLightColor;
            engineLight.intensity = normalLightIntensity;
            engineLight.range = normalLightRange;
            engineLight.enabled = true;
        }
    }

    public void SetBoostActive(bool active)
    {
        isBoosted = active;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionRoutine(active ? 1f : 0f));
    }

    private void HandleSpeedBoostActivated()
    {
        SetBoostActive(true);
    }

    private void HandleSpeedBoostDeactivated()
    {
        SetBoostActive(false);
    }

    private void HandleReset()
    {
        ResetToNormalImmediate();
    }

    private IEnumerator TransitionRoutine(float targetProgress)
    {
        float startProgress = currentBoostProgress;
        float duration = Mathf.Max(0.01f, transitionDuration * Mathf.Abs(targetProgress - startProgress));
        float elapsed = 0f;

        // Apply materials at the onset of transition
        if (targetProgress > 0.5f && boostMaterial != null)
        {
            SetThrusterMaterials(boostMaterial);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, rawT);

            currentBoostProgress = Mathf.Lerp(startProgress, targetProgress, smoothT);
            ApplyBoostProgress(currentBoostProgress);

            yield return null;
        }

        currentBoostProgress = targetProgress;
        ApplyBoostProgress(currentBoostProgress);

        // When returning to zero boost, restore normal blue material
        if (currentBoostProgress <= 0.01f && normalMaterial != null)
        {
            SetThrusterMaterials(normalMaterial);
        }

        transitionCoroutine = null;
    }

    private void ApplyBoostProgress(float progress)
    {
        for (int i = 0; i < thrusters.Count; i++)
        {
            ThrusterData t = thrusters[i];
            if (t.transform == null || t.particleSystem == null)
                continue;

            // 1. Scale and Length (Z axis extends the flame length backwards)
            Vector3 targetScale = new Vector3(
                t.baseLocalScale.x * Mathf.Lerp(1f, scaleMultiplier, progress),
                t.baseLocalScale.y * Mathf.Lerp(1f, scaleMultiplier, progress),
                t.baseLocalScale.z * Mathf.Lerp(1f, lengthMultiplier, progress)
            );
            t.transform.localScale = targetScale;

            // 2. Emission Rate (amount of fire / density)
            var emission = t.particleSystem.emission;
            emission.rateOverTime = Mathf.Lerp(t.baseEmissionRate, t.baseEmissionRate * emissionMultiplier, progress);

            // 3. Particle Ejection Speed and Size
            var main = t.particleSystem.main;
            main.startSpeed = Mathf.Lerp(t.baseStartSpeed, t.baseStartSpeed * speedMultiplier, progress);
            main.startSize = Mathf.Lerp(t.baseStartSize, t.baseStartSize * sizeMultiplier, progress);
        }

        // 4. Dynamic Light Glow on Hull and Thrusters
        if (engineLight != null && enableDynamicLight)
        {
            engineLight.color = Color.Lerp(normalLightColor, boostLightColor, progress);
            engineLight.intensity = Mathf.Lerp(normalLightIntensity, boostLightIntensity, progress);
            engineLight.range = Mathf.Lerp(normalLightRange, boostLightRange, progress);
        }
    }

    private void SetThrusterMaterials(Material mat)
    {
        if (mat == null)
            return;

        for (int i = 0; i < thrusters.Count; i++)
        {
            if (thrusters[i].renderer != null)
            {
                thrusters[i].renderer.sharedMaterial = mat;
            }
        }
    }

    public void ResetToNormalImmediate()
    {
        isBoosted = false;
        currentBoostProgress = 0f;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        ApplyBoostProgress(0f);

        if (normalMaterial != null)
        {
            SetThrusterMaterials(normalMaterial);
        }

        if (engineLight != null && enableDynamicLight)
        {
            engineLight.color = normalLightColor;
            engineLight.intensity = normalLightIntensity;
            engineLight.range = normalLightRange;
        }
    }
}

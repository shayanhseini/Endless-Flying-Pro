using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple and clean Speed Lines effect for Boost PowerUp.
/// Displays a soft, subtle radial speed lines overlay around the screen edges,
/// keeping the center 100% clean and clear with zero dots or clutter.
/// </summary>
public class SpeedTunnelController : MonoBehaviour
{
    [Header("Screen Speed Lines")]
    [SerializeField] private Image speedVignetteImage;
    [SerializeField, Range(0.05f, 1f)] private float maxVignetteAlpha = 0.28f;
    [SerializeField] private bool subtleJitter = true;
    [SerializeField] private float jitterSpeed = 16f;

    [Header("Transitions")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Assets References")]
    [SerializeField] private Sprite vignetteSprite;

    private Coroutine transitionCoroutine;
    private float currentAlpha = 0f;
    private bool isBoostActive = false;
    private RectTransform imageRect;
    private Vector3 initialScale = Vector3.one;

    public bool IsActive => isBoostActive;

    private void Awake()
    {
        // Clean up any legacy 3D particle objects to ensure zero dots in the middle
        Transform oldStreaks = transform.Find("Speed Streaks");
        if (oldStreaks != null) Destroy(oldStreaks.gameObject);

        Transform oldRings = transform.Find("Speed Rings");
        if (oldRings != null) Destroy(oldRings.gameObject);

        ResolveDependencies();
    }

    private void Start()
    {
        ApplyIntensity(0f);
    }

    private void OnEnable()
    {
        GameEvents.SpeedBoostActivated += HandleBoostActivated;
        GameEvents.SpeedBoostDeactivated += HandleBoostDeactivated;
        GameEvents.CrashLandingStarted += HandleEmergencyStop;
        GameEvents.GameOver += HandleEmergencyStop;
        GameEvents.RoundCountdownStarted += HandleEmergencyStop;
    }

    private void OnDisable()
    {
        GameEvents.SpeedBoostActivated -= HandleBoostActivated;
        GameEvents.SpeedBoostDeactivated -= HandleBoostDeactivated;
        GameEvents.CrashLandingStarted -= HandleEmergencyStop;
        GameEvents.GameOver -= HandleEmergencyStop;
        GameEvents.RoundCountdownStarted -= HandleEmergencyStop;

        StopAllCoroutines();
        ApplyIntensity(0f, immediateClear: true);
    }

    private void Update()
    {
        if (speedVignetteImage != null && isBoostActive && currentAlpha > 0.001f)
        {
            float dynamicAlpha = currentAlpha;
            if (subtleJitter)
            {
                // Very gentle, subtle breathing/jitter on edges
                float pulse = Mathf.Sin(Time.time * jitterSpeed) * 0.03f;
                dynamicAlpha = Mathf.Clamp(currentAlpha + pulse, 0f, 1f);

                if (imageRect != null)
                {
                    float scaleShift = 1f + Mathf.Sin(Time.time * (jitterSpeed * 1.5f)) * 0.015f;
                    imageRect.localScale = initialScale * scaleShift;
                }
            }

            Color col = speedVignetteImage.color;
            col.a = dynamicAlpha;
            speedVignetteImage.color = col;
        }
    }

    public void SetTunnelActive(bool active, bool immediate = false)
    {
        if (isBoostActive == active && !immediate)
            return;

        isBoostActive = active;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (immediate)
        {
            ApplyIntensity(active ? 1f : 0f, immediateClear: !active);
            currentAlpha = active ? maxVignetteAlpha : 0f;
            if (speedVignetteImage != null)
            {
                Color c = speedVignetteImage.color;
                c.a = currentAlpha;
                speedVignetteImage.color = c;
            }
        }
        else
        {
            float target = active ? 1f : 0f;
            float duration = active ? fadeInDuration : fadeOutDuration;
            transitionCoroutine = StartCoroutine(TransitionRoutine(target, duration));
        }
    }

    private IEnumerator TransitionRoutine(float targetWeight, float duration)
    {
        float startWeight = currentAlpha / Mathf.Max(0.01f, maxVignetteAlpha);
        startWeight = Mathf.Clamp01(startWeight);

        if (targetWeight > 0.01f && speedVignetteImage != null)
        {
            speedVignetteImage.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float weight = Mathf.Lerp(startWeight, targetWeight, t);

            ApplyIntensity(weight);
            currentAlpha = weight * maxVignetteAlpha;
            yield return null;
        }

        ApplyIntensity(targetWeight);
        currentAlpha = targetWeight * maxVignetteAlpha;

        if (targetWeight <= 0.01f && speedVignetteImage != null)
        {
            speedVignetteImage.gameObject.SetActive(false);
            if (imageRect != null)
                imageRect.localScale = initialScale;
        }

        transitionCoroutine = null;
    }

    private void ApplyIntensity(float weight, bool immediateClear = false)
    {
        weight = Mathf.Clamp01(weight);

        if (speedVignetteImage != null)
        {
            Color c = speedVignetteImage.color;
            c.a = weight * maxVignetteAlpha;
            speedVignetteImage.color = c;
            speedVignetteImage.gameObject.SetActive(weight > 0.001f || isBoostActive);

            if (immediateClear)
            {
                c.a = 0f;
                speedVignetteImage.color = c;
                speedVignetteImage.gameObject.SetActive(false);
                if (imageRect != null)
                    imageRect.localScale = initialScale;
            }
        }
    }

    private void HandleBoostActivated()
    {
        SetTunnelActive(true);
    }

    private void HandleBoostDeactivated()
    {
        SetTunnelActive(false);
    }

    private void HandleEmergencyStop()
    {
        SetTunnelActive(false, immediate: true);
    }

    public void ResolveDependencies()
    {
        EnsureScreenVignette();
    }

    private void EnsureScreenVignette()
    {
        if (speedVignetteImage != null)
        {
            imageRect = speedVignetteImage.rectTransform;
            initialScale = imageRect != null ? imageRect.localScale : Vector3.one;
            return;
        }

        GameObject vignetteObj = GameObject.Find("Speed Lines Vignette");
        if (vignetteObj != null && vignetteObj.TryGetComponent<Image>(out var img))
        {
            speedVignetteImage = img;
            imageRect = speedVignetteImage.rectTransform;
            initialScale = imageRect != null ? imageRect.localScale : Vector3.one;
            return;
        }

        GameObject gameplayUI = GameObject.Find("Gameplay UI");
        Transform parentTransform = gameplayUI != null ? gameplayUI.transform : null;
        if (parentTransform == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                parentTransform = canvas.transform;
        }

        if (parentTransform != null)
        {
            GameObject go = new GameObject("Speed Lines Vignette");
            go.transform.SetParent(parentTransform, false);
            go.transform.SetSiblingIndex(0);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            speedVignetteImage = go.AddComponent<Image>();
            speedVignetteImage.raycastTarget = false;
            if (vignetteSprite != null)
                speedVignetteImage.sprite = vignetteSprite;
            speedVignetteImage.color = new Color(0.85f, 0.95f, 1f, 0f);
            go.SetActive(false);

            imageRect = rt;
            initialScale = rt.localScale;
        }
    }
}

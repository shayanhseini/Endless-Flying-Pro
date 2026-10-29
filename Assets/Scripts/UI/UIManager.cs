using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RTLTMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Crash Damage")]
    [SerializeField] private UnityEngine.UI.Image damageImage;
    [Min(0.1f)]
    [SerializeField] private float damagePulseDuration = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float damageMaximumAlpha = 0.2f;

    [Header("Round Countdown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_FontAsset countdownFont;
    [Min(1f)]
    [SerializeField] private float countdownStartFontSize = 50f;
    [Min(1f)]
    [SerializeField] private float countdownEndFontSize = 500f;

    [Header("Planet Sequence UI")]
    [SerializeField] private GameObject leftPlanetPanel;
    [SerializeField] private GameObject rightPlanetPanel;
    [SerializeField] private RTLTextMeshPro leftPlanetNameText;
    [SerializeField] private RTLTextMeshPro leftPlanetDescriptionText;
    [SerializeField] private RTLTextMeshPro rightPlanetNameText;
    [SerializeField] private RTLTextMeshPro rightPlanetDescriptionText;
    [SerializeField] private UnityEngine.UI.Image leftPlanetImage;
    [SerializeField] private UnityEngine.UI.Image rightPlanetImage;
    [UnityEngine.Serialization.FormerlySerializedAs("planetSprites")]
    [SerializeField] private Sprite[] planetPanelSprites = new Sprite[9];

    [Header("Situation Panel")]
    [SerializeField] private GameObject situationPanel;
    [SerializeField] private bool showSituationBackground = false;
    [SerializeField] private UnityEngine.UI.Image currentPlanetImage;
    [SerializeField] private TMP_Text planetProgressText;
    [SerializeField] private RectTransform planetsLine;
    [SerializeField] private Sprite[] situationPlanetSprites = new Sprite[9];
    [SerializeField] private Color reachedPlanetColor = new(0.2f, 0.65f, 1f, 1f);
    [SerializeField] private Color unreachedPlanetColor = new(0.38f, 0.42f, 0.48f, 1f);
    [SerializeField] private float reachedPlanetDotSize = 14f;
    [SerializeField] private float unreachedPlanetDotSize = 18f;
    [SerializeField] private float currentPlanetDotSize = 30f;
    [Tooltip("Visual scale multiplier for Saturn progress icon to compensate for wide ring aspect ratio.")]
    [SerializeField] private float saturnProgressDotScale = 1.45f;
    [SerializeField] private UnityEngine.UI.Image[] planetProgressDots = new UnityEngine.UI.Image[9];
    [SerializeField] private UnityEngine.UI.Image progressTrackImage;
    [SerializeField] private UnityEngine.UI.Image completedProgressImage;
    [SerializeField] private UnityEngine.UI.Image planetRingImage;
    [SerializeField] private RTLTextMeshPro[] planetNameTexts = new RTLTextMeshPro[9];
    [SerializeField] private Color reachedPlanetNameColor = new(0.9f, 0.9f, 0.9f, 0.85f);
    [SerializeField] private Color unreachedPlanetNameColor = new(0.6f, 0.6f, 0.6f, 0.45f);
    [SerializeField] private Color activePlanetNameColor = new(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private float activePlanetPulseSpeed = 3.5f;

    private static readonly string[] PersianPlanetNames =
    {
        "نپتون",
        "اورانوس",
        "زحل",
        "مشتری",
        "مریخ",
        "زمین",
        "زهره",
        "عطارد",
        "خورشید"
    };

    private Coroutine activePlanetPulseCoroutine;
    [Min(0.05f)]
    [SerializeField] private float planetImageTransitionDuration = 0.8f;
    [Header("Planet Reached Effect")]
    [Min(0.01f)]
    [SerializeField] private float planetDotGrowDuration = 0.2f;
    [Min(0.01f)]
    [SerializeField] private float planetDotReturnDuration = 0.4f;
    [Min(1f)]
    [SerializeField] private float planetDotPulseMultiplier = 1.7f;
    [Min(0.05f)]
    [SerializeField] private float planetRingFadeDuration = 0.7f;
    [Min(0f)]
    [SerializeField] private float planetRingVisibleDuration = 5f;
    [Min(0.05f)]
    [SerializeField] private float progressGlowDuration = 0.6f;
    [Min(1f)]
    [SerializeField] private float progressGlowThicknessMultiplier = 2.3f;
    [SerializeField] private Color progressGlowColor = new(0.15f, 0.9f, 1f, 1f);
    [SerializeField] private GameObject planetPhasePanel;
    [SerializeField] private RTLTextMeshPro planetTitleText;
    [SerializeField] private RTLTextMeshPro planetDescriptionText;
    [SerializeField] private RTLTextMeshPro planetTimerText;
    [SerializeField] private GameObject winPanel;

    private CanvasGroup leftPlanetCanvasGroup;
    private CanvasGroup rightPlanetCanvasGroup;
    private CanvasGroup situationCanvasGroup;
    private Coroutine damagePulseCoroutine;
    private Coroutine situationImageCoroutine;
    private Coroutine countdownAnimationCoroutine;
    private Coroutine planetDotPulseCoroutine;
    private Coroutine planetRingFadeCoroutine;
    private Coroutine progressGlowCoroutine;
    private Vector2 currentPlanetImageAnchoredPosition;
    private Vector2 currentPlanetImageSizeDelta;
    private float currentPlanetImageBaseAlpha = 1f;
    private int currentSituationPlanetIndex = -1;
    private Color progressLineBaseColor;
    private float progressLineBaseHeight = 3f;
    private float progressLineVisualHeight = 3f;

    private readonly Dictionary<CanvasGroup, Coroutine> fadeCoroutines = new();

    private void Awake()
    {
        ResolvePlanetPanelReferences();
        ResolveSituationPanelReferences();
        ResolveCountdownReferences();
        if (currentPlanetImage != null)
        {
            currentPlanetImageAnchoredPosition = currentPlanetImage.rectTransform.anchoredPosition;
            currentPlanetImageSizeDelta = currentPlanetImage.rectTransform.sizeDelta;
            currentPlanetImageBaseAlpha = currentPlanetImage.color.a;
        }
        EnsurePlanetSequenceUI();
        InitializePlanetPanelFades();
        InitializePlanetReachedEffects();
        InitializePlanetProgressLine();
        FindAndInitializeDamageImage();

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (planetPhasePanel != null &&
            leftPlanetPanel == null &&
            rightPlanetPanel == null)
            planetPhasePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.GameStarted += HandleGameStarted;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.PlanetIntroductionStarted += ShowPlanetIntroduction;
        GameEvents.PlanetIntroductionEnded += HidePlanetIntroduction;
        GameEvents.PlanetProgressChanged += UpdateRealtimePlanetProgress;
        GameEvents.GameWon += HandleGameWon;
        GameEvents.CrashLandingStarted += HandleCrashLandingStarted;
        GameEvents.VictoryOrbitStarted += HidePlanetIntroduction;
        GameEvents.AtmosphereReached += HideDamageWarning;
        GameEvents.RoundCountdownStarted += HandleRoundCountdownStarted;
        GameEvents.RoundCountdownTick += ShowCountdownNumber;
        GameEvents.RoundCountdownCompleted += HandleRoundCountdownCompleted;
    }

    private void OnDisable()
    {
        if (activePlanetPulseCoroutine != null)
        {
            StopCoroutine(activePlanetPulseCoroutine);
            activePlanetPulseCoroutine = null;
        }
        GameEvents.GameStarted -= HandleGameStarted;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.PlanetIntroductionStarted -= ShowPlanetIntroduction;
        GameEvents.PlanetIntroductionEnded -= HidePlanetIntroduction;
        GameEvents.PlanetProgressChanged -= UpdateRealtimePlanetProgress;
        GameEvents.GameWon -= HandleGameWon;
        GameEvents.CrashLandingStarted -= HandleCrashLandingStarted;
        GameEvents.VictoryOrbitStarted -= HidePlanetIntroduction;
        GameEvents.AtmosphereReached -= HideDamageWarning;
        GameEvents.RoundCountdownStarted -= HandleRoundCountdownStarted;
        GameEvents.RoundCountdownTick -= ShowCountdownNumber;
        GameEvents.RoundCountdownCompleted -= HandleRoundCountdownCompleted;
    }

    private void HandleGameStarted()
    {
        FadeIn(gameCanvasGroup);
    }

    private void HandleGameOver()
    {
        if (activePlanetPulseCoroutine != null)
        {
            StopCoroutine(activePlanetPulseCoroutine);
            activePlanetPulseCoroutine = null;
        }

        FadeOut(gameCanvasGroup);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        FadeIn(gameOverCanvasGroup);
    }

    private void HandleCrashLandingStarted()
    {
        HidePlanetIntroduction();
        FadeOut(situationCanvasGroup);

        if (damageImage == null)
            return;

        if (damagePulseCoroutine != null)
            StopCoroutine(damagePulseCoroutine);

        damageImage.gameObject.SetActive(true);
        damagePulseCoroutine = StartCoroutine(PulseDamageRoutine());
    }

    private void HandleRoundCountdownStarted()
    {
        HidePlanetIntroduction();
        FadeOut(situationCanvasGroup);
        HideDamageWarning();

        if (countdownPanel != null)
            countdownPanel.SetActive(true);
    }

    private void ShowCountdownNumber(int value, float stepDuration)
    {
        if (countdownPanel == null || countdownText == null)
            return;

        countdownPanel.SetActive(true);
        if (countdownAnimationCoroutine != null)
            StopCoroutine(countdownAnimationCoroutine);

        countdownAnimationCoroutine = StartCoroutine(
            AnimateCountdownNumber(value, stepDuration));
    }

    private IEnumerator AnimateCountdownNumber(int value, float stepDuration)
    {
        EnsureCountdownFont();
        countdownText.text = ToPersianDigits(value.ToString());
        Color color = countdownText.color;
        color.a = 1f;
        countdownText.color = color;
        countdownText.fontSize = countdownStartFontSize;

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, stepDuration));
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            countdownText.fontSize = Mathf.Lerp(
                countdownStartFontSize,
                countdownEndFontSize,
                easedProgress);
            color.a = 1f - Mathf.SmoothStep(0f, 1f, progress);
            countdownText.color = color;
            yield return null;
        }

        countdownText.fontSize = countdownEndFontSize;
        color.a = 0f;
        countdownText.color = color;
        countdownAnimationCoroutine = null;
    }

    private void HandleRoundCountdownCompleted()
    {
        if (countdownAnimationCoroutine != null)
        {
            StopCoroutine(countdownAnimationCoroutine);
            countdownAnimationCoroutine = null;
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        HideDamageWarning();
        FadeIn(situationCanvasGroup);
    }

    private void ResolveCountdownReferences()
    {
        if (countdownPanel == null)
        {
            RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (rectTransform.gameObject.name == "Countdown Panel")
                {
                    countdownPanel = rectTransform.gameObject;
                    break;
                }
            }
        }

        if (countdownPanel != null && countdownText == null)
            countdownText = countdownPanel.GetComponentInChildren<TMP_Text>(true);

        if (countdownText != null)
        {
            countdownText.raycastTarget = false;
            EnsureCountdownFont();
        }
    }

    private void EnsureCountdownFont()
    {
        if (countdownText == null)
            return;

        if (countdownFont != null)
        {
            if (countdownText.font != countdownFont)
                countdownText.font = countdownFont;
        }
        else if (leftPlanetNameText != null && leftPlanetNameText.font != null)
        {
            if (countdownText.font != leftPlanetNameText.font)
                countdownText.font = leftPlanetNameText.font;
        }

        // Disable any SDF underlay / shadow on countdown numbers
        Material mat = countdownText.fontMaterial;
        if (mat != null)
        {
            mat.DisableKeyword("UNDERLAY_ON");
            mat.DisableKeyword("UNDERLAY_INNER");
            mat.SetColor("_UnderlayColor", Color.clear);
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", 0f);
            mat.SetFloat("_UnderlayDilate", 0f);
            mat.SetFloat("_UnderlaySoftness", 0f);
        }
    }

    private void FindAndInitializeDamageImage()
    {
        if (damageImage == null)
        {
            UnityEngine.UI.Image[] images = FindObjectsByType<UnityEngine.UI.Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (UnityEngine.UI.Image image in images)
            {
                if (image.gameObject.name == "Damage")
                {
                    damageImage = image;
                    break;
                }
            }
        }

        if (damageImage == null)
            return;

        Color color = damageImage.color;
        color.a = 0f;
        damageImage.color = color;
        damageImage.raycastTarget = false;
        damageImage.gameObject.SetActive(true);
    }

    private IEnumerator PulseDamageRoutine()
    {
        Color color = damageImage.color;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            float cycle = Mathf.Repeat(elapsed / damagePulseDuration, 1f);
            color.a = Mathf.PingPong(cycle * 2f, 1f) * damageMaximumAlpha;
            damageImage.color = color;
            yield return null;
        }
    }

    private void HideDamageWarning()
    {
        if (damageImage == null)
            return;

        if (damagePulseCoroutine != null)
            StopCoroutine(damagePulseCoroutine);

        damagePulseCoroutine = StartCoroutine(FadeDamageWarningOut());
    }

    private IEnumerator FadeDamageWarningOut()
    {
        Color color = damageImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / fadeDuration));
            color.a = Mathf.Lerp(startAlpha, 0f, progress);
            damageImage.color = color;
            yield return null;
        }

        color.a = 0f;
        damageImage.color = color;
        damagePulseCoroutine = null;
    }

    private void ShowPlanetIntroduction(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft,
        int planetIndex)
    {
        SelectPlanetPanel(planetOnLeft);

        RTLTextMeshPro nameText = planetOnLeft
            ? rightPlanetNameText
            : leftPlanetNameText;
        RTLTextMeshPro descriptionText = planetOnLeft
            ? rightPlanetDescriptionText
            : leftPlanetDescriptionText;

        if (nameText != null)
        {
            nameText.gameObject.SetActive(true);
            nameText.horizontalAlignment = HorizontalAlignmentOptions.Justified;
            nameText.text = planetName;
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.horizontalAlignment = HorizontalAlignmentOptions.Justified;
            descriptionText.text = description;
        }

        UnityEngine.UI.Image planetImage = planetOnLeft
            ? rightPlanetImage
            : leftPlanetImage;
        SetPlanetImage(planetImage, planetIndex);
        UpdateSituationPanel(planetIndex);
        PlayPlanetReachedEffects(planetIndex);
        FadeIn(situationCanvasGroup);
    }

    private void InitializePlanetReachedEffects()
    {
        if (completedProgressImage != null)
        {
            progressLineBaseColor = completedProgressImage.color;
            progressLineBaseHeight = Mathf.Max(
                1f,
                completedProgressImage.rectTransform.sizeDelta.y);
            progressLineVisualHeight = progressLineBaseHeight;
        }

        if (planetRingImage != null)
        {
            Color color = planetRingImage.color;
            color.a = 0f;
            planetRingImage.color = color;
            planetRingImage.raycastTarget = false;
        }
    }

    private void PlayPlanetReachedEffects(int planetIndex)
    {
        if (planetRingImage != null)
        {
            if (planetRingFadeCoroutine != null)
                StopCoroutine(planetRingFadeCoroutine);
            planetRingFadeCoroutine = StartCoroutine(FadePlanetRing());
        }

        if (completedProgressImage != null)
        {
            if (progressGlowCoroutine != null)
                StopCoroutine(progressGlowCoroutine);
            progressGlowCoroutine = StartCoroutine(PulseProgressGlow());
        }
    }

    private IEnumerator PulsePlanetDot(UnityEngine.UI.Image dot)
    {
        RectTransform rect = dot.rectTransform;
        float normalSize = currentPlanetDotSize;
        float pulseSize = normalSize * planetDotPulseMultiplier;
        float elapsed = 0f;

        while (elapsed < planetDotGrowDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / planetDotGrowDuration));
            SetSquareSize(rect, Mathf.Lerp(normalSize, pulseSize, progress));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < planetDotReturnDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / planetDotReturnDuration));
            SetSquareSize(rect, Mathf.Lerp(pulseSize, normalSize, progress));
            yield return null;
        }

        SetSquareSize(rect, normalSize);
        planetDotPulseCoroutine = null;
    }

    private static void SetSquareSize(RectTransform rect, float size)
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }

    private IEnumerator FadePlanetRing()
    {
        Color color = planetRingImage.color;
        float fadeInDuration = planetRingFadeDuration * 0.25f;
        float fadeOutDuration = planetRingFadeDuration - fadeInDuration;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / fadeInDuration));
            planetRingImage.color = color;
            yield return null;
        }

        color.a = 1f;
        planetRingImage.color = color;
        if (planetRingVisibleDuration > 0f)
            yield return new WaitForSeconds(planetRingVisibleDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / fadeOutDuration));
            planetRingImage.color = color;
            yield return null;
        }

        color.a = 0f;
        planetRingImage.color = color;
        planetRingFadeCoroutine = null;
    }

    private IEnumerator PulseProgressGlow()
    {
        float elapsed = 0f;
        while (elapsed < progressGlowDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / progressGlowDuration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            completedProgressImage.color = Color.Lerp(
                progressLineBaseColor,
                progressGlowColor,
                pulse);
            progressLineVisualHeight = progressLineBaseHeight * Mathf.Lerp(
                1f,
                progressGlowThicknessMultiplier,
                pulse);
            yield return null;
        }

        completedProgressImage.color = progressLineBaseColor;
        progressLineVisualHeight = progressLineBaseHeight;
        progressGlowCoroutine = null;
    }

    private void SetPlanetImage(UnityEngine.UI.Image image, int planetIndex)
    {
        if (image == null || planetPanelSprites == null ||
            planetIndex < 0 || planetIndex >= planetPanelSprites.Length)
            return;

        Sprite sprite = planetPanelSprites[planetIndex];
        if (sprite == null || sprite.rect.height <= 0f)
            return;

        SetImageKeepingHeightAndCenter(image, sprite);
    }

    private static void SetImageKeepingHeightAndCenter(
        UnityEngine.UI.Image image,
        Sprite sprite)
    {
        if (image == null || sprite == null || sprite.rect.height <= 0f)
            return;

        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        float fixedHeight = rect.rect.height;
        if (fixedHeight <= 0f)
            fixedHeight = rect.sizeDelta.y;
        if (fixedHeight <= 0f)
            return;

        Vector3 visualCenterBeforeResize = rect.TransformPoint(rect.rect.center);
        float aspectRatio = sprite.rect.width / sprite.rect.height;
        rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            fixedHeight * aspectRatio);

        Vector3 visualCenterAfterResize = rect.TransformPoint(rect.rect.center);
        rect.position += visualCenterBeforeResize - visualCenterAfterResize;
    }

    private static string ToPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace('0', '۰')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '۴')
            .Replace('5', '۵')
            .Replace('6', '۶')
            .Replace('7', '۷')
            .Replace('8', '۸')
            .Replace('9', '۹');
    }

    private void UpdateSituationPanel(int planetIndex)
    {
        int totalPlanets = planetProgressDots != null && planetProgressDots.Length > 0
            ? planetProgressDots.Length
            : PersianPlanetNames.Length;

        if (planetIndex < 0 || planetIndex >= totalPlanets)
            return;

        if (planetProgressText != null)
            planetProgressText.text = ToPersianDigits($"{planetIndex + 1}/{totalPlanets}");

        Sprite situationSprite = GetSituationPlanetSprite(planetIndex);
        UpdateSituationPlanetImage(situationSprite, planetIndex);

        if (planetProgressDots == null || planetProgressDots.Length == 0)
            return;

        for (int i = 0; i < planetProgressDots.Length; i++)
        {
            UnityEngine.UI.Image dot = planetProgressDots[i];
            if (dot == null)
                continue;

            bool isCurrent = i == planetIndex;
            bool isPassed = i < planetIndex;
            float dotScale = GetPlanetProgressDotScale(i);

            if (isCurrent)
            {
                dot.color = Color.white;
                SetSquareSize(dot.rectTransform, currentPlanetDotSize * dotScale);
            }
            else if (isPassed)
            {
                dot.color = reachedPlanetColor;
                SetSquareSize(dot.rectTransform, reachedPlanetDotSize * dotScale);
            }
            else
            {
                dot.color = unreachedPlanetColor;
                SetSquareSize(dot.rectTransform, unreachedPlanetDotSize * dotScale);
            }

            if (planetNameTexts != null && i < planetNameTexts.Length && planetNameTexts[i] != null)
            {
                if (isCurrent)
                {
                    planetNameTexts[i].color = activePlanetNameColor;
                    planetNameTexts[i].fontStyle = FontStyles.Bold;
                }
                else if (isPassed)
                {
                    planetNameTexts[i].color = reachedPlanetNameColor;
                    planetNameTexts[i].fontStyle = FontStyles.Normal;
                }
                else
                {
                    planetNameTexts[i].color = unreachedPlanetNameColor;
                    planetNameTexts[i].fontStyle = FontStyles.Normal;
                }
            }
        }

        if (activePlanetPulseCoroutine != null)
        {
            StopCoroutine(activePlanetPulseCoroutine);
            activePlanetPulseCoroutine = null;
        }

        activePlanetPulseCoroutine = StartCoroutine(PulseActivePlanetRoutine(planetIndex));

        UpdateCompletedProgressLine(planetIndex);
        currentSituationPlanetIndex = planetIndex;
    }

    private float GetPlanetProgressDotScale(int planetIndex)
    {
        if (planetIndex == 2)
            return saturnProgressDotScale;

        return 1f;
    }

    private IEnumerator PulseActivePlanetRoutine(int activeIndex)
    {
        if (planetProgressDots == null || activeIndex < 0 || activeIndex >= planetProgressDots.Length)
            yield break;

        UnityEngine.UI.Image activeImage = planetProgressDots[activeIndex];
        if (activeImage == null)
            yield break;

        RectTransform rect = activeImage.rectTransform;
        float baseSize = currentPlanetDotSize * GetPlanetProgressDotScale(activeIndex);
        float maxSize = baseSize * planetDotPulseMultiplier;

        while (true)
        {
            float t = (Mathf.Sin(Time.time * activePlanetPulseSpeed) + 1f) * 0.5f;
            float size = Mathf.Lerp(baseSize, maxSize, t);
            SetSquareSize(rect, size);
            yield return null;
        }
    }

    private Sprite GetSituationPlanetSprite(int planetIndex)
    {
        if (situationPlanetSprites != null &&
            planetIndex >= 0 && planetIndex < situationPlanetSprites.Length &&
            situationPlanetSprites[planetIndex] != null)
        {
            Sprite sprite = situationPlanetSprites[planetIndex];
            if (planetIndex == 2 && sprite != null && sprite.name.Contains("Ringless"))
            {
                if (planetPanelSprites != null && planetPanelSprites.Length > 2 && planetPanelSprites[2] != null)
                    return planetPanelSprites[2];
            }
            return sprite;
        }

        if (planetPanelSprites != null &&
            planetIndex >= 0 && planetIndex < planetPanelSprites.Length)
            return planetPanelSprites[planetIndex];

        return null;
    }

    private void UpdateSituationPlanetImage(Sprite sprite, int planetIndex)
    {
        if (currentPlanetImage == null || sprite == null)
            return;

        if (currentSituationPlanetIndex < 0 || currentSituationPlanetIndex == planetIndex)
        {
            SetCurrentPlanetSituationImage(sprite);
            SetCurrentPlanetImageAlpha(currentPlanetImageBaseAlpha);
            return;
        }

        if (situationImageCoroutine != null)
            StopCoroutine(situationImageCoroutine);
        situationImageCoroutine = StartCoroutine(ChangeSituationImageRoutine(sprite));
    }

    private IEnumerator ChangeSituationImageRoutine(Sprite sprite)
    {
        float halfDuration = planetImageTransitionDuration * 0.5f;
        float startAlpha = currentPlanetImage.color.a;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            SetCurrentPlanetImageAlpha(Mathf.Lerp(startAlpha, 0f, progress));
            RestoreCurrentPlanetImageRect();
            yield return null;
        }

        SetCurrentPlanetImageAlpha(0f);
        SetCurrentPlanetSituationImage(sprite);
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            SetCurrentPlanetImageAlpha(Mathf.Lerp(0f, currentPlanetImageBaseAlpha, progress));
            RestoreCurrentPlanetImageRect();
            yield return null;
        }

        SetCurrentPlanetImageAlpha(currentPlanetImageBaseAlpha);
        RestoreCurrentPlanetImageRect();
        situationImageCoroutine = null;
    }

    private void SetCurrentPlanetSituationImage(Sprite sprite)
    {
        if (currentPlanetImage == null || sprite == null || sprite.rect.height <= 0f)
            return;

        currentPlanetImage.sprite = sprite;
        currentPlanetImage.preserveAspect = true;
        currentPlanetImage.raycastTarget = false;
        RestoreCurrentPlanetImageRect();
    }

    private void RestoreCurrentPlanetImageRect()
    {
        if (currentPlanetImage == null)
            return;

        RectTransform rect = currentPlanetImage.rectTransform;
        rect.sizeDelta = currentPlanetImageSizeDelta;
        rect.anchoredPosition = currentPlanetImageAnchoredPosition;
    }

    private void SetCurrentPlanetImageAlpha(float alpha)
    {
        Color color = currentPlanetImage.color;
        color.a = alpha;
        currentPlanetImage.color = color;
    }

    private void ResolveSituationPanelReferences()
    {
        RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (RectTransform rectTransform in rectTransforms)
        {
            string objectName = rectTransform.gameObject.name;
            if (situationPanel == null && objectName == "Situation Panel")
                situationPanel = rectTransform.gameObject;
            else if (planetsLine == null && objectName == "Planets Line")
                planetsLine = rectTransform;
        }

        if (situationPanel == null)
            return;

        if (situationPanel.TryGetComponent<UnityEngine.UI.Image>(out var bgImage))
        {
            bgImage.enabled = showSituationBackground;
        }

        UnityEngine.UI.Image[] images = situationPanel.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (UnityEngine.UI.Image image in images)
        {
            if (currentPlanetImage == null && image.gameObject.name == "Current planet Image")
                currentPlanetImage = image;
        }

        TMP_Text[] texts = situationPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text.text.Contains("/") && (text.text.Contains("8") || text.text.Contains("۸") || text.text.Contains("9") || text.text.Contains("۹")))
            {
                planetProgressText = text;
                break;
            }
        }
    }

    private void EnsurePlanetNameLabels()
    {
        if (planetsLine == null || planetProgressDots == null)
            return;

        if (planetNameTexts == null || planetNameTexts.Length != planetProgressDots.Length)
            planetNameTexts = new RTLTextMeshPro[planetProgressDots.Length];

        TMP_FontAsset persianFont = null;
        Material persianMaterial = null;
        if (leftPlanetNameText != null)
        {
            persianFont = leftPlanetNameText.font;
            persianMaterial = leftPlanetNameText.fontSharedMaterial;
        }

        for (int i = 0; i < planetProgressDots.Length; i++)
        {
            if (planetProgressDots[i] == null)
                continue;

            Transform dotTransform = planetProgressDots[i].transform;
            RTLTextMeshPro label = dotTransform.GetComponentInChildren<RTLTextMeshPro>(true);

            if (label == null)
            {
                Transform labelTransform = dotTransform.Find("PlanetName");
                if (labelTransform == null)
                {
                    GameObject labelObj = new("PlanetName", typeof(RectTransform), typeof(RTLTextMeshPro));
                    labelObj.layer = 5;
                    labelTransform = labelObj.transform;
                    labelTransform.SetParent(dotTransform, false);
                }

                label = labelTransform.GetComponent<RTLTextMeshPro>();
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 28f);
            labelRect.sizeDelta = new Vector2(70f, 22f);

            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.enableAutoSizing = false;
            label.Farsi = true;
            label.PreserveNumbers = false;

            if (persianFont != null)
            {
                label.font = persianFont;
                if (persianMaterial != null)
                    label.fontSharedMaterial = persianMaterial;
            }

            string planetName = (i >= 0 && i < PersianPlanetNames.Length)
                ? PersianPlanetNames[i]
                : string.Empty;

            label.text = planetName;
            planetNameTexts[i] = label;
        }
    }

    private void InitializePlanetProgressLine()
    {
        if (planetsLine == null || planetProgressDots == null || planetProgressDots.Length == 0)
            return;

        EnsurePlanetNameLabels();

        float startX = -246.0912f;
        float endX = 246.0912f;
        float stepX = (endX - startX) / Mathf.Max(1, planetProgressDots.Length - 1);

        for (int i = 0; i < planetProgressDots.Length; i++)
        {
            if (planetProgressDots[i] == null)
                return;

            planetProgressDots[i].raycastTarget = false;
            planetProgressDots[i].preserveAspect = true;

            Sprite sprite = GetSituationPlanetSprite(i);
            if (sprite != null)
                planetProgressDots[i].sprite = sprite;

            RectTransform dotRect = planetProgressDots[i].rectTransform;
            dotRect.anchoredPosition = new Vector2(startX + i * stepX, 0f);
        }

        UpdateSituationPanel(0);
    }

    private void SetProgressLineRect(RectTransform rect, float startX, float endX)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((startX + endX) * 0.5f, 0f);
        rect.sizeDelta = new Vector2(
            Mathf.Max(0f, endX - startX),
            progressLineVisualHeight);
    }

    private void UpdateCompletedProgressLine(int planetIndex)
    {
        if (completedProgressImage == null || planetProgressDots == null || planetProgressDots.Length == 0)
            return;

        int maxIdx = planetProgressDots.Length - 1;
        float startX = planetProgressDots[0].rectTransform.anchoredPosition.x;
        float endX = planetProgressDots[Mathf.Clamp(planetIndex, 0, maxIdx)]
            .rectTransform.anchoredPosition.x;

        SetProgressLineRect(completedProgressImage.rectTransform, startX, endX);
    }

    private void UpdateRealtimePlanetProgress(int planetIndex, float progress)
    {
        if (completedProgressImage == null || planetProgressDots == null || planetProgressDots.Length == 0)
            return;

        int maxIdx = planetProgressDots.Length - 1;
        int currentIndex = Mathf.Clamp(planetIndex, 0, maxIdx);
        int nextIndex = Mathf.Min(currentIndex + 1, maxIdx);
        float startX = planetProgressDots[0].rectTransform.anchoredPosition.x;
        float currentX = planetProgressDots[currentIndex].rectTransform.anchoredPosition.x;
        float nextX = planetProgressDots[nextIndex].rectTransform.anchoredPosition.x;
        float endX = Mathf.Lerp(currentX, nextX, Mathf.Clamp01(progress));
        SetProgressLineRect(completedProgressImage.rectTransform, startX, endX);
    }

    private void ResolvePlanetPanelReferences()
    {
        RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (leftPlanetPanel == null && rectTransform.gameObject.name == "Left Panel")
                leftPlanetPanel = rectTransform.gameObject;
            else if (rightPlanetPanel == null && rectTransform.gameObject.name == "Right Panel")
                rightPlanetPanel = rectTransform.gameObject;
        }

        ResolvePanelContent(
            leftPlanetPanel,
            ref leftPlanetNameText,
            ref leftPlanetDescriptionText,
            ref leftPlanetImage);
        ResolvePanelContent(
            rightPlanetPanel,
            ref rightPlanetNameText,
            ref rightPlanetDescriptionText,
            ref rightPlanetImage);
    }

    private static void ResolvePanelContent(
        GameObject panel,
        ref RTLTextMeshPro nameText,
        ref RTLTextMeshPro descriptionText,
        ref UnityEngine.UI.Image planetImage)
    {
        if (panel == null)
            return;

        RTLTextMeshPro[] texts = panel.GetComponentsInChildren<RTLTextMeshPro>(true);
        foreach (RTLTextMeshPro text in texts)
        {
            string objectName = text.gameObject.name;
            if (nameText == null && objectName.Contains("Name"))
            {
                nameText = text;
                nameText.horizontalAlignment = HorizontalAlignmentOptions.Justified;
            }
            else if (descriptionText == null && objectName.Contains("Desc"))
            {
                descriptionText = text;
                descriptionText.horizontalAlignment = HorizontalAlignmentOptions.Justified;
            }
        }

        UnityEngine.UI.Image[] images = panel.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (UnityEngine.UI.Image image in images)
        {
            if (image.gameObject.name == "Planet Image")
            {
                planetImage = image;
                return;
            }
        }
    }

    private void HidePlanetIntroduction()
    {
        FadePlanetPanel(leftPlanetCanvasGroup, false);
        FadePlanetPanel(rightPlanetCanvasGroup, false);
    }

    private void HandleGameWon()
    {
        if (activePlanetPulseCoroutine != null)
        {
            StopCoroutine(activePlanetPulseCoroutine);
            activePlanetPulseCoroutine = null;
        }

        FadeOut(gameCanvasGroup);

        FadePlanetPanel(leftPlanetCanvasGroup, false);
        FadePlanetPanel(rightPlanetCanvasGroup, false);

        if (leftPlanetCanvasGroup == null &&
            rightPlanetCanvasGroup == null &&
            planetPhasePanel != null)
            planetPhasePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(true);
    }

    private void FadeIn(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.gameObject.SetActive(true);
        StartFade(canvasGroup, canvasGroup.alpha, 1f);
    }

    private void FadeOut(CanvasGroup canvasGroup)
    {
        if (canvasGroup != null)
            StartFade(canvasGroup, canvasGroup.alpha, 0f);
    }

    private void StartFade(CanvasGroup canvasGroup, float from, float to)
    {
        if (fadeCoroutines.TryGetValue(canvasGroup, out Coroutine running))
            StopCoroutine(running);

        fadeCoroutines[canvasGroup] = StartCoroutine(FadeCanvasGroup(canvasGroup, from, to));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to)
    {
        canvasGroup.alpha = from;
        canvasGroup.interactable = to > from;
        canvasGroup.blocksRaycasts = to > from;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.interactable = to > 0f;
        canvasGroup.blocksRaycasts = to > 0f;
        fadeCoroutines.Remove(canvasGroup);
    }

    private void EnsurePlanetSequenceUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        if (leftPlanetPanel != null && rightPlanetPanel != null)
        {
            planetPhasePanel = rightPlanetPanel;
        }
        else if (planetPhasePanel == null)
            CreatePlanetPhasePanel(canvas.transform);

        if (winPanel == null)
            CreateWinPanel(canvas.transform);
    }

    private void SelectPlanetPanel(bool planetOnLeft)
    {
        GameObject selectedPanel = planetOnLeft
            ? rightPlanetPanel
            : leftPlanetPanel;

        if (selectedPanel == null)
            selectedPanel = planetPhasePanel;

        FadePlanetPanel(
            leftPlanetCanvasGroup,
            selectedPanel == leftPlanetPanel);
        FadePlanetPanel(
            rightPlanetCanvasGroup,
            selectedPanel == rightPlanetPanel);

        planetPhasePanel = selectedPanel;
        if (planetPhasePanel == null)
            return;

        Transform contentParent = planetTitleText != null
            ? planetTitleText.transform.parent
            : null;
        if (contentParent != null && contentParent != planetPhasePanel.transform)
        {
            planetTitleText.transform.SetParent(planetPhasePanel.transform, false);
            planetDescriptionText.transform.SetParent(planetPhasePanel.transform, false);
            planetTimerText.transform.SetParent(planetPhasePanel.transform, false);
        }

        if (leftPlanetCanvasGroup == null && rightPlanetCanvasGroup == null)
            planetPhasePanel.SetActive(true);
    }

    private void InitializePlanetPanelFades()
    {
        leftPlanetCanvasGroup = GetOrAddCanvasGroup(leftPlanetPanel);
        rightPlanetCanvasGroup = GetOrAddCanvasGroup(rightPlanetPanel);
        situationCanvasGroup = GetOrAddCanvasGroup(situationPanel);

        InitializeHiddenCanvasGroup(leftPlanetCanvasGroup);
        InitializeHiddenCanvasGroup(rightPlanetCanvasGroup);
        InitializeHiddenCanvasGroup(situationCanvasGroup);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject panel)
    {
        if (panel == null)
            return null;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private static void InitializeHiddenCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void FadePlanetPanel(CanvasGroup canvasGroup, bool show)
    {
        if (canvasGroup == null)
            return;

        if (show)
            FadeIn(canvasGroup);
        else
            FadeOut(canvasGroup);
    }

    private void CreatePlanetPhasePanel(Transform canvasTransform)
    {
        planetPhasePanel = CreatePanel(
            "Planet Phase Panel",
            canvasTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -35f),
            new Vector2(840f, 230f),
            new Color(0.025f, 0.055f, 0.1f, 0.88f));

        Transform panel = planetPhasePanel.transform;
        // planetTitleText = CreateText("Planet Title", panel, 40f, FontStyles.Bold,
        //     new Vector2(0f, -22f), new Vector2(700f, 55f));
        // planetDescriptionText = CreateText("Planet Description", panel, 25f, FontStyles.Normal,
        //     new Vector2(0f, -92f), new Vector2(710f, 90f));
        // planetTimerText = CreateText("Phase Timer", panel, 42f, FontStyles.Bold,
        //     new Vector2(350f, -22f), new Vector2(105f, 55f));
        // planetDescriptionText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateWinPanel(Transform canvasTransform)
    {
        winPanel = CreatePanel(
            "Win Panel",
            canvasTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0.01f, 0.02f, 0.05f, 0.35f));

        RTLTextMeshPro title = CreateRTLText("Win Title", winPanel.transform, 56f, FontStyles.Bold,
            new Vector2(0f, -380f), new Vector2(1200f, 90f));
        title.color = new Color(1f, 0.88f, 0.25f, 1f);
        title.text = "ماموریت با موفقیت انجام شد";

        RTLTextMeshPro message = CreateRTLText("Win Message", winPanel.transform, 32f, FontStyles.Normal,
            new Vector2(0f, -490f), new Vector2(1300f, 80f));
        message.color = Color.white;
        message.text = "شما با موفقیت منظومه شمسی را پشت سر گذاشته و به خورشید رسیدید!";
    }

    private RTLTextMeshPro CreateRTLText(
        string objectName,
        Transform parent,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(RTLTextMeshPro));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        RTLTextMeshPro text = textObject.GetComponent<RTLTextMeshPro>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.Farsi = true;
        text.PreserveNumbers = false;

        TMP_FontAsset persianFont = null;
        Material persianMaterial = null;
        if (leftPlanetNameText != null)
        {
            persianFont = leftPlanetNameText.font;
            persianMaterial = leftPlanetNameText.fontSharedMaterial;
        }

        if (persianFont != null)
        {
            text.font = persianFont;
            if (persianMaterial != null)
                text.fontSharedMaterial = persianMaterial;
        }

        return text;
    }

    private static GameObject CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject panel = new(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.layer = 5;
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        UnityEngine.UI.Image image = panel.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}

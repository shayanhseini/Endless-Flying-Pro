using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [Header("Warning")]
    [SerializeField] private UnityEngine.UI.Image targetImage;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float flashSpeed = 0.15f;

    [Header("Planet Sequence UI")]
    [SerializeField] private GameObject leftPlanetPanel;
    [SerializeField] private GameObject rightPlanetPanel;
    [SerializeField] private GameObject planetPhasePanel;
    [SerializeField] private TMP_Text planetTitleText;
    [SerializeField] private TMP_Text planetDescriptionText;
    [SerializeField] private TMP_Text planetTimerText;
    [SerializeField] private GameObject winPanel;

    private CanvasGroup leftPlanetCanvasGroup;
    private CanvasGroup rightPlanetCanvasGroup;

    private readonly Dictionary<CanvasGroup, Coroutine> fadeCoroutines = new();

    private void Awake()
    {
        EnsurePlanetSequenceUI();
        InitializePlanetPanelFades();

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
        GameEvents.LaunchMissile += StartWarning;
        GameEvents.PlanetIntroductionStarted += ShowPlanetIntroduction;
        GameEvents.PlanetDodgeStarted += ShowDodgePhase;
        GameEvents.PlanetPhaseTimeChanged += UpdatePlanetTimer;
        GameEvents.GameWon += HandleGameWon;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= HandleGameStarted;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.LaunchMissile -= StartWarning;
        GameEvents.PlanetIntroductionStarted -= ShowPlanetIntroduction;
        GameEvents.PlanetDodgeStarted -= ShowDodgePhase;
        GameEvents.PlanetPhaseTimeChanged -= UpdatePlanetTimer;
        GameEvents.GameWon -= HandleGameWon;
    }

    private void HandleGameStarted()
    {
        FadeIn(gameCanvasGroup);
    }

    private void HandleGameOver()
    {
        FadeOut(gameCanvasGroup);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        FadeIn(gameOverCanvasGroup);
    }

    private void ShowPlanetIntroduction(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft)
    {
        SelectPlanetPanel(planetOnLeft);

        if (planetTitleText != null)
            planetTitleText.text = planetName;

        if (planetDescriptionText != null)
            planetDescriptionText.text = description;

        UpdatePlanetTimer(duration);
    }

    private void ShowDodgePhase(
        string planetName,
        float duration,
        bool planetOnLeft)
    {
        SelectPlanetPanel(planetOnLeft);

        if (planetTitleText != null)
            planetTitleText.text = $"ASTEROID FIELD — {planetName}";

        if (planetDescriptionText != null)
            planetDescriptionText.text = "Dodge the asteroids until the timer reaches zero.";

        UpdatePlanetTimer(duration);
    }

    private void UpdatePlanetTimer(float remainingTime)
    {
        if (planetTimerText != null)
            planetTimerText.text = Mathf.CeilToInt(remainingTime).ToString("00");
    }

    private void HandleGameWon()
    {
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

    public void StartWarning()
    {
        if (targetImage == null)
            return;

        StopAllCoroutines();
        StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        targetImage.color = Color.white;

        float elapsed = 0f;
        bool isRed = false;
        while (elapsed < duration)
        {
            isRed = !isRed;
            targetImage.color = isRed ? Color.red : Color.white;
            yield return new WaitForSeconds(flashSpeed);
            elapsed += flashSpeed;
        }

        targetImage.color = Color.white;
    }

    private void EnsurePlanetSequenceUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        if (leftPlanetPanel != null && rightPlanetPanel != null)
        {
            planetPhasePanel = rightPlanetPanel;
            CreatePlanetTextContent(rightPlanetPanel.transform);
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

        InitializeHiddenCanvasGroup(leftPlanetCanvasGroup);
        InitializeHiddenCanvasGroup(rightPlanetCanvasGroup);
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

    private void CreatePlanetTextContent(Transform panel)
    {
        if (planetTitleText != null &&
            planetDescriptionText != null &&
            planetTimerText != null)
            return;

        planetTitleText = CreateText("Planet Title", panel, 42f, FontStyles.Bold,
            new Vector2(0f, -90f), new Vector2(440f, 70f));
        planetDescriptionText = CreateText("Planet Description", panel, 25f, FontStyles.Normal,
            new Vector2(0f, -190f), new Vector2(430f, 210f));
        planetTimerText = CreateText("Phase Timer", panel, 52f, FontStyles.Bold,
            new Vector2(0f, -430f), new Vector2(180f, 75f));
        planetDescriptionText.alignment = TextAlignmentOptions.Center;
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
        planetTitleText = CreateText("Planet Title", panel, 40f, FontStyles.Bold,
            new Vector2(0f, -22f), new Vector2(700f, 55f));
        planetDescriptionText = CreateText("Planet Description", panel, 25f, FontStyles.Normal,
            new Vector2(0f, -92f), new Vector2(710f, 90f));
        planetTimerText = CreateText("Phase Timer", panel, 42f, FontStyles.Bold,
            new Vector2(350f, -22f), new Vector2(105f, 55f));
        planetDescriptionText.alignment = TextAlignmentOptions.Center;
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
            new Color(0.01f, 0.025f, 0.06f, 0.94f));

        TMP_Text title = CreateText("Win Title", winPanel.transform, 72f, FontStyles.Bold,
            new Vector2(0f, -390f), new Vector2(1000f, 100f));
        title.text = "MISSION COMPLETE";

        TMP_Text message = CreateText("Win Message", winPanel.transform, 34f, FontStyles.Normal,
            new Vector2(0f, -510f), new Vector2(1100f, 90f));
        message.text = "You completed all 8 planets of the Solar System.";
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

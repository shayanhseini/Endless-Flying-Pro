using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gamePanel;


    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup startCanvasGroup;
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private CanvasGroup winCanvasGroup;


    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;


    [Header("Score")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text finalCoinText;
    [SerializeField] private TMP_Text finalCoinWinText;
    [SerializeField] private TMP_Text totalCoinText;
    [SerializeField] private TMP_Text highestScoreText;


    [Header("Flight Info")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text distanceText;

    
    [Header("Shield")]
    [SerializeField] private TMP_Text shieldText;
    [SerializeField] private Image shieldImage;

    [SerializeField] private string shieldActiveText = "SHIELD: ON";
    [SerializeField] private string shieldInactiveText = "SHIELD: OFF";

    [SerializeField] private Color shieldActiveTextColor = Color.cyan;
    [SerializeField] private Color shieldInactiveTextColor = Color.white;

    [SerializeField, Range(0f, 1f)]
    private float shieldInactiveAlpha = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float shieldActiveAlpha = 1f;
    
    [Header("Warning")]
    [SerializeField] private Image targetImage;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float flashSpeed = 0.15f;
    
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private WorldMovementManager worldMovement;
    [SerializeField] private Transform player;

    [SerializeField] private CameraTransitionController cameraTransition;
    [SerializeField] private Button startButton;


    private readonly Dictionary<CanvasGroup, Coroutine> fadeCoroutines =
        new Dictionary<CanvasGroup, Coroutine>();


    private void Awake()
    {
        ShowStartUI();

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        if (winPanel != null)
            winPanel.SetActive(false);
        
        UpdateCoin(0);
        UpdateFinalScore(0);

        UpdateShieldText(false);
    }


    private void OnEnable()
    {
        GameEvents.GameStarted += HandleGameStarted;
        
        GameEvents.GameOver += HandleGameOver;
        GameEvents.Victory += HandleWin;
        
        GameEvents.ScoreChanged += UpdateCoin;

        GameEvents.ShieldActivated += HandleShieldActivated;
        GameEvents.ShieldDeactivated += HandleShieldDeactivated;

        GameEvents.LaunchMissile += StartWarning;
        
        HandleLoadGame();
    }


    private void OnDisable()
    {
        GameEvents.GameStarted -= HandleGameStarted;
        
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.Victory -= HandleWin;
        
        
        GameEvents.ScoreChanged -= UpdateCoin;

        GameEvents.ShieldActivated -= HandleShieldActivated;
        GameEvents.ShieldDeactivated -= HandleShieldDeactivated;
        
        GameEvents.LaunchMissile -= StartWarning;
    }
    private void Update()
    {
        if (gameManager == null)
            return;


        if (gameManager.State != GameState.Playing)
            return;


        UpdateFlightInfo();
    }
    
    // ==================================================
    // Game State
    // ==================================================

    private void HandleLoadGame()
    {
                
        int totalCoin =
            saveManager.GetTotalCoin();

        totalCoinText.text = totalCoin.ToString("D4");
        
        int highestScore =
            (int)saveManager.GetHighestScore();
        highestScoreText.text = highestScore.ToString("D4");
    }
    
    private void HandleGameStarted()
    {
        FadeOut(startCanvasGroup);
        FadeIn(gameCanvasGroup);
    }


    private void HandleGameOver()
    {
        HidePanel(startPanel);

        FadeOut(gameCanvasGroup);
        
        ShowPanel(gameOverPanel);
        FadeIn(gameOverCanvasGroup);

        UpdateFinalScore(gameManager.Distance);
    }

    private void HandleWin()
    {
        HidePanel(startPanel);

        FadeOut(gameCanvasGroup);
        
        ShowPanel(winPanel);
        FadeIn(winCanvasGroup);

        UpdateFinalScore(gameManager.Distance);
    }
    
    // ==================================================
    // Score
    // ==================================================

    private void UpdateCoin(int score)
    {
        if (coinText == null)
            return;


        coinText.text =
            score.ToString("D4");
    }


    private void UpdateFinalScore(float score)
    {
        if (finalCoinText == null ||  finalCoinText.text == null)
            return;

        int scoreInt = (int)score;

        finalCoinText.text =
            scoreInt.ToString("D4");
        finalCoinWinText.text = scoreInt.ToString("D4");
    }


    // ==================================================
    // Flight Info
    // ==================================================

    private void UpdateFlightInfo()
    {
        UpdateSpeed();
        UpdateDistance();
    }


    private void UpdateSpeed()
    {
        if (speedText == null ||
            worldMovement == null)
            return;


        float displayedSpeed =
            worldMovement.RealSpeed;

        // only when the shown number changes: setting .text rebuilds the text mesh (and the
        // Persian RTL shaping) and allocates a string, which happened every frame before
        long shown = (long)System.Math.Round(displayedSpeed, System.MidpointRounding.AwayFromZero);
        if (shown == lastShownSpeed)
            return;
        lastShownSpeed = shown;

        speedText.text =
            $"{displayedSpeed:000}";
    }


    private void UpdateDistance()
    {
        if (distanceText == null ||
            gameManager == null)
            return;

        long shown = (long)System.Math.Round(gameManager.Distance, System.MidpointRounding.AwayFromZero);
        if (shown == lastShownDistance)
            return;
        lastShownDistance = shown;

        distanceText.text =
            $"{gameManager.Distance:000}";
    }

    private long lastShownSpeed = long.MinValue;
    private long lastShownDistance = long.MinValue;


    // ==================================================
    // Shield
    // ==================================================

    private void HandleShieldActivated()
    {
        UpdateShieldText(true);
    }


    private void HandleShieldDeactivated()
    {
        UpdateShieldText(false);
    }


    private void UpdateShieldText(bool active)
    {
        // ------------------------------
        // Text
        // ------------------------------

        if (shieldText != null)
        {
            shieldText.text =
                active
                    ? shieldActiveText
                    : shieldInactiveText;


            shieldText.color =
                active
                    ? shieldActiveTextColor
                    : shieldInactiveTextColor;
        }


        // ------------------------------
        // Image Alpha
        // ------------------------------

        if (shieldImage != null)
        {
            Color color =
                shieldImage.color;


            color.a =
                active
                    ? shieldActiveAlpha
                    : shieldInactiveAlpha;


            shieldImage.color = color;
        }
    }


    // ==================================================
    // Start Panel
    // ==================================================

    private void ShowStartUI()
    {
        if (startPanel != null)
            startPanel.SetActive(true);


        if (startCanvasGroup != null)
        {
            startCanvasGroup.alpha = 1f;
            startCanvasGroup.interactable = true;
            startCanvasGroup.blocksRaycasts = true;
        }
    }


    // ==================================================
    // Buttons
    // ==================================================

    public void StartGame()
    {
        if (startButton != null)
            startButton.interactable = false;


        if (cameraTransition != null)
            cameraTransition.StartGameTransition();
    }
    
    // ==================================================
    // Fade
    // ==================================================

    private void FadeIn(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;


        canvasGroup.gameObject.SetActive(true);


        StartFade(
            canvasGroup,
            canvasGroup.alpha,
            1f
        );
    }


    private void FadeOut(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;


        StartFade(
            canvasGroup,
            canvasGroup.alpha,
            0f
        );
    }


    private void StartFade(
        CanvasGroup canvasGroup,
        float from,
        float to)
    {
        if (fadeCoroutines.TryGetValue(
                canvasGroup,
                out Coroutine existingCoroutine))
        {
            StopCoroutine(existingCoroutine);

            fadeCoroutines.Remove(canvasGroup);
        }


        Coroutine newCoroutine =
            StartCoroutine(
                FadeCanvasGroup(
                    canvasGroup,
                    from,
                    to
                )
            );


        fadeCoroutines[canvasGroup] =
            newCoroutine;
    }


    private IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float from,
        float to)
    {
        canvasGroup.alpha = from;


        if (to < from)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            canvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );


            yield return null;
        }


        canvasGroup.alpha = to;


        if (to <= 0f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }


        fadeCoroutines.Remove(canvasGroup);
    }


    // ==================================================
    // Panel Visibility
    // ==================================================

    private void HidePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }


    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }
    
    // ==================================================
    // Warning
    // ==================================================
    
    public void StartWarning()
    {
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
}
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
    [SerializeField] private GameObject gamePanel;


    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup startCanvasGroup;
    [SerializeField] private CanvasGroup gameCanvasGroup;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;


    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;


    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;


    [Header("Flight Info")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text altitudeText;

    [SerializeField] private float speedDisplayMultiplier = 20f;
    [SerializeField] private float altitudeDisplayMultiplier = 10f;


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


        UpdateScore(0);
        UpdateFinalScore(0);

        UpdateShieldText(false);
    }


    private void OnEnable()
    {
        GameEvents.GameStarted += HandleGameStarted;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.ScoreChanged += UpdateScore;

        GameEvents.ShieldActivated += HandleShieldActivated;
        GameEvents.ShieldDeactivated += HandleShieldDeactivated;

        GameEvents.LaunchMissile += StartWarning;
        
        HandleLoadGame();
    }


    private void OnDisable()
    {
        GameEvents.GameStarted -= HandleGameStarted;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.ScoreChanged -= UpdateScore;

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
                
        int highScore =
            saveManager.GetHighScore();

        highScoreText.text = highScore.ToString("D4");
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

        UpdateFinalScore(gameManager.Score);
    }
    
    // ==================================================
    // Score
    // ==================================================

    private void UpdateScore(int score)
    {
        if (scoreText == null)
            return;


        scoreText.text =
            score.ToString("D4");
    }


    private void UpdateFinalScore(int score)
    {
        if (finalScoreText == null)
            return;


        finalScoreText.text =
            score.ToString("D4");
    }


    // ==================================================
    // Flight Info
    // ==================================================

    private void UpdateFlightInfo()
    {
        UpdateSpeed();
        UpdateAltitude();
    }


    private void UpdateSpeed()
    {
        if (speedText == null ||
            worldMovement == null)
            return;


        float displayedSpeed =
            worldMovement.WorldSpeed *
            speedDisplayMultiplier;


        speedText.text =
            $"{displayedSpeed:0}";
    }


    private void UpdateAltitude()
    {
        if (altitudeText == null ||
            player == null)
            return;


        float displayedAltitude =
            (player.position.y *
             altitudeDisplayMultiplier) +
            50f;


        altitudeText.text =
            $"{displayedAltitude:0}";
    }


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
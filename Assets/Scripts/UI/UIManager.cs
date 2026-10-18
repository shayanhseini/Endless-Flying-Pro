using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly Dictionary<CanvasGroup, Coroutine> fadeCoroutines = new();

    private void Awake()
    {
        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.GameStarted += HandleGameStarted;
        GameEvents.GameOver += HandleGameOver;
        GameEvents.LaunchMissile += StartWarning;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= HandleGameStarted;
        GameEvents.GameOver -= HandleGameOver;
        GameEvents.LaunchMissile -= StartWarning;
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
}

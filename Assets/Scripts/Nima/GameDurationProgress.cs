using UnityEngine;
using UnityEngine.UI;

public class GameDurationProgress : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Image progressBar;
    [SerializeField] private RectTransform movingImage;

    [Header("Extra Time")]
    [SerializeField] private float extraTime = 1f;

    [Header("X Position")]
    [SerializeField] private float startX = 2f;
    [SerializeField] private float endX = 900f;

    private float timer;
    private float totalDuration;

    private void Start()
    {
        totalDuration = gameManager.RemainingTime + extraTime;

        timer = 0f;

        progressBar.fillAmount = 0f;

        SetImagePosition(0f);
    }

    private void Update()
    {
        if (gameManager.State != GameState.Playing)
            return;

        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / totalDuration);

        progressBar.fillAmount = progress;

        SetImagePosition(progress);

        if (progress >= 1f)
        {
            enabled = false;
        }
    }

    private void SetImagePosition(float progress)
    {
        Vector2 position = movingImage.anchoredPosition;

        position.x = Mathf.Lerp(
            startX,
            endX,
            progress
        );

        movingImage.anchoredPosition = position;
    }
}
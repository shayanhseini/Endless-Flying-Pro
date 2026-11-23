using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyUIController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;

    [Header("Button Images")]
    [SerializeField] private Image easyImage;
    [SerializeField] private Image normalImage;
    [SerializeField] private Image hardImage;

    [Header("Sprites - Easy")]
    [SerializeField] private Sprite easyNormalSprite;
    [SerializeField] private Sprite easyActiveSprite;

    [Header("Sprites - Normal")]
    [SerializeField] private Sprite normalNormalSprite;
    [SerializeField] private Sprite normalActiveSprite;

    [Header("Sprites - Hard")]
    [SerializeField] private Sprite hardNormalSprite;
    [SerializeField] private Sprite hardActiveSprite;

    [Header("Indicator Arrow")]
    [SerializeField] private RectTransform indicatorArrow;
    [SerializeField] private float easyArrowX = -151f;
    [SerializeField] private float normalArrowX = 0f;
    [SerializeField] private float hardArrowX = 151f;
    [SerializeField] private float arrowY = -23f;
    [SerializeField] private bool animateIndicator = true;
    [SerializeField] private float indicatorSpeed = 20f;

    [Header("Legacy Support (Optional)")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TMP_Text difficultyLabel;

    private GameManager gameManager;
    private GameAudioController audioController;
    private int currentDifficulty = 1;
    private float targetArrowX = 0f;
    private bool isIndicatorMoving = false;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        audioController = FindFirstObjectByType<GameAudioController>();

        if (easyButton != null)
            easyButton.onClick.AddListener(() => OnDifficultyButtonClicked(0));

        if (normalButton != null)
            normalButton.onClick.AddListener(() => OnDifficultyButtonClicked(1));

        if (hardButton != null)
            hardButton.onClick.AddListener(() => OnDifficultyButtonClicked(2));

        if (difficultySlider != null)
            difficultySlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void Start()
    {
        InitializeDifficulty();
    }

    private void OnEnable()
    {
        InitializeDifficulty();
    }

    private void Update()
    {
        if (!isIndicatorMoving || indicatorArrow == null || !animateIndicator)
            return;

        Vector2 pos = indicatorArrow.anchoredPosition;
        if (Mathf.Abs(pos.x - targetArrowX) > 0.5f)
        {
            pos.x = Mathf.Lerp(pos.x, targetArrowX, Time.unscaledDeltaTime * indicatorSpeed);
            indicatorArrow.anchoredPosition = pos;
        }
        else
        {
            pos.x = targetArrowX;
            indicatorArrow.anchoredPosition = pos;
            isIndicatorMoving = false;
        }
    }

    public void InitializeDifficulty()
    {
        int level = (int)DifficultyManager.SelectedDifficulty;

        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.LoadSavedDifficulty();
            level = (int)DifficultyManager.Instance.CurrentDifficulty;
        }
        else if (PlayerPrefs.HasKey("GameDifficulty"))
        {
            level = PlayerPrefs.GetInt("GameDifficulty", 1);
        }

        currentDifficulty = Mathf.Clamp(level, 0, 2);

        if (difficultySlider != null)
        {
            difficultySlider.minValue = 0;
            difficultySlider.maxValue = 2;
            difficultySlider.wholeNumbers = true;
            difficultySlider.SetValueWithoutNotify(currentDifficulty);
        }

        UpdateVisuals(currentDifficulty, immediate: true);
    }

    public void InitializeSlider()
    {
        InitializeDifficulty();
    }

    public void OnDifficultyButtonClicked(int level)
    {
        if (currentDifficulty == level) return;

        SetDifficulty(level);

        if (audioController == null)
            audioController = FindFirstObjectByType<GameAudioController>();

        if (audioController != null)
        {
            audioController.PlayButtonSound();
        }
    }

    public void OnSliderValueChanged(float value)
    {
        int level = Mathf.RoundToInt(value);
        if (level != currentDifficulty)
        {
            SetDifficulty(level);

            GameAudioController audio = FindFirstObjectByType<GameAudioController>();
            if (audio != null)
            {
                audio.PlayButtonSound();
            }
        }
    }

    public void SetDifficulty(int level)
    {
        currentDifficulty = Mathf.Clamp(level, 0, 2);

        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.SetDifficulty(currentDifficulty);
        }
        else
        {
            PlayerPrefs.SetInt("GameDifficulty", currentDifficulty);
            PlayerPrefs.Save();
        }

        if (gameManager != null)
        {
            gameManager.ResetWaitingTimer();
        }

        if (difficultySlider != null && Mathf.RoundToInt(difficultySlider.value) != currentDifficulty)
        {
            difficultySlider.SetValueWithoutNotify(currentDifficulty);
        }

        UpdateVisuals(currentDifficulty, immediate: false);
    }

    private void UpdateVisuals(int level, bool immediate)
    {
        // 1. Easy Button
        if (easyImage != null)
        {
            easyImage.sprite = (level == 0) ? easyActiveSprite : easyNormalSprite;
        }

        // 2. Normal Button
        if (normalImage != null)
        {
            normalImage.sprite = (level == 1) ? normalActiveSprite : normalNormalSprite;
        }

        // 3. Hard Button
        if (hardImage != null)
        {
            hardImage.sprite = (level == 2) ? hardActiveSprite : hardNormalSprite;
        }

        // 4. Indicator Arrow
        switch (level)
        {
            case 0:
                targetArrowX = easyArrowX;
                break;
            case 2:
                targetArrowX = hardArrowX;
                break;
            case 1:
            default:
                targetArrowX = normalArrowX;
                break;
        }

        if (indicatorArrow != null)
        {
            if (immediate || !animateIndicator)
            {
                indicatorArrow.anchoredPosition = new Vector2(targetArrowX, arrowY);
                isIndicatorMoving = false;
            }
            else
            {
                isIndicatorMoving = true;
            }
        }

        // 5. Optional Legacy Label
        if (difficultyLabel != null)
        {
            switch (level)
            {
                case 0:
                    difficultyLabel.text = "DIFFICULTY: EASY";
                    difficultyLabel.color = new Color(0.2f, 0.95f, 0.95f, 1f);
                    break;
                case 2:
                    difficultyLabel.text = "DIFFICULTY: HARD";
                    difficultyLabel.color = new Color(1f, 0.3f, 0.3f, 1f);
                    break;
                case 1:
                default:
                    difficultyLabel.text = "DIFFICULTY: NORMAL";
                    difficultyLabel.color = new Color(1f, 0.85f, 0.2f, 1f);
                    break;
            }
        }
    }
}

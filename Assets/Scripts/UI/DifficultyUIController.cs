using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultyUIController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TMP_Text difficultyLabel;

    [Header("Difficulty Texts")]
    [SerializeField] private string easyText = "DIFFICULTY: EASY";
    [SerializeField] private string normalText = "DIFFICULTY: NORMAL";
    [SerializeField] private string hardText = "DIFFICULTY: HARD";

    [Header("Difficulty Colors")]
    [SerializeField] private Color easyColor = new Color(0.2f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color hardColor = new Color(1f, 0.3f, 0.3f, 1f);

    private GameManager gameManager;

    private void Awake()
    {
        if (difficultySlider == null)
            difficultySlider = GetComponentInChildren<Slider>();

        if (difficultyLabel == null)
            difficultyLabel = GetComponentInChildren<TMP_Text>();

        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        InitializeSlider();
    }

    private void OnEnable()
    {
        if (difficultySlider != null)
        {
            difficultySlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            difficultySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        InitializeSlider();
    }

    private void OnDisable()
    {
        if (difficultySlider != null)
        {
            difficultySlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    public void InitializeSlider()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.LoadSavedDifficulty();
            int current = (int)DifficultyManager.Instance.CurrentDifficulty;
            if (difficultySlider != null)
            {
                difficultySlider.minValue = 0;
                difficultySlider.maxValue = 2;
                difficultySlider.wholeNumbers = true;
                difficultySlider.SetValueWithoutNotify(current);
            }
            UpdateLabel(current);
        }
        else
        {
            int saved = PlayerPrefs.GetInt("GameDifficulty", 1);
            if (difficultySlider != null)
            {
                difficultySlider.minValue = 0;
                difficultySlider.maxValue = 2;
                difficultySlider.wholeNumbers = true;
                difficultySlider.SetValueWithoutNotify(saved);
            }
            UpdateLabel(saved);
        }
    }

    public void OnSliderValueChanged(float value)
    {
        int level = Mathf.RoundToInt(value);

        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.SetDifficulty(level);
        }
        else
        {
            PlayerPrefs.SetInt("GameDifficulty", level);
            PlayerPrefs.Save();
        }

        if (gameManager != null)
        {
            gameManager.ResetWaitingTimer();
        }

        GameAudioController audio = FindFirstObjectByType<GameAudioController>();
        if (audio != null)
        {
            audio.PlayButtonSound();
        }

        UpdateLabel(level);
    }

    private void UpdateLabel(int level)
    {
        if (difficultyLabel == null) return;

        switch (level)
        {
            case 0:
                difficultyLabel.text = easyText;
                difficultyLabel.color = easyColor;
                break;
            case 2:
                difficultyLabel.text = hardText;
                difficultyLabel.color = hardColor;
                break;
            case 1:
            default:
                difficultyLabel.text = normalText;
                difficultyLabel.color = normalColor;
                break;
        }
    }
}

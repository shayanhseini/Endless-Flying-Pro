using UnityEngine;

public enum GameDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Difficulty Setting")]
    [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Normal;

    [Header("Score")]
    [SerializeField] private int maxScore = 500;

    [Header("Coins")]
    [SerializeField] private int startCoinCount = 10;
    [SerializeField] private int maxCoinCount = 20;

    [Header("Obstacles")]
    [SerializeField] private int startObstacleCount = 10;
    [SerializeField] private int maxObstacleCount = 26;

    [Header("Shield")]
    [SerializeField] private bool startWithShield = true;
    [SerializeField] private int shieldDisableScore = 100;

    [Header("Speed Power-Up")]
    [SerializeField] private bool startWithSpeedPowerUp = true;
    [SerializeField] private int speedPowerUpEnableScore = 150;

    [Header("Air Defense Spawn Probability")]
    [SerializeField, Range(0f, 1f)] private float easyDefenseChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float normalDefenseChance = 0.70f;
    [SerializeField, Range(0f, 1f)] private float hardDefenseChance = 1.00f;

    [Header("Missile Tracking Accuracy (Turn Speed Multiplier)")]
    [SerializeField] private Vector2 easyTurnSpeedRange = new Vector2(0.40f, 0.60f);
    [SerializeField] private Vector2 normalTurnSpeedRange = new Vector2(0.70f, 0.85f);
    [SerializeField] private Vector2 hardTurnSpeedRange = new Vector2(0.95f, 1.20f);

    [Header("Missile Prediction Time Multiplier")]
    [SerializeField] private Vector2 easyPredictionRange = new Vector2(0.20f, 0.40f);
    [SerializeField] private Vector2 normalPredictionRange = new Vector2(0.65f, 0.85f);
    [SerializeField] private Vector2 hardPredictionRange = new Vector2(0.95f, 1.15f);

    public GameDifficulty CurrentDifficulty => currentDifficulty;

    public int CoinCount { get; private set; }
    public int ObstacleCount { get; private set; }
    public bool HasShield { get; private set; }
    public bool HasSpeedPowerUp { get; private set; }

    private int currentScore = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadSavedDifficulty();
        UpdateDifficulty(0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
    }

    public void LoadSavedDifficulty()
    {
        int saved = PlayerPrefs.GetInt("GameDifficulty", (int)GameDifficulty.Normal);
        currentDifficulty = (GameDifficulty)Mathf.Clamp(saved, 0, 2);
    }

    public void SetDifficulty(int level)
    {
        currentDifficulty = (GameDifficulty)Mathf.Clamp(level, 0, 2);
        PlayerPrefs.SetInt("GameDifficulty", (int)currentDifficulty);
        PlayerPrefs.Save();
        UpdateDifficulty(currentScore);
    }

    public void SetDifficulty(float sliderValue)
    {
        SetDifficulty(Mathf.RoundToInt(sliderValue));
    }

    private void OnScoreChanged(int score)
    {
        currentScore = score;
        UpdateDifficulty(score);
    }

    private void UpdateDifficulty(int score)
    {
        float progress =
            Mathf.Clamp01(
                (float)score / maxScore
            );

        float obstacleMultiplier = 1f;
        int shieldBonusScore = 0;

        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                obstacleMultiplier = 0.75f;
                shieldBonusScore = 50;
                break;
            case GameDifficulty.Hard:
                obstacleMultiplier = 1.25f;
                shieldBonusScore = -30;
                break;
            case GameDifficulty.Normal:
            default:
                obstacleMultiplier = 1.0f;
                shieldBonusScore = 0;
                break;
        }

        // Coins
        CoinCount =
            Mathf.RoundToInt(
                Mathf.Lerp(
                    startCoinCount,
                    maxCoinCount,
                    progress
                )
            );

        // Obstacles
        ObstacleCount =
            Mathf.RoundToInt(
                Mathf.Lerp(
                    startObstacleCount * obstacleMultiplier,
                    maxObstacleCount * obstacleMultiplier,
                    progress
                )
            );

        // Shield
        HasShield =
            startWithShield &&
            score < (shieldDisableScore + shieldBonusScore);

        // Speed Power-Up
        HasSpeedPowerUp =
            startWithSpeedPowerUp &&
            score >= speedPowerUpEnableScore;
    }

    public bool RollAirDefenseActive()
    {
        float chance = normalDefenseChance;
        switch (currentDifficulty)
        {
            case GameDifficulty.Easy: chance = easyDefenseChance; break;
            case GameDifficulty.Normal: chance = normalDefenseChance; break;
            case GameDifficulty.Hard: chance = hardDefenseChance; break;
        }
        return Random.value <= chance;
    }

    public void GetAirDefenseAccuracyModifiers(out float turnSpeedMult, out float predMult)
    {
        Vector2 turnRange;
        Vector2 predRange;

        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                turnRange = easyTurnSpeedRange;
                predRange = easyPredictionRange;
                break;
            case GameDifficulty.Hard:
                turnRange = hardTurnSpeedRange;
                predRange = hardPredictionRange;
                break;
            case GameDifficulty.Normal:
            default:
                turnRange = normalTurnSpeedRange;
                predRange = normalPredictionRange;
                break;
        }

        turnSpeedMult = Random.Range(turnRange.x, turnRange.y);
        predMult = Random.Range(predRange.x, predRange.y);
    }

    public int GetMaxScore()
    {
        return maxScore;
    }
}

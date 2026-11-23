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

    public static GameDifficulty SelectedDifficulty { get; private set; } = GameDifficulty.Normal;
    public static bool HasUserSelectedDifficulty { get; private set; } = false;

    [Header("Difficulty Setting")]
    [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Normal;

    [Header("Score")]
    [SerializeField] private int maxScore = 500;

    [Header("Shield & Power-Up Defaults")]
    [SerializeField] private bool startWithShield = true;
    [SerializeField] private int shieldDisableScore = 100;
    [SerializeField] private bool startWithSpeedPowerUp = true;
    [SerializeField] private int speedPowerUpEnableScore = 150;

    [Header("Easy Settings")]
    [SerializeField] private int easyStartObstacles = 1;
    [SerializeField] private int easyMaxObstacles = 2;
    [SerializeField] private float easyBaseSpeed = 9.0f;
    [SerializeField] private float easyMaxSpeed = 10.5f;
    [SerializeField, Range(0f, 1f)] private float easyDefenseChance = 0.0f;
    [SerializeField] private Vector2 easyTurnSpeedRange = new Vector2(0.20f, 0.30f);
    [SerializeField] private Vector2 easyPredictionRange = new Vector2(0.10f, 0.20f);
    [SerializeField] private int easyShieldBonusScore = 150;

    [Header("Normal Settings")]
    [SerializeField] private int normalStartObstacles = 6;
    [SerializeField] private int normalMaxObstacles = 11;
    [SerializeField] private float normalBaseSpeed = 10.0f;
    [SerializeField] private float normalMaxSpeed = 12.0f;
    [SerializeField, Range(0f, 1f)] private float normalDefenseChance = 0.60f;
    [SerializeField] private Vector2 normalTurnSpeedRange = new Vector2(0.70f, 0.85f);
    [SerializeField] private Vector2 normalPredictionRange = new Vector2(0.65f, 0.85f);
    [SerializeField] private int normalShieldBonusScore = 0;

    [Header("Hard Settings")]
    [SerializeField] private int hardStartObstacles = 12;
    [SerializeField] private int hardMaxObstacles = 18;
    [SerializeField] private float hardBaseSpeed = 11.5f;
    [SerializeField] private float hardMaxSpeed = 13.5f;
    [SerializeField, Range(0f, 1f)] private float hardDefenseChance = 0.90f;
    [SerializeField] private Vector2 hardTurnSpeedRange = new Vector2(1.05f, 1.30f);
    [SerializeField] private Vector2 hardPredictionRange = new Vector2(1.00f, 1.25f);
    [SerializeField] private int hardShieldBonusScore = -50;

    public GameDifficulty CurrentDifficulty => currentDifficulty;

    public float EasyBaseSpeed => easyBaseSpeed;
    public float EasyMaxSpeed => easyMaxSpeed;
    public float NormalBaseSpeed => normalBaseSpeed;
    public float NormalMaxSpeed => normalMaxSpeed;
    public float HardBaseSpeed => hardBaseSpeed;
    public float HardMaxSpeed => hardMaxSpeed;

    public float CurrentProgress { get; private set; }

    public int CoinCount { get; private set; }
    public int ObstacleCount { get; private set; }
    public int ShieldCount { get; private set; }
    public bool HasShield => ShieldCount > 0;
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
        UpdateProgress();
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

    private bool difficultyLoaded = false;

    public void LoadSavedDifficulty()
    {
        if (HasUserSelectedDifficulty)
        {
            currentDifficulty = SelectedDifficulty;
        }
        else if (PlayerPrefs.HasKey("GameDifficulty"))
        {
            int saved = PlayerPrefs.GetInt("GameDifficulty", (int)GameDifficulty.Normal);
            SelectedDifficulty = (GameDifficulty)Mathf.Clamp(saved, 0, 2);
            currentDifficulty = SelectedDifficulty;
            HasUserSelectedDifficulty = true;
        }
        else
        {
            SelectedDifficulty = currentDifficulty;
        }
        difficultyLoaded = true;
    }

    public void SetDifficulty(int level)
    {
        currentDifficulty = (GameDifficulty)Mathf.Clamp(level, 0, 2);
        SelectedDifficulty = currentDifficulty;
        HasUserSelectedDifficulty = true;
        difficultyLoaded = true;
        PlayerPrefs.SetInt("GameDifficulty", (int)currentDifficulty);
        PlayerPrefs.Save();
        UpdateProgress();
    }

    public void SetDifficulty(float sliderValue)
    {
        SetDifficulty(Mathf.RoundToInt(sliderValue));
    }

    private void OnScoreChanged(int score)
    {
        currentScore = score;
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        if (!difficultyLoaded)
        {
            LoadSavedDifficulty();
        }
        float timeProgress = 0f;
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
        {
            float duration = GameManager.Instance.GameDuration > 0f ? GameManager.Instance.GameDuration : 65f;
            timeProgress = Mathf.Clamp01((duration - GameManager.Instance.RemainingTime) / duration);
        }

        float scoreProgress = Mathf.Clamp01((float)currentScore / Mathf.Max(1, maxScore));

        // Blended progression: time progress guarantees scaling over 60 seconds
        CurrentProgress = Mathf.Clamp01(Mathf.Max(timeProgress, scoreProgress * 0.8f));

        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                ObstacleCount = Mathf.RoundToInt(Mathf.Lerp(easyStartObstacles, easyMaxObstacles, CurrentProgress));
                CoinCount = Mathf.RoundToInt(Mathf.Lerp(12, 18, CurrentProgress));
                break;

            case GameDifficulty.Hard:
                ObstacleCount = Mathf.RoundToInt(Mathf.Lerp(hardStartObstacles, hardMaxObstacles, CurrentProgress));
                CoinCount = Mathf.RoundToInt(Mathf.Lerp(5, 10, CurrentProgress));
                break;

            case GameDifficulty.Normal:
            default:
                ObstacleCount = Mathf.RoundToInt(Mathf.Lerp(normalStartObstacles, normalMaxObstacles, CurrentProgress));
                CoinCount = Mathf.RoundToInt(Mathf.Lerp(8, 14, CurrentProgress));
                break;
        }

        HasSpeedPowerUp = startWithSpeedPowerUp && (currentScore >= speedPowerUpEnableScore || CurrentProgress >= 0.45f);
    }

    public int GetShieldCountForChunk()
    {
        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                // Abundant shield balloons on Easy (2 to 4 per chunk)
                return UnityEngine.Random.Range(2, 5);

            case GameDifficulty.Hard:
                // Rare shield balloons on Hard (30% chance for 1, otherwise 0)
                return UnityEngine.Random.value < 0.30f ? 1 : 0;

            case GameDifficulty.Normal:
            default:
                // Balanced: 1 shield balloon per chunk
                return 1;
        }
    }

    public float GetCurrentTargetBaseSpeed()
    {
        float startSpd;
        float maxSpd;

        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                startSpd = easyBaseSpeed;
                maxSpd = easyMaxSpeed;
                break;

            case GameDifficulty.Hard:
                startSpd = hardBaseSpeed;
                maxSpd = hardMaxSpeed;
                break;

            case GameDifficulty.Normal:
            default:
                startSpd = normalBaseSpeed;
                maxSpd = normalMaxSpeed;
                break;
        }

        return Mathf.Lerp(startSpd, maxSpd, CurrentProgress);
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

using UnityEngine;

public class WorldMovementManager : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float startSpeed = 10f;
    [SerializeField] private float speedAtMaxScore = 12f;
    [SerializeField] private float speedIncreaseAfterMaxScore = 0.02f;

    [Header("Real Speed")]
    [SerializeField] private float realSpeedMultiplier = 20f;

    [Header("Difficulty")]
    [SerializeField] private DifficultyManager difficultyManager;


    public float RealSpeed =>
        WorldSpeed * realSpeedMultiplier;

    private int _maxScore;


    public float BaseWorldSpeed { get; private set; }

    public float WorldSpeed { get; private set; }

    public float SpeedMultiplier { get; private set; } = 1f;

    public bool IsMovementEnabled { get; private set; }


    private void Awake()
    {
        _maxScore =
            difficultyManager != null ? difficultyManager.GetMaxScore() : 500;

        float initialSpeed =
            difficultyManager != null ? difficultyManager.GetCurrentTargetBaseSpeed() : startSpeed;

        BaseWorldSpeed =
            initialSpeed;

        WorldSpeed =
            initialSpeed;

        SpeedMultiplier =
            1f;

        IsMovementEnabled =
            false;
    }


    private float progressThrottleTimer = 0f;
    private const float PROGRESS_THROTTLE_INTERVAL = 0.25f;

    private void Update()
    {
        if (!IsMovementEnabled || difficultyManager == null)
            return;

        progressThrottleTimer += Time.deltaTime;
        if (progressThrottleTimer >= PROGRESS_THROTTLE_INTERVAL)
        {
            progressThrottleTimer = 0f;
            difficultyManager.UpdateProgress();
            BaseWorldSpeed = difficultyManager.GetCurrentTargetBaseSpeed();
            UpdateWorldSpeed();
        }
    }


    private void OnEnable()
    {
        GameEvents.GameStarted += StartMovement;
        
        GameEvents.GameOver += StopMovement;
        GameEvents.Victory += StopMovement;
        
        GameEvents.ScoreChanged += UpdateSpeed;
    }


    private void OnDisable()
    {
        GameEvents.GameStarted -= StartMovement;
        
        GameEvents.GameOver -= StopMovement;
        GameEvents.Victory -= StopMovement;
        
        GameEvents.ScoreChanged -= UpdateSpeed;
    }


    private void StartMovement()
    {
        IsMovementEnabled = true;
    }


    private void StopMovement()
    {
        IsMovementEnabled = false;
    }


    private void UpdateSpeed(int score)
    {
        if (difficultyManager != null)
        {
            difficultyManager.UpdateProgress();
            BaseWorldSpeed = difficultyManager.GetCurrentTargetBaseSpeed();
        }
        else
        {
            float newBaseSpeed;
            if (score <= _maxScore)
            {
                float progress =
                    Mathf.Clamp01(
                        (float)score / _maxScore
                    );

                newBaseSpeed =
                    Mathf.Lerp(
                        startSpeed,
                        speedAtMaxScore,
                        progress
                    );
            }
            else
            {
                int scoreAfterMax =
                    score - _maxScore;

                newBaseSpeed =
                    speedAtMaxScore +
                    scoreAfterMax *
                    speedIncreaseAfterMaxScore;
            }

            BaseWorldSpeed =
                newBaseSpeed;
        }

        UpdateWorldSpeed();
    }


    public void SetSpeedMultiplier(
        float multiplier)
    {
        SpeedMultiplier =
            Mathf.Max(
                0f,
                multiplier
            );


        UpdateWorldSpeed();
    }


    private void UpdateWorldSpeed()
    {
        float targetSpeed =
            BaseWorldSpeed *
            SpeedMultiplier;


        if (targetSpeed >= WorldSpeed)
        {
            WorldSpeed = targetSpeed;
        }
        else
        {
            if (SpeedMultiplier <= 1f)
            {
                WorldSpeed = targetSpeed;
            }
        }
    }
}
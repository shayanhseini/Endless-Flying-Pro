using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FadeController fadeController;
    [SerializeField] private WorldMovementManager worldMovement;


    [Header("Game Timer")]
    [SerializeField] private float gameDuration = 120f;

    [Header("Auto Restart")]
    [SerializeField] private float autoRestartDelay = 3f;

    [Header("Auto Start")]
    [SerializeField] private bool autoStartEnabled = true;
    [Min(0.5f)]
    [SerializeField] private float autoStartDelay = 7f;

    public float RemainingTime { get; private set; }


    public int Score { get; private set; }

    public float Distance { get; private set; }

    public GameState State { get; private set; }

    public GroundType CurrentGroundType { get; private set; }
    
    private bool gameTimeFinished;
    private Coroutine autoRestartCoroutine;
    private float waitingTimer;
    private bool waitingForFirstInput = true;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        State = GameState.Waiting;

        Score = 0;

        Distance = 0f;

        RemainingTime = gameDuration;

        waitingForFirstInput = true;
        waitingTimer = 0f;
    }

    private void Start()
    {
        if (DroneVision.DroneVisionInput.Instance != null)
        {
            DroneVision.DroneVisionInput.Instance.OnLockChanged += HandleDroneVisionLockChanged;
        }
    }

    private void HandleDroneVisionLockChanged(bool locked)
    {
        if (locked && State == GameState.Waiting)
        {
            TriggerAutoStart();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (DroneVision.DroneVisionInput.Instance != null)
        {
            DroneVision.DroneVisionInput.Instance.OnLockChanged -= HandleDroneVisionLockChanged;
        }
    }


    private void OnEnable()
    {
        SelectRandomGround();
        
        gameTimeFinished = false;
    }


    private void Update()
    {
        if (State == GameState.Waiting && waitingForFirstInput)
        {
            waitingTimer += Time.deltaTime;
            if (autoStartEnabled && waitingTimer >= autoStartDelay)
            {
                waitingForFirstInput = false;
                TriggerAutoStart();
            }
            return;
        }

        if (State != GameState.Playing)
            return;


        UpdateDistance();

        UpdateTimer();
    }

    public void ResetWaitingTimer()
    {
        waitingTimer = 0f;
    }

    public void TriggerAutoStart()
    {
        if (State != GameState.Waiting)
            return;

        waitingForFirstInput = false;

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.StartGame();
        }
        else
        {
            StartGame();
        }
    }


    private void SelectRandomGround()
    {
        /*GroundType[] groundTypes =
            (GroundType[])Enum.GetValues(
                typeof(GroundType)
            );


        int randomIndex =
            UnityEngine.Random.Range(
                0,
                groundTypes.Length
            );


        CurrentGroundType =
            groundTypes[randomIndex];
            */

        CurrentGroundType = GroundType.Dirt;


        Debug.Log(
            $"Selected Ground: {CurrentGroundType}"
        );
    }


    public void StartGame()
    {
        waitingForFirstInput = false;

        if (State != GameState.Waiting)
            return;

        if (DroneVision.DroneVisionInput.Instance != null)
        {
            DroneVision.DroneVisionInput.Instance.SetPaused(false);
        }

        State =
            GameState.Playing;


        RemainingTime =
            gameDuration;


        GameEvents.RaiseGameStarted();
    }


    public void AddScore(int amount)
    {
        if (State != GameState.Playing)
            return;


        Score += amount;


        GameEvents.RaiseScoreChanged(
            Score
        );
    }


    private void UpdateDistance()
    {
        if (worldMovement == null)
            return;


        Distance +=
            worldMovement.RealSpeed *
            Time.deltaTime;
    }


    private void UpdateTimer()
    {
        if (gameTimeFinished)
            return;

        RemainingTime -= Time.deltaTime;

        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            gameTimeFinished = true;

            GameEvents.RaiseGameTimeFinished();
        }
    }


    public void GameOver()
    {
        if (State != GameState.Playing)
            return;

        if (DroneVision.DroneVisionInput.Instance != null)
        {
            DroneVision.DroneVisionInput.Instance.SetPaused(true);
        }

        State =
            GameState.GameOver;


        GameEvents.RaiseGameOver();

        if (autoRestartCoroutine == null)
        {
            autoRestartCoroutine =
                StartCoroutine(AutoRestartAfterGameOver());
        }
    }


    private IEnumerator AutoRestartAfterGameOver()
    {
        yield return new WaitForSecondsRealtime(autoRestartDelay);
        RestartGame();
    }


    public void Victory()
    {
        if (DroneVision.DroneVisionInput.Instance != null)
        {
            DroneVision.DroneVisionInput.Instance.SetPaused(true);
        }

        State =
            GameState.Victory;


        GameEvents.RaiseVictory();
    }


    public void RestartGameWithEffect()
    {
        StartCoroutine(
            RestartGameRoutine()
        );
    }


    private IEnumerator RestartGameRoutine()
    {
        fadeController.FadeOut();

        yield return new WaitForSeconds(1f);

        RestartGame();
    }


    public void RestartGame()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }


    public void Quit()
    {
        Application.Quit();
    }
}


public enum GameState
{
    Waiting,
    Playing,
    Victory,
    GameOver
}


public enum GroundType
{
    Desert,
    Dirt
}

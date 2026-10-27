using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FadeController fadeController;

    [Header("Round Countdown")]
    [Min(0.1f)]
    [SerializeField] private float roundCountdownDuration = 5f;
    [Min(1)]
    [SerializeField] private int countdownStartValue = 5;

    public GameState State { get; private set; }

    public GroundType CurrentGroundType { get; private set; }
    private bool waitingForFirstInput;
    private Coroutine roundCountdownCoroutine;


    private void Awake()
    {
        State = GameState.Waiting;
    }

    private void Start()
    {
        waitingForFirstInput = true;
    }

    private void Update()
    {
        if (!waitingForFirstInput)
            return;

        bool keyboardPressed = Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (!keyboardPressed && !mousePressed)
            return;

        waitingForFirstInput = false;
        CameraTransitionController cameraTransition =
            FindFirstObjectByType<CameraTransitionController>();

        if (cameraTransition != null)
            cameraTransition.StartGameTransition();
        else
            StartGame();
    }
    
    private void OnEnable()
    {
        SelectRandomGround();
    }


    private void SelectRandomGround()
    {
        GroundType[] groundTypes =
            (GroundType[])Enum.GetValues(typeof(GroundType));

        int randomIndex =
            UnityEngine.Random.Range(0, groundTypes.Length);

        CurrentGroundType = groundTypes[randomIndex];

    }


    public void StartGame()
    {
        if (State != GameState.Waiting)
            return;

        State = GameState.Playing;

        GameEvents.RaiseGameStarted();
    }


    public void GameOver()
    {
        if (State != GameState.Playing && State != GameState.CrashLanding)
            return;

        State = GameState.GameOver;

        GameEvents.RaiseGameOver();
    }

    public bool BeginCrashLanding()
    {
        if (State != GameState.Playing)
            return false;

        State = GameState.CrashLanding;
        GameEvents.RaiseCrashLandingStarted();
        return true;
    }

    public bool BeginVictoryOrbit()
    {
        if (State != GameState.Playing)
            return false;

        State = GameState.VictoryOrbit;
        GameEvents.RaiseVictoryOrbitStarted();
        return true;
    }

    public bool BeginVictorySequence()
    {
        if (State != GameState.Playing)
            return false;

        State = GameState.VictorySequence;
        return true;
    }

    public void WinGame()
    {
        if (State != GameState.Playing && State != GameState.VictoryOrbit)
            return;

        State = GameState.Won;
        GameEvents.RaiseGameWon();
    }


    public void RestartGameWithEffect()
    {
        StartCoroutine(RestartGameRoutine());
    }

    public void BeginRestartFade()
    {
        if (fadeController != null)
            fadeController.FadeOut();
    }

    public void StartRoundCountdown(bool restartFromFirstPlanet)
    {
        if (roundCountdownCoroutine != null)
            return;

        roundCountdownCoroutine = StartCoroutine(
            RoundCountdownRoutine(restartFromFirstPlanet));
    }

    private IEnumerator RoundCountdownRoutine(bool restartFromFirstPlanet)
    {
        State = GameState.Countdown;
        GameEvents.RaiseRoundCountdownStarted();

        int stepCount = countdownStartValue + 1;
        float stepDuration = roundCountdownDuration / Mathf.Max(1, stepCount);
        for (int value = countdownStartValue; value >= 0; value--)
        {
            GameEvents.RaiseRoundCountdownTick(value, stepDuration);
            yield return new WaitForSeconds(stepDuration);
        }

        State = GameState.Playing;

        if (restartFromFirstPlanet)
        {
            PlanetSequenceManager sequence = FindFirstObjectByType<PlanetSequenceManager>();
            if (sequence != null)
                sequence.RestartFromFirstPlanet();
        }

        GameEvents.RaiseRoundCountdownCompleted();
        roundCountdownCoroutine = null;
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
    CrashLanding,
    VictoryOrbit,
    VictorySequence,
    Countdown,
    Won,
    GameOver
}


public enum GroundType
{
    Desert,
    Dirt
}

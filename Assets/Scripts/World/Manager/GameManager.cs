using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FadeController fadeController;

    public GameState State { get; private set; }

    public GroundType CurrentGroundType { get; private set; }
    private bool waitingForFirstInput;


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
    Won,
    GameOver
}


public enum GroundType
{
    Desert,
    Dirt
}

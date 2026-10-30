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
    [SerializeField] private float roundCountdownDuration = 3f;
    [Min(1)]
    [SerializeField] private int countdownStartValue = 3;

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

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
        {
            StartVictorySequence();
        }
#endif

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

        StartCountdown(isInitialGameStart: true, restartFromFirstPlanet: false);
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

    public bool BeginVictorySequence(float obstacleFadeDuration = 0.45f)
    {
        if (State != GameState.Playing)
            return false;

        State = GameState.VictorySequence;
        GameEvents.RaiseVictorySequenceStarted(obstacleFadeDuration);
        return true;
    }

    private Coroutine victorySequenceCoroutine;

    public void StartVictorySequence()
    {
        if (victorySequenceCoroutine != null)
            return;

        if (State != GameState.Playing && State != GameState.VictorySequence && State != GameState.VictoryOrbit)
            return;

        victorySequenceCoroutine = StartCoroutine(VictorySequenceRoutine());
    }

    public void WinGame()
    {
        StartVictorySequence();
    }

    private IEnumerator VictorySequenceRoutine()
    {
        State = GameState.Won;
        GameEvents.RaiseGameWon();
        GameEvents.RaiseVictorySequenceStarted(0.45f);

        PlanetSequenceManager sequence = FindFirstObjectByType<PlanetSequenceManager>();

        PlaneController plane = FindFirstObjectByType<PlaneController>();
        if (plane != null)
        {
            plane.PrepareForVictoryFlyaway();
        }

        // Start playing the ending victory audio (End.wav)
        if (sequence != null)
        {
            sequence.PlayVictoryNarration();
        }

        // Visible forward flight: smooth ease-in acceleration for ~2.0 seconds
        float flyDuration = 2.0f;
        float maxSpeed = 110f;
        Quaternion planeStartRot = plane != null ? plane.transform.rotation : Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            // Smooth ease-in acceleration (starts gently, accelerates forward into the distance)
            float speed = Mathf.Lerp(15f, maxSpeed, t * t);

            if (plane != null && plane.gameObject.activeInHierarchy)
            {
                plane.transform.rotation = Quaternion.Slerp(planeStartRot, Quaternion.identity, t * 2f);
                Vector3 moveDelta = (Vector3.forward * speed + Vector3.up * (speed * 0.08f)) * Time.deltaTime;
                plane.transform.position += moveDelta;
            }

            yield return null;
        }

        // Only hide/deactivate ship after it has clearly left the visible area
        if (plane != null)
        {
            plane.gameObject.SetActive(false);
        }

        // Wait until the ending narration clip (End.wav) finishes playing
        if (sequence != null)
        {
            while (sequence.IsNarrationPlaying)
            {
                yield return null;
            }
        }

        // Keep the victory state visible briefly after voice ends
        yield return new WaitForSeconds(1f);

        // Fade entire screen to full black
        float fadeDuration = 1.0f;
        if (fadeController != null)
        {
            fadeController.FadeOut(fadeDuration);
        }

        // Wait until fade completes and screen is fully black
        yield return new WaitForSeconds(fadeDuration);

        // Keep black overlay visible for a clean 2 seconds
        yield return new WaitForSeconds(2f);

        // Reset persistent session state so the new run starts clean
        PlanetSequenceManager.ResetSessionState();

        // Reload the game from the beginning
        RestartGame();
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

        StartCountdown(isInitialGameStart: false, restartFromFirstPlanet: restartFromFirstPlanet);
    }

    private void StartCountdown(bool isInitialGameStart, bool restartFromFirstPlanet)
    {
        if (roundCountdownCoroutine != null)
            return;

        roundCountdownCoroutine = StartCoroutine(
            CountdownRoutine(isInitialGameStart, restartFromFirstPlanet));
    }

    private IEnumerator CountdownRoutine(bool isInitialGameStart, bool restartFromFirstPlanet)
    {
        State = GameState.Countdown;
        GameEvents.RaiseRoundCountdownStarted();

        int stepCount = countdownStartValue;
        float stepDuration = roundCountdownDuration / Mathf.Max(1, stepCount);
        for (int value = countdownStartValue; value >= 1; value--)
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

        if (isInitialGameStart)
        {
            GameEvents.RaiseGameStarted();
        }

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

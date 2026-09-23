using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FadeController fadeController;
    [SerializeField] private WorldMovementManager worldMovement;


    [Header("Game Timer")]
    [SerializeField] private float gameDuration = 120f;

    public float RemainingTime { get; private set; }


    public int Score { get; private set; }

    public float Distance { get; private set; }

    public GameState State { get; private set; }

    public GroundType CurrentGroundType { get; private set; }
    
    private bool gameTimeFinished;


    private void Awake()
    {
        State = GameState.Waiting;

        Score = 0;

        Distance = 0f;

        RemainingTime = gameDuration;
    }


    private void OnEnable()
    {
        SelectRandomGround();
        
        gameTimeFinished = false;
    }


    private void Update()
    {
        if (State != GameState.Playing)
            return;


        UpdateDistance();

        UpdateTimer();
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
        if (State != GameState.Waiting)
            return;


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


        State =
            GameState.GameOver;


        GameEvents.RaiseGameOver();
    }


    public void Victory()
    {
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
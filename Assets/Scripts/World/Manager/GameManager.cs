using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private FadeController fadeController;
    
    public int Score { get; private set; }

    public GameState State { get; private set; }


    private void Awake()
    {
        State = GameState.Waiting;
        Score = 0;
    }

    
    private void OnEnable()
    {
        GameEvents.GameOver += RestartGameOnStay;
        
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= RestartGameOnStay;
    }
    
    

    public void StartGame()
    {
        if (State != GameState.Waiting)
            return;

        State = GameState.Playing;

        GameEvents.RaiseGameStarted();
    }

    
    public void AddScore(int amount)
    {
        if (State != GameState.Playing)
            return;

        Score += amount;
        
        GameEvents.RaiseScoreChanged(Score);
    }


    public void GameOver()
    {
        if (State != GameState.Playing)
            return;

        State = GameState.GameOver;

        GameEvents.RaiseGameOver();
    }

    private void RestartGameOnStay()
    {
        StartCoroutine(RestartGameRoutine());
    }
    
    private IEnumerator RestartGameRoutine()
    {
        yield return new WaitForSeconds(2f);
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
    GameOver
}
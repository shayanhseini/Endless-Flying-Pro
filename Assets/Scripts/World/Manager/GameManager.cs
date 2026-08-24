using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int Score { get; private set; }

    public GameState State { get; private set; }


    private void Awake()
    {
        State = GameState.Waiting;
        Score = 0;
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
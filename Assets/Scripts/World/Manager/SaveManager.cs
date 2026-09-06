using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;


    private const string HighScoreKey = "HighScore";


    private void OnEnable()
    {
        GameEvents.GameOver += SaveHighScore;
    }


    private void OnDisable()
    {
        GameEvents.GameOver -= SaveHighScore;
    }


    private void SaveHighScore()
    {
        int currentScore =
            gameManager.Score;

        int highScore =
            GetHighScore();


        if (currentScore <= highScore)
            return;


        PlayerPrefs.SetInt(
            HighScoreKey,
            currentScore
        );

        PlayerPrefs.Save();
    }


    public int GetHighScore()
    {
        return PlayerPrefs.GetInt(
            HighScoreKey,
            0
        );
    }


    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(
            HighScoreKey
        );

        PlayerPrefs.Save();
    }
}
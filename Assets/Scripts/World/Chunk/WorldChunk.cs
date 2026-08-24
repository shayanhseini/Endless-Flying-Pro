using UnityEngine;
using UnityEngine.Events;

public class WorldChunk : MonoBehaviour
{
    [SerializeField]
    private float length = 20f;

    [SerializeField]
    private ChunkContent content;

    [Header("Environment Variants")]
    [SerializeField]
    private GameObject[] environmentVariants;

    [Header("Event")] public UnityEvent onResetContent;


    public float Length => length;


    public void ResetContent(
        DifficultyManager difficultyManager)
    {
        onResetContent?.Invoke();
        
        RandomizeEnvironment();

        content.ApplyDifficulty(
            difficultyManager.CoinCount,
            difficultyManager.ObstacleCount,
            difficultyManager.HasShield,
            difficultyManager.HasSpeedPowerUp
        );
    }


    private void RandomizeEnvironment()
    {
        if (environmentVariants == null ||
            environmentVariants.Length == 0)
            return;


        foreach (GameObject environment
                 in environmentVariants)
        {
            if (environment != null)
                environment.SetActive(false);
        }


        int randomIndex =
            Random.Range(
                0,
                environmentVariants.Length
            );


        GameObject selectedEnvironment =
            environmentVariants[randomIndex];


        if (selectedEnvironment != null)
        {
            selectedEnvironment.SetActive(true);
            
            selectedEnvironment.GetComponent<GroundDefenceHolder>().ResetMissile();
        }
            
        
    }


    public void Move(Vector3 movement)
    {
        transform.position += movement;
    }


    public void MoveTo(Vector3 position)
    {
        transform.position = position;
    }
}
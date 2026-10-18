using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Obstacles")]
    [SerializeField] private int startObstacleCount = 10;


    [Header("Shield")]
    [SerializeField] private bool startWithShield = true;


    [Header("Speed Power-Up")]
    [SerializeField] private bool startWithSpeedPowerUp = true;

    public int ObstacleCount { get; private set; }

    public bool HasShield { get; private set; }

    public bool HasSpeedPowerUp { get; private set; }


    private void Awake()
    {
        ObstacleCount = startObstacleCount;
        HasShield = startWithShield;
        HasSpeedPowerUp = startWithSpeedPowerUp;
    }
}

using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Obstacles")]
    [SerializeField] private int startObstacleCount = 10;


    [Header("Shield")]
    [SerializeField] private bool startWithShield = true;
    [Range(0f, 1f)]
    [SerializeField] private float shieldSpawnChance = 0.85f;
    [Range(1, 10)]
    [SerializeField] private int shieldCountPerChunk = 2;


    [Header("Speed Power-Up")]
    [SerializeField] private bool startWithSpeedPowerUp = true;
    [Range(0f, 1f)]
    [SerializeField] private float speedPowerUpSpawnChance = 0.8f;
    [Range(1, 10)]
    [SerializeField] private int speedPowerUpCountPerChunk = 2;

    public int ObstacleCount { get; private set; }

    public bool HasShield { get; private set; }

    public bool HasSpeedPowerUp { get; private set; }

    public float ShieldSpawnChance => shieldSpawnChance;
    public float SpeedPowerUpSpawnChance => speedPowerUpSpawnChance;

    public int ShieldCountPerChunk => shieldCountPerChunk;
    public int SpeedPowerUpCountPerChunk => speedPowerUpCountPerChunk;

    public bool ShouldSpawnShield => HasShield && (shieldSpawnChance >= 1f || Random.value <= shieldSpawnChance);
    public bool ShouldSpawnSpeedPowerUp => HasSpeedPowerUp && (speedPowerUpSpawnChance >= 1f || Random.value <= speedPowerUpSpawnChance);

    public int GetShieldSpawnCount()
    {
        if (!HasShield)
            return 0;

        if (shieldSpawnChance < 1f && Random.value > shieldSpawnChance)
            return 0;

        return Mathf.Max(1, shieldCountPerChunk);
    }

    public int GetSpeedPowerUpSpawnCount()
    {
        if (!HasSpeedPowerUp)
            return 0;

        if (speedPowerUpSpawnChance < 1f && Random.value > speedPowerUpSpawnChance)
            return 0;

        return Mathf.Max(1, speedPowerUpCountPerChunk);
    }


    private void Awake()
    {
        ObstacleCount = startObstacleCount;
        HasShield = startWithShield;
        HasSpeedPowerUp = startWithSpeedPowerUp;
    }
}

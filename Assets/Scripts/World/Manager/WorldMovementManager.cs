using UnityEngine;

public class WorldMovementManager : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float startSpeed = 10f;
    [Header("Real Speed")]
    [SerializeField] private float realSpeedMultiplier = 20f;
    
    
    public float RealSpeed =>
        WorldSpeed * realSpeedMultiplier;

    public float BaseWorldSpeed { get; private set; }

    public float WorldSpeed { get; private set; }

    public float SpeedMultiplier { get; private set; } = 1f;

    public bool IsMovementEnabled { get; private set; }


    private void Awake()
    {
        BaseWorldSpeed =
            startSpeed;


        WorldSpeed =
            startSpeed;


        SpeedMultiplier =
            1f;


        IsMovementEnabled =
            false;
    }


    private void OnEnable()
    {
        GameEvents.GameStarted += StartMovement;
        GameEvents.GameOver += StopMovement;
    }


    private void OnDisable()
    {
        GameEvents.GameStarted -= StartMovement;
        GameEvents.GameOver -= StopMovement;
    }


    private void StartMovement()
    {
        IsMovementEnabled = true;
    }


    private void StopMovement()
    {
        IsMovementEnabled = false;
    }


    public void SetSpeedMultiplier(
        float multiplier)
    {
        SpeedMultiplier =
            Mathf.Max(
                0f,
                multiplier
            );


        UpdateWorldSpeed();
    }


    private void UpdateWorldSpeed()
    {
        float targetSpeed =
            BaseWorldSpeed *
            SpeedMultiplier;
        
        if (targetSpeed >= WorldSpeed)
        {
            WorldSpeed = targetSpeed;
        }
        else
        {
            if (SpeedMultiplier <= 1f)
            {
                WorldSpeed = targetSpeed;
            }
        }
    }
}

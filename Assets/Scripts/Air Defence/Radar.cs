using UnityEngine;

public class Radar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AirDefenseController airDefense;


    private bool hasDetectedPlayer;


    private void Awake()
    {
        if (airDefense == null)
        {
            airDefense =
                GetComponentInParent<
                    AirDefenseController>();
        }
    }
    
    public void SetDetectedPlayer(bool isDetected)
    {
        hasDetectedPlayer = isDetected;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (hasDetectedPlayer)
            return;


        if (!other.CompareTag("Player"))
            return;


        hasDetectedPlayer = true;


        if (airDefense != null)
        {
            airDefense.Fire();
        }
    }
}
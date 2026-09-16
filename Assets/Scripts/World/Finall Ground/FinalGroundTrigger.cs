using UnityEngine;

public class FinalGroundTrigger : MonoBehaviour
{
    [SerializeField] private PlaneController planeController;
    [SerializeField] private Transform finalTarget;

    private bool triggered;


    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;


        if (!other.CompareTag("Player"))
            return;


        triggered = true;


        if (planeController != null)
        {
            planeController.StartFinalAutoPilot(
                finalTarget
            );
        }
        
        Debug.Log("FinalGroundTrigger entered");
    }
}
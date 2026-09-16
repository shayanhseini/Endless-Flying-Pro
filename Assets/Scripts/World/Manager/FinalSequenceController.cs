using UnityEngine;

public class FinalSequenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform plane;

    [Header("Auto Pilot")]
    [SerializeField] private float autoPilotSpeed = 20f;
    [SerializeField] private float autoPilotRotationSpeed = 4f;


    private Transform finalTarget;

    private bool isAutoPilot;


    private void OnEnable()
    {
        GameEvents.FinalGroundActivated +=
            StartFinalSequence;
    }


    private void OnDisable()
    {
        GameEvents.FinalGroundActivated -=
            StartFinalSequence;
    }


    private void Update()
    {
        if (!isAutoPilot)
            return;


        MoveTowardsFinalTarget();
    }


    private void StartFinalSequence(
        Transform target)
    {
        if (plane == null ||
            target == null)
            return;


        finalTarget = target;

        isAutoPilot = true;
    }


    private void MoveTowardsFinalTarget()
    {
        if (finalTarget == null)
        {
            isAutoPilot = false;
            return;
        }


        Vector3 direction =
            finalTarget.position -
            plane.position;


        if (direction.sqrMagnitude <= 0.001f)
            return;


        direction.Normalize();


        plane.position +=
            direction *
            autoPilotSpeed *
            Time.deltaTime;


        RotatePlane(direction);
    }


    private void RotatePlane(
        Vector3 direction)
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        plane.rotation =
            Quaternion.Lerp(
                plane.rotation,
                targetRotation,
                autoPilotRotationSpeed *
                Time.deltaTime
            );
    }
}
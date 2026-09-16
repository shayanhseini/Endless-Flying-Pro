using UnityEngine;

public class CinematicCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Vector3 followOffset =
        new Vector3(0f, 2f, -8f);

    [SerializeField] private float positionSmoothness = 3f;

    [Header("Look At")]
    [SerializeField] private Vector3 lookAtOffset =
        new Vector3(0f, 0.5f, 3f);

    [SerializeField] private float rotationSmoothness = 4f;


    private Transform target;
    private bool isFollowing;


    public void StartFollow(Transform target)
    {
        if (target == null)
            return;

        this.target = target;
        isFollowing = true;
    }


    public void StopFollow()
    {
        isFollowing = false;
        target = null;
    }


    private void LateUpdate()
    {
        if (!isFollowing || target == null)
            return;


        Vector3 desiredPosition =
            target.TransformPoint(followOffset);


        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                positionSmoothness * Time.deltaTime
            );


        Vector3 lookPosition =
            target.TransformPoint(lookAtOffset);


        Vector3 direction =
            lookPosition - transform.position;


        if (direction.sqrMagnitude <= 0.001f)
            return;


        Quaternion targetRotation =
            Quaternion.LookRotation(direction);


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmoothness * Time.deltaTime
            );
    }
}
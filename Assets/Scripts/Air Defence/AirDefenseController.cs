using System;
using UnityEngine;

public class AirDefenseController : MonoBehaviour
{
    [Header("References")]
    private Transform player;

    [SerializeField] private Radar radar;
    [SerializeField] private Transform missileSpawnPoint;
    [SerializeField] private Missile missile;


    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;


    private bool hasFired;

    private float initialX;
    private float initialZ;


    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player =
                    playerObject.transform;
        }


        Vector3 initialEuler =
            transform.rotation.eulerAngles;


        initialX =
            initialEuler.x;

        initialZ =
            initialEuler.z;
    }


    private void OnEnable()
    {
        hasFired = false;


        if (radar != null)
            radar.gameObject.SetActive(true);


        if (missile != null)
        {
            missile.gameObject.SetActive(false);
        }
    }


    private void Update()
    {
        if (player == null)
            return;


        RotateTowardsPlayer();
    }


    private void RotateTowardsPlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;


        direction.y = 0f;


        if (direction.sqrMagnitude <= 0.001f)
            return;


        float targetY =
            Quaternion.LookRotation(direction)
                .eulerAngles.y;


        Quaternion targetRotation =
            Quaternion.Euler(
                initialX,
                targetY + 90f,
                initialZ
            );


        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    public void Fire()
    {
        if (hasFired)
            return;


        if (missile == null ||
            player == null)
            return;


        hasFired = true;
        
        missile.gameObject.SetActive(true);
        
        missile.SetTarget(player);
    }
}
using System;
using UnityEngine;

public class AirDefenseController : MonoBehaviour
{
    [Header("References")]
    private Transform player;

    [SerializeField] private Radar radar;
    [SerializeField] private Transform missileSpawnPoint;
    [SerializeField] private Missile missile;
    [SerializeField] private AudioSource launchSound;


    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;


    private bool hasFired;

    private float initialY;


    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                Debug.Log("Plane Found");
            }
        }

        if (player != null)
        {
            SetInitialRotation();
        }
    }


    private void OnEnable()
    {
        hasFired = false;

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                SetInitialRotation();
            }
        }

        if (radar != null)
            radar.gameObject.SetActive(true);

        if (missile != null)
            missile.gameObject.SetActive(false);
    }


    private void Update()
    {
        if (player == null)
            return;

        float dz = transform.position.z - player.position.z;
        if (dz < -15f || dz > 120f)
            return;

        RotateTowardsPlayer();
    }


    private void SetInitialRotation()
    {
        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        // زاویه افقی نسبت به Forward
        float targetY = Quaternion.LookRotation(direction).eulerAngles.y;

        // چون Forward مدل ما -Right است، 90 درجه Offset داریم
        targetY += 90f;

        initialY = targetY;

        transform.rotation = Quaternion.Euler(90f, initialY, 0f);
    }


    private void RotateTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        float targetY = Quaternion.LookRotation(direction).eulerAngles.y + 90f;

        Quaternion targetRotation = Quaternion.Euler(
            90f,
            targetY,
            0f
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }


    public void Fire()
    {
        if (hasFired)
            return;

        if (missile == null ||
            player == null)
            return;

        GameEvents.RaiseLaunchMissile();

        hasFired = true;

        missile.gameObject.SetActive(true);

        missile.SetTarget(player);

        if (launchSound != null)
            launchSound.Play();
    }
}
using UnityEngine;

public class SelfRotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f;

    private static Transform cachedPlayer;
    private static float nextPlayerCheckTime;

    private void Update()
    {
        if (cachedPlayer == null)
        {
            if (Time.time >= nextPlayerCheckTime)
            {
                nextPlayerCheckTime = Time.time + 1f;
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                    cachedPlayer = p.transform;
            }
        }

        if (cachedPlayer != null)
        {
            float dz = transform.position.z - cachedPlayer.position.z;
            if (dz > 80f || dz < -10f)
                return;
        }

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
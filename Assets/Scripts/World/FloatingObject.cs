using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float amplitude = 0.2f;
    [SerializeField] private float speed = 2f;

    private Vector3 initialPosition;
    private float randomOffset;

    private static Transform cachedPlayer;
    private static float nextPlayerCheckTime;

    private void Awake()
    {
        initialPosition = transform.localPosition;

        randomOffset =
            Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        transform.localPosition = initialPosition;
    }

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

        float offset =
            Mathf.Sin(
                Time.time * speed + randomOffset
            ) * amplitude;

        Vector3 position =
            initialPosition;

        position.y += offset;

        transform.localPosition = position;
    }
}
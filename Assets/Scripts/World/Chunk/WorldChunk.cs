using UnityEngine;
using UnityEngine.Events;

public class WorldChunk : MonoBehaviour
{
    [SerializeField]
    private float length = 20f;

    [SerializeField]
    private ChunkContent content;

    [Header("Environment Variants")]
    [SerializeField]
    private bool showGroundEnvironment = false;

    [SerializeField]
    private GameObject[] desertVariants;
    [SerializeField]
    private GameObject[] dirtVariants;

    [Header("Event")]
    public UnityEvent onResetContent;


    public float Length => length;


    public void ResetContent(
        DifficultyManager difficultyManager,
        GroundType groundType)
    {
        onResetContent?.Invoke();

        if (showGroundEnvironment)
            RandomizeEnvironment(groundType);
        else
            DisableAllEnvironments();

        content.ApplyDifficulty(
            difficultyManager.ObstacleCount,
            difficultyManager.HasShield,
            difficultyManager.HasSpeedPowerUp
        );
    }


    private void RandomizeEnvironment(GroundType groundType)
    {
        DisableAllEnvironments();

        GameObject[] variants = GetVariants(groundType);

        if (variants == null || variants.Length == 0)
            return;

        int randomIndex =
            Random.Range(0, variants.Length);

        GameObject selectedEnvironment =
            variants[randomIndex];

        if (selectedEnvironment == null)
            return;

        selectedEnvironment.SetActive(true);

        GroundDefenceHolder holder =
            selectedEnvironment.GetComponent<GroundDefenceHolder>();

        if (holder != null)
            holder.ResetMissile();
    }


    private GameObject[] GetVariants(GroundType groundType)
    {
        switch (groundType)
        {
            case GroundType.Desert:
                return desertVariants;

            case GroundType.Dirt:
                return dirtVariants;

            default:
                return null;
        }
    }


    private void DisableAllEnvironments()
    {
        DisableVariants(desertVariants);
        DisableVariants(dirtVariants);
    }


    private void DisableVariants(GameObject[] variants)
    {
        if (variants == null)
            return;

        foreach (GameObject environment in variants)
        {
            if (environment != null)
                environment.SetActive(false);
        }
    }


    public void Move(Vector3 movement)
    {
        transform.position += movement;
    }


    public void MoveTo(Vector3 position)
    {
        transform.position = position;
    }
}

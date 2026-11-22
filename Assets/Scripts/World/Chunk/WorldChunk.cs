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
    private GameObject[] desertVariants;

    [SerializeField]
    private GameObject[] dirtVariants;


    [Header("Final Ground")]
    [SerializeField]
    private GameObject finalGround;

    [SerializeField]
    private Transform finalTarget;


    [Header("Event")]
    public UnityEvent onResetContent;


    public float Length =>
        length;


    public Transform FinalTarget =>
        finalTarget;


    public void ResetContent(
        DifficultyManager difficultyManager,
        GroundType groundType)
    {
        onResetContent?.Invoke();

        DisableFinalGround();

        RandomizeEnvironment(
            groundType,
            difficultyManager
        );


        if (content != null && difficultyManager != null)
        {
            content.ApplyDifficulty(
                difficultyManager.CoinCount,
                difficultyManager.ObstacleCount,
                difficultyManager.GetShieldCountForChunk(),
                difficultyManager.HasSpeedPowerUp
            );
        }
    }


    public void ActivateFinalGround()
    {
        // تمام Content خاموش
        if (content != null)
        {
            content.DisableContent();
        }


        // Groundهای معمولی خاموش
        DisableAllEnvironments();


        // Final Ground روشن
        if (finalGround != null)
        {
            finalGround.SetActive(true);
        }
    }


    private void DisableFinalGround()
    {
        if (finalGround != null)
        {
            finalGround.SetActive(false);
        }
    }


    private void RandomizeEnvironment(
        GroundType groundType,
        DifficultyManager difficultyManager = null)
    {
        DisableAllEnvironments();


        GameObject[] variants =
            GetVariants(
                groundType
            );


        if (variants == null ||
            variants.Length == 0)
            return;


        int randomIndex =
            Random.Range(
                0,
                variants.Length
            );


        GameObject selectedEnvironment =
            variants[randomIndex];


        if (selectedEnvironment == null)
            return;


        selectedEnvironment.SetActive(true);


        GroundDefenceHolder holder =
            selectedEnvironment.GetComponent<
                GroundDefenceHolder
            >();


        if (holder != null)
        {
            holder.ResetMissile(difficultyManager);
        }
    }


    private GameObject[] GetVariants(
        GroundType groundType)
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
        DisableVariants(
            desertVariants
        );

        DisableVariants(
            dirtVariants
        );
    }


    private void DisableVariants(
        GameObject[] variants)
    {
        if (variants == null)
            return;


        foreach (GameObject environment
                 in variants)
        {
            if (environment != null)
            {
                environment.SetActive(false);
            }
        }
    }


    public void Move(
        Vector3 movement)
    {
        transform.position += movement;
    }


    public void MoveTo(
        Vector3 position)
    {
        transform.position = position;
    }
}
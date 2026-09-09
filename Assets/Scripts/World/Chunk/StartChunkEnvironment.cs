using UnityEngine;

public class StartChunkEnvironment : MonoBehaviour
{
    [Header("Environment")]
    [SerializeField] private GameObject desertEnvironment;
    [SerializeField] private GameObject dirtEnvironment;

    [Header("References")]
    [SerializeField] private GameManager gameManager;


    private void Start()
    {
        ApplyEnvironment();
    }


    private void ApplyEnvironment()
    {
        desertEnvironment.SetActive(false);
        dirtEnvironment.SetActive(false);

        switch (gameManager.CurrentGroundType)
        {
            case GroundType.Desert:
                desertEnvironment.SetActive(true);
                break;

            case GroundType.Dirt:
                dirtEnvironment.SetActive(true);
                break;
        }
    }
}
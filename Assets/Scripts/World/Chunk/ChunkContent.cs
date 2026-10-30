using UnityEngine;

public class ChunkContent : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private GameObject[] obstacles;
    [SerializeField] private GameObject[] shields;
    [SerializeField] private GameObject[] speedPowerUps;


    public void ApplyDifficulty(
        int obstacleCount,
        bool hasShield,
        bool hasSpeedPowerUp)
    {
        ApplyDifficulty(
            obstacleCount,
            hasShield ? 1 : 0,
            hasSpeedPowerUp ? 1 : 0
        );
    }


    public void ApplyDifficulty(
        int obstacleCount,
        int shieldCount,
        int speedPowerUpCount)
    {
        ActivateRandomObjects(
            obstacles,
            obstacleCount
        );


        // Shield
        if (shieldCount > 0)
        {
            ActivateRandomObjects(
                shields,
                shieldCount
            );
        }
        else
        {
            DisableAll(shields);
        }


        // Speed Power-Up
        if (speedPowerUpCount > 0)
        {
            ActivateRandomObjects(
                speedPowerUps,
                speedPowerUpCount
            );
        }
        else
        {
            DisableAll(speedPowerUps);
        }
    }


    private void ActivateRandomObjects(
        GameObject[] objects,
        int amount)
    {
        if (objects == null || objects.Length == 0)
            return;


        DisableAll(objects);


        amount =
            Mathf.Clamp(
                amount,
                0,
                objects.Length
            );


        int activated = 0;


        while (activated < amount)
        {
            int index =
                Random.Range(
                    0,
                    objects.Length
                );


            if (objects[index].activeSelf)
                continue;


            objects[index].SetActive(true);

            activated++;
        }
    }


    private void DisableAll(
        GameObject[] objects)
    {
        if (objects == null)
            return;


        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}

using UnityEngine;

public class ChunkContent : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private GameObject[] coins;
    [SerializeField] private GameObject[] obstacles;
    [SerializeField] private GameObject[] shields;
    [SerializeField] private GameObject[] speedPowerUps;


    public void ApplyDifficulty(
        int coinCount,
        int obstacleCount,
        bool hasShield,
        bool hasSpeedPowerUp)
    {
        ActivateRandomObjects(
            coins,
            coinCount
        );


        ActivateRandomObjects(
            obstacles,
            obstacleCount
        );


        // Shield
        if (hasShield)
        {
            ActivateRandomObjects(
                shields,
                1
            );
        }
        else
        {
            DisableAll(shields);
        }


        // Speed Power-Up
        if (hasSpeedPowerUp)
        {
            ActivateRandomObjects(
                speedPowerUps,
                1
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
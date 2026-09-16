using UnityEngine;

public class PlaneDeathController : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.GameOver += DisablePlane;
        GameEvents.Victory += DisablePlane;
    }


    private void OnDisable()
    {
        GameEvents.GameOver -= DisablePlane;
        GameEvents.Victory -= DisablePlane;
    }


    private void DisablePlane()
    {
        gameObject.SetActive(false);
    }
}
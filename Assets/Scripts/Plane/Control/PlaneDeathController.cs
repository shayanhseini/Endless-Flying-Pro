using UnityEngine;

public class PlaneDeathController : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.GameOver += DisablePlane;
    }


    private void OnDisable()
    {
        GameEvents.GameOver -= DisablePlane;
    }


    private void DisablePlane()
    {
        gameObject.SetActive(false);
    }
}
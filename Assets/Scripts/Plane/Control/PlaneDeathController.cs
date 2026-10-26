using UnityEngine;

public class PlaneDeathController : MonoBehaviour
{
    [Tooltip("Leave disabled so the plane remains visible after landing.")]
    [SerializeField] private bool hidePlaneOnGameOver;

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
        if (hidePlaneOnGameOver)
            gameObject.SetActive(false);
    }
}

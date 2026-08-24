using System;
using UnityEngine;

public class SpeedLineController : MonoBehaviour
{
    [SerializeField] private GameObject speedLine;
    
    private void OnEnable()
    {
        GameEvents.GameOver += DisableSpeedLine;
        GameEvents.GameStarted += EnableSpeedLine;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= DisableSpeedLine;
        GameEvents.GameStarted -= EnableSpeedLine;
    }

    private void DisableSpeedLine()
    {
        speedLine.SetActive(false);
    }

    private void EnableSpeedLine()
    {
        speedLine.SetActive(true);
    }
}

using System;
using UnityEngine;

public class GroundDefenceHolder : MonoBehaviour
{
    [SerializeField] private Radar radar;
    [SerializeField] private Missile missile;

    public void ResetMissile()
    {
        radar.SetDetectedPlayer(false);

        missile.StopAndReset();
    }
}

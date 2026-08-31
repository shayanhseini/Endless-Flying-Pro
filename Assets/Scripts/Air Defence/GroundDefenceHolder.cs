using System;
using UnityEngine;

public class GroundDefenceHolder : MonoBehaviour
{
    [SerializeField] private Radar[] radars;
    [SerializeField] private Missile[] missiles;

    public void ResetMissile()
    {
        foreach (Radar radar in radars)
        {
            radar.SetDetectedPlayer(false);
        }

        foreach (Missile missile in missiles)
        {
            missile.StopAndReset();
        }
    }
}

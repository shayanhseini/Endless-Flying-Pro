using System;
using UnityEngine;

public class GroundDefenceHolder : MonoBehaviour
{
    [SerializeField] private Radar[] radars;
    [SerializeField] private Missile[] missiles;

    public void ResetMissile()
    {
        ResetMissile(DifficultyManager.Instance);
    }

    public void ResetMissile(DifficultyManager difficultyManager)
    {
        bool isActive = true;
        float turnSpeedMult = 1f;
        float predMult = 1f;

        if (difficultyManager != null)
        {
            isActive = difficultyManager.RollAirDefenseActive();
            difficultyManager.GetAirDefenseAccuracyModifiers(out turnSpeedMult, out predMult);
        }

        if (radars != null)
        {
            foreach (Radar radar in radars)
            {
                if (radar == null) continue;
                radar.SetDetectedPlayer(false);
                radar.gameObject.SetActive(isActive);
            }
        }

        if (missiles != null)
        {
            foreach (Missile missile in missiles)
            {
                if (missile == null) continue;
                missile.StopAndReset();
                missile.ApplyDifficultyModifiers(turnSpeedMult, predMult);
                if (!isActive)
                {
                    missile.gameObject.SetActive(false);
                }
            }
        }

        AirDefenseController[] controllers = GetComponentsInChildren<AirDefenseController>(true);
        if (controllers != null)
        {
            foreach (AirDefenseController controller in controllers)
            {
                if (controller != null)
                    controller.enabled = isActive;
            }
        }
    }
}

using System;

public static class GameEvents
{
    public static event Action GameStarted;
    public static event Action GameOver;
    public static event Action GameWon;
    public static event Action<string, string, float, bool> PlanetIntroductionStarted;
    public static event Action<string, float, bool> PlanetDodgeStarted;
    public static event Action<float> PlanetPhaseTimeChanged;

    public static event Action PlaneHit;

    public static event Action ShieldActivated;
    public static event Action ShieldDeactivated;
    
    public static event Action SpeedBoostActivated;
    public static event Action SpeedBoostDeactivated;

    public static event Action LaunchMissile;


    public static void RaiseGameStarted()
    {
        GameStarted?.Invoke();
    }


    public static void RaiseGameOver()
    {
        GameOver?.Invoke();
    }

    public static void RaiseGameWon()
    {
        GameWon?.Invoke();
    }

    public static void RaisePlanetIntroductionStarted(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft)
    {
        PlanetIntroductionStarted?.Invoke(
            planetName,
            description,
            duration,
            planetOnLeft);
    }

    public static void RaisePlanetDodgeStarted(
        string planetName,
        float duration,
        bool planetOnLeft)
    {
        PlanetDodgeStarted?.Invoke(planetName, duration, planetOnLeft);
    }

    public static void RaisePlanetPhaseTimeChanged(float remainingTime)
    {
        PlanetPhaseTimeChanged?.Invoke(remainingTime);
    }


    public static void RaisePlaneHit()
    {
        PlaneHit?.Invoke();
    }
    

    public static void RaiseShieldActivated()
    {
        ShieldActivated?.Invoke();
    }


    public static void RaiseShieldDeactivated()
    {
        ShieldDeactivated?.Invoke();
    }

    public static void RaiseSpeedBoostActivated()
    {
        SpeedBoostActivated?.Invoke();
    }

    public static void RaiseSpeedBoostDeactivated()
    {
        SpeedBoostDeactivated?.Invoke();
    }

    public static void RaiseLaunchMissile()
    {
        LaunchMissile?.Invoke();
    }
    
}

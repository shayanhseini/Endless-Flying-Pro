using System;

public static class GameEvents
{
    public static event Action GameStarted;
    public static event Action GameOver;
    public static event Action GameWon;
    public static event Action CrashLandingStarted;
    public static event Action VictoryOrbitStarted;
    public static event Action AtmosphereReached;
    public static event Action<string, string, float, bool, int> PlanetIntroductionStarted;
    public static event Action PlanetIntroductionEnded;
    public static event Action<int, float> PlanetProgressChanged;

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

    public static void RaiseCrashLandingStarted()
    {
        CrashLandingStarted?.Invoke();
    }

    public static void RaiseVictoryOrbitStarted()
    {
        VictoryOrbitStarted?.Invoke();
    }

    public static void RaiseAtmosphereReached()
    {
        AtmosphereReached?.Invoke();
    }

    public static void RaisePlanetIntroductionStarted(
        string planetName,
        string description,
        float duration,
        bool planetOnLeft,
        int planetIndex)
    {
        PlanetIntroductionStarted?.Invoke(
            planetName,
            description,
            duration,
            planetOnLeft,
            planetIndex);
    }

    public static void RaisePlanetIntroductionEnded()
    {
        PlanetIntroductionEnded?.Invoke();
    }

    public static void RaisePlanetProgressChanged(int planetIndex, float progress)
    {
        PlanetProgressChanged?.Invoke(planetIndex, progress);
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

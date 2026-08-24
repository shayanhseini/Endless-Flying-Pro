using System;

public static class GameEvents
{
    public static event Action GameStarted;
    public static event Action GameOver;

    public static event Action<int> ScoreChanged;

    public static event Action PlaneHit;

    public static event Action ShieldActivated;
    public static event Action ShieldDeactivated;
    
    public static event Action SpeedBoostActivated;
    public static event Action SpeedBoostDeactivated;


    public static void RaiseGameStarted()
    {
        GameStarted?.Invoke();
    }


    public static void RaiseGameOver()
    {
        GameOver?.Invoke();
    }


    public static void RaiseScoreChanged(int score)
    {
        ScoreChanged?.Invoke(score);
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
}
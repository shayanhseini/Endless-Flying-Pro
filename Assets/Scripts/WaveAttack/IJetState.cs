namespace Game.Enemies.Fighter
{
    /// <summary>
    /// Contract for every state a FighterJet can be in.
    /// Adding new behaviour later (e.g. a "Diving" or "Firing" state)
    /// only means creating a new class here - nothing else needs to change (Open/Closed Principle).
    /// </summary>
    public interface IJetState
    {
        void Enter(FighterJetController jet);
        void Update(FighterJetController jet);
        void Exit(FighterJetController jet);
    }
}
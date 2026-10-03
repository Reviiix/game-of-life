namespace GameOfLife.Gameplay
{
    /// <summary>The stages the game moves through between editing cells and running the simulation.</summary>
    public enum GamePhase
    {
        Editing,
        CountingDown,
        Running
    }
}

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>The stages classic mode moves through between editing cells and running the simulation.</summary>
    public enum ClassicGamePhase
    {
        Editing,
        CountingDown,
        Running
    }
}

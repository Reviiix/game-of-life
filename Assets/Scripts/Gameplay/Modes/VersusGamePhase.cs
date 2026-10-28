namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Versus mode's own presentation and timing state, layered on VersusMatch.Stage, which owns the rules; adds waiting, countdown and pause.</summary>
    public enum VersusGamePhase
    {
        Setup,
        WaitingToStart,
        CountingDown,
        Running,
        Paused,
        Finished
    }
}

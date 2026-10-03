namespace GameOfLife.Versus
{
    /// <summary>Which part of a versus match is in progress: turn-based setup, the timed match, or finished.</summary>
    public enum VersusStage
    {
        Setup,
        Match,
        Finished
    }
}

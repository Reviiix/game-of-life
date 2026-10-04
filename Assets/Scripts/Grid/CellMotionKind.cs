namespace GameOfLife.Grid
{
    /// <summary>The animation a cell plays after it changes.</summary>
    public enum CellMotionKind : byte
    {
        Appear,
        Disappear,
        Convert
    }
}

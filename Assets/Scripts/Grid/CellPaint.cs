namespace GameOfLife.Grid
{
    /// <summary>What a cell on the board is drawn as; every colour except Custom comes from the current theme palette.</summary>
    public enum CellPaint : byte
    {
        Empty,
        Ink,
        Player,
        Opponent,
        Custom
    }
}

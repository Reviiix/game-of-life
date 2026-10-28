namespace GameOfLife.Grid
{
    /// <summary>What a cell on the board is drawn as; every colour except Custom comes from the current theme palette, Danger marks a pattern ghost that cannot be placed where it is, and Accent is Classic's yellow. Add new paints at the end.</summary>
    public enum CellPaint : byte
    {
        Empty,
        Ink,
        Player,
        Opponent,
        Custom,
        Danger,
        Accent
    }
}

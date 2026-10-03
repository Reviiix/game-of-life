namespace GameOfLife.Simulation
{
    /// <summary>Which side a cell belongs to; None means the cell is dead.</summary>
    public enum CellOwner : byte
    {
        None = 0,
        Player = 1,
        Opponent = 2
    }
}

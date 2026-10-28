namespace GameOfLife.Simulation
{
    /// <summary>Which side, or colour, owns a cell; None means the cell is dead. Versus uses Player and Opponent, and Classic's colours use all four.</summary>
    public enum CellOwner : byte
    {
        None = 0,
        Player = 1,
        Opponent = 2,
        Third = 3,
        Fourth = 4
    }
}

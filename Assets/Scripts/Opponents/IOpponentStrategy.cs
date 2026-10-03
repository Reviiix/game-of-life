using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>Chooses where the app places its next square in versus mode.</summary>
    public interface IOpponentStrategy
    {
        /// <summary>Picks an empty cell inside the region for the given side without changing the grid, or returns false if the region has no empty cell.</summary>
        bool TryChooseCell(CellGrid grid, PlacementRegion region, CellOwner self, out int cellIndex);
    }
}

using System;
using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>The Easy opponent: places on a cell the region allows, chosen uniformly at random.</summary>
    public sealed class RandomOpponentStrategy : IOpponentStrategy
    {
        private const int NoCell = -1;

        private readonly Random random;
        private readonly int[] candidateCellIndices;

        /// <summary>Allocates the candidate buffer for the largest allowed grid up front so choosing never allocates.</summary>
        public RandomOpponentStrategy(Random random, int maximumCellCount)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            candidateCellIndices = new int[maximumCellCount];
        }

        /// <summary>Picks a uniformly random cell the region allows, or returns false if it allows none.</summary>
        public bool TryChooseCell(CellGrid grid, PlacementRegion region, CellOwner self, out int cellIndex)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (grid.CellCount > candidateCellIndices.Length)
            {
                throw new ArgumentException($"A grid of {grid.CellCount} cells does not fit in {candidateCellIndices.Length} cells.", nameof(grid));
            }

            var candidateCount = region.CollectPlaceableCellIndices(grid, candidateCellIndices);
            if (candidateCount == 0)
            {
                cellIndex = NoCell;
                return false;
            }

            cellIndex = candidateCellIndices[random.Next(candidateCount)];
            return true;
        }
    }
}

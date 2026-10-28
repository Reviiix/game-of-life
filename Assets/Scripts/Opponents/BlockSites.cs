using System;
using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>Scores how far a square on a cell gets the side towards a finished 2x2 block (the simplest still life) that has the required empty space around it.</summary>
    public sealed class BlockSites
    {
        private const int BlockSize = 2;
        private const int BlockCellCount = BlockSize * BlockSize;
        private const double CompletionBonus = 1.0;

        private readonly int[] livingPrefixSums;
        private int prefixColumns;

        /// <summary>Allocates the summed-area table up front for any board shape up to the maximum cell count, so preparing never allocates.</summary>
        public BlockSites(int maximumCellCount)
        {
            // (rows + 1) * (columns + 1) is largest for a single row or column, where it is 2 * cells + 2.
            livingPrefixSums = new int[2 * maximumCellCount + 2];
        }

        /// <summary>Builds the summed-area table of living cells in one pass, so any rectangle's living-cell count is four reads.</summary>
        public void Prepare(CellGrid grid)
        {
            if ((long)(grid.Rows + 1) * (grid.Columns + 1) > livingPrefixSums.Length)
            {
                throw new ArgumentException($"A {grid.Rows}x{grid.Columns} grid does not fit the block table.", nameof(grid));
            }

            prefixColumns = grid.Columns + 1;
            for (var column = 0; column < prefixColumns; column++)
            {
                livingPrefixSums[column] = 0;
            }

            for (var row = 0; row < grid.Rows; row++)
            {
                var rowTotal = 0;
                livingPrefixSums[(row + 1) * prefixColumns] = 0;
                for (var column = 0; column < grid.Columns; column++)
                {
                    rowTotal += grid.IsAlive(grid.ToCellIndex(row, column)) ? 1 : 0;
                    livingPrefixSums[(row + 1) * prefixColumns + column + 1] = livingPrefixSums[row * prefixColumns + column + 1] + rowTotal;
                }
            }
        }

        /// <summary>Returns the best block progress for a square on the empty cell: 0.25 per block cell it would make (seed, pair, L), 2 for the cell that finishes a block, or 0 if no block fits inside the region with the clearance empty around it.</summary>
        public double Progress(CellGrid grid, PlacementRegion region, int cellIndex, CellOwner self, int clearance)
        {
            var row = grid.RowOf(cellIndex);
            var column = grid.ColumnOf(cellIndex);
            var firstRow = region.FirstRow;
            var endRow = Math.Min(region.EndRowExclusive, grid.Rows);
            var best = 0.0;
            for (var rowOffset = 0; rowOffset < BlockSize; rowOffset++)
            {
                for (var columnOffset = 0; columnOffset < BlockSize; columnOffset++)
                {
                    var top = row - rowOffset;
                    var left = column - columnOffset;
                    if (top < firstRow || top + BlockSize > endRow || left < 0 || left + BlockSize > grid.Columns)
                    {
                        continue;
                    }

                    var ownCells = CountOwnCells(grid, top, left, self);
                    if (CountLiving(grid, top - clearance, left - clearance, top + BlockSize - 1 + clearance, left + BlockSize - 1 + clearance) != ownCells)
                    {
                        continue;
                    }

                    var blockCells = ownCells + 1;
                    var progress = blockCells / (double)BlockCellCount + (blockCells == BlockCellCount ? CompletionBonus : 0.0);
                    best = Math.Max(best, progress);
                }
            }

            return best;
        }

        /// <summary>Counts the living cells of either side within the given row-or-column distance of the cell.</summary>
        public int CountLivingAround(CellGrid grid, int cellIndex, int distance)
        {
            var row = grid.RowOf(cellIndex);
            var column = grid.ColumnOf(cellIndex);
            return CountLiving(grid, row - distance, column - distance, row + distance, column + distance);
        }

        /// <summary>Counts the side's cells in the 2x2 block with the given top-left cell.</summary>
        private static int CountOwnCells(CellGrid grid, int top, int left, CellOwner self)
        {
            var ownCells = 0;
            for (var row = top; row < top + BlockSize; row++)
            {
                for (var column = left; column < left + BlockSize; column++)
                {
                    ownCells += grid.GetOwner(grid.ToCellIndex(row, column)) == self ? 1 : 0;
                }
            }

            return ownCells;
        }

        /// <summary>Counts living cells in the inclusive rectangle, clipped to the board; space off the board counts as empty.</summary>
        private int CountLiving(CellGrid grid, int top, int left, int bottom, int right)
        {
            top = Math.Max(top, 0);
            left = Math.Max(left, 0);
            bottom = Math.Min(bottom, grid.Rows - 1);
            right = Math.Min(right, grid.Columns - 1);
            if (top > bottom || left > right)
            {
                return 0;
            }

            return livingPrefixSums[(bottom + 1) * prefixColumns + right + 1]
                - livingPrefixSums[top * prefixColumns + right + 1]
                - livingPrefixSums[(bottom + 1) * prefixColumns + left]
                + livingPrefixSums[top * prefixColumns + left];
        }
    }
}

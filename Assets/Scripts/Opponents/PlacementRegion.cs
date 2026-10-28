using System;
using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>A band of whole grid rows, from FirstRow up to but not including EndRowExclusive, where a side may place squares, optionally only next to a living cell.</summary>
    public readonly struct PlacementRegion
    {
        public int FirstRow { get; }
        public int EndRowExclusive { get; }
        public bool RequiresLivingNeighbour { get; }

        /// <summary>Creates a region covering rows from firstRow up to but not including endRowExclusive; requiresLivingNeighbour limits it to empty cells touching a living cell.</summary>
        public PlacementRegion(int firstRow, int endRowExclusive, bool requiresLivingNeighbour = false)
        {
            if (firstRow < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstRow), firstRow, "The first row cannot be negative.");
            }

            if (endRowExclusive < firstRow)
            {
                throw new ArgumentOutOfRangeException(nameof(endRowExclusive), endRowExclusive, "The end row cannot come before the first row.");
            }

            FirstRow = firstRow;
            EndRowExclusive = endRowExclusive;
            RequiresLivingNeighbour = requiresLivingNeighbour;
        }

        /// <summary>Returns a region covering every row of the grid, optionally only next to a living cell.</summary>
        public static PlacementRegion WholeGrid(CellGrid grid, bool requiresLivingNeighbour = false)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            return new PlacementRegion(0, grid.Rows, requiresLivingNeighbour);
        }

        /// <summary>Returns whether the cell lies inside the grid's active area and within this region's rows.</summary>
        public bool Contains(CellGrid grid, int cellIndex)
        {
            return cellIndex >= GetFirstCellIndex(grid) && cellIndex < GetEndCellIndex(grid);
        }

        /// <summary>Returns whether a square may go on the cell: inside this region's rows, empty, and touching a living cell if this region requires it.</summary>
        public bool CanPlaceOn(CellGrid grid, int cellIndex)
        {
            return Contains(grid, cellIndex) && IsPlaceableCell(grid, cellIndex);
        }

        /// <summary>Returns whether at least one cell in this region can take a square.</summary>
        public bool HasPlaceableCell(CellGrid grid)
        {
            var endCellIndex = GetEndCellIndex(grid);
            for (var cellIndex = GetFirstCellIndex(grid); cellIndex < endCellIndex; cellIndex++)
            {
                if (IsPlaceableCell(grid, cellIndex))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns whether this region spans every row of the grid's active area.</summary>
        internal bool CoversWholeGrid(CellGrid grid)
        {
            return FirstRow == 0 && EndRowExclusive >= grid.Rows;
        }

        /// <summary>Writes the index of every cell in this region that can take a square into the buffer in ascending order and returns how many it wrote.</summary>
        internal int CollectPlaceableCellIndices(CellGrid grid, int[] destination)
        {
            var placeableCellCount = 0;
            var endCellIndex = GetEndCellIndex(grid);
            for (var cellIndex = GetFirstCellIndex(grid); cellIndex < endCellIndex; cellIndex++)
            {
                if (IsPlaceableCell(grid, cellIndex))
                {
                    destination[placeableCellCount] = cellIndex;
                    placeableCellCount++;
                }
            }

            return placeableCellCount;
        }

        /// <summary>Returns whether a cell already known to be in this region is empty and, if required, touches a living cell.</summary>
        private bool IsPlaceableCell(CellGrid grid, int cellIndex)
        {
            return !grid.IsAlive(cellIndex) && (!RequiresLivingNeighbour || grid.HasLivingNeighbour(cellIndex));
        }

        /// <summary>Returns the index of the first cell in this region, clamped to the grid's active area, since a band of whole rows is one contiguous run of cell indices.</summary>
        internal int GetFirstCellIndex(CellGrid grid)
        {
            return Math.Min(FirstRow, grid.Rows) * grid.Columns;
        }

        /// <summary>Returns one past the index of the last cell in this region, clamped to the grid's active area.</summary>
        internal int GetEndCellIndex(CellGrid grid)
        {
            return Math.Min(EndRowExclusive, grid.Rows) * grid.Columns;
        }
    }
}

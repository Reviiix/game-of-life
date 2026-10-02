using System.Collections.Generic;

namespace GameOfLife.Simulation
{
    /// <summary>Stores which cells are alive and advances every cell at once using Conway's rules.</summary>
    public sealed class CellGrid
    {
        private const int MaximumNeighboursPerCell = 8;
        private const int NeighboursNeededToSurviveMinimum = 2;
        private const int NeighboursNeededToSurviveMaximum = 3;
        private const int NeighboursNeededToBeBorn = 3;

        private bool[] currentGeneration;
        private bool[] nextGeneration;
        private readonly int[] neighbourIndexTable;
        private readonly int[] neighbourCountTable;
        private readonly List<int> changedCellIndices;

        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public int CellCount => Rows * Columns;
        public IReadOnlyList<int> ChangedCellIndices => changedCellIndices;

        /// <summary>Allocates every buffer for the largest allowed grid up front so resizing never allocates.</summary>
        public CellGrid(int maximumRows, int maximumColumns)
        {
            var maximumCellCount = maximumRows * maximumColumns;
            currentGeneration = new bool[maximumCellCount];
            nextGeneration = new bool[maximumCellCount];
            neighbourIndexTable = new int[maximumCellCount * MaximumNeighboursPerCell];
            neighbourCountTable = new int[maximumCellCount];
            changedCellIndices = new List<int>(maximumCellCount);
        }

        /// <summary>Changes the grid dimensions, kills every cell and rebuilds the neighbour lookup table.</summary>
        public void Resize(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            KillAllCells();
            BuildNeighbourIndexTable();
        }

        /// <summary>Converts a row and column into the flat index used by every other method.</summary>
        public int ToCellIndex(int row, int column)
        {
            return row * Columns + column;
        }

        /// <summary>Returns whether the cell at the given index is alive.</summary>
        public bool IsAlive(int cellIndex)
        {
            return currentGeneration[cellIndex];
        }

        /// <summary>Flips a cell between alive and dead.</summary>
        public void ToggleCell(int cellIndex)
        {
            currentGeneration[cellIndex] = !currentGeneration[cellIndex];
        }

        /// <summary>Kills every cell in the active area of the grid.</summary>
        public void KillAllCells()
        {
            System.Array.Clear(currentGeneration, 0, currentGeneration.Length);
            changedCellIndices.Clear();
        }

        /// <summary>Returns whether at least one cell is alive.</summary>
        public bool HasAnyLivingCell()
        {
            for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            {
                if (currentGeneration[cellIndex])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Calculates the next generation for every cell from the current one, then records which cells changed.</summary>
        public void AdvanceGeneration()
        {
            changedCellIndices.Clear();
            for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            {
                var isAlive = currentGeneration[cellIndex];
                var willBeAlive = WillBeAliveNextGeneration(isAlive, CountLivingNeighbours(cellIndex));
                nextGeneration[cellIndex] = willBeAlive;
                if (willBeAlive != isAlive)
                {
                    changedCellIndices.Add(cellIndex);
                }
            }

            (currentGeneration, nextGeneration) = (nextGeneration, currentGeneration);
        }

        /// <summary>Applies Conway's rules: survive with two or three neighbours, be born with exactly three.</summary>
        private static bool WillBeAliveNextGeneration(bool isAlive, int livingNeighbours)
        {
            if (isAlive)
            {
                return livingNeighbours >= NeighboursNeededToSurviveMinimum && livingNeighbours <= NeighboursNeededToSurviveMaximum;
            }

            return livingNeighbours == NeighboursNeededToBeBorn;
        }

        /// <summary>Counts living neighbours using the precomputed lookup table.</summary>
        private int CountLivingNeighbours(int cellIndex)
        {
            var tableStart = cellIndex * MaximumNeighboursPerCell;
            var tableEnd = tableStart + neighbourCountTable[cellIndex];
            var livingNeighbours = 0;
            for (var tableIndex = tableStart; tableIndex < tableEnd; tableIndex++)
            {
                if (currentGeneration[neighbourIndexTable[tableIndex]])
                {
                    livingNeighbours++;
                }
            }

            return livingNeighbours;
        }

        /// <summary>Precomputes each cell's in-bounds neighbours so a generation needs no bounds checks.</summary>
        private void BuildNeighbourIndexTable()
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var cellIndex = ToCellIndex(row, column);
                    var tableStart = cellIndex * MaximumNeighboursPerCell;
                    var neighbourCount = 0;
                    for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
                    {
                        for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
                        {
                            var neighbourRow = row + rowOffset;
                            var neighbourColumn = column + columnOffset;
                            var isSelf = rowOffset == 0 && columnOffset == 0;
                            var isInsideGrid = neighbourRow >= 0 && neighbourRow < Rows && neighbourColumn >= 0 && neighbourColumn < Columns;
                            if (isSelf || !isInsideGrid)
                            {
                                continue;
                            }

                            neighbourIndexTable[tableStart + neighbourCount] = ToCellIndex(neighbourRow, neighbourColumn);
                            neighbourCount++;
                        }
                    }

                    neighbourCountTable[cellIndex] = neighbourCount;
                }
            }
        }
    }
}

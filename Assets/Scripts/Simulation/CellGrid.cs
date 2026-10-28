using System;
using System.Collections.Generic;

namespace GameOfLife.Simulation
{
    /// <summary>Stores which side or colour owns each cell and advances every cell at once using Conway's rules, with each newborn taking its parents' colour.</summary>
    public sealed class CellGrid
    {
        private const int MaximumNeighboursPerCell = 8;
        private const int NeighboursNeededToSurviveMinimum = 2;
        private const int NeighboursNeededToSurviveMaximum = 3;
        private const int NeighboursNeededToBeBorn = 3;

        private static readonly CellOwner[] LivingOwners = { CellOwner.Player, CellOwner.Opponent, CellOwner.Third, CellOwner.Fourth };

        private readonly CellOwner[] cellOwners;
        private readonly byte[] livingNeighbourCounts;
        private readonly CellOwner[] nextOwners;
        private readonly int[] generationChangeIndices;
        private readonly int[] neighbourIndexTable;
        private readonly int[] neighbourCountTable;
        private readonly List<int> changedCellIndices;
        private readonly bool[] isCellInChangedList;

        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public int CellCount => Rows * Columns;
        public int MaximumCellCount { get; }
        public IReadOnlyList<int> ChangedCellIndices => changedCellIndices;

        /// <summary>Allocates every buffer for the largest allowed grid up front so resizing never allocates.</summary>
        public CellGrid(int maximumRows, int maximumColumns)
        {
            if (maximumRows < 0 || maximumColumns < 0 || (long)maximumRows * maximumColumns * MaximumNeighboursPerCell > int.MaxValue)
            {
                throw new ArgumentException($"A {maximumRows}x{maximumColumns} grid cannot be allocated.");
            }

            MaximumCellCount = maximumRows * maximumColumns;
            cellOwners = new CellOwner[MaximumCellCount];
            livingNeighbourCounts = new byte[MaximumCellCount];
            nextOwners = new CellOwner[MaximumCellCount];
            generationChangeIndices = new int[MaximumCellCount];
            neighbourIndexTable = new int[MaximumCellCount * MaximumNeighboursPerCell];
            neighbourCountTable = new int[MaximumCellCount];
            changedCellIndices = new List<int>(MaximumCellCount);
            isCellInChangedList = new bool[MaximumCellCount];
        }

        /// <summary>Changes the grid dimensions, kills every cell, rebuilds the neighbour lookup table and clears the change list.</summary>
        public void Resize(int rows, int columns)
        {
            if (rows < 0 || columns < 0 || (long)rows * columns > MaximumCellCount)
            {
                throw new ArgumentException($"A {rows}x{columns} grid does not fit in {MaximumCellCount} cells.");
            }

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

        /// <summary>Returns the row of the cell at the given index.</summary>
        public int RowOf(int cellIndex)
        {
            return cellIndex / Columns;
        }

        /// <summary>Returns the column of the cell at the given index.</summary>
        public int ColumnOf(int cellIndex)
        {
            return cellIndex % Columns;
        }

        /// <summary>Returns whether the cell at the given index is alive, whoever owns it.</summary>
        public bool IsAlive(int cellIndex)
        {
            return cellOwners[cellIndex] != CellOwner.None;
        }

        /// <summary>Returns whether any of the cell's 8-way neighbours is alive, whoever owns it.</summary>
        public bool HasLivingNeighbour(int cellIndex)
        {
            var tableStart = cellIndex * MaximumNeighboursPerCell;
            var tableEnd = tableStart + neighbourCountTable[cellIndex];
            for (var tableIndex = tableStart; tableIndex < tableEnd; tableIndex++)
            {
                if (cellOwners[neighbourIndexTable[tableIndex]] != CellOwner.None)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns which side owns the cell at the given index.</summary>
        public CellOwner GetOwner(int cellIndex)
        {
            return cellOwners[cellIndex];
        }

        /// <summary>Gives a cell to a side, or kills it with None, recording a change only if the owner differs.</summary>
        public void SetOwner(int cellIndex, CellOwner owner)
        {
            if (cellOwners[cellIndex] == owner)
            {
                return;
            }

            cellOwners[cellIndex] = owner;
            RecordChange(cellIndex);
        }

        /// <summary>Kills every cell and clears the change list, so callers repaint the whole grid.</summary>
        public void KillAllCells()
        {
            Array.Clear(cellOwners, 0, cellOwners.Length);
            ClearChangedCells();
        }

        /// <summary>Empties the list of changed cells once the caller has repainted them.</summary>
        public void ClearChangedCells()
        {
            for (var listPosition = 0; listPosition < changedCellIndices.Count; listPosition++)
            {
                isCellInChangedList[changedCellIndices[listPosition]] = false;
            }

            changedCellIndices.Clear();
        }

        /// <summary>Returns whether at least one cell is alive.</summary>
        public bool HasAnyLivingCell()
        {
            var cellCount = CellCount;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                if (cellOwners[cellIndex] != CellOwner.None)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns whether living cells of at least two different owners are on the board, the only time clusters can absorb each other.</summary>
        public bool HasLivingCellsOfMoreThanOneOwner()
        {
            var cellCount = CellCount;
            var firstOwner = CellOwner.None;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var owner = cellOwners[cellIndex];
                if (owner == CellOwner.None || owner == firstOwner)
                {
                    continue;
                }

                if (firstOwner != CellOwner.None)
                {
                    return true;
                }

                firstOwner = owner;
            }

            return false;
        }

        /// <summary>Counts the cells in the active area with exactly this owner, so None counts empty cells.</summary>
        public int CountCells(CellOwner owner)
        {
            var cellCount = CellCount;
            var matchingCells = 0;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                if (cellOwners[cellIndex] == owner)
                {
                    matchingCells++;
                }
            }

            return matchingCells;
        }

        /// <summary>Returns how many in-bounds neighbours a cell has: three in a corner, five on an edge, eight inside.</summary>
        public int GetNeighbourCount(int cellIndex)
        {
            return neighbourCountTable[cellIndex];
        }

        /// <summary>Returns the index of one of a cell's neighbours, with the slot in the range [0, GetNeighbourCount).</summary>
        public int GetNeighbourIndex(int cellIndex, int neighbourSlot)
        {
            return neighbourIndexTable[cellIndex * MaximumNeighboursPerCell + neighbourSlot];
        }

        /// <summary>Calculates the next generation for every cell at once: every cell is decided from the current board first, then the changes land together and join the change list.</summary>
        public void AdvanceGeneration()
        {
            var cellCount = CellCount;
            CountLivingNeighboursOfEveryCell(cellCount);
            var generationChangeCount = 0;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var owner = cellOwners[cellIndex];
                var nextOwner = DetermineNextOwner(cellIndex, owner);
                if (nextOwner != owner)
                {
                    nextOwners[cellIndex] = nextOwner;
                    generationChangeIndices[generationChangeCount] = cellIndex;
                    generationChangeCount++;
                }
            }

            for (var changePosition = 0; changePosition < generationChangeCount; changePosition++)
            {
                var cellIndex = generationChangeIndices[changePosition];
                cellOwners[cellIndex] = nextOwners[cellIndex];
                RecordChange(cellIndex);
            }
        }

        /// <summary>Makes this grid an exact copy of another grid's dimensions, cells and neighbour table without recording changes.</summary>
        public void CopyFrom(CellGrid source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var cellCount = source.CellCount;
            if (cellCount > MaximumCellCount)
            {
                throw new ArgumentException($"A grid of {cellCount} cells does not fit in {MaximumCellCount} cells.", nameof(source));
            }

            // The neighbour table depends only on the dimensions, so a grid of the same shape already holds the right one.
            var hasSameShape = Rows == source.Rows && Columns == source.Columns;
            Rows = source.Rows;
            Columns = source.Columns;
            Array.Copy(source.cellOwners, cellOwners, cellCount);
            if (!hasSameShape)
            {
                Array.Copy(source.neighbourIndexTable, neighbourIndexTable, cellCount * MaximumNeighboursPerCell);
                Array.Copy(source.neighbourCountTable, neighbourCountTable, cellCount);
            }

            ClearChangedCells();
        }

        /// <summary>Returns a cell's owner next generation from the current board: survivors keep their colour and newborns take their parents' colour.</summary>
        private CellOwner DetermineNextOwner(int cellIndex, CellOwner owner)
        {
            var isAlive = owner != CellOwner.None;
            if (!WillBeAliveNextGeneration(isAlive, livingNeighbourCounts[cellIndex]))
            {
                return CellOwner.None;
            }

            return isAlive ? owner : ChooseNewbornOwner(cellIndex);
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

        /// <summary>Gives a newborn cell the colour held by at least two of its three parents, or, when all three differ, the one living colour none of them has.</summary>
        private CellOwner ChooseNewbornOwner(int cellIndex)
        {
            var first = CellOwner.None;
            var second = CellOwner.None;
            var third = CellOwner.None;
            var tableStart = cellIndex * MaximumNeighboursPerCell;
            var tableEnd = tableStart + neighbourCountTable[cellIndex];
            for (var tableIndex = tableStart; tableIndex < tableEnd; tableIndex++)
            {
                var parent = cellOwners[neighbourIndexTable[tableIndex]];
                if (parent == CellOwner.None)
                {
                    continue;
                }

                if (first == CellOwner.None)
                {
                    first = parent;
                }
                else if (second == CellOwner.None)
                {
                    second = parent;
                }
                else
                {
                    third = parent;
                }
            }

            if (first == second || first == third)
            {
                return first;
            }

            if (second == third)
            {
                return second;
            }

            foreach (var owner in LivingOwners)
            {
                if (owner != first && owner != second && owner != third)
                {
                    return owner;
                }
            }

            return first;
        }

        /// <summary>Counts every cell's living neighbours, whatever their colour, by adding each living cell to its neighbours' counts, using the lookup table.</summary>
        private void CountLivingNeighboursOfEveryCell(int cellCount)
        {
            Array.Clear(livingNeighbourCounts, 0, cellCount);
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                if (cellOwners[cellIndex] == CellOwner.None)
                {
                    continue;
                }

                var tableStart = cellIndex * MaximumNeighboursPerCell;
                var tableEnd = tableStart + neighbourCountTable[cellIndex];
                for (var tableIndex = tableStart; tableIndex < tableEnd; tableIndex++)
                {
                    livingNeighbourCounts[neighbourIndexTable[tableIndex]]++;
                }
            }
        }

        /// <summary>Adds a cell to the change list unless it is already there.</summary>
        private void RecordChange(int cellIndex)
        {
            if (isCellInChangedList[cellIndex])
            {
                return;
            }

            isCellInChangedList[cellIndex] = true;
            changedCellIndices.Add(cellIndex);
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

using System;
using System.Collections.Generic;
using GameOfLife.Simulation;

namespace GameOfLife.Grid
{
    /// <summary>Remembers the cells placed while a game runs and counts down the generations each stays fresh, so the board draws them in a lighter variant of their colour until they settle; a cell stops being fresh as soon as it dies or changes owner.</summary>
    public sealed class FreshCellTracker
    {
        private const int NotListed = -1;

        private readonly int[] generationsLeft;
        private readonly CellOwner[] placedOwners;
        private readonly int[] freshCellIndices;
        private readonly int[] listPositions;
        private readonly List<int> settledCellIndices;
        private int freshCellCount;

        /// <summary>How many generations a placed cell stays fresh; 0 means placed cells are never fresh.</summary>
        public int FreshGenerations { get; }

        /// <summary>How many cells are fresh right now.</summary>
        public int FreshCellCount => freshCellCount;

        /// <summary>The cells whose fresh generations ran out in the last AdvanceGeneration, still owned by whoever placed them.</summary>
        public IReadOnlyList<int> SettledCellIndices => settledCellIndices;

        /// <summary>Allocates per-cell state for the largest board up front so tracking never allocates.</summary>
        public FreshCellTracker(int maximumCellCount, int freshGenerations)
        {
            if (maximumCellCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCellCount), maximumCellCount, "The cell count cannot be negative.");
            }

            if (freshGenerations < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(freshGenerations), freshGenerations, "The fresh generations cannot be negative.");
            }

            FreshGenerations = freshGenerations;
            generationsLeft = new int[maximumCellCount];
            placedOwners = new CellOwner[maximumCellCount];
            freshCellIndices = new int[maximumCellCount];
            listPositions = new int[maximumCellCount];
            settledCellIndices = new List<int>(maximumCellCount);
            for (var cellIndex = 0; cellIndex < maximumCellCount; cellIndex++)
            {
                listPositions[cellIndex] = NotListed;
            }
        }

        /// <summary>Makes a cell fresh for the full number of generations on behalf of the side that placed it, restarting its count if it was already fresh.</summary>
        public void Mark(int cellIndex, CellOwner placedOwner)
        {
            if (FreshGenerations == 0 || placedOwner == CellOwner.None)
            {
                Forget(cellIndex);
                return;
            }

            generationsLeft[cellIndex] = FreshGenerations;
            placedOwners[cellIndex] = placedOwner;
            if (listPositions[cellIndex] == NotListed)
            {
                listPositions[cellIndex] = freshCellCount;
                freshCellIndices[freshCellCount] = cellIndex;
                freshCellCount++;
            }
        }

        /// <summary>Returns whether a cell is fresh and still owned by the side that placed it.</summary>
        public bool IsFresh(int cellIndex, CellOwner currentOwner)
        {
            return listPositions[cellIndex] != NotListed && placedOwners[cellIndex] == currentOwner;
        }

        /// <summary>Stops a cell being fresh once it has died or been absorbed, so a later cell in its place starts plain.</summary>
        public void ForgetIfOwnerChanged(int cellIndex, CellOwner currentOwner)
        {
            if (listPositions[cellIndex] != NotListed && placedOwners[cellIndex] != currentOwner)
            {
                Forget(cellIndex);
            }
        }

        /// <summary>Counts one generation off every fresh cell and lists the ones that have now settled in SettledCellIndices.</summary>
        public void AdvanceGeneration()
        {
            settledCellIndices.Clear();
            for (var listPosition = freshCellCount - 1; listPosition >= 0; listPosition--)
            {
                var cellIndex = freshCellIndices[listPosition];
                generationsLeft[cellIndex]--;
                if (generationsLeft[cellIndex] > 0)
                {
                    continue;
                }

                Forget(cellIndex);
                settledCellIndices.Add(cellIndex);
            }
        }

        /// <summary>Forgets every fresh cell, for a new or resized board.</summary>
        public void Clear()
        {
            for (var listPosition = 0; listPosition < freshCellCount; listPosition++)
            {
                listPositions[freshCellIndices[listPosition]] = NotListed;
            }

            freshCellCount = 0;
            settledCellIndices.Clear();
        }

        /// <summary>Removes a cell from the fresh list by moving the last entry into its place.</summary>
        private void Forget(int cellIndex)
        {
            var listPosition = listPositions[cellIndex];
            if (listPosition == NotListed)
            {
                return;
            }

            freshCellCount--;
            var lastCellIndex = freshCellIndices[freshCellCount];
            freshCellIndices[listPosition] = lastCellIndex;
            listPositions[lastCellIndex] = listPosition;
            listPositions[cellIndex] = NotListed;
        }
    }
}

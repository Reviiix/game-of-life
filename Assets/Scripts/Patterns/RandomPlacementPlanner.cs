using System;
using System.Collections.Generic;
using GameOfLife.Simulation;

namespace GameOfLife.Patterns
{
    /// <summary>Plans a random placement: empty cells in the allowed rows, sometimes including whole catalogue patterns in random poses with a clear border so they keep their shape, and the rest as scattered single cells. Every buffer is allocated once, so planning never allocates.</summary>
    public sealed class RandomPlacementPlanner
    {
        private const int PoseCount = 8;
        private const int OriginAttemptsPerPattern = 16;

        private readonly CellPattern[] patterns;
        private readonly Random random;
        private readonly int cellsPerPatternChance;
        private readonly double patternChance;
        private readonly int[] candidateCells;
        private readonly bool[] isTaken;
        private readonly bool[] isReserved;
        private readonly int[] fittingPatternIndices;
        private readonly List<int> plannedCells;

        /// <summary>The cells the last plan chose, each listed once: whole patterns first, then single cells.</summary>
        public IReadOnlyList<int> PlannedCells => plannedCells;

        /// <summary>How many whole catalogue patterns the last plan included.</summary>
        public int PlannedPatternCount { get; private set; }

        /// <summary>Copies the catalogue and allocates every buffer for the largest board; each plan gets one chance to include a pattern per the given number of cells, taken with the given probability.</summary>
        public RandomPlacementPlanner(int maximumCellCount, IReadOnlyList<CellPattern> cataloguePatterns, Random randomSource, int cellsPerPatternChance, double patternChance)
        {
            if (maximumCellCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCellCount), maximumCellCount, "A board cannot have a negative number of cells.");
            }

            if (cellsPerPatternChance < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(cellsPerPatternChance), cellsPerPatternChance, "There must be at least one cell per pattern chance.");
            }

            if (patternChance < 0d || patternChance > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(patternChance), patternChance, "A chance must lie between 0 and 1.");
            }

            if (cataloguePatterns == null)
            {
                throw new ArgumentNullException(nameof(cataloguePatterns));
            }

            patterns = new CellPattern[cataloguePatterns.Count];
            for (var index = 0; index < patterns.Length; index++)
            {
                patterns[index] = cataloguePatterns[index] ?? throw new ArgumentException($"Catalogue pattern {index} is missing.", nameof(cataloguePatterns));
            }

            random = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            this.cellsPerPatternChance = cellsPerPatternChance;
            this.patternChance = patternChance;
            candidateCells = new int[maximumCellCount];
            isTaken = new bool[maximumCellCount];
            isReserved = new bool[maximumCellCount];
            fittingPatternIndices = new int[patterns.Length];
            plannedCells = new List<int>(maximumCellCount);
        }

        /// <summary>Returns whether any cell inside the bounds' rows is empty, so a random placement could place something; stops at the first one found.</summary>
        public static bool HasEmptyCell(CellGrid grid, in PlacementBounds bounds)
        {
            for (var row = bounds.FirstRow; row < bounds.EndRowExclusive; row++)
            {
                for (var column = 0; column < bounds.Columns; column++)
                {
                    if (!grid.IsAlive(grid.ToCellIndex(row, column)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Chooses up to the given number of empty cells inside the bounds' rows, or every empty cell there when there are fewer, and returns how many it chose; read them from PlannedCells.</summary>
        public int Plan(CellGrid grid, in PlacementBounds bounds, int cellBudget)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (bounds.BoardRows != grid.Rows || bounds.Columns != grid.Columns)
            {
                throw new ArgumentException($"Bounds for a {bounds.BoardRows}x{bounds.Columns} board do not match a {grid.Rows}x{grid.Columns} grid.", nameof(bounds));
            }

            if (grid.CellCount > candidateCells.Length)
            {
                throw new ArgumentException($"A grid of {grid.CellCount} cells does not fit in {candidateCells.Length} cells.", nameof(grid));
            }

            plannedCells.Clear();
            PlannedPatternCount = 0;
            var candidateCount = MarkLivingCellsAndListEmptyCells(grid, bounds);
            var remaining = Math.Max(0, Math.Min(cellBudget, candidateCount));
            var patternChances = remaining / cellsPerPatternChance;
            for (var chance = 0; chance < patternChances && remaining > 0; chance++)
            {
                if (random.NextDouble() < patternChance)
                {
                    remaining -= TryPlanPattern(grid, bounds, remaining);
                }
            }

            ShuffleCandidates(candidateCount);
            remaining = PlanSingleCells(candidateCount, remaining, false);
            PlanSingleCells(candidateCount, remaining, true);
            return plannedCells.Count;
        }

        /// <summary>Marks every living cell as taken, clears every pattern border, and lists the empty cells inside the bounds' rows, returning how many there are.</summary>
        private int MarkLivingCellsAndListEmptyCells(CellGrid grid, in PlacementBounds bounds)
        {
            var cellCount = grid.CellCount;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                isTaken[cellIndex] = grid.IsAlive(cellIndex);
                isReserved[cellIndex] = false;
            }

            var candidateCount = 0;
            for (var row = bounds.FirstRow; row < bounds.EndRowExclusive; row++)
            {
                for (var column = 0; column < bounds.Columns; column++)
                {
                    var cellIndex = grid.ToCellIndex(row, column);
                    if (!isTaken[cellIndex])
                    {
                        candidateCells[candidateCount] = cellIndex;
                        candidateCount++;
                    }
                }
            }

            return candidateCount;
        }

        /// <summary>Tries to plan one randomly chosen pattern that fits the remaining cells, in a random pose and at a random spot with no living or planned cell touching it, and returns how many cells it used.</summary>
        private int TryPlanPattern(CellGrid grid, in PlacementBounds bounds, int remaining)
        {
            var fittingCount = 0;
            for (var index = 0; index < patterns.Length; index++)
            {
                var pattern = patterns[index];
                if (pattern.CellCount <= remaining && pattern.FitsAnyOrientation(bounds.RowCount, bounds.Columns))
                {
                    fittingPatternIndices[fittingCount] = index;
                    fittingCount++;
                }
            }

            if (fittingCount == 0)
            {
                return 0;
            }

            var chosen = patterns[fittingPatternIndices[random.Next(fittingCount)]];
            var pose = new PatternPose(random.Next(PoseCount / 2), random.Next(2) == 1);
            var orientation = chosen.GetOrientation(pose);
            if (!orientation.Fits(bounds))
            {
                orientation = chosen.GetOrientation(pose.RotatedClockwise());
            }

            for (var attempt = 0; attempt < OriginAttemptsPerPattern; attempt++)
            {
                var originRow = bounds.FirstRow + random.Next(bounds.RowCount - orientation.Rows + 1);
                var originColumn = random.Next(bounds.Columns - orientation.Columns + 1);
                if (HasRoomFor(grid, orientation, originRow, originColumn))
                {
                    TakePattern(grid, orientation, originRow, originColumn);
                    PlannedPatternCount++;
                    return orientation.CellCount;
                }
            }

            return 0;
        }

        /// <summary>Returns whether every cell of the orientation at the origin is free and no living or planned cell touches it, so it keeps its shape; a cell reserved around another pattern always touches that pattern, so this also keeps patterns apart.</summary>
        private bool HasRoomFor(CellGrid grid, PatternOrientation orientation, int originRow, int originColumn)
        {
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                var cellIndex = grid.ToCellIndex(originRow + orientation.GetRowOffset(cell), originColumn + orientation.GetColumnOffset(cell));
                if (isTaken[cellIndex])
                {
                    return false;
                }

                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var slot = 0; slot < neighbourCount; slot++)
                {
                    if (isTaken[grid.GetNeighbourIndex(cellIndex, slot)])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Plans the orientation's cells and reserves the cells around them, so single cells keep clear of it while they can.</summary>
        private void TakePattern(CellGrid grid, PatternOrientation orientation, int originRow, int originColumn)
        {
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                var cellIndex = grid.ToCellIndex(originRow + orientation.GetRowOffset(cell), originColumn + orientation.GetColumnOffset(cell));
                isTaken[cellIndex] = true;
                plannedCells.Add(cellIndex);
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var slot = 0; slot < neighbourCount; slot++)
                {
                    isReserved[grid.GetNeighbourIndex(cellIndex, slot)] = true;
                }
            }
        }

        /// <summary>Puts the candidate cells in a random order (Fisher-Yates).</summary>
        private void ShuffleCandidates(int candidateCount)
        {
            for (var position = candidateCount - 1; position > 0; position--)
            {
                var swapWith = random.Next(position + 1);
                (candidateCells[position], candidateCells[swapWith]) = (candidateCells[swapWith], candidateCells[position]);
            }
        }

        /// <summary>Plans free candidate cells in their shuffled order until none remain to place, skipping pattern borders unless they are allowed, and returns how many are still to place.</summary>
        private int PlanSingleCells(int candidateCount, int remaining, bool mayTouchPatterns)
        {
            for (var position = 0; position < candidateCount && remaining > 0; position++)
            {
                var cellIndex = candidateCells[position];
                if (isTaken[cellIndex] || (isReserved[cellIndex] && !mayTouchPatterns))
                {
                    continue;
                }

                isTaken[cellIndex] = true;
                plannedCells.Add(cellIndex);
                remaining--;
            }

            return remaining;
        }
    }
}

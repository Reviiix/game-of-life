using System;
using System.Collections.Generic;
using GameOfLife.Simulation;

namespace GameOfLife.Patterns
{
    /// <summary>Chooses a catalogue Pulsate pattern to run behind the main menu: one that, placed centred across the board at a chosen height, as drawn or turned a quarter, keeps pulsating there forever, preferring one other than the last shown; allocates its buffers once.</summary>
    public sealed class PulsatingFormationPicker
    {
        private const int MaximumPeriodChecked = 30;
        private const int NoPattern = -1;

        private readonly IReadOnlyList<CellPattern> patterns;
        private readonly CellGrid scratchBoard;
        private readonly List<int> plannedCells;
        private readonly List<int> candidatePatterns;
        private readonly List<PatternPose> candidatePoses;

        /// <summary>The cells of the last plan, on the board it was made for.</summary>
        public IReadOnlyList<int> PlannedCells => plannedCells;

        /// <summary>The catalogue index of the pattern in the last plan, or -1 when nothing was planned.</summary>
        public int PlannedPatternIndex { get; private set; } = NoPattern;

        /// <summary>Keeps the catalogue and allocates a scratch board of the largest size and every list at its largest.</summary>
        public PulsatingFormationPicker(IReadOnlyList<CellPattern> cataloguePatterns, int maximumRows, int maximumColumns)
        {
            patterns = cataloguePatterns ?? throw new ArgumentNullException(nameof(cataloguePatterns));
            scratchBoard = new CellGrid(maximumRows, maximumColumns);
            var largestCellCount = 0;
            for (var index = 0; index < patterns.Count; index++)
            {
                largestCellCount = Math.Max(largestCellCount, patterns[index].CellCount);
            }

            plannedCells = new List<int>(largestCellCount);
            candidatePatterns = new List<int>(patterns.Count);
            candidatePoses = new List<PatternPose>(patterns.Count);
        }

        /// <summary>Plans a pulsating pattern centred across a board of the given size, its middle at the given fraction of the height from the top, avoiding the given catalogue index when another pattern also works; returns false, planning nothing, when no Pulsate pattern keeps pulsating there.</summary>
        public bool TryPlan(int rows, int columns, float verticalCentre, int avoidedPatternIndex, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            plannedCells.Clear();
            PlannedPatternIndex = NoPattern;
            FindCandidates(rows, columns, verticalCentre);
            if (candidatePatterns.Count == 0)
            {
                return false;
            }

            var avoidedPosition = candidatePatterns.IndexOf(avoidedPatternIndex);
            var canAvoid = avoidedPosition >= 0 && candidatePatterns.Count > 1;
            var position = random.Next(canAvoid ? candidatePatterns.Count - 1 : candidatePatterns.Count);
            if (canAvoid && position >= avoidedPosition)
            {
                position++;
            }

            PlannedPatternIndex = candidatePatterns[position];
            WritePlacedCells(patterns[PlannedPatternIndex].GetOrientation(candidatePoses[position]), rows, columns, verticalCentre, plannedCells);
            return true;
        }

        /// <summary>Lists every Pulsate pattern, with the first of its upright or quarter-turned poses that keeps pulsating where it would be placed.</summary>
        private void FindCandidates(int rows, int columns, float verticalCentre)
        {
            candidatePatterns.Clear();
            candidatePoses.Clear();
            var turned = PatternPose.Upright.RotatedClockwise();
            for (var index = 0; index < patterns.Count; index++)
            {
                var pattern = patterns[index];
                if (pattern.Category != PatternCategory.Pulsate)
                {
                    continue;
                }

                if (KeepsPulsating(pattern.Upright, rows, columns, verticalCentre))
                {
                    candidatePatterns.Add(index);
                    candidatePoses.Add(PatternPose.Upright);
                }
                else if (KeepsPulsating(pattern.GetOrientation(turned), rows, columns, verticalCentre))
                {
                    candidatePatterns.Add(index);
                    candidatePoses.Add(turned);
                }
            }
        }

        /// <summary>Returns whether the shape, placed on a board of the given size, changes and then returns exactly to its start within the checked generations, so it pulsates forever there.</summary>
        private bool KeepsPulsating(PatternOrientation orientation, int rows, int columns, float verticalCentre)
        {
            if (!orientation.Fits(rows, columns))
            {
                return false;
            }

            scratchBoard.Resize(rows, columns);
            WritePlacedCells(orientation, rows, columns, verticalCentre, plannedCells);
            for (var position = 0; position < plannedCells.Count; position++)
            {
                scratchBoard.SetOwner(plannedCells[position], CellOwner.Player);
            }

            for (var generation = 1; generation <= MaximumPeriodChecked; generation++)
            {
                scratchBoard.AdvanceGeneration();
                scratchBoard.ClearChangedCells();
                if (!scratchBoard.HasAnyLivingCell())
                {
                    return false;
                }

                if (IsBackAtStart())
                {
                    return generation > 1;
                }
            }

            return false;
        }

        /// <summary>Returns whether exactly the planned cells are alive on the scratch board.</summary>
        private bool IsBackAtStart()
        {
            if (scratchBoard.CountCells(CellOwner.Player) != plannedCells.Count)
            {
                return false;
            }

            for (var position = 0; position < plannedCells.Count; position++)
            {
                if (!scratchBoard.IsAlive(plannedCells[position]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Fills the list with the shape's cells centred across the board, its middle at the given fraction of the height from the top, kept inside the board.</summary>
        private static void WritePlacedCells(PatternOrientation orientation, int rows, int columns, float verticalCentre, List<int> cells)
        {
            cells.Clear();
            var firstRow = Math.Clamp((int)Math.Floor(verticalCentre * rows - orientation.Rows / 2f), 0, rows - orientation.Rows);
            var firstColumn = (columns - orientation.Columns) / 2;
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                cells.Add((firstRow + orientation.GetRowOffset(cell)) * columns + firstColumn + orientation.GetColumnOffset(cell));
            }
        }
    }
}

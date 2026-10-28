using System;
using System.Collections.Generic;

namespace GameOfLife.Patterns
{
    /// <summary>Positions one pattern on the board: centres it on the first press, lets a drag carry it, keeps it inside its bounds, turns and mirrors it, and asks the rules whether it may be placed; allocates one list up front and nothing afterwards.</summary>
    public sealed class PatternPlacementSession
    {
        private readonly List<int> cellIndices;
        private IPatternPlacementRules rules;
        private int grabRowOffset;
        private int grabColumnOffset;

        public bool IsActive { get; private set; }
        public bool HasGhost { get; private set; }
        public bool IsValid { get; private set; }
        public CellPattern Pattern { get; private set; }
        public PatternPose Pose { get; private set; }
        public PlacementBounds Bounds { get; private set; }
        public int OriginRow { get; private set; }
        public int OriginColumn { get; private set; }

        /// <summary>Whether a quarter turn would still fit the bounds.</summary>
        public bool CanRotate => IsActive && Pattern.GetOrientation(Pose.RotatedClockwise()).Fits(Bounds);

        /// <summary>The shape in the current pose, or null between sessions.</summary>
        public PatternOrientation Orientation => Pattern?.GetOrientation(Pose);

        /// <summary>The board cells the ghost covers, in the pattern's cell order; empty until the first press.</summary>
        public IReadOnlyList<int> CellIndices => cellIndices;

        /// <summary>Allocates room for the largest pattern's cells.</summary>
        public PatternPlacementSession(int maximumPatternCells)
        {
            cellIndices = new List<int>(maximumPatternCells);
        }

        /// <summary>Starts positioning a pattern within the bounds in the first pose that fits, with no ghost until the board is pressed; throws if the pattern cannot fit.</summary>
        public void Begin(CellPattern pattern, in PlacementBounds bounds, IPatternPlacementRules placementRules)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (!pattern.CanFit(bounds) || !pattern.TryGetFirstFittingPose(bounds, out var pose))
            {
                throw new InvalidOperationException($"The {pattern.Name} cannot fit {bounds.RowCount} rows by {bounds.Columns} columns with {bounds.SquaresAvailable} squares.");
            }

            rules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
            Pattern = pattern;
            Bounds = bounds;
            Pose = pose;
            IsActive = true;
            HasGhost = false;
            IsValid = false;
            cellIndices.Clear();
        }

        /// <summary>Handles a press on a cell: grabs the ghost where it was pressed, or centres the pattern on the cell; returns whether the ghost moved or appeared.</summary>
        public bool Press(int row, int column)
        {
            if (!IsActive)
            {
                return false;
            }

            if (HasGhost && IsInsideGhost(row, column))
            {
                grabRowOffset = row - OriginRow;
                grabColumnOffset = column - OriginColumn;
                return false;
            }

            return CentreOn(row, column);
        }

        /// <summary>Handles a drag onto a cell: keeps the grabbed cell under the finger, within the bounds; returns whether the ghost moved or appeared.</summary>
        public bool Drag(int row, int column)
        {
            if (!IsActive)
            {
                return false;
            }

            if (!HasGhost)
            {
                return CentreOn(row, column);
            }

            var newRow = ClampRow(row - grabRowOffset);
            var newColumn = ClampColumn(column - grabColumnOffset);
            if (newRow == OriginRow && newColumn == OriginColumn)
            {
                return false;
            }

            OriginRow = newRow;
            OriginColumn = newColumn;
            Rebuild();
            return true;
        }

        /// <summary>Turns the pattern a quarter turn clockwise about its centre cell, keeping it within the bounds; refused when the turned shape would not fit.</summary>
        public bool RotateClockwise()
        {
            if (!CanRotate)
            {
                return false;
            }

            var centreRow = OriginRow + CentreRowOffset(Orientation);
            var centreColumn = OriginColumn + CentreColumnOffset(Orientation);
            Pose = Pose.RotatedClockwise();
            if (!HasGhost)
            {
                return true;
            }

            grabRowOffset = CentreRowOffset(Orientation);
            grabColumnOffset = CentreColumnOffset(Orientation);
            OriginRow = ClampRow(centreRow - grabRowOffset);
            OriginColumn = ClampColumn(centreColumn - grabColumnOffset);
            Rebuild();
            return true;
        }

        /// <summary>Mirrors the shown shape left to right in place.</summary>
        public bool Mirror()
        {
            if (!IsActive)
            {
                return false;
            }

            Pose = Pose.Mirrored();
            if (HasGhost)
            {
                Rebuild();
            }

            return true;
        }

        /// <summary>Asks the rules to place the ghost's cells; ends the session when they do, else re-checks validity and keeps positioning.</summary>
        public bool TryCommit()
        {
            if (!IsActive || !HasGhost)
            {
                return false;
            }

            if (rules.TryPlacePattern(cellIndices))
            {
                End();
                return true;
            }

            IsValid = rules.CanPlacePattern(cellIndices);
            return false;
        }

        /// <summary>Ends the session without placing anything.</summary>
        public void Cancel()
        {
            End();
        }

        /// <summary>Puts the pattern's centre cell on the pressed cell, clamped inside the bounds, and shows the ghost.</summary>
        private bool CentreOn(int row, int column)
        {
            grabRowOffset = CentreRowOffset(Orientation);
            grabColumnOffset = CentreColumnOffset(Orientation);
            OriginRow = ClampRow(row - grabRowOffset);
            OriginColumn = ClampColumn(column - grabColumnOffset);
            HasGhost = true;
            Rebuild();
            return true;
        }

        /// <summary>Returns whether the cell lies inside the ghost's box.</summary>
        private bool IsInsideGhost(int row, int column)
        {
            return row >= OriginRow && row < OriginRow + Orientation.Rows
                && column >= OriginColumn && column < OriginColumn + Orientation.Columns;
        }

        /// <summary>Keeps the ghost's top row inside the band.</summary>
        private int ClampRow(int row)
        {
            return Math.Max(Bounds.FirstRow, Math.Min(row, Bounds.EndRowExclusive - Orientation.Rows));
        }

        /// <summary>Keeps the ghost's left column inside the board.</summary>
        private int ClampColumn(int column)
        {
            return Math.Max(0, Math.Min(column, Bounds.Columns - Orientation.Columns));
        }

        /// <summary>Recomputes the ghost's cell indices from the origin and pose, and asks the rules whether they may be placed.</summary>
        private void Rebuild()
        {
            cellIndices.Clear();
            var orientation = Orientation;
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                cellIndices.Add((OriginRow + orientation.GetRowOffset(cell)) * Bounds.Columns + OriginColumn + orientation.GetColumnOffset(cell));
            }

            IsValid = rules.CanPlacePattern(cellIndices);
        }

        /// <summary>Forgets the pattern, the ghost and the rules.</summary>
        private void End()
        {
            IsActive = false;
            HasGhost = false;
            IsValid = false;
            cellIndices.Clear();
            Pattern = null;
            rules = null;
        }

        /// <summary>The row of a shape's centre cell, rounding towards the top for even heights.</summary>
        private static int CentreRowOffset(PatternOrientation orientation)
        {
            return (orientation.Rows - 1) / 2;
        }

        /// <summary>The column of a shape's centre cell, rounding towards the left for even widths.</summary>
        private static int CentreColumnOffset(PatternOrientation orientation)
        {
            return (orientation.Columns - 1) / 2;
        }
    }
}

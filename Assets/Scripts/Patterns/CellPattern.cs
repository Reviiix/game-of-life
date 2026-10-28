using System;
using System.Collections.Generic;

namespace GameOfLife.Patterns
{
    /// <summary>One catalogue pattern: its name, description and group, with all eight turned and mirrored orientations built once so positioning never computes them.</summary>
    public sealed class CellPattern
    {
        private const int PoseCount = 8;

        private readonly PatternOrientation[] orientations;

        public string Name { get; }
        public string Description { get; }
        public PatternCategory Category { get; }

        /// <summary>How many living cells the pattern has, the same in every orientation.</summary>
        public int CellCount => Upright.CellCount;

        /// <summary>The pattern exactly as drawn.</summary>
        public PatternOrientation Upright => orientations[0];

        /// <summary>Builds every orientation from the upright shape: mirrored first when the pose says so, then turned its quarter turns.</summary>
        private CellPattern(string name, string description, PatternCategory category, PatternOrientation upright)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Category = category;
            orientations = new PatternOrientation[PoseCount];
            for (var index = 0; index < PoseCount; index++)
            {
                var pose = new PatternPose(index / 2, index % 2 == 1);
                var shape = pose.IsMirrored ? upright.MirroredHorizontally() : upright;
                for (var turn = 0; turn < pose.QuarterTurns; turn++)
                {
                    shape = shape.RotatedClockwise();
                }

                orientations[pose.Index] = shape;
            }
        }

        /// <summary>Returns the shape shown in a pose.</summary>
        public PatternOrientation GetOrientation(PatternPose pose)
        {
            return orientations[pose.Index];
        }

        /// <summary>Returns whether the pattern fits an area of the given size either as drawn or turned a quarter turn; the other poses have the same two box sizes.</summary>
        public bool FitsAnyOrientation(int rows, int columns)
        {
            return Upright.Fits(rows, columns) || GetOrientation(PatternPose.Upright.RotatedClockwise()).Fits(rows, columns);
        }

        /// <summary>Returns whether the pattern can be placed within the bounds: enough squares, and some orientation fits the band.</summary>
        public bool CanFit(in PlacementBounds bounds)
        {
            return CellCount <= bounds.SquaresAvailable && FitsAnyOrientation(bounds.RowCount, bounds.Columns);
        }

        /// <summary>Returns why the pattern cannot be placed within the bounds, checking size before squares, or None when it can.</summary>
        public PatternUnavailableReason GetUnavailableReason(in PlacementBounds bounds)
        {
            if (!FitsAnyOrientation(bounds.RowCount, bounds.Columns))
            {
                return bounds.IsWholeBoard || !FitsAnyOrientation(bounds.BoardRows, bounds.Columns)
                    ? PatternUnavailableReason.TooBigForBoard
                    : PatternUnavailableReason.TooBigForHalf;
            }

            return CellCount > bounds.SquaresAvailable ? PatternUnavailableReason.NotEnoughSquares : PatternUnavailableReason.None;
        }

        /// <summary>Finds the pose to start positioning in: upright if it fits the bounds, else turned once; false when neither fits.</summary>
        public bool TryGetFirstFittingPose(in PlacementBounds bounds, out PatternPose pose)
        {
            pose = PatternPose.Upright;
            if (Upright.Fits(bounds))
            {
                return true;
            }

            pose = pose.RotatedClockwise();
            return GetOrientation(pose).Fits(bounds);
        }

        /// <summary>Builds a pattern from rows of '.' and 'O', top row first.</summary>
        public static CellPattern FromRows(string name, string description, PatternCategory category, IReadOnlyList<string> rows)
        {
            return new CellPattern(name, description, category, PatternOrientation.FromRows(rows));
        }
    }
}

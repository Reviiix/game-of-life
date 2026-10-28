namespace GameOfLife.Patterns
{
    /// <summary>One of the eight ways a pattern can be turned and mirrored; rotating and mirroring always act on the shape the player sees.</summary>
    public readonly struct PatternPose
    {
        private const int QuarterTurnsPerFullTurn = 4;

        public int QuarterTurns { get; }
        public bool IsMirrored { get; }

        /// <summary>The pose that shows a pattern exactly as drawn.</summary>
        public static PatternPose Upright => default;

        /// <summary>The index of this pose's orientation in a pattern's table: two per quarter turn, the mirrored one second.</summary>
        public int Index => QuarterTurns * 2 + (IsMirrored ? 1 : 0);

        /// <summary>Builds a pose from a number of clockwise quarter turns, wrapped to a full turn, and whether the shape is mirrored first.</summary>
        public PatternPose(int quarterTurns, bool isMirrored)
        {
            QuarterTurns = (quarterTurns % QuarterTurnsPerFullTurn + QuarterTurnsPerFullTurn) % QuarterTurnsPerFullTurn;
            IsMirrored = isMirrored;
        }

        /// <summary>Returns the pose after turning the shown shape a quarter turn clockwise.</summary>
        public PatternPose RotatedClockwise()
        {
            return new PatternPose(QuarterTurns + 1, IsMirrored);
        }

        /// <summary>Returns the pose after mirroring the shown shape left to right; mirroring turns the rotation the other way, hence the subtraction.</summary>
        public PatternPose Mirrored()
        {
            return new PatternPose((QuarterTurnsPerFullTurn - QuarterTurns) % QuarterTurnsPerFullTurn, !IsMirrored);
        }
    }
}

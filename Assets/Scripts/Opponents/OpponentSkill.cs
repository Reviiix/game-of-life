using System;

namespace GameOfLife.Opponents
{
    /// <summary>How well a SurvivalOpponentStrategy plays: the breathing room it leaves around its blocks, how noisy its choices are, and how far ahead it looks.</summary>
    public readonly struct OpponentSkill
    {
        /// <summary>Medium: builds blocks with one empty cell around them, plays noisily and never looks ahead.</summary>
        public static readonly OpponentSkill Medium = new(1, 1.0, 0, 0);

        /// <summary>Hard: builds blocks with two empty cells around them, plays precisely and checks its best match moves five generations ahead.</summary>
        public static readonly OpponentSkill Hard = new(2, 0.25, 8, 5);

        public int BlockClearance { get; }
        public double MaximumScoreNoise { get; }
        public int LookaheadCandidates { get; }
        public int LookaheadGenerations { get; }

        /// <summary>Creates a skill; every value must be zero or more, and a lookahead needs at least one generation.</summary>
        public OpponentSkill(int blockClearance, double maximumScoreNoise, int lookaheadCandidates, int lookaheadGenerations)
        {
            if (blockClearance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockClearance), blockClearance, "The block clearance cannot be negative.");
            }

            if (maximumScoreNoise < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumScoreNoise), maximumScoreNoise, "The score noise cannot be negative.");
            }

            if (lookaheadCandidates < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lookaheadCandidates), lookaheadCandidates, "The lookahead candidates cannot be negative.");
            }

            if (lookaheadGenerations < 0 || lookaheadCandidates > 0 && lookaheadGenerations == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lookaheadGenerations), lookaheadGenerations, "A lookahead needs at least one generation, and the count cannot be negative.");
            }

            BlockClearance = blockClearance;
            MaximumScoreNoise = maximumScoreNoise;
            LookaheadCandidates = lookaheadCandidates;
            LookaheadGenerations = lookaheadGenerations;
        }
    }
}

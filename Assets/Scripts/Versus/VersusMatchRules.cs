using System;

namespace GameOfLife.Versus
{
    /// <summary>How many squares each side may place during setup and, separately, during the match.</summary>
    public readonly struct VersusMatchRules
    {
        public int SetupSquaresEach { get; }
        public int MatchSquaresEach { get; }

        /// <summary>Creates rules with the given square pools for each side, neither of which may be negative.</summary>
        public VersusMatchRules(int setupSquaresEach, int matchSquaresEach)
        {
            if (setupSquaresEach < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(setupSquaresEach), setupSquaresEach, "The setup squares for each side cannot be negative.");
            }

            if (matchSquaresEach < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchSquaresEach), matchSquaresEach, "The match squares for each side cannot be negative.");
            }

            SetupSquaresEach = setupSquaresEach;
            MatchSquaresEach = matchSquaresEach;
        }
    }
}

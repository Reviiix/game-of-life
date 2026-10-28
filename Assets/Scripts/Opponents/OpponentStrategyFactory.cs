using System;

namespace GameOfLife.Opponents
{
    /// <summary>Creates the opponent strategy that plays at a given difficulty: random for Easy, survival-minded for Medium and Hard.</summary>
    public static class OpponentStrategyFactory
    {
        /// <summary>Returns a strategy for the difficulty with every buffer sized for the largest allowed grid.</summary>
        public static IOpponentStrategy Create(OpponentDifficulty difficulty, Random random, int maximumRows, int maximumColumns)
        {
            switch (difficulty)
            {
                case OpponentDifficulty.Easy:
                    return new RandomOpponentStrategy(random, maximumRows * maximumColumns);
                case OpponentDifficulty.Medium:
                    return new SurvivalOpponentStrategy(random, OpponentSkill.Medium, maximumRows, maximumColumns);
                case OpponentDifficulty.Hard:
                    return new SurvivalOpponentStrategy(random, OpponentSkill.Hard, maximumRows, maximumColumns);
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown opponent difficulty.");
            }
        }
    }
}

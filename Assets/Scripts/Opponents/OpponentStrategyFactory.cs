using System;

namespace GameOfLife.Opponents
{
    /// <summary>Creates the opponent strategy that plays at a given difficulty.</summary>
    public static class OpponentStrategyFactory
    {
        /// <summary>Returns a strategy for the difficulty with every buffer sized for the largest allowed grid.</summary>
        public static IOpponentStrategy Create(OpponentDifficulty difficulty, Random random, int maximumRows, int maximumColumns)
        {
            switch (difficulty)
            {
                case OpponentDifficulty.Easy:
                    return new RandomOpponentStrategy(random, maximumRows * maximumColumns);
                case OpponentDifficulty.Hard:
                    return new StrategicOpponentStrategy(random, maximumRows, maximumColumns);
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown opponent difficulty.");
            }
        }
    }
}

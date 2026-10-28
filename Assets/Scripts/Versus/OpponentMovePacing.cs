using System;

namespace GameOfLife.Versus
{
    /// <summary>Decides when the app places its next square, in setup or in the match: its squares left are spread evenly across the time left, jittered so its timing isn't predictable, and placed at its quick turn pace once the player has nothing left to place in setup.</summary>
    public static class OpponentMovePacing
    {
        private const float MinimumJitter = 0.7f;
        private const float MaximumJitter = 1.3f;

        /// <summary>Returns the seconds until the app's next square: the even share of the time left for each square left, scaled by a jitter chosen from a 0-1 random value.</summary>
        public static float SecondsUntilNextMove(float secondsLeft, int squaresLeft, double randomValue)
        {
            if (squaresLeft < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(squaresLeft), squaresLeft, "The squares left cannot be negative.");
            }

            var evenSpacing = Math.Max(secondsLeft, 0f) / (squaresLeft + 1f);
            var jitter = MinimumJitter + (MaximumJitter - MinimumJitter) * (float)Math.Clamp(randomValue, 0d, 1d);
            return evenSpacing * jitter;
        }

        /// <summary>Returns the seconds until the app's next setup square: evenly paced while the player is still placing, or no longer than its quick turn delay once the player is done, so nobody waits on the app.</summary>
        public static float SecondsUntilNextSetupMove(float secondsLeft, int squaresLeft, double randomValue, bool playerIsDone, float quickTurnDelay)
        {
            var pacedDelay = SecondsUntilNextMove(secondsLeft, squaresLeft, randomValue);
            return playerIsDone ? Math.Min(pacedDelay, quickTurnDelay) : pacedDelay;
        }
    }
}

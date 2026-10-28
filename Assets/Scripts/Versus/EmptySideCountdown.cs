using System;

namespace GameOfLife.Versus
{
    /// <summary>Times the short wait between a side losing its last living square and the match ending, separately for each side: a side's wait starts in full when it is emptied and is dropped once it has squares again, the match ends when any running wait runs out, and it runs on match time, so pausing the match pauses it.</summary>
    public sealed class EmptySideCountdown
    {
        private readonly float waitSeconds;
        private float playerSecondsRemaining;
        private float opponentSecondsRemaining;

        /// <summary>Whether the player has no living squares and their wait is counting down.</summary>
        public bool IsWaitingOnPlayer { get; private set; }

        /// <summary>Whether the app has no living squares and its wait is counting down.</summary>
        public bool IsWaitingOnOpponent { get; private set; }

        /// <summary>Whether either side's wait is counting down.</summary>
        public bool IsRunning => IsWaitingOnPlayer || IsWaitingOnOpponent;

        /// <summary>The match time left before the soonest running wait ends the match, or the full wait when none is running.</summary>
        public float SecondsRemaining
        {
            get
            {
                if (IsWaitingOnPlayer && IsWaitingOnOpponent)
                {
                    return Math.Min(playerSecondsRemaining, opponentSecondsRemaining);
                }

                return IsWaitingOnPlayer ? playerSecondsRemaining : IsWaitingOnOpponent ? opponentSecondsRemaining : waitSeconds;
            }
        }

        /// <summary>Waits the given number of match seconds after a side is emptied before ending the match.</summary>
        public EmptySideCountdown(float waitSeconds)
        {
            if (waitSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(waitSeconds), waitSeconds, "The wait cannot be negative.");
            }

            this.waitSeconds = waitSeconds;
        }

        /// <summary>Starts a side's wait in full when it has just been emptied, keeps a running wait as it is while the side stays empty, and drops it once the side has living squares again.</summary>
        public void Refresh(bool playerHasNoCells, bool opponentHasNoCells)
        {
            RefreshSide(playerHasNoCells, IsWaitingOnPlayer, out var isWaitingOnPlayer, ref playerSecondsRemaining);
            RefreshSide(opponentHasNoCells, IsWaitingOnOpponent, out var isWaitingOnOpponent, ref opponentSecondsRemaining);
            IsWaitingOnPlayer = isWaitingOnPlayer;
            IsWaitingOnOpponent = isWaitingOnOpponent;
        }

        /// <summary>Counts every running wait down by the match time that passed and returns whether one has run out, so the match should end now.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (IsWaitingOnPlayer)
            {
                playerSecondsRemaining -= deltaSeconds;
            }

            if (IsWaitingOnOpponent)
            {
                opponentSecondsRemaining -= deltaSeconds;
            }

            return (IsWaitingOnPlayer && playerSecondsRemaining <= 0f) || (IsWaitingOnOpponent && opponentSecondsRemaining <= 0f);
        }

        /// <summary>Stops both waits, such as when the match ends or a new one begins.</summary>
        public void Cancel()
        {
            IsWaitingOnPlayer = false;
            IsWaitingOnOpponent = false;
        }

        /// <summary>Works out one side's wait: started in full when the side is newly empty, kept while it stays empty, and stopped when it has squares.</summary>
        private void RefreshSide(bool hasNoCells, bool wasWaiting, out bool isWaiting, ref float secondsRemaining)
        {
            isWaiting = hasNoCells;
            if (hasNoCells && !wasWaiting)
            {
                secondsRemaining = waitSeconds;
            }
        }
    }
}

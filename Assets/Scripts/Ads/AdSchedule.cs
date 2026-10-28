using System;

namespace GameOfLife.Ads
{
    /// <summary>Decides when an ad is due: one at app start, one before every game except the session's first, and one every interval, never while a game is being played; times are real-time seconds since ads started at launch.</summary>
    public sealed class AdSchedule
    {
        private readonly float startupWindowSeconds;
        private readonly float timedIntervalSeconds;
        private readonly float minimumSecondsBetweenAds;
        private bool hasStartedAGame;
        private bool isStartupAdPending = true;
        private float lastAdTime = float.NegativeInfinity;
        private float nextTimedAdTime;

        /// <summary>Starts the schedule at app launch, with the first timed ad due one interval from now.</summary>
        public AdSchedule(float startupWindowSeconds, float timedIntervalSeconds, float minimumSecondsBetweenAds)
        {
            if (startupWindowSeconds < 0f || timedIntervalSeconds <= 0f || minimumSecondsBetweenAds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timedIntervalSeconds), "Ad timings must not be negative, and the timed interval must be positive.");
            }

            this.startupWindowSeconds = startupWindowSeconds;
            this.timedIntervalSeconds = timedIntervalSeconds;
            this.minimumSecondsBetweenAds = minimumSecondsBetweenAds;
            nextTimedAdTime = timedIntervalSeconds;
        }

        /// <summary>When the next timed ad falls due.</summary>
        public float NextTimedAdTime => nextTimedAdTime;

        /// <summary>Returns whether the start-up ad should show now: only once, only within the start-up window, and only before the player is playing.</summary>
        public bool IsStartupAdDue(float now, bool isPlaying)
        {
            if (isStartupAdPending && (now > startupWindowSeconds || hasStartedAGame))
            {
                isStartupAdPending = false;
            }

            return isStartupAdPending && !isPlaying;
        }

        /// <summary>Records that a game is starting and returns whether an ad should show first: never for the session's first game, nor too soon after another ad.</summary>
        public bool ShouldShowBeforeGame(float now)
        {
            var isFirstGame = !hasStartedAGame;
            hasStartedAGame = true;
            isStartupAdPending = false;
            return !isFirstGame && now - lastAdTime >= minimumSecondsBetweenAds;
        }

        /// <summary>Returns whether the timed ad is due and may show now, which it never does while a game is being played.</summary>
        public bool IsTimedAdDue(float now, bool isPlaying)
        {
            return !isPlaying && now >= nextTimedAdTime;
        }

        /// <summary>Records that an ad was shown, which restarts the timed interval.</summary>
        public void RecordAdShown(float now)
        {
            lastAdTime = now;
            isStartupAdPending = false;
            nextTimedAdTime = now + timedIntervalSeconds;
        }
    }
}

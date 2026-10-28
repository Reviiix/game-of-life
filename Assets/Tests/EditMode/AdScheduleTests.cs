using System;
using GameOfLife.Ads;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks when ads fall due: at start-up, before games after the first, and on the timed interval outside play.</summary>
    [TestFixture]
    public sealed class AdScheduleTests
    {
        private const float StartupWindow = 10f;
        private const float Interval = 1800f;
        private const float MinimumGap = 120f;

        /// <summary>The start-up ad shows once, within the window, and not while playing.</summary>
        [Test]
        public void StartupAd_ShowsOnceWithinTheWindowOutsidePlay()
        {
            var schedule = CreateSchedule();

            Assert.That(schedule.IsStartupAdDue(2f, isPlaying: true), Is.False);
            Assert.That(schedule.IsStartupAdDue(3f, isPlaying: false), Is.True);
            schedule.RecordAdShown(3f);
            Assert.That(schedule.IsStartupAdDue(4f, isPlaying: false), Is.False);
        }

        /// <summary>A start-up ad that loads after the window is skipped for good.</summary>
        [Test]
        public void StartupAd_AfterTheWindow_IsSkipped()
        {
            var schedule = CreateSchedule();

            Assert.That(schedule.IsStartupAdDue(StartupWindow + 1f, isPlaying: false), Is.False);
            Assert.That(schedule.IsStartupAdDue(1f, isPlaying: false), Is.False);
        }

        /// <summary>Starting the first game cancels a start-up ad that has not shown yet.</summary>
        [Test]
        public void StartupAd_AfterTheFirstGameStarts_IsSkipped()
        {
            var schedule = CreateSchedule();

            schedule.ShouldShowBeforeGame(1f);

            Assert.That(schedule.IsStartupAdDue(2f, isPlaying: false), Is.False);
        }

        /// <summary>The session's first game gets no ad; later games do once the minimum gap since the last ad has passed.</summary>
        [Test]
        public void GameStart_SkipsTheFirstGameAndRespectsTheMinimumGap()
        {
            var schedule = CreateSchedule();
            schedule.RecordAdShown(1f);

            Assert.That(schedule.ShouldShowBeforeGame(5f), Is.False, "The session's first game must not get an ad.");
            Assert.That(schedule.ShouldShowBeforeGame(60f), Is.False, "An ad showed less than the minimum gap ago.");
            Assert.That(schedule.ShouldShowBeforeGame(1f + MinimumGap), Is.True);
        }

        /// <summary>The timed ad falls due one interval after the last ad, and waits while a game is being played.</summary>
        [Test]
        public void TimedAd_FallsDueAfterTheIntervalOnlyOutsidePlay()
        {
            var schedule = CreateSchedule();
            schedule.RecordAdShown(100f);

            Assert.That(schedule.IsTimedAdDue(100f + Interval - 1f, isPlaying: false), Is.False);
            Assert.That(schedule.IsTimedAdDue(100f + Interval, isPlaying: true), Is.False);
            Assert.That(schedule.IsTimedAdDue(100f + Interval + 30f, isPlaying: false), Is.True);
            Assert.That(schedule.NextTimedAdTime, Is.EqualTo(100f + Interval));
        }

        /// <summary>Invalid timings are rejected.</summary>
        [Test]
        public void Constructor_WithInvalidTimings_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdSchedule(-1f, Interval, MinimumGap));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdSchedule(StartupWindow, 0f, MinimumGap));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdSchedule(StartupWindow, Interval, -1f));
        }

        /// <summary>Creates a schedule with the test timings.</summary>
        private static AdSchedule CreateSchedule()
        {
            return new AdSchedule(StartupWindow, Interval, MinimumGap);
        }
    }
}

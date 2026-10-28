using System;
using GameOfLife.Versus;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks the waits between a side losing its last living square and the match ending: each side's wait starts in full when it is emptied, runs out after the set match time, is dropped when the side has squares again, and the soonest running wait decides.</summary>
    [TestFixture]
    public sealed class EmptySideCountdownTests
    {
        private const float WaitSeconds = 3f;
        private const float FrameSeconds = 0.25f;
        private const float Tolerance = 0.0001f;

        /// <summary>Nothing counts down while both sides have living squares.</summary>
        [Test]
        public void WithBothSidesAlive_NeverRunsOut()
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(false, false);

            Assert.That(countdown.IsRunning, Is.False);
            Assert.That(countdown.Tick(WaitSeconds * 10f), Is.False, "An idle countdown never runs out.");
        }

        /// <summary>Once a side is emptied its wait runs for exactly the set time: not out just before it, out once it has passed; it says which side it waits on.</summary>
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void AfterASideIsEmptied_RunsOutAfterTheWait(bool playerHasNoCells, bool opponentHasNoCells)
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(playerHasNoCells, opponentHasNoCells);
            Assert.That(countdown.IsWaitingOnPlayer, Is.EqualTo(playerHasNoCells));
            Assert.That(countdown.IsWaitingOnOpponent, Is.EqualTo(opponentHasNoCells));
            Assert.That(countdown.SecondsRemaining, Is.EqualTo(WaitSeconds).Within(Tolerance));

            var elapsed = 0f;
            while (elapsed + FrameSeconds < WaitSeconds)
            {
                Assert.That(countdown.Tick(FrameSeconds), Is.False, $"It ran out after only {elapsed + FrameSeconds} of {WaitSeconds} seconds.");
                elapsed += FrameSeconds;
            }

            Assert.That(countdown.Tick(FrameSeconds), Is.True, "It should run out once the wait has passed.");
        }

        /// <summary>Refreshing while a side is still empty keeps its running wait instead of starting it again, so later events cannot put the end off.</summary>
        [Test]
        public void RefreshingWhileStillEmpty_KeepsTheRunningWait()
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(false, true);
            countdown.Tick(2f);
            countdown.Refresh(false, true);

            Assert.That(countdown.SecondsRemaining, Is.EqualTo(WaitSeconds - 2f).Within(Tolerance));
        }

        /// <summary>A side that gets living squares back drops its wait; losing them again later starts a full new wait.</summary>
        [Test]
        public void GettingSquaresBack_DropsTheWaitAndALaterEmptyingWaitsInFull()
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(true, false);
            countdown.Tick(2.5f);
            countdown.Refresh(false, false);

            Assert.That(countdown.IsRunning, Is.False);
            Assert.That(countdown.Tick(WaitSeconds), Is.False, "A dropped wait never runs out.");

            countdown.Refresh(true, false);
            Assert.That(countdown.SecondsRemaining, Is.EqualTo(WaitSeconds).Within(Tolerance), "A new emptying waits the full time.");
        }

        /// <summary>When both sides are emptied at different moments and the first recovers, the second still gets its full wait from when it was emptied.</summary>
        [Test]
        public void BothEmptiedThenTheFirstRecovers_TheSecondGetsItsFullWait()
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(true, false);
            Assert.That(countdown.Tick(2.5f), Is.False);
            countdown.Refresh(true, true);
            Assert.That(countdown.SecondsRemaining, Is.EqualTo(0.5f).Within(Tolerance), "The soonest wait, the player's, decides while both are empty.");

            Assert.That(countdown.Tick(0.3f), Is.False);
            countdown.Refresh(false, true);
            Assert.That(countdown.IsWaitingOnPlayer, Is.False);
            Assert.That(countdown.IsWaitingOnOpponent, Is.True);
            Assert.That(countdown.SecondsRemaining, Is.EqualTo(WaitSeconds - 0.3f).Within(Tolerance), "The app's wait started when it was emptied, 0.3 seconds ago.");
            Assert.That(countdown.Tick(WaitSeconds - 0.3f - FrameSeconds), Is.False, "The app gets its full wait.");
            Assert.That(countdown.Tick(FrameSeconds), Is.True);
        }

        /// <summary>With both sides empty the soonest wait ends the match, even while the other side's wait still has time left.</summary>
        [Test]
        public void BothEmpty_TheSoonestWaitEndsTheMatch()
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(false, true);
            countdown.Tick(1f);
            countdown.Refresh(true, true);

            Assert.That(countdown.Tick(WaitSeconds - 1f), Is.True, "The app's wait, started a second earlier, runs out first.");
        }

        /// <summary>Cancel stops both waits, as when the match ends or restarts.</summary>
        [Test]
        public void Cancel_StopsBothWaits()
        {
            var countdown = new EmptySideCountdown(WaitSeconds);
            countdown.Refresh(true, true);
            countdown.Cancel();

            Assert.That(countdown.IsRunning, Is.False);
            Assert.That(countdown.Tick(WaitSeconds * 2f), Is.False);
        }

        /// <summary>A wait of zero ends the match on the next tick, and a negative wait is refused.</summary>
        [Test]
        public void ZeroWait_RunsOutOnTheNextTick_AndANegativeWaitIsRefused()
        {
            var countdown = new EmptySideCountdown(0f);
            countdown.Refresh(true, false);

            Assert.That(countdown.Tick(FrameSeconds), Is.True);
            Assert.That(() => new EmptySideCountdown(-1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}

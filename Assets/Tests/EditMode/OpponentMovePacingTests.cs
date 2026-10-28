using System;
using GameOfLife.Versus;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that OpponentMovePacing spreads the app's squares across the time left and hurries its setup squares once the player is done.</summary>
    [TestFixture]
    public sealed class OpponentMovePacingTests
    {
        private const float Tolerance = 0.0001f;

        /// <summary>The middle jitter places each square at an even share of the time left, one share per square plus one, so the last lands before time runs out.</summary>
        [Test]
        public void SecondsUntilNextMove_WithMiddleJitter_IsAnEvenShare()
        {
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(30f, 19, 0.5d), Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(10f, 0, 0.5d), Is.EqualTo(10f).Within(Tolerance));
        }

        /// <summary>The jitter scales the even share between 70% and 130%, clamping random values outside 0 to 1.</summary>
        [Test]
        public void SecondsUntilNextMove_JitterSpansSeventyToOneHundredThirtyPercent()
        {
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(20f, 1, 0d), Is.EqualTo(7f).Within(Tolerance));
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(20f, 1, 1d), Is.EqualTo(13f).Within(Tolerance));
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(20f, 1, 5d), Is.EqualTo(13f).Within(Tolerance));
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(20f, 1, -5d), Is.EqualTo(7f).Within(Tolerance));
        }

        /// <summary>With no time left the next move is due at once, and negative squares are refused.</summary>
        [Test]
        public void SecondsUntilNextMove_EdgeCases()
        {
            Assert.That(OpponentMovePacing.SecondsUntilNextMove(-3f, 4, 0.5d), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => OpponentMovePacing.SecondsUntilNextMove(10f, -1, 0.5d));
        }

        /// <summary>While the player is still placing, setup squares are paced like match squares; once the player is done they come at the quick turn delay at most.</summary>
        [Test]
        public void SecondsUntilNextSetupMove_HurriesOnceThePlayerIsDone()
        {
            Assert.That(OpponentMovePacing.SecondsUntilNextSetupMove(30f, 19, 0.5d, false, 0.35f), Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(OpponentMovePacing.SecondsUntilNextSetupMove(30f, 19, 0.5d, true, 0.35f), Is.EqualTo(0.35f).Within(Tolerance));
            Assert.That(OpponentMovePacing.SecondsUntilNextSetupMove(1f, 9, 0.5d, true, 0.35f), Is.EqualTo(0.1f).Within(Tolerance));
        }
    }
}

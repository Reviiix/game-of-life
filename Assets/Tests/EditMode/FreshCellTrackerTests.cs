using System;
using System.Linq;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that FreshCellTracker keeps a placed cell fresh for exactly its generations, for its placer only, and forgets it once it dies or changes owner.</summary>
    [TestFixture]
    public sealed class FreshCellTrackerTests
    {
        private const int CellCount = 16;
        private const int FreshGenerations = 3;

        private FreshCellTracker tracker;

        /// <summary>Creates a tracker that keeps cells fresh for three generations.</summary>
        [SetUp]
        public void SetUp()
        {
            tracker = new FreshCellTracker(CellCount, FreshGenerations);
        }

        /// <summary>A placed cell stays fresh through two generations and settles on the third, listed once.</summary>
        [Test]
        public void MarkedCell_SettlesOnItsThirdGeneration()
        {
            tracker.Mark(5, CellOwner.Player);

            for (var generation = 1; generation < FreshGenerations; generation++)
            {
                tracker.AdvanceGeneration();
                Assert.That(tracker.IsFresh(5, CellOwner.Player), Is.True, $"generation {generation}");
                Assert.That(tracker.SettledCellIndices, Is.Empty, $"generation {generation}");
            }

            tracker.AdvanceGeneration();
            Assert.That(tracker.IsFresh(5, CellOwner.Player), Is.False);
            Assert.That(tracker.SettledCellIndices, Is.EqualTo(new[] { 5 }));
            Assert.That(tracker.FreshCellCount, Is.Zero);

            tracker.AdvanceGeneration();
            Assert.That(tracker.SettledCellIndices, Is.Empty);
        }

        /// <summary>A fresh cell is fresh only while the side that placed it still owns it.</summary>
        [Test]
        public void IsFresh_OnlyForThePlacingOwner()
        {
            tracker.Mark(2, CellOwner.Opponent);

            Assert.That(tracker.IsFresh(2, CellOwner.Opponent), Is.True);
            Assert.That(tracker.IsFresh(2, CellOwner.Player), Is.False);
            Assert.That(tracker.IsFresh(2, CellOwner.None), Is.False);
            Assert.That(tracker.IsFresh(3, CellOwner.Opponent), Is.False);
        }

        /// <summary>A fresh cell that dies or is absorbed is forgotten, so a later cell of the same colour there starts plain and never settles.</summary>
        [Test]
        public void ForgetIfOwnerChanged_DropsDeadOrAbsorbedCells()
        {
            tracker.Mark(1, CellOwner.Player);
            tracker.Mark(2, CellOwner.Player);
            tracker.Mark(3, CellOwner.Player);

            tracker.ForgetIfOwnerChanged(1, CellOwner.None);
            tracker.ForgetIfOwnerChanged(2, CellOwner.Opponent);
            tracker.ForgetIfOwnerChanged(3, CellOwner.Player);

            Assert.That(tracker.IsFresh(1, CellOwner.Player), Is.False);
            Assert.That(tracker.IsFresh(2, CellOwner.Player), Is.False);
            Assert.That(tracker.IsFresh(3, CellOwner.Player), Is.True);
            Assert.That(tracker.FreshCellCount, Is.EqualTo(1));

            for (var generation = 0; generation < FreshGenerations; generation++)
            {
                tracker.AdvanceGeneration();
            }

            Assert.That(tracker.SettledCellIndices, Is.EqualTo(new[] { 3 }));
        }

        /// <summary>Placing on a fresh cell again restarts its count instead of listing it twice.</summary>
        [Test]
        public void MarkingAgain_RestartsTheCount()
        {
            tracker.Mark(4, CellOwner.Player);
            tracker.AdvanceGeneration();
            tracker.AdvanceGeneration();

            tracker.Mark(4, CellOwner.Player);
            tracker.AdvanceGeneration();
            tracker.AdvanceGeneration();
            Assert.That(tracker.IsFresh(4, CellOwner.Player), Is.True);
            Assert.That(tracker.FreshCellCount, Is.EqualTo(1));

            tracker.AdvanceGeneration();
            Assert.That(tracker.SettledCellIndices, Is.EqualTo(new[] { 4 }));
        }

        /// <summary>Cells placed in different generations settle in different generations, and removing cells mid-list keeps the rest counting.</summary>
        [Test]
        public void CellsPlacedApart_SettleApart()
        {
            tracker.Mark(0, CellOwner.Player);
            tracker.Mark(1, CellOwner.Player);
            tracker.AdvanceGeneration();
            tracker.Mark(7, CellOwner.Opponent);
            tracker.Mark(8, CellOwner.Opponent);
            tracker.ForgetIfOwnerChanged(0, CellOwner.None);
            tracker.AdvanceGeneration();

            tracker.AdvanceGeneration();
            Assert.That(tracker.SettledCellIndices, Is.EqualTo(new[] { 1 }));

            tracker.AdvanceGeneration();
            Assert.That(tracker.SettledCellIndices.OrderBy(cellIndex => cellIndex), Is.EqualTo(new[] { 7, 8 }));
            Assert.That(tracker.FreshCellCount, Is.Zero);
        }

        /// <summary>Clear forgets every fresh cell at once.</summary>
        [Test]
        public void Clear_ForgetsEveryFreshCell()
        {
            for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            {
                tracker.Mark(cellIndex, CellOwner.Player);
            }

            tracker.Clear();

            Assert.That(tracker.FreshCellCount, Is.Zero);
            Assert.That(tracker.IsFresh(0, CellOwner.Player), Is.False);
            tracker.AdvanceGeneration();
            Assert.That(tracker.SettledCellIndices, Is.Empty);
        }

        /// <summary>With no fresh generations, or for a dead owner, marking makes nothing fresh.</summary>
        [Test]
        public void ZeroGenerationsOrNoOwner_MarkNothing()
        {
            var disabled = new FreshCellTracker(CellCount, 0);
            disabled.Mark(0, CellOwner.Player);
            tracker.Mark(1, CellOwner.None);

            Assert.That(disabled.IsFresh(0, CellOwner.Player), Is.False);
            Assert.That(tracker.IsFresh(1, CellOwner.None), Is.False);
            Assert.That(disabled.FreshCellCount + tracker.FreshCellCount, Is.Zero);
        }

        /// <summary>Negative sizes are refused.</summary>
        [Test]
        public void Constructor_WithNegativeValues_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FreshCellTracker(-1, FreshGenerations));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FreshCellTracker(CellCount, -1));
        }
    }
}

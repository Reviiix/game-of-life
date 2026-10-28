using System;
using GameOfLife.Patterns;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks the band of rows a pattern may be placed in: its size, which cells it holds, the whole-board shortcut and the arguments it rejects.</summary>
    [TestFixture]
    public sealed class PlacementBoundsTests
    {
        private const int BoardRows = 24;
        private const int BoardColumns = 12;

        /// <summary>A band of rows counts its rows, keeps its square limit and is not the whole board.</summary>
        [Test]
        public void Band_CountsItsRowsAndKeepsItsLimits()
        {
            var band = new PlacementBounds(3, 6, BoardRows, BoardColumns, 5);

            Assert.That(band.RowCount, Is.EqualTo(3));
            Assert.That(band.FirstRow, Is.EqualTo(3));
            Assert.That(band.EndRowExclusive, Is.EqualTo(6));
            Assert.That(band.Columns, Is.EqualTo(BoardColumns));
            Assert.That(band.IsWholeBoard, Is.False);
            Assert.That(band.SquaresAvailable, Is.EqualTo(5));
        }

        /// <summary>The whole-board bounds span every row with no limit on squares.</summary>
        [Test]
        public void WholeBoard_SpansEveryRowWithUnlimitedSquares()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);

            Assert.That(bounds.FirstRow, Is.Zero);
            Assert.That(bounds.EndRowExclusive, Is.EqualTo(BoardRows));
            Assert.That(bounds.BoardRows, Is.EqualTo(BoardRows));
            Assert.That(bounds.Columns, Is.EqualTo(BoardColumns));
            Assert.That(bounds.RowCount, Is.EqualTo(BoardRows));
            Assert.That(bounds.SquaresAvailable, Is.EqualTo(int.MaxValue));
            Assert.That(bounds.IsWholeBoard, Is.True);
        }

        /// <summary>A half of the board that starts at the top is still not the whole board.</summary>
        [Test]
        public void Half_IsNotTheWholeBoard()
        {
            var half = new PlacementBounds(0, BoardRows / 2, BoardRows, BoardColumns, 20);

            Assert.That(half.IsWholeBoard, Is.False);
            Assert.That(half.RowCount, Is.EqualTo(BoardRows / 2));
        }

        /// <summary>Bands outside the board, backwards bands, negative widths and negative square counts are rejected.</summary>
        [Test]
        public void Constructor_WithInvalidArguments_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementBounds(-1, 6, BoardRows, BoardColumns, 5), "A negative first row.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementBounds(6, 3, BoardRows, BoardColumns, 5), "An end row before the first row.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementBounds(3, BoardRows + 1, BoardRows, BoardColumns, 5), "An end row past the board.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementBounds(3, 6, BoardRows, -1, 5), "A negative column count.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementBounds(3, 6, BoardRows, BoardColumns, -1), "A negative square count.");
            Assert.DoesNotThrow(() => new PlacementBounds(3, 3, BoardRows, BoardColumns, 0), "An empty band with no squares is allowed.");
        }
    }
}

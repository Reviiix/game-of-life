using System;
using System.Text;
using GameOfLife.Patterns;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that an orientation parses rows into a tight box, keeps its cells in row-major order, turns and mirrors correctly and knows what it fits.</summary>
    [TestFixture]
    public sealed class PatternOrientationTests
    {
        private const int QuarterTurnsPerFullTurn = 4;
        private const int PoseCount = 8;

        /// <summary>Ragged rows, no rows, rows without a living cell and an unknown symbol are each rejected.</summary>
        [Test]
        public void FromRows_RejectsRaggedEmptyOrUnknownRows()
        {
            Assert.Throws<ArgumentException>(() => PatternOrientation.FromRows(new[] { "OO.", "O" }), "ragged rows");
            Assert.Throws<ArgumentException>(() => PatternOrientation.FromRows(new string[0]), "no rows");
            Assert.Throws<ArgumentException>(() => PatternOrientation.FromRows(null), "null rows");
            Assert.Throws<ArgumentException>(() => PatternOrientation.FromRows(new[] { "" }), "an empty row");
            Assert.Throws<ArgumentException>(() => PatternOrientation.FromRows(new[] { "...", "..." }), "no living cell");
            Assert.Throws<ArgumentException>(() => PatternOrientation.FromRows(new[] { "OX." }), "an unknown symbol");
        }

        /// <summary>Empty border rows and columns are trimmed away, and every orientation of every catalogue shape touches all four sides of its box.</summary>
        [Test]
        public void FromRows_TrimsToTheTightBoundingBox()
        {
            var orientation = PatternOrientation.FromRows(new[] { "....", ".OO.", "...." });

            Assert.That(orientation.Rows, Is.EqualTo(1), "rows after trimming");
            Assert.That(orientation.Columns, Is.EqualTo(2), "columns after trimming");
            Assert.That(orientation.CellCount, Is.EqualTo(2), "cell count after trimming");
            Assert.That(DescribeOffsets(orientation), Is.EqualTo("(0,0) (0,1)"), "offsets after trimming");

            foreach (var pattern in PatternTestShapes.CatalogueShapes())
            {
                for (var index = 0; index < PoseCount; index++)
                {
                    var pose = new PatternPose(index / 2, index % 2 == 1);
                    AssertTouchesEverySide(pattern.GetOrientation(pose), $"{pattern.Name} after {pose.QuarterTurns} turns{(pose.IsMirrored ? " mirrored" : string.Empty)}");
                }
            }
        }

        /// <summary>Cells enumerate top row first and left to right within a row.</summary>
        [Test]
        public void Offsets_AreRowMajorAndSorted()
        {
            var orientation = PatternOrientation.FromRows(new[] { "O.O", ".O." });

            Assert.That(orientation.CellCount, Is.EqualTo(3), "cell count");
            Assert.That(orientation.Rows, Is.EqualTo(2), "rows");
            Assert.That(orientation.Columns, Is.EqualTo(3), "columns");
            Assert.That(DescribeOffsets(orientation), Is.EqualTo("(0,0) (0,2) (1,1)"), "row-major offsets");
        }

        /// <summary>A clockwise quarter turn maps rows to columns, swaps the box size, keeps row-major order and comes back round after four turns.</summary>
        [Test]
        public void RotatedClockwise_MapsRowsToColumns()
        {
            var original = PatternOrientation.FromRows(new[] { "OO.", "..O" });

            var rotated = original.RotatedClockwise();

            Assert.That(rotated.Rows, Is.EqualTo(original.Columns), "rotated rows are the original columns");
            Assert.That(rotated.Columns, Is.EqualTo(original.Rows), "rotated columns are the original rows");
            Assert.That(rotated.CellCount, Is.EqualTo(original.CellCount), "cell count is unchanged");
            Assert.That(PatternTestShapes.Describe(rotated), Is.EqualTo(new[] { ".O", ".O", "O." }), "the shape turned clockwise");
            Assert.That(DescribeOffsets(rotated), Is.EqualTo("(0,1) (1,1) (2,0)"), "rotated cells stay row-major");

            var turnedFullCircle = original;
            for (var turn = 0; turn < QuarterTurnsPerFullTurn; turn++)
            {
                turnedFullCircle = turnedFullCircle.RotatedClockwise();
            }

            Assert.That(PatternTestShapes.Describe(turnedFullCircle), Is.EqualTo(PatternTestShapes.Describe(original)), "four quarter turns return the original");
        }

        /// <summary>Mirroring reverses the columns of every row, keeps row-major order and undoes itself.</summary>
        [Test]
        public void MirroredHorizontally_ReversesColumns()
        {
            var original = PatternOrientation.FromRows(new[] { "OO.", "..O" });

            var mirrored = original.MirroredHorizontally();

            Assert.That(mirrored.Rows, Is.EqualTo(original.Rows), "mirrored rows are unchanged");
            Assert.That(mirrored.Columns, Is.EqualTo(original.Columns), "mirrored columns are unchanged");
            Assert.That(PatternTestShapes.Describe(mirrored), Is.EqualTo(new[] { ".OO", "O.." }), "the shape mirrored left to right");
            Assert.That(DescribeOffsets(mirrored), Is.EqualTo("(0,1) (0,2) (1,0)"), "mirrored cells stay row-major");
            Assert.That(PatternTestShapes.Describe(mirrored.MirroredHorizontally()), Is.EqualTo(PatternTestShapes.Describe(original)), "two mirrors return the original");
        }

        /// <summary>A shape fits an area at least as tall and as wide as its box, and fits bounds by their band's rows and the board's columns.</summary>
        [Test]
        public void Fits_ComparesRowsAndColumns()
        {
            var shape = PatternOrientation.FromRows(new[] { "OO", "O.", "OO" });

            Assert.That(shape.Fits(3, 2), Is.True, "a 3x2 shape fits 3 rows by 2 columns");
            Assert.That(shape.Fits(4, 3), Is.True, "a 3x2 shape fits 4 rows by 3 columns");
            Assert.That(shape.Fits(2, 2), Is.False, "a 3x2 shape does not fit 2 rows");
            Assert.That(shape.Fits(3, 1), Is.False, "a 3x2 shape does not fit 1 column");

            Assert.That(shape.Fits(new PlacementBounds(2, 5, 10, 2, 0)), Is.True, "a band of 3 rows on 2 columns");
            Assert.That(shape.Fits(new PlacementBounds(0, 2, 10, 2, 0)), Is.False, "a band of 2 rows on a 10-row board");
            Assert.That(shape.Fits(new PlacementBounds(0, 10, 10, 1, 0)), Is.False, "the whole board but only 1 column");
        }

        /// <summary>Lists an orientation's cells as "(row,column)" pairs in their stored order.</summary>
        private static string DescribeOffsets(PatternOrientation orientation)
        {
            var text = new StringBuilder();
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                if (cell > 0)
                {
                    text.Append(' ');
                }

                text.Append('(').Append(orientation.GetRowOffset(cell)).Append(',').Append(orientation.GetColumnOffset(cell)).Append(')');
            }

            return text.ToString();
        }

        /// <summary>Asserts that some cell lies in the first row, the first column, the last row and the last column of the box.</summary>
        private static void AssertTouchesEverySide(PatternOrientation orientation, string name)
        {
            var touchesTop = false;
            var touchesBottom = false;
            var touchesLeft = false;
            var touchesRight = false;
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                var row = orientation.GetRowOffset(cell);
                var column = orientation.GetColumnOffset(cell);
                Assert.That(row, Is.InRange(0, orientation.Rows - 1), $"{name}: cell {cell} row lies inside the box");
                Assert.That(column, Is.InRange(0, orientation.Columns - 1), $"{name}: cell {cell} column lies inside the box");
                touchesTop |= row == 0;
                touchesBottom |= row == orientation.Rows - 1;
                touchesLeft |= column == 0;
                touchesRight |= column == orientation.Columns - 1;
            }

            Assert.That(touchesTop, Is.True, $"{name} touches the top of its box");
            Assert.That(touchesBottom, Is.True, $"{name} touches the bottom of its box");
            Assert.That(touchesLeft, Is.True, $"{name} touches the left of its box");
            Assert.That(touchesRight, Is.True, $"{name} touches the right of its box");
        }
    }
}

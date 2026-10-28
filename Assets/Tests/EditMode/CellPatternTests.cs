using GameOfLife.Patterns;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that a pattern's eight poses follow the rotate and mirror buttons, and that it judges fit, availability and the starting pose from its bounds.</summary>
    [TestFixture]
    public sealed class CellPatternTests
    {
        private const int PoseCount = 8;
        private const int BlockCellCount = 4;
        private const int PulsarCellCount = 48;
        private const int PulsarSize = 13;
        private const int LargeBoardRows = 60;
        private const int LargeBoardColumns = 30;
        private const int DefaultBoardRows = 24;
        private const int DefaultBoardColumns = 12;
        private const int DefaultHalfEndRow = 12;
        private const int VersusBoardRows = 24;
        private const int VersusBoardColumns = 30;
        private const int VersusHalfEndRow = 12;
        private const int SmallBoardSize = 6;

        /// <summary>Every pose shows the upright shape mirrored first (when the pose says so) and then turned its quarter turns.</summary>
        [Test]
        public void GetOrientation_EqualsMirrorThenRotateOfTheUprightShape()
        {
            foreach (var pattern in PatternTestShapes.CatalogueShapes())
            {
                Assert.That(pattern.GetOrientation(PatternPose.Upright), Is.SameAs(pattern.Upright), $"{pattern.Name} upright pose");
                foreach (var pose in AllPoses())
                {
                    var expected = pose.IsMirrored ? pattern.Upright.MirroredHorizontally() : pattern.Upright;
                    for (var turn = 0; turn < pose.QuarterTurns; turn++)
                    {
                        expected = expected.RotatedClockwise();
                    }

                    Assert.That(PatternTestShapes.Describe(pattern.GetOrientation(pose)), Is.EqualTo(PatternTestShapes.Describe(expected)), $"{pattern.Name} in pose {Name(pose)}");
                    Assert.That(pattern.GetOrientation(pose).CellCount, Is.EqualTo(pattern.CellCount), $"{pattern.Name} keeps its cell count in pose {Name(pose)}");
                }
            }
        }

        /// <summary>Turning a pose shows the previously shown shape turned a quarter turn clockwise, whatever the pose.</summary>
        [Test]
        public void RotatedPose_RotatesTheDisplayedShape()
        {
            foreach (var pattern in PatternTestShapes.CatalogueShapes())
            {
                foreach (var pose in AllPoses())
                {
                    var shownThenTurned = PatternTestShapes.Describe(pattern.GetOrientation(pose).RotatedClockwise());
                    var turnedPose = PatternTestShapes.Describe(pattern.GetOrientation(pose.RotatedClockwise()));

                    Assert.That(turnedPose, Is.EqualTo(shownThenTurned), $"{pattern.Name} turned from pose {Name(pose)}");
                }
            }
        }

        /// <summary>Mirroring a pose shows the previously shown shape flipped left to right, whatever the pose, and mirroring twice returns the pose.</summary>
        [Test]
        public void MirroredPose_MirrorsTheDisplayedShape()
        {
            foreach (var pattern in PatternTestShapes.CatalogueShapes())
            {
                foreach (var pose in AllPoses())
                {
                    var shownThenMirrored = PatternTestShapes.Describe(pattern.GetOrientation(pose).MirroredHorizontally());
                    var mirroredPose = PatternTestShapes.Describe(pattern.GetOrientation(pose.Mirrored()));

                    Assert.That(mirroredPose, Is.EqualTo(shownThenMirrored), $"{pattern.Name} mirrored from pose {Name(pose)}");
                }
            }

            foreach (var pose in AllPoses())
            {
                var mirroredTwice = pose.Mirrored().Mirrored();

                Assert.That(mirroredTwice.QuarterTurns, Is.EqualTo(pose.QuarterTurns), $"quarter turns after mirroring {Name(pose)} twice");
                Assert.That(mirroredTwice.IsMirrored, Is.EqualTo(pose.IsMirrored), $"mirroring after mirroring {Name(pose)} twice");
                Assert.That(mirroredTwice.Index, Is.EqualTo(pose.Index), $"index after mirroring {Name(pose)} twice");
            }
        }

        /// <summary>A pattern fits when either its upright box or its turned box fits the band.</summary>
        [Test]
        public void CanFit_ConsidersBothDimensionSets()
        {
            var blinker = PatternTestShapes.Blinker();
            Assert.That(blinker.CanFit(PlacementBounds.WholeBoard(1, 3)), Is.True, "Blinker on 1 row by 3 columns");
            Assert.That(blinker.CanFit(PlacementBounds.WholeBoard(3, 1)), Is.True, "Blinker turned on 3 rows by 1 column");
            Assert.That(blinker.CanFit(PlacementBounds.WholeBoard(2, 2)), Is.False, "Blinker on 2 rows by 2 columns");

            const int wideShapeColumns = 35;
            var wideShape = PatternTestShapes.Pattern(new string('O', wideShapeColumns), new string('O', wideShapeColumns));
            Assert.That(wideShape.Upright.Fits(LargeBoardRows, LargeBoardColumns), Is.False, "a 2x35 shape upright on 60x30");
            Assert.That(wideShape.FitsAnyOrientation(LargeBoardRows, LargeBoardColumns), Is.True, "a 2x35 shape turned on 60x30");
            Assert.That(wideShape.CanFit(PlacementBounds.WholeBoard(LargeBoardRows, LargeBoardColumns)), Is.True, "a 2x35 shape can fit 60x30");

            var pulsar = PatternTestShapes.Pulsar();
            var defaultPlayerHalf = new PlacementBounds(0, DefaultHalfEndRow, DefaultBoardRows, DefaultBoardColumns, int.MaxValue);
            Assert.That(pulsar.Upright.Rows, Is.EqualTo(PulsarSize), "Pulsar rows");
            Assert.That(pulsar.Upright.Columns, Is.EqualTo(PulsarSize), "Pulsar columns");
            Assert.That(pulsar.FitsAnyOrientation(defaultPlayerHalf.RowCount, defaultPlayerHalf.Columns), Is.False, "Pulsar in any orientation on 12 rows by 12 columns");
            Assert.That(pulsar.CanFit(defaultPlayerHalf), Is.False, "Pulsar on the default 24x12 player half");
        }

        /// <summary>A pattern that fits is still refused when it has more cells than squares are available.</summary>
        [Test]
        public void CanFit_RefusesWhenCellsExceedSquaresAvailable()
        {
            var block = PatternTestShapes.Block();
            Assert.That(block.CellCount, Is.EqualTo(BlockCellCount), "Block cell count");
            Assert.That(block.CanFit(new PlacementBounds(0, SmallBoardSize, SmallBoardSize, SmallBoardSize, BlockCellCount - 1)), Is.False, "Block with 3 squares");
            Assert.That(block.CanFit(new PlacementBounds(0, SmallBoardSize, SmallBoardSize, SmallBoardSize, BlockCellCount)), Is.True, "Block with 4 squares");

            const int fewSquares = 20;
            var pulsar = PatternTestShapes.Pulsar();
            Assert.That(pulsar.CellCount, Is.EqualTo(PulsarCellCount), "Pulsar cell count");
            Assert.That(pulsar.CanFit(new PlacementBounds(0, LargeBoardRows, LargeBoardRows, LargeBoardColumns, fewSquares)), Is.False, "Pulsar with 20 squares on 60x30");
            Assert.That(pulsar.CanFit(new PlacementBounds(0, LargeBoardRows, LargeBoardRows, LargeBoardColumns, PulsarCellCount)), Is.True, "Pulsar with exactly 48 squares on 60x30");
        }

        /// <summary>The reason names the size problem first, telling a half from the board, and only then the squares.</summary>
        [Test]
        public void GetUnavailableReason_NamesTheFirstFailure()
        {
            var pulsar = PatternTestShapes.Pulsar();
            Assert.That(pulsar.GetUnavailableReason(PlacementBounds.WholeBoard(DefaultHalfEndRow, DefaultBoardColumns)), Is.EqualTo(PatternUnavailableReason.TooBigForBoard), "Pulsar on a 12x12 board");

            var versusHalf = new PlacementBounds(0, VersusHalfEndRow, VersusBoardRows, VersusBoardColumns, int.MaxValue);
            Assert.That(pulsar.GetUnavailableReason(versusHalf), Is.EqualTo(PatternUnavailableReason.TooBigForHalf), "Pulsar on the 12-row half of a 24x30 board");

            var tooBigEitherWay = Square(VersusBoardRows + 1);
            Assert.That(tooBigEitherWay.GetUnavailableReason(versusHalf), Is.EqualTo(PatternUnavailableReason.TooBigForBoard), "a 25x25 shape fits nowhere on a 24x30 board");

            Assert.That(pulsar.GetUnavailableReason(new PlacementBounds(0, DefaultHalfEndRow, DefaultHalfEndRow, DefaultBoardColumns, 0)), Is.EqualTo(PatternUnavailableReason.TooBigForBoard), "size is reported before squares");

            var block = PatternTestShapes.Block();
            Assert.That(block.GetUnavailableReason(new PlacementBounds(0, SmallBoardSize, SmallBoardSize, SmallBoardSize, BlockCellCount - 1)), Is.EqualTo(PatternUnavailableReason.NotEnoughSquares), "Block with 3 squares");
            Assert.That(block.GetUnavailableReason(new PlacementBounds(0, SmallBoardSize, SmallBoardSize, SmallBoardSize, BlockCellCount)), Is.EqualTo(PatternUnavailableReason.None), "Block with 4 squares");
        }

        /// <summary>Positioning starts upright when that fits, otherwise turned once, and reports when neither fits.</summary>
        [Test]
        public void TryGetFirstFittingPose_PrefersUprightThenRotated()
        {
            const int lineLength = 5;
            var line = PatternTestShapes.Pattern(new string('O', lineLength));

            Assert.That(line.TryGetFirstFittingPose(PlacementBounds.WholeBoard(lineLength, 1), out var turnedPose), Is.True, "a 1x5 line on 5 rows by 1 column");
            Assert.That(turnedPose.QuarterTurns, Is.EqualTo(1), "the line is turned once");
            Assert.That(turnedPose.IsMirrored, Is.False, "the line is not mirrored");

            Assert.That(line.TryGetFirstFittingPose(PlacementBounds.WholeBoard(1, lineLength), out var uprightPose), Is.True, "a 1x5 line on 1 row by 5 columns");
            Assert.That(uprightPose.Index, Is.EqualTo(PatternPose.Upright.Index), "the line stays upright");

            Assert.That(line.TryGetFirstFittingPose(PlacementBounds.WholeBoard(2, 2), out _), Is.False, "a 1x5 line on 2 rows by 2 columns");
        }

        /// <summary>The eight poses in table order: two per quarter turn, the mirrored one second.</summary>
        private static PatternPose[] AllPoses()
        {
            var poses = new PatternPose[PoseCount];
            for (var index = 0; index < PoseCount; index++)
            {
                poses[index] = new PatternPose(index / 2, index % 2 == 1);
            }

            return poses;
        }

        /// <summary>Names a pose for assertion messages.</summary>
        private static string Name(PatternPose pose)
        {
            return $"{pose.QuarterTurns} turns{(pose.IsMirrored ? " mirrored" : string.Empty)}";
        }

        /// <summary>Builds a solid square shape of the given size.</summary>
        private static CellPattern Square(int size)
        {
            var rows = new string[size];
            for (var row = 0; row < size; row++)
            {
                rows[row] = new string('O', size);
            }

            return PatternTestShapes.Pattern(rows);
        }
    }
}

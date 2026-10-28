using System;
using System.Collections.Generic;
using GameOfLife.HowToPlay;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that DemoFrameBuilder plays a demo through the real engine: one frame per step, the guide's symbols in Versus and Classic colours, absorption after every step in both modes, and clear errors for malformed boards.</summary>
    [TestFixture]
    public sealed class DemoFrameBuilderTests
    {
        private const char EmptySymbol = '.';
        private const char InkSymbol = 'O';
        private const char BlueSymbol = 'B';
        private const char RedSymbol = 'R';
        private const char YellowSymbol = 'Y';
        private const char UnexpectedOwnerSymbol = '?';
        private const bool VersusColours = true;
        private const bool ClassicColours = false;
        private const int GliderGenerations = 8;

        /// <summary>A glider in the top-left corner of a 6x6 Classic board, drawn with 'O' as the guide's Classic demos are.</summary>
        private static readonly string[] GliderStart =
        {
            ".O....",
            "..O...",
            "OOO...",
            "......",
            "......",
            "......",
        };

        /// <summary>A blue pair and a red pair two rows apart, so they do not touch until a placement joins them.</summary>
        private static readonly string[] SeparatedPairsStart =
        {
            "......",
            ".BB...",
            "......",
            "...RR.",
            "......",
        };

        /// <summary>One blue cell that joins the blue pair into a three-cell cluster and touches the red pair diagonally.</summary>
        private static readonly string[] JoiningPlacement =
        {
            "......",
            "......",
            "..B...",
            "......",
            "......",
        };

        /// <summary>A blue L that grows into a four-cell block in one generation while the red pair dies and is reborn a row lower, touching the block.</summary>
        private static readonly string[] GrowingContactStart =
        {
            "......",
            "..RR..",
            "......",
            "..B...",
            "..BB..",
            "......",
        };

        /// <summary>The first frame shows the start board in Classic colours, ink 'O', blue 'B' and red 'R' each kept apart, and later steps leave it untouched.</summary>
        [Test]
        public void Build_FirstFrame_IsTheStartBoard()
        {
            var start = new[] { "O..", ".B.", "..R" };

            var frames = Build(start, ClassicColours, Generation());

            Assert.That(frames, Has.Length.EqualTo(2), "A demo with one step should have the start frame and one more.");
            Assert.That(Describe(frames[0], start[0].Length, ClassicColours), Is.EqualTo(start), "The first frame should be the start board.");
            Assert.That(Describe(frames[1], start[0].Length, ClassicColours), Is.EqualTo(new[] { "...", ".B.", "..." }), "The generation should change the board without changing the first frame.");
        }

        /// <summary>Generation steps give exactly the boards CellGrid produces when the same glider is advanced directly.</summary>
        [Test]
        public void Build_GenerationSteps_MatchCellGridAdvancedDirectly()
        {
            var steps = new (DemoStepKind kind, string[] cells)[GliderGenerations];
            for (var step = 0; step < steps.Length; step++)
            {
                steps[step] = Generation();
            }

            var frames = Build(GliderStart, ClassicColours, steps);
            var grid = CreateGrid(GliderStart, ClassicColours);

            Assert.That(frames, Has.Length.EqualTo(GliderGenerations + 1), "A demo should have the start frame plus one frame per generation.");
            for (var generation = 1; generation <= GliderGenerations; generation++)
            {
                grid.AdvanceGeneration();
                Assert.That(Describe(frames[generation], grid.Columns, ClassicColours), Is.EqualTo(Describe(grid, ClassicColours)), $"Frame {generation} differs from the glider advanced {generation} generations on CellGrid.");
            }

            Assert.That(Describe(frames[GliderGenerations], grid.Columns, ClassicColours), Is.Not.EqualTo(Describe(frames[0], grid.Columns, ClassicColours)), "The glider should have moved, or the comparison proves nothing.");
        }

        /// <summary>A Classic Place step adds 'O' as ink, 'B' as blue Third and 'R' as red, leaves '.' cells as they were and removes '-' cells.</summary>
        [Test]
        public void Build_PlaceStep_AddsCellsKeepsDotsAndRemovesDashes()
        {
            var start = new[] { "BB..", "..R.", "...R" };

            var frames = Build(start, ClassicColours, Place("-.O.", "..-B", "R..."));

            Assert.That(Describe(frames[1], start[0].Length, ClassicColours), Is.EqualTo(new[] { ".BO.", "...B", "R..R" }), "The placement should add, keep and remove exactly the cells its symbols name.");
            Assert.That(CountCells(frames[1], CellOwner.Third), Is.EqualTo(2), "The kept and the placed 'B' should both be Third, Classic's blue.");
            Assert.That(CountCells(frames[1], CellOwner.Player), Is.EqualTo(1), "Only the placed 'O' should be Player, Classic's ink.");
        }

        /// <summary>Each symbol stands for its colour's owner, on the start board and in a Place step: 'B' is Player in Versus but Third in Classic, and 'Y' is Classic's Fourth.</summary>
        [TestCase(InkSymbol, VersusColours, CellOwner.Player)]
        [TestCase(BlueSymbol, VersusColours, CellOwner.Player)]
        [TestCase(RedSymbol, VersusColours, CellOwner.Opponent)]
        [TestCase(InkSymbol, ClassicColours, CellOwner.Player)]
        [TestCase(BlueSymbol, ClassicColours, CellOwner.Third)]
        [TestCase(RedSymbol, ClassicColours, CellOwner.Opponent)]
        [TestCase(YellowSymbol, ClassicColours, CellOwner.Fourth)]
        public void Build_Symbol_StandsForItsColoursOwner(char symbol, bool versusColours, CellOwner expectedOwner)
        {
            var colours = versusColours ? "Versus" : "Classic";
            var start = new[] { $"{symbol}.." };

            var frames = Build(start, versusColours, Place($"..{symbol}"));

            Assert.That(frames[0][0], Is.EqualTo(expectedOwner), $"'{symbol}' on a {colours} start board should be {expectedOwner}.");
            Assert.That(frames[1][2], Is.EqualTo(expectedOwner), $"'{symbol}' placed in {colours} colours should be {expectedOwner}.");
            Assert.That(frames[1][0], Is.EqualTo(expectedOwner), $"The start cell should keep its {expectedOwner} owner after the placement.");
        }

        /// <summary>Versus has no yellow, so 'Y' is refused on a Versus start board and in a Versus Place step.</summary>
        [Test]
        public void Build_YellowInVersusColours_Throws()
        {
            Assert.That(() => Build(new[] { "Y.." }, VersusColours), Throws.ArgumentException, "'Y' on a Versus start board should be refused.");
            Assert.That(() => Build(new[] { "..." }, VersusColours, Place("..Y")), Throws.ArgumentException, "'Y' in a Versus placement should be refused.");
        }

        /// <summary>In Versus colours, a placement that makes a three-cell blue cluster touch the red pair turns the pair blue in the placement's own frame.</summary>
        [Test]
        public void Build_VersusPlaceJoiningABiggerBlueCluster_ConvertsTheRedPairInThatFrame()
        {
            var frames = Build(SeparatedPairsStart, VersusColours, Place(JoiningPlacement));

            Assert.That(CountCells(frames[0], CellOwner.Opponent), Is.EqualTo(2), "The red pair should be red before the placement.");
            Assert.That(Describe(frames[1], SeparatedPairsStart[0].Length, VersusColours), Is.EqualTo(new[]
            {
                "......",
                ".BB...",
                "..B...",
                "...BB.",
                "......",
            }), "The three-cell blue cluster should absorb the touching red pair in the placement's frame.");
        }

        /// <summary>In Versus colours a generation is followed by absorption too, matching CellGrid then ClusterAbsorber run by hand.</summary>
        [Test]
        public void Build_VersusGenerationStep_ResolvesAbsorptionLikeCellGridAndClusterAbsorber()
        {
            AssertGenerationStepResolvesAbsorption(VersusColours, CellOwner.Player);
        }

        /// <summary>Classic absorbs too: the same joining placement in Classic colours turns the red pair Classic's blue, Third, in the placement's own frame.</summary>
        [Test]
        public void Build_ClassicPlaceJoiningABiggerBlueCluster_AbsorbsTheRedPairToo()
        {
            var frames = Build(SeparatedPairsStart, ClassicColours, Place(JoiningPlacement));

            Assert.That(CountCells(frames[0], CellOwner.Opponent), Is.EqualTo(2), "The red pair should be red before the placement.");
            Assert.That(Describe(frames[1], SeparatedPairsStart[0].Length, ClassicColours), Is.EqualTo(new[]
            {
                "......",
                ".BB...",
                "..B...",
                "...BB.",
                "......",
            }), "The three-cell blue cluster should absorb the touching red pair in Classic as in Versus.");
            Assert.That(CountCells(frames[1], CellOwner.Third), Is.EqualTo(5), "Every blue cell, the absorbed pair included, should be Third in Classic colours.");
        }

        /// <summary>Classic absorbs after a generation too, matching CellGrid then ClusterAbsorber run by hand.</summary>
        [Test]
        public void Build_ClassicGenerationStep_ResolvesAbsorptionToo()
        {
            AssertGenerationStepResolvesAbsorption(ClassicColours, CellOwner.Third);
        }

        /// <summary>A different number of step kinds and step cells is refused, whichever list is longer.</summary>
        [TestCase(2, 1)]
        [TestCase(1, 2)]
        public void Build_StepKindsAndCellsOfDifferentCounts_Throws(int kindCount, int cellsCount)
        {
            var kinds = new DemoStepKind[kindCount];
            for (var step = 0; step < kindCount; step++)
            {
                kinds[step] = DemoStepKind.Generation;
            }

            var cells = new IReadOnlyList<string>[cellsCount];
            for (var step = 0; step < cellsCount; step++)
            {
                cells[step] = Array.Empty<string>();
            }

            Assert.That(() => DemoFrameBuilder.Build(new[] { "..." }, kinds, cells, ClassicColours), Throws.ArgumentException, $"{kindCount} step kinds with {cellsCount} sets of cells should be refused.");
        }

        /// <summary>A start board with no rows is refused.</summary>
        [Test]
        public void Build_EmptyStartBoard_Throws()
        {
            Assert.That(() => Build(Array.Empty<string>(), ClassicColours), Throws.ArgumentException, "An empty start board should be refused.");
        }

        /// <summary>A start board whose rows differ in width is refused, whether a later row is narrower or wider.</summary>
        [TestCase("..")]
        [TestCase("....")]
        public void Build_RaggedStartRows_Throws(string secondRow)
        {
            var start = new[] { "...", secondRow };

            Assert.That(() => Build(start, ClassicColours), Throws.ArgumentException, $"A start board with rows of {start[0].Length} and {secondRow.Length} symbols should be refused.");
        }

        /// <summary>A Place step with fewer or more rows than the board is refused.</summary>
        [TestCase(2)]
        [TestCase(4)]
        public void Build_PlaceStepWithTheWrongRowCount_Throws(int placementRowCount)
        {
            var start = new[] { "...", "...", "..." };
            var placement = new string[placementRowCount];
            for (var row = 0; row < placementRowCount; row++)
            {
                placement[row] = "O..";
            }

            Assert.That(() => Build(start, ClassicColours, Place(placement)), Throws.ArgumentException, $"A {placementRowCount}-row placement on a {start.Length}-row board should be refused.");
        }

        /// <summary>A symbol the guide does not define is refused on the start board.</summary>
        [Test]
        public void Build_UnknownSymbolOnTheStartBoard_Throws()
        {
            Assert.That(() => Build(new[] { "..X" }, ClassicColours), Throws.ArgumentException, "'X' on the start board should be refused.");
        }

        /// <summary>A symbol the guide does not define is refused in a Place step.</summary>
        [Test]
        public void Build_UnknownSymbolInAPlaceStep_Throws()
        {
            Assert.That(() => Build(new[] { "..." }, ClassicColours, Place(".X.")), Throws.ArgumentException, "'X' in a placement should be refused.");
        }

        /// <summary>'-' only means something in a Place step, so it is refused on the start board.</summary>
        [Test]
        public void Build_RemovalOnTheStartBoard_Throws()
        {
            Assert.That(() => Build(new[] { ".-." }, ClassicColours), Throws.ArgumentException, "'-' on the start board should be refused.");
        }

        /// <summary>Plays the growing-contact board one generation in the given colours and checks the frame matches CellGrid advanced and then resolved by ClusterAbsorber, with the red cells absorbed into the blue owner.</summary>
        private static void AssertGenerationStepResolvesAbsorption(bool versusColours, CellOwner blueOwner)
        {
            var frames = Build(GrowingContactStart, versusColours, Generation());
            var grid = CreateGrid(GrowingContactStart, versusColours);

            grid.AdvanceGeneration();
            Assert.That(grid.CountCells(CellOwner.Opponent), Is.GreaterThan(0), "The generation alone should leave red cells for absorption to act on.");
            var convertedCellCount = new ClusterAbsorber(grid.MaximumCellCount).ResolveAbsorptions(grid);

            Assert.That(convertedCellCount, Is.GreaterThan(0), "The generation should bring the blue block into contact with the smaller red pair.");
            Assert.That(Describe(frames[1], grid.Columns, versusColours), Is.EqualTo(Describe(grid, versusColours)), "The generation's frame should match CellGrid advanced and then resolved by ClusterAbsorber.");
            Assert.That(CountCells(frames[1], CellOwner.Opponent), Is.Zero, "No red cell should survive the absorption in the generation's frame.");
            Assert.That(CountCells(frames[1], blueOwner), Is.EqualTo(grid.CountCells(blueOwner)).And.GreaterThan(0), $"The blue cells should all be {blueOwner}.");
        }

        /// <summary>Builds a demo from its start board and its steps, each a kind with its rows of cells.</summary>
        private static CellOwner[][] Build(IReadOnlyList<string> start, bool versusColours, params (DemoStepKind kind, string[] cells)[] steps)
        {
            var kinds = new DemoStepKind[steps.Length];
            var cells = new IReadOnlyList<string>[steps.Length];
            for (var step = 0; step < steps.Length; step++)
            {
                kinds[step] = steps[step].kind;
                cells[step] = steps[step].cells;
            }

            return DemoFrameBuilder.Build(start, kinds, cells, versusColours);
        }

        /// <summary>Returns a Place step drawing the given rows.</summary>
        private static (DemoStepKind kind, string[] cells) Place(params string[] rows)
        {
            return (DemoStepKind.Place, rows);
        }

        /// <summary>Returns a Generation step, which needs no cells.</summary>
        private static (DemoStepKind kind, string[] cells) Generation()
        {
            return (DemoStepKind.Generation, Array.Empty<string>());
        }

        /// <summary>Returns a board sized to the rows with each symbol set as its owner in the given colours, independently of the builder: 'O' Player, 'B' Player in Versus or Third in Classic, 'R' Opponent and 'Y' Fourth.</summary>
        private static CellGrid CreateGrid(IReadOnlyList<string> rows, bool versusColours)
        {
            var grid = new CellGrid(rows.Count, rows[0].Length);
            grid.Resize(rows.Count, rows[0].Length);
            for (var row = 0; row < rows.Count; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), ToOwner(rows[row][column], versusColours));
                }
            }

            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Returns the owner a test board symbol stands for in the given colours.</summary>
        private static CellOwner ToOwner(char symbol, bool versusColours)
        {
            switch (symbol)
            {
                case EmptySymbol:
                    return CellOwner.None;
                case InkSymbol:
                    return CellOwner.Player;
                case BlueSymbol:
                    return versusColours ? CellOwner.Player : CellOwner.Third;
                case RedSymbol:
                    return CellOwner.Opponent;
                case YellowSymbol when !versusColours:
                    return CellOwner.Fourth;
                default:
                    throw new ArgumentException($"'{symbol}' is not a test board symbol in these colours.", nameof(symbol));
            }
        }

        /// <summary>Counts the cells in a frame with exactly this owner.</summary>
        private static int CountCells(CellOwner[] frame, CellOwner owner)
        {
            var count = 0;
            foreach (var cellOwner in frame)
            {
                if (cellOwner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Renders a grid's cells in the given colours, as Describe renders a frame.</summary>
        private static string[] Describe(CellGrid grid, bool versusColours)
        {
            var owners = new CellOwner[grid.CellCount];
            for (var cellIndex = 0; cellIndex < owners.Length; cellIndex++)
            {
                owners[cellIndex] = grid.GetOwner(cellIndex);
            }

            return Describe(owners, grid.Columns, versusColours);
        }

        /// <summary>Renders a frame as rows of the colour each cell shows, top row first: in Versus 'B' for Player and 'R' for Opponent; in Classic 'O' for Player, 'B' for Third, 'R' for Opponent and 'Y' for Fourth; '.' for empty and '?' for an owner the colours have no symbol for.</summary>
        private static string[] Describe(CellOwner[] frame, int columns, bool versusColours)
        {
            var rows = new string[frame.Length / columns];
            var symbols = new char[columns];
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    symbols[column] = ToSymbol(frame[row * columns + column], versusColours);
                }

                rows[row] = new string(symbols);
            }

            return rows;
        }

        /// <summary>Returns the symbol for the colour an owner shows in the given colours, '?' for an owner those colours do not have.</summary>
        private static char ToSymbol(CellOwner owner, bool versusColours)
        {
            switch (owner)
            {
                case CellOwner.None:
                    return EmptySymbol;
                case CellOwner.Player:
                    return versusColours ? BlueSymbol : InkSymbol;
                case CellOwner.Opponent:
                    return RedSymbol;
                case CellOwner.Third when !versusColours:
                    return BlueSymbol;
                case CellOwner.Fourth when !versusColours:
                    return YellowSymbol;
                default:
                    return UnexpectedOwnerSymbol;
            }
        }
    }
}

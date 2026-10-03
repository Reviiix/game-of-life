using System;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that ClusterAbsorber converts smaller touching enemy clusters in the agreed deterministic order.</summary>
    [TestFixture]
    public sealed class ClusterAbsorberTests
    {
        private const char EmptySymbol = '.';
        private const char PlayerSymbol = 'P';
        private const char OpponentSymbol = 'O';

        /// <summary>A Player cluster converts a smaller Opponent cluster it touches.</summary>
        [Test]
        public void LargerPlayerCluster_AbsorbsSmallerTouchingOpponentCluster()
        {
            var grid = CreateGrid(
                "......",
                ".PPPO.",
                "......");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(1));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "......",
                ".PPPP.",
                "......"
            }));
        }

        /// <summary>An Opponent cluster converts a smaller Player cluster it touches.</summary>
        [Test]
        public void LargerOpponentCluster_AbsorbsSmallerTouchingPlayerCluster()
        {
            var grid = CreateGrid(
                "......",
                ".OOP..",
                "......");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(1));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "......",
                ".OOO..",
                "......"
            }));
        }

        /// <summary>Clusters touching only at a corner still count as touching.</summary>
        [Test]
        public void DiagonalContact_CountsAsTouching()
        {
            var grid = CreateGrid(
                ".....",
                ".PP..",
                "...O.",
                ".....");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(1));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".PP..",
                "...P.",
                "....."
            }));
        }

        /// <summary>Same-colour cells joined only at a corner form one cluster, so their combined size counts.</summary>
        [Test]
        public void DiagonalSameColourCells_FormOneCluster()
        {
            var grid = CreateGrid(
                ".....",
                ".P...",
                "..PO.",
                ".....");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(1));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".P...",
                "..PP.",
                "....."
            }));
        }

        /// <summary>Touching clusters of equal size leave each other alone.</summary>
        [Test]
        public void EqualSizedTouchingClusters_Stand()
        {
            var pattern = new[]
            {
                "......",
                ".PPOO.",
                "......"
            };
            var grid = CreateGrid(pattern);

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.Zero);
            Assert.That(DescribeGrid(grid), Is.EqualTo(pattern));
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>Clusters of the same colour never convert each other, whatever their sizes.</summary>
        [Test]
        public void SameColourClusters_NeverConvert()
        {
            var pattern = new[]
            {
                ".......",
                ".PPP.P.",
                ".......",
                ".......",
                ".OOO.O.",
                "......."
            };
            var grid = CreateGrid(pattern);

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.Zero);
            Assert.That(DescribeGrid(grid), Is.EqualTo(pattern));
        }

        /// <summary>Enemy clusters with a gap between them do not interact.</summary>
        [Test]
        public void ClustersWithAGapBetweenThem_DoNotInteract()
        {
            var pattern = new[]
            {
                ".......",
                ".PPP.O.",
                "......."
            };
            var grid = CreateGrid(pattern);

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.Zero);
            Assert.That(DescribeGrid(grid), Is.EqualTo(pattern));
        }

        /// <summary>One cluster converts every smaller enemy cluster it touches, judging each one by its own size.</summary>
        [Test]
        public void OneClusterTouchingTwoSmallerClusters_ConvertsBoth()
        {
            var grid = CreateGrid(
                ".........",
                ".OOPPPOO.",
                ".........");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(4));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".........",
                ".PPPPPPP.",
                "........."
            }));
        }

        /// <summary>A cluster that grows by absorbing can then absorb a cluster that used to be its equal.</summary>
        [Test]
        public void GrowingCluster_TriggersChainReaction()
        {
            var grid = CreateGrid(
                "...........",
                ".OOOOPPPPO.",
                "...........");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(5));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "...........",
                ".PPPPPPPPP.",
                "..........."
            }));
        }

        /// <summary>The largest absorbing cluster acts first, so the middle cluster cannot grow by absorbing first.</summary>
        [Test]
        public void LargestAbsorbingCluster_ResolvesFirst()
        {
            var grid = CreateGrid(
                "............",
                ".PPOOOPPPPP.",
                "............");

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(3));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "............",
                ".PPPPPPPPPP.",
                "............"
            }));
        }

        /// <summary>Between equally large absorbers, the one containing the lowest cell index acts first, whatever its colour.</summary>
        [TestCase(".OPPPOOOP.", ".PPPPPPPP.")]
        [TestCase(".POOOPPPO.", ".OOOOOOOO.")]
        public void EqualSizedAbsorbers_ResolveFromLowestCellIndex(string startRow, string expectedRow)
        {
            var emptyRow = new string(EmptySymbol, startRow.Length);
            var grid = CreateGrid(emptyRow, startRow, emptyRow);

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.EqualTo(4));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[] { emptyRow, expectedRow, emptyRow }));
        }

        /// <summary>Every converted cell is recorded in the grid's change list.</summary>
        [Test]
        public void ConvertedCells_AreRecordedAsChanges()
        {
            var grid = CreateGrid(
                ".......",
                ".OPPPO.",
                ".......");

            Resolve(grid);

            Assert.That(grid.ChangedCellIndices, Is.EquivalentTo(new[] { grid.ToCellIndex(1, 1), grid.ToCellIndex(1, 5) }));
        }

        /// <summary>Resolving a board that is already stable converts nothing.</summary>
        [Test]
        public void ResolvingAStableBoardAgain_ConvertsNothing()
        {
            var grid = CreateGrid(
                ".........",
                ".OOPPPOO.",
                "...O.....");
            var absorber = new ClusterAbsorber(grid.MaximumCellCount);
            absorber.ResolveAbsorptions(grid);
            grid.ClearChangedCells();

            var convertedCellCount = absorber.ResolveAbsorptions(grid);

            Assert.That(convertedCellCount, Is.Zero);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>An absorber cannot resolve a grid larger than the size it preallocated for.</summary>
        [Test]
        public void GridLargerThanMaximum_Throws()
        {
            var grid = CreateGrid(
                "...",
                ".P.",
                "...");
            var absorber = new ClusterAbsorber(4);

            Assert.Throws<ArgumentException>(() => absorber.ResolveAbsorptions(grid));
        }

        /// <summary>Clusters that sit next to each other only in the flat index, across a row end, do not touch.</summary>
        [Test]
        public void CellsEitherSideOfARowEnd_DoNotTouch()
        {
            var pattern = new[]
            {
                "..PP",
                "O...",
                "...."
            };
            var grid = CreateGrid(pattern);

            var convertedCellCount = Resolve(grid);

            Assert.That(convertedCellCount, Is.Zero);
            Assert.That(DescribeGrid(grid), Is.EqualTo(pattern));
        }

        /// <summary>One absorber reused on a grid that shrank below its maximum resolves only the new active area.</summary>
        [Test]
        public void ReusedAbsorber_ResolvesAGridThatShrankBelowItsMaximum()
        {
            var grid = new CellGrid(6, 6);
            var absorber = new ClusterAbsorber(grid.MaximumCellCount);
            LoadPattern(grid,
                "PPPPOO",
                "PPPPOO",
                "PPPPOO",
                "PPPPOO",
                "PPPPOO",
                "PPPPOO");
            Assert.That(absorber.ResolveAbsorptions(grid), Is.EqualTo(12));

            LoadPattern(grid,
                "....",
                ".OOP",
                "O...");
            var convertedCellCount = absorber.ResolveAbsorptions(grid);

            Assert.That(convertedCellCount, Is.EqualTo(1));
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "....",
                ".OOO",
                "O..."
            }));
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { grid.ToCellIndex(1, 3) }));
        }

        /// <summary>A grid copied from a differently shaped source resolves with the source's adjacency and ignores its own stale cells.</summary>
        [Test]
        public void GridCopiedFromADifferentShape_ResolvesWithTheSourceAdjacency()
        {
            var source = CreateGrid(
                "PP.....O..",
                ".O........");
            var copy = new CellGrid(6, 6);
            copy.Resize(6, 6);
            for (var cellIndex = 0; cellIndex < copy.CellCount; cellIndex++)
            {
                copy.SetOwner(cellIndex, CellOwner.Opponent);
            }

            copy.CopyFrom(source);
            var convertedCellCount = new ClusterAbsorber(copy.MaximumCellCount).ResolveAbsorptions(copy);

            Assert.That(convertedCellCount, Is.EqualTo(1));
            Assert.That(DescribeGrid(copy), Is.EqualTo(new[]
            {
                "PP.....O..",
                ".P........"
            }));
            Assert.That(copy.CountCells(CellOwner.Opponent), Is.EqualTo(1));
        }

        /// <summary>Resolves absorptions with a fresh absorber sized for the grid and returns the converted cell count.</summary>
        private static int Resolve(CellGrid grid)
        {
            return new ClusterAbsorber(grid.MaximumCellCount).ResolveAbsorptions(grid);
        }

        /// <summary>Builds a grid sized exactly to rows of text where '.' is empty, 'P' is Player and 'O' is Opponent, with no recorded changes.</summary>
        private static CellGrid CreateGrid(params string[] rows)
        {
            var grid = new CellGrid(rows.Length, rows[0].Length);
            LoadPattern(grid, rows);
            return grid;
        }

        /// <summary>Resizes an existing grid to rows of text in the CreateGrid format and fills it, leaving no recorded changes.</summary>
        private static void LoadPattern(CellGrid grid, params string[] rows)
        {
            grid.Resize(rows.Length, rows[0].Length);
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), ParseOwner(rows[row][column]));
                }
            }

            grid.ClearChangedCells();
        }

        /// <summary>Renders the grid as rows of text in the format CreateGrid reads.</summary>
        private static string[] DescribeGrid(CellGrid grid)
        {
            var rows = new string[grid.Rows];
            var symbols = new char[grid.Columns];
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var column = 0; column < grid.Columns; column++)
                {
                    symbols[column] = ToSymbol(grid.GetOwner(grid.ToCellIndex(row, column)));
                }

                rows[row] = new string(symbols);
            }

            return rows;
        }

        /// <summary>Converts a pattern character into the owner it stands for.</summary>
        private static CellOwner ParseOwner(char symbol)
        {
            switch (symbol)
            {
                case PlayerSymbol:
                    return CellOwner.Player;
                case OpponentSymbol:
                    return CellOwner.Opponent;
                case EmptySymbol:
                    return CellOwner.None;
                default:
                    throw new ArgumentException($"Unknown cell symbol '{symbol}'.", nameof(symbol));
            }
        }

        /// <summary>Converts an owner into its pattern character.</summary>
        private static char ToSymbol(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player:
                    return PlayerSymbol;
                case CellOwner.Opponent:
                    return OpponentSymbol;
                default:
                    return EmptySymbol;
            }
        }
    }
}

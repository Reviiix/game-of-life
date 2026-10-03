using System;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks the two-colour Conway rules, grid geometry and change tracking of CellGrid.</summary>
    [TestFixture]
    public sealed class CellGridTests
    {
        private const char EmptySymbol = '.';
        private const char PlayerSymbol = 'P';
        private const char OpponentSymbol = 'O';

        /// <summary>A blinker flips between horizontal and vertical every generation.</summary>
        [Test]
        public void Blinker_OscillatesWithPeriodTwo()
        {
            var grid = CreateGrid(
                ".....",
                ".....",
                ".PPP.",
                ".....",
                ".....");

            grid.AdvanceGeneration();
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                "..P..",
                "..P..",
                "..P..",
                "....."
            }));

            grid.AdvanceGeneration();
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".....",
                ".PPP.",
                ".....",
                "....."
            }));
        }

        /// <summary>A block is a still life and never changes.</summary>
        [Test]
        public void Block_StaysStill()
        {
            var expected = new[]
            {
                "....",
                ".PP.",
                ".PP.",
                "...."
            };
            var grid = CreateGrid(expected);

            for (var generation = 0; generation < 5; generation++)
            {
                grid.AdvanceGeneration();
            }

            Assert.That(DescribeGrid(grid), Is.EqualTo(expected));
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>A glider reappears one cell down and one cell right after four generations.</summary>
        [Test]
        public void Glider_MovesOneCellDiagonallyAfterFourGenerations()
        {
            var grid = CreateGrid(
                ".P....",
                "..P...",
                "PPP...",
                "......",
                "......",
                "......");

            for (var generation = 0; generation < 4; generation++)
            {
                grid.AdvanceGeneration();
            }

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "......",
                "..P...",
                "...P..",
                ".PPP..",
                "......",
                "......"
            }));
        }

        /// <summary>A blinker on the top edge loses its upper birth instead of wrapping to the bottom row.</summary>
        [Test]
        public void TopAndBottomEdges_DoNotWrap()
        {
            var grid = CreateGrid(
                ".PPP.",
                ".....",
                ".....",
                ".....",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "..P..",
                "..P..",
                ".....",
                ".....",
                "....."
            }));
        }

        /// <summary>A blinker on the left edge loses its left birth instead of wrapping to the right column.</summary>
        [Test]
        public void LeftAndRightEdges_DoNotWrap()
        {
            var grid = CreateGrid(
                ".....",
                "P....",
                "P....",
                "P....",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".....",
                "PP...",
                ".....",
                "....."
            }));
        }

        /// <summary>Corner cells have three neighbours, edge cells five and inner cells eight.</summary>
        [Test]
        public void GetNeighbourCount_IsThreeInCornersFiveOnEdgesAndEightInside()
        {
            var grid = CreateEmptyGrid(4, 5);

            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(0, 0)), Is.EqualTo(3));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(3, 4)), Is.EqualTo(3));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(0, 2)), Is.EqualTo(5));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(2, 0)), Is.EqualTo(5));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(1, 1)), Is.EqualTo(8));
        }

        /// <summary>The neighbour slots of a corner cell list exactly its three in-bounds neighbours.</summary>
        [Test]
        public void GetNeighbourIndex_ListsOnlyInBoundsNeighbours()
        {
            var grid = CreateEmptyGrid(3, 3);
            var cornerIndex = grid.ToCellIndex(0, 0);
            var neighbours = new int[grid.GetNeighbourCount(cornerIndex)];

            for (var neighbourSlot = 0; neighbourSlot < neighbours.Length; neighbourSlot++)
            {
                neighbours[neighbourSlot] = grid.GetNeighbourIndex(cornerIndex, neighbourSlot);
            }

            Assert.That(neighbours, Is.EquivalentTo(new[] { grid.ToCellIndex(0, 1), grid.ToCellIndex(1, 0), grid.ToCellIndex(1, 1) }));
        }

        /// <summary>Row, column and flat index conversions agree with each other.</summary>
        [Test]
        public void RowOfAndColumnOf_InvertToCellIndex()
        {
            var grid = CreateEmptyGrid(4, 7);
            var cellIndex = grid.ToCellIndex(3, 5);

            Assert.That(cellIndex, Is.EqualTo(26));
            Assert.That(grid.RowOf(cellIndex), Is.EqualTo(3));
            Assert.That(grid.ColumnOf(cellIndex), Is.EqualTo(5));
        }

        /// <summary>A newborn takes the colour of the majority of its three parents.</summary>
        [Test]
        public void NewbornCell_TakesMajorityColourOfItsParents()
        {
            var playerMajorityGrid = CreateGrid(
                ".....",
                ".....",
                ".PPO.",
                ".....",
                ".....");
            var opponentMajorityGrid = CreateGrid(
                ".....",
                ".....",
                ".OOP.",
                ".....",
                ".....");

            playerMajorityGrid.AdvanceGeneration();
            opponentMajorityGrid.AdvanceGeneration();

            Assert.That(DescribeGrid(playerMajorityGrid), Is.EqualTo(new[]
            {
                ".....",
                "..P..",
                "..P..",
                "..P..",
                "....."
            }));
            Assert.That(DescribeGrid(opponentMajorityGrid), Is.EqualTo(new[]
            {
                ".....",
                "..O..",
                "..O..",
                "..O..",
                "....."
            }));
        }

        /// <summary>A surviving cell keeps its colour even when its neighbours are the other colour.</summary>
        [Test]
        public void SurvivingCell_KeepsItsColour()
        {
            var grid = CreateGrid(
                ".....",
                ".....",
                ".POP.",
                ".....",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                "..P..",
                "..O..",
                "..P..",
                "....."
            }));
        }

        /// <summary>A board of only Player cells never produces an Opponent cell, so classic mode is unaffected by colours.</summary>
        [Test]
        public void PlayerOnlyBoard_NeverProducesOpponentCells()
        {
            var grid = CreateEmptyGrid(20, 20);
            var random = new Random(1234);
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                if (random.NextDouble() < 0.35)
                {
                    grid.ToggleCell(cellIndex);
                }
            }

            for (var generation = 0; generation < 100; generation++)
            {
                grid.AdvanceGeneration();
                Assert.That(grid.CountCells(CellOwner.Opponent), Is.Zero);
            }
        }

        /// <summary>Toggling switches a cell between empty and Player and records each change.</summary>
        [Test]
        public void ToggleCell_SwitchesBetweenEmptyAndPlayer()
        {
            var grid = CreateEmptyGrid(3, 3);
            var cellIndex = grid.ToCellIndex(1, 1);

            grid.ToggleCell(cellIndex);
            Assert.That(grid.GetOwner(cellIndex), Is.EqualTo(CellOwner.Player));
            Assert.That(grid.IsAlive(cellIndex), Is.True);
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { cellIndex }));

            grid.ClearChangedCells();
            grid.ToggleCell(cellIndex);
            Assert.That(grid.GetOwner(cellIndex), Is.EqualTo(CellOwner.None));
            Assert.That(grid.IsAlive(cellIndex), Is.False);
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { cellIndex }));
        }

        /// <summary>Counting by owner counts living cells per side and empty cells for None.</summary>
        [Test]
        public void CountCells_CountsEachOwnerSeparately()
        {
            var grid = CreateGrid(
                "PP.",
                ".O.",
                "..P");

            Assert.That(grid.CountCells(CellOwner.Player), Is.EqualTo(3));
            Assert.That(grid.CountCells(CellOwner.Opponent), Is.EqualTo(1));
            Assert.That(grid.CountCells(CellOwner.None), Is.EqualTo(5));
            Assert.That(grid.HasAnyLivingCell(), Is.True);
        }

        /// <summary>Setting a cell to the owner it already has records nothing.</summary>
        [Test]
        public void SetOwner_ToSameOwner_RecordsNoChange()
        {
            var grid = CreateEmptyGrid(3, 3);

            grid.SetOwner(4, CellOwner.None);
            Assert.That(grid.ChangedCellIndices, Is.Empty);

            grid.SetOwner(4, CellOwner.Opponent);
            grid.ClearChangedCells();
            grid.SetOwner(4, CellOwner.Opponent);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>A cell that changes several times appears once, in the order of its first change.</summary>
        [Test]
        public void ChangedCellIndices_ListsEachCellOnceInOrderOfFirstChange()
        {
            var grid = CreateEmptyGrid(3, 3);

            grid.SetOwner(5, CellOwner.Player);
            grid.SetOwner(2, CellOwner.Player);
            grid.SetOwner(5, CellOwner.Opponent);
            grid.SetOwner(5, CellOwner.None);

            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 5, 2 }));
        }

        /// <summary>Changes from placements and generations accumulate without duplicates until the caller clears them.</summary>
        [Test]
        public void ChangedCellIndices_AccumulatesAcrossGenerationsUntilCleared()
        {
            var grid = CreateEmptyGrid(5, 5);
            grid.ToggleCell(grid.ToCellIndex(2, 1));
            grid.ToggleCell(grid.ToCellIndex(2, 2));
            grid.ToggleCell(grid.ToCellIndex(2, 3));
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 11, 12, 13 }));

            grid.AdvanceGeneration();
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 11, 12, 13, 7, 17 }));

            grid.AdvanceGeneration();
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 11, 12, 13, 7, 17 }));

            grid.ClearChangedCells();
            Assert.That(grid.ChangedCellIndices, Is.Empty);

            grid.AdvanceGeneration();
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 7, 11, 13, 17 }));
        }

        /// <summary>Killing every cell empties the board and the change list.</summary>
        [Test]
        public void KillAllCells_EmptiesBoardAndChangeList()
        {
            var grid = CreateGrid(
                "PO.",
                ".P.",
                "..O");
            grid.SetOwner(2, CellOwner.Player);

            grid.KillAllCells();

            Assert.That(grid.HasAnyLivingCell(), Is.False);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>Resizing changes the dimensions, kills every cell, rebuilds neighbours and clears the change list.</summary>
        [Test]
        public void Resize_KillsCellsRebuildsNeighboursAndClearsChanges()
        {
            var grid = new CellGrid(6, 6);
            grid.Resize(6, 6);
            grid.SetOwner(grid.ToCellIndex(1, 1), CellOwner.Player);

            grid.Resize(3, 4);

            Assert.That(grid.Rows, Is.EqualTo(3));
            Assert.That(grid.Columns, Is.EqualTo(4));
            Assert.That(grid.CellCount, Is.EqualTo(12));
            Assert.That(grid.MaximumCellCount, Is.EqualTo(36));
            Assert.That(grid.HasAnyLivingCell(), Is.False);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(0, 3)), Is.EqualTo(3));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(1, 2)), Is.EqualTo(8));
        }

        /// <summary>Resizing beyond the preallocated maximum is rejected.</summary>
        [Test]
        public void Resize_BeyondMaximum_Throws()
        {
            var grid = new CellGrid(4, 4);

            Assert.Throws<ArgumentException>(() => grid.Resize(5, 4));
        }

        /// <summary>A copy matches its source's dimensions and cells, records no changes and evolves identically.</summary>
        [Test]
        public void CopyFrom_CopiesDimensionsCellsAndNeighboursWithoutRecordingChanges()
        {
            var source = CreateGrid(
                ".....",
                ".POP.",
                "..O..",
                ".P...",
                ".....");
            var copy = new CellGrid(6, 6);
            copy.Resize(6, 6);
            copy.SetOwner(0, CellOwner.Opponent);

            copy.CopyFrom(source);

            Assert.That(copy.Rows, Is.EqualTo(5));
            Assert.That(copy.Columns, Is.EqualTo(5));
            Assert.That(DescribeGrid(copy), Is.EqualTo(DescribeGrid(source)));
            Assert.That(copy.ChangedCellIndices, Is.Empty);

            for (var generation = 0; generation < 3; generation++)
            {
                source.AdvanceGeneration();
                copy.AdvanceGeneration();
            }

            Assert.That(DescribeGrid(copy), Is.EqualTo(DescribeGrid(source)));
        }

        /// <summary>A copy into a bigger grid full of stale cells uses the source's shape and never reads the stale cells past the copied area.</summary>
        [Test]
        public void CopyFrom_IntoALargerDirtyGrid_IgnoresCellsPastTheCopiedArea()
        {
            var source = CreateGrid(
                "........",
                ".PPP....",
                "........");
            var copy = CreateEmptyGrid(6, 6);
            for (var cellIndex = 0; cellIndex < copy.CellCount; cellIndex++)
            {
                copy.SetOwner(cellIndex, CellOwner.Opponent);
            }

            copy.CopyFrom(source);
            copy.AdvanceGeneration();

            Assert.That(DescribeGrid(copy), Is.EqualTo(new[]
            {
                "..P.....",
                "..P.....",
                "..P....."
            }));
            Assert.That(copy.CountCells(CellOwner.Opponent), Is.Zero);
        }

        /// <summary>Copying repeatedly from same-shaped and differently shaped sources keeps the neighbour table matching each source.</summary>
        [Test]
        public void CopyFrom_SwitchingBetweenShapes_KeepsNeighboursMatchingEachSource()
        {
            var wideSource = CreateGrid(
                "......",
                "PPP...",
                "......");
            var tallSource = CreateGrid(
                "..O",
                "..O",
                "..O",
                "...",
                "...",
                "...");
            var copy = new CellGrid(6, 6);

            foreach (var source in new[] { wideSource, wideSource, tallSource, tallSource, wideSource })
            {
                copy.CopyFrom(source);
                copy.AdvanceGeneration();
                var expected = CreateGrid(DescribeGrid(source));
                expected.AdvanceGeneration();

                Assert.That(DescribeGrid(copy), Is.EqualTo(DescribeGrid(expected)));
                Assert.That(copy.GetNeighbourCount(copy.CellCount - 1), Is.EqualTo(3));
            }
        }

        /// <summary>Resize reports an oversized request as an ArgumentException, even when rows times columns overflows an int, and keeps the old size.</summary>
        [TestCase(46341, 46341)]
        [TestCase(65536, 65536)]
        [TestCase(-1, 4)]
        [TestCase(4, -1)]
        public void Resize_ToAnImpossibleSize_ThrowsAndKeepsTheOldSize(int rows, int columns)
        {
            var grid = CreateEmptyGrid(3, 4);

            Assert.Throws<ArgumentException>(() => grid.Resize(rows, columns));
            Assert.That(grid.Rows, Is.EqualTo(3));
            Assert.That(grid.Columns, Is.EqualTo(4));
        }

        /// <summary>A grid whose buffers could not be allocated is rejected up front.</summary>
        [TestCase(-1, 4)]
        [TestCase(4, -1)]
        [TestCase(46341, 46341)]
        public void Constructor_WithAnImpossibleMaximum_Throws(int maximumRows, int maximumColumns)
        {
            Assert.Throws<ArgumentException>(() => new CellGrid(maximumRows, maximumColumns));
        }

        /// <summary>Copying a grid that is larger than this grid's maximum is rejected.</summary>
        [Test]
        public void CopyFrom_SourceLargerThanMaximum_Throws()
        {
            var source = CreateEmptyGrid(5, 5);
            var copy = new CellGrid(4, 4);

            Assert.Throws<ArgumentException>(() => copy.CopyFrom(source));
        }

        /// <summary>Creates an empty grid sized exactly to the given dimensions.</summary>
        /// <summary>HasLivingNeighbour sees living cells of either colour on all eight sides, including diagonals, and ignores the cell itself.</summary>
        [Test]
        public void HasLivingNeighbour_SeesEitherColourOnAllEightSides()
        {
            var grid = CreateGrid(
                "O....",
                ".....",
                "..P..",
                ".....",
                ".....");

            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(1, 1)), Is.True);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(3, 3)), Is.True);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(0, 1)), Is.True);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(2, 2)), Is.False);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(4, 4)), Is.False);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(0, 4)), Is.False);
        }

        private static CellGrid CreateEmptyGrid(int rows, int columns)
        {
            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            return grid;
        }

        /// <summary>Builds a grid from rows of text where '.' is empty, 'P' is Player and 'O' is Opponent, with no recorded changes.</summary>
        private static CellGrid CreateGrid(params string[] rows)
        {
            var grid = CreateEmptyGrid(rows.Length, rows[0].Length);
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), ParseOwner(rows[row][column]));
                }
            }

            grid.ClearChangedCells();
            return grid;
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

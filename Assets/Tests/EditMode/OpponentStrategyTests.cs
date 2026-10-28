using System;
using System.Collections.Generic;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that every opponent strategy only chooses legal cells, and that Hard scores match moves by local survival and a five-generation lookahead.</summary>
    [TestFixture]
    public sealed class OpponentStrategyTests
    {
        private const char EmptySymbol = '.';
        private const char PlayerSymbol = 'P';
        private const char OpponentSymbol = 'O';
        private const int SeedsTried = 25;
        private const int LookaheadGenerations = 5;

        /// <summary>Over many seeds, each strategy only picks empty cells inside the region, even when living cells sit just outside it.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_OnlyPicksEmptyCellsInsideTheRegion(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "P.P.P.",
                ".PPP..",
                "..P..P",
                "O.O.OO",
                ".OO...",
                "O...OO");
            var region = new PlacementRegion(3, 6);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(difficulty, grid, seed);

                var chosen = strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex);

                Assert.That(chosen, Is.True);
                Assert.That(region.Contains(grid, cellIndex), Is.True, $"Seed {seed} chose cell {cellIndex} outside the region.");
                Assert.That(grid.IsAlive(cellIndex), Is.False, $"Seed {seed} chose occupied cell {cellIndex}.");
            }
        }

        /// <summary>In setup, Hard only picks empty cells inside its own rows, even when the only life on the board is outside them.</summary>
        [Test]
        public void Hard_WithLifeOnlyOutsideTheRegion_StillPicksInsideTheRegion()
        {
            var grid = CreateGrid(
                ".PP...",
                ".PP...",
                "......",
                "......",
                "......",
                "......");
            var region = new PlacementRegion(4, 6);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(region.Contains(grid, cellIndex), Is.True, $"Seed {seed} chose cell {cellIndex} outside the region.");
                Assert.That(grid.IsAlive(cellIndex), Is.False);
            }
        }

        /// <summary>Each strategy returns false when every cell in the region is occupied, even if the rest of the grid is empty.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_ReturnsFalse_WhenTheRegionIsFull(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "....",
                "....",
                "PPOO",
                "OOPP");
            var region = new PlacementRegion(2, 4);
            var strategy = CreateStrategy(difficulty, grid, 0);

            var chosen = strategy.TryChooseCell(grid, region, CellOwner.Opponent, out _);

            Assert.That(chosen, Is.False);
            Assert.That(region.HasPlaceableCell(grid), Is.False);
        }

        /// <summary>Each strategy returns false for a region with no rows.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_ReturnsFalse_ForARegionWithNoRows(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "...",
                "...");
            var strategy = CreateStrategy(difficulty, grid, 0);

            Assert.That(strategy.TryChooseCell(grid, new PlacementRegion(1, 1), CellOwner.Opponent, out _), Is.False);
        }

        /// <summary>When the region has a single empty cell, each strategy picks exactly that cell.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_PicksTheOnlyEmptyCellInTheRegion(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "....",
                "....",
                "OOPO",
                "OP.P");
            var strategy = CreateStrategy(difficulty, grid, 0);

            Assert.That(strategy.TryChooseCell(grid, new PlacementRegion(2, 4), CellOwner.Opponent, out var cellIndex), Is.True);
            Assert.That(cellIndex, Is.EqualTo(grid.ToCellIndex(3, 2)));
        }

        /// <summary>Choosing a cell leaves the grid's cells and its pending change list exactly as they were.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_NeverChangesTheGrid(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "......",
                ".PPP..",
                "......",
                "..OO..",
                "..OO..",
                "......");
            var pendingChangeIndex = grid.ToCellIndex(1, 1);
            grid.SetOwner(pendingChangeIndex, CellOwner.None);
            grid.SetOwner(pendingChangeIndex, CellOwner.Player);
            var cellsBefore = DescribeGrid(grid);
            var strategy = CreateStrategy(difficulty, grid, 0);

            strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out _);

            Assert.That(DescribeGrid(grid), Is.EqualTo(cellsBefore));
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { pendingChangeIndex }));
        }

        /// <summary>Over enough tries Easy picks every empty cell in the region, so no empty cell is ever excluded.</summary>
        [Test]
        public void Easy_EventuallyPicksEveryEmptyCellInTheRegion()
        {
            var grid = CreateGrid(
                "P.P",
                ".O.",
                "O.P");
            var strategy = new RandomOpponentStrategy(new Random(7), grid.MaximumCellCount);
            var expectedCells = new HashSet<int>();
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                if (!grid.IsAlive(cellIndex))
                {
                    expectedCells.Add(cellIndex);
                }
            }

            var pickedCells = new HashSet<int>();
            for (var attempt = 0; attempt < 200; attempt++)
            {
                strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex);
                pickedCells.Add(cellIndex);
            }

            Assert.That(pickedCells, Is.EquivalentTo(expectedCells));
        }

        /// <summary>Hard fills the gap between its block and an equal Player block, so its joined cluster absorbs every Player cell.</summary>
        [Test]
        public void Hard_PicksTheAbsorbingMove_WhenOneIsClearlyAvailable()
        {
            var grid = CreateGrid(
                "........",
                "........",
                ".OO.PP..",
                ".OO.PP..",
                "........",
                "........");

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);
                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);

                var outcome = PlaceAndResolve(grid, cellIndex, CellOwner.Opponent);
                Assert.That(grid.ColumnOf(cellIndex), Is.EqualTo(3), $"Seed {seed} chose cell {cellIndex}, which is not in the gap between the blocks.");
                Assert.That(outcome.CountCells(CellOwner.Player), Is.Zero, $"Seed {seed} chose cell {cellIndex}, which absorbs nothing.");
                Assert.That(outcome.CountCells(CellOwner.Opponent), Is.EqualTo(9));
            }
        }

        /// <summary>Hard plays for whichever side it is given, so as the Player it absorbs the Opponent instead.</summary>
        [Test]
        public void Hard_PlayingAsPlayer_AbsorbsTheOpponent()
        {
            var grid = CreateGrid(
                "........",
                ".PP.OO..",
                ".PP.OO..",
                "........");
            var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, 3);

            Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Player, out var cellIndex), Is.True);

            Assert.That(grid.ColumnOf(cellIndex), Is.EqualTo(3));
        }

        /// <summary>In setup on an empty board, Hard still picks a cell in its rows.</summary>
        [Test]
        public void Hard_OnAnEmptyBoard_PicksAnEmptyCellInTheRegion()
        {
            var grid = CreateGrid(
                ".....",
                ".....",
                ".....",
                ".....");
            var region = new PlacementRegion(2, 4);
            var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, 0);

            Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
            Assert.That(region.Contains(grid, cellIndex), Is.True);
        }

        /// <summary>In a setup half dotted with lone cells of its own, Hard grows a block from one of them, so its pick is legal and next to life.</summary>
        [Test]
        public void Hard_InSetupAmongLoneCells_BuildsNextToOneOfThem()
        {
            const int size = 40;
            const int latticeSpacing = 4;
            var grid = CreateLatticeGrid(size, size, latticeSpacing, size);
            var region = new PlacementRegion(size / 2, size);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(region.Contains(grid, cellIndex), Is.True);
                Assert.That(grid.IsAlive(cellIndex), Is.False);
                Assert.That(HasLivingNeighbour(grid, cellIndex), Is.True, $"Seed {seed} chose cell {cellIndex}, which is not next to life.");
            }
        }

        /// <summary>Hard scores every candidate locally before shortlisting, so the only absorbing move, listed after hundreds of others, is found for every seed.</summary>
        [Test]
        public void Hard_ShortlistsTheAbsorbingMove_AmongHundredsOfCandidates()
        {
            const int size = 24;
            const int latticeSpacing = 3;
            const int latticeEndRow = 16;
            const int blockTopRow = 20;
            const int gapColumn = 11;
            var grid = CreateLatticeGrid(size, size, latticeSpacing, latticeEndRow);
            SetBlock(grid, blockTopRow, gapColumn - 2, CellOwner.Opponent);
            SetBlock(grid, blockTopRow, gapColumn + 1, CellOwner.Player);
            grid.ClearChangedCells();

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(grid.ColumnOf(cellIndex), Is.EqualTo(gapColumn), $"Seed {seed} chose cell {cellIndex}, which is not in the gap between the blocks.");
                Assert.That(grid.RowOf(cellIndex), Is.GreaterThanOrEqualTo(blockTopRow - 1), $"Seed {seed} chose cell {cellIndex}, which does not touch both blocks.");
            }
        }

        /// <summary>Every move adds one cell now, so Hard picks one of the four that turn its pair into an L, the only shape that becomes a block next generation.</summary>
        [Test]
        public void Hard_PrefersTheMoveThatGrowsNextGeneration()
        {
            var grid = CreateGrid(
                "......",
                "......",
                "..OO..",
                "......",
                "......",
                "......");
            var blockMakingCells = new[] { grid.ToCellIndex(1, 2), grid.ToCellIndex(1, 3), grid.ToCellIndex(3, 2), grid.ToCellIndex(3, 3) };

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(blockMakingCells, Does.Contain(cellIndex), $"Seed {seed} chose cell {cellIndex}, which does not make a block next generation.");
            }
        }

        /// <summary>Hard turns down a one-cell absorption for the row of three that leaves it more cells five generations later; both moves wipe out the Player.</summary>
        [Test]
        public void Hard_PrefersMoreCellsFiveGenerationsLater_OverAnAbsorptionNow()
        {
            var grid = CreateGrid(
                ".......",
                "..P....",
                ".......",
                ".P..OO.",
                ".......",
                ".......",
                ".......");
            var lastingCell = grid.ToCellIndex(3, 3);
            var absorbingCell = grid.ToCellIndex(2, 3);

            Assert.That(CountOpponentCellsAfter(grid, lastingCell, LookaheadGenerations), Is.GreaterThan(CountOpponentCellsAfter(grid, absorbingCell, LookaheadGenerations)));
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(cellIndex, Is.EqualTo(lastingCell), $"Seed {seed} chose cell {cellIndex}, which leaves fewer cells five generations later.");
            }
        }

        /// <summary>Of every move beside its lone cell, only (3,0) leaves Hard a lasting block five generations later, so it is chosen over the burst at (3,1) that dies out.</summary>
        [Test]
        public void Hard_PrefersTheMoveStillAliveFiveGenerationsLater()
        {
            var grid = CreateGrid(
                ".......",
                "..P....",
                ".P.....",
                ".......",
                ".O.....",
                ".......",
                ".......");
            var blockCell = grid.ToCellIndex(3, 0);

            Assert.That(CountOpponentCellsAfter(grid, blockCell, LookaheadGenerations), Is.EqualTo(4));
            Assert.That(CountOpponentCellsAfter(grid, grid.ToCellIndex(3, 1), LookaheadGenerations), Is.Zero);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(cellIndex, Is.EqualTo(blockCell), $"Seed {seed} chose cell {cellIndex}, which does not last five generations.");
            }
        }

        /// <summary>When the Player lines its border row, every region cell next to life would be absorbed, so Hard places away from it and keeps its cell.</summary>
        [Test]
        public void Hard_WhenEveryCellNextToLifeWouldBeAbsorbed_PlacesWhereItsCellIsKept()
        {
            var grid = CreateGrid(
                "......",
                "......",
                "......",
                "PPPP..",
                "......",
                "......",
                "......",
                "......");
            var region = new PlacementRegion(4, 8);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(region.Contains(grid, cellIndex), Is.True);
                Assert.That(PlaceAndResolve(grid, cellIndex, CellOwner.Opponent).CountCells(CellOwner.Opponent), Is.EqualTo(1), $"Seed {seed} chose cell {cellIndex}, which the Player absorbs.");
            }
        }

        /// <summary>On the whole grid, as in the match, a lone cell dies next generation, so Hard keeps to cells next to life even when the Player block would absorb each one.</summary>
        [Test]
        public void Hard_OnTheWholeGrid_KeepsToCellsNextToLife_EvenWhenEachWouldBeAbsorbed()
        {
            var grid = CreateGrid(
                "........",
                "........",
                "........",
                "...PP...",
                "...PP...",
                "........",
                "........",
                "........");

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(OpponentDifficulty.Hard, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(HasLivingNeighbour(grid, cellIndex), Is.True, $"Seed {seed} chose cell {cellIndex}, which is not next to life.");
            }
        }

        /// <summary>The same seed and board always produce the same Medium or Hard move.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_IsDeterministicForASeed(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "......",
                ".PP...",
                "...P..",
                "..OO..",
                ".O....",
                "......");

            CreateStrategy(difficulty, grid, 11).TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var firstChoice);
            CreateStrategy(difficulty, grid, 11).TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var secondChoice);

            Assert.That(secondChoice, Is.EqualTo(firstChoice));
        }

        /// <summary>Medium and Hard need a real side to play for.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_PlayingForNoOne_Throws(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid("...");
            var strategy = CreateStrategy(difficulty, grid, 0);

            Assert.Throws<ArgumentException>(() => strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.None, out _));
        }

        /// <summary>A strategy cannot choose on a grid larger than the size it preallocated for.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_GridLargerThanMaximum_Throws(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "...",
                "...");
            var strategy = OpponentStrategyFactory.Create(difficulty, new Random(0), 1, 2);

            Assert.Throws<ArgumentException>(() => strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out _));
        }

        /// <summary>A grid with no more cells than the maximum fits whatever its shape, even one row longer than the maximum's.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_OnAGridReshapedWithinTheMaximumCells_StillChooses(OpponentDifficulty difficulty)
        {
            var grid = new CellGrid(4, 4);
            grid.Resize(1, 16);
            grid.SetOwner(grid.ToCellIndex(0, 5), CellOwner.Player);
            var strategy = OpponentStrategyFactory.Create(difficulty, new Random(0), 4, 4);

            Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid), CellOwner.Opponent, out var cellIndex), Is.True);
            Assert.That(grid.IsAlive(cellIndex), Is.False);
        }

        /// <summary>In the match on a board with no life, where the side may place anywhere, Medium and Hard fall back to any empty cell of the grid.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_InTheMatchWithNoLife_FallsBackToAnyEmptyCell(OpponentDifficulty difficulty)
        {
            var grid = CreateGrid(
                "....",
                "....",
                "....",
                "....");
            var region = PlacementRegion.WholeGrid(grid);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = CreateStrategy(difficulty, grid, seed);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Player, out var cellIndex), Is.True);
                Assert.That(region.CanPlaceOn(grid, cellIndex), Is.True, $"Seed {seed} chose cell {cellIndex}, which the region does not allow.");
            }
        }

        /// <summary>The factory builds the random strategy for Easy and the survival strategy for Medium and Hard.</summary>
        [TestCase(OpponentDifficulty.Easy, typeof(RandomOpponentStrategy))]
        [TestCase(OpponentDifficulty.Medium, typeof(SurvivalOpponentStrategy))]
        [TestCase(OpponentDifficulty.Hard, typeof(SurvivalOpponentStrategy))]
        public void Factory_CreatesTheStrategyForTheDifficulty(OpponentDifficulty difficulty, Type expectedType)
        {
            var strategy = OpponentStrategyFactory.Create(difficulty, new Random(0), 4, 4);

            Assert.That(strategy, Is.TypeOf(expectedType));
        }

        /// <summary>The factory rejects a difficulty it does not know.</summary>
        [Test]
        public void Factory_UnknownDifficulty_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OpponentStrategyFactory.Create((OpponentDifficulty)99, new Random(0), 4, 4));
        }

        /// <summary>A region contains exactly the cells in its rows that are inside the grid's active area.</summary>
        [Test]
        public void Region_ContainsOnlyCellsInItsRowsInsideTheGrid()
        {
            var grid = CreateGrid(
                "...",
                "...",
                "...",
                "...");
            var region = new PlacementRegion(2, 10);

            Assert.That(region.Contains(grid, grid.ToCellIndex(1, 2)), Is.False);
            Assert.That(region.Contains(grid, grid.ToCellIndex(2, 0)), Is.True);
            Assert.That(region.Contains(grid, grid.ToCellIndex(3, 2)), Is.True);
            Assert.That(region.Contains(grid, grid.CellCount), Is.False);
            Assert.That(region.Contains(grid, -1), Is.False);
        }

        /// <summary>The whole-grid region spans every row of the grid.</summary>
        [Test]
        public void WholeGridRegion_SpansEveryRow()
        {
            var grid = CreateGrid(
                "..",
                "..",
                "..");

            var region = PlacementRegion.WholeGrid(grid);

            Assert.That(region.FirstRow, Is.Zero);
            Assert.That(region.EndRowExclusive, Is.EqualTo(3));
            Assert.That(region.Contains(grid, 0), Is.True);
            Assert.That(region.Contains(grid, grid.CellCount - 1), Is.True);
        }

        /// <summary>A region reports an empty cell only when one of its own rows has one.</summary>
        [Test]
        public void Region_HasPlaceableCell_LooksOnlyAtItsOwnRows()
        {
            var grid = CreateGrid(
                "...",
                "PPP",
                "OO.");

            Assert.That(new PlacementRegion(1, 2).HasPlaceableCell(grid), Is.False);
            Assert.That(new PlacementRegion(1, 3).HasPlaceableCell(grid), Is.True);
            Assert.That(new PlacementRegion(0, 1).HasPlaceableCell(grid), Is.True);
        }

        /// <summary>A region starting far past the grid holds no cells, even where its first row times the column count overflows an int.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Region_StartingFarPastTheGrid_HoldsNoCells(OpponentDifficulty difficulty)
        {
            const int rows = 60;
            const int columns = 30;
            const int firstRowThatWrapsToCellFourteen = 143165577;
            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            var region = new PlacementRegion(firstRowThatWrapsToCellFourteen, firstRowThatWrapsToCellFourteen + 1);
            var strategy = CreateStrategy(difficulty, grid, 0);

            Assert.That(region.Contains(grid, 14), Is.False);
            Assert.That(region.Contains(grid, grid.CellCount - 1), Is.False);
            Assert.That(region.HasPlaceableCell(grid), Is.False);
            Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out _), Is.False);
        }

        /// <summary>A region cannot start at a negative row or end before it starts.</summary>
        [Test]
        public void Region_WithInvalidRows_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementRegion(-1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementRegion(3, 2));
        }

        /// <summary>When the region requires a living neighbour, every strategy only ever picks an empty cell touching a living cell.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Choice_InRegionRequiringLivingNeighbour_AlwaysTouchesALivingCell(OpponentDifficulty difficulty)
        {
            for (var seed = 0; seed < 60; seed++)
            {
                var boardRandom = new Random(seed);
                var grid = new CellGrid(12, 10);
                grid.Resize(12, 10);
                for (var placement = 0; placement < 1 + boardRandom.Next(6); placement++)
                {
                    grid.SetOwner(boardRandom.Next(grid.CellCount), boardRandom.Next(2) == 0 ? CellOwner.Player : CellOwner.Opponent);
                }

                var strategy = CreateStrategy(difficulty, grid, seed);
                var region = PlacementRegion.WholeGrid(grid, requiresLivingNeighbour: true);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(grid.IsAlive(cellIndex), Is.False);
                Assert.That(grid.HasLivingNeighbour(cellIndex), Is.True, $"seed {seed} picked an isolated cell");
            }
        }

        /// <summary>When the region requires a living neighbour and the board is empty, every strategy declines to choose.</summary>
        [TestCase(OpponentDifficulty.Easy)]
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Choice_InRegionRequiringLivingNeighbour_OnEmptyBoard_ReturnsFalse(OpponentDifficulty difficulty)
        {
            var grid = new CellGrid(6, 6);
            grid.Resize(6, 6);
            var strategy = CreateStrategy(difficulty, grid, 0);

            Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid, requiresLivingNeighbour: true), CellOwner.Opponent, out _), Is.False);
        }

        /// <summary>CanPlaceOn accepts only empty cells in the region's rows, and only next to life when the region requires it.</summary>
        [Test]
        public void Region_CanPlaceOn_RespectsRowsEmptinessAndLivingNeighbourRule()
        {
            var grid = new CellGrid(4, 4);
            grid.Resize(4, 4);
            grid.SetOwner(grid.ToCellIndex(1, 1), CellOwner.Player);
            var anywhere = new PlacementRegion(0, 4);
            var nextToLife = new PlacementRegion(0, 4, requiresLivingNeighbour: true);
            var bottomRows = new PlacementRegion(2, 4, requiresLivingNeighbour: true);

            Assert.That(anywhere.CanPlaceOn(grid, grid.ToCellIndex(3, 3)), Is.True);
            Assert.That(anywhere.CanPlaceOn(grid, grid.ToCellIndex(1, 1)), Is.False);
            Assert.That(nextToLife.CanPlaceOn(grid, grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(nextToLife.CanPlaceOn(grid, grid.ToCellIndex(2, 2)), Is.True);
            Assert.That(bottomRows.CanPlaceOn(grid, grid.ToCellIndex(0, 0)), Is.False);
            Assert.That(bottomRows.CanPlaceOn(grid, grid.ToCellIndex(2, 0)), Is.True);
        }

        /// <summary>Creates a strategy of the given difficulty sized for the grid with a seeded random source.</summary>
        private static IOpponentStrategy CreateStrategy(OpponentDifficulty difficulty, CellGrid grid, int seed)
        {
            return OpponentStrategyFactory.Create(difficulty, new Random(seed), grid.Rows, grid.Columns);
        }

        /// <summary>Returns how many Opponent cells remain after placing an Opponent square on the cell and running the given generations with absorption, on a copy of the grid.</summary>
        private static int CountOpponentCellsAfter(CellGrid grid, int cellIndex, int generations)
        {
            var outcome = PlaceAndResolve(grid, cellIndex, CellOwner.Opponent);
            var absorber = new ClusterAbsorber(outcome.MaximumCellCount);
            for (var generation = 0; generation < generations; generation++)
            {
                outcome.AdvanceGeneration();
                absorber.ResolveAbsorptions(outcome);
            }

            return outcome.CountCells(CellOwner.Opponent);
        }

        /// <summary>Returns a copy of the grid with the cell placed for the side and absorptions resolved, leaving the original untouched.</summary>
        private static CellGrid PlaceAndResolve(CellGrid grid, int cellIndex, CellOwner side)
        {
            var outcome = new CellGrid(grid.Rows, grid.Columns);
            outcome.CopyFrom(grid);
            outcome.SetOwner(cellIndex, side);
            new ClusterAbsorber(outcome.MaximumCellCount).ResolveAbsorptions(outcome);
            return outcome;
        }

        /// <summary>Builds a grid with lone cells of alternating owners every spacing rows and columns above the end row, so no two touch.</summary>
        private static CellGrid CreateLatticeGrid(int rows, int columns, int spacing, int endRowExclusive)
        {
            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            for (var row = 0; row < endRowExclusive; row += spacing)
            {
                for (var column = 0; column < columns; column += spacing)
                {
                    var isPlayerCell = (row / spacing + column / spacing) % 2 == 0;
                    grid.SetOwner(grid.ToCellIndex(row, column), isPlayerCell ? CellOwner.Player : CellOwner.Opponent);
                }
            }

            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Fills the 2x2 block whose top-left cell is at the given row and column with the owner.</summary>
        private static void SetBlock(CellGrid grid, int topRow, int leftColumn, CellOwner owner)
        {
            for (var row = topRow; row < topRow + 2; row++)
            {
                for (var column = leftColumn; column < leftColumn + 2; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), owner);
                }
            }
        }

        /// <summary>Returns whether any of the cell's 8-way neighbours is alive.</summary>
        private static bool HasLivingNeighbour(CellGrid grid, int cellIndex)
        {
            for (var neighbourSlot = 0; neighbourSlot < grid.GetNeighbourCount(cellIndex); neighbourSlot++)
            {
                if (grid.IsAlive(grid.GetNeighbourIndex(cellIndex, neighbourSlot)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Builds a grid from rows of text where '.' is empty, 'P' is Player and 'O' is Opponent, with no recorded changes.</summary>
        private static CellGrid CreateGrid(params string[] rows)
        {
            var grid = new CellGrid(rows.Length, rows[0].Length);
            grid.Resize(rows.Length, rows[0].Length);
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

using System;
using System.Collections.Generic;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace GameOfLife.Tests
{
    /// <summary>Checks the survival tactics Medium and Hard use: finishing blocks, leaving breathing room, supporting new squares, and choosing without allocating.</summary>
    [TestFixture]
    public sealed class SurvivalOpponentStrategyTests
    {
        private const int SeedsTried = 25;
        private const int SetupRuns = 5;
        private const int SetupSquares = 20;
        private const int SquaresBeyondTheBlockSites = 40;

        /// <summary>Given an L of its own during setup, Medium and Hard place the square that finishes the block.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_InSetup_FinishesAStartedBlock(OpponentDifficulty difficulty)
        {
            var grid = OpponentTestBoards.Create(
                "........",
                "........",
                "........",
                "........",
                "...OO...",
                "...O....",
                "........",
                "........",
                "........",
                "........");
            var region = new PlacementRegion(3, 10);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = OpponentStrategyFactory.Create(difficulty, new Random(seed), grid.Rows, grid.Columns);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(cellIndex, Is.EqualTo(grid.ToCellIndex(5, 4)), $"Seed {seed} chose cell {cellIndex}, which does not finish the block.");
            }
        }

        /// <summary>Placing the usual setup squares alone on an empty half, which leaves room to spare, Hard builds only separate blocks with two empty cells between them, so none crowds or feeds another.</summary>
        [Test]
        public void Hard_InSetup_BuildsSeparateBlocksTwoCellsApart()
        {
            for (var seed = 0; seed < SetupRuns; seed++)
            {
                var grid = PlaceSetupSquaresAlone(OpponentDifficulty.Hard, seed, SetupSquares);

                Assert.That(AreAllClustersBlocks(grid), Is.True, $"Seed {seed} left a cluster that is not a 2x2 block.");
                Assert.That(FindSmallestGapBetweenClusters(grid), Is.GreaterThanOrEqualTo(3), $"Seed {seed} put two clusters closer than two empty cells.");
            }
        }

        /// <summary>Medium keeps an empty cell around its blocks during setup, less room than Hard, so every cluster it builds fits inside one 2x2 block, finished or not.</summary>
        [Test]
        public void Medium_InSetup_KeepsEveryClusterWithinOneBlock()
        {
            for (var seed = 0; seed < SetupRuns; seed++)
            {
                var grid = PlaceSetupSquaresAlone(OpponentDifficulty.Medium, seed, SetupSquares);

                Assert.That(AreAllClustersWithinABlock(grid), Is.True, $"Seed {seed} let a cluster grow past a 2x2 block, so two blocks touch.");
            }
        }

        /// <summary>With more setup squares than block sites in the half, Medium and Hard still never put a square beside a finished block.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_WithMoreSquaresThanBlockSites_NeverCrowdsAFinishedBlock(OpponentDifficulty difficulty)
        {
            for (var seed = 0; seed < SetupRuns; seed++)
            {
                var grid = PlaceSetupSquaresAlone(difficulty, seed, SquaresBeyondTheBlockSites);

                Assert.That(AreFinishedBlocksClearOfOtherLife(grid), Is.True, $"Seed {seed} put a square beside a finished block.");
            }
        }

        /// <summary>When no block fits with Hard's two cells of room, Medium and Hard still finish a block that has one cell of room.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_InACrowdedSetup_FinishesABlockWithOneCellOfRoom(OpponentDifficulty difficulty)
        {
            var grid = OpponentTestBoards.Create(
                ".....",
                "OO.OO",
                "OO.O.");
            var region = new PlacementRegion(1, 3);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = OpponentStrategyFactory.Create(difficulty, new Random(seed), grid.Rows, grid.Columns);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(cellIndex, Is.EqualTo(grid.ToCellIndex(2, 4)), $"Seed {seed} chose cell {cellIndex}, which does not finish the block.");
            }
        }

        /// <summary>When no block can be built at all, Medium and Hard put the square where it touches no life rather than beside their block.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_InSetupWithNoBlockSiteLeft_KeepsTheSquareOffLife(OpponentDifficulty difficulty)
        {
            var grid = OpponentTestBoards.Create(
                "....",
                "OO..",
                "OO..");
            var region = new PlacementRegion(1, 3);

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = OpponentStrategyFactory.Create(difficulty, new Random(seed), grid.Rows, grid.Columns);

                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                Assert.That(grid.HasLivingNeighbour(cellIndex), Is.False, $"Seed {seed} chose cell {cellIndex}, beside the block.");
            }
        }

        /// <summary>Next to a finished block in setup, Hard starts its next block outside the block's breathing room rather than crowding it.</summary>
        [Test]
        public void Hard_InSetup_DoesNotStartABlockBesideAFinishedOne()
        {
            var grid = OpponentTestBoards.CreateEmpty(12, 12);
            for (var row = 7; row < 9; row++)
            {
                for (var column = 5; column < 7; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), CellOwner.Opponent);
                }
            }

            grid.ClearChangedCells();
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = OpponentStrategyFactory.Create(OpponentDifficulty.Hard, new Random(seed), grid.Rows, grid.Columns);

                Assert.That(strategy.TryChooseCell(grid, new PlacementRegion(6, 12), CellOwner.Opponent, out var cellIndex), Is.True);
                var distance = Math.Max(DistanceOutside(grid.RowOf(cellIndex), 7, 8), DistanceOutside(grid.ColumnOf(cellIndex), 5, 6));
                Assert.That(distance, Is.GreaterThanOrEqualTo(3), $"Seed {seed} chose cell {cellIndex}, inside the block's breathing room.");
            }
        }

        /// <summary>In the match, where squares must touch life, Medium and Hard pick a cell with two or three living neighbours so the square survives the next generation.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_InTheMatch_PicksACellThatSurvivesTheNextGeneration(OpponentDifficulty difficulty)
        {
            var grid = OpponentTestBoards.Create(
                "..........",
                ".O........",
                "..........",
                "..........",
                "......OO..",
                "..........",
                "..........");

            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var strategy = OpponentStrategyFactory.Create(difficulty, new Random(seed), grid.Rows, grid.Columns);

                Assert.That(strategy.TryChooseCell(grid, PlacementRegion.WholeGrid(grid, requiresLivingNeighbour: true), CellOwner.Opponent, out var cellIndex), Is.True);
                var livingNeighbours = CountLivingNeighbours(grid, cellIndex);
                Assert.That(livingNeighbours, Is.InRange(2, 3), $"Seed {seed} chose cell {cellIndex}, with {livingNeighbours} living neighbours.");
            }
        }

        /// <summary>After a first call, choosing never allocates, in setup or in the match, at the largest board size.</summary>
        [TestCase(OpponentDifficulty.Medium)]
        [TestCase(OpponentDifficulty.Hard)]
        public void Strategy_Choosing_DoesNotAllocate(OpponentDifficulty difficulty)
        {
            const int rows = 60;
            const int columns = 30;
            var grid = OpponentTestBoards.CreateEmpty(rows, columns);
            var random = new Random(3);
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                var roll = random.NextDouble();
                if (roll < 0.1)
                {
                    grid.SetOwner(cellIndex, CellOwner.Player);
                }
                else if (roll < 0.2)
                {
                    grid.SetOwner(cellIndex, CellOwner.Opponent);
                }
            }

            var strategy = OpponentStrategyFactory.Create(difficulty, new Random(5), rows, columns);
            var setupRegion = new PlacementRegion(rows / 2, rows);
            var matchRegion = PlacementRegion.WholeGrid(grid, requiresLivingNeighbour: true);
            strategy.TryChooseCell(grid, setupRegion, CellOwner.Opponent, out _);
            strategy.TryChooseCell(grid, matchRegion, CellOwner.Opponent, out _);

            Assert.That(() =>
            {
                strategy.TryChooseCell(grid, setupRegion, CellOwner.Opponent, out _);
                strategy.TryChooseCell(grid, matchRegion, CellOwner.Opponent, out _);
            }, Is.Not.AllocatingGCMemory());
        }

        /// <summary>A skill rejects negative values and a lookahead with no generations.</summary>
        [Test]
        public void Skill_WithInvalidValues_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OpponentSkill(-1, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OpponentSkill(0, -0.5, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OpponentSkill(0, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OpponentSkill(0, 0, 4, 0));
            Assert.DoesNotThrow(() => new OpponentSkill(0, 0, 0, 0));
        }

        /// <summary>Plays the given setup squares for the Opponent alone on an empty 24x12 board's bottom half and returns the board.</summary>
        private static CellGrid PlaceSetupSquaresAlone(OpponentDifficulty difficulty, int seed, int squares)
        {
            var grid = OpponentTestBoards.CreateEmpty(24, 12);
            var region = new PlacementRegion(12, 24);
            var strategy = OpponentStrategyFactory.Create(difficulty, new Random(seed), grid.Rows, grid.Columns);
            for (var square = 0; square < squares; square++)
            {
                Assert.That(strategy.TryChooseCell(grid, region, CellOwner.Opponent, out var cellIndex), Is.True);
                grid.SetOwner(cellIndex, CellOwner.Opponent);
            }

            return grid;
        }

        /// <summary>Returns whether every 8-way cluster of living cells is a complete 2x2 block.</summary>
        private static bool AreAllClustersBlocks(CellGrid grid)
        {
            var clusterOfCell = LabelClusters(grid, out var clusterCount);
            for (var cluster = 0; cluster < clusterCount; cluster++)
            {
                int size = 0, top = int.MaxValue, bottom = -1, left = int.MaxValue, right = -1;
                for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
                {
                    if (clusterOfCell[cellIndex] != cluster)
                    {
                        continue;
                    }

                    size++;
                    top = Math.Min(top, grid.RowOf(cellIndex));
                    bottom = Math.Max(bottom, grid.RowOf(cellIndex));
                    left = Math.Min(left, grid.ColumnOf(cellIndex));
                    right = Math.Max(right, grid.ColumnOf(cellIndex));
                }

                if (size != 4 || bottom - top != 1 || right - left != 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns whether every 8-way cluster of living cells fits inside a 2x2 square.</summary>
        private static bool AreAllClustersWithinABlock(CellGrid grid)
        {
            var clusterOfCell = LabelClusters(grid, out var clusterCount);
            for (var cluster = 0; cluster < clusterCount; cluster++)
            {
                int top = int.MaxValue, bottom = -1, left = int.MaxValue, right = -1;
                for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
                {
                    if (clusterOfCell[cellIndex] != cluster)
                    {
                        continue;
                    }

                    top = Math.Min(top, grid.RowOf(cellIndex));
                    bottom = Math.Max(bottom, grid.RowOf(cellIndex));
                    left = Math.Min(left, grid.ColumnOf(cellIndex));
                    right = Math.Max(right, grid.ColumnOf(cellIndex));
                }

                if (bottom - top > 1 || right - left > 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns whether every finished Opponent 2x2 block has no other living cell touching it.</summary>
        private static bool AreFinishedBlocksClearOfOtherLife(CellGrid grid)
        {
            for (var top = 0; top + 1 < grid.Rows; top++)
            {
                for (var left = 0; left + 1 < grid.Columns; left++)
                {
                    if (CountCells(grid, top, left, top + 1, left + 1, CellOwner.Opponent) == 4
                        && CountCells(grid, top - 1, left - 1, top + 2, left + 2, CellOwner.Opponent) + CountCells(grid, top - 1, left - 1, top + 2, left + 2, CellOwner.Player) != 4)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Counts the side's cells in the inclusive rectangle, clipped to the board.</summary>
        private static int CountCells(CellGrid grid, int top, int left, int bottom, int right, CellOwner side)
        {
            var count = 0;
            for (var row = Math.Max(0, top); row <= Math.Min(grid.Rows - 1, bottom); row++)
            {
                for (var column = Math.Max(0, left); column <= Math.Min(grid.Columns - 1, right); column++)
                {
                    count += grid.GetOwner(grid.ToCellIndex(row, column)) == side ? 1 : 0;
                }
            }

            return count;
        }

        /// <summary>Returns the smallest row-or-column distance between living cells of different clusters, or int.MaxValue with fewer than two clusters.</summary>
        private static int FindSmallestGapBetweenClusters(CellGrid grid)
        {
            var clusterOfCell = LabelClusters(grid, out _);
            var smallest = int.MaxValue;
            for (var first = 0; first < grid.CellCount; first++)
            {
                for (var second = first + 1; second < grid.CellCount; second++)
                {
                    if (clusterOfCell[first] < 0 || clusterOfCell[second] < 0 || clusterOfCell[first] == clusterOfCell[second])
                    {
                        continue;
                    }

                    var gap = Math.Max(Math.Abs(grid.RowOf(first) - grid.RowOf(second)), Math.Abs(grid.ColumnOf(first) - grid.ColumnOf(second)));
                    smallest = Math.Min(smallest, gap);
                }
            }

            return smallest;
        }

        /// <summary>Labels every living cell with its 8-way cluster, regardless of colour; empty cells get -1.</summary>
        private static int[] LabelClusters(CellGrid grid, out int clusterCount)
        {
            var clusterOfCell = new int[grid.CellCount];
            for (var cellIndex = 0; cellIndex < clusterOfCell.Length; cellIndex++)
            {
                clusterOfCell[cellIndex] = -1;
            }

            clusterCount = 0;
            var stack = new Stack<int>();
            for (var start = 0; start < grid.CellCount; start++)
            {
                if (!grid.IsAlive(start) || clusterOfCell[start] >= 0)
                {
                    continue;
                }

                clusterOfCell[start] = clusterCount;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var cellIndex = stack.Pop();
                    for (var slot = 0; slot < grid.GetNeighbourCount(cellIndex); slot++)
                    {
                        var neighbourIndex = grid.GetNeighbourIndex(cellIndex, slot);
                        if (grid.IsAlive(neighbourIndex) && clusterOfCell[neighbourIndex] < 0)
                        {
                            clusterOfCell[neighbourIndex] = clusterCount;
                            stack.Push(neighbourIndex);
                        }
                    }
                }

                clusterCount++;
            }

            return clusterOfCell;
        }

        /// <summary>Returns how far a coordinate lies outside an inclusive range, or 0 inside it.</summary>
        private static int DistanceOutside(int value, int first, int last)
        {
            return value < first ? first - value : value > last ? value - last : 0;
        }

        /// <summary>Counts a cell's living neighbours of either colour.</summary>
        private static int CountLivingNeighbours(CellGrid grid, int cellIndex)
        {
            var count = 0;
            for (var slot = 0; slot < grid.GetNeighbourCount(cellIndex); slot++)
            {
                count += grid.IsAlive(grid.GetNeighbourIndex(cellIndex, slot)) ? 1 : 0;
            }

            return count;
        }
    }
}

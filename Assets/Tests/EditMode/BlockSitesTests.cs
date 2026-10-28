using GameOfLife.Opponents;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks block progress scores and the breathing room a block needs.</summary>
    [TestFixture]
    public sealed class BlockSitesTests
    {
        private const double Tolerance = 1e-9;

        /// <summary>Progress rises with each block cell: a seed, a pair, an L, then the finishing cell with its bonus.</summary>
        [TestCase(new string[] { "......", "......", "......", "......", "......", "......" }, 2, 2, 0.25)]
        [TestCase(new string[] { "......", "......", "..O...", "......", "......", "......" }, 2, 3, 0.5)]
        [TestCase(new string[] { "......", "......", "..OO..", "......", "......", "......" }, 3, 2, 0.75)]
        [TestCase(new string[] { "......", "......", "..OO..", "..O...", "......", "......" }, 3, 3, 2.0)]
        public void Progress_RisesWithEachBlockCell(string[] rows, int row, int column, double expected)
        {
            var grid = OpponentTestBoards.Create(rows);

            Assert.That(Progress(grid, PlacementRegion.WholeGrid(grid), row, column, 1), Is.EqualTo(expected).Within(Tolerance));
        }

        /// <summary>An enemy cell within the clearance means no block can be built there.</summary>
        [Test]
        public void Progress_WithAnEnemyInTheClearance_IsZero()
        {
            var grid = OpponentTestBoards.Create(
                "......",
                "......",
                "..OO..",
                "..O...",
                "....P.",
                "......");

            Assert.That(Progress(grid, PlacementRegion.WholeGrid(grid), 3, 3, 1), Is.Zero);
        }

        /// <summary>One empty column between blocks is enough breathing room at clearance 1 but not at clearance 2.</summary>
        [Test]
        public void Progress_NextToAFinishedBlock_DependsOnTheClearance()
        {
            var grid = OpponentTestBoards.Create(
                "OO.....",
                "OO.....",
                ".......",
                ".......");

            Assert.That(Progress(grid, PlacementRegion.WholeGrid(grid), 0, 3, 1), Is.EqualTo(0.25).Within(Tolerance));
            Assert.That(Progress(grid, PlacementRegion.WholeGrid(grid), 0, 3, 2), Is.Zero);
        }

        /// <summary>Blocks at the board's edges and corners are fine, because space off the board counts as empty.</summary>
        [Test]
        public void Progress_InACorner_IsAllowed()
        {
            var grid = OpponentTestBoards.CreateEmpty(6, 6);

            Assert.That(Progress(grid, PlacementRegion.WholeGrid(grid), 0, 0, 2), Is.EqualTo(0.25).Within(Tolerance));
        }

        /// <summary>A block must lie wholly inside the region's rows, so a one-row region has no block sites.</summary>
        [Test]
        public void Progress_InARegionTooShortForABlock_IsZero()
        {
            var grid = OpponentTestBoards.CreateEmpty(6, 6);

            Assert.That(Progress(grid, new PlacementRegion(3, 4), 3, 2, 1), Is.Zero);
        }

        /// <summary>The table fits any board shape up to the maximum cell count, even a single long row.</summary>
        [Test]
        public void Prepare_OnASingleRowOfTheMaximumCells_Fits()
        {
            var grid = new CellGrid(4, 4);
            grid.Resize(1, 16);
            var sites = new BlockSites(16);

            Assert.DoesNotThrow(() => sites.Prepare(grid));
        }

        /// <summary>Counting living cells around a cell includes both sides, stops at the given distance and treats space off the board as empty.</summary>
        [Test]
        public void CountLivingAround_CountsBothSidesWithinTheDistance()
        {
            var grid = OpponentTestBoards.Create(
                "O....",
                ".P...",
                ".....",
                "....O");
            var sites = new BlockSites(grid.CellCount);
            sites.Prepare(grid);

            Assert.That(sites.CountLivingAround(grid, grid.ToCellIndex(0, 1), 1), Is.EqualTo(2));
            Assert.That(sites.CountLivingAround(grid, grid.ToCellIndex(2, 2), 1), Is.EqualTo(1));
            Assert.That(sites.CountLivingAround(grid, grid.ToCellIndex(2, 2), 2), Is.EqualTo(3));
        }

        /// <summary>Prepares block sites for the grid and returns the Opponent's progress for a square on the cell.</summary>
        private static double Progress(CellGrid grid, PlacementRegion region, int row, int column, int clearance)
        {
            var sites = new BlockSites(grid.CellCount);
            sites.Prepare(grid);
            return sites.Progress(grid, region, grid.ToCellIndex(row, column), CellOwner.Opponent, clearance);
        }
    }
}

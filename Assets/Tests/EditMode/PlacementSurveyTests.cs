using System;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks the survey's local placement judgements against the real engine and hand-drawn boards.</summary>
    [TestFixture]
    public sealed class PlacementSurveyTests
    {
        private const int RandomBoards = 200;
        private const int BoardRows = 12;
        private const int BoardColumns = 10;

        /// <summary>The local next-generation gain equals what one real generation does, for every empty cell of many random boards and both sides.</summary>
        [Test]
        public void NextGenerationOwnGain_MatchesOneSimulatedGeneration()
        {
            var random = new Random(17);
            var board = new CellGrid(BoardRows, BoardColumns);
            var withSquare = new CellGrid(BoardRows, BoardColumns);
            var withoutSquare = new CellGrid(BoardRows, BoardColumns);
            var survey = new PlacementSurvey(board.MaximumCellCount);
            for (var boardNumber = 0; boardNumber < RandomBoards; boardNumber++)
            {
                FillRandomly(board, random, 0.1 + 0.4 * random.NextDouble());
                var self = boardNumber % 2 == 0 ? CellOwner.Opponent : CellOwner.Player;
                survey.Survey(board, self);
                withoutSquare.CopyFrom(board);
                withoutSquare.AdvanceGeneration();
                var ownWithoutSquare = withoutSquare.CountCells(self);
                for (var cellIndex = 0; cellIndex < board.CellCount; cellIndex++)
                {
                    if (board.IsAlive(cellIndex))
                    {
                        continue;
                    }

                    withSquare.CopyFrom(board);
                    withSquare.SetOwner(cellIndex, self);
                    withSquare.AdvanceGeneration();
                    Assert.That(survey.NextGenerationOwnGain(cellIndex), Is.EqualTo(withSquare.CountCells(self) - ownWithoutSquare), $"Board {boardNumber}, cell {cellIndex}");
                }
            }
        }

        /// <summary>A lone square beside a larger enemy cluster would be converted, scoring minus twice its merged size.</summary>
        [Test]
        public void ScoreAbsorption_BesideALargerEnemy_IsAPenalty()
        {
            var grid = OpponentTestBoards.Create(
                "PPP.",
                "....");
            var survey = Survey(grid);

            Assert.That(survey.ScoreAbsorption(grid.ToCellIndex(1, 3)), Is.EqualTo(-2));
        }

        /// <summary>Filling the gap between an own block and a smaller enemy pair absorbs the pair, scoring its cells.</summary>
        [Test]
        public void ScoreAbsorption_JoiningPastASmallerEnemy_ScoresItsCells()
        {
            var grid = OpponentTestBoards.Create(
                "OO...",
                "OO.PP",
                ".....");
            var survey = Survey(grid);

            Assert.That(survey.ScoreAbsorption(grid.ToCellIndex(1, 2)), Is.EqualTo(2));
        }

        /// <summary>Equal sizes neither absorb nor are absorbed.</summary>
        [Test]
        public void ScoreAbsorption_EqualSizes_ScoresNothing()
        {
            var grid = OpponentTestBoards.Create("O.PP");
            var survey = Survey(grid);

            Assert.That(survey.ScoreAbsorption(grid.ToCellIndex(0, 1)), Is.Zero);
        }

        /// <summary>An enemy cluster touching the cell on two sides is counted once.</summary>
        [Test]
        public void ScoreAbsorption_EnemyTouchingTwice_IsCountedOnce()
        {
            var grid = OpponentTestBoards.Create(
                "OO..",
                "OO.P",
                "...P");
            var survey = Survey(grid);

            Assert.That(survey.ScoreAbsorption(grid.ToCellIndex(1, 2)), Is.EqualTo(2));
        }

        /// <summary>Joining own clusters absorbs the smaller enemies touching any of them, not just those touching the new square.</summary>
        [Test]
        public void ScoreAbsorption_MergingOwnClusters_CountsTheEnemiesTouchingThem()
        {
            var grid = OpponentTestBoards.Create(
                "........",
                "........",
                ".OO.OO..",
                "P.......",
                "P.......");
            var survey = Survey(grid);

            Assert.That(survey.ScoreAbsorption(grid.ToCellIndex(2, 3)), Is.EqualTo(2));
        }

        /// <summary>An enemy touching both the new square and an own cluster it merges is counted once.</summary>
        [Test]
        public void ScoreAbsorption_EnemyTouchingTheSquareAndAMergedCluster_IsCountedOnce()
        {
            var grid = OpponentTestBoards.Create(
                "OO..",
                "PP..");
            var survey = Survey(grid);

            Assert.That(survey.ScoreAbsorption(grid.ToCellIndex(0, 2)), Is.EqualTo(2));
        }

        /// <summary>Surveys the grid for the Opponent.</summary>
        private static PlacementSurvey Survey(CellGrid grid)
        {
            var survey = new PlacementSurvey(grid.MaximumCellCount);
            survey.Survey(grid, CellOwner.Opponent);
            return survey;
        }

        /// <summary>Fills the board with random Player and Opponent cells at the given density.</summary>
        private static void FillRandomly(CellGrid board, Random random, double density)
        {
            board.Resize(BoardRows, BoardColumns);
            for (var cellIndex = 0; cellIndex < board.CellCount; cellIndex++)
            {
                if (random.NextDouble() < density)
                {
                    board.SetOwner(cellIndex, random.Next(2) == 0 ? CellOwner.Player : CellOwner.Opponent);
                }
            }
        }
    }
}

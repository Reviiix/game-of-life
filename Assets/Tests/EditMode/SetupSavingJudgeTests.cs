using GameOfLife.Opponents;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that the app saves its setup squares only when the projected board leaves it level with or ahead of the player.</summary>
    [TestFixture]
    public sealed class SetupSavingJudgeTests
    {
        private const int Rows = 8;
        private const int Columns = 8;
        private const int GenerationsAhead = 4;

        /// <summary>A stable app block against player squares that die out means the app is ahead, so it saves its squares.</summary>
        [Test]
        public void AppAheadAfterProjection_SavesSquares()
        {
            var board = CreateBoard();
            PlaceBlock(board, 5, 5, CellOwner.Opponent);
            board.SetOwner(board.ToCellIndex(0, 0), CellOwner.Player);
            board.SetOwner(board.ToCellIndex(0, 4), CellOwner.Player);

            Assert.That(new SetupSavingJudge(Rows, Columns, GenerationsAhead).ShouldSaveSquares(board), Is.True);
        }

        /// <summary>A stable player block against a lone app square means the app is behind, so it keeps placing.</summary>
        [Test]
        public void AppBehindAfterProjection_KeepsPlacing()
        {
            var board = CreateBoard();
            PlaceBlock(board, 0, 0, CellOwner.Player);
            board.SetOwner(board.ToCellIndex(6, 6), CellOwner.Opponent);

            Assert.That(new SetupSavingJudge(Rows, Columns, GenerationsAhead).ShouldSaveSquares(board), Is.False);
        }

        /// <summary>Judging works on a copy, so the real board is left exactly as it was.</summary>
        [Test]
        public void Judging_LeavesTheBoardUntouched()
        {
            var board = CreateBoard();
            board.SetOwner(board.ToCellIndex(0, 0), CellOwner.Player);
            board.ClearChangedCells();

            new SetupSavingJudge(Rows, Columns, GenerationsAhead).ShouldSaveSquares(board);

            Assert.That(board.GetOwner(board.ToCellIndex(0, 0)), Is.EqualTo(CellOwner.Player));
            Assert.That(board.ChangedCellIndices, Is.Empty);
        }

        /// <summary>Creates an empty board at the test size.</summary>
        private static CellGrid CreateBoard()
        {
            var board = new CellGrid(Rows, Columns);
            board.Resize(Rows, Columns);
            return board;
        }

        /// <summary>Places a 2x2 block, a still life that never changes, with its top-left corner at the given cell.</summary>
        private static void PlaceBlock(CellGrid board, int row, int column, CellOwner owner)
        {
            board.SetOwner(board.ToCellIndex(row, column), owner);
            board.SetOwner(board.ToCellIndex(row, column + 1), owner);
            board.SetOwner(board.ToCellIndex(row + 1, column), owner);
            board.SetOwner(board.ToCellIndex(row + 1, column + 1), owner);
        }
    }
}

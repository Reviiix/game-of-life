using GameOfLife.Simulation;

namespace GameOfLife.Tests
{
    /// <summary>Builds small boards for opponent tests from rows of text: '.' empty, 'P' Player, 'O' Opponent.</summary>
    public static class OpponentTestBoards
    {
        /// <summary>Returns a board sized to the rows with every drawn cell set and no changes recorded.</summary>
        public static CellGrid Create(params string[] rows)
        {
            var grid = new CellGrid(rows.Length, rows[0].Length);
            grid.Resize(rows.Length, rows[0].Length);
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    var symbol = rows[row][column];
                    if (symbol != '.')
                    {
                        grid.SetOwner(grid.ToCellIndex(row, column), symbol == 'P' ? CellOwner.Player : CellOwner.Opponent);
                    }
                }
            }

            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Returns an empty board of the given size.</summary>
        public static CellGrid CreateEmpty(int rows, int columns)
        {
            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            return grid;
        }
    }
}

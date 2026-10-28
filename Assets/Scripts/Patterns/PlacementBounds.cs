using System;

namespace GameOfLife.Patterns
{
    /// <summary>Where a pattern may go right now: a band of whole rows on a board of the given size, and how many squares may be spent on it.</summary>
    public readonly struct PlacementBounds
    {
        public int FirstRow { get; }
        public int EndRowExclusive { get; }
        public int BoardRows { get; }
        public int Columns { get; }
        public int SquaresAvailable { get; }

        /// <summary>How many rows the band spans.</summary>
        public int RowCount => EndRowExclusive - FirstRow;

        /// <summary>Whether the band covers every row of the board.</summary>
        public bool IsWholeBoard => FirstRow == 0 && EndRowExclusive == BoardRows;

        /// <summary>Whether only a limited number of squares may be spent, as in Versus; Classic's whole-board bounds have no limit.</summary>
        public bool HasSquareLimit => SquaresAvailable != int.MaxValue;

        /// <summary>Describes a band of rows on a board, rejecting a band outside the board, a negative width or a negative square count.</summary>
        public PlacementBounds(int firstRow, int endRowExclusive, int boardRows, int columns, int squaresAvailable)
        {
            if (firstRow < 0 || endRowExclusive < firstRow || endRowExclusive > boardRows)
            {
                throw new ArgumentOutOfRangeException(nameof(endRowExclusive), $"Rows {firstRow} to {endRowExclusive} do not lie on a board of {boardRows} rows.");
            }

            if (columns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns), columns, "A board cannot have a negative number of columns.");
            }

            if (squaresAvailable < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(squaresAvailable), squaresAvailable, "The squares available cannot be negative.");
            }

            FirstRow = firstRow;
            EndRowExclusive = endRowExclusive;
            BoardRows = boardRows;
            Columns = columns;
            SquaresAvailable = squaresAvailable;
        }

        /// <summary>Bounds covering the whole board with no limit on squares, as Classic allows.</summary>
        public static PlacementBounds WholeBoard(int rows, int columns)
        {
            return new PlacementBounds(0, rows, rows, columns, int.MaxValue);
        }
    }
}

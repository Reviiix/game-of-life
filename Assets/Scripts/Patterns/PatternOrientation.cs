using System;
using System.Collections.Generic;

namespace GameOfLife.Patterns
{
    /// <summary>One way round of a pattern: its living cells as row and column offsets inside a tight box, in row-major order, built once and never changed.</summary>
    public sealed class PatternOrientation
    {
        private const char EmptySymbol = '.';
        private const char LivingSymbol = 'O';

        private readonly int[] rowOffsets;
        private readonly int[] columnOffsets;

        public int Rows { get; }
        public int Columns { get; }

        /// <summary>How many living cells the pattern has.</summary>
        public int CellCount => rowOffsets.Length;

        /// <summary>Stores the box size and the sorted offsets; callers hand over arrays they no longer touch.</summary>
        private PatternOrientation(int rows, int columns, int[] rowOffsets, int[] columnOffsets)
        {
            Rows = rows;
            Columns = columns;
            this.rowOffsets = rowOffsets;
            this.columnOffsets = columnOffsets;
        }

        /// <summary>Returns the row of a living cell, counted from the top of the box.</summary>
        public int GetRowOffset(int cell)
        {
            return rowOffsets[cell];
        }

        /// <summary>Returns the column of a living cell, counted from the left of the box.</summary>
        public int GetColumnOffset(int cell)
        {
            return columnOffsets[cell];
        }

        /// <summary>Returns whether the box fits inside an area of the given size without turning.</summary>
        public bool Fits(int rows, int columns)
        {
            return Rows <= rows && Columns <= columns;
        }

        /// <summary>Returns whether the box fits inside the bounds' band of rows and columns.</summary>
        public bool Fits(in PlacementBounds bounds)
        {
            return Fits(bounds.RowCount, bounds.Columns);
        }

        /// <summary>Parses rows of '.' and 'O' (top row first) into the tight box around the living cells; rejects ragged, empty or lifeless rows and any other symbol.</summary>
        public static PatternOrientation FromRows(IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                throw new ArgumentException("A pattern needs at least one row.", nameof(rows));
            }

            var width = rows[0]?.Length ?? 0;
            var livingCount = 0;
            int top = int.MaxValue, bottom = -1, left = int.MaxValue, right = -1;
            for (var row = 0; row < rows.Count; row++)
            {
                var text = rows[row];
                if (text == null || text.Length != width || width == 0)
                {
                    throw new ArgumentException($"Row {row} of the pattern is not {width} symbols wide.", nameof(rows));
                }

                for (var column = 0; column < width; column++)
                {
                    var symbol = text[column];
                    if (symbol == EmptySymbol)
                    {
                        continue;
                    }

                    if (symbol != LivingSymbol)
                    {
                        throw new ArgumentException($"Row {row} of the pattern holds '{symbol}'; only '{EmptySymbol}' and '{LivingSymbol}' are allowed.", nameof(rows));
                    }

                    livingCount++;
                    top = Math.Min(top, row);
                    bottom = Math.Max(bottom, row);
                    left = Math.Min(left, column);
                    right = Math.Max(right, column);
                }
            }

            if (livingCount == 0)
            {
                throw new ArgumentException("A pattern needs at least one living cell.", nameof(rows));
            }

            var rowOffsets = new int[livingCount];
            var columnOffsets = new int[livingCount];
            var cell = 0;
            for (var row = top; row <= bottom; row++)
            {
                for (var column = left; column <= right; column++)
                {
                    if (rows[row][column] == LivingSymbol)
                    {
                        rowOffsets[cell] = row - top;
                        columnOffsets[cell] = column - left;
                        cell++;
                    }
                }
            }

            return new PatternOrientation(bottom - top + 1, right - left + 1, rowOffsets, columnOffsets);
        }

        /// <summary>Returns the pattern turned a quarter turn clockwise, in a box with the rows and columns swapped.</summary>
        public PatternOrientation RotatedClockwise()
        {
            var keys = new int[CellCount];
            for (var cell = 0; cell < CellCount; cell++)
            {
                keys[cell] = columnOffsets[cell] * Rows + (Rows - 1 - rowOffsets[cell]);
            }

            return FromSortedKeys(Columns, Rows, keys);
        }

        /// <summary>Returns the pattern mirrored left to right, in the same size of box.</summary>
        public PatternOrientation MirroredHorizontally()
        {
            var keys = new int[CellCount];
            for (var cell = 0; cell < CellCount; cell++)
            {
                keys[cell] = rowOffsets[cell] * Columns + (Columns - 1 - columnOffsets[cell]);
            }

            return FromSortedKeys(Rows, Columns, keys);
        }

        /// <summary>Builds an orientation from row-major cell keys (row times columns plus column), sorting them so cells stay in row-major order.</summary>
        private static PatternOrientation FromSortedKeys(int rows, int columns, int[] keys)
        {
            Array.Sort(keys);
            var rowOffsets = new int[keys.Length];
            var columnOffsets = new int[keys.Length];
            for (var cell = 0; cell < keys.Length; cell++)
            {
                rowOffsets[cell] = keys[cell] / columns;
                columnOffsets[cell] = keys[cell] % columns;
            }

            return new PatternOrientation(rows, columns, rowOffsets, columnOffsets);
        }
    }
}

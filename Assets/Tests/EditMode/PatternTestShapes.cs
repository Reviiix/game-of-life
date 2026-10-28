using GameOfLife.Patterns;
using GameOfLife.Simulation;

namespace GameOfLife.Tests
{
    /// <summary>Builds test patterns from rows of text, describes an orientation or a board as rows ('.' empty, 'O' alive) and stamps an orientation onto a board as Player cells.</summary>
    public static class PatternTestShapes
    {
        private const char EmptySymbol = '.';
        private const char LivingSymbol = 'O';
        private const string TestPatternName = "Test";

        /// <summary>Builds a Persist pattern named Test from rows of '.' and 'O', top row first.</summary>
        public static CellPattern Pattern(params string[] rows)
        {
            return CellPattern.FromRows(TestPatternName, string.Empty, PatternCategory.Persist, rows);
        }

        /// <summary>The catalogue's Block: a 2x2 still life of four cells.</summary>
        public static CellPattern Block()
        {
            return CellPattern.FromRows("Block", string.Empty, PatternCategory.Persist, new[] { "OO", "OO" });
        }

        /// <summary>The catalogue's Beehive: a six-cell still life shaped like a hexagon.</summary>
        public static CellPattern Beehive()
        {
            return CellPattern.FromRows("Beehive", string.Empty, PatternCategory.Persist, new[] { ".OO.", "O..O", ".OO." });
        }

        /// <summary>The catalogue's Blinker: three cells in a row.</summary>
        public static CellPattern Blinker()
        {
            return CellPattern.FromRows("Blinker", string.Empty, PatternCategory.Pulsate, new[] { "OOO" });
        }

        /// <summary>The catalogue's Pulsar: the largest catalogue pattern, 13x13 with 48 cells.</summary>
        public static CellPattern Pulsar()
        {
            return CellPattern.FromRows("Pulsar", string.Empty, PatternCategory.Pulsate, new[]
            {
                "..OOO...OOO..",
                ".............",
                "O....O.O....O",
                "O....O.O....O",
                "O....O.O....O",
                "..OOO...OOO..",
                ".............",
                "..OOO...OOO..",
                "O....O.O....O",
                "O....O.O....O",
                "O....O.O....O",
                ".............",
                "..OOO...OOO..",
            });
        }

        /// <summary>The catalogue's Glider: the five-cell spaceship that creeps diagonally.</summary>
        public static CellPattern Glider()
        {
            return CellPattern.FromRows("Glider", string.Empty, PatternCategory.Move, new[] { ".O.", "..O", "OOO" });
        }

        /// <summary>The catalogue's Lightweight spaceship: a 4x5 shape with no symmetry at all.</summary>
        public static CellPattern LightweightSpaceship()
        {
            return CellPattern.FromRows("Lightweight spaceship", string.Empty, PatternCategory.Move, new[] { ".O..O", "O....", "O...O", "OOOO." });
        }

        /// <summary>Every catalogue shape the tests use, freshly built.</summary>
        public static CellPattern[] CatalogueShapes()
        {
            return new[] { Block(), Beehive(), Blinker(), Pulsar(), Glider(), LightweightSpaceship() };
        }

        /// <summary>Describes an orientation's tight box as rows of '.' and 'O'.</summary>
        public static string[] Describe(PatternOrientation orientation)
        {
            var symbols = CreateEmptyRows(orientation.Rows, orientation.Columns);
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                symbols[orientation.GetRowOffset(cell)][orientation.GetColumnOffset(cell)] = LivingSymbol;
            }

            return ToRows(symbols);
        }

        /// <summary>Describes a board as rows of '.' and 'O', whichever side owns each living cell.</summary>
        public static string[] Describe(CellGrid grid)
        {
            var symbols = CreateEmptyRows(grid.Rows, grid.Columns);
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                if (grid.IsAlive(cellIndex))
                {
                    symbols[grid.RowOf(cellIndex)][grid.ColumnOf(cellIndex)] = LivingSymbol;
                }
            }

            return ToRows(symbols);
        }

        /// <summary>Sets every living cell of the orientation as a Player cell, with the top-left corner of its box at the origin.</summary>
        public static void Stamp(CellGrid grid, PatternOrientation orientation, int originRow, int originColumn)
        {
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                var row = originRow + orientation.GetRowOffset(cell);
                var column = originColumn + orientation.GetColumnOffset(cell);
                grid.SetOwner(grid.ToCellIndex(row, column), CellOwner.Player);
            }
        }

        /// <summary>Returns a block of empty symbols of the given size.</summary>
        private static char[][] CreateEmptyRows(int rows, int columns)
        {
            var symbols = new char[rows][];
            for (var row = 0; row < rows; row++)
            {
                symbols[row] = new string(EmptySymbol, columns).ToCharArray();
            }

            return symbols;
        }

        /// <summary>Joins each row's symbols into one string.</summary>
        private static string[] ToRows(char[][] symbols)
        {
            var rows = new string[symbols.Length];
            for (var row = 0; row < symbols.Length; row++)
            {
                rows[row] = new string(symbols[row]);
            }

            return rows;
        }
    }
}

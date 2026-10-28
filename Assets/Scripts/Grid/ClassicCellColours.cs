using GameOfLife.Simulation;
using GameOfLife.Theming;

namespace GameOfLife.Grid
{
    /// <summary>Classic's four cell colours in the order the colour button steps through them, and how each owner is drawn: Player in ink, Third in blue, Opponent in red and Fourth in yellow.</summary>
    public static class ClassicCellColours
    {
        private static readonly CellOwner[] OwnersInButtonOrder = { CellOwner.Player, CellOwner.Third, CellOwner.Opponent, CellOwner.Fourth };

        /// <summary>How many colours Classic offers.</summary>
        public static int Count => OwnersInButtonOrder.Length;

        /// <summary>Returns the owner at a position in the colour button's order, starting with ink.</summary>
        public static CellOwner GetOwner(int position)
        {
            return OwnersInButtonOrder[position];
        }

        /// <summary>Returns the position that follows the given one, wrapping back to ink after the last colour.</summary>
        public static int GetNextPosition(int position)
        {
            return (position + 1) % OwnersInButtonOrder.Length;
        }

        /// <summary>Returns how a Classic cell with this owner is drawn, Empty for a dead cell.</summary>
        public static CellPaint ToPaint(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player:
                    return CellPaint.Ink;
                case CellOwner.Third:
                    return CellPaint.Player;
                case CellOwner.Opponent:
                    return CellPaint.Opponent;
                case CellOwner.Fourth:
                    return CellPaint.Accent;
                default:
                    return CellPaint.Empty;
            }
        }

        /// <summary>Returns the theme role of a Classic owner's colour, Ink for a dead cell, for anything that shows the colour outside the board.</summary>
        public static ThemeColour ToThemeColour(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Third:
                    return ThemeColour.Player;
                case CellOwner.Opponent:
                    return ThemeColour.Opponent;
                case CellOwner.Fourth:
                    return ThemeColour.Accent;
                default:
                    return ThemeColour.Ink;
            }
        }
    }
}

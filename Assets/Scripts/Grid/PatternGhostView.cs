using GameOfLife.Configuration;
using GameOfLife.Patterns;
using GameOfLife.Theming;
using UnityEngine;

namespace GameOfLife.Grid
{
    /// <summary>Draws a pattern's ghost over the board with a second GridView, sized and placed by the board's cells: where it can be placed, opaque tiles in the active mode's placement colour blended toward the background by the theme's ghost opacity, so the ghost still shows over cells of its own colour; where it cannot, see-through danger tiles, so the cells blocking it show.</summary>
    [RequireComponent(typeof(GridView), typeof(Canvas))]
    public sealed class PatternGhostView : MonoBehaviour
    {
        private GridView ghostGrid;
        private Canvas ghostCanvas;
        private RectTransform ghostTransform;
        private GridView board;
        private ThemeService themeService;
        private PatternOrientation shownOrientation;
        private bool shownValid;
        private CellPaint shownPlaceablePaint = CellPaint.Player;

        /// <summary>Prepares the ghost's grid for the largest board and hides it until a pattern is positioned.</summary>
        public void Initialise(GameSettings settings, ThemeService theme, GridView boardView)
        {
            ghostGrid = GetComponent<GridView>();
            ghostCanvas = GetComponent<Canvas>();
            ghostTransform = (RectTransform)transform;
            board = boardView;
            themeService = theme;
            ghostGrid.Initialise(settings, theme, showHighlights: false);
            ghostCanvas.enabled = false;
        }

        /// <summary>Shows the orientation's cells popping in at the board cell whose top-left corner the origin names, in the colour it will be placed in, or the danger colour while it cannot be placed.</summary>
        public void Show(PatternOrientation orientation, int originRow, int originColumn, bool valid, CellPaint placeablePaint)
        {
            shownOrientation = orientation;
            shownValid = valid;
            shownPlaceablePaint = placeablePaint;
            ghostGrid.Resize(orientation.Rows, orientation.Columns);
            var colour = GetGhostColour(valid);
            ShowThroughIfBlocked(valid);
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                ghostGrid.PaintCell(GetGhostCellIndex(orientation, cell), colour);
            }

            ghostGrid.ApplyPaintedCells();
            var cellSize = board.CellSize;
            ghostTransform.sizeDelta = new Vector2(orientation.Columns * cellSize.x, orientation.Rows * cellSize.y);
            MoveTo(originRow, originColumn);
            ghostCanvas.enabled = true;
        }

        /// <summary>Moves the ghost so its top-left cell sits on the given board cell.</summary>
        public void MoveTo(int originRow, int originColumn)
        {
            var cellSize = board.CellSize;
            ghostTransform.anchoredPosition = new Vector2(originColumn * cellSize.x, -originRow * cellSize.y);
        }

        /// <summary>Recolours the ghost between its placeable and blocked looks without animating; does nothing when the look is unchanged.</summary>
        public void SetValid(bool valid)
        {
            if (valid == shownValid || shownOrientation == null)
            {
                return;
            }

            shownValid = valid;
            var colour = GetGhostColour(valid);
            ShowThroughIfBlocked(valid);
            for (var cell = 0; cell < shownOrientation.CellCount; cell++)
            {
                ghostGrid.PaintCellImmediately(GetGhostCellIndex(shownOrientation, cell), colour);
            }

            ghostGrid.ApplyPaintedCells();
        }

        /// <summary>Hides the ghost, which costs nothing to draw while hidden.</summary>
        public void Hide()
        {
            ghostCanvas.enabled = false;
        }

        /// <summary>Returns the ghost grid's index for one of the orientation's cells.</summary>
        private static int GetGhostCellIndex(PatternOrientation orientation, int cell)
        {
            return orientation.GetRowOffset(cell) * orientation.Columns + orientation.GetColumnOffset(cell);
        }

        /// <summary>Returns a ghost tile's colour: when placeable, the placement colour blended toward the background by the theme's ghost opacity, so it looks faded over empty cells and paler than cells of its own colour; when blocked, the plain danger colour, which ShowThroughIfBlocked fades.</summary>
        private Color32 GetGhostColour(bool valid)
        {
            var palette = themeService.Palette;
            if (!valid)
            {
                return palette.Get(ThemeColour.Danger);
            }

            var colour = Color.Lerp(palette.Get(ThemeColour.Background), palette.Get(GridView.GetThemeColour(shownPlaceablePaint)), themeService.Theme.GhostTileOpacity);
            colour.a = 1f;
            return colour;
        }

        /// <summary>Fades a blocked ghost to the theme's ghost opacity so the occupied cells under it show through, and keeps a placeable ghost opaque.</summary>
        private void ShowThroughIfBlocked(bool valid)
        {
            ghostGrid.canvasRenderer.SetAlpha(valid ? 1f : themeService.Theme.GhostTileOpacity);
        }
    }
}

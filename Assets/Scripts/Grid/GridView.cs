using GameOfLife.Configuration;
using GameOfLife.Motion;
using GameOfLife.Theming;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameOfLife.Grid
{
    /// <summary>Draws the board as one quad of rounded tiles; one texture pixel per cell holds its colour and animated tile size, the theme supplies every colour, and a second instance draws the pattern ghost.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GridView : MaskableGraphic
    {
        private const AdditionalCanvasShaderChannels RequiredShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        private const float FullyRoundRadiusPerTileSide = 0.5f;

        [Tooltip("Only used to preview the board size in Edit mode; at runtime GameInitialiser supplies the settings.")]
        [FormerlySerializedAs("settings")]
        [SerializeField] private GameSettings editModePreviewSettings;
        [Tooltip("Only used to preview the tile shape and colours in Edit mode; at runtime the ThemeService supplies the theme.")]
        [SerializeField] private Theme editModePreviewTheme;

        private ThemeService themeService;
        private CellAnimator cellAnimator;
        private CellPaint[] cellPaints;
        private Color32[] customCellColours;
        private bool[] freshCells;
        private Texture2D cellTexture;
        private NativeArray<Color32> cellPixels;
        private Texture2D editModePreviewTexture;
        private FrameTicker cellAnimationTicker;
        private int rows;
        private int columns;
        private bool highlightsVisible;

        public override Texture mainTexture => cellTexture ? cellTexture : GetEditModePreviewTexture();

        /// <summary>The size of one cell in this RectTransform's units.</summary>
        public Vector2 CellSize
        {
            get
            {
                var bounds = rectTransform.rect.size;
                return new Vector2(bounds.x / Mathf.Max(columns, 1), bounds.y / Mathf.Max(rows, 1));
            }
        }

        private int CellCount => rows * columns;

        private Theme CurrentTheme => themeService != null ? themeService.Theme : editModePreviewTheme;

        /// <summary>Allocates the cell texture and per-cell state for the largest board once and starts following the theme; Resize sets the board's dimensions.</summary>
        public void Initialise(GameSettings settings, ThemeService theme, bool showHighlights)
        {
            Initialise(settings.MaximumRows, settings.MaximumColumns, theme, showHighlights);
        }

        /// <summary>Allocates the cell texture and per-cell state for a board of at most the given size, such as a small demo board, and starts following the theme.</summary>
        public void Initialise(int maximumRows, int maximumColumns, ThemeService theme, bool showHighlights)
        {
            themeService = theme;
            var maximumCellCount = maximumRows * maximumColumns;
            var motion = theme.Theme;
            cellAnimator = new CellAnimator(maximumCellCount, motion.CellAppear, motion.CellDisappear, motion.CellConvert, motion.CellSettle, motion.ConvertSquashScale, motion.ColourChangeShare);
            cellAnimationTicker = new FrameTicker(AdvanceCellAnimations);
            cellPaints = new CellPaint[maximumCellCount];
            customCellColours = new Color32[maximumCellCount];
            freshCells = new bool[maximumCellCount];
            cellTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GridCells",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            highlightsVisible = showHighlights;
            themeService.PaletteChanged += ApplyPalette;
            WarnIfCanvasLacksShaderChannels();
            SetMaterialDirty();
        }

        /// <summary>Resizes the board and empties every cell at once.</summary>
        public void Resize(int newRows, int newColumns)
        {
            StopAnimatingCells();
            rows = newRows;
            columns = newColumns;
            cellTexture.Reinitialize(columns, rows);
            cellPixels = cellTexture.GetPixelData<Color32>(0);
            for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            {
                cellPaints[cellIndex] = CellPaint.Empty;
                freshCells[cellIndex] = false;
            }

            cellAnimator.Clear(CellCount, cellPixels);
            cellTexture.Apply(false);
            SetVerticesDirty();
        }

        /// <summary>Stages a cell's new look in a palette colour, in its lighter fresh variant when asked, animating from its old look; call ApplyPaintedCells once all changes are staged.</summary>
        public void PaintCell(int cellIndex, CellPaint paint, bool fresh = false)
        {
            cellPaints[cellIndex] = paint;
            freshCells[cellIndex] = fresh && paint != CellPaint.Empty;
            cellAnimator.SetCell(cellIndex, GetPaintColour(cellIndex), paint != CellPaint.Empty, Time.time, cellPixels);
        }

        /// <summary>Stages a living cell in a colour of its own, such as a random colour, in its lighter fresh variant when asked, animating from its old look.</summary>
        public void PaintCell(int cellIndex, Color32 customColour, bool fresh = false)
        {
            customCellColours[cellIndex] = customColour;
            PaintCell(cellIndex, CellPaint.Custom, fresh);
        }

        /// <summary>Blends a fresh cell back to its own colour without squashing it; call ApplyPaintedCells once all are staged. Cells that are not fresh are left alone.</summary>
        public void SettleCell(int cellIndex)
        {
            if (!freshCells[cellIndex])
            {
                return;
            }

            freshCells[cellIndex] = false;
            cellAnimator.RecolourCell(cellIndex, GetPaintColour(cellIndex), Time.time, cellPixels);
        }

        /// <summary>Draws the first frame of every staged change, uploads the texture once and keeps animating every frame until every cell has settled.</summary>
        public void ApplyPaintedCells()
        {
            if (AdvanceCellAnimations() && isActiveAndEnabled)
            {
                cellAnimationTicker.Start();
            }
        }

        /// <summary>Stages a cell's new look without animating it, for a recolour that must not squash; call ApplyPaintedCells to upload it.</summary>
        public void PaintCellImmediately(int cellIndex, CellPaint paint)
        {
            cellPaints[cellIndex] = paint;
            freshCells[cellIndex] = false;
            cellAnimator.SetCellImmediately(cellIndex, GetPaintColour(cellIndex), paint != CellPaint.Empty, cellPixels);
        }

        /// <summary>Stages a living cell in a colour of its own without animating it; call ApplyPaintedCells to upload it.</summary>
        public void PaintCellImmediately(int cellIndex, Color32 customColour)
        {
            customCellColours[cellIndex] = customColour;
            PaintCellImmediately(cellIndex, CellPaint.Custom);
        }

        /// <summary>Returns the theme role a palette paint is drawn in; Empty and Custom have none and return Background.</summary>
        public static ThemeColour GetThemeColour(CellPaint paint)
        {
            switch (paint)
            {
                case CellPaint.Ink:
                    return ThemeColour.Ink;
                case CellPaint.Player:
                    return ThemeColour.Player;
                case CellPaint.Opponent:
                    return ThemeColour.Opponent;
                case CellPaint.Danger:
                    return ThemeColour.Danger;
                case CellPaint.Accent:
                    return ThemeColour.Accent;
                default:
                    return ThemeColour.Background;
            }
        }

        /// <summary>Caps cell animations so they settle within the time between generations.</summary>
        public void LimitCellAnimationsTo(float seconds)
        {
            cellAnimator.LimitDurationTo(seconds * themeService.Theme.CellMotionShareOfGeneration);
        }

        /// <summary>Shows or hides the faint tiles that mark empty cells.</summary>
        public void SetHighlightsVisible(bool visible)
        {
            highlightsVisible = visible;
            SetVerticesDirty();
        }

        /// <summary>Returns the centre of a cell in this RectTransform's local space.</summary>
        public Vector2 GetCellCentre(int cellIndex)
        {
            var bounds = rectTransform.rect;
            var cellSize = CellSize;
            var column = cellIndex % columns;
            var rowFromTop = cellIndex / columns;
            return new Vector2(bounds.xMin + (column + 0.5f) * cellSize.x, bounds.yMax - (rowFromTop + 0.5f) * cellSize.y);
        }

        /// <summary>Finds the cell under a screen position; returns false when the position is outside the grid.</summary>
        public bool TryGetCellIndexAtScreenPosition(Vector2 screenPosition, Camera eventCamera, out int cellIndex)
        {
            cellIndex = -1;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out var localPosition))
            {
                return false;
            }

            var normalisedPosition = Rect.PointToNormalized(rectTransform.rect, localPosition);
            var column = Mathf.FloorToInt(normalisedPosition.x * columns);
            var rowFromTop = Mathf.FloorToInt((1f - normalisedPosition.y) * rows);
            if (column < 0 || column >= columns || rowFromTop < 0 || rowFromTop >= rows)
            {
                return false;
            }

            cellIndex = rowFromTop * columns + column;
            return true;
        }

        /// <summary>Finds the cell nearest a screen position, treating positions past any edge as that edge's cell, so a drag off the board still follows it; false only when the position cannot be mapped or the board is empty.</summary>
        public bool TryGetNearestCellIndexAtScreenPosition(Vector2 screenPosition, Camera eventCamera, out int cellIndex)
        {
            cellIndex = -1;
            if (CellCount == 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out var localPosition))
            {
                return false;
            }

            var normalisedPosition = Rect.PointToNormalized(rectTransform.rect, localPosition);
            var column = Mathf.Clamp(Mathf.FloorToInt(normalisedPosition.x * columns), 0, columns - 1);
            var rowFromTop = Mathf.Clamp(Mathf.FloorToInt((1f - normalisedPosition.y) * rows), 0, rows - 1);
            cellIndex = rowFromTop * columns + column;
            return true;
        }

        /// <summary>Builds the board quad and passes grid size, cell size, tile gap, corner radius and empty-tile opacity to the shader through spare UV channels, leaving the vertex alpha for the canvas so fades reach every tile.</summary>
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var theme = CurrentTheme;
            var drawnRows = cellTexture ? rows : editModePreviewSettings ? editModePreviewSettings.StartingRows : 1;
            var drawnColumns = cellTexture ? columns : editModePreviewSettings ? editModePreviewSettings.StartingColumns : 1;
            var showHighlights = cellTexture ? highlightsVisible : !editModePreviewSettings || editModePreviewSettings.GridHighlightsVisibleOnStart;
            if (drawnRows <= 0 || drawnColumns <= 0)
            {
                return;
            }

            var bounds = GetPixelAdjustedRect();
            var cellSize = new Vector2(bounds.width / drawnColumns, bounds.height / drawnRows);
            var shorterCellSide = Mathf.Min(cellSize.x, cellSize.y);
            var tileGap = theme ? theme.TileGap * shorterCellSide : 0f;
            var cornerRadius = theme ? theme.TileRoundness * (shorterCellSide - tileGap) * FullyRoundRadiusPerTileSide : 0f;
            var emptyTileColour = GetEmptyTileColour(theme);
            var emptyTileOpacity = showHighlights ? emptyTileColour.a / 255f : 0f;
            emptyTileColour.a = byte.MaxValue;

            var gridShape = new Vector4(drawnColumns, drawnRows, cellSize.x, cellSize.y);
            var tileShape = new Vector4(tileGap, cornerRadius, emptyTileOpacity, 0f);
            AddVertex(vertexHelper, new Vector2(bounds.xMin, bounds.yMin), new Vector2(0f, 1f), gridShape, tileShape, emptyTileColour);
            AddVertex(vertexHelper, new Vector2(bounds.xMin, bounds.yMax), new Vector2(0f, 0f), gridShape, tileShape, emptyTileColour);
            AddVertex(vertexHelper, new Vector2(bounds.xMax, bounds.yMax), new Vector2(1f, 0f), gridShape, tileShape, emptyTileColour);
            AddVertex(vertexHelper, new Vector2(bounds.xMax, bounds.yMin), new Vector2(1f, 1f), gridShape, tileShape, emptyTileColour);
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        /// <summary>Recalculates tile sizes when the board's on-screen size changes.</summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        /// <summary>Settles every animating cell when the board is disabled, so nothing animates a board that isn't drawn.</summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            StopAnimatingCells();
        }

        /// <summary>Stops following the theme, removes the animation ticker and releases the textures this component created.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (themeService != null)
            {
                themeService.PaletteChanged -= ApplyPalette;
            }

            cellAnimationTicker?.Kill();

            DestroyCreatedTexture(cellTexture);
            DestroyCreatedTexture(editModePreviewTexture);
        }

        /// <summary>Redraws every animating cell for the current time and uploads the texture; returns whether any cell is still animating.</summary>
        private bool AdvanceCellAnimations()
        {
            var isAnimating = cellAnimator.Advance(Time.time, cellPixels);
            cellTexture.Apply(false);
            return isAnimating;
        }

        /// <summary>Stops the animation ticker and jumps every animating cell to its final look.</summary>
        private void StopAnimatingCells()
        {
            cellAnimationTicker?.Stop();
            if (cellAnimator != null && cellAnimator.HasActiveAnimations)
            {
                cellAnimator.FinishAll(cellPixels);
                cellTexture.Apply(false);
            }
        }

        /// <summary>Recolours every cell and the empty tiles at once when the palette changes.</summary>
        private void ApplyPalette(ThemePalette palette)
        {
            StopAnimatingCells();
            for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            {
                var paint = cellPaints[cellIndex];
                cellAnimator.SetCellImmediately(cellIndex, GetPaintColour(cellIndex), paint != CellPaint.Empty, cellPixels);
            }

            cellTexture.Apply(false);
            SetVerticesDirty();
        }

        /// <summary>Returns the colour a cell is painted in the current palette, the colour it settles on once any animation ends.</summary>
        public Color32 GetCellColour(int cellIndex)
        {
            return GetPaintColour(cellIndex);
        }

        /// <summary>Returns the colour a cell's paint stands for in the current palette, blended toward the background while the cell is fresh.</summary>
        private Color32 GetPaintColour(int cellIndex)
        {
            Color32 colour;
            switch (cellPaints[cellIndex])
            {
                case CellPaint.Empty:
                    return default;
                case CellPaint.Custom:
                    colour = customCellColours[cellIndex];
                    break;
                default:
                    colour = themeService.Palette.Get(GetThemeColour(cellPaints[cellIndex]));
                    break;
            }

            return freshCells[cellIndex] ? Color32.Lerp(colour, themeService.Palette.Get(ThemeColour.Background), themeService.Theme.FreshCellBackgroundBlend) : colour;
        }

        /// <summary>Returns the colour of the faint tiles marking empty cells, from the live palette or, in Edit mode, the preview theme's light palette.</summary>
        private Color32 GetEmptyTileColour(Theme theme)
        {
            if (themeService != null)
            {
                return themeService.Palette.Get(ThemeColour.SurfaceStrong);
            }

            return theme ? theme.LightPalette.Get(ThemeColour.SurfaceStrong) : (Color32)Color.grey;
        }

        /// <summary>Adds one corner of the board quad; UVs run top to bottom so texture row 0 is the top row of cells.</summary>
        private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Vector2 cellUv, Vector4 gridShape, Vector4 tileShape, Color32 emptyTileColour)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = emptyTileColour;
            vertex.uv0 = cellUv;
            vertex.uv1 = gridShape;
            vertex.uv2 = tileShape;
            vertexHelper.AddVert(vertex);
        }

        /// <summary>Returns a one-pixel empty-cell texture so the board previews in Edit mode without saving anything into the scene.</summary>
        private Texture GetEditModePreviewTexture()
        {
            if (editModePreviewTexture)
            {
                return editModePreviewTexture;
            }

            editModePreviewTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GridEditModePreview",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            editModePreviewTexture.SetPixel(0, 0, Color.clear);
            editModePreviewTexture.Apply(false);
            return editModePreviewTexture;
        }

        /// <summary>Logs a warning when the parent canvas does not pass the extra UV channels the grid shader needs; finds the canvas even while it is disabled, as a hidden dialog's is.</summary>
        private void WarnIfCanvasLacksShaderChannels()
        {
            var parentCanvas = canvas ? canvas : GetComponentInParent<Canvas>(true);
            if (parentCanvas && (parentCanvas.rootCanvas.additionalShaderChannels & RequiredShaderChannels) != RequiredShaderChannels)
            {
                Debug.LogWarning($"{name}: enable TexCoord1 and TexCoord2 in the root Canvas's Additional Shader Channels or the tiles will not draw.", this);
            }
        }

        /// <summary>Destroys a texture created at runtime, using the correct call for Edit and Play mode.</summary>
        private static void DestroyCreatedTexture(Texture2D texture)
        {
            if (!texture)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

#if UNITY_EDITOR
        /// <summary>Refreshes the Edit mode preview when its settings change in the Inspector.</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            DestroyCreatedTexture(editModePreviewTexture);
            editModePreviewTexture = null;
        }
#endif
    }
}

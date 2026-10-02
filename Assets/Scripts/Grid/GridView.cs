using GameOfLife.Configuration;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.Grid
{
    /// <summary>Draws the whole cell grid as one quad whose texture holds one pixel per cell, with grid lines drawn by the GridCells shader.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GridView : MaskableGraphic
    {
        private const AdditionalCanvasShaderChannels RequiredShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;

        [Tooltip("Supplies colours, line thickness and the grid size shown in Edit mode.")]
        [SerializeField] private GameSettings settings;

        private Texture2D cellTexture;
        private NativeArray<Color32> cellPixels;
        private Texture2D editModePreviewTexture;
        private int rows;
        private int columns;
        private bool gridLinesVisible;

        public override Texture mainTexture => cellTexture ? cellTexture : GetEditModePreviewTexture();

        /// <summary>Creates the cell texture at the largest allowed size so later resizes reuse the same memory.</summary>
        public void Initialise(int maximumRows, int maximumColumns)
        {
            cellTexture = new Texture2D(maximumColumns, maximumRows, TextureFormat.RGBA32, false)
            {
                name = "GridCells",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            gridLinesVisible = settings.GridLinesVisibleOnStart;
            WarnIfCanvasLacksShaderChannels();
        }

        /// <summary>Resizes the drawn grid and paints every cell with the given colour.</summary>
        public void Resize(int newRows, int newColumns, Color32 fillColour)
        {
            rows = newRows;
            columns = newColumns;
            cellTexture.Reinitialize(columns, rows);
            cellPixels = cellTexture.GetPixelData<Color32>(0);
            for (var pixelIndex = 0; pixelIndex < cellPixels.Length; pixelIndex++)
            {
                cellPixels[pixelIndex] = fillColour;
            }

            ApplyCellColours();
            SetVerticesDirty();
        }

        /// <summary>Stages a colour change for one cell; call ApplyCellColours once all changes are staged.</summary>
        public void SetCellColour(int cellIndex, Color32 cellColour)
        {
            cellPixels[cellIndex] = cellColour;
        }

        /// <summary>Uploads every staged cell colour to the GPU in one call.</summary>
        public void ApplyCellColours()
        {
            cellTexture.Apply(false);
        }

        /// <summary>Shows or hides the lines between cells.</summary>
        public void SetGridLinesVisible(bool visible)
        {
            gridLinesVisible = visible;
            SetVerticesDirty();
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

        /// <summary>Builds the grid quad and passes grid size and line thickness to the shader through spare UV channels.</summary>
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var drawnRows = cellTexture ? rows : settings ? settings.StartingRows : 1;
            var drawnColumns = cellTexture ? columns : settings ? settings.StartingColumns : 1;
            var showLines = cellTexture ? gridLinesVisible : settings && settings.GridLinesVisibleOnStart;
            if (drawnRows <= 0 || drawnColumns <= 0)
            {
                return;
            }

            var bounds = GetPixelAdjustedRect();
            var gridSize = new Vector2(drawnColumns, drawnRows);
            var lineThickness = showLines && settings ? settings.GridLineThickness : 0f;
            var lineColour = settings ? settings.GridLineColour : (Color32)Color.black;
            var lineThicknessAsCellFraction = new Vector2(
                lineThickness / Mathf.Max(bounds.width / drawnColumns, Mathf.Epsilon),
                lineThickness / Mathf.Max(bounds.height / drawnRows, Mathf.Epsilon));

            AddVertex(vertexHelper, new Vector2(bounds.xMin, bounds.yMin), new Vector2(0f, 1f), gridSize, lineThicknessAsCellFraction, lineColour);
            AddVertex(vertexHelper, new Vector2(bounds.xMin, bounds.yMax), new Vector2(0f, 0f), gridSize, lineThicknessAsCellFraction, lineColour);
            AddVertex(vertexHelper, new Vector2(bounds.xMax, bounds.yMax), new Vector2(1f, 0f), gridSize, lineThicknessAsCellFraction, lineColour);
            AddVertex(vertexHelper, new Vector2(bounds.xMax, bounds.yMin), new Vector2(1f, 1f), gridSize, lineThicknessAsCellFraction, lineColour);
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        /// <summary>Recalculates line thickness when the grid's on-screen size changes.</summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        /// <summary>Releases the textures this component created.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyCreatedTexture(cellTexture);
            DestroyCreatedTexture(editModePreviewTexture);
        }

        /// <summary>Adds one corner of the grid quad; UVs run top to bottom so texture row 0 is the top row of cells.</summary>
        private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Vector2 cellUv, Vector2 gridSize, Vector2 lineThicknessAsCellFraction, Color32 lineColour)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = lineColour;
            vertex.uv0 = cellUv;
            vertex.uv1 = gridSize;
            vertex.uv2 = lineThicknessAsCellFraction;
            vertexHelper.AddVert(vertex);
        }

        /// <summary>Returns a one-pixel dead-cell texture so the grid previews in Edit mode without saving anything into the scene.</summary>
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
            editModePreviewTexture.SetPixel(0, 0, settings ? settings.DeadCellColour : Color.white);
            editModePreviewTexture.Apply(false);
            return editModePreviewTexture;
        }

        /// <summary>Logs a warning when the parent canvas does not pass the extra UV channels the grid shader needs.</summary>
        private void WarnIfCanvasLacksShaderChannels()
        {
            if ((canvas.additionalShaderChannels & RequiredShaderChannels) != RequiredShaderChannels)
            {
                Debug.LogWarning($"{name}: enable TexCoord1 and TexCoord2 in the parent Canvas's Additional Shader Channels or grid lines will not draw.", this);
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
        /// <summary>Refreshes the Edit mode preview when settings change in the Inspector.</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            DestroyCreatedTexture(editModePreviewTexture);
            editModePreviewTexture = null;
        }
#endif
    }
}

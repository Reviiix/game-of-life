using GameOfLife.Patterns;
using UnityEngine;

namespace GameOfLife.UI.Patterns
{
    /// <summary>Builds, once at start-up, the tiny texture a catalogue row shows: a white block per living cell on a transparent ground, so a themed RawImage tints it.</summary>
    public static class PatternThumbnails
    {
        private const int TexelsPerCell = 4;
        private const int BlockTexels = 3;
        private const int Margin = 1;
        private static readonly Color32 Living = new(255, 255, 255, 255);
        private static readonly Color32 Clear = new(255, 255, 255, 0);

        /// <summary>Draws the orientation with its top row at the top of the texture and a one-texel gap around every block.</summary>
        public static Texture2D Create(PatternOrientation orientation)
        {
            var width = orientation.Columns * TexelsPerCell + Margin;
            var height = orientation.Rows * TexelsPerCell + Margin;
            var texels = new Color32[width * height];
            for (var texel = 0; texel < texels.Length; texel++)
            {
                texels[texel] = Clear;
            }

            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                var left = orientation.GetColumnOffset(cell) * TexelsPerCell + Margin;
                var top = orientation.GetRowOffset(cell) * TexelsPerCell + Margin;
                for (var yFromTop = top; yFromTop < top + BlockTexels; yFromTop++)
                {
                    var rowStart = (height - 1 - yFromTop) * width;
                    for (var x = left; x < left + BlockTexels; x++)
                    {
                        texels[rowStart + x] = Living;
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "PatternThumbnail",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(texels);
            texture.Apply(false, true);
            return texture;
        }
    }
}

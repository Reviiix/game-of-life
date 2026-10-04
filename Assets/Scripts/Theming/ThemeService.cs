using System;
using System.Collections.Generic;
using GameOfLife.Motion;
using UnityEngine;

namespace GameOfLife.Theming
{
    /// <summary>Holds the palette in use, blends to the other one when dark mode changes and recolours every registered graphic, the camera background and any listener.</summary>
    public sealed class ThemeService : IDisposable
    {
        private const int ExpectedGraphicCount = 128;

        private readonly List<ThemedGraphic> themedGraphics = new(ExpectedGraphicCount);
        private readonly ThemePalette currentPalette = new();
        private readonly ThemePalette blendStartPalette = new();
        private readonly ReplayableTween crossfade;
        private readonly Camera backgroundCamera;
        private ThemePalette targetPalette;
        private bool isDarkMode;

        public Theme Theme { get; }

        /// <summary>The palette being shown, part-way between light and dark while they blend; listeners should read it rather than keep a copy.</summary>
        public ThemePalette Palette => currentPalette;

        /// <summary>Raised every time the palette changes, including each step of a blend.</summary>
        public event Action<ThemePalette> PaletteChanged;

        /// <summary>Starts on the light or dark palette and paints the camera background with it.</summary>
        public ThemeService(Theme theme, bool darkMode, Camera backgroundCamera)
        {
            Theme = theme;
            isDarkMode = darkMode;
            this.backgroundCamera = backgroundCamera;
            targetPalette = theme.GetPalette(darkMode);
            currentPalette.CopyFrom(targetPalette);
            crossfade = new ReplayableTween(theme.PaletteCrossfade, ShowBlendTowardsTarget);
            backgroundCamera.backgroundColor = currentPalette.Get(ThemeColour.Background);
        }

        /// <summary>Blends from the colours on screen to the light or dark palette.</summary>
        public void SetDarkMode(bool darkMode)
        {
            if (darkMode == isDarkMode)
            {
                return;
            }

            isDarkMode = darkMode;
            blendStartPalette.CopyFrom(currentPalette);
            targetPalette = Theme.GetPalette(darkMode);
            crossfade.Play();
        }

        /// <summary>Removes the blend tween for good when the game shuts down.</summary>
        public void Dispose()
        {
            crossfade.Kill();
        }

        /// <summary>Starts recolouring a graphic whenever the palette changes and paints it now.</summary>
        public void Register(ThemedGraphic graphic)
        {
            themedGraphics.Add(graphic);
            graphic.ApplyPalette(currentPalette);
        }

        /// <summary>Stops recolouring a graphic.</summary>
        public void Unregister(ThemedGraphic graphic)
        {
            themedGraphics.Remove(graphic);
        }

        /// <summary>Sets the palette part-way to the target and repaints everything that follows it.</summary>
        private void ShowBlendTowardsTarget(float progress)
        {
            currentPalette.Blend(blendStartPalette, targetPalette, progress);
            backgroundCamera.backgroundColor = currentPalette.Get(ThemeColour.Background);
            for (var graphicIndex = 0; graphicIndex < themedGraphics.Count; graphicIndex++)
            {
                themedGraphics[graphicIndex].ApplyPalette(currentPalette);
            }

            PaletteChanged?.Invoke(currentPalette);
        }
    }
}

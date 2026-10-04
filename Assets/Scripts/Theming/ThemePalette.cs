using System;
using UnityEngine;

namespace GameOfLife.Theming
{
    /// <summary>One colour for every ThemeColour role; the Theme holds a light and a dark palette.</summary>
    [Serializable]
    public sealed class ThemePalette
    {
        private static readonly ThemeColour[] AllRoles = (ThemeColour[])Enum.GetValues(typeof(ThemeColour));

        [Tooltip("Screens and the board behind the tiles.")]
        [SerializeField] private Color background = Color.white;
        [Tooltip("Cards, bars and secondary buttons that sit on the background.")]
        [SerializeField] private Color surface = Color.white;
        [Tooltip("Empty tiles, slider tracks and switches that are off.")]
        [SerializeField] private Color surfaceStrong = Color.white;
        [Tooltip("Dialog cards that float above a dimmed screen.")]
        [SerializeField] private Color raised = Color.white;
        [Tooltip("Text, icons, living cells in classic mode and primary buttons.")]
        [SerializeField] private Color ink = Color.black;
        [Tooltip("Secondary text such as hints, values and section headings.")]
        [SerializeField] private Color inkMuted = Color.grey;
        [Tooltip("Text and icons drawn on top of ink, such as primary button labels.")]
        [SerializeField] private Color inkInverse = Color.white;
        [SerializeField] private Color player = Color.blue;
        [SerializeField] private Color opponent = Color.red;
        [Tooltip("Destructive actions such as reset, and the timer's last seconds.")]
        [SerializeField] private Color danger = Color.red;
        [Tooltip("Dims the screen behind dialogs.")]
        [SerializeField] private Color scrim = new(0f, 0f, 0f, 0.5f);
        [Tooltip("Shades the app's half of the board during Versus setup.")]
        [SerializeField] private Color shade = new(0f, 0f, 0f, 0.1f);

        /// <summary>Returns the colour for a role.</summary>
        public Color Get(ThemeColour role)
        {
            switch (role)
            {
                case ThemeColour.Background:
                    return background;
                case ThemeColour.Surface:
                    return surface;
                case ThemeColour.SurfaceStrong:
                    return surfaceStrong;
                case ThemeColour.Raised:
                    return raised;
                case ThemeColour.Ink:
                    return ink;
                case ThemeColour.InkMuted:
                    return inkMuted;
                case ThemeColour.InkInverse:
                    return inkInverse;
                case ThemeColour.Player:
                    return player;
                case ThemeColour.Opponent:
                    return opponent;
                case ThemeColour.Danger:
                    return danger;
                case ThemeColour.Scrim:
                    return scrim;
                case ThemeColour.Shade:
                    return shade;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        /// <summary>Makes every role match another palette.</summary>
        public void CopyFrom(ThemePalette source)
        {
            Blend(source, source, 0f);
        }

        /// <summary>Sets every role part-way between two palettes, where 0 is the first and 1 the second.</summary>
        public void Blend(ThemePalette from, ThemePalette to, float amount)
        {
            foreach (var role in AllRoles)
            {
                Set(role, Color.LerpUnclamped(from.Get(role), to.Get(role), amount));
            }
        }

        /// <summary>Changes the colour for a role.</summary>
        private void Set(ThemeColour role, Color colour)
        {
            switch (role)
            {
                case ThemeColour.Background:
                    background = colour;
                    break;
                case ThemeColour.Surface:
                    surface = colour;
                    break;
                case ThemeColour.SurfaceStrong:
                    surfaceStrong = colour;
                    break;
                case ThemeColour.Raised:
                    raised = colour;
                    break;
                case ThemeColour.Ink:
                    ink = colour;
                    break;
                case ThemeColour.InkMuted:
                    inkMuted = colour;
                    break;
                case ThemeColour.InkInverse:
                    inkInverse = colour;
                    break;
                case ThemeColour.Player:
                    player = colour;
                    break;
                case ThemeColour.Opponent:
                    opponent = colour;
                    break;
                case ThemeColour.Danger:
                    danger = colour;
                    break;
                case ThemeColour.Scrim:
                    scrim = colour;
                    break;
                case ThemeColour.Shade:
                    shade = colour;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}

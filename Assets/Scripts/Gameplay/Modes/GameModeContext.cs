using GameOfLife.Ads;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Effects;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using GameOfLife.Theming;
using GameOfLife.UI.Screens;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Everything game modes share: the one board, its view and its absorption particles, the countdown, audio, theme, ad breaks, settings, options and screens.</summary>
    public sealed class GameModeContext
    {
        public GameSettings Settings { get; }
        public GameOptions Options { get; }
        public ScreenNavigator Screens { get; }
        public CellGrid Grid { get; }
        public GridView GridView { get; }
        public AbsorptionEffect AbsorptionEffect { get; }
        public CountdownDisplay Countdown { get; }
        public AudioManager Audio { get; }
        public ThemeService Theme { get; }
        public IAdBreaks AdBreaks { get; }

        /// <summary>Bundles the shared systems handed to each game mode by the GameModeDirector.</summary>
        public GameModeContext(GameSettings settings, GameOptions options, ScreenNavigator screens, CellGrid grid, GridView gridView, AbsorptionEffect absorptionEffect, CountdownDisplay countdown, AudioManager audio, ThemeService theme, IAdBreaks adBreaks)
        {
            AbsorptionEffect = absorptionEffect;
            AdBreaks = adBreaks;
            Theme = theme;
            Settings = settings;
            Options = options;
            Screens = screens;
            Grid = grid;
            GridView = gridView;
            Countdown = countdown;
            Audio = audio;
        }
    }
}

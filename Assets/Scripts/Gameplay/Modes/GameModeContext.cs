using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using GameOfLife.UI.Screens;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Everything game modes share: the one board and its view, the countdown, audio, settings, options and screens.</summary>
    public sealed class GameModeContext
    {
        public GameSettings Settings { get; }
        public GameOptions Options { get; }
        public ScreenNavigator Screens { get; }
        public CellGrid Grid { get; }
        public GridView GridView { get; }
        public CountdownDisplay Countdown { get; }
        public AudioManager Audio { get; }

        /// <summary>Bundles the shared systems handed to each game mode by the GameModeDirector.</summary>
        public GameModeContext(GameSettings settings, GameOptions options, ScreenNavigator screens, CellGrid grid, GridView gridView, CountdownDisplay countdown, AudioManager audio)
        {
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

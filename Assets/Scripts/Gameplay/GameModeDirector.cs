using System;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Gameplay.Modes;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using GameOfLife.Theming;
using GameOfLife.UI.Screens;
using UnityEngine;

namespace GameOfLife.Gameplay
{
    /// <summary>Owns the shared board and keeps exactly one game mode active, routing taps, buttons and option changes to it.</summary>
    public sealed class GameModeDirector : MonoBehaviour
    {
        [SerializeField] private GridView gridView;
        [SerializeField] private GridTouchInput gridTouchInput;
        [SerializeField] private CountdownDisplay countdownDisplay;
        [Tooltip("Every playable mode; the first one is active at start-up. Add a new mode's component here.")]
        [SerializeField] private GameModeBase[] modes;

        private ScreenNavigator screens;
        private GameOptions options;
        private GameModeBase activeMode;
        private bool isMainMenuOpen = true;

        public bool IsSimulationActive => activeMode.IsSimulationActive;

        public event Action<bool> SimulationActivityChanged;

        /// <summary>Allocates the board at its largest size, prepares every mode and starts the first one behind the main menu.</summary>
        public void Initialise(GameSettings settings, GameOptions gameOptions, ScreenNavigator navigator, AudioManager audio, ThemeService theme)
        {
            options = gameOptions;
            screens = navigator;
            var grid = new CellGrid(settings.MaximumRows, settings.MaximumColumns);
            gridView.Initialise(settings, theme, options.GridHighlightsVisible);
            gridView.LimitCellAnimationsTo(options.EvolutionInterval);
            countdownDisplay.Initialise(settings, audio, theme);

            var context = new GameModeContext(settings, options, navigator, grid, gridView, countdownDisplay, audio, theme);
            foreach (var mode in modes)
            {
                mode.Initialise(context);
                mode.SimulationActivityChanged += ForwardSimulationActivity;
            }

            gridTouchInput.CellTapped += ForwardCellTap;
            options.GridSizeChanged += ForwardGridSizeChange;
            options.MatchRulesChanged += ForwardMatchRulesChange;
            options.GridHighlightsVisibilityChanged += gridView.SetHighlightsVisible;
            options.EvolutionIntervalChanged += gridView.LimitCellAnimationsTo;
            SwitchTo(modes[0]);
        }

        /// <summary>Closes the main menu into the chosen mode, starting it fresh if it isn't already the active one.</summary>
        public void PlayMode(GameModeType modeType)
        {
            screens.MainMenu.Hide();
            isMainMenuOpen = false;
            var mode = GetMode(modeType);
            if (mode == activeMode)
            {
                activeMode.OnMainMenuClosed();
            }
            else
            {
                SwitchTo(mode);
            }
        }

        /// <summary>Opens the main menu over the active mode.</summary>
        public void OpenMainMenu()
        {
            screens.MainMenu.Show();
            isMainMenuOpen = true;
            activeMode.OnMainMenuOpened();
        }

        /// <summary>Passes the play/pause button to the active mode, ignoring presses that finish after the menu has opened.</summary>
        public void TogglePlayPause()
        {
            if (!isMainMenuOpen)
            {
                activeMode.TogglePlayPause();
            }
        }

        /// <summary>Passes the reset button to the active mode, ignoring presses that finish after the menu has opened.</summary>
        public void RestartActiveMode()
        {
            if (!isMainMenuOpen)
            {
                activeMode.Restart();
            }
        }

        /// <summary>Stops listening to shared systems when destroyed.</summary>
        private void OnDestroy()
        {
            if (gridTouchInput)
            {
                gridTouchInput.CellTapped -= ForwardCellTap;
            }

            if (options != null)
            {
                options.GridSizeChanged -= ForwardGridSizeChange;
                options.MatchRulesChanged -= ForwardMatchRulesChange;
                options.GridHighlightsVisibilityChanged -= gridView.SetHighlightsVisible;
                options.EvolutionIntervalChanged -= gridView.LimitCellAnimationsTo;
            }
        }

        /// <summary>Exits the current mode and enters the new one.</summary>
        private void SwitchTo(GameModeBase mode)
        {
            if (activeMode)
            {
                activeMode.Exit();
            }

            activeMode = mode;
            activeMode.Enter();
            ForwardSimulationActivity(activeMode.IsSimulationActive);
        }

        /// <summary>Returns the mode component for a mode type; throws if no mode in the list has that type.</summary>
        private GameModeBase GetMode(GameModeType modeType)
        {
            foreach (var mode in modes)
            {
                if (mode.ModeType == modeType)
                {
                    return mode;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(modeType), modeType, $"No game mode of this type is listed on the {nameof(GameModeDirector)}.");
        }

        /// <summary>Sends a cell tap to the active mode.</summary>
        private void ForwardCellTap(int cellIndex)
        {
            activeMode.OnCellTapped(cellIndex);
        }

        /// <summary>Tells the active mode the grid size changed.</summary>
        private void ForwardGridSizeChange()
        {
            activeMode.OnGridSizeChanged();
        }

        /// <summary>Tells the active mode the match rules changed.</summary>
        private void ForwardMatchRulesChange()
        {
            activeMode.OnMatchRulesChanged();
        }

        /// <summary>Re-raises a mode's simulation activity so the play/pause button can follow whichever mode is active.</summary>
        private void ForwardSimulationActivity(bool active)
        {
            SimulationActivityChanged?.Invoke(active);
        }
    }
}

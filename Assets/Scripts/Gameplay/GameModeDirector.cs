using System;
using GameOfLife.Ads;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Effects;
using GameOfLife.Gameplay.Modes;
using GameOfLife.Grid;
using GameOfLife.Patterns;
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
        [Tooltip("Bursts particles from absorbed cells in any mode; it sits on the board so it moves with it.")]
        [SerializeField] private AbsorptionEffect absorptionEffect;
        [SerializeField] private GridTouchInput gridTouchInput;
        [SerializeField] private CountdownDisplay countdownDisplay;
        [Tooltip("Every playable mode; the first one is active at start-up. Add a new mode's component here.")]
        [SerializeField] private GameModeBase[] modes;
        [Tooltip("Positions catalogue patterns on the board; cancelled whenever the board is rebuilt or covered.")]
        [SerializeField] private PatternPlacer patternPlacer;

        private ScreenNavigator screens;
        private GameOptions options;
        private AudioManager audioManager;
        private GameModeBase activeMode;
        private Action resumeFromSettings;
        private bool isMainMenuOpen = true;
        private bool isSettingsOpen;

        public bool IsSimulationActive => activeMode.IsSimulationActive;
        public bool IsPatternPlacementAvailable => activeMode.IsPatternPlacementAvailable;

        public event Action<bool> SimulationActivityChanged;
        public event Action<bool> PatternPlacementAvailabilityChanged;

        /// <summary>Whether a menu, settings, the catalogue or a pattern ghost covers the board, so late button presses are ignored.</summary>
        private bool IsGameCovered => isMainMenuOpen || isSettingsOpen || patternPlacer.IsBusy;

        /// <summary>Allocates the board at its largest size, prepares its absorption particles, the pattern placer and every mode, and starts the first mode behind the main menu.</summary>
        public void Initialise(GameSettings settings, GameOptions gameOptions, ScreenNavigator navigator, AudioManager audio, ThemeService theme, IAdBreaks adBreaks)
        {
            options = gameOptions;
            screens = navigator;
            audioManager = audio;
            resumeFromSettings = ResumeFromSettings;
            var grid = new CellGrid(settings.MaximumRows, settings.MaximumColumns);
            gridView.Initialise(settings, theme, options.GridHighlightsVisible);
            gridView.LimitCellAnimationsTo(options.EvolutionInterval);
            absorptionEffect.Initialise(gridView);
            countdownDisplay.Initialise(settings, audio, theme);
            patternPlacer.Initialise(settings, theme, audio, grid, gridView, gridTouchInput);

            var context = new GameModeContext(settings, options, navigator, grid, gridView, absorptionEffect, countdownDisplay, audio, theme, adBreaks);
            foreach (var mode in modes)
            {
                mode.Initialise(context);
                mode.SimulationActivityChanged += ForwardSimulationActivity;
                mode.PatternPlacementAvailabilityChanged += ForwardPatternPlacementAvailability;
            }

            gridTouchInput.CellTapped += ForwardCellTap;
            options.GridSizeChanged += ForwardGridSizeChange;
            options.MatchRulesChanged += ForwardMatchRulesChange;
            options.GridHighlightsVisibilityChanged += gridView.SetHighlightsVisible;
            options.EvolutionIntervalChanged += gridView.LimitCellAnimationsTo;
            SwitchTo(modes[0]);
            if (activeMode is ClassicGameMode classicMode)
            {
                classicMode.StartMenuShowcase(new PulsatingFormationPicker(patternPlacer.Patterns, settings.MaximumRows, settings.MaximumColumns));
            }
        }

        /// <summary>Closes the main menu and shows the in-game controls, then enters the chosen mode, starting it fresh if it isn't already the active one.</summary>
        public void PlayMode(GameModeType modeType)
        {
            patternPlacer.Cancel();
            screens.MainMenu.Hide();
            screens.GameControls.Show();
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

        /// <summary>Opens the main menu over the active mode and hides the in-game controls behind it; ignored while something already covers the board.</summary>
        public void OpenMainMenu()
        {
            if (IsGameCovered)
            {
                return;
            }

            screens.MainMenu.Show();
            screens.GameControls.Hide();
            isMainMenuOpen = true;
            activeMode.OnMainMenuOpened();
        }

        /// <summary>Passes the play/pause button to the active mode, ignoring presses that finish after a menu, settings, the catalogue or a pattern has covered the board.</summary>
        public void TogglePlayPause()
        {
            if (!IsGameCovered)
            {
                activeMode.TogglePlayPause();
            }
        }

        /// <summary>Passes the reset button to the active mode, ignoring presses that finish after a menu, settings, the catalogue or a pattern has covered the board.</summary>
        public void RestartActiveMode()
        {
            if (!IsGameCovered)
            {
                activeMode.Restart();
            }
        }

        /// <summary>Opens settings over the game, pausing the mode as the main menu does; closing it returns to the game.</summary>
        public void OpenSettings()
        {
            if (IsGameCovered)
            {
                return;
            }

            isSettingsOpen = true;
            activeMode.OnMainMenuOpened();
            screens.OpenSettingsMenuOverGame(resumeFromSettings);
        }

        /// <summary>Opens the pattern catalogue for the active mode, or plays the invalid sound when it cannot place patterns now.</summary>
        public void OpenPatternCatalogue()
        {
            if (IsGameCovered)
            {
                return;
            }

            if (!patternPlacer.TryOpenCatalogue(activeMode))
            {
                audioManager.Play(SoundEffect.Invalid);
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
            ForwardPatternPlacementAvailability(activeMode.IsPatternPlacementAvailable);
        }

        /// <summary>Lets the mode carry on once settings opened from the game has closed.</summary>
        private void ResumeFromSettings()
        {
            isSettingsOpen = false;
            activeMode.OnSettingsClosed();
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

        /// <summary>Sends a cell tap to the active mode, unless a pattern is being positioned on the board.</summary>
        private void ForwardCellTap(int cellIndex)
        {
            if (!patternPlacer.IsPositioning)
            {
                activeMode.OnCellTapped(cellIndex);
            }
        }

        /// <summary>Tells the active mode the grid size changed, dropping any pattern being positioned first.</summary>
        private void ForwardGridSizeChange()
        {
            patternPlacer.Cancel();
            activeMode.OnGridSizeChanged();
        }

        /// <summary>Tells the active mode the match rules changed, dropping any pattern being positioned first.</summary>
        private void ForwardMatchRulesChange()
        {
            patternPlacer.Cancel();
            activeMode.OnMatchRulesChanged();
        }

        /// <summary>Re-raises a mode's pattern availability so the Patterns button can follow whichever mode is active, and drops any pattern being positioned once the mode stops taking patterns, as when Versus setup time runs out.</summary>
        private void ForwardPatternPlacementAvailability(bool available)
        {
            if (!available)
            {
                patternPlacer.Cancel();
            }

            PatternPlacementAvailabilityChanged?.Invoke(available);
        }

        /// <summary>Re-raises a mode's simulation activity so the play/pause button can follow whichever mode is active.</summary>
        private void ForwardSimulationActivity(bool active)
        {
            SimulationActivityChanged?.Invoke(active);
        }
    }
}

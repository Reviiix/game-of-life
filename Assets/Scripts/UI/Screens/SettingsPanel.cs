using System;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Gameplay;
using GameOfLife.Opponents;
using GameOfLife.Theming;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameOfLife.UI.Screens
{
    /// <summary>Connects the settings rows to GameOptions, starting each control at its GameSettings value.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private SliderSetting rowsSetting;
        [SerializeField] private SliderSetting columnsSetting;
        [SerializeField] private SliderSetting evolutionIntervalSetting;
        [SerializeField] private Toggle randomColoursToggle;
        [FormerlySerializedAs("gridLinesToggle")]
        [SerializeField] private Toggle gridHighlightsToggle;

        [Header("Appearance")]
        [SerializeField] private Toggle darkModeToggle;

        [Header("Audio")]
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle soundEffectsToggle;

        [Header("Versus")]
        [SerializeField] private SliderSetting setupSquaresSetting;
        [SerializeField] private SliderSetting setupTimeSetting;
        [SerializeField] private SliderSetting matchSquaresSetting;
        [SerializeField] private SliderSetting matchTimeSetting;
        [Tooltip("Steps through the opponent difficulties, showing each one's name.")]
        [SerializeField] private SliderSetting opponentDifficultySetting;

        private GameOptions options;
        private AudioManager audioManager;

        /// <summary>Sets every control's range and starting value, prepares the switches, then listens for changes.</summary>
        public void Initialise(GameSettings settings, GameOptions gameOptions, AudioManager audio, ThemeService theme)
        {
            options = gameOptions;
            audioManager = audio;
            rowsSetting.Configure(settings.MinimumGridSize, settings.MaximumRows, options.Rows, true);
            columnsSetting.Configure(settings.MinimumGridSize, settings.MaximumColumns, options.Columns, true);
            evolutionIntervalSetting.Configure(settings.MinimumEvolutionInterval, settings.MaximumEvolutionInterval, options.EvolutionInterval, false);
            setupSquaresSetting.Configure(settings.MinimumSetupSquares, settings.MaximumSetupSquares, options.VersusSetupSquares, true);
            setupTimeSetting.Configure(settings.MinimumSetupSeconds, settings.MaximumSetupSeconds, options.VersusSetupSeconds, true);
            matchSquaresSetting.Configure(settings.MinimumMatchSquares, settings.MaximumMatchSquares, options.VersusMatchSquares, true);
            matchTimeSetting.Configure(settings.MinimumMatchSeconds, settings.MaximumMatchSeconds, options.VersusMatchSeconds, true);
            opponentDifficultySetting.ConfigureNamed(GetDifficultyNames(), (int)options.OpponentDifficulty);
            randomColoursToggle.SetIsOnWithoutNotify(options.RandomColoursEnabled);
            gridHighlightsToggle.SetIsOnWithoutNotify(options.GridHighlightsVisible);
            darkModeToggle.SetIsOnWithoutNotify(options.DarkModeEnabled);
            musicToggle.SetIsOnWithoutNotify(options.MusicEnabled);
            soundEffectsToggle.SetIsOnWithoutNotify(options.SoundEffectsEnabled);

            rowsSetting.AddValueChangedListener(OnRowsChanged);
            columnsSetting.AddValueChangedListener(OnColumnsChanged);
            evolutionIntervalSetting.AddValueChangedListener(options.SetEvolutionInterval);
            setupSquaresSetting.AddValueChangedListener(OnSetupSquaresChanged);
            setupTimeSetting.AddValueChangedListener(OnSetupTimeChanged);
            matchSquaresSetting.AddValueChangedListener(OnMatchSquaresChanged);
            matchTimeSetting.AddValueChangedListener(OnMatchTimeChanged);
            randomColoursToggle.onValueChanged.AddListener(options.SetRandomColoursEnabled);
            gridHighlightsToggle.onValueChanged.AddListener(options.SetGridHighlightsVisible);
            darkModeToggle.onValueChanged.AddListener(options.SetDarkModeEnabled);
            opponentDifficultySetting.AddValueChangedListener(OnOpponentDifficultyChanged);
            musicToggle.onValueChanged.AddListener(options.SetMusicEnabled);
            soundEffectsToggle.onValueChanged.AddListener(options.SetSoundEffectsEnabled);
            foreach (var toggle in new[] { randomColoursToggle, gridHighlightsToggle, darkModeToggle, musicToggle, soundEffectsToggle })
            {
                toggle.onValueChanged.AddListener(PlayToggleClick);
                var toggleSwitch = toggle.GetComponent<ToggleSwitch>();
                toggleSwitch.Initialise(theme);
                toggleSwitch.SnapToValue();
            }
        }

        /// <summary>Clicks when any toggle changes; turning sound effects back on clicks too, as confirmation.</summary>
        private void PlayToggleClick(bool isOn)
        {
            audioManager.Play(SoundEffect.ButtonPress);
        }

        /// <summary>Passes the new row count on.</summary>
        private void OnRowsChanged(float rows)
        {
            options.SetRows((int)rows);
        }

        /// <summary>Passes the new column count on.</summary>
        private void OnColumnsChanged(float columns)
        {
            options.SetColumns((int)columns);
        }

        /// <summary>Passes the new setup square allowance on.</summary>
        private void OnSetupSquaresChanged(float squares)
        {
            options.SetVersusSetupSquares((int)squares);
        }

        /// <summary>Passes the new setup time on.</summary>
        private void OnSetupTimeChanged(float seconds)
        {
            options.SetVersusSetupSeconds((int)seconds);
        }

        /// <summary>Passes the new in-match square allowance on.</summary>
        private void OnMatchSquaresChanged(float squares)
        {
            options.SetVersusMatchSquares((int)squares);
        }

        /// <summary>Passes the new match length on.</summary>
        private void OnMatchTimeChanged(float seconds)
        {
            options.SetVersusMatchSeconds((int)seconds);
        }

        /// <summary>Sets how well the app plays from the slider's step.</summary>
        private void OnOpponentDifficultyChanged(float step)
        {
            options.SetOpponentDifficulty((OpponentDifficulty)Mathf.RoundToInt(step));
        }

        /// <summary>Returns the difficulty names in enum order, upper-cased for the settings label; built once at start-up.</summary>
        private static string[] GetDifficultyNames()
        {
            var names = Enum.GetNames(typeof(OpponentDifficulty));
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                names[nameIndex] = names[nameIndex].ToUpperInvariant();
            }

            return names;
        }
    }
}

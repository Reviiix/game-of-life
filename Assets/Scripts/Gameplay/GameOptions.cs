using System;
using GameOfLife.Configuration;
using GameOfLife.Opponents;
using UnityEngine;

namespace GameOfLife.Gameplay
{
    /// <summary>The values the player has chosen in Settings, starting from GameSettings and shared by every game mode; the audio toggles are remembered between launches.</summary>
    public sealed class GameOptions
    {
        private const string MusicEnabledKey = "Options.MusicEnabled";
        private const string SoundEffectsEnabledKey = "Options.SoundEffectsEnabled";

        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public float EvolutionInterval { get; private set; }
        public bool RandomColoursEnabled { get; private set; }
        public bool GridLinesVisible { get; private set; }
        public int VersusSetupSquares { get; private set; }
        public int VersusMatchSquares { get; private set; }
        public int VersusMatchSeconds { get; private set; }
        public OpponentDifficulty OpponentDifficulty { get; private set; }
        public bool MusicEnabled { get; private set; }
        public bool SoundEffectsEnabled { get; private set; }

        public event Action GridSizeChanged;
        public event Action<bool> GridLinesVisibilityChanged;
        public event Action MatchRulesChanged;
        public event Action<bool> MusicEnabledChanged;
        public event Action<bool> SoundEffectsEnabledChanged;

        /// <summary>Starts every option at its GameSettings value.</summary>
        public GameOptions(GameSettings settings)
        {
            Rows = settings.StartingRows;
            Columns = settings.StartingColumns;
            EvolutionInterval = settings.StartingEvolutionInterval;
            RandomColoursEnabled = settings.RandomColoursEnabledOnStart;
            GridLinesVisible = settings.GridLinesVisibleOnStart;
            VersusSetupSquares = settings.StartingSetupSquares;
            VersusMatchSquares = settings.StartingMatchSquares;
            VersusMatchSeconds = settings.StartingMatchSeconds;
            OpponentDifficulty = settings.OpponentDifficultyOnStart;
            MusicEnabled = LoadSavedToggle(MusicEnabledKey, settings.MusicEnabledOnStart);
            SoundEffectsEnabled = LoadSavedToggle(SoundEffectsEnabledKey, settings.SoundEffectsEnabledOnStart);
        }

        /// <summary>Turns the background music on or off.</summary>
        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            SaveToggle(MusicEnabledKey, enabled);
            MusicEnabledChanged?.Invoke(enabled);
        }

        /// <summary>Turns sound effects on or off.</summary>
        public void SetSoundEffectsEnabled(bool enabled)
        {
            SoundEffectsEnabled = enabled;
            SaveToggle(SoundEffectsEnabledKey, enabled);
            SoundEffectsEnabledChanged?.Invoke(enabled);
        }

        /// <summary>Sets the row count and tells the active mode to rebuild its board.</summary>
        public void SetRows(int rows)
        {
            Rows = rows;
            GridSizeChanged?.Invoke();
        }

        /// <summary>Sets the column count and tells the active mode to rebuild its board.</summary>
        public void SetColumns(int columns)
        {
            Columns = columns;
            GridSizeChanged?.Invoke();
        }

        /// <summary>Sets the seconds between generations.</summary>
        public void SetEvolutionInterval(float seconds)
        {
            EvolutionInterval = seconds;
        }

        /// <summary>Turns random colours for classic mode on or off.</summary>
        public void SetRandomColoursEnabled(bool enabled)
        {
            RandomColoursEnabled = enabled;
        }

        /// <summary>Shows or hides the lines between cells.</summary>
        public void SetGridLinesVisible(bool visible)
        {
            GridLinesVisible = visible;
            GridLinesVisibilityChanged?.Invoke(visible);
        }

        /// <summary>Sets how many squares each side places during Versus setup; a match still in setup restarts with it.</summary>
        public void SetVersusSetupSquares(int squares)
        {
            VersusSetupSquares = squares;
            MatchRulesChanged?.Invoke();
        }

        /// <summary>Sets how many squares each side can place during a Versus match; a match still in setup restarts with it.</summary>
        public void SetVersusMatchSquares(int squares)
        {
            VersusMatchSquares = squares;
            MatchRulesChanged?.Invoke();
        }

        /// <summary>Sets how long a Versus match lasts; a match still in setup restarts with it.</summary>
        public void SetVersusMatchSeconds(int seconds)
        {
            VersusMatchSeconds = seconds;
            MatchRulesChanged?.Invoke();
        }

        /// <summary>Sets how well the app plays; takes effect from the app's next move.</summary>
        public void SetOpponentDifficulty(OpponentDifficulty difficulty)
        {
            OpponentDifficulty = difficulty;
        }

        /// <summary>Reads a toggle saved on this device, falling back to the GameSettings value the first time the game runs.</summary>
        private static bool LoadSavedToggle(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        /// <summary>Saves a toggle on this device; only called when the player changes it.</summary>
        private static void SaveToggle(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

using GameOfLife.Opponents;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameOfLife.Configuration
{
    /// <summary>Designer-editable values that control grid size, starting options, timings, the store and the frame rate; colours, shapes and every animation live in the Theme.</summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game of Life/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        private const int AbsoluteMinimumGridSize = 3;
        private const int MaximumCountdownSeconds = 5;

        [Header("Grid Size")]
        [SerializeField, Min(AbsoluteMinimumGridSize)] private int minimumGridSize = AbsoluteMinimumGridSize;
        [SerializeField, Min(AbsoluteMinimumGridSize)] private int maximumRows = 60;
        [SerializeField, Min(AbsoluteMinimumGridSize)] private int maximumColumns = 30;
        [SerializeField, Min(AbsoluteMinimumGridSize)] private int startingRows = 24;
        [SerializeField, Min(AbsoluteMinimumGridSize)] private int startingColumns = 12;

        [Header("Appearance")]
        [Tooltip("Whether the faint tiles marking empty cells show the first time the game runs.")]
        [FormerlySerializedAs("gridLinesVisibleOnStart")]
        [SerializeField] private bool gridHighlightsVisibleOnStart = true;
        [SerializeField] private bool randomColoursEnabledOnStart;
        [Tooltip("Whether dark mode is on the first time the game runs; after that the player's choice is remembered.")]
        [SerializeField] private bool darkModeEnabledOnStart;

        [Header("Timing")]
        [SerializeField, Range(1, MaximumCountdownSeconds)] private int countdownSeconds = 1;
        [Tooltip("Seconds between generations, chosen with the speed slider.")]
        [SerializeField, Min(0.01f)] private float minimumEvolutionInterval = 0.1f;
        [SerializeField, Min(0.01f)] private float maximumEvolutionInterval = 5f;
        [SerializeField, Min(0.01f)] private float startingEvolutionInterval = 1f;

        [Header("Audio")]
        [SerializeField] private bool musicEnabledOnStart = true;
        [SerializeField] private bool soundEffectsEnabledOnStart = true;

        [Header("Versus")]
        [SerializeField, Min(1)] private int minimumSetupSquares = 5;
        [SerializeField, Min(1)] private int maximumSetupSquares = 50;
        [SerializeField, Min(1)] private int startingSetupSquares = 20;
        [SerializeField, Min(0)] private int minimumMatchSquares;
        [SerializeField, Min(0)] private int maximumMatchSquares = 20;
        [SerializeField, Min(0)] private int startingMatchSquares = 5;
        [SerializeField, Min(5)] private int minimumMatchSeconds = 15;
        [SerializeField, Min(5)] private int maximumMatchSeconds = 180;
        [SerializeField, Min(5)] private int startingMatchSeconds = 60;
        [SerializeField] private OpponentDifficulty opponentDifficultyOnStart = OpponentDifficulty.Easy;
        [Tooltip("Pause before the app places a square during setup, so its turn is visible.")]
        [SerializeField, Min(0)] private float opponentTurnDelay = 0.35f;

        [Header("Store")]
        [Tooltip("Must match the non-consumable product created in App Store Connect and Google Play Console.")]
        [SerializeField] private string adPassProductId = "ad_pass";

        [Header("Presentation")]
        [SerializeField, Min(30)] private int targetFrameRate = 60;

        public int MinimumGridSize => minimumGridSize;
        public int MaximumRows => maximumRows;
        public int MaximumColumns => maximumColumns;
        public int StartingRows => startingRows;
        public int StartingColumns => startingColumns;
        public bool GridHighlightsVisibleOnStart => gridHighlightsVisibleOnStart;
        public bool RandomColoursEnabledOnStart => randomColoursEnabledOnStart;
        public bool DarkModeEnabledOnStart => darkModeEnabledOnStart;
        public int CountdownSeconds => countdownSeconds;
        public float MinimumEvolutionInterval => minimumEvolutionInterval;
        public float MaximumEvolutionInterval => maximumEvolutionInterval;
        public float StartingEvolutionInterval => startingEvolutionInterval;
        public bool MusicEnabledOnStart => musicEnabledOnStart;
        public bool SoundEffectsEnabledOnStart => soundEffectsEnabledOnStart;
        public int MinimumSetupSquares => minimumSetupSquares;
        public int MaximumSetupSquares => maximumSetupSquares;
        public int StartingSetupSquares => startingSetupSquares;
        public int MinimumMatchSquares => minimumMatchSquares;
        public int MaximumMatchSquares => maximumMatchSquares;
        public int StartingMatchSquares => startingMatchSquares;
        public int MinimumMatchSeconds => minimumMatchSeconds;
        public int MaximumMatchSeconds => maximumMatchSeconds;
        public int StartingMatchSeconds => startingMatchSeconds;
        public OpponentDifficulty OpponentDifficultyOnStart => opponentDifficultyOnStart;
        public float OpponentTurnDelay => opponentTurnDelay;
        public string AdPassProductId => adPassProductId;
        public int TargetFrameRate => targetFrameRate;

        /// <summary>Keeps related values consistent whenever they are edited in the Inspector.</summary>
        private void OnValidate()
        {
            maximumRows = Mathf.Max(maximumRows, minimumGridSize);
            maximumColumns = Mathf.Max(maximumColumns, minimumGridSize);
            startingRows = Mathf.Clamp(startingRows, minimumGridSize, maximumRows);
            startingColumns = Mathf.Clamp(startingColumns, minimumGridSize, maximumColumns);
            maximumEvolutionInterval = Mathf.Max(maximumEvolutionInterval, minimumEvolutionInterval);
            startingEvolutionInterval = Mathf.Clamp(startingEvolutionInterval, minimumEvolutionInterval, maximumEvolutionInterval);
            maximumSetupSquares = Mathf.Max(maximumSetupSquares, minimumSetupSquares);
            startingSetupSquares = Mathf.Clamp(startingSetupSquares, minimumSetupSquares, maximumSetupSquares);
            maximumMatchSquares = Mathf.Max(maximumMatchSquares, minimumMatchSquares);
            startingMatchSquares = Mathf.Clamp(startingMatchSquares, minimumMatchSquares, maximumMatchSquares);
            maximumMatchSeconds = Mathf.Max(maximumMatchSeconds, minimumMatchSeconds);
            startingMatchSeconds = Mathf.Clamp(startingMatchSeconds, minimumMatchSeconds, maximumMatchSeconds);
        }
    }
}

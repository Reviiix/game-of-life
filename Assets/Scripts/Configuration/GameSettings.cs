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
        private const int MaximumEmptySideEndSeconds = 10;

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
        [Tooltip("Where the pulsating pattern behind the main menu at launch is centred, as a fraction of the board's height from the top; the default sits it between the title and the buttons.")]
        [SerializeField, Range(0f, 1f)] private float menuFormationVerticalCentre = 0.37f;
        [Tooltip("How many generations a cell placed while a game runs stays a lighter variant of its colour before turning its own colour; 0 turns this off. The Theme sets how light.")]
        [SerializeField, Min(0)] private int freshCellGenerations = 3;

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
        [Tooltip("Seconds both sides have to place their setup squares, set by SETUP TIME in Settings; squares not placed when it runs out are lost.")]
        [SerializeField, Min(5)] private int minimumSetupSeconds = 10;
        [SerializeField, Min(5)] private int maximumSetupSeconds = 120;
        [SerializeField, Min(5)] private int startingSetupSeconds = 30;
        [SerializeField, Min(0)] private int minimumMatchSquares;
        [SerializeField, Min(0)] private int maximumMatchSquares = 20;
        [SerializeField, Min(0)] private int startingMatchSquares = 5;
        [SerializeField, Min(5)] private int minimumMatchSeconds = 15;
        [SerializeField, Min(5)] private int maximumMatchSeconds = 180;
        [SerializeField, Min(5)] private int startingMatchSeconds = 60;
        [SerializeField] private OpponentDifficulty opponentDifficultyOnStart = OpponentDifficulty.Easy;
        [Tooltip("Seconds between the app's setup squares once the player has nothing left to place, so it finishes quickly but visibly.")]
        [SerializeField, Min(0)] private float opponentTurnDelay = 0.35f;
        [Tooltip("When the player ends setup early, the app plays the board this many generations ahead; if it would still be level or ahead it saves its squares too, otherwise it keeps placing.")]
        [SerializeField, Min(0)] private int opponentSetupLookAheadGenerations = 4;
        [Tooltip("Seconds of match time a match keeps going after a side loses its last living square; it ends then unless that side has placed a square again.")]
        [SerializeField, Range(0, MaximumEmptySideEndSeconds)] private int emptySideEndSeconds = 3;

        [Header("Random Placement")]
        [Tooltip("Share of a Classic board's cells that RANDOM brings to life, skipping cells already alive; in Versus it spends every setup square left instead.")]
        [SerializeField, Range(0.05f, 0.8f)] private float randomPlacementBoardShare = 0.3f;
        [Tooltip("RANDOM gets one chance to drop a whole catalogue pattern, in a random pose, for every this many cells it places.")]
        [SerializeField, Min(1)] private int randomPlacementCellsPerPatternChance = 10;
        [Tooltip("How likely each of those chances is to drop a pattern, when one fits.")]
        [SerializeField, Range(0f, 1f)] private float randomPlacementPatternChance = 0.4f;

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
        public int FreshCellGenerations => freshCellGenerations;
        public float MenuFormationVerticalCentre => menuFormationVerticalCentre;
        public int CountdownSeconds => countdownSeconds;
        public float MinimumEvolutionInterval => minimumEvolutionInterval;
        public float MaximumEvolutionInterval => maximumEvolutionInterval;
        public float StartingEvolutionInterval => startingEvolutionInterval;
        public bool MusicEnabledOnStart => musicEnabledOnStart;
        public bool SoundEffectsEnabledOnStart => soundEffectsEnabledOnStart;
        public int MinimumSetupSquares => minimumSetupSquares;
        public int MaximumSetupSquares => maximumSetupSquares;
        public int StartingSetupSquares => startingSetupSquares;
        public int MinimumSetupSeconds => minimumSetupSeconds;
        public int MaximumSetupSeconds => maximumSetupSeconds;
        public int StartingSetupSeconds => startingSetupSeconds;
        public int MinimumMatchSquares => minimumMatchSquares;
        public int MaximumMatchSquares => maximumMatchSquares;
        public int StartingMatchSquares => startingMatchSquares;
        public int MinimumMatchSeconds => minimumMatchSeconds;
        public int MaximumMatchSeconds => maximumMatchSeconds;
        public int StartingMatchSeconds => startingMatchSeconds;
        public OpponentDifficulty OpponentDifficultyOnStart => opponentDifficultyOnStart;
        public float OpponentTurnDelay => opponentTurnDelay;
        public int OpponentSetupLookAheadGenerations => opponentSetupLookAheadGenerations;
        public int EmptySideEndSeconds => emptySideEndSeconds;
        public float RandomPlacementBoardShare => randomPlacementBoardShare;
        public int RandomPlacementCellsPerPatternChance => randomPlacementCellsPerPatternChance;
        public float RandomPlacementPatternChance => randomPlacementPatternChance;
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
            maximumSetupSeconds = Mathf.Max(maximumSetupSeconds, minimumSetupSeconds);
            startingSetupSeconds = Mathf.Clamp(startingSetupSeconds, minimumSetupSeconds, maximumSetupSeconds);
            maximumMatchSquares = Mathf.Max(maximumMatchSquares, minimumMatchSquares);
            startingMatchSquares = Mathf.Clamp(startingMatchSquares, minimumMatchSquares, maximumMatchSquares);
            maximumMatchSeconds = Mathf.Max(maximumMatchSeconds, minimumMatchSeconds);
            startingMatchSeconds = Mathf.Clamp(startingMatchSeconds, minimumMatchSeconds, maximumMatchSeconds);
        }
    }
}

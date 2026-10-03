using GameOfLife.Opponents;
using UnityEngine;

namespace GameOfLife.Configuration
{
    /// <summary>Designer-editable values that control grid size, cell appearance, timings and presentation.</summary>
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

        [Header("Cell Appearance")]
        [SerializeField] private Color32 aliveCellColour = new(0, 0, 0, 255);
        [SerializeField] private Color32 deadCellColour = new(255, 255, 255, 255);
        [SerializeField] private Color32 gridLineColour = new(0, 0, 0, 255);
        [Tooltip("Thickness of the lines between cells, in canvas units.")]
        [SerializeField, Min(0)] private float gridLineThickness = 5f;
        [SerializeField] private bool gridLinesVisibleOnStart = true;
        [SerializeField] private bool randomColoursEnabledOnStart;

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
        [SerializeField] private Color32 playerCellColour = new(0, 102, 255, 255);
        [SerializeField] private Color32 opponentCellColour = new(230, 38, 38, 255);
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
        [SerializeField, Min(0)] private float startupFadeDuration = 1f;
        [SerializeField, Min(0.01f)] private float buttonPressDuration = 0.5f;
        [Tooltip("Button scale over the press animation; time runs from 0 to 1.")]
        [SerializeField] private AnimationCurve buttonPressScale = new(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.2f), new Keyframe(1f, 1f));
        [SerializeField, Min(30)] private int targetFrameRate = 60;

        public int MinimumGridSize => minimumGridSize;
        public int MaximumRows => maximumRows;
        public int MaximumColumns => maximumColumns;
        public int StartingRows => startingRows;
        public int StartingColumns => startingColumns;
        public Color32 AliveCellColour => aliveCellColour;
        public Color32 DeadCellColour => deadCellColour;
        public Color32 GridLineColour => gridLineColour;
        public float GridLineThickness => gridLineThickness;
        public bool GridLinesVisibleOnStart => gridLinesVisibleOnStart;
        public bool RandomColoursEnabledOnStart => randomColoursEnabledOnStart;
        public int CountdownSeconds => countdownSeconds;
        public float MinimumEvolutionInterval => minimumEvolutionInterval;
        public float MaximumEvolutionInterval => maximumEvolutionInterval;
        public float StartingEvolutionInterval => startingEvolutionInterval;
        public bool MusicEnabledOnStart => musicEnabledOnStart;
        public bool SoundEffectsEnabledOnStart => soundEffectsEnabledOnStart;
        public Color32 PlayerCellColour => playerCellColour;
        public Color32 OpponentCellColour => opponentCellColour;
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
        public float StartupFadeDuration => startupFadeDuration;
        public float ButtonPressDuration => buttonPressDuration;
        public AnimationCurve ButtonPressScale => buttonPressScale;
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

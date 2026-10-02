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
        }
    }
}

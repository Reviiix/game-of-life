using GameOfLife.Configuration;
using GameOfLife.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Screens
{
    /// <summary>Connects the settings sliders and toggles to the game, starting them at the values in GameSettings.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider rowsSlider;
        [SerializeField] private Slider columnsSlider;
        [SerializeField] private Slider evolutionIntervalSlider;
        [SerializeField] private Toggle randomColoursToggle;
        [SerializeField] private Toggle gridLinesToggle;

        private GameController gameController;

        /// <summary>Sets every control's range and starting value, then listens for changes.</summary>
        public void Initialise(GameSettings settings, GameController controller)
        {
            gameController = controller;
            ConfigureSlider(rowsSlider, settings.MinimumGridSize, settings.MaximumRows, settings.StartingRows, true);
            ConfigureSlider(columnsSlider, settings.MinimumGridSize, settings.MaximumColumns, settings.StartingColumns, true);
            ConfigureSlider(evolutionIntervalSlider, settings.MinimumEvolutionInterval, settings.MaximumEvolutionInterval, settings.StartingEvolutionInterval, false);
            randomColoursToggle.SetIsOnWithoutNotify(settings.RandomColoursEnabledOnStart);
            gridLinesToggle.SetIsOnWithoutNotify(settings.GridLinesVisibleOnStart);

            rowsSlider.onValueChanged.AddListener(OnRowsChanged);
            columnsSlider.onValueChanged.AddListener(OnColumnsChanged);
            evolutionIntervalSlider.onValueChanged.AddListener(gameController.SetEvolutionInterval);
            randomColoursToggle.onValueChanged.AddListener(gameController.SetRandomColoursEnabled);
            gridLinesToggle.onValueChanged.AddListener(gameController.SetGridLinesVisible);
        }

        /// <summary>Applies a slider's range and value without triggering its change event.</summary>
        private static void ConfigureSlider(Slider slider, float minimum, float maximum, float startingValue, bool wholeNumbers)
        {
            slider.wholeNumbers = wholeNumbers;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.SetValueWithoutNotify(startingValue);
        }

        /// <summary>Passes the new row count to the game.</summary>
        private void OnRowsChanged(float rows)
        {
            gameController.SetRows((int)rows);
        }

        /// <summary>Passes the new column count to the game.</summary>
        private void OnColumnsChanged(float columns)
        {
            gameController.SetColumns((int)columns);
        }
    }
}

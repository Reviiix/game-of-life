using GameOfLife.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Starts or pauses the active mode, showing a pause icon while its simulation is counting down or running.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class PlayPauseButton : AnimatedButton
    {
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;

        private Image iconImage;
        private GameModeDirector gameModeDirector;

        /// <summary>Connects to the director and shows the icon for the active mode.</summary>
        public void Initialise(GameModeDirector director)
        {
            iconImage = GetComponent<Image>();
            gameModeDirector = director;
            gameModeDirector.SimulationActivityChanged += ShowIconForSimulationActivity;
            ShowIconForSimulationActivity(gameModeDirector.IsSimulationActive);
        }

        /// <summary>Starts or pauses the active mode.</summary>
        protected override void OnPressed()
        {
            gameModeDirector.TogglePlayPause();
        }

        /// <summary>Stops listening for activity changes and removes the press tweens when destroyed.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (gameModeDirector)
            {
                gameModeDirector.SimulationActivityChanged -= ShowIconForSimulationActivity;
            }
        }

        /// <summary>Shows the pause icon while the simulation is active, otherwise the play icon.</summary>
        private void ShowIconForSimulationActivity(bool simulationActive)
        {
            iconImage.sprite = simulationActive ? pauseIcon : playIcon;
        }
    }
}

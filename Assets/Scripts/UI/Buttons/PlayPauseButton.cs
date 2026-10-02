using GameOfLife.Core;
using GameOfLife.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Starts or pauses the simulation and shows a play icon while editing and a pause icon otherwise.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class PlayPauseButton : AnimatedButton
    {
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;

        private Image iconImage;
        private GameController gameController;

        /// <summary>Connects to the game controller and shows the icon for the current phase.</summary>
        public void Initialise(GameController controller)
        {
            iconImage = GetComponent<Image>();
            gameController = controller;
            gameController.PhaseChanged += ShowIconForPhase;
            ShowIconForPhase(gameController.Phase);
        }

        /// <summary>Starts or pauses the game.</summary>
        protected override void OnPressed()
        {
            gameController.TogglePlayPause();
        }

        /// <summary>Stops listening for phase changes when destroyed.</summary>
        private void OnDestroy()
        {
            if (gameController)
            {
                gameController.PhaseChanged -= ShowIconForPhase;
            }
        }

        /// <summary>Shows the play icon while editing, otherwise the pause icon.</summary>
        private void ShowIconForPhase(GamePhase phase)
        {
            iconImage.sprite = phase == GamePhase.Editing ? playIcon : pauseIcon;
        }
    }
}

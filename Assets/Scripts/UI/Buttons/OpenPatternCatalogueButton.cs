using GameOfLife.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens the pattern catalogue from the bottom bar, greying itself while the active mode cannot place patterns.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class OpenPatternCatalogueButton : AnimatedButton
    {
        private Button catalogueButton;
        private GameModeDirector gameModeDirector;

        /// <summary>Connects to the director and follows whether a pattern may be placed.</summary>
        public void Initialise(GameModeDirector director)
        {
            catalogueButton = GetComponent<Button>();
            gameModeDirector = director;
            gameModeDirector.PatternPlacementAvailabilityChanged += SetInteractable;
            SetInteractable(gameModeDirector.IsPatternPlacementAvailable);
        }

        /// <summary>Opens the catalogue.</summary>
        protected override void OnPressed()
        {
            gameModeDirector.OpenPatternCatalogue();
        }

        /// <summary>Stops following the director and removes the press tweens when destroyed.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (gameModeDirector)
            {
                gameModeDirector.PatternPlacementAvailabilityChanged -= SetInteractable;
            }
        }

        /// <summary>Greys the button while no pattern may be placed.</summary>
        private void SetInteractable(bool available)
        {
            catalogueButton.interactable = available;
        }
    }
}

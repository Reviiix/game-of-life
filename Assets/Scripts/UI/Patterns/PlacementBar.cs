using System;
using GameOfLife.UI.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Patterns
{
    /// <summary>The controls shown while a pattern is being positioned: cancel, rotate, flip and confirm; it re-raises their presses and greys the ones that cannot act.</summary>
    public sealed class PlacementBar : MonoBehaviour
    {
        [SerializeField] private ActionButton cancelButton;
        [SerializeField] private ActionButton rotateButton;
        [SerializeField] private ActionButton flipButton;
        [SerializeField] private ActionButton confirmButton;

        private Button rotate;
        private Button confirm;

        public event Action CancelPressed;
        public event Action RotatePressed;
        public event Action FlipPressed;
        public event Action ConfirmPressed;

        /// <summary>Caches the buttons that get greyed and listens to every press.</summary>
        public void Initialise()
        {
            rotate = rotateButton.GetComponent<Button>();
            confirm = confirmButton.GetComponent<Button>();
            cancelButton.Pressed += RaiseCancelPressed;
            rotateButton.Pressed += RaiseRotatePressed;
            flipButton.Pressed += RaiseFlipPressed;
            confirmButton.Pressed += RaiseConfirmPressed;
        }

        /// <summary>Shows the bar.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Hides the bar, which also resets any button mid-press.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Greys the tick while the ghost cannot be placed where it is.</summary>
        public void SetConfirmEnabled(bool enabled)
        {
            confirm.interactable = enabled;
        }

        /// <summary>Greys rotate while a quarter turn would not fit.</summary>
        public void SetRotateEnabled(bool enabled)
        {
            rotate.interactable = enabled;
        }

        /// <summary>Stops listening to the buttons when destroyed.</summary>
        private void OnDestroy()
        {
            if (cancelButton)
            {
                cancelButton.Pressed -= RaiseCancelPressed;
            }

            if (rotateButton)
            {
                rotateButton.Pressed -= RaiseRotatePressed;
            }

            if (flipButton)
            {
                flipButton.Pressed -= RaiseFlipPressed;
            }

            if (confirmButton)
            {
                confirmButton.Pressed -= RaiseConfirmPressed;
            }
        }

        /// <summary>Reports the cross.</summary>
        private void RaiseCancelPressed()
        {
            CancelPressed?.Invoke();
        }

        /// <summary>Reports the rotate button.</summary>
        private void RaiseRotatePressed()
        {
            RotatePressed?.Invoke();
        }

        /// <summary>Reports the flip button.</summary>
        private void RaiseFlipPressed()
        {
            FlipPressed?.Invoke();
        }

        /// <summary>Reports the tick.</summary>
        private void RaiseConfirmPressed()
        {
            ConfirmPressed?.Invoke();
        }
    }
}

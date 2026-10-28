using System;
using GameOfLife.Theming;
using UnityEngine;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Classic's colour button: shows the colour new cells will be placed in as a swatch, and raises Pressed on every tap, without waiting for its spring, so Classic can step through the colours quickly.</summary>
    public sealed class CellColourButton : AnimatedButton
    {
        [Tooltip("The swatch that shows the chosen colour.")]
        [SerializeField] private ThemedGraphic swatch;

        public event Action Pressed;

        protected override bool ActsImmediately => true;

        /// <summary>Colours the swatch with a theme role, so it follows dark mode like every other colour.</summary>
        public void ShowColour(ThemeColour colour)
        {
            swatch.SetColour(colour);
        }

        /// <summary>Tells Classic the button was pressed.</summary>
        protected override void OnPressed()
        {
            Pressed?.Invoke();
        }
    }
}

using System;

namespace GameOfLife.UI.Buttons
{
    /// <summary>A button whose action is supplied by code: it raises Pressed once its spring settles, for dialogs whose owner decides what pressing means.</summary>
    public sealed class ActionButton : AnimatedButton
    {
        public event Action Pressed;

        /// <summary>Tells the owner the button was pressed.</summary>
        protected override void OnPressed()
        {
            Pressed?.Invoke();
        }
    }
}

using GameOfLife.Core;
using GameOfLife.Gameplay;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Starts the active mode again from an empty board.</summary>
    public sealed class ResetButton : AnimatedButton
    {
        /// <summary>Restarts the active mode.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<GameModeDirector>().RestartActiveMode();
        }
    }
}

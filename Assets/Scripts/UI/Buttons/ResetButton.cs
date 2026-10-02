using GameOfLife.Core;
using GameOfLife.Gameplay;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Stops the simulation and clears every cell.</summary>
    public sealed class ResetButton : AnimatedButton
    {
        /// <summary>Resets the game.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<GameController>().ResetGame();
        }
    }
}

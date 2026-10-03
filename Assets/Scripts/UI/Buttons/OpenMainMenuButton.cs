using GameOfLife.Core;
using GameOfLife.Gameplay;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens the main menu over the game.</summary>
    public sealed class OpenMainMenuButton : AnimatedButton
    {
        /// <summary>Shows the main menu; Versus pauses while it is open.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<GameModeDirector>().OpenMainMenu();
        }
    }
}

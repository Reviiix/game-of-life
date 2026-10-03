using GameOfLife.Core;
using GameOfLife.UI.Screens;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Closes the main menu to reveal the game.</summary>
    public sealed class CloseMainMenuButton : AnimatedButton
    {
        /// <summary>Hides the main menu.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<ScreenNavigator>().MainMenu.Hide();
        }
    }
}

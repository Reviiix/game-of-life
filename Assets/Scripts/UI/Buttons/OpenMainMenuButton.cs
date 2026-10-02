using GameOfLife.Core;
using GameOfLife.UI.Screens;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens the main menu over the game.</summary>
    public sealed class OpenMainMenuButton : AnimatedButton
    {
        /// <summary>Shows the main menu.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<ScreenNavigator>().MainMenu.Show();
        }
    }
}

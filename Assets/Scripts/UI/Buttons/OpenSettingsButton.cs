using GameOfLife.Core;
using GameOfLife.UI.Screens;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens the settings menu in place of the main menu.</summary>
    public sealed class OpenSettingsButton : AnimatedButton
    {
        /// <summary>Swaps the main menu for the settings menu.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<ScreenNavigator>().OpenSettingsMenu();
        }
    }
}

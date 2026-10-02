using GameOfLife.Core;
using GameOfLife.UI.Screens;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens the settings menu.</summary>
    public sealed class OpenSettingsButton : AnimatedButton
    {
        /// <summary>Shows the settings menu.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<ScreenNavigator>().SettingsMenu.Show();
        }
    }
}

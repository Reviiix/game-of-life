using GameOfLife.Core;
using GameOfLife.UI.HowToPlay;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens the how-to-play guide from the main menu.</summary>
    public sealed class OpenHowToPlayButton : AnimatedButton
    {
        /// <summary>Opens the guide on its Classic tab.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<HowToPlayPanel>().Open();
        }
    }
}

using GameOfLife.Core;
using GameOfLife.Gameplay;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Opens Settings over the game from the bottom bar, through the director so the mode pauses and late presses are ignored.</summary>
    public sealed class OpenSettingsOverGameButton : AnimatedButton
    {
        /// <summary>Asks the director to cover the game with settings.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<GameModeDirector>().OpenSettings();
        }
    }
}

using GameOfLife.Core;
using GameOfLife.Gameplay;
using GameOfLife.Gameplay.Modes;
using UnityEngine;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Main menu button that closes the menu into one game mode; set the mode on each prefab variant.</summary>
    public sealed class StartGameModeButton : AnimatedButton
    {
        [SerializeField] private GameModeType mode;

        /// <summary>Plays the chosen mode, resuming it if it is already active.</summary>
        protected override void OnPressed()
        {
            ServiceLocator.Get<GameModeDirector>().PlayMode(mode);
        }
    }
}

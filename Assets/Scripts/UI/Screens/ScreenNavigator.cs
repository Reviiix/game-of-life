using UnityEngine;

namespace GameOfLife.UI.Screens
{
    /// <summary>Gives every system one place to reach the game's screens and sets which screens show at start-up.</summary>
    public sealed class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] private ScreenPanel mainMenu;
        [SerializeField] private ScreenPanel settingsMenu;
        [SerializeField] private ScreenPanel invalidGameDialog;

        public ScreenPanel MainMenu => mainMenu;
        public ScreenPanel SettingsMenu => settingsMenu;
        public ScreenPanel InvalidGameDialog => invalidGameDialog;

        /// <summary>Shows the main menu and hides every dialog, whatever state the scene was saved in.</summary>
        public void Initialise()
        {
            mainMenu.Show();
            settingsMenu.Hide();
            invalidGameDialog.Hide();
        }
    }
}

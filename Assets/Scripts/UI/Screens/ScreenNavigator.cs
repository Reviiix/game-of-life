using UnityEngine;

namespace GameOfLife.UI.Screens
{
    /// <summary>Gives every system one place to reach the game's screens and sets which screens show at start-up.</summary>
    public sealed class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] private ScreenPanel mainMenu;
        [SerializeField] private ScreenPanel settingsMenu;
        [SerializeField] private ScreenPanel invalidGameDialog;
        [SerializeField] private ScreenPanel matchResultDialog;
        [SerializeField] private ScreenPanel messageDialog;

        public ScreenPanel MainMenu => mainMenu;
        public ScreenPanel InvalidGameDialog => invalidGameDialog;

        /// <summary>Shows the main menu, hides every dialog whatever state the scene was saved in, and returns to the menu when settings close.</summary>
        public void Initialise()
        {
            mainMenu.SetVisible(true);
            settingsMenu.SetVisible(false);
            invalidGameDialog.SetVisible(false);
            matchResultDialog.SetVisible(false);
            messageDialog.SetVisible(false);
            settingsMenu.Hidden += mainMenu.Show;
        }

        /// <summary>Replaces the main menu with the settings menu so the two never overlap.</summary>
        public void OpenSettingsMenu()
        {
            mainMenu.SetVisible(false);
            settingsMenu.Show();
        }

        /// <summary>Stops listening to the settings menu when destroyed.</summary>
        private void OnDestroy()
        {
            if (settingsMenu)
            {
                settingsMenu.Hidden -= mainMenu.Show;
            }
        }
    }
}

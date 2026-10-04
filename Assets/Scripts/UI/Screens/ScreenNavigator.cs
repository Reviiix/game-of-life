using GameOfLife.Theming;
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

        /// <summary>Gives every screen its transitions, shows the main menu, hides every dialog whatever state the scene was saved in, and returns to the menu when settings close.</summary>
        public void Initialise(Theme theme)
        {
            foreach (var screen in new[] { mainMenu, settingsMenu, invalidGameDialog, matchResultDialog, messageDialog })
            {
                screen.Initialise(theme);
            }

            mainMenu.SetVisible(true);
            settingsMenu.SetVisible(false);
            invalidGameDialog.SetVisible(false);
            matchResultDialog.SetVisible(false);
            messageDialog.SetVisible(false);
            settingsMenu.Shown += HideMainMenuBehindSettings;
            settingsMenu.Hidden += mainMenu.Show;
        }

        /// <summary>Opens the settings menu over the main menu, which is hidden once settings has fully appeared so the two never draw together for long.</summary>
        public void OpenSettingsMenu()
        {
            settingsMenu.Show();
        }

        /// <summary>Hides the main menu behind the fully opaque settings menu so it costs nothing to draw.</summary>
        private void HideMainMenuBehindSettings()
        {
            mainMenu.SetVisible(false);
        }

        /// <summary>Stops listening to the settings menu when destroyed.</summary>
        private void OnDestroy()
        {
            if (settingsMenu)
            {
                settingsMenu.Shown -= HideMainMenuBehindSettings;
                settingsMenu.Hidden -= mainMenu.Show;
            }
        }
    }
}

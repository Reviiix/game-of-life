using System;
using GameOfLife.Theming;
using UnityEngine;

namespace GameOfLife.UI.Screens
{
    /// <summary>Gives every system one place to reach the game's screens and sets which screens show at start-up.</summary>
    public sealed class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] private ScreenPanel mainMenu;
        [Tooltip("The in-game controls (button bar, Versus HUD, countdown); hidden while the main menu is open.")]
        [SerializeField] private ScreenPanel gameControls;
        [SerializeField] private ScreenPanel settingsMenu;
        [SerializeField] private ScreenPanel invalidGameDialog;
        [SerializeField] private ScreenPanel matchResultDialog;
        [SerializeField] private ScreenPanel messageDialog;
        [SerializeField] private ScreenPanel earlyStartDialog;
        [SerializeField] private ScreenPanel patternsDialog;
        [SerializeField] private ScreenPanel howToPlayDialog;

        private Action settingsClosedOverGame;

        public ScreenPanel MainMenu => mainMenu;
        public ScreenPanel GameControls => gameControls;
        public ScreenPanel InvalidGameDialog => invalidGameDialog;

        /// <summary>Gives every screen its transitions, shows the main menu, hides the in-game controls and every dialog whatever state the scene was saved in, and returns to the menu, or to the game, when settings close; the guide opens over the menu and hides it the same way, and the menu stops taking taps as soon as either page starts opening.</summary>
        public void Initialise(Theme theme)
        {
            foreach (var screen in new[] { mainMenu, gameControls, settingsMenu, invalidGameDialog, matchResultDialog, messageDialog, earlyStartDialog, patternsDialog, howToPlayDialog })
            {
                screen.Initialise(theme);
            }

            mainMenu.SetVisible(true);
            gameControls.SetVisible(false);
            settingsMenu.SetVisible(false);
            invalidGameDialog.SetVisible(false);
            matchResultDialog.SetVisible(false);
            messageDialog.SetVisible(false);
            earlyStartDialog.SetVisible(false);
            patternsDialog.SetVisible(false);
            howToPlayDialog.SetVisible(false);
            settingsMenu.Showing += mainMenu.BlockTaps;
            settingsMenu.Shown += HideMainMenuBehindPage;
            settingsMenu.Hidden += ReturnFromSettings;
            howToPlayDialog.Showing += mainMenu.BlockTaps;
            howToPlayDialog.Shown += HideMainMenuBehindPage;
            howToPlayDialog.Hidden += mainMenu.Show;
        }

        /// <summary>Opens the settings menu over the main menu, which is hidden once settings has fully appeared so the two never draw together for long.</summary>
        public void OpenSettingsMenu()
        {
            settingsMenu.Show();
        }

        /// <summary>Opens the settings menu over the game; closing it runs the callback instead of showing the main menu.</summary>
        public void OpenSettingsMenuOverGame(Action closed)
        {
            settingsClosedOverGame = closed;
            settingsMenu.Show();
        }

        /// <summary>Returns to wherever settings was opened from: the game, through its callback, or else the main menu.</summary>
        private void ReturnFromSettings()
        {
            var closed = settingsClosedOverGame;
            settingsClosedOverGame = null;
            if (closed != null)
            {
                closed();
            }
            else
            {
                mainMenu.Show();
            }
        }

        /// <summary>Hides the main menu behind a fully opaque page, settings or the guide, so it costs nothing to draw.</summary>
        private void HideMainMenuBehindPage()
        {
            mainMenu.SetVisible(false);
        }

        /// <summary>Stops listening to the settings menu and the guide when destroyed.</summary>
        private void OnDestroy()
        {
            if (settingsMenu)
            {
                settingsMenu.Showing -= mainMenu.BlockTaps;
                settingsMenu.Shown -= HideMainMenuBehindPage;
                settingsMenu.Hidden -= ReturnFromSettings;
            }

            if (howToPlayDialog)
            {
                howToPlayDialog.Showing -= mainMenu.BlockTaps;
                howToPlayDialog.Shown -= HideMainMenuBehindPage;
                howToPlayDialog.Hidden -= mainMenu.Show;
            }
        }
    }
}

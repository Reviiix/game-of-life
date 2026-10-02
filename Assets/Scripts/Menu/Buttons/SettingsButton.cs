using System;

namespace Menu.Buttons
{
    public class SettingsButton : GameButton
    {
        protected override void OnClick(Action callBack)
        {
            StartCoroutine(AnimateButton(EnableSettingsDialog));
        }
        
        private static void EnableSettingsDialog()
        {
            MenuManager.Instance.EnableSettings();

        }
    }
}

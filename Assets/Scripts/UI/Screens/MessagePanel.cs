using TMPro;
using UnityEngine;

namespace GameOfLife.UI.Screens
{
    /// <summary>A dialog that shows one line of text, for short confirmations and errors.</summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class MessagePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageLabel;

        private ScreenPanel screenPanel;

        /// <summary>Writes the message into the dialog and opens it.</summary>
        public void ShowMessage(string message)
        {
            if (!screenPanel)
            {
                screenPanel = GetComponent<ScreenPanel>();
            }

            messageLabel.SetText(message);
            screenPanel.Show();
        }
    }
}

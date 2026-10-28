using System.Text;
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
            messageLabel.SetText(message);
            Open();
        }

        /// <summary>Writes a message built in a StringBuilder into the dialog without allocating a string, and opens it.</summary>
        public void ShowMessage(StringBuilder message)
        {
            messageLabel.SetText(message);
            Open();
        }

        /// <summary>Opens the dialog, finding its screen panel on first use.</summary>
        private void Open()
        {
            if (!screenPanel)
            {
                screenPanel = GetComponent<ScreenPanel>();
            }

            screenPanel.Show();
        }
    }
}

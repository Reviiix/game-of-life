using System;
using System.Text;
using GameOfLife.UI.Buttons;
using GameOfLife.UI.Screens;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI.Versus
{
    /// <summary>Asks the player to confirm starting a Versus match before they have placed every setup square, explaining how many they keep for the match.</summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class EarlyStartPanel : MonoBehaviour
    {
        private const string Headline = "START THE MATCH NOW?";
        private const string OneSquareLeftText = "YOU HAVE 1 SQUARE LEFT TO PLACE. IT WILL BE LOST IF YOU START NOW.";
        private const string SquaresLeftStart = "YOU HAVE ";
        private const string SquaresLeftEnd = " SQUARES LEFT TO PLACE. ";
        private const string SquaresKeptEnd = " WILL CARRY INTO THE MATCH AND THE REST WILL BE LOST.";

        [SerializeField] private TMP_Text messageLabel;
        [Tooltip("Starts the match; the dialog's own close button keeps the player placing.")]
        [SerializeField] private ActionButton startButton;

        private readonly StringBuilder messageBuilder = new();
        private ScreenPanel screenPanel;
        private bool isStartConfirmed;

        /// <summary>Raised when the player chooses to start the match now.</summary>
        public event Action StartConfirmed;

        /// <summary>Raised when the player closes the dialog to keep placing.</summary>
        public event Action Declined;

        /// <summary>Connects the start button and listens for the dialog closing.</summary>
        public void Initialise()
        {
            screenPanel = GetComponent<ScreenPanel>();
            startButton.Pressed += ConfirmStart;
            screenPanel.Hidden += ReportChoice;
        }

        /// <summary>Explains how many setup squares are left and how many carry into the match, then opens the dialog.</summary>
        public void ShowConfirmation(int squaresLeft, int squaresKept)
        {
            messageBuilder.Clear().Append(Headline).Append('\n');
            if (squaresLeft == 1)
            {
                messageBuilder.Append(OneSquareLeftText);
            }
            else
            {
                messageBuilder.Append(SquaresLeftStart).Append(squaresLeft).Append(SquaresLeftEnd).Append(squaresKept).Append(SquaresKeptEnd);
            }

            messageLabel.SetText(messageBuilder);
            isStartConfirmed = false;
            screenPanel.Show();
        }

        /// <summary>Stops listening when destroyed.</summary>
        private void OnDestroy()
        {
            if (startButton)
            {
                startButton.Pressed -= ConfirmStart;
            }

            if (screenPanel)
            {
                screenPanel.Hidden -= ReportChoice;
            }
        }

        /// <summary>Closes the dialog as a confirmed start.</summary>
        private void ConfirmStart()
        {
            isStartConfirmed = true;
            screenPanel.Hide();
        }

        /// <summary>Tells the owner which choice closed the dialog.</summary>
        private void ReportChoice()
        {
            if (isStartConfirmed)
            {
                StartConfirmed?.Invoke();
            }
            else
            {
                Declined?.Invoke();
            }
        }
    }
}

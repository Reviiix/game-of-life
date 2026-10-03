using GameOfLife.Configuration;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI.Versus
{
    /// <summary>Shows the Versus scores, time left and status line on its own canvas, so frequent updates never rebuild the rest of the game screen.</summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class VersusHud : MonoBehaviour
    {
        private const string PlayerSetupTurnFormat = "YOUR TURN - {0} LEFT";
        private const string OpponentSetupTurnText = "APP'S TURN";
        private const string PressPlayToStartText = "PRESS PLAY TO START";
        private const string GetReadyText = "GET READY";
        private const string MatchSquaresLeftFormat = "TAP TO PLACE - {0} LEFT";
        private const string PausedText = "PAUSED - PRESS PLAY";
        private const string MatchOverText = "MATCH OVER - PRESS PLAY";
        private const string PlayerScoreFormat = "YOU {0}";
        private const string OpponentScoreFormat = "APP {0}";
        private const int SecondsPerMinute = 60;

        [SerializeField] private TMP_Text playerScoreLabel;
        [SerializeField] private TMP_Text opponentScoreLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text statusLabel;
        [Tooltip("Shades the app's half of the board during setup; it sits in the Versus board area, over the grid.")]
        [SerializeField] private RectTransform opponentHalfShade;

        private Canvas hudCanvas;
        private string[] timerLabels;

        /// <summary>Colours the score labels, precomputes every timer label and starts hidden.</summary>
        public void Initialise(GameSettings settings)
        {
            hudCanvas = GetComponent<Canvas>();
            playerScoreLabel.color = (Color)settings.PlayerCellColour;
            opponentScoreLabel.color = (Color)settings.OpponentCellColour;
            timerLabels = BuildTimerLabels(settings.MaximumMatchSeconds);
            HideOpponentHalfShade();
            Hide();
        }

        /// <summary>Makes the HUD visible.</summary>
        public void Show()
        {
            hudCanvas.enabled = true;
        }

        /// <summary>Hides the HUD.</summary>
        public void Hide()
        {
            hudCanvas.enabled = false;
        }

        /// <summary>Shows how many living squares each side has.</summary>
        public void ShowScores(int playerScore, int opponentScore)
        {
            playerScoreLabel.SetText(PlayerScoreFormat, playerScore);
            opponentScoreLabel.SetText(OpponentScoreFormat, opponentScore);
        }

        /// <summary>Shows the match time left as minutes and seconds.</summary>
        public void ShowTimeRemaining(int seconds)
        {
            timerLabel.SetText(timerLabels[Mathf.Clamp(seconds, 0, timerLabels.Length - 1)]);
        }

        /// <summary>Tells the player it is their setup turn and how many setup squares they have left.</summary>
        public void ShowPlayerSetupTurn(int squaresLeft)
        {
            statusLabel.SetText(PlayerSetupTurnFormat, squaresLeft);
        }

        /// <summary>Tells the player the app is placing a setup square.</summary>
        public void ShowOpponentSetupTurn()
        {
            statusLabel.SetText(OpponentSetupTurnText);
        }

        /// <summary>Asks the player to press play to start the match.</summary>
        public void ShowPressPlayToStart()
        {
            statusLabel.SetText(PressPlayToStartText);
        }

        /// <summary>Shows that the countdown is running.</summary>
        public void ShowGetReady()
        {
            statusLabel.SetText(GetReadyText);
        }

        /// <summary>Shows how many squares the player can still place during the match.</summary>
        public void ShowMatchSquaresLeft(int squaresLeft)
        {
            statusLabel.SetText(MatchSquaresLeftFormat, squaresLeft);
        }

        /// <summary>Shows that the match is paused.</summary>
        public void ShowPaused()
        {
            statusLabel.SetText(PausedText);
        }

        /// <summary>Shows that the match has finished and play starts a new one.</summary>
        public void ShowMatchOver()
        {
            statusLabel.SetText(MatchOverText);
        }

        /// <summary>Shades the rows the app owns during setup, from its first row down to the bottom of the grid.</summary>
        public void ShowOpponentHalfShade(int rows, int opponentFirstRow)
        {
            var shadedFraction = (rows - opponentFirstRow) / (float)rows;
            opponentHalfShade.anchorMin = Vector2.zero;
            opponentHalfShade.anchorMax = new Vector2(1f, shadedFraction);
            opponentHalfShade.offsetMin = Vector2.zero;
            opponentHalfShade.offsetMax = Vector2.zero;
            opponentHalfShade.gameObject.SetActive(true);
        }

        /// <summary>Removes the setup shading.</summary>
        public void HideOpponentHalfShade()
        {
            opponentHalfShade.gameObject.SetActive(false);
        }

        /// <summary>Builds "m:ss" labels for every second a match can last, once, so the timer never builds strings during play.</summary>
        private static string[] BuildTimerLabels(int maximumSeconds)
        {
            var labels = new string[maximumSeconds + 1];
            var builder = new System.Text.StringBuilder();
            for (var seconds = 0; seconds <= maximumSeconds; seconds++)
            {
                builder.Clear();
                builder.Append(seconds / SecondsPerMinute).Append(':').Append((seconds % SecondsPerMinute).ToString("00"));
                labels[seconds] = builder.ToString();
            }

            return labels;
        }
    }
}

using GameOfLife.Configuration;
using GameOfLife.Theming;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI.Versus
{
    /// <summary>Shows the Versus scores, time left and status line on its own canvas, so frequent updates never rebuild the rest of the game screen; changing scores and the last seconds pop.</summary>
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
        private const int NoValueShown = -1;

        [SerializeField] private TMP_Text playerScoreLabel;
        [SerializeField] private TMP_Text opponentScoreLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text statusLabel;
        [Tooltip("Shades the app's half of the board during setup; it sits in the Versus board area, over the grid.")]
        [SerializeField] private RectTransform opponentHalfShade;
        [Tooltip("In the last this many seconds the timer turns to the danger colour and pops every second.")]
        [SerializeField, Min(0)] private int urgentSeconds = 10;

        private Canvas hudCanvas;
        private ThemedGraphic timerColour;
        private string[] timerLabels;
        private LabelPop playerScorePop;
        private LabelPop opponentScorePop;
        private LabelPop timerPop;
        private int shownPlayerScore = NoValueShown;
        private int shownOpponentScore = NoValueShown;
        private int shownSeconds = NoValueShown;

        /// <summary>Precomputes every timer label, builds the pop tweens and starts hidden.</summary>
        public void Initialise(GameSettings settings, Theme theme)
        {
            hudCanvas = GetComponent<Canvas>();
            timerColour = timerLabel.GetComponent<ThemedGraphic>();
            timerLabels = BuildTimerLabels(settings.MaximumMatchSeconds);
            playerScorePop = new LabelPop(playerScoreLabel.transform, theme);
            opponentScorePop = new LabelPop(opponentScoreLabel.transform, theme);
            timerPop = new LabelPop(timerLabel.transform, theme);
            HideOpponentHalfShade();
            Hide();
        }

        /// <summary>Removes the pop tweens with the HUD.</summary>
        private void OnDestroy()
        {
            playerScorePop?.Kill();
            opponentScorePop?.Kill();
            timerPop?.Kill();
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

        /// <summary>Shows how many living squares each side has, popping any score that changed.</summary>
        public void ShowScores(int playerScore, int opponentScore)
        {
            if (playerScore != shownPlayerScore)
            {
                shownPlayerScore = playerScore;
                playerScoreLabel.SetText(PlayerScoreFormat, playerScore);
                playerScorePop.Play();
            }

            if (opponentScore != shownOpponentScore)
            {
                shownOpponentScore = opponentScore;
                opponentScoreLabel.SetText(OpponentScoreFormat, opponentScore);
                opponentScorePop.Play();
            }
        }

        /// <summary>Shows the match time left as minutes and seconds; in the last seconds it turns to the danger colour and pops as each second passes.</summary>
        public void ShowTimeRemaining(int seconds)
        {
            if (seconds == shownSeconds)
            {
                return;
            }

            shownSeconds = seconds;
            timerLabel.SetText(timerLabels[Mathf.Clamp(seconds, 0, timerLabels.Length - 1)]);
            var isUrgent = seconds <= urgentSeconds;
            timerColour.SetColour(isUrgent ? ThemeColour.Danger : ThemeColour.Ink);
            if (isUrgent)
            {
                timerPop.Play();
            }
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

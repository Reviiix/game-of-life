using System.Text;
using GameOfLife.UI.Screens;
using GameOfLife.Versus;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI.Versus
{
    /// <summary>Fills the match result dialog with the winner and final scores, then shows it.</summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class MatchResultPanel : MonoBehaviour
    {
        private const string PlayerWinsHeadline = "YOU WIN!";
        private const string OpponentWinsHeadline = "THE APP WINS";
        private const string DrawHeadline = "IT'S A DRAW";
        private const string ScoreSeparator = " - ";

        [SerializeField] private TMP_Text messageLabel;

        private readonly StringBuilder messageBuilder = new();

        /// <summary>Writes the headline and "player - app" score, then opens the dialog.</summary>
        public void ShowResult(VersusOutcome outcome, int playerScore, int opponentScore)
        {
            messageBuilder.Clear()
                .Append(GetHeadline(outcome))
                .Append('\n')
                .Append(playerScore)
                .Append(ScoreSeparator)
                .Append(opponentScore);
            messageLabel.SetText(messageBuilder);
            GetComponent<ScreenPanel>().Show();
        }

        /// <summary>Returns the headline for an outcome.</summary>
        private static string GetHeadline(VersusOutcome outcome)
        {
            switch (outcome)
            {
                case VersusOutcome.PlayerWins:
                    return PlayerWinsHeadline;
                case VersusOutcome.OpponentWins:
                    return OpponentWinsHeadline;
                default:
                    return DrawHeadline;
            }
        }
    }
}

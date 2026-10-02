using System.Collections;
using GameOfLife.Configuration;
using GameOfLife.Core;
using TMPro;
using UnityEngine;

namespace GameOfLife.Gameplay
{
    /// <summary>Shows the countdown before the simulation starts, using labels built once at start-up.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CountdownDisplay : MonoBehaviour
    {
        private const string SingleSecondLabel = "GO!";

        private readonly WaitForSeconds oneSecond = new(1f);
        private TMP_Text label;
        private Color defaultLabelColour;
        private string[] secondLabels;

        /// <summary>Caches the label and precomputes the text for every second the countdown can show.</summary>
        public void Initialise(GameSettings settings)
        {
            label = GetComponent<TMP_Text>();
            defaultLabelColour = label.color;
            secondLabels = new string[settings.CountdownSeconds + 1];
            for (var second = 1; second < secondLabels.Length; second++)
            {
                secondLabels[second] = second.ToString();
            }

            Clear();
        }

        /// <summary>Counts down one label per second; a one-second countdown just shows GO! in the default colour.</summary>
        public IEnumerator PlayCountdown(int seconds, bool useRandomColours)
        {
            if (seconds == 1)
            {
                ShowLabel(SingleSecondLabel, useRandomColour: false);
                yield return oneSecond;
                Clear();
                yield break;
            }

            for (var second = seconds; second >= 1; second--)
            {
                ShowLabel(secondLabels[second], useRandomColours);
                yield return oneSecond;
            }

            Clear();
        }

        /// <summary>Hides the countdown text.</summary>
        public void Clear()
        {
            label.text = string.Empty;
        }

        /// <summary>Shows one countdown label in either its default or a random colour.</summary>
        private void ShowLabel(string text, bool useRandomColour)
        {
            label.text = text;
            label.color = useRandomColour ? RandomColour.CreateOpaque() : defaultLabelColour;
        }
    }
}

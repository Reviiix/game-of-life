using System.Collections;
using GameOfLife.Audio;
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
        private AudioManager audioManager;
        private Color defaultLabelColour;
        private string[] secondLabels;
        private int countdownSeconds;

        /// <summary>Caches the label and audio and precomputes the text for every second the countdown can show.</summary>
        public void Initialise(GameSettings settings, AudioManager audio)
        {
            label = GetComponent<TMP_Text>();
            audioManager = audio;
            defaultLabelColour = label.color;
            countdownSeconds = settings.CountdownSeconds;
            secondLabels = new string[countdownSeconds + 1];
            for (var second = 1; second < secondLabels.Length; second++)
            {
                secondLabels[second] = second.ToString();
            }

            Clear();
        }

        /// <summary>Counts down the GameSettings countdown length, one label per second; a one-second countdown just shows GO! in the default colour.</summary>
        public IEnumerator PlayCountdown(bool useRandomColours)
        {
            if (countdownSeconds == 1)
            {
                ShowLabel(SingleSecondLabel, useRandomColour: false);
                audioManager.Play(SoundEffect.CountdownGo);
                yield return oneSecond;
                Clear();
                yield break;
            }

            for (var second = countdownSeconds; second >= 1; second--)
            {
                ShowLabel(secondLabels[second], useRandomColours);
                audioManager.Play(SoundEffect.CountdownBeep);
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

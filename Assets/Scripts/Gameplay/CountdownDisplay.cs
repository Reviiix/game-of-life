using System.Collections;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Core;
using GameOfLife.Motion;
using GameOfLife.Theming;
using TMPro;
using UnityEngine;

namespace GameOfLife.Gameplay
{
    /// <summary>Shows the countdown before the simulation starts, popping each label in; the labels are built once at start-up.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CountdownDisplay : MonoBehaviour
    {
        private const string SingleSecondLabel = "GO!";
        private const float FadeInShareOfPop = 0.4f;

        private readonly WaitForSeconds oneSecond = new(1f);
        private TMP_Text label;
        private Transform labelTransform;
        private AudioManager audioManager;
        private ThemeService themeService;
        private ReplayableTween labelPop;
        private float popStartScale;
        private string[] secondLabels;
        private int countdownSeconds;

        /// <summary>Caches the label, audio and theme, builds the pop tween and precomputes the text for every second the countdown can show.</summary>
        public void Initialise(GameSettings settings, AudioManager audio, ThemeService theme)
        {
            label = GetComponent<TMP_Text>();
            labelTransform = label.transform;
            audioManager = audio;
            themeService = theme;
            popStartScale = theme.Theme.LabelPopScale;
            labelPop = new ReplayableTween(theme.Theme.LabelPop, ShowPopProgress);
            countdownSeconds = settings.CountdownSeconds;
            secondLabels = new string[countdownSeconds + 1];
            for (var second = 1; second < secondLabels.Length; second++)
            {
                secondLabels[second] = second.ToString();
            }

            Clear();
        }

        /// <summary>Counts down the GameSettings countdown length, one label per second; a one-second countdown just shows GO! in the ink colour.</summary>
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
            labelPop.Stop();
            labelTransform.localScale = Vector3.one;
            label.text = string.Empty;
        }

        /// <summary>Removes the pop tween with the label.</summary>
        private void OnDestroy()
        {
            labelPop?.Kill();
        }

        /// <summary>Shows one countdown label in the theme's ink or a random colour and pops it in.</summary>
        private void ShowLabel(string text, bool useRandomColour)
        {
            label.text = text;
            label.color = useRandomColour ? RandomColour.CreateOpaque() : themeService.Palette.Get(ThemeColour.Ink);
            labelPop.Play();
        }

        /// <summary>Shrinks the label from its pop scale to full size while it fades in.</summary>
        private void ShowPopProgress(float progress)
        {
            labelTransform.localScale = Vector3.one * Mathf.LerpUnclamped(popStartScale, 1f, progress);
            label.alpha = Mathf.Clamp01(progress / FadeInShareOfPop);
        }
    }
}

using System;
using GameOfLife.HowToPlay;
using GameOfLife.Theming;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI.HowToPlay
{
    /// <summary>One card of the how-to-play guide: its heading, its explanation and, when it has one, its looping demo board.</summary>
    public sealed class GuideStepView : MonoBehaviour
    {
        [SerializeField] private TMP_Text headingLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private DemoBoardView demoBoard;

        private bool hasDemo;

        /// <summary>Fills in the text and builds the demo board, or removes the board for a step with none or with a broken demo.</summary>
        public void Initialise(GuideStep step, ThemeService theme)
        {
            headingLabel.SetText(step.Heading);
            bodyLabel.SetText(step.Body);
            hasDemo = step.Demo != null && step.Demo.HasBoard && TryInitialiseDemo(step, theme);
            demoBoard.gameObject.SetActive(hasDemo);
        }

        /// <summary>Starts the demo board from its first frame, if there is one.</summary>
        public void PlayDemo()
        {
            if (hasDemo)
            {
                demoBoard.Play();
            }
        }

        /// <summary>Stops the demo board where it is, if there is one.</summary>
        public void StopDemo()
        {
            if (hasDemo)
            {
                demoBoard.Stop();
            }
        }

        /// <summary>Builds the demo board, or logs why its demo is broken so the card shows as text only rather than stopping start-up.</summary>
        private bool TryInitialiseDemo(GuideStep step, ThemeService theme)
        {
            try
            {
                demoBoard.gameObject.SetActive(true);
                demoBoard.Initialise(step.Demo, theme);
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"The how-to-play step '{step.Heading}' has a broken demo, so it shows as text only. {exception.Message}", this);
                return false;
            }
        }
    }
}

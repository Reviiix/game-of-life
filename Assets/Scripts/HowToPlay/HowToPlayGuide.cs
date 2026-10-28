using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameOfLife.HowToPlay
{
    /// <summary>The designer's how-to-play guide: the Classic and Versus steps, each with its text and looping demo board.</summary>
    [CreateAssetMenu(fileName = "HowToPlayGuide", menuName = "Game of Life/How To Play Guide")]
    public sealed class HowToPlayGuide : ScriptableObject
    {
        [SerializeField] private GuideStep[] classicSteps;
        [SerializeField] private GuideStep[] versusSteps;

        public IReadOnlyList<GuideStep> ClassicSteps => classicSteps ?? Array.Empty<GuideStep>();
        public IReadOnlyList<GuideStep> VersusSteps => versusSteps ?? Array.Empty<GuideStep>();

        /// <summary>Warns in the Editor about any demo whose boards are ragged, the wrong size or use an unknown symbol.</summary>
        private void OnValidate()
        {
            WarnAboutInvalidDemos(ClassicSteps);
            WarnAboutInvalidDemos(VersusSteps);
        }

        /// <summary>Builds every demo in the list and logs the ones that cannot be played.</summary>
        private void WarnAboutInvalidDemos(IReadOnlyList<GuideStep> steps)
        {
            foreach (var step in steps)
            {
                if (step?.Demo == null || !step.Demo.HasBoard)
                {
                    continue;
                }

                try
                {
                    step.Demo.BuildFrames();
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError($"{name}: the demo for '{step.Heading}' is invalid, so the game will show this step as text only. {exception.Message}", this);
                }
            }
        }
    }
}

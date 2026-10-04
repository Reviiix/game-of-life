using GameOfLife.Motion;
using GameOfLife.Theming;
using UnityEngine;

namespace GameOfLife.UI
{
    /// <summary>Makes a label swell and spring back to its normal size with the theme's label pop, reusing one tween that its owner kills when destroyed.</summary>
    public sealed class LabelPop
    {
        private readonly Transform labelTransform;
        private readonly ReplayableTween pop;
        private readonly float popScale;

        /// <summary>Builds the pop tween for a label.</summary>
        public LabelPop(Transform label, Theme theme)
        {
            labelTransform = label;
            popScale = theme.LabelPopScale;
            pop = new ReplayableTween(theme.LabelPop, ShowProgress);
        }

        /// <summary>Pops the label, restarting if it is already popping.</summary>
        public void Play()
        {
            pop.Play();
        }

        /// <summary>Removes the pop tween for good; call it when the owner is destroyed.</summary>
        public void Kill()
        {
            pop.Kill();
        }

        /// <summary>Scales the label from its pop scale back to full size.</summary>
        private void ShowProgress(float progress)
        {
            labelTransform.localScale = Vector3.one * Mathf.LerpUnclamped(popScale, 1f, progress);
        }
    }
}

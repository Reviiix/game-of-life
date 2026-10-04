using System;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace GameOfLife.Motion
{
    /// <summary>A duration and a DOTween ease, editable in the Inspector and shared by tweens and the board's cell animations.</summary>
    [Serializable]
    public struct EasedMotion
    {
        private const float DefaultOvershootOrAmplitude = 1.70158f;

        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private Ease ease;
        [Tooltip("Overshoot for Back eases or amplitude for Elastic eases; DOTween's default is 1.70158.")]
        [SerializeField] private float overshootOrAmplitude;
        [Tooltip("Period for Elastic eases; 0 lets DOTween pick one from the duration.")]
        [SerializeField, Min(0f)] private float period;

        public float Duration => duration;

        /// <summary>Creates a motion with DOTween's default overshoot unless one is given.</summary>
        public EasedMotion(float duration, Ease ease, float overshootOrAmplitude = DefaultOvershootOrAmplitude, float period = 0f)
        {
            this.duration = duration;
            this.ease = ease;
            this.overshootOrAmplitude = overshootOrAmplitude;
            this.period = period;
        }

        /// <summary>Returns the eased progress after the elapsed seconds of a run lasting the given seconds, using DOTween's own easing maths.</summary>
        public float Evaluate(float elapsed, float runDuration)
        {
            return EaseManager.Evaluate(ease, null, elapsed, runDuration, overshootOrAmplitude, period);
        }

        /// <summary>Gives a tween this motion's ease, overshoot and period.</summary>
        public TTween ApplyEaseTo<TTween>(TTween tween) where TTween : Tween
        {
            tween.SetEase(ease, overshootOrAmplitude, period);
            return tween;
        }
    }
}

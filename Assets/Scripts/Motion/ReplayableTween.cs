using System;
using DG.Tweening;

namespace GameOfLife.Motion
{
    /// <summary>A DOTween tween of progress from 0 to 1 that is built once and replayed, so repeated animations never allocate; the owner maps progress onto whatever it animates and kills it when destroyed.</summary>
    public sealed class ReplayableTween
    {
        private readonly Tweener tweener;
        private readonly Action<float> applyProgress;
        private float progress;

        /// <summary>Builds the paused tween; the completion callback runs only when a run finishes.</summary>
        public ReplayableTween(EasedMotion motion, Action<float> applyProgress, TweenCallback onComplete = null)
        {
            this.applyProgress = applyProgress;
            tweener = DOTween.To(GetProgress, SetProgress, 1f, motion.Duration)
                .SetAutoKill(false)
                .Pause();
            motion.ApplyEaseTo(tweener);
            if (onComplete != null)
            {
                tweener.OnComplete(onComplete);
            }
        }

        /// <summary>Plays from the start; a run already playing starts again.</summary>
        public void Play()
        {
            progress = 0f;
            tweener.Restart();
        }

        /// <summary>Stops wherever it is without completing, so the completion callback does not run.</summary>
        public void Stop()
        {
            tweener.Pause();
        }

        /// <summary>Removes the tween for good; call it when the owner is destroyed.</summary>
        public void Kill()
        {
            tweener.Kill();
        }

        /// <summary>Returns the current progress for DOTween.</summary>
        private float GetProgress()
        {
            return progress;
        }

        /// <summary>Stores progress from DOTween and passes it to the owner.</summary>
        private void SetProgress(float value)
        {
            progress = value;
            applyProgress(value);
        }
    }
}

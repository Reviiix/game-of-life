using System;
using DG.Tweening;

namespace GameOfLife.Motion
{
    /// <summary>Calls a method on every DOTween update until it returns false, then pauses; built once, so starting and stopping it never allocates.</summary>
    public sealed class FrameTicker
    {
        private const float LoopSeconds = 1f;

        private readonly Func<bool> tick;
        private readonly Sequence loop;

        /// <summary>Builds the paused, endlessly looping tween that drives the ticks.</summary>
        public FrameTicker(Func<bool> tick)
        {
            this.tick = tick;
            loop = DOTween.Sequence()
                .AppendInterval(LoopSeconds)
                .SetLoops(-1)
                .SetAutoKill(false)
                .OnUpdate(Tick)
                .Pause();
        }

        /// <summary>Starts ticking from the next update if it isn't already.</summary>
        public void Start()
        {
            if (!loop.IsPlaying())
            {
                loop.Play();
            }
        }

        /// <summary>Stops ticking until Start is called again.</summary>
        public void Stop()
        {
            loop.Pause();
        }

        /// <summary>Removes the tween for good; call it when the owner is destroyed.</summary>
        public void Kill()
        {
            loop.Kill();
        }

        /// <summary>Runs one tick and pauses once the owner reports there is nothing left to do.</summary>
        private void Tick()
        {
            if (!tick())
            {
                loop.Pause();
            }
        }
    }
}

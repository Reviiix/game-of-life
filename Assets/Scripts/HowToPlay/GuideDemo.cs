using System;
using System.Collections.Generic;
using GameOfLife.Simulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameOfLife.HowToPlay
{
    /// <summary>A looping mini board for one guide step: a start board, the steps played on it with the game's real rules, and how quickly they play.</summary>
    [Serializable]
    public sealed class GuideDemo
    {
        [Tooltip("Uses Versus colours: blue for you and red for the app. Otherwise Classic colours: ink, blue, red and yellow. Every demo absorbs after each placement and generation, as both modes do.")]
        [SerializeField, FormerlySerializedAs("versusRules")] private bool versusColours;
        [Tooltip("Shades the bottom half, the app's side during Versus setup.")]
        [SerializeField] private bool shadeAppHalf;
        [Tooltip("Seconds each step stays on screen.")]
        [SerializeField, Min(0.1f)] private float secondsPerStep = 0.9f;
        [Tooltip("Seconds the last step stays on screen before the demo starts again.")]
        [SerializeField, Min(0.1f)] private float secondsBeforeRepeat = 1.5f;
        [Tooltip("The board the demo starts from, top row first, '.' for empty. Versus: 'B' (or 'O') is your blue cell and 'R' the app's red. Classic: 'O' ink, 'B' blue, 'R' red and 'Y' yellow. Leave empty for a step with no board.")]
        [SerializeField] private string[] start;
        [SerializeField] private GuideDemoStep[] steps;

        public bool VersusColours => versusColours;
        public bool ShadeAppHalf => shadeAppHalf;
        public float SecondsPerStep => secondsPerStep;
        public float SecondsBeforeRepeat => secondsBeforeRepeat;
        public bool HasBoard => start != null && start.Length > 0;
        public int Rows => HasBoard ? start.Length : 0;
        public int Columns => HasBoard ? start[0].Length : 0;

        /// <summary>Plays the demo once through the engine and returns the start board followed by one frame per step, each as every cell's owner row by row.</summary>
        public CellOwner[][] BuildFrames()
        {
            var stepCount = steps?.Length ?? 0;
            var kinds = new DemoStepKind[stepCount];
            var cells = new IReadOnlyList<string>[stepCount];
            for (var step = 0; step < stepCount; step++)
            {
                kinds[step] = steps[step].Kind;
                cells[step] = steps[step].Cells;
            }

            return DemoFrameBuilder.Build(start ?? Array.Empty<string>(), kinds, cells, versusColours);
        }
    }
}

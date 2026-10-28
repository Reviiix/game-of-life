using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameOfLife.HowToPlay
{
    /// <summary>One step of a demo: place or remove cells drawn as rows of symbols, or run one generation.</summary>
    [Serializable]
    public sealed class GuideDemoStep
    {
        [SerializeField] private DemoStepKind kind;
        [Tooltip("For Place: rows the size of the board, top first, in the demo's colour symbols (see the start board); '-' removes a cell and '.' leaves it. Ignored for Generation.")]
        [SerializeField] private string[] cells;

        public DemoStepKind Kind => kind;
        public IReadOnlyList<string> Cells => cells ?? Array.Empty<string>();
    }
}

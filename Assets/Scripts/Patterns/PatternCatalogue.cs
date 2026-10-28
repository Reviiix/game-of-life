using System;
using UnityEngine;

namespace GameOfLife.Patterns
{
    /// <summary>The designer's list of catalogue patterns, converted once at start-up into immutable CellPatterns with every orientation ready.</summary>
    [CreateAssetMenu(fileName = "PatternCatalogue", menuName = "Game of Life/Pattern Catalogue")]
    public sealed class PatternCatalogue : ScriptableObject
    {
        [SerializeField] private PatternDefinition[] patterns;

        /// <summary>Builds every pattern in catalogue order.</summary>
        public CellPattern[] BuildPatterns()
        {
            var built = new CellPattern[patterns.Length];
            for (var index = 0; index < patterns.Length; index++)
            {
                built[index] = patterns[index].Build();
            }

            return built;
        }

        /// <summary>Warns in the Editor about any entry whose rows are ragged, lifeless or use an unknown symbol.</summary>
        private void OnValidate()
        {
            if (patterns == null)
            {
                return;
            }

            foreach (var definition in patterns)
            {
                try
                {
                    definition.Build();
                }
                catch (ArgumentException exception)
                {
                    Debug.LogWarning($"{name}: the pattern '{definition.DisplayName}' is invalid. {exception.Message}", this);
                }
            }
        }

        /// <summary>One designer entry: a name, a one-line description, a group and the shape as rows of '.' and 'O'.</summary>
        [Serializable]
        private sealed class PatternDefinition
        {
            [SerializeField] private string displayName;
            [SerializeField, TextArea] private string description;
            [SerializeField] private PatternCategory category;
            [Tooltip("Top row first; '.' is empty and 'O' is alive.")]
            [SerializeField] private string[] rows;

            public string DisplayName => displayName;

            /// <summary>Converts the entry into an immutable pattern.</summary>
            public CellPattern Build()
            {
                return CellPattern.FromRows(displayName, description, category, rows);
            }
        }
    }
}

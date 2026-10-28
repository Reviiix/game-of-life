using System;
using System.Collections.Generic;
using GameOfLife.Patterns;
using GameOfLife.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Patterns
{
    /// <summary>The catalogue dialog: a random placement row on its own card, then one row per pattern under its category card, built at start-up; greys the rows the board cannot take before showing, and reports the choice once the dialog has hidden.</summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class PatternsPanel : MonoBehaviour
    {
        private const int NoChoice = -1;
        private const int RandomChoice = -2;
        private const string RandomName = "Random cells";
        private const string RandomClassicDescription = "Scatters random cells, sometimes with whole patterns too.";
        private const string RandomVersusDescription = "Places all your squares that fit, sometimes as patterns.";
        private static readonly string[] RandomThumbnailRows = { "O..O.O", ".O....", "..O..O", "OOO.O.", "....OO", ".O...." };

        [SerializeField] private PatternRow rowPrefab;
        [Tooltip("The card at the top of the list that holds the random placement row.")]
        [SerializeField] private RectTransform randomCard;
        [Tooltip("One card per PatternCategory, in enum order.")]
        [SerializeField] private RectTransform[] categoryCards;
        [SerializeField] private ScrollRect scroll;

        private ScreenPanel screenPanel;
        private IReadOnlyList<CellPattern> patterns;
        private PatternRow[] rows;
        private PatternRow randomRow;
        private int chosenIndex = NoChoice;

        public bool IsOpen { get; private set; }

        public event Action<int> PatternChosen;
        public event Action RandomChosen;
        public event Action Closed;

        /// <summary>Instantiates the random placement row and a row for every pattern under its category card, with every thumbnail drawn once.</summary>
        public void Initialise(IReadOnlyList<CellPattern> cataloguePatterns)
        {
            screenPanel = GetComponent<ScreenPanel>();
            patterns = cataloguePatterns;
            randomRow = Instantiate(rowPrefab, randomCard);
            randomRow.Initialise(RandomChoice, RandomName, RandomClassicDescription, PatternThumbnails.Create(PatternOrientation.FromRows(RandomThumbnailRows)));
            randomRow.Chosen += ChooseRow;
            rows = new PatternRow[patterns.Count];
            for (var index = 0; index < patterns.Count; index++)
            {
                var pattern = patterns[index];
                var row = Instantiate(rowPrefab, categoryCards[(int)pattern.Category]);
                row.Initialise(index, pattern.Name, pattern.Description, PatternThumbnails.Create(pattern.Upright));
                row.Chosen += ChooseRow;
                rows[index] = row;
            }

            screenPanel.Hidden += ReportChoice;
        }

        /// <summary>Describes random placement for the mode and greys it when the band has no empty cell, greys every pattern row the bounds cannot take, scrolls to the top and shows the dialog.</summary>
        public void ShowFor(in PlacementBounds bounds, bool hasEmptyCell)
        {
            randomRow.SetDescription(bounds.HasSquareLimit ? RandomVersusDescription : RandomClassicDescription);
            randomRow.SetAvailable(hasEmptyCell ? PatternUnavailableReason.None : PatternUnavailableReason.NoEmptyCells);
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].SetAvailable(patterns[index].GetUnavailableReason(bounds));
            }

            chosenIndex = NoChoice;
            scroll.verticalNormalizedPosition = 1f;
            IsOpen = true;
            screenPanel.Show();
        }

        /// <summary>Hides the dialog without choosing anything.</summary>
        public void Close()
        {
            if (IsOpen)
            {
                screenPanel.Hide();
            }
        }

        /// <summary>Stops listening to the rows and the screen when destroyed.</summary>
        private void OnDestroy()
        {
            if (screenPanel)
            {
                screenPanel.Hidden -= ReportChoice;
            }

            if (randomRow)
            {
                randomRow.Chosen -= ChooseRow;
            }

            if (rows == null)
            {
                return;
            }

            foreach (var row in rows)
            {
                if (row)
                {
                    row.Chosen -= ChooseRow;
                }
            }
        }

        /// <summary>Remembers the tapped row and hides the dialog; the choice is reported once it has hidden.</summary>
        private void ChooseRow(int index)
        {
            chosenIndex = index;
            screenPanel.Hide();
        }

        /// <summary>Reports that the dialog closed, then the remembered choice, if any, now that the dialog is out of the way.</summary>
        private void ReportChoice()
        {
            IsOpen = false;
            var chosen = chosenIndex;
            chosenIndex = NoChoice;
            Closed?.Invoke();
            if (chosen == RandomChoice)
            {
                RandomChosen?.Invoke();
            }
            else if (chosen != NoChoice)
            {
                PatternChosen?.Invoke(chosen);
            }
        }
    }
}

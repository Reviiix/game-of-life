using System;
using System.Collections;
using GameOfLife.Configuration;
using GameOfLife.Core;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using GameOfLife.UI.Screens;
using UnityEngine;

namespace GameOfLife.Gameplay
{
    /// <summary>Runs the core loop: editing cells, the start countdown, timed generations, pausing and resetting.</summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private GridView gridView;
        [SerializeField] private GridTouchInput gridTouchInput;
        [SerializeField] private CountdownDisplay countdownDisplay;

        private GameSettings settings;
        private ScreenNavigator screenNavigator;
        private CellGrid cellGrid;
        private WaitForSeconds delayBetweenGenerations;
        private Coroutine activeRoutine;
        private bool randomColoursEnabled;

        public GamePhase Phase { get; private set; }

        public event Action<GamePhase> PhaseChanged;

        /// <summary>Allocates the simulation at its largest size, applies the starting settings and shows an empty grid.</summary>
        public void Initialise(GameSettings gameSettings, ScreenNavigator navigator)
        {
            settings = gameSettings;
            screenNavigator = navigator;
            cellGrid = new CellGrid(settings.MaximumRows, settings.MaximumColumns);
            gridView.Initialise(settings);
            countdownDisplay.Initialise(settings);
            randomColoursEnabled = settings.RandomColoursEnabledOnStart;
            SetEvolutionInterval(settings.StartingEvolutionInterval);
            ResizeGrid(settings.StartingRows, settings.StartingColumns);
            gridTouchInput.CellTapped += ToggleCell;
            SetPhase(GamePhase.Editing);
        }

        /// <summary>Starts the countdown when editing; otherwise stops the countdown or simulation and returns to editing.</summary>
        public void TogglePlayPause()
        {
            if (Phase == GamePhase.Editing)
            {
                activeRoutine = StartCoroutine(CountDownThenRunSimulation());
            }
            else
            {
                ReturnToEditing();
            }
        }

        /// <summary>Stops the simulation and kills every cell.</summary>
        public void ResetGame()
        {
            ReturnToEditing();
            ResizeGrid(cellGrid.Rows, cellGrid.Columns);
        }

        /// <summary>Changes the number of rows, which stops the simulation and clears the grid.</summary>
        public void SetRows(int rows)
        {
            ReturnToEditing();
            ResizeGrid(rows, cellGrid.Columns);
        }

        /// <summary>Changes the number of columns, which stops the simulation and clears the grid.</summary>
        public void SetColumns(int columns)
        {
            ReturnToEditing();
            ResizeGrid(cellGrid.Rows, columns);
        }

        /// <summary>Sets how many seconds pass between generations.</summary>
        public void SetEvolutionInterval(float seconds)
        {
            delayBetweenGenerations = new WaitForSeconds(seconds);
        }

        /// <summary>Turns random colours on or off for cells that come alive from now on.</summary>
        public void SetRandomColoursEnabled(bool enabled)
        {
            randomColoursEnabled = enabled;
        }

        /// <summary>Shows or hides the lines between cells.</summary>
        public void SetGridLinesVisible(bool visible)
        {
            gridView.SetGridLinesVisible(visible);
        }

        /// <summary>Stops listening for taps when this object is destroyed.</summary>
        private void OnDestroy()
        {
            if (gridTouchInput)
            {
                gridTouchInput.CellTapped -= ToggleCell;
            }
        }

        /// <summary>Plays the countdown, then advances one generation per interval until paused; shows a warning if no cells are alive.</summary>
        private IEnumerator CountDownThenRunSimulation()
        {
            SetPhase(GamePhase.CountingDown);
            yield return countdownDisplay.PlayCountdown(settings.CountdownSeconds, randomColoursEnabled);

            if (!cellGrid.HasAnyLivingCell())
            {
                activeRoutine = null;
                SetPhase(GamePhase.Editing);
                screenNavigator.InvalidGameDialog.Show();
                yield break;
            }

            SetPhase(GamePhase.Running);
            while (true)
            {
                AdvanceGeneration();
                yield return delayBetweenGenerations;
            }
        }

        /// <summary>Stops any countdown or simulation that is running and lets the player edit cells again.</summary>
        private void ReturnToEditing()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            countdownDisplay.Clear();
            SetPhase(GamePhase.Editing);
        }

        /// <summary>Resizes the simulation and the view together, leaving every cell dead.</summary>
        private void ResizeGrid(int rows, int columns)
        {
            cellGrid.Resize(rows, columns);
            gridView.Resize(rows, columns, settings.DeadCellColour);
        }

        /// <summary>Flips a tapped cell and redraws it.</summary>
        private void ToggleCell(int cellIndex)
        {
            cellGrid.ToggleCell(cellIndex);
            gridView.SetCellColour(cellIndex, GetCellColour(cellGrid.IsAlive(cellIndex)));
            gridView.ApplyCellColours();
        }

        /// <summary>Advances the simulation one step and redraws only the cells that changed.</summary>
        private void AdvanceGeneration()
        {
            cellGrid.AdvanceGeneration();
            var changedCellIndices = cellGrid.ChangedCellIndices;
            for (var changeIndex = 0; changeIndex < changedCellIndices.Count; changeIndex++)
            {
                var cellIndex = changedCellIndices[changeIndex];
                gridView.SetCellColour(cellIndex, GetCellColour(cellGrid.IsAlive(cellIndex)));
            }

            gridView.ApplyCellColours();
        }

        /// <summary>Returns the colour for a cell; living cells get a fresh random colour when random colours are on.</summary>
        private Color32 GetCellColour(bool isAlive)
        {
            if (!isAlive)
            {
                return settings.DeadCellColour;
            }

            return randomColoursEnabled ? RandomColour.CreateOpaque() : settings.AliveCellColour;
        }

        /// <summary>Updates the phase, allows taps on the grid except during the countdown, and notifies listeners.</summary>
        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            gridTouchInput.SetAcceptsTaps(phase != GamePhase.CountingDown);
            PhaseChanged?.Invoke(phase);
        }
    }
}

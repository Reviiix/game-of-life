using System;
using System.Collections.Generic;
using System.Text;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Gameplay.Modes;
using GameOfLife.Grid;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using GameOfLife.Theming;
using GameOfLife.UI.Patterns;
using UnityEngine;

namespace GameOfLife.Gameplay
{
    /// <summary>Lets the player choose a catalogue pattern and position it on the board, or place random cells at once: owns the positioning session and the random planner, swaps the bottom bar for the placement controls, drives the ghost from presses and drags, and places through the active mode's rules.</summary>
    public sealed class PatternPlacer : MonoBehaviour
    {
        private const string DragToMoveText = "DRAG TO MOVE - TICK TO PLACE";
        private const string MoveToEmptyCellsText = "MOVE TO EMPTY CELLS";
        private const string TapToPlaceStart = "TAP THE BOARD TO PLACE THE ";

        [SerializeField] private PatternCatalogue catalogue;
        [SerializeField] private PatternsPanel patternsPanel;
        [SerializeField] private PatternGhostView ghost;
        [SerializeField] private PlacementBar placementBar;
        [Tooltip("The normal bottom bar, hidden while a pattern is being positioned.")]
        [SerializeField] private GameObject buttonBar;

        private CellPattern[] patterns;
        private string[] tapHints;
        private PatternPlacementSession session;
        private RandomPlacementPlanner randomPlanner;
        private CellGrid board;
        private float randomBoardShare;
        private AudioManager audio;
        private GridTouchInput input;
        private GameModeBase mode;
        private GameModeBase catalogueMode;
        private string shownHint;
        private bool isCommitting;

        /// <summary>Every catalogue pattern, built once at start-up.</summary>
        public IReadOnlyList<CellPattern> Patterns => patterns;

        /// <summary>Whether a pattern is being positioned on the board, so taps belong to the ghost.</summary>
        public bool IsPositioning => session != null && session.IsActive;

        /// <summary>Whether the catalogue is open or a pattern is being positioned, so the game is covered.</summary>
        public bool IsBusy => IsPositioning || patternsPanel.IsOpen;

        /// <summary>Builds every pattern and its hint once, prepares the random planner, the ghost, the dialog and the controls, and starts listening to the board.</summary>
        public void Initialise(GameSettings settings, ThemeService theme, AudioManager audioManager, CellGrid grid, GridView boardView, GridTouchInput touchInput)
        {
            audio = audioManager;
            input = touchInput;
            board = grid;
            randomBoardShare = settings.RandomPlacementBoardShare;
            patterns = catalogue.BuildPatterns();
            var largestCellCount = 1;
            tapHints = new string[patterns.Length];
            var hintBuilder = new StringBuilder();
            for (var index = 0; index < patterns.Length; index++)
            {
                largestCellCount = Math.Max(largestCellCount, patterns[index].CellCount);
                hintBuilder.Clear().Append(TapToPlaceStart).Append(patterns[index].Name.ToUpperInvariant());
                tapHints[index] = hintBuilder.ToString();
            }

            session = new PatternPlacementSession(largestCellCount);
            randomPlanner = new RandomPlacementPlanner(grid.MaximumCellCount, patterns, new System.Random(), settings.RandomPlacementCellsPerPatternChance, settings.RandomPlacementPatternChance);
            ghost.Initialise(settings, theme, boardView);
            patternsPanel.Initialise(patterns);
            placementBar.Initialise();
            patternsPanel.Closed += ReportCatalogueClosed;
            patternsPanel.PatternChosen += BeginPositioning;
            patternsPanel.RandomChosen += PlaceRandomly;
            placementBar.RotatePressed += Rotate;
            placementBar.FlipPressed += Flip;
            placementBar.ConfirmPressed += Confirm;
            placementBar.CancelPressed += Cancel;
            input.CellPressed += OnCellPressed;
            input.CellDragged += OnCellDragged;
        }

        /// <summary>Opens the catalogue for the mode if it can take a pattern now, greying the patterns its bounds cannot fit, and random placement when there is no empty cell for it.</summary>
        public bool TryOpenCatalogue(GameModeBase rulesMode)
        {
            if (!rulesMode.TryGetPatternPlacementBounds(out var bounds))
            {
                return false;
            }

            mode = rulesMode;
            catalogueMode = rulesMode;
            patternsPanel.ShowFor(bounds, RandomPlacementPlanner.HasEmptyCell(board, bounds));
            rulesMode.OnPatternCatalogueOpened();
            return true;
        }

        /// <summary>Closes the catalogue or drops the pattern being positioned, placing nothing; does nothing when idle, or while a placement is being committed, which ends positioning itself.</summary>
        public void Cancel()
        {
            if (isCommitting)
            {
                return;
            }

            if (patternsPanel.IsOpen)
            {
                patternsPanel.Close();
            }

            if (!IsPositioning)
            {
                mode = null;
                return;
            }

            session.Cancel();
            End();
        }

        /// <summary>Stops listening to the dialog, the controls and the board when destroyed.</summary>
        private void OnDestroy()
        {
            if (patternsPanel)
            {
                patternsPanel.Closed -= ReportCatalogueClosed;
                patternsPanel.PatternChosen -= BeginPositioning;
                patternsPanel.RandomChosen -= PlaceRandomly;
            }

            if (placementBar)
            {
                placementBar.RotatePressed -= Rotate;
                placementBar.FlipPressed -= Flip;
                placementBar.ConfirmPressed -= Confirm;
                placementBar.CancelPressed -= Cancel;
            }

            if (input)
            {
                input.CellPressed -= OnCellPressed;
                input.CellDragged -= OnCellDragged;
            }
        }

        /// <summary>Tells the mode the catalogue opened for that it has closed, before any choice is acted on.</summary>
        private void ReportCatalogueClosed()
        {
            var closedFor = catalogueMode;
            catalogueMode = null;
            closedFor?.OnPatternCatalogueClosed();
        }

        /// <summary>Starts positioning the chosen pattern, swapping in the placement controls, unless the board can no longer take it.</summary>
        private void BeginPositioning(int patternIndex)
        {
            if (mode == null || !mode.TryGetPatternPlacementBounds(out var bounds) || !patterns[patternIndex].CanFit(bounds))
            {
                audio.Play(SoundEffect.Invalid);
                mode = null;
                return;
            }

            session.Begin(patterns[patternIndex], bounds, mode);
            buttonBar.SetActive(false);
            placementBar.Show();
            placementBar.SetRotateEnabled(session.CanRotate);
            placementBar.SetConfirmEnabled(false);
            ShowHint(tapHints[patternIndex]);
        }

        /// <summary>Places random cells as one move through the mode: every square left in Versus, or the configured share of a Classic board, sometimes including whole patterns; plays the invalid sound if nothing can be placed.</summary>
        private void PlaceRandomly()
        {
            var rulesMode = mode;
            mode = null;
            if (rulesMode == null || !rulesMode.TryGetPatternPlacementBounds(out var bounds))
            {
                audio.Play(SoundEffect.Invalid);
                return;
            }

            var cellBudget = bounds.HasSquareLimit ? bounds.SquaresAvailable : Mathf.Max(1, Mathf.RoundToInt(randomBoardShare * bounds.RowCount * bounds.Columns));
            if (randomPlanner.Plan(board, bounds, cellBudget) == 0 || !rulesMode.TryPlacePattern(randomPlanner.PlannedCells))
            {
                audio.Play(SoundEffect.Invalid);
            }
        }

        /// <summary>Drops the ghost on the pressed cell, or grabs it there.</summary>
        private void OnCellPressed(int cellIndex)
        {
            if (!IsPositioning)
            {
                return;
            }

            var appeared = !session.HasGhost;
            if (session.Press(cellIndex / session.Bounds.Columns, cellIndex % session.Bounds.Columns))
            {
                RefreshView(appeared);
            }
        }

        /// <summary>Carries the ghost with a drag.</summary>
        private void OnCellDragged(int cellIndex)
        {
            if (!IsPositioning)
            {
                return;
            }

            var appeared = !session.HasGhost;
            if (session.Drag(cellIndex / session.Bounds.Columns, cellIndex % session.Bounds.Columns))
            {
                RefreshView(appeared);
            }
        }

        /// <summary>Turns the pattern a quarter turn clockwise.</summary>
        private void Rotate()
        {
            if (session.RotateClockwise())
            {
                RefreshView(true);
            }
        }

        /// <summary>Mirrors the pattern left to right.</summary>
        private void Flip()
        {
            if (session.Mirror())
            {
                RefreshView(true);
            }
        }

        /// <summary>Places the ghost's cells through the mode, or plays the invalid sound if the mode refuses.</summary>
        private void Confirm()
        {
            if (!IsPositioning)
            {
                return;
            }

            isCommitting = true;
            var committed = session.TryCommit();
            isCommitting = false;
            if (committed)
            {
                End();
                return;
            }

            audio.Play(SoundEffect.Invalid);
            RefreshView(false);
        }

        /// <summary>Updates the ghost, the controls and the hint from the session; redraws the ghost when its shape changed, else only moves and recolours it.</summary>
        private void RefreshView(bool redraw)
        {
            placementBar.SetRotateEnabled(session.CanRotate);
            if (!session.HasGhost)
            {
                return;
            }

            if (redraw)
            {
                ghost.Show(session.Orientation, session.OriginRow, session.OriginColumn, session.IsValid, mode.PlacementPaint);
            }
            else
            {
                ghost.MoveTo(session.OriginRow, session.OriginColumn);
                ghost.SetValid(session.IsValid);
            }

            placementBar.SetConfirmEnabled(session.IsValid);
            ShowHint(session.IsValid ? DragToMoveText : MoveToEmptyCellsText);
        }

        /// <summary>Sends a hint to the mode only when it differs from the one already shown.</summary>
        private void ShowHint(string hint)
        {
            if (ReferenceEquals(hint, shownHint))
            {
                return;
            }

            shownHint = hint;
            mode.ShowPlacementHint(hint);
        }

        /// <summary>Hides the ghost and the controls, brings the bottom bar back and tells the mode positioning is over.</summary>
        private void End()
        {
            ghost.Hide();
            placementBar.Hide();
            buttonBar.SetActive(true);
            shownHint = null;
            var ended = mode;
            mode = null;
            ended?.OnPatternPositioningEnded();
        }
    }
}

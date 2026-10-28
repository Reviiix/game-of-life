using System;
using System.Collections.Generic;
using GameOfLife.Audio;
using GameOfLife.Grid;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Base for a way of playing on the shared board; the GameModeDirector keeps exactly one mode active at a time, and each mode says whether and where catalogue patterns may be placed.</summary>
    public abstract class GameModeBase : MonoBehaviour, IPatternPlacementRules
    {
        private WaitForSeconds delayBetweenGenerations;
        private float cachedEvolutionInterval = -1f;
        private FreshCellTracker freshCells;

        protected GameModeContext Context { get; private set; }

        public abstract GameModeType ModeType { get; }
        public bool IsSimulationActive { get; private set; }
        public bool IsPatternPlacementAvailable { get; private set; }

        /// <summary>How a pattern this mode would place right now is drawn, so its ghost previews that colour.</summary>
        public virtual CellPaint PlacementPaint => CellPaint.Player;

        public event Action<bool> SimulationActivityChanged;
        public event Action<bool> PatternPlacementAvailabilityChanged;

        /// <summary>Stores the shared systems, allocates the fresh-cell tracker for the largest board, then runs mode-specific set-up.</summary>
        public void Initialise(GameModeContext context)
        {
            Context = context;
            freshCells = new FreshCellTracker(context.Grid.MaximumCellCount, context.Settings.FreshCellGenerations);
            OnInitialised();
        }

        /// <summary>Takes over the board with a fresh game.</summary>
        public abstract void Enter();

        /// <summary>Stops everything this mode is running before another mode takes over.</summary>
        public abstract void Exit();

        /// <summary>Responds to the player tapping a cell.</summary>
        public abstract void OnCellTapped(int cellIndex);

        /// <summary>Responds to the play/pause button.</summary>
        public abstract void TogglePlayPause();

        /// <summary>Responds to the reset button by starting this mode again from scratch.</summary>
        public abstract void Restart();

        /// <summary>Called when the main menu or the settings page covers this mode.</summary>
        public virtual void OnMainMenuOpened()
        {
        }

        /// <summary>Called when the player leaves the main menu back into this mode.</summary>
        public virtual void OnMainMenuClosed()
        {
        }

        /// <summary>Called when settings opened from the game closes, so the mode resumes without treating it as a return from the menu.</summary>
        public virtual void OnSettingsClosed()
        {
        }

        /// <summary>Reports where and with how many cells a pattern may be placed right now; false when this mode cannot take one.</summary>
        public virtual bool TryGetPatternPlacementBounds(out PlacementBounds bounds)
        {
            bounds = default;
            return false;
        }

        /// <summary>Returns whether every listed cell may be placed as one move right now.</summary>
        public virtual bool CanPlacePattern(IReadOnlyList<int> cellIndices)
        {
            return false;
        }

        /// <summary>Places every listed cell as one move if allowed, repainting and sounding once.</summary>
        public virtual bool TryPlacePattern(IReadOnlyList<int> cellIndices)
        {
            return false;
        }

        /// <summary>Shows the pattern-positioning hint wherever this mode has room for it.</summary>
        public virtual void ShowPlacementHint(string hint)
        {
        }

        /// <summary>Called when positioning ends, placed or cancelled, so the mode can restore its status text.</summary>
        public virtual void OnPatternPositioningEnded()
        {
        }

        /// <summary>Called when the pattern catalogue opens over the board for this mode.</summary>
        public virtual void OnPatternCatalogueOpened()
        {
        }

        /// <summary>Called when the pattern catalogue closes, before any chosen pattern is positioned or placed.</summary>
        public virtual void OnPatternCatalogueClosed()
        {
        }

        /// <summary>Rebuilds the board after the player changes the grid size; by default this restarts the mode.</summary>
        public virtual void OnGridSizeChanged()
        {
            Restart();
        }

        /// <summary>Called when the player changes Settings that define a match, such as square pools or match length.</summary>
        public virtual void OnMatchRulesChanged()
        {
        }

        /// <summary>Runs once after Initialise for mode-specific set-up.</summary>
        protected virtual void OnInitialised()
        {
        }

        /// <summary>Stages how the board should draw a cell after its owner changed.</summary>
        protected abstract void PaintCell(int cellIndex);

        /// <summary>Returns a wait for the chosen evolution interval, rebuilt only when the interval has changed since the last call.</summary>
        protected WaitForSeconds GetDelayBetweenGenerations()
        {
            var interval = Context.Options.EvolutionInterval;
            if (!Mathf.Approximately(interval, cachedEvolutionInterval))
            {
                cachedEvolutionInterval = interval;
                delayBetweenGenerations = new WaitForSeconds(interval);
            }

            return delayBetweenGenerations;
        }

        /// <summary>Resizes the board to the current options, leaving every cell dead and none fresh.</summary>
        protected void ResetBoardToChosenSize()
        {
            freshCells.Clear();
            var grid = Context.Grid;
            grid.Resize(Context.Options.Rows, Context.Options.Columns);
            Context.GridView.Resize(grid.Rows, grid.Columns);
        }

        /// <summary>Redraws only the cells whose owner changed since the last repaint, animating each change, then forgets those changes; a changed cell is no longer fresh unless it was just placed and kept its placer's colour. Returns false and uploads nothing when nothing changed.</summary>
        protected bool RepaintChangedCells()
        {
            var grid = Context.Grid;
            var changedCellIndices = grid.ChangedCellIndices;
            if (changedCellIndices.Count == 0)
            {
                return false;
            }

            for (var changeIndex = 0; changeIndex < changedCellIndices.Count; changeIndex++)
            {
                var cellIndex = changedCellIndices[changeIndex];
                freshCells.ForgetIfOwnerChanged(cellIndex, grid.GetOwner(cellIndex));
                PaintCell(cellIndex);
            }

            Context.GridView.ApplyPaintedCells();
            grid.ClearChangedCells();
            return true;
        }

        /// <summary>Makes a cell just placed while the game runs fresh for the side that placed it, so it is drawn lighter for a few generations; call before RepaintChangedCells.</summary>
        protected void MarkPlacedCellFresh(int cellIndex, CellOwner placedOwner)
        {
            freshCells.Mark(cellIndex, placedOwner);
        }

        /// <summary>Returns whether a cell is still fresh from being placed while the game ran, so PaintCell draws it lighter.</summary>
        protected bool IsCellFresh(int cellIndex)
        {
            return freshCells.IsFresh(cellIndex, Context.Grid.GetOwner(cellIndex));
        }

        /// <summary>Counts a generation off every fresh cell and blends the ones whose fresh generations are over back to their own colour; call after the generation's repaint.</summary>
        protected void SettleFreshCells()
        {
            freshCells.AdvanceGeneration();
            var settledCellIndices = freshCells.SettledCellIndices;
            if (settledCellIndices.Count == 0)
            {
                return;
            }

            var gridView = Context.GridView;
            for (var position = 0; position < settledCellIndices.Count; position++)
            {
                gridView.SettleCell(settledCellIndices[position]);
            }

            gridView.ApplyPaintedCells();
        }

        /// <summary>Plays the placement note for a cell, pitched by its row so building a pattern plays a tune.</summary>
        protected void PlayPlacementSound(SoundEffect effect, int cellIndex)
        {
            var grid = Context.Grid;
            Context.Audio.PlayNoteForRow(effect, grid.RowOf(cellIndex), grid.Rows);
        }

        /// <summary>Updates whether the simulation is counting down or running, which drives the play/pause icon.</summary>
        protected void SetSimulationActive(bool active)
        {
            IsSimulationActive = active;
            SimulationActivityChanged?.Invoke(active);
        }

        /// <summary>Updates whether a pattern may be placed now, which drives the Patterns button; tells listeners only when it changes.</summary>
        protected void SetPatternPlacementAvailable(bool available)
        {
            if (IsPatternPlacementAvailable == available)
            {
                return;
            }

            IsPatternPlacementAvailable = available;
            PatternPlacementAvailabilityChanged?.Invoke(available);
        }
    }
}

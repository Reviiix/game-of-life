using System;
using GameOfLife.Audio;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Base for a way of playing on the shared board; the GameModeDirector keeps exactly one mode active at a time.</summary>
    public abstract class GameModeBase : MonoBehaviour
    {
        private WaitForSeconds delayBetweenGenerations;
        private float cachedEvolutionInterval = -1f;

        protected GameModeContext Context { get; private set; }

        public abstract GameModeType ModeType { get; }
        public bool IsSimulationActive { get; private set; }

        public event Action<bool> SimulationActivityChanged;

        /// <summary>Stores the shared systems, then runs mode-specific set-up.</summary>
        public void Initialise(GameModeContext context)
        {
            Context = context;
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

        /// <summary>Called when the main menu opens over this mode.</summary>
        public virtual void OnMainMenuOpened()
        {
        }

        /// <summary>Called when the player leaves the main menu back into this mode.</summary>
        public virtual void OnMainMenuClosed()
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

        /// <summary>Returns the colour to draw a cell in after its owner changed.</summary>
        protected abstract Color32 GetCellColour(int cellIndex);

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

        /// <summary>Resizes the board to the current options, leaving every cell dead.</summary>
        protected void ResetBoardToChosenSize()
        {
            var grid = Context.Grid;
            grid.Resize(Context.Options.Rows, Context.Options.Columns);
            Context.GridView.Resize(grid.Rows, grid.Columns, Context.Settings.DeadCellColour);
        }

        /// <summary>Redraws only the cells whose owner changed since the last repaint, then forgets those changes; returns false and uploads nothing when nothing changed.</summary>
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
                Context.GridView.SetCellColour(cellIndex, GetCellColour(cellIndex));
            }

            Context.GridView.ApplyCellColours();
            grid.ClearChangedCells();
            return true;
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
    }
}

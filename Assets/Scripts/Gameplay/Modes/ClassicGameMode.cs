using System.Collections;
using GameOfLife.Audio;
using GameOfLife.Core;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>The original game: tap cells, press play, and watch Conway's rules run until paused or reset.</summary>
    public sealed class ClassicGameMode : GameModeBase
    {
        private Coroutine activeRoutine;
        private ClassicGamePhase phase;

        public override GameModeType ModeType => GameModeType.Classic;

        /// <summary>Starts with an empty board in the editing phase.</summary>
        public override void Enter()
        {
            ResetBoardToChosenSize();
            SetPhase(ClassicGamePhase.Editing);
        }

        /// <summary>Stops any countdown or simulation.</summary>
        public override void Exit()
        {
            ReturnToEditing();
        }

        /// <summary>Flips the tapped cell, except during the countdown.</summary>
        public override void OnCellTapped(int cellIndex)
        {
            if (phase == ClassicGamePhase.CountingDown)
            {
                return;
            }

            Context.Grid.ToggleCell(cellIndex);
            RepaintChangedCells();
            if (Context.Grid.IsAlive(cellIndex))
            {
                PlayPlacementSound(SoundEffect.CellPlaced, cellIndex);
            }
            else
            {
                Context.Audio.Play(SoundEffect.CellRemoved);
            }
        }

        /// <summary>Starts the countdown when editing with at least one living cell, warns straight away if the board is empty, and otherwise stops and returns to editing.</summary>
        public override void TogglePlayPause()
        {
            if (phase != ClassicGamePhase.Editing)
            {
                ReturnToEditing();
            }
            else if (!Context.Grid.HasAnyLivingCell())
            {
                Context.Audio.Play(SoundEffect.Invalid);
                Context.Screens.InvalidGameDialog.Show();
            }
            else
            {
                activeRoutine = StartCoroutine(CountDownThenRunSimulation());
            }
        }

        /// <summary>Stops the simulation and kills every cell.</summary>
        public override void Restart()
        {
            ReturnToEditing();
            ResetBoardToChosenSize();
        }

        /// <summary>Draws living cells in the theme's ink, or a fresh random colour when random colours are on.</summary>
        protected override void PaintCell(int cellIndex)
        {
            var gridView = Context.GridView;
            if (!Context.Grid.IsAlive(cellIndex))
            {
                gridView.PaintCell(cellIndex, CellPaint.Empty);
            }
            else if (Context.Options.RandomColoursEnabled)
            {
                gridView.PaintCell(cellIndex, RandomColour.CreateOpaque());
            }
            else
            {
                gridView.PaintCell(cellIndex, CellPaint.Ink);
            }
        }

        /// <summary>Plays the countdown, then advances one generation per interval until paused.</summary>
        private IEnumerator CountDownThenRunSimulation()
        {
            SetPhase(ClassicGamePhase.CountingDown);
            yield return Context.Countdown.PlayCountdown(Context.Options.RandomColoursEnabled);
            SetPhase(ClassicGamePhase.Running);
            while (true)
            {
                Context.Grid.AdvanceGeneration();
                if (RepaintChangedCells())
                {
                    Context.Audio.Play(SoundEffect.GenerationTick);
                }

                yield return GetDelayBetweenGenerations();
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

            Context.Countdown.Clear();
            SetPhase(ClassicGamePhase.Editing);
        }

        /// <summary>Updates the phase and reports whether the simulation is active.</summary>
        private void SetPhase(ClassicGamePhase newPhase)
        {
            phase = newPhase;
            SetSimulationActive(newPhase != ClassicGamePhase.Editing);
        }
    }
}

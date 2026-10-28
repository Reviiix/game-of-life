using System.Collections;
using System.Collections.Generic;
using GameOfLife.Audio;
using GameOfLife.Core;
using GameOfLife.Grid;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using GameOfLife.UI.Buttons;
using GameOfLife.UI.Patterns;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>The original game: tap cells or drop catalogue patterns in any of four colours, press play, and watch Conway's rules run until paused or reset; colours that meet absorb each other as in Versus.</summary>
    public sealed class ClassicGameMode : GameModeBase
    {
        [Tooltip("Shows the pattern-positioning hint above the board; Versus shows its own in the HUD.")]
        [SerializeField] private PlacementHintLabel placementHint;
        [Tooltip("Steps through Classic's cell colours; shown only while Classic is the active mode.")]
        [SerializeField] private CellColourButton colourButton;

        private const string LastFormationKey = "MenuFormation.Last";
        private const int NoFormation = -1;

        private Coroutine activeRoutine;
        private ClassicGamePhase phase;
        private ClusterAbsorber absorber;
        private PulsatingFormationPicker formationPicker;
        private System.Random formationRandom;
        private int chosenColourPosition;

        public override GameModeType ModeType => GameModeType.Classic;

        /// <summary>Patterns are placed in the chosen colour, so the ghost previews it.</summary>
        public override CellPaint PlacementPaint => ClassicCellColours.ToPaint(ChosenOwner);

        private CellOwner ChosenOwner => ClassicCellColours.GetOwner(chosenColourPosition);

        /// <summary>Builds the hint label's animation, allocates the absorber for the largest board and shows the chosen colour on the colour button.</summary>
        protected override void OnInitialised()
        {
            placementHint.Initialise(Context.Theme.Theme);
            absorber = new ClusterAbsorber(Context.Grid.MaximumCellCount);
            formationRandom = new System.Random();
            colourButton.Pressed += ChooseNextColour;
            ShowChosenColour();
        }

        /// <summary>Shows the colour button and starts with an empty board in the editing phase.</summary>
        public override void Enter()
        {
            colourButton.gameObject.SetActive(true);
            ResetBoardToChosenSize();
            SetPhase(ClassicGamePhase.Editing);
        }

        /// <summary>Runs a pulsating catalogue pattern, centred and silent, behind the main menu at launch, a different one from the last launch when another fits; the player's first visit to Classic clears it.</summary>
        public void StartMenuShowcase(PulsatingFormationPicker picker)
        {
            formationPicker = picker;
            ShowFormation();
        }

        /// <summary>Clears the background formation when the player first chooses Classic, so their game starts on an empty board.</summary>
        public override void OnMainMenuClosed()
        {
            if (phase == ClassicGamePhase.Showcase)
            {
                ReturnToEditing();
                ResetBoardToChosenSize();
            }
        }

        /// <summary>Rebuilds the background formation for the new size while it is showing, otherwise restarts with an empty board.</summary>
        public override void OnGridSizeChanged()
        {
            if (phase == ClassicGamePhase.Showcase)
            {
                ShowFormation();
                return;
            }

            Restart();
        }

        /// <summary>Stops any countdown or simulation, clears absorption particles and hides the colour button, which only Classic uses.</summary>
        public override void Exit()
        {
            ReturnToEditing();
            Context.AbsorptionEffect.Clear();
            colourButton.gameObject.SetActive(false);
        }

        /// <summary>Brings an empty tapped cell to life in the chosen colour, fresh while the simulation runs, or removes a living one whatever its colour, except during the countdown or the background formation; any absorption it causes, or a removal that splits a cluster lets happen, happens at once.</summary>
        public override void OnCellTapped(int cellIndex)
        {
            if (phase == ClassicGamePhase.CountingDown || phase == ClassicGamePhase.Showcase)
            {
                return;
            }

            var grid = Context.Grid;
            var isRemoving = grid.IsAlive(cellIndex);
            grid.SetOwner(cellIndex, isRemoving ? CellOwner.None : ChosenOwner);
            if (!isRemoving && phase == ClassicGamePhase.Running)
            {
                MarkPlacedCellFresh(cellIndex, ChosenOwner);
            }

            var absorbedCellCount = ResolveAbsorptions();
            RepaintChangedCells();
            if (isRemoving)
            {
                Context.Audio.Play(SoundEffect.CellRemoved);
            }
            else
            {
                PlayPlacementSound(SoundEffect.CellPlaced, cellIndex);
            }

            CelebrateAbsorptions(absorbedCellCount);
        }

        /// <summary>Starts the countdown, after any ad break, when editing with at least one living cell, warns straight away if the board is empty, and otherwise stops and returns to editing.</summary>
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
                Context.AdBreaks.RunBeforeGame(StartRunIfStillEditing);
            }
        }

        /// <summary>Stops the simulation, kills every cell and clears absorption particles; the chosen colour stays.</summary>
        public override void Restart()
        {
            ReturnToEditing();
            Context.AbsorptionEffect.Clear();
            ResetBoardToChosenSize();
        }

        /// <summary>Patterns may go anywhere on the board whenever cells can be tapped, with no limit on cells.</summary>
        public override bool TryGetPatternPlacementBounds(out PlacementBounds bounds)
        {
            if (!CanTakePatterns(phase))
            {
                bounds = default;
                return false;
            }

            bounds = PlacementBounds.WholeBoard(Context.Grid.Rows, Context.Grid.Columns);
            return true;
        }

        /// <summary>Any position is fine while editing or running, because a pattern merges with living cells.</summary>
        public override bool CanPlacePattern(IReadOnlyList<int> cellIndices)
        {
            return CanTakePatterns(phase);
        }

        /// <summary>Brings every listed cell alive at once in the chosen colour, repainting any living cell it covers, then resolves absorptions, with one placement note; while the simulation runs, every cell that changed colour is fresh.</summary>
        public override bool TryPlacePattern(IReadOnlyList<int> cellIndices)
        {
            if (!CanTakePatterns(phase))
            {
                return false;
            }

            var grid = Context.Grid;
            var chosenOwner = ChosenOwner;
            var isRunning = phase == ClassicGamePhase.Running;
            for (var position = 0; position < cellIndices.Count; position++)
            {
                var cellIndex = cellIndices[position];
                if (isRunning && grid.GetOwner(cellIndex) != chosenOwner)
                {
                    MarkPlacedCellFresh(cellIndex, chosenOwner);
                }

                grid.SetOwner(cellIndex, chosenOwner);
            }

            var absorbedCellCount = ResolveAbsorptions();
            RepaintChangedCells();
            PlayPlacementSound(SoundEffect.CellPlaced, cellIndices[cellIndices.Count / 2]);
            CelebrateAbsorptions(absorbedCellCount);
            return true;
        }

        /// <summary>Shows the positioning hint above the board.</summary>
        public override void ShowPlacementHint(string hint)
        {
            placementHint.Show(hint);
        }

        /// <summary>Hides the hint once positioning ends.</summary>
        public override void OnPatternPositioningEnded()
        {
            placementHint.Hide();
        }

        /// <summary>Draws each living cell in its Classic colour, lighter while it is fresh; with random colours on, ink cells get a new random colour instead.</summary>
        protected override void PaintCell(int cellIndex)
        {
            var gridView = Context.GridView;
            var owner = Context.Grid.GetOwner(cellIndex);
            var fresh = IsCellFresh(cellIndex);
            if (owner == CellOwner.Player && Context.Options.RandomColoursEnabled)
            {
                gridView.PaintCell(cellIndex, RandomColour.CreateOpaque(), fresh);
            }
            else
            {
                gridView.PaintCell(cellIndex, ClassicCellColours.ToPaint(owner), fresh);
            }
        }

        /// <summary>Stops listening to the colour button when destroyed.</summary>
        private void OnDestroy()
        {
            if (colourButton)
            {
                colourButton.Pressed -= ChooseNextColour;
            }
        }

        /// <summary>Moves to the next Classic colour, wrapping back to ink, for the cells and patterns placed from now on.</summary>
        private void ChooseNextColour()
        {
            chosenColourPosition = ClassicCellColours.GetNextPosition(chosenColourPosition);
            ShowChosenColour();
        }

        /// <summary>Shows the chosen colour on the colour button.</summary>
        private void ShowChosenColour()
        {
            colourButton.ShowColour(ClassicCellColours.ToThemeColour(ChosenOwner));
        }

        /// <summary>Lets each bigger cluster turn the smaller touching clusters of other colours its own colour, as in Versus, and returns how many cells changed; a board of one colour is skipped cheaply.</summary>
        private int ResolveAbsorptions()
        {
            if (!Context.Grid.HasLivingCellsOfMoreThanOneOwner())
            {
                absorber.ForgetConvertedCells();
                return 0;
            }

            return absorber.ResolveAbsorptions(Context.Grid);
        }

        /// <summary>Bursts particles from the cells the last absorption converted and plays the absorb sound; call it after repainting so the particles take the cells' new colours.</summary>
        private void CelebrateAbsorptions(int absorbedCellCount)
        {
            if (absorbedCellCount == 0)
            {
                return;
            }

            Context.AbsorptionEffect.Play(absorber.ConvertedCellIndices);
            Context.Audio.Play(SoundEffect.Absorb);
        }

        /// <summary>Starts the countdown once an ad break ends, unless the board was cleared or a run started meanwhile.</summary>
        private void StartRunIfStillEditing()
        {
            if (phase == ClassicGamePhase.Editing && Context.Grid.HasAnyLivingCell())
            {
                activeRoutine = StartCoroutine(CountDownThenRunSimulation());
            }
        }

        /// <summary>Plays the countdown, then advances one generation per interval, resolving absorptions and settling fresh cells after each, until paused.</summary>
        private IEnumerator CountDownThenRunSimulation()
        {
            SetPhase(ClassicGamePhase.CountingDown);
            yield return Context.Countdown.PlayCountdown(Context.Options.RandomColoursEnabled);
            SetPhase(ClassicGamePhase.Running);
            while (true)
            {
                Context.Grid.AdvanceGeneration();
                var absorbedCellCount = ResolveAbsorptions();
                if (RepaintChangedCells())
                {
                    Context.Audio.Play(SoundEffect.GenerationTick);
                }

                SettleFreshCells();
                CelebrateAbsorptions(absorbedCellCount);
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

        /// <summary>Updates the phase and reports whether the simulation is active and whether patterns may be placed; the background formation is not a game, so it counts as neither.</summary>
        private void SetPhase(ClassicGamePhase newPhase)
        {
            phase = newPhase;
            SetSimulationActive(newPhase == ClassicGamePhase.CountingDown || newPhase == ClassicGamePhase.Running);
            SetPatternPlacementAvailable(CanTakePatterns(newPhase));
        }

        /// <summary>Returns whether patterns may be placed in a phase: while editing or running.</summary>
        private static bool CanTakePatterns(ClassicGamePhase checkedPhase)
        {
            return checkedPhase == ClassicGamePhase.Editing || checkedPhase == ClassicGamePhase.Running;
        }

        /// <summary>Starts a fresh board with a pulsating formation centred on it in the chosen colour and runs it silently; leaves the board empty when no Pulsate pattern keeps pulsating on it.</summary>
        private void ShowFormation()
        {
            ReturnToEditing();
            ResetBoardToChosenSize();
            var grid = Context.Grid;
            if (!formationPicker.TryPlan(grid.Rows, grid.Columns, Context.Settings.MenuFormationVerticalCentre, PlayerPrefs.GetInt(LastFormationKey, NoFormation), formationRandom))
            {
                return;
            }

            PlayerPrefs.SetInt(LastFormationKey, formationPicker.PlannedPatternIndex);
            PlayerPrefs.Save();
            var cells = formationPicker.PlannedCells;
            for (var position = 0; position < cells.Count; position++)
            {
                grid.SetOwner(cells[position], ChosenOwner);
            }

            RepaintChangedCells();
            SetPhase(ClassicGamePhase.Showcase);
            activeRoutine = StartCoroutine(RunFormation());
        }

        /// <summary>Advances the background formation one generation per interval, without sound, until Classic leaves the showcase.</summary>
        private IEnumerator RunFormation()
        {
            while (true)
            {
                yield return GetDelayBetweenGenerations();
                Context.Grid.AdvanceGeneration();
                RepaintChangedCells();
            }
        }
    }
}

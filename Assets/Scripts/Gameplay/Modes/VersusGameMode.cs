using System.Collections;
using GameOfLife.Audio;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using GameOfLife.UI.Versus;
using GameOfLife.Versus;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Player versus the app: take turns placing squares on your own half, then a timed match where larger clusters absorb smaller ones.</summary>
    public sealed class VersusGameMode : GameModeBase
    {
        private const float MinimumOpponentMoveJitter = 0.7f;
        private const float MaximumOpponentMoveJitter = 1.3f;
        private const float OpponentAbsorbPitch = 0.75f;

        [SerializeField] private VersusHud hud;
        [SerializeField] private MatchResultPanel matchResultPanel;
        [Tooltip("The area between the HUD and the button bar; the board moves here during Versus so every cell can be tapped.")]
        [SerializeField] private RectTransform boardArea;

        private VersusMatch match;
        private IOpponentStrategy easyOpponent;
        private IOpponentStrategy hardOpponent;
        private OpponentMoveSearch opponentMoveSearch;
        private System.Random timingRandom;
        private WaitForSeconds opponentTurnDelay;
        private Coroutine opponentSetupTurnRoutine;
        private Coroutine countdownRoutine;
        private Coroutine matchClockRoutine;
        private Coroutine opponentMatchMoveRoutine;
        private VersusGamePhase phase;
        private bool isMainMenuOpen;
        private float matchTimeRemaining;
        private float timeUntilNextGeneration;
        private float timeUntilOpponentMove;
        private int displayedSecondsRemaining;
        private BoardPlacement boardHomePlacement;

        public override GameModeType ModeType => GameModeType.Versus;

        private IOpponentStrategy ActiveOpponent => Context.Options.OpponentDifficulty == OpponentDifficulty.Hard ? hardOpponent : easyOpponent;

        /// <summary>Builds the match rules, both opponents and the background move search once; each opponent gets its own random source because it runs off the main thread.</summary>
        protected override void OnInitialised()
        {
            var settings = Context.Settings;
            var seeds = new System.Random();
            timingRandom = new System.Random(seeds.Next());
            match = new VersusMatch(Context.Grid, new ClusterAbsorber(Context.Grid.MaximumCellCount));
            easyOpponent = OpponentStrategyFactory.Create(OpponentDifficulty.Easy, new System.Random(seeds.Next()), settings.MaximumRows, settings.MaximumColumns);
            hardOpponent = OpponentStrategyFactory.Create(OpponentDifficulty.Hard, new System.Random(seeds.Next()), settings.MaximumRows, settings.MaximumColumns);
            opponentMoveSearch = new OpponentMoveSearch(settings.MaximumRows, settings.MaximumColumns);
            opponentTurnDelay = new WaitForSeconds(settings.OpponentTurnDelay);
            hud.Initialise(settings);
        }

        /// <summary>Moves the board between the HUD and the button bar, shows the HUD and starts a new match; the menu has always just closed.</summary>
        public override void Enter()
        {
            isMainMenuOpen = false;
            var board = Context.GridView.rectTransform;
            boardHomePlacement = BoardPlacement.Capture(board);
            board.SetParent(boardArea, false);
            board.SetAsFirstSibling();
            BoardPlacement.StretchToParent(board);
            hud.Show();
            Restart();
        }

        /// <summary>Stops the match, hides the HUD and puts the board back where classic mode expects it.</summary>
        public override void Exit()
        {
            StopAllMatchRoutines();
            phase = VersusGamePhase.Finished;
            SetSimulationActive(false);
            hud.HideOpponentHalfShade();
            hud.Hide();
            boardHomePlacement.Restore(Context.GridView.rectTransform);
        }

        /// <summary>Clears the board and starts a new match with the current Settings, beginning with setup.</summary>
        public override void Restart()
        {
            StopAllMatchRoutines();
            ResetBoardToChosenSize();
            var options = Context.Options;
            match.Begin(new VersusMatchRules(options.VersusSetupSquares, options.VersusMatchSquares));
            matchTimeRemaining = options.VersusMatchSeconds;
            phase = VersusGamePhase.Setup;
            SetSimulationActive(false);
            ContinueSetup();
        }

        /// <summary>Places the player's square during their setup turn or during the running match; ignores taps otherwise.</summary>
        public override void OnCellTapped(int cellIndex)
        {
            if (phase == VersusGamePhase.Setup)
            {
                if (match.SideToPlace == CellOwner.Player && match.TryPlace(CellOwner.Player, cellIndex))
                {
                    RepaintChangedCells();
                    PlayPlacementSounds(SoundEffect.CellPlaced, cellIndex);
                    ContinueSetup();
                }

                return;
            }

            if (phase == VersusGamePhase.Running && match.TryPlace(CellOwner.Player, cellIndex))
            {
                RepaintChangedCells();
                PlayPlacementSounds(SoundEffect.CellPlaced, cellIndex);
                RefreshHud();
                EndMatchIfDecided();
            }
        }

        /// <summary>Starts or resumes with a countdown, cancels a countdown, pauses, or starts a new match after one has finished.</summary>
        public override void TogglePlayPause()
        {
            switch (phase)
            {
                case VersusGamePhase.WaitingToStart:
                case VersusGamePhase.Paused:
                    BeginCountdown();
                    break;
                case VersusGamePhase.CountingDown:
                    CancelCountdown();
                    break;
                case VersusGamePhase.Running:
                    PauseMatch();
                    break;
                case VersusGamePhase.Finished:
                    Restart();
                    break;
            }
        }

        /// <summary>Pauses a running match or countdown while the main menu covers the board, so no time is lost.</summary>
        public override void OnMainMenuOpened()
        {
            isMainMenuOpen = true;
            if (phase == VersusGamePhase.Running)
            {
                PauseMatch();
            }
            else if (phase == VersusGamePhase.CountingDown)
            {
                CancelCountdown();
            }
        }

        /// <summary>Starts a new match after a finished one, or starts the first countdown if setup finished while the menu was open.</summary>
        public override void OnMainMenuClosed()
        {
            isMainMenuOpen = false;
            if (phase == VersusGamePhase.Finished)
            {
                Restart();
            }
            else if (phase == VersusGamePhase.WaitingToStart && match.Stage == VersusStage.Setup)
            {
                BeginCountdown();
            }
        }

        /// <summary>Restarts the match with the new rules if it hasn't started yet; a running match keeps its rules until the next one.</summary>
        public override void OnMatchRulesChanged()
        {
            if (match.Stage == VersusStage.Setup)
            {
                Restart();
            }
        }

        /// <summary>Draws each cell in its owner's colour.</summary>
        protected override Color32 GetCellColour(int cellIndex)
        {
            switch (Context.Grid.GetOwner(cellIndex))
            {
                case CellOwner.Player:
                    return Context.Settings.PlayerCellColour;
                case CellOwner.Opponent:
                    return Context.Settings.OpponentCellColour;
                default:
                    return Context.Settings.DeadCellColour;
            }
        }

        /// <summary>Moves setup on: lets the app take its turn, or starts the countdown once both sides have placed everything and the menu is closed.</summary>
        private void ContinueSetup()
        {
            if (match.IsSetupComplete)
            {
                hud.HideOpponentHalfShade();
                if (isMainMenuOpen)
                {
                    phase = VersusGamePhase.WaitingToStart;
                    RefreshHud();
                }
                else
                {
                    BeginCountdown();
                }

                return;
            }

            RefreshHud();
            if (match.SideToPlace == CellOwner.Opponent)
            {
                opponentSetupTurnRoutine = StartCoroutine(PlayOpponentSetupTurn());
            }
        }

        /// <summary>Waits briefly so the player sees the app's turn, then places the app's setup square.</summary>
        private IEnumerator PlayOpponentSetupTurn()
        {
            yield return opponentTurnDelay;
            yield return ChooseAndPlaceOpponentSquare();
            opponentSetupTurnRoutine = null;
            ContinueSetup();
        }

        /// <summary>Searches for the app's move in the background, then places it; if the board changed meanwhile and the cell is taken, an Easy move is used instead.</summary>
        private IEnumerator ChooseAndPlaceOpponentSquare()
        {
            yield return opponentMoveSearch.Search(Context.Grid, match.GetPlacementRegion(CellOwner.Opponent), ActiveOpponent);
            var placed = opponentMoveSearch.FoundCell && match.TryPlace(CellOwner.Opponent, opponentMoveSearch.ChosenCellIndex);
            if (!placed && easyOpponent.TryChooseCell(Context.Grid, match.GetPlacementRegion(CellOwner.Opponent), CellOwner.Opponent, out var fallbackCellIndex))
            {
                placed = match.TryPlace(CellOwner.Opponent, fallbackCellIndex);
            }

            if (placed)
            {
                RepaintChangedCells();
                Context.Audio.Play(SoundEffect.OpponentPlaced);
                PlayAbsorbSoundIfAnyCellsConverted();
            }
        }

        /// <summary>Plays the countdown before the match starts or resumes.</summary>
        private void BeginCountdown()
        {
            phase = VersusGamePhase.CountingDown;
            SetSimulationActive(true);
            RefreshHud();
            countdownRoutine = StartCoroutine(CountDownThenRunMatch());
        }

        /// <summary>Stops the countdown and waits for the player to press play again.</summary>
        private void CancelCountdown()
        {
            StopRoutine(ref countdownRoutine);
            Context.Countdown.Clear();
            phase = match.Stage == VersusStage.Match ? VersusGamePhase.Paused : VersusGamePhase.WaitingToStart;
            SetSimulationActive(false);
            RefreshHud();
        }

        /// <summary>Counts down, starts the match the first time, then hands over to the match clock.</summary>
        private IEnumerator CountDownThenRunMatch()
        {
            yield return Context.Countdown.PlayCountdown(false);
            countdownRoutine = null;
            if (match.Stage == VersusStage.Setup)
            {
                match.StartMatch();
                timeUntilNextGeneration = Context.Options.EvolutionInterval;
                ScheduleNextOpponentMove();
            }

            phase = VersusGamePhase.Running;
            RefreshHud();
            if (!EndMatchIfDecided())
            {
                matchClockRoutine = StartCoroutine(RunMatchClock());
            }
        }

        /// <summary>One clock for the whole match: counts the time down and triggers generations and the app's moves, keeping exact remaining times across pauses.</summary>
        private IEnumerator RunMatchClock()
        {
            while (true)
            {
                yield return null;
                var deltaTime = Time.deltaTime;
                matchTimeRemaining -= deltaTime;
                ShowTimeRemainingIfChanged();
                if (matchTimeRemaining <= 0f)
                {
                    matchClockRoutine = null;
                    EndMatch();
                    yield break;
                }

                timeUntilNextGeneration -= deltaTime;
                if (timeUntilNextGeneration <= 0f)
                {
                    timeUntilNextGeneration = Context.Options.EvolutionInterval;
                    AdvanceGeneration();
                    if (phase == VersusGamePhase.Finished)
                    {
                        yield break;
                    }
                }

                timeUntilOpponentMove -= deltaTime;
                var opponentCanMove = opponentMatchMoveRoutine == null && match.GetRemainingMatchSquares(CellOwner.Opponent) > 0;
                if (timeUntilOpponentMove <= 0f && opponentCanMove)
                {
                    opponentMatchMoveRoutine = StartCoroutine(MakeOpponentMatchMove());
                }
            }
        }

        /// <summary>Advances one generation, redraws changed cells and ends the match early if a side cannot come back.</summary>
        private void AdvanceGeneration()
        {
            match.AdvanceGeneration();
            if (RepaintChangedCells())
            {
                Context.Audio.Play(SoundEffect.GenerationTick);
            }

            PlayAbsorbSoundIfAnyCellsConverted();
            ShowScores();
            EndMatchIfDecided();
        }

        /// <summary>Places one of the app's match squares, then schedules the next.</summary>
        private IEnumerator MakeOpponentMatchMove()
        {
            yield return ChooseAndPlaceOpponentSquare();
            opponentMatchMoveRoutine = null;
            ShowScores();
            ScheduleNextOpponentMove();
            EndMatchIfDecided();
        }

        /// <summary>Spreads the app's remaining match squares across the remaining time, with some randomness so its timing isn't predictable.</summary>
        private void ScheduleNextOpponentMove()
        {
            var squaresLeft = match.GetRemainingMatchSquares(CellOwner.Opponent);
            if (squaresLeft == 0)
            {
                return;
            }

            var evenSpacing = matchTimeRemaining / (squaresLeft + 1f);
            var jitter = Mathf.Lerp(MinimumOpponentMoveJitter, MaximumOpponentMoveJitter, (float)timingRandom.NextDouble());
            timeUntilOpponentMove = evenSpacing * jitter;
        }

        /// <summary>Stops the match clock and any app move until the player presses play again; remaining times are kept.</summary>
        private void PauseMatch()
        {
            StopRunningRoutines();
            phase = VersusGamePhase.Paused;
            SetSimulationActive(false);
            RefreshHud();
        }

        /// <summary>Ends the match now if a side has no living squares and none left to place.</summary>
        private bool EndMatchIfDecided()
        {
            if (match.Stage != VersusStage.Match || !match.IsDecidedEarly())
            {
                return false;
            }

            EndMatch();
            return true;
        }

        /// <summary>Stops the match, works out the winner and shows the result dialog.</summary>
        private void EndMatch()
        {
            StopRunningRoutines();
            var outcome = match.Finish();
            phase = VersusGamePhase.Finished;
            SetSimulationActive(false);
            RefreshHud();
            Context.Audio.Play(GetResultSound(outcome));
            matchResultPanel.ShowResult(outcome, match.CountCells(CellOwner.Player), match.CountCells(CellOwner.Opponent));
        }

        /// <summary>Plays the player's placement note, then the absorb sound if the placement converted a cluster.</summary>
        private void PlayPlacementSounds(SoundEffect placementEffect, int cellIndex)
        {
            PlayPlacementSound(placementEffect, cellIndex);
            PlayAbsorbSoundIfAnyCellsConverted();
        }

        /// <summary>Plays the absorb sound after the last placement or generation converted cells: bright when the player gained them, lower when the app did.</summary>
        private void PlayAbsorbSoundIfAnyCellsConverted()
        {
            switch (match.LastAbsorbingSide)
            {
                case CellOwner.Player:
                    Context.Audio.Play(SoundEffect.Absorb);
                    break;
                case CellOwner.Opponent:
                    Context.Audio.Play(SoundEffect.Absorb, OpponentAbsorbPitch);
                    break;
            }
        }

        /// <summary>Returns the jingle for a match outcome.</summary>
        private static SoundEffect GetResultSound(VersusOutcome outcome)
        {
            switch (outcome)
            {
                case VersusOutcome.PlayerWins:
                    return SoundEffect.MatchWon;
                case VersusOutcome.OpponentWins:
                    return SoundEffect.MatchLost;
                default:
                    return SoundEffect.MatchDrawn;
            }
        }

        /// <summary>Updates the scores, timer, status line and setup shading to match the current state.</summary>
        private void RefreshHud()
        {
            ShowScores();
            displayedSecondsRemaining = SecondsToShow();
            hud.ShowTimeRemaining(displayedSecondsRemaining);
            switch (phase)
            {
                case VersusGamePhase.Setup:
                    hud.ShowOpponentHalfShade(Context.Grid.Rows, match.GetSetupRegion(CellOwner.Opponent).FirstRow);
                    if (match.SideToPlace == CellOwner.Player)
                    {
                        hud.ShowPlayerSetupTurn(match.GetRemainingSetupSquares(CellOwner.Player));
                    }
                    else
                    {
                        hud.ShowOpponentSetupTurn();
                    }

                    break;
                case VersusGamePhase.WaitingToStart:
                    hud.ShowPressPlayToStart();
                    break;
                case VersusGamePhase.CountingDown:
                    hud.ShowGetReady();
                    break;
                case VersusGamePhase.Running:
                    hud.ShowMatchSquaresLeft(match.GetRemainingMatchSquares(CellOwner.Player));
                    break;
                case VersusGamePhase.Paused:
                    hud.ShowPaused();
                    break;
                case VersusGamePhase.Finished:
                    hud.ShowMatchOver();
                    break;
            }
        }

        /// <summary>Shows how many living squares each side has.</summary>
        private void ShowScores()
        {
            hud.ShowScores(match.CountCells(CellOwner.Player), match.CountCells(CellOwner.Opponent));
        }

        /// <summary>Updates the timer only when the whole seconds shown would change.</summary>
        private void ShowTimeRemainingIfChanged()
        {
            var seconds = SecondsToShow();
            if (seconds != displayedSecondsRemaining)
            {
                displayedSecondsRemaining = seconds;
                hud.ShowTimeRemaining(seconds);
            }
        }

        /// <summary>Returns the match time left rounded up to whole seconds, never below zero.</summary>
        private int SecondsToShow()
        {
            return Mathf.Max(0, Mathf.CeilToInt(matchTimeRemaining));
        }

        /// <summary>Stops every coroutine this mode may be running, including setup turns and the countdown.</summary>
        private void StopAllMatchRoutines()
        {
            StopRoutine(ref opponentSetupTurnRoutine);
            StopRoutine(ref countdownRoutine);
            Context.Countdown.Clear();
            StopRunningRoutines();
        }

        /// <summary>Stops the coroutines that only run while the match is running.</summary>
        private void StopRunningRoutines()
        {
            StopRoutine(ref matchClockRoutine);
            StopRoutine(ref opponentMatchMoveRoutine);
        }

        /// <summary>Stops a coroutine if it is running and forgets it.</summary>
        private void StopRoutine(ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            routine = null;
        }
    }
}

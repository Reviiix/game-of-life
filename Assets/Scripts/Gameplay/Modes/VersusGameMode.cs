using System.Collections;
using System.Collections.Generic;
using System.Text;
using GameOfLife.Audio;
using GameOfLife.Grid;
using GameOfLife.Opponents;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using GameOfLife.UI.Screens;
using GameOfLife.UI.Versus;
using GameOfLife.Versus;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Player versus the app: both place squares on their own halves at the same time against the setup clock (play ends setup early once at least half are placed), then a timed match where larger clusters absorb smaller ones.</summary>
    public sealed class VersusGameMode : GameModeBase
    {
        private const float OpponentAbsorbPitch = 0.75f;
        private const string TooFewPlacedStart = "PLACE AT LEAST ";
        private const string TooFewPlacedMiddle = " OF YOUR ";
        private const string TooFewPlacedEnd = " SQUARES BEFORE THE MATCH CAN START.\nYOU HAVE PLACED ";

        [SerializeField] private VersusHud hud;
        [SerializeField] private MatchResultPanel matchResultPanel;
        [Tooltip("The area between the HUD and the button bar; the board moves here during Versus so every cell can be tapped.")]
        [SerializeField] private RectTransform boardArea;
        [Tooltip("Asks the player to confirm starting before every setup square is placed.")]
        [SerializeField] private EarlyStartPanel earlyStartPanel;
        [Tooltip("Explains that at least half the setup squares must be placed before the match can start.")]
        [SerializeField] private MessagePanel messagePanel;

        private VersusMatch match;
        private IOpponentStrategy[] opponentsByDifficulty;
        private OpponentMoveSearch opponentMoveSearch;
        private SetupSavingJudge setupSavingJudge;
        private readonly StringBuilder messageBuilder = new();
        private System.Random timingRandom;
        private Coroutine setupClockRoutine;
        private Coroutine opponentSetupMoveRoutine;
        private Coroutine countdownRoutine;
        private Coroutine matchClockRoutine;
        private Coroutine opponentMatchMoveRoutine;
        private VersusGamePhase phase;
        private bool isBoardCovered;
        private bool isConfirmingEarlyStart;
        private bool isCatalogueOpen;
        private float setupTimeRemaining;
        private float matchTimeRemaining;
        private float timeUntilNextGeneration;
        private float timeUntilOpponentMove;
        private int displayedSecondsRemaining;
        private EmptySideCountdown emptySideCountdown;
        private int displayedEmptySideSeconds;
        private BoardPlacement boardHomePlacement;
        private bool opponentWillSaveSquares;
        private bool opponentEndedSetupEarly;

        public override GameModeType ModeType => GameModeType.Versus;

        /// <summary>The setup clock stands still while the board is covered, the early-start dialog waits for an answer or the pattern catalogue is open, and so do the app's setup moves.</summary>
        private bool IsSetupClockHeld => isBoardCovered || isConfirmingEarlyStart || isCatalogueOpen;

        private IOpponentStrategy ActiveOpponent => opponentsByDifficulty[(int)Context.Options.OpponentDifficulty];

        /// <summary>The random Easy strategy, cheap enough to run on the main thread when a searched cell is no longer free.</summary>
        private IOpponentStrategy FallbackOpponent => opponentsByDifficulty[(int)OpponentDifficulty.Easy];

        /// <summary>Builds the match rules, one opponent per difficulty and the background move search once; each opponent gets its own random source because it runs off the main thread.</summary>
        protected override void OnInitialised()
        {
            var settings = Context.Settings;
            var seeds = new System.Random();
            timingRandom = new System.Random(seeds.Next());
            match = new VersusMatch(Context.Grid, new ClusterAbsorber(Context.Grid.MaximumCellCount));
            emptySideCountdown = new EmptySideCountdown(settings.EmptySideEndSeconds);
            var difficulties = (OpponentDifficulty[])System.Enum.GetValues(typeof(OpponentDifficulty));
            opponentsByDifficulty = new IOpponentStrategy[difficulties.Length];
            foreach (var difficulty in difficulties)
            {
                opponentsByDifficulty[(int)difficulty] = OpponentStrategyFactory.Create(difficulty, new System.Random(seeds.Next()), settings.MaximumRows, settings.MaximumColumns);
            }
            opponentMoveSearch = new OpponentMoveSearch(settings.MaximumRows, settings.MaximumColumns);
            setupSavingJudge = new SetupSavingJudge(settings.MaximumRows, settings.MaximumColumns, settings.OpponentSetupLookAheadGenerations);
            earlyStartPanel.Initialise();
            earlyStartPanel.StartConfirmed += EndPlayerSetupEarly;
            earlyStartPanel.Declined += ResumeSetup;
            hud.Initialise(settings, Context.Theme.Theme);
        }

        /// <summary>Moves the board between the HUD and the button bar, shows the HUD and starts a new match; the menu has always just closed.</summary>
        public override void Enter()
        {
            isBoardCovered = false;
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
            emptySideCountdown.Cancel();
            Context.AbsorptionEffect.Clear();
            phase = VersusGamePhase.Finished;
            SetSimulationActive(false);
            SetPatternPlacementAvailable(false);
            hud.HideOpponentHalfShade();
            hud.Hide();
            boardHomePlacement.Restore(Context.GridView.rectTransform);
        }

        /// <summary>Clears the board and starts a new match with the current Settings, beginning with the timed setup stage.</summary>
        public override void Restart()
        {
            StopAllMatchRoutines();
            emptySideCountdown.Cancel();
            isConfirmingEarlyStart = false;
            Context.AbsorptionEffect.Clear();
            ResetBoardToChosenSize();
            var options = Context.Options;
            match.Begin(new VersusMatchRules(options.VersusSetupSquares, options.VersusMatchSquares));
            opponentWillSaveSquares = false;
            opponentEndedSetupEarly = false;
            setupTimeRemaining = options.VersusSetupSeconds;
            matchTimeRemaining = options.VersusMatchSeconds;
            phase = VersusGamePhase.Setup;
            SetSimulationActive(false);
            ScheduleNextOpponentSetupMove();
            setupClockRoutine = StartCoroutine(RunSetupClock());
            ContinueSetup();
        }

        /// <summary>Places the player's square during setup or, fresh, during the running match; ignores taps otherwise.</summary>
        public override void OnCellTapped(int cellIndex)
        {
            if (phase == VersusGamePhase.Setup)
            {
                if (!isConfirmingEarlyStart && match.TryPlace(CellOwner.Player, cellIndex))
                {
                    RepaintChangedCells();
                    PlayPlacementSound(SoundEffect.CellPlaced, cellIndex);
                    CelebrateAbsorptions();
                    ContinueSetup();
                }

                return;
            }

            if (phase == VersusGamePhase.Running && match.TryPlace(CellOwner.Player, cellIndex))
            {
                MarkPlacedCellFresh(cellIndex, CellOwner.Player);
                RepaintChangedCells();
                PlayPlacementSound(SoundEffect.CellPlaced, cellIndex);
                CelebrateAbsorptions();
                RefreshEmptySideCountdown();
                RefreshHud();
            }
        }

        /// <summary>Asks to end setup early, starts or resumes with a countdown, cancels a countdown, pauses, or starts a new match after one has finished.</summary>
        public override void TogglePlayPause()
        {
            switch (phase)
            {
                case VersusGamePhase.Setup:
                    RequestEarlyStart();
                    break;
                case VersusGamePhase.WaitingToStart:
                case VersusGamePhase.Paused:
                    StartOrResumeMatch();
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

        /// <summary>Pauses a running match or countdown while the main menu or settings covers the board, so no time is lost, and hides the setup shading behind it.</summary>
        public override void OnMainMenuOpened()
        {
            isBoardCovered = true;
            hud.HideOpponentHalfShade();
            if (phase == VersusGamePhase.Running)
            {
                PauseMatch();
            }
            else if (phase == VersusGamePhase.CountingDown)
            {
                CancelCountdown();
            }
        }

        /// <summary>Starts a new match after a finished one, or resumes from under the menu: the first countdown if setup finished meanwhile, else the setup shading.</summary>
        public override void OnMainMenuClosed()
        {
            isBoardCovered = false;
            if (phase == VersusGamePhase.Finished)
            {
                Restart();
                return;
            }

            ResumeFromCover();
        }

        /// <summary>Resumes from under the settings page the same way as from the menu, except that a finished match stays finished.</summary>
        public override void OnSettingsClosed()
        {
            isBoardCovered = false;
            ResumeFromCover();
        }

        /// <summary>Patterns may be placed during setup while the player can still place, anywhere in their half, spending one setup square per cell, but not while the early-start dialog waits for an answer.</summary>
        public override bool TryGetPatternPlacementBounds(out PlacementBounds bounds)
        {
            if (!CanPlayerPlaceInSetup())
            {
                bounds = default;
                return false;
            }

            var region = match.GetSetupRegion(CellOwner.Player);
            bounds = new PlacementBounds(region.FirstRow, region.EndRowExclusive, Context.Grid.Rows, Context.Grid.Columns, match.GetRemainingSetupSquares(CellOwner.Player));
            return true;
        }

        /// <summary>Returns whether the match lets the player place every listed cell as one setup move.</summary>
        public override bool CanPlacePattern(IReadOnlyList<int> cellIndices)
        {
            return phase == VersusGamePhase.Setup && match.CanPlaceSetupPattern(CellOwner.Player, cellIndices);
        }

        /// <summary>Places the player's pattern as one setup move.</summary>
        public override bool TryPlacePattern(IReadOnlyList<int> cellIndices)
        {
            if (phase != VersusGamePhase.Setup || !match.TryPlaceSetupPattern(CellOwner.Player, cellIndices))
            {
                return false;
            }

            RepaintChangedCells();
            PlayPlacementSound(SoundEffect.CellPlaced, cellIndices[cellIndices.Count / 2]);
            CelebrateAbsorptions();
            ContinueSetup();
            return true;
        }

        /// <summary>Shows the positioning hint in the HUD's status line.</summary>
        public override void ShowPlacementHint(string hint)
        {
            hud.ShowPlacementHint(hint);
        }

        /// <summary>Puts the usual status line back once positioning ends.</summary>
        public override void OnPatternPositioningEnded()
        {
            RefreshHud();
        }

        /// <summary>Holds the setup clock while the catalogue covers the board.</summary>
        public override void OnPatternCatalogueOpened()
        {
            isCatalogueOpen = true;
        }

        /// <summary>Lets the setup clock run again once the catalogue closes.</summary>
        public override void OnPatternCatalogueClosed()
        {
            isCatalogueOpen = false;
        }

        /// <summary>Starts the first countdown if setup finished while the board was covered, or brings back the setup shading.</summary>
        private void ResumeFromCover()
        {
            if (phase == VersusGamePhase.WaitingToStart && match.Stage == VersusStage.Setup)
            {
                StartOrResumeMatch();
            }
            else if (phase == VersusGamePhase.Setup)
            {
                RefreshHud();
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

        /// <summary>Draws each cell in its owner's colour, lighter while it is fresh from being placed mid-match.</summary>
        protected override void PaintCell(int cellIndex)
        {
            Context.GridView.PaintCell(cellIndex, GetPaint(Context.Grid.GetOwner(cellIndex)), IsCellFresh(cellIndex));
        }

        /// <summary>Returns the board paint for a cell owner.</summary>
        private static CellPaint GetPaint(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player:
                    return CellPaint.Player;
                case CellOwner.Opponent:
                    return CellPaint.Opponent;
                default:
                    return CellPaint.Empty;
            }
        }

        /// <summary>Stops listening to the early-start dialog when destroyed.</summary>
        private void OnDestroy()
        {
            if (earlyStartPanel)
            {
                earlyStartPanel.StartConfirmed -= EndPlayerSetupEarly;
                earlyStartPanel.Declined -= ResumeSetup;
            }
        }

        /// <summary>Handles play during setup: warns if fewer than half the player's setup squares are placed, otherwise holds the setup clock and the app and asks the player to confirm starting early.</summary>
        private void RequestEarlyStart()
        {
            if (match.IsSetupComplete || match.GetRemainingSetupSquares(CellOwner.Player) == 0)
            {
                return;
            }

            if (!match.CanEndSetupEarly(CellOwner.Player))
            {
                Context.Audio.Play(SoundEffect.Invalid);
                messagePanel.ShowMessage(BuildTooFewPlacedMessage());
                return;
            }

            StopRoutine(ref opponentSetupMoveRoutine);
            isConfirmingEarlyStart = true;
            SetPatternPlacementAvailable(false);
            earlyStartPanel.ShowConfirmation(match.GetRemainingSetupSquares(CellOwner.Player), match.GetSetupSquaresKeptIfEndedEarly(CellOwner.Player));
        }

        /// <summary>Builds the message telling the player how many setup squares they must place before the match can start.</summary>
        private StringBuilder BuildTooFewPlacedMessage()
        {
            var placed = match.GetPlacedSetupSquares(CellOwner.Player);
            var total = placed + match.GetRemainingSetupSquares(CellOwner.Player);
            return messageBuilder.Clear()
                .Append(TooFewPlacedStart).Append(match.MinimumSetupSquaresBeforeEndingEarly)
                .Append(TooFewPlacedMiddle).Append(total)
                .Append(TooFewPlacedEnd).Append(placed).Append('.');
        }

        /// <summary>Ends the player's setup after they confirm: half their remaining squares carry into the match, and the app decides from the board whether to save its squares too or keep placing.</summary>
        private void EndPlayerSetupEarly()
        {
            if (phase == VersusGamePhase.Setup && match.CanEndSetupEarly(CellOwner.Player))
            {
                match.EndSetupEarly(CellOwner.Player);
                opponentWillSaveSquares = setupSavingJudge.ShouldSaveSquares(Context.Grid);
            }

            ResumeSetup();
        }

        /// <summary>Carries on with setup after the early-start dialog closes; the setup clock runs again once the dialog no longer holds it.</summary>
        private void ResumeSetup()
        {
            isConfirmingEarlyStart = false;
            if (phase == VersusGamePhase.Setup)
            {
                ContinueSetup();
            }
        }

        /// <summary>Moves setup on after any placement: lets the app save its squares if it decided to and may, hurries the app once the player is done, and starts the countdown once setup is complete.</summary>
        private void ContinueSetup()
        {
            if (opponentWillSaveSquares && match.CanEndSetupEarly(CellOwner.Opponent))
            {
                match.EndSetupEarly(CellOwner.Opponent);
                opponentEndedSetupEarly = true;
            }

            if (match.IsSetupComplete)
            {
                FinishSetup();
                return;
            }

            if (!match.CanStillPlaceInSetup(CellOwner.Player))
            {
                timeUntilOpponentMove = Mathf.Min(timeUntilOpponentMove, Context.Settings.OpponentTurnDelay);
            }

            RefreshHud();
        }

        /// <summary>Stops the setup clock and the app's setup moves, removes the setup shading and starts the countdown unless the board is covered.</summary>
        private void FinishSetup()
        {
            StopRoutine(ref setupClockRoutine);
            StopRoutine(ref opponentSetupMoveRoutine);
            hud.HideOpponentHalfShade();
            phase = VersusGamePhase.WaitingToStart;
            RefreshHud();
            if (!isBoardCovered)
            {
                StartOrResumeMatch();
            }
        }

        /// <summary>One clock for the setup stage: counts the setup time down unless held, ends setup when it runs out, and triggers the app's setup squares, keeping the exact time left across holds.</summary>
        private IEnumerator RunSetupClock()
        {
            while (true)
            {
                yield return null;
                if (IsSetupClockHeld)
                {
                    continue;
                }

                var deltaTime = Time.deltaTime;
                setupTimeRemaining -= deltaTime;
                ShowTimeRemainingIfChanged();
                if (setupTimeRemaining <= 0f)
                {
                    setupClockRoutine = null;
                    match.RunOutOfSetupTime();
                    FinishSetup();
                    yield break;
                }

                timeUntilOpponentMove -= deltaTime;
                if (timeUntilOpponentMove <= 0f && opponentSetupMoveRoutine == null && match.CanStillPlaceInSetup(CellOwner.Opponent))
                {
                    opponentSetupMoveRoutine = StartCoroutine(MakeOpponentSetupMove());
                }
            }
        }

        /// <summary>Places one of the app's setup squares, schedules the next and moves setup on.</summary>
        private IEnumerator MakeOpponentSetupMove()
        {
            yield return ChooseAndPlaceOpponentSquare();
            opponentSetupMoveRoutine = null;
            ScheduleNextOpponentSetupMove();
            ContinueSetup();
        }

        /// <summary>Spreads the app's remaining setup squares across the setup time left, or hurries them once the player has nothing left to place.</summary>
        private void ScheduleNextOpponentSetupMove()
        {
            timeUntilOpponentMove = OpponentMovePacing.SecondsUntilNextSetupMove(
                setupTimeRemaining,
                match.GetRemainingSetupSquares(CellOwner.Opponent),
                timingRandom.NextDouble(),
                !match.CanStillPlaceInSetup(CellOwner.Player),
                Context.Settings.OpponentTurnDelay);
        }

        /// <summary>Returns whether the player may place in setup now: setup is running, they can still place, and the early-start dialog isn't waiting for an answer.</summary>
        private bool CanPlayerPlaceInSetup()
        {
            return phase == VersusGamePhase.Setup && !isConfirmingEarlyStart && match.CanStillPlaceInSetup(CellOwner.Player);
        }

        /// <summary>Searches for the app's move in the background, then places it, fresh during the running match; if the board changed meanwhile and the cell is taken, an Easy move is used instead.</summary>
        private IEnumerator ChooseAndPlaceOpponentSquare()
        {
            yield return opponentMoveSearch.Search(Context.Grid, match.GetPlacementRegion(CellOwner.Opponent), ActiveOpponent);
            var placedCellIndex = opponentMoveSearch.ChosenCellIndex;
            var placed = opponentMoveSearch.FoundCell && match.TryPlace(CellOwner.Opponent, placedCellIndex);
            if (!placed && FallbackOpponent.TryChooseCell(Context.Grid, match.GetPlacementRegion(CellOwner.Opponent), CellOwner.Opponent, out var fallbackCellIndex))
            {
                placedCellIndex = fallbackCellIndex;
                placed = match.TryPlace(CellOwner.Opponent, placedCellIndex);
            }

            if (placed)
            {
                if (phase == VersusGamePhase.Running)
                {
                    MarkPlacedCellFresh(placedCellIndex, CellOwner.Opponent);
                }

                RepaintChangedCells();
                Context.Audio.Play(SoundEffect.OpponentPlaced);
                CelebrateAbsorptions();
            }
        }

        /// <summary>Starts the countdown, behind an ad break when it begins a new match rather than resuming one.</summary>
        private void StartOrResumeMatch()
        {
            if (match.Stage == VersusStage.Setup)
            {
                Context.AdBreaks.RunBeforeGame(BeginCountdownIfStillWaiting);
            }
            else
            {
                BeginCountdown();
            }
        }

        /// <summary>Starts the first countdown once an ad break ends, unless the menu opened or the match changed meanwhile.</summary>
        private void BeginCountdownIfStillWaiting()
        {
            if (phase == VersusGamePhase.WaitingToStart && match.Stage == VersusStage.Setup && !isBoardCovered)
            {
                BeginCountdown();
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
            RefreshEmptySideCountdown();
            RefreshHud();
            matchClockRoutine = StartCoroutine(RunMatchClock());
        }

        /// <summary>One clock for the whole match: counts the time down, ends the match when time or the wait after a side is emptied runs out, and triggers generations and the app's moves, keeping exact remaining times across pauses.</summary>
        private IEnumerator RunMatchClock()
        {
            while (true)
            {
                yield return null;
                var deltaTime = Time.deltaTime;
                matchTimeRemaining -= deltaTime;
                ShowTimeRemainingIfChanged();
                if (matchTimeRemaining <= 0f || emptySideCountdown.Tick(deltaTime))
                {
                    matchClockRoutine = null;
                    EndMatch();
                    yield break;
                }

                ShowEmptySideSecondsIfChanged();
                timeUntilNextGeneration -= deltaTime;
                if (timeUntilNextGeneration <= 0f)
                {
                    timeUntilNextGeneration = Context.Options.EvolutionInterval;
                    AdvanceGeneration();
                }

                timeUntilOpponentMove -= deltaTime;
                var opponentCanMove = opponentMatchMoveRoutine == null && match.GetRemainingMatchSquares(CellOwner.Opponent) > 0;
                if (timeUntilOpponentMove <= 0f && opponentCanMove)
                {
                    opponentMatchMoveRoutine = StartCoroutine(MakeOpponentMatchMove());
                }
            }
        }

        /// <summary>Advances one generation, redraws changed cells, settles fresh cells and starts or cancels the wait before the match ends when a side has no living squares.</summary>
        private void AdvanceGeneration()
        {
            match.AdvanceGeneration();
            if (RepaintChangedCells())
            {
                Context.Audio.Play(SoundEffect.GenerationTick);
            }

            SettleFreshCells();
            CelebrateAbsorptions();
            ShowScores();
            RefreshEmptySideCountdown();
        }

        /// <summary>Places one of the app's match squares, then schedules the next.</summary>
        private IEnumerator MakeOpponentMatchMove()
        {
            yield return ChooseAndPlaceOpponentSquare();
            opponentMatchMoveRoutine = null;
            ShowScores();
            ScheduleNextOpponentMove();
            RefreshEmptySideCountdown();
        }

        /// <summary>Spreads the app's remaining match squares across the remaining time, with some randomness so its timing isn't predictable.</summary>
        private void ScheduleNextOpponentMove()
        {
            var squaresLeft = match.GetRemainingMatchSquares(CellOwner.Opponent);
            if (squaresLeft == 0)
            {
                return;
            }

            timeUntilOpponentMove = OpponentMovePacing.SecondsUntilNextMove(matchTimeRemaining, squaresLeft, timingRandom.NextDouble());
        }

        /// <summary>Stops the match clock and any app move until the player presses play again; remaining times are kept.</summary>
        private void PauseMatch()
        {
            StopRunningRoutines();
            phase = VersusGamePhase.Paused;
            SetSimulationActive(false);
            RefreshHud();
        }

        /// <summary>Starts a side's wait before the match ends when it has just lost its last living square, keeps a running wait going, drops it once the side has squares again, and updates the status line to match.</summary>
        private void RefreshEmptySideCountdown()
        {
            var wasRunning = emptySideCountdown.IsRunning;
            emptySideCountdown.Refresh(match.HasNoCells(CellOwner.Player), match.HasNoCells(CellOwner.Opponent));
            if (phase == VersusGamePhase.Running && (wasRunning || emptySideCountdown.IsRunning))
            {
                ShowRunningStatus();
            }
        }

        /// <summary>Shows the running match's status: which side is wiped out and the seconds before the match ends while a side has no living squares, otherwise how many squares the player can still place.</summary>
        private void ShowRunningStatus()
        {
            if (!emptySideCountdown.IsRunning)
            {
                hud.ShowMatchSquaresLeft(match.GetRemainingMatchSquares(CellOwner.Player));
                return;
            }

            displayedEmptySideSeconds = SecondsUntilEmptySideEnd();
            hud.ShowSideWipedOut(emptySideCountdown.IsWaitingOnPlayer, emptySideCountdown.IsWaitingOnOpponent, displayedEmptySideSeconds);
        }

        /// <summary>Updates the wiped-out countdown in the status line only when the whole seconds shown would change.</summary>
        private void ShowEmptySideSecondsIfChanged()
        {
            if (emptySideCountdown.IsRunning && SecondsUntilEmptySideEnd() != displayedEmptySideSeconds)
            {
                ShowRunningStatus();
            }
        }

        /// <summary>Returns the whole seconds before a wiped-out side ends the match, never more than the match clock has left, since the match ends at whichever comes first.</summary>
        private int SecondsUntilEmptySideEnd()
        {
            return Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(emptySideCountdown.SecondsRemaining, matchTimeRemaining)));
        }

        /// <summary>Stops the match, works out the winner and shows the result dialog.</summary>
        private void EndMatch()
        {
            StopRunningRoutines();
            emptySideCountdown.Cancel();
            var outcome = match.Finish();
            phase = VersusGamePhase.Finished;
            SetSimulationActive(false);
            RefreshHud();
            Context.Audio.Play(GetResultSound(outcome));
            matchResultPanel.ShowResult(outcome, match.CountCells(CellOwner.Player), match.CountCells(CellOwner.Opponent));
        }

        /// <summary>After a placement or generation, bursts particles from every converted cell and plays the absorb sound: bright when the player gained cells, lower when the app did.</summary>
        private void CelebrateAbsorptions()
        {
            Context.AbsorptionEffect.Play(match.LastConvertedCellIndices);
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

        /// <summary>Updates the scores, timer, status line and setup shading to match the current state, and whether a pattern may be placed; the shading stays hidden while the board is covered.</summary>
        private void RefreshHud()
        {
            ShowScores();
            displayedSecondsRemaining = SecondsToShow();
            hud.ShowTimeRemaining(displayedSecondsRemaining);
            switch (phase)
            {
                case VersusGamePhase.Setup:
                    if (!isBoardCovered)
                    {
                        hud.ShowOpponentHalfShade(Context.Grid.Rows, match.GetSetupRegion(CellOwner.Opponent).FirstRow);
                    }
                    if (match.CanStillPlaceInSetup(CellOwner.Player))
                    {
                        hud.ShowPlayerSetup(match.GetRemainingSetupSquares(CellOwner.Player), match.CanEndSetupEarly(CellOwner.Player));
                    }
                    else
                    {
                        hud.ShowOpponentKeepsPlacing();
                    }

                    break;
                case VersusGamePhase.WaitingToStart:
                    hud.ShowPressPlayToStart();
                    break;
                case VersusGamePhase.CountingDown:
                    if (opponentEndedSetupEarly && match.Stage == VersusStage.Setup)
                    {
                        hud.ShowOpponentSavedSquares();
                    }
                    else
                    {
                        hud.ShowGetReady();
                    }

                    break;
                case VersusGamePhase.Running:
                    ShowRunningStatus();
                    break;
                case VersusGamePhase.Paused:
                    hud.ShowPaused();
                    break;
                case VersusGamePhase.Finished:
                    hud.ShowMatchOver();
                    break;
            }

            SetPatternPlacementAvailable(CanPlayerPlaceInSetup());
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

        /// <summary>Returns the setup time left during setup, otherwise the match time left, rounded up to whole seconds and never below zero.</summary>
        private int SecondsToShow()
        {
            var secondsLeft = phase == VersusGamePhase.Setup ? setupTimeRemaining : matchTimeRemaining;
            return Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
        }

        /// <summary>Stops every coroutine this mode may be running, including the setup clock, the app's setup moves and the countdown.</summary>
        private void StopAllMatchRoutines()
        {
            StopRoutine(ref setupClockRoutine);
            StopRoutine(ref opponentSetupMoveRoutine);
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

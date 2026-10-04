using System;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using GameOfLife.Versus;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks that VersusMatch enforces turns, halves and square pools, and decides matches by living cells.</summary>
    [TestFixture]
    public sealed class VersusMatchTests
    {
        private const char EmptySymbol = '.';
        private const char PlayerSymbol = 'P';
        private const char OpponentSymbol = 'O';

        /// <summary>Rules refuse negative square pools.</summary>
        [Test]
        public void Rules_WithNegativePools_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VersusMatchRules(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VersusMatchRules(0, -1));
        }

        /// <summary>A new match is finished until Begin is called, so nothing can be placed.</summary>
        [Test]
        public void NewMatch_BeforeBegin_AllowsNoPlacement()
        {
            var match = CreateMatch(4, 4, out var grid);

            Assert.That(match.Stage, Is.EqualTo(VersusStage.Finished));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.None));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0)), Is.False);
        }

        /// <summary>Begin starts setup with the Player to place and both sides' pools full.</summary>
        [Test]
        public void Begin_StartsSetupWithPlayerToPlaceAndFullPools()
        {
            var match = CreateMatch(6, 6, out _);

            match.Begin(new VersusMatchRules(5, 2));

            Assert.That(match.Stage, Is.EqualTo(VersusStage.Setup));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Player));
            Assert.That(match.IsSetupComplete, Is.False);
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Player), Is.EqualTo(5));
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Opponent), Is.EqualTo(5));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Player), Is.EqualTo(2));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(2));
        }

        /// <summary>Calling Begin again mid-match kills every cell, refills the pools and returns to setup.</summary>
        [Test]
        public void Begin_DuringAMatch_KillsEveryCellAndRefillsPools()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 2, out var grid);
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(5, 5));

            match.Begin(new VersusMatchRules(3, 2));

            Assert.That(grid.HasAnyLivingCell(), Is.False);
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Setup));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Player));
            Assert.That(match.IsSetupComplete, Is.False);
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Player), Is.EqualTo(3));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(() => match.StartMatch());
        }

        /// <summary>A match built before the grid is resized follows the grid's current size, so the setup halves and the placeable area track every resize.</summary>
        [Test]
        public void Begin_AfterTheGridIsResized_UsesTheCurrentSize()
        {
            var grid = new CellGrid(10, 10);
            var match = new VersusMatch(grid, new ClusterAbsorber(grid.MaximumCellCount));

            grid.Resize(6, 6);
            match.Begin(new VersusMatchRules(3, 0));
            Assert.That(match.IsSetupComplete, Is.False);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Player));
            Assert.That(match.GetSetupRegion(CellOwner.Player), Is.EqualTo(new PlacementRegion(0, 3)));
            Assert.That(match.GetSetupRegion(CellOwner.Opponent), Is.EqualTo(new PlacementRegion(3, 6)));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(2, 5)), Is.True);
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(3, 0)), Is.True);

            grid.Resize(5, 4);
            match.Begin(new VersusMatchRules(0, 1));
            Assert.That(match.GetSetupRegion(CellOwner.Player), Is.EqualTo(new PlacementRegion(0, 2)));
            Assert.That(match.GetSetupRegion(CellOwner.Opponent), Is.EqualTo(new PlacementRegion(2, 5)));
            match.StartMatch();

            Assert.That(grid.CellCount, Is.LessThan(grid.MaximumCellCount));
            Assert.That(match.GetPlacementRegion(CellOwner.Player), Is.EqualTo(new PlacementRegion(0, 5)));
            Assert.That(match.CanPlace(CellOwner.Player, grid.CellCount), Is.False);
            Assert.That(match.CanPlace(CellOwner.Opponent, grid.CellCount), Is.False);
            Assert.That(match.CanPlace(CellOwner.Player, grid.CellCount - 1), Is.True);
        }

        /// <summary>Setup turns alternate one square at a time, Player first.</summary>
        [Test]
        public void Setup_TurnsAlternateStartingWithPlayer()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(3, 0));

            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0)), Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(5, 0)), Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Player));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 2)), Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
        }

        /// <summary>A side cannot place during setup when it is not its turn.</summary>
        [Test]
        public void Setup_SideOutOfTurn_CannotPlace()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(3, 0));
            var opponentCell = grid.ToCellIndex(5, 0);

            Assert.That(match.CanPlace(CellOwner.Opponent, opponentCell), Is.False);
            Assert.That(match.TryPlace(CellOwner.Opponent, opponentCell), Is.False);
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0)), Is.True);
            Assert.That(match.CanPlace(CellOwner.Player, grid.ToCellIndex(0, 2)), Is.False);
            Assert.That(grid.IsAlive(opponentCell), Is.False);
        }

        /// <summary>The Player's setup half is the top Rows / 2 rows and the Opponent's is the rest, including the odd middle row.</summary>
        [Test]
        public void SetupRegions_SplitTheBoardAtHalfTheRows()
        {
            var match = CreateMatch(5, 4, out _);
            match.Begin(new VersusMatchRules(3, 0));

            var playerRegion = match.GetSetupRegion(CellOwner.Player);
            var opponentRegion = match.GetSetupRegion(CellOwner.Opponent);

            Assert.That(playerRegion.FirstRow, Is.EqualTo(0));
            Assert.That(playerRegion.EndRowExclusive, Is.EqualTo(2));
            Assert.That(opponentRegion.FirstRow, Is.EqualTo(2));
            Assert.That(opponentRegion.EndRowExclusive, Is.EqualTo(5));
            Assert.That(match.GetPlacementRegion(CellOwner.Player), Is.EqualTo(playerRegion));
            Assert.That(match.GetPlacementRegion(CellOwner.Opponent), Is.EqualTo(opponentRegion));
        }

        /// <summary>During setup each side can only place on its own half, and a refused placement keeps the turn and the pool.</summary>
        [Test]
        public void Setup_EachSideCanOnlyPlaceOnItsOwnHalf()
        {
            var match = CreateMatch(5, 4, out var grid);
            match.Begin(new VersusMatchRules(3, 0));

            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(2, 0)), Is.False);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Player));
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Player), Is.EqualTo(3));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 3)), Is.True);

            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(1, 0)), Is.False);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(2, 0)), Is.True);
            Assert.That(grid.IsAlive(grid.ToCellIndex(1, 0)), Is.False);
        }

        /// <summary>Occupied cells, cells outside the grid and the None owner are always refused.</summary>
        [Test]
        public void TryPlace_OnOccupiedOrOutOfGridCellsOrForNone_IsRefused()
        {
            var match = StartMatchOnEmptyBoard(4, 4, 3, out var grid);
            var occupiedCell = grid.ToCellIndex(0, 0);
            match.TryPlace(CellOwner.Player, occupiedCell);

            Assert.That(match.TryPlace(CellOwner.Opponent, occupiedCell), Is.False);
            Assert.That(match.TryPlace(CellOwner.Player, occupiedCell), Is.False);
            Assert.That(match.TryPlace(CellOwner.Player, -1), Is.False);
            Assert.That(match.TryPlace(CellOwner.Player, grid.CellCount), Is.False);
            Assert.That(match.CanPlace(CellOwner.None, grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(grid.GetOwner(occupiedCell), Is.EqualTo(CellOwner.Player));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Player), Is.EqualTo(2));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(3));
        }

        /// <summary>Asking for the pools, regions or cell count of None is a programming error.</summary>
        [Test]
        public void SideQueries_ForNone_Throw()
        {
            var match = CreateMatch(4, 4, out _);
            match.Begin(new VersusMatchRules(1, 1));

            Assert.Throws<ArgumentException>(() => match.GetRemainingSetupSquares(CellOwner.None));
            Assert.Throws<ArgumentException>(() => match.GetRemainingMatchSquares(CellOwner.None));
            Assert.Throws<ArgumentException>(() => match.GetSetupRegion(CellOwner.None));
            Assert.Throws<ArgumentException>(() => match.GetPlacementRegion(CellOwner.None));
            Assert.Throws<ArgumentException>(() => match.CountCells(CellOwner.None));
        }

        /// <summary>A setup placement spends one setup square from the placing side only and leaves the match pools alone.</summary>
        [Test]
        public void Setup_PlacementSpendsOnlyThePlacingSidesSetupSquare()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(3, 2));

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));

            Assert.That(match.GetRemainingSetupSquares(CellOwner.Player), Is.EqualTo(2));
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Opponent), Is.EqualTo(3));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Player), Is.EqualTo(2));
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(2));
        }

        /// <summary>Setup completes once both sides have placed every square, after which nobody can place until the match starts.</summary>
        [Test]
        public void Setup_CompletesWhenBothSidesHavePlacedEverySquare()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(2, 1));

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(5, 0));
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 2));
            Assert.That(match.IsSetupComplete, Is.False);
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(5, 2));

            Assert.That(match.IsSetupComplete, Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.None));
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Setup));
            Assert.That(match.CanPlace(CellOwner.Player, grid.ToCellIndex(1, 4)), Is.False);
            Assert.That(match.CanPlace(CellOwner.Opponent, grid.ToCellIndex(4, 4)), Is.False);
            Assert.That(match.CountCells(CellOwner.Player), Is.EqualTo(2));
            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(2));
        }

        /// <summary>A side whose half is full is skipped, so the other side places its remaining squares in a row.</summary>
        [Test]
        public void Setup_SideWithNoSpaceLeft_IsSkipped()
        {
            var match = CreateMatch(3, 2, out var grid);
            match.Begin(new VersusMatchRules(3, 0));

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(2, 0));
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 1));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(2, 1));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(1, 0));

            Assert.That(match.IsSetupComplete, Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.None));
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Player), Is.EqualTo(1));
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Opponent), Is.EqualTo(0));
        }

        /// <summary>With no setup squares neither side can place, so setup is complete straight away.</summary>
        [Test]
        public void Begin_WithNoSetupSquares_CompletesSetupImmediately()
        {
            var match = CreateMatch(6, 6, out _);

            match.Begin(new VersusMatchRules(0, 3));

            Assert.That(match.IsSetupComplete, Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.None));
        }

        /// <summary>On a one-row board the Player's half has no cells, so the Opponent takes the first turn and every turn after.</summary>
        [Test]
        public void Begin_WhenPlayerHalfHasNoCells_GivesEveryTurnToOpponent()
        {
            var match = CreateMatch(1, 6, out var grid);

            match.Begin(new VersusMatchRules(2, 0));

            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(0, 0)), Is.True);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Opponent));
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(0, 4)), Is.True);
            Assert.That(match.IsSetupComplete, Is.True);
        }

        /// <summary>A setup placement resolves absorptions, so a bigger cluster immediately converts the smaller enemy it now touches.</summary>
        [Test]
        public void TryPlace_DuringSetup_ResolvesAbsorptionsStraightAway()
        {
            var match = CreateMatch(4, 4, out var grid);
            match.Begin(new VersusMatchRules(2, 0));

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 1));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(2, 2));
            Assert.That(grid.GetOwner(grid.ToCellIndex(2, 2)), Is.EqualTo(CellOwner.Opponent));
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 2));

            Assert.That(grid.GetOwner(grid.ToCellIndex(2, 2)), Is.EqualTo(CellOwner.Player));
            Assert.That(match.CountCells(CellOwner.Player), Is.EqualTo(3));
            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(0));
        }

        /// <summary>A match placement also resolves absorptions straight away, before any generation runs.</summary>
        [Test]
        public void TryPlace_DuringTheMatch_ResolvesAbsorptionsStraightAway()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 3, out var grid);
            var opponentCell = grid.ToCellIndex(2, 2);

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 1));
            match.TryPlace(CellOwner.Opponent, opponentCell);
            Assert.That(grid.GetOwner(opponentCell), Is.EqualTo(CellOwner.Opponent));
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 2));

            Assert.That(grid.GetOwner(opponentCell), Is.EqualTo(CellOwner.Player));
            Assert.That(match.CountCells(CellOwner.Player), Is.EqualTo(3));
            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(0));
        }

        /// <summary>The match never clears the grid's change list, so every placed, converted and evolved cell stays listed once for the display to repaint.</summary>
        [Test]
        public void PlacementsAbsorptionsAndGenerations_AccumulateInTheChangeListOnce()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(2, 0));
            var playerFirstCell = grid.ToCellIndex(2, 1);
            var absorbedCell = grid.ToCellIndex(3, 2);
            var playerSecondCell = grid.ToCellIndex(2, 2);
            var opponentLoneCell = grid.ToCellIndex(5, 5);

            match.TryPlace(CellOwner.Player, playerFirstCell);
            match.TryPlace(CellOwner.Opponent, absorbedCell);
            match.TryPlace(CellOwner.Player, playerSecondCell);
            match.TryPlace(CellOwner.Opponent, opponentLoneCell);
            Assert.That(grid.GetOwner(absorbedCell), Is.EqualTo(CellOwner.Player));
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { playerFirstCell, absorbedCell, playerSecondCell, opponentLoneCell }));

            match.StartMatch();
            match.AdvanceGeneration();

            var bornCell = grid.ToCellIndex(3, 1);
            Assert.That(grid.GetOwner(bornCell), Is.EqualTo(CellOwner.Player));
            Assert.That(grid.IsAlive(opponentLoneCell), Is.False);
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { playerFirstCell, absorbedCell, playerSecondCell, opponentLoneCell, bornCell }));
        }

        /// <summary>The match cannot start while setup still has squares to place.</summary>
        [Test]
        public void StartMatch_BeforeSetupIsComplete_Throws()
        {
            var match = CreateMatch(6, 6, out _);
            match.Begin(new VersusMatchRules(1, 1));

            Assert.Throws<InvalidOperationException>(() => match.StartMatch());
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Setup));
        }

        /// <summary>The match can only start from a completed setup, so a running or finished match cannot be started again.</summary>
        [Test]
        public void StartMatch_DuringOrAfterTheMatch_Throws()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 2, out var grid);

            Assert.Throws<InvalidOperationException>(() => match.StartMatch());
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Match));

            match.Finish();

            Assert.Throws<InvalidOperationException>(() => match.StartMatch());
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Finished));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0)), Is.False);
        }

        /// <summary>Starting the match opens the whole grid to both sides with no turn order.</summary>
        [Test]
        public void StartMatch_OpensTheWholeGridToBothSides()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 3, out var grid);

            Assert.That(match.Stage, Is.EqualTo(VersusStage.Match));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.None));
            Assert.That(match.GetPlacementRegion(CellOwner.Player).EndRowExclusive, Is.EqualTo(6));
            Assert.That(match.GetPlacementRegion(CellOwner.Opponent).FirstRow, Is.EqualTo(0));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 1)), Is.True);
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(0, 0)), Is.True);
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(5, 5)), Is.True);
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(5, 0)), Is.True);
        }

        /// <summary>Match placements spend match squares, leave setup squares alone and stop once the pool is empty.</summary>
        [Test]
        public void Match_PlacementsSpendMatchSquaresUntilThePoolIsEmpty()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(0, 2));
            match.StartMatch();

            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0)), Is.True);
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Player), Is.EqualTo(1));
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 4)), Is.True);
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Player), Is.EqualTo(0));

            Assert.That(match.CanPlace(CellOwner.Player, grid.ToCellIndex(4, 4)), Is.False);
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(4, 4)), Is.False);
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(2));
            Assert.That(match.GetRemainingSetupSquares(CellOwner.Player), Is.EqualTo(0));
            Assert.That(match.CountCells(CellOwner.Player), Is.EqualTo(2));
        }

        /// <summary>A generation runs Conway's rules and then resolves the absorptions the new cells cause.</summary>
        [Test]
        public void AdvanceGeneration_RunsConwayThenResolvesAbsorptions()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 2, out var grid);
            PlacePattern(match, grid,
                "......",
                "......",
                "......",
                "..POP.",
                ".....O",
                "......");
            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(2));

            match.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "......",
                "......",
                "...P..",
                "...PP.",
                "...PP.",
                "......"
            }));
        }

        /// <summary>Generations only run during the match, so a lone cell survives an AdvanceGeneration call made during setup.</summary>
        [Test]
        public void AdvanceGeneration_OutsideTheMatch_DoesNothing()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(2, 0));
            var loneCell = grid.ToCellIndex(0, 0);
            match.TryPlace(CellOwner.Player, loneCell);

            match.AdvanceGeneration();

            Assert.That(grid.GetOwner(loneCell), Is.EqualTo(CellOwner.Player));
        }

        /// <summary>The match is never decided early during setup, even with an empty board.</summary>
        [Test]
        public void IsDecidedEarly_DuringSetup_IsFalse()
        {
            var match = CreateMatch(6, 6, out _);
            match.Begin(new VersusMatchRules(0, 0));

            Assert.That(match.IsDecidedEarly(), Is.False);
        }

        /// <summary>A side with no cells is still in the match while it has match squares, and out once it has neither.</summary>
        [Test]
        public void IsDecidedEarly_NeedsBothNoCellsAndNoMatchSquares()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 1, out var grid);
            Assert.That(match.IsDecidedEarly(), Is.False);

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));
            Assert.That(match.IsDecidedEarly(), Is.False);

            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(5, 5));
            Assert.That(match.IsDecidedEarly(), Is.False);

            match.AdvanceGeneration();
            Assert.That(match.IsDecidedEarly(), Is.True);
            Assert.That(match.DetermineOutcome(), Is.EqualTo(VersusOutcome.Draw));
        }

        /// <summary>The app's last match square must touch life, so next to the player's bigger cluster it is absorbed at once and the match is decided for the player.</summary>
        [Test]
        public void IsDecidedEarly_WhenTheAppSpendsItsLastSquareAndItIsAbsorbed_IsTrueAndThePlayerWins()
        {
            var match = CreateMatch(8, 8, out var grid);
            match.Begin(new VersusMatchRules(4, 1));
            int[] playerBlock =
            {
                FromOwnEdge(grid, CellOwner.Player, 1, 1), FromOwnEdge(grid, CellOwner.Player, 1, 2),
                FromOwnEdge(grid, CellOwner.Player, 2, 1), FromOwnEdge(grid, CellOwner.Player, 2, 2)
            };
            int[] opponentLoneCells =
            {
                FromOwnEdge(grid, CellOwner.Opponent, 2, 0), FromOwnEdge(grid, CellOwner.Opponent, 2, 3),
                FromOwnEdge(grid, CellOwner.Opponent, 2, 6), FromOwnEdge(grid, CellOwner.Opponent, 0, 1)
            };
            for (var turn = 0; turn < playerBlock.Length; turn++)
            {
                Assert.That(match.TryPlace(CellOwner.Player, playerBlock[turn]), Is.True);
                Assert.That(match.TryPlace(CellOwner.Opponent, opponentLoneCells[turn]), Is.True);
            }

            match.StartMatch();
            match.AdvanceGeneration();
            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(0));
            Assert.That(match.IsDecidedEarly(), Is.False);

            Assert.That(match.TryPlace(CellOwner.Opponent, FromOwnEdge(grid, CellOwner.Player, 1, 6)), Is.False);
            Assert.That(match.TryPlace(CellOwner.Opponent, FromOwnEdge(grid, CellOwner.Player, 1, 3)), Is.True);

            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(0));
            Assert.That(match.CountCells(CellOwner.Player), Is.EqualTo(5));
            Assert.That(match.IsDecidedEarly(), Is.True);
            Assert.That(match.DetermineOutcome(), Is.EqualTo(VersusOutcome.PlayerWins));
        }

        /// <summary>The match is decided as soon as the player has lost every cell and spent every match square, while the app lives on and wins; the player may spend the last square on a lone cell that then dies.</summary>
        [TestCase(CellOwner.Player, VersusOutcome.OpponentWins)]
        public void IsDecidedEarly_WhenOneSideRunsOut_IsTrueAndTheOtherSideWins(CellOwner sideThatRunsOut, VersusOutcome expectedOutcome)
        {
            var match = CreateMatch(8, 8, out var grid);
            match.Begin(new VersusMatchRules(4, 1));
            var survivingSide = OtherSide(sideThatRunsOut);
            int[] survivorBlock =
            {
                FromOwnEdge(grid, survivingSide, 1, 1), FromOwnEdge(grid, survivingSide, 1, 2),
                FromOwnEdge(grid, survivingSide, 2, 1), FromOwnEdge(grid, survivingSide, 2, 2)
            };
            int[] loneCells =
            {
                FromOwnEdge(grid, sideThatRunsOut, 2, 0), FromOwnEdge(grid, sideThatRunsOut, 2, 3),
                FromOwnEdge(grid, sideThatRunsOut, 2, 6), FromOwnEdge(grid, sideThatRunsOut, 0, 1)
            };
            var playerCells = survivingSide == CellOwner.Player ? survivorBlock : loneCells;
            var opponentCells = survivingSide == CellOwner.Player ? loneCells : survivorBlock;
            for (var turn = 0; turn < playerCells.Length; turn++)
            {
                Assert.That(match.TryPlace(CellOwner.Player, playerCells[turn]), Is.True);
                Assert.That(match.TryPlace(CellOwner.Opponent, opponentCells[turn]), Is.True);
            }

            match.StartMatch();
            match.AdvanceGeneration();
            Assert.That(match.CountCells(sideThatRunsOut), Is.EqualTo(0));
            Assert.That(match.IsDecidedEarly(), Is.False);

            Assert.That(match.TryPlace(sideThatRunsOut, FromOwnEdge(grid, sideThatRunsOut, 1, 6)), Is.True);
            Assert.That(match.IsDecidedEarly(), Is.False);
            match.AdvanceGeneration();

            Assert.That(match.IsDecidedEarly(), Is.True);
            Assert.That(match.CountCells(survivingSide), Is.EqualTo(4));
            Assert.That(match.DetermineOutcome(), Is.EqualTo(expectedOutcome));
        }

        /// <summary>The side with more living cells wins and equal counts are a draw.</summary>
        [TestCase(2, 1, VersusOutcome.PlayerWins)]
        [TestCase(1, 3, VersusOutcome.OpponentWins)]
        [TestCase(2, 2, VersusOutcome.Draw)]
        [TestCase(0, 0, VersusOutcome.Draw)]
        public void DetermineOutcome_ComparesLivingCellCounts(int playerCells, int opponentCells, VersusOutcome expectedOutcome)
        {
            var match = StartMatchOnEmptyBoard(6, 6, 3, out var grid);
            for (var placed = 0; placed < playerCells; placed++)
            {
                grid.SetOwner(grid.ToCellIndex(0, placed * 2), CellOwner.Player);
            }

            for (var placed = 0; placed < opponentCells; placed++)
            {
                grid.SetOwner(grid.ToCellIndex(4, placed * 2), CellOwner.Opponent);
            }

            Assert.That(match.CountCells(CellOwner.Player), Is.EqualTo(playerCells));
            Assert.That(match.CountCells(CellOwner.Opponent), Is.EqualTo(opponentCells));
            Assert.That(match.DetermineOutcome(), Is.EqualTo(expectedOutcome));
        }

        /// <summary>Finishing returns the outcome and stops all further placements and generations.</summary>
        [Test]
        public void Finish_ReturnsTheOutcomeAndStopsTheMatch()
        {
            var match = StartMatchOnEmptyBoard(6, 6, 2, out var grid);
            var loneCell = grid.ToCellIndex(0, 0);
            grid.SetOwner(loneCell, CellOwner.Opponent);

            var outcome = match.Finish();

            Assert.That(outcome, Is.EqualTo(VersusOutcome.OpponentWins));
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Finished));
            Assert.That(match.CanPlace(CellOwner.Player, grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(match.GetPlacementRegion(CellOwner.Player).HasPlaceableCell(grid), Is.False);
            Assert.That(match.IsDecidedEarly(), Is.False);
            match.AdvanceGeneration();
            Assert.That(grid.GetOwner(loneCell), Is.EqualTo(CellOwner.Opponent));
        }

        /// <summary>Finishing during setup also ends the turn order, so neither side is left with a turn or a placement.</summary>
        [Test]
        public void Finish_DuringSetup_LeavesNoSideToPlace()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(3, 0));
            var playerCell = grid.ToCellIndex(0, 0);
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.Player));

            var outcome = match.Finish();

            Assert.That(outcome, Is.EqualTo(VersusOutcome.Draw));
            Assert.That(match.Stage, Is.EqualTo(VersusStage.Finished));
            Assert.That(match.SideToPlace, Is.EqualTo(CellOwner.None));
            Assert.That(match.CanPlace(CellOwner.Player, playerCell), Is.False);
            Assert.That(match.TryPlace(CellOwner.Player, playerCell), Is.False);
            Assert.That(grid.IsAlive(playerCell), Is.False);
        }

        /// <summary>Creates a match over a fresh grid of the given size with its own absorber.</summary>
        /// <summary>During the match the app cannot place a square with no living neighbour, and the refused square stays in its pool.</summary>
        [Test]
        public void MatchPlacement_ByOpponentWithNoLivingNeighbour_IsRefused()
        {
            var match = CreateMatch(5, 5, out var grid);
            match.Begin(new VersusMatchRules(0, 2));
            match.StartMatch();
            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0)), Is.True);

            Assert.That(match.CanPlace(CellOwner.Opponent, grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(grid.IsAlive(grid.ToCellIndex(3, 3)), Is.False);
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(2));
        }

        /// <summary>During the match the app may place next to a living cell of either colour, diagonals included.</summary>
        [Test]
        public void MatchPlacement_ByOpponentNextToAnyLivingCell_IsAllowed()
        {
            var match = CreateMatch(5, 5, out var grid);
            match.Begin(new VersusMatchRules(0, 2));
            match.StartMatch();
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));

            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(1, 1)), Is.True);
            Assert.That(match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(2, 2)), Is.True);
            Assert.That(match.GetRemainingMatchSquares(CellOwner.Opponent), Is.EqualTo(0));
        }

        /// <summary>The player's own match squares may still go on any empty cell, because the player sees what they place.</summary>
        [Test]
        public void MatchPlacement_ByPlayerWithNoLivingNeighbour_IsAllowed()
        {
            var match = CreateMatch(5, 5, out var grid);
            match.Begin(new VersusMatchRules(0, 1));
            match.StartMatch();

            Assert.That(match.TryPlace(CellOwner.Player, grid.ToCellIndex(2, 2)), Is.True);
        }

        /// <summary>Only the app's match region requires a living neighbour; setup regions and the player's match region never do.</summary>
        [Test]
        public void PlacementRegions_RequireALivingNeighbourOnlyForTheOpponentDuringTheMatch()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(1, 1));
            Assert.That(match.GetPlacementRegion(CellOwner.Player).RequiresLivingNeighbour, Is.False);
            Assert.That(match.GetSetupRegion(CellOwner.Opponent).RequiresLivingNeighbour, Is.False);

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(5, 5));
            match.StartMatch();

            Assert.That(match.GetPlacementRegion(CellOwner.Opponent).RequiresLivingNeighbour, Is.True);
            Assert.That(match.GetPlacementRegion(CellOwner.Player).RequiresLivingNeighbour, Is.False);
        }

        /// <summary>With no living cell on the board the app has nowhere to place during the match.</summary>
        [Test]
        public void OpponentMatchRegion_OnAnEmptyBoard_HasNoPlaceableCell()
        {
            var match = CreateMatch(4, 4, out var grid);
            match.Begin(new VersusMatchRules(0, 1));
            match.StartMatch();

            Assert.That(match.GetPlacementRegion(CellOwner.Opponent).HasPlaceableCell(grid), Is.False);
        }

        /// <summary>LastAbsorbingSide names the side that gained cells when a placement converted a cluster.</summary>
        [Test]
        public void LastAbsorbingSide_AfterPlayerPlacementConvertsAppCell_IsPlayer()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(0, 3));
            match.StartMatch();
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 1));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(2, 2));
            Assert.That(match.LastAbsorbingSide, Is.EqualTo(CellOwner.None));

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(1, 2));

            Assert.That(match.LastAbsorbingSide, Is.EqualTo(CellOwner.Player));
            Assert.That(grid.GetOwner(grid.ToCellIndex(2, 2)), Is.EqualTo(CellOwner.Player));
        }

        /// <summary>LastAbsorbingSide is the app when its placement grows a cluster that converts a smaller player cluster, and resets when nothing converts.</summary>
        [Test]
        public void LastAbsorbingSide_AfterAppConvertsPlayerCell_IsOpponentThenResets()
        {
            var match = CreateMatch(6, 6, out var grid);
            match.Begin(new VersusMatchRules(0, 3));
            match.StartMatch();
            match.TryPlace(CellOwner.Player, grid.ToCellIndex(0, 0));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(1, 1));
            match.TryPlace(CellOwner.Opponent, grid.ToCellIndex(1, 2));

            Assert.That(match.LastAbsorbingSide, Is.EqualTo(CellOwner.Opponent));
            Assert.That(grid.GetOwner(grid.ToCellIndex(0, 0)), Is.EqualTo(CellOwner.Opponent));

            match.TryPlace(CellOwner.Player, grid.ToCellIndex(5, 5));
            Assert.That(match.LastAbsorbingSide, Is.EqualTo(CellOwner.None));
        }

        private static VersusMatch CreateMatch(int rows, int columns, out CellGrid grid)
        {
            grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            return new VersusMatch(grid, new ClusterAbsorber(grid.MaximumCellCount));
        }

        /// <summary>Creates a match with no setup squares and moves straight into the match stage with the given match squares each.</summary>
        private static VersusMatch StartMatchOnEmptyBoard(int rows, int columns, int matchSquaresEach, out CellGrid grid)
        {
            var match = CreateMatch(rows, columns, out grid);
            match.Begin(new VersusMatchRules(0, matchSquaresEach));
            match.StartMatch();
            return match;
        }

        /// <summary>Returns the cell at a row counted from the side's own board edge, so one pattern lands mirrored on either side's half.</summary>
        private static int FromOwnEdge(CellGrid grid, CellOwner side, int rowFromOwnEdge, int column)
        {
            var row = side == CellOwner.Player ? rowFromOwnEdge : grid.Rows - 1 - rowFromOwnEdge;
            return grid.ToCellIndex(row, column);
        }

        /// <summary>Returns the side playing against the given side.</summary>
        private static CellOwner OtherSide(CellOwner side)
        {
            return side == CellOwner.Player ? CellOwner.Opponent : CellOwner.Player;
        }

        /// <summary>Places every 'P' and 'O' of a text pattern as match squares, row by row, asserting each placement succeeds.</summary>
        private static void PlacePattern(VersusMatch match, CellGrid grid, params string[] rows)
        {
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    var owner = ParseOwner(rows[row][column]);
                    if (owner != CellOwner.None)
                    {
                        Assert.That(match.TryPlace(owner, grid.ToCellIndex(row, column)), Is.True);
                    }
                }
            }
        }

        /// <summary>Renders the grid as rows of text where '.' is empty, 'P' is Player and 'O' is Opponent.</summary>
        private static string[] DescribeGrid(CellGrid grid)
        {
            var rows = new string[grid.Rows];
            var symbols = new char[grid.Columns];
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var column = 0; column < grid.Columns; column++)
                {
                    symbols[column] = ToSymbol(grid.GetOwner(grid.ToCellIndex(row, column)));
                }

                rows[row] = new string(symbols);
            }

            return rows;
        }

        /// <summary>Converts a pattern character into the owner it stands for.</summary>
        private static CellOwner ParseOwner(char symbol)
        {
            switch (symbol)
            {
                case PlayerSymbol:
                    return CellOwner.Player;
                case OpponentSymbol:
                    return CellOwner.Opponent;
                case EmptySymbol:
                    return CellOwner.None;
                default:
                    throw new ArgumentException($"Unknown cell symbol '{symbol}'.", nameof(symbol));
            }
        }

        /// <summary>Converts an owner into its pattern character.</summary>
        private static char ToSymbol(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player:
                    return PlayerSymbol;
                case CellOwner.Opponent:
                    return OpponentSymbol;
                default:
                    return EmptySymbol;
            }
        }

        /// <summary>Beginning a match forgets cells converted before it, so no stale absorption effects play.</summary>
        [Test]
        public void Begin_ForgetsCellsConvertedBeforeIt()
        {
            var grid = new CellGrid(3, 6);
            grid.Resize(3, 6);
            var absorber = new ClusterAbsorber(grid.MaximumCellCount);
            var match = new VersusMatch(grid, absorber);
            foreach (var column in new[] { 1, 2, 3 })
            {
                grid.SetOwner(grid.ToCellIndex(1, column), CellOwner.Player);
            }

            grid.SetOwner(grid.ToCellIndex(1, 4), CellOwner.Opponent);
            absorber.ResolveAbsorptions(grid);
            Assume.That(match.LastConvertedCellIndices, Is.Not.Empty);

            match.Begin(new VersusMatchRules(1, 0));

            Assert.That(match.LastConvertedCellIndices, Is.Empty);
        }
    }
}

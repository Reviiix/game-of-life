using System;
using System.Collections.Generic;
using GameOfLife.Opponents;
using GameOfLife.Simulation;

namespace GameOfLife.Versus
{
    /// <summary>Applies the rules of one versus match: a setup stage where both sides place on their own halves at the same time, singly or as whole patterns, until both are done or the mode runs out of setup time, then free placement between generations.</summary>
    public sealed class VersusMatch
    {
        private const int SideCount = 2;
        private const int PlayerSlot = 0;
        private const int OpponentSlot = 1;

        private static readonly PlacementRegion RegionWithNoRows = new PlacementRegion(0, 0);

        private readonly CellGrid grid;
        private readonly ClusterAbsorber absorber;
        private readonly int[] remainingSetupSquares;
        private readonly int[] remainingMatchSquares;
        private int setupSquaresEach;

        public VersusStage Stage { get; private set; }
        public bool IsSetupComplete { get; private set; }
        public CellOwner LastAbsorbingSide { get; private set; }

        /// <summary>How many setup squares a side must have placed before it may end setup early: half its setup squares, rounded up.</summary>
        public int MinimumSetupSquaresBeforeEndingEarly => (setupSquaresEach + 1) / 2;

        /// <summary>The cells the last placement or generation converted to the other side, each once; empty when nothing was absorbed.</summary>
        public IReadOnlyList<int> LastConvertedCellIndices => absorber.ConvertedCellIndices;

        /// <summary>Wraps the shared grid and absorber in the Finished stage, so nothing can be placed until Begin is called.</summary>
        public VersusMatch(CellGrid grid, ClusterAbsorber absorber)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.absorber = absorber ?? throw new ArgumentNullException(nameof(absorber));
            remainingSetupSquares = new int[SideCount];
            remainingMatchSquares = new int[SideCount];
            Stage = VersusStage.Finished;
        }

        /// <summary>Kills every cell, refills both sides' pools and starts setup, which is complete at once if neither side can place.</summary>
        public void Begin(VersusMatchRules rules)
        {
            grid.KillAllCells();
            setupSquaresEach = rules.SetupSquaresEach;
            remainingSetupSquares[PlayerSlot] = rules.SetupSquaresEach;
            remainingSetupSquares[OpponentSlot] = rules.SetupSquaresEach;
            remainingMatchSquares[PlayerSlot] = rules.MatchSquaresEach;
            remainingMatchSquares[OpponentSlot] = rules.MatchSquaresEach;
            Stage = VersusStage.Setup;
            LastAbsorbingSide = CellOwner.None;
            absorber.ForgetConvertedCells();
            RefreshSetupCompletion();
        }

        /// <summary>Returns how many setup squares the side still has to place.</summary>
        public int GetRemainingSetupSquares(CellOwner side)
        {
            return remainingSetupSquares[ToSideSlot(side)];
        }

        /// <summary>Returns how many setup squares the side has placed so far this match.</summary>
        public int GetPlacedSetupSquares(CellOwner side)
        {
            return setupSquaresEach - remainingSetupSquares[ToSideSlot(side)];
        }

        /// <summary>Returns how many of the side's remaining setup squares it would keep for the match if it ended setup now: half, rounded down.</summary>
        public int GetSetupSquaresKeptIfEndedEarly(CellOwner side)
        {
            return remainingSetupSquares[ToSideSlot(side)] / 2;
        }

        /// <summary>Returns whether the side may stop placing setup squares now: setup is still running, the side has squares left, and it has placed at least the minimum.</summary>
        public bool CanEndSetupEarly(CellOwner side)
        {
            return Stage == VersusStage.Setup
                && !IsSetupComplete
                && remainingSetupSquares[ToSideSlot(side)] > 0
                && GetPlacedSetupSquares(side) >= MinimumSetupSquaresBeforeEndingEarly;
        }

        /// <summary>Stops the side placing setup squares: half its remaining ones (rounded down) move to its match pool, the rest are lost, and the other side places alone until it too is done.</summary>
        public void EndSetupEarly(CellOwner side)
        {
            if (!CanEndSetupEarly(side))
            {
                throw new InvalidOperationException($"{side} cannot end setup early: it has placed {GetPlacedSetupSquares(side)} of the {MinimumSetupSquaresBeforeEndingEarly} setup squares needed, with {remainingSetupSquares[ToSideSlot(side)]} left, in the {Stage} stage.");
            }

            var slot = ToSideSlot(side);
            remainingMatchSquares[slot] += remainingSetupSquares[slot] / 2;
            remainingSetupSquares[slot] = 0;
            RefreshSetupCompletion();
        }

        /// <summary>Ends setup when its time is up: both sides lose any setup squares they have not placed, and setup is complete.</summary>
        public void RunOutOfSetupTime()
        {
            if (Stage != VersusStage.Setup)
            {
                throw new InvalidOperationException($"Setup time can only run out during setup, but the stage is {Stage}.");
            }

            remainingSetupSquares[PlayerSlot] = 0;
            remainingSetupSquares[OpponentSlot] = 0;
            IsSetupComplete = true;
        }

        /// <summary>Returns whether the side can still place in setup: setup is running, and the side has setup squares left and an empty cell on its half to put one on.</summary>
        public bool CanStillPlaceInSetup(CellOwner side)
        {
            return Stage == VersusStage.Setup && remainingSetupSquares[ToSideSlot(side)] > 0 && GetSetupRegion(side).HasPlaceableCell(grid);
        }

        /// <summary>Returns how many match squares the side still has to place.</summary>
        public int GetRemainingMatchSquares(CellOwner side)
        {
            return remainingMatchSquares[ToSideSlot(side)];
        }

        /// <summary>Returns the half of the board the side places on during setup: the top rows for the Player, the rest for the Opponent.</summary>
        public PlacementRegion GetSetupRegion(CellOwner side)
        {
            var halfwayRow = grid.Rows / 2;
            return ToSideSlot(side) == PlayerSlot
                ? new PlacementRegion(0, halfwayRow)
                : new PlacementRegion(halfwayRow, grid.Rows);
        }

        /// <summary>Returns where the side may place right now: its setup half during setup, the whole grid during the match (only next to life for the app), and no rows once finished.</summary>
        public PlacementRegion GetPlacementRegion(CellOwner side)
        {
            EnsureIsSide(side);
            switch (Stage)
            {
                case VersusStage.Setup:
                    return GetSetupRegion(side);
                case VersusStage.Match:
                    return PlacementRegion.WholeGrid(grid, MustTouchLivingCellDuringMatch(side));
                default:
                    return RegionWithNoRows;
            }
        }

        /// <summary>Returns whether the side may place a square on the cell now, given the stage, its pool and the cell.</summary>
        public bool CanPlace(CellOwner side, int cellIndex)
        {
            if (!IsSide(side) || !IsEmptyCellInGrid(cellIndex))
            {
                return false;
            }

            switch (Stage)
            {
                case VersusStage.Setup:
                    return !IsSetupComplete
                        && remainingSetupSquares[ToSideSlot(side)] > 0
                        && GetSetupRegion(side).Contains(grid, cellIndex);
                case VersusStage.Match:
                    return remainingMatchSquares[ToSideSlot(side)] > 0
                        && GetPlacementRegion(side).CanPlaceOn(grid, cellIndex);
                default:
                    return false;
            }
        }

        /// <summary>Places the side's square if allowed, spends it from the current stage's pool and resolves absorptions; in setup it then checks whether both sides are done.</summary>
        public bool TryPlace(CellOwner side, int cellIndex)
        {
            if (!CanPlace(side, cellIndex))
            {
                return false;
            }

            var isSetup = Stage == VersusStage.Setup;
            var pool = isSetup ? remainingSetupSquares : remainingMatchSquares;
            pool[ToSideSlot(side)]--;
            grid.SetOwner(cellIndex, side);
            ResolveAbsorptionsAndRecordAbsorbingSide();
            if (isSetup)
            {
                RefreshSetupCompletion();
            }

            return true;
        }

        /// <summary>Returns whether the side may place all the listed cells as one setup move: setup still running, enough squares, and every cell empty, inside its half and listed once.</summary>
        public bool CanPlaceSetupPattern(CellOwner side, IReadOnlyList<int> cellIndices)
        {
            if (cellIndices == null)
            {
                throw new ArgumentNullException(nameof(cellIndices));
            }

            if (!IsSide(side) || Stage != VersusStage.Setup || IsSetupComplete)
            {
                return false;
            }

            var count = cellIndices.Count;
            if (count < 1 || count > remainingSetupSquares[ToSideSlot(side)])
            {
                return false;
            }

            var region = GetSetupRegion(side);
            for (var position = 0; position < count; position++)
            {
                var cellIndex = cellIndices[position];
                if (!IsEmptyCellInGrid(cellIndex) || !region.Contains(grid, cellIndex) || IsListedBefore(cellIndices, position))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Places the listed cells as one setup move if allowed: one square per cell and absorptions resolved once for the whole pattern.</summary>
        public bool TryPlaceSetupPattern(CellOwner side, IReadOnlyList<int> cellIndices)
        {
            if (!CanPlaceSetupPattern(side, cellIndices))
            {
                return false;
            }

            var count = cellIndices.Count;
            remainingSetupSquares[ToSideSlot(side)] -= count;
            for (var position = 0; position < count; position++)
            {
                grid.SetOwner(cellIndices[position], side);
            }

            ResolveAbsorptionsAndRecordAbsorbingSide();
            RefreshSetupCompletion();
            return true;
        }

        /// <summary>Returns whether the side's match squares must touch a living cell: true for the app, so its squares never appear out of nowhere; the player sees their own taps.</summary>
        private static bool MustTouchLivingCellDuringMatch(CellOwner side)
        {
            return side == CellOwner.Opponent;
        }

        /// <summary>Resolves absorptions and records which side gained cells from them, or None when nothing was absorbed or both sides broke even.</summary>
        private void ResolveAbsorptionsAndRecordAbsorbingSide()
        {
            var playerCellsBefore = grid.CountCells(CellOwner.Player);
            LastAbsorbingSide = CellOwner.None;
            if (absorber.ResolveAbsorptions(grid) == 0)
            {
                return;
            }

            var playerCellsAfter = grid.CountCells(CellOwner.Player);
            if (playerCellsAfter != playerCellsBefore)
            {
                LastAbsorbingSide = playerCellsAfter > playerCellsBefore ? CellOwner.Player : CellOwner.Opponent;
            }
        }

        /// <summary>Moves from setup to the match, which is only allowed once setup is complete.</summary>
        public void StartMatch()
        {
            if (Stage != VersusStage.Setup || !IsSetupComplete)
            {
                throw new InvalidOperationException($"The match can only start once setup is complete, but the stage is {Stage} and setup is {(IsSetupComplete ? "complete" : "incomplete")}.");
            }

            Stage = VersusStage.Match;
        }

        /// <summary>Advances one generation and resolves absorptions during the match, and does nothing in any other stage.</summary>
        public void AdvanceGeneration()
        {
            if (Stage != VersusStage.Match)
            {
                return;
            }

            grid.AdvanceGeneration();
            ResolveAbsorptionsAndRecordAbsorbingSide();
        }

        /// <summary>Returns how many living cells the side owns, throwing if the owner is not a side that places squares.</summary>
        public int CountCells(CellOwner side)
        {
            EnsureIsSide(side);
            return grid.CountCells(side);
        }

        /// <summary>Returns whether, during the match, the side has no living cells, whatever squares it has left; the match then ends after a short wait unless the side gets a square back.</summary>
        public bool HasNoCells(CellOwner side)
        {
            EnsureIsSide(side);
            return Stage == VersusStage.Match && grid.CountCells(side) == 0;
        }

        /// <summary>Returns the side with more living cells as the winner, or a draw when the counts are equal.</summary>
        public VersusOutcome DetermineOutcome()
        {
            var playerCells = grid.CountCells(CellOwner.Player);
            var opponentCells = grid.CountCells(CellOwner.Opponent);
            if (playerCells > opponentCells)
            {
                return VersusOutcome.PlayerWins;
            }

            return opponentCells > playerCells ? VersusOutcome.OpponentWins : VersusOutcome.Draw;
        }

        /// <summary>Ends the match so nothing more can be placed and returns its outcome.</summary>
        public VersusOutcome Finish()
        {
            Stage = VersusStage.Finished;
            return DetermineOutcome();
        }

        /// <summary>Marks setup complete once neither side can place any more, because each has spent its squares, ended early or filled its half.</summary>
        private void RefreshSetupCompletion()
        {
            IsSetupComplete = !CanStillPlaceInSetup(CellOwner.Player) && !CanStillPlaceInSetup(CellOwner.Opponent);
        }

        /// <summary>Returns whether the cell at the position also appears earlier in the list.</summary>
        private static bool IsListedBefore(IReadOnlyList<int> cellIndices, int position)
        {
            var cellIndex = cellIndices[position];
            for (var earlier = 0; earlier < position; earlier++)
            {
                if (cellIndices[earlier] == cellIndex)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns whether the cell index lies in the grid's active area and the cell there is empty.</summary>
        private bool IsEmptyCellInGrid(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < grid.CellCount && !grid.IsAlive(cellIndex);
        }

        /// <summary>Returns whether the owner is one of the two sides that place squares.</summary>
        private static bool IsSide(CellOwner owner)
        {
            return owner == CellOwner.Player || owner == CellOwner.Opponent;
        }

        /// <summary>Returns the pool slot for a side, throwing if the owner is not a side that places squares.</summary>
        private static int ToSideSlot(CellOwner side)
        {
            EnsureIsSide(side);
            return side == CellOwner.Player ? PlayerSlot : OpponentSlot;
        }

        /// <summary>Throws if the owner is not one of the two sides that place squares.</summary>
        private static void EnsureIsSide(CellOwner side)
        {
            if (!IsSide(side))
            {
                throw new ArgumentException($"{side} is not a side that places squares.", nameof(side));
            }
        }
    }
}

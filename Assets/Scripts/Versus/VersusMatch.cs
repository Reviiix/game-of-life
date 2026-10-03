using System;
using GameOfLife.Opponents;
using GameOfLife.Simulation;

namespace GameOfLife.Versus
{
    /// <summary>Applies the rules of one versus match: turn-based setup on each side's half, then free placement between generations.</summary>
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

        public VersusStage Stage { get; private set; }
        public CellOwner SideToPlace { get; private set; }
        public bool IsSetupComplete { get; private set; }
        public CellOwner LastAbsorbingSide { get; private set; }

        /// <summary>Wraps the shared grid and absorber in the Finished stage, so nothing can be placed until Begin is called.</summary>
        public VersusMatch(CellGrid grid, ClusterAbsorber absorber)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.absorber = absorber ?? throw new ArgumentNullException(nameof(absorber));
            remainingSetupSquares = new int[SideCount];
            remainingMatchSquares = new int[SideCount];
            Stage = VersusStage.Finished;
            SideToPlace = CellOwner.None;
        }

        /// <summary>Kills every cell, refills both sides' pools and starts setup with the Player's turn, skipping any side that cannot place.</summary>
        public void Begin(VersusMatchRules rules)
        {
            grid.KillAllCells();
            remainingSetupSquares[PlayerSlot] = rules.SetupSquaresEach;
            remainingSetupSquares[OpponentSlot] = rules.SetupSquaresEach;
            remainingMatchSquares[PlayerSlot] = rules.MatchSquaresEach;
            remainingMatchSquares[OpponentSlot] = rules.MatchSquaresEach;
            Stage = VersusStage.Setup;
            IsSetupComplete = false;
            LastAbsorbingSide = CellOwner.None;
            PassSetupTurn(CellOwner.Player, CellOwner.Opponent);
        }

        /// <summary>Returns how many setup squares the side still has to place.</summary>
        public int GetRemainingSetupSquares(CellOwner side)
        {
            return remainingSetupSquares[ToSideSlot(side)];
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

        /// <summary>Returns whether the side may place a square on the cell now, given the stage, the turn, its pool and the cell.</summary>
        public bool CanPlace(CellOwner side, int cellIndex)
        {
            if (!IsSide(side) || !IsEmptyCellInGrid(cellIndex))
            {
                return false;
            }

            switch (Stage)
            {
                case VersusStage.Setup:
                    return side == SideToPlace
                        && remainingSetupSquares[ToSideSlot(side)] > 0
                        && GetSetupRegion(side).Contains(grid, cellIndex);
                case VersusStage.Match:
                    return remainingMatchSquares[ToSideSlot(side)] > 0
                        && GetPlacementRegion(side).CanPlaceOn(grid, cellIndex);
                default:
                    return false;
            }
        }

        /// <summary>Places the side's square if allowed, spends it from the current stage's pool, resolves absorptions and passes the setup turn.</summary>
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
                PassSetupTurn(OtherSide(side), side);
            }

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

        /// <summary>Returns whether, during the match, either side has no living cells and no match squares left.</summary>
        public bool IsDecidedEarly()
        {
            return Stage == VersusStage.Match && (HasRunOut(CellOwner.Player) || HasRunOut(CellOwner.Opponent));
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
            SideToPlace = CellOwner.None;
            return DetermineOutcome();
        }

        /// <summary>Gives the setup turn to the preferred side if it can still place, else to the fallback side, else marks setup complete.</summary>
        private void PassSetupTurn(CellOwner preferredSide, CellOwner fallbackSide)
        {
            if (CanStillPlaceInSetup(preferredSide))
            {
                SideToPlace = preferredSide;
                return;
            }

            if (CanStillPlaceInSetup(fallbackSide))
            {
                SideToPlace = fallbackSide;
                return;
            }

            SideToPlace = CellOwner.None;
            IsSetupComplete = true;
        }

        /// <summary>Returns whether the side has setup squares left and an empty cell on its half to put one on.</summary>
        private bool CanStillPlaceInSetup(CellOwner side)
        {
            return remainingSetupSquares[ToSideSlot(side)] > 0 && GetSetupRegion(side).HasPlaceableCell(grid);
        }

        /// <summary>Returns whether the side has no living cells and no match squares left, so it can no longer score.</summary>
        private bool HasRunOut(CellOwner side)
        {
            return remainingMatchSquares[ToSideSlot(side)] == 0 && grid.CountCells(side) == 0;
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

        /// <summary>Returns the side playing against the given side.</summary>
        private static CellOwner OtherSide(CellOwner side)
        {
            return side == CellOwner.Player ? CellOwner.Opponent : CellOwner.Player;
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

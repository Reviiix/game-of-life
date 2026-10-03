using System;
using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>The Hard opponent: tries each promising empty cell on a scratch grid and places where its cell lead grows most now and one generation later.</summary>
    public sealed class StrategicOpponentStrategy : IOpponentStrategy
    {
        private const int NoCell = -1;
        private const int MaximumCandidatesEvaluated = 160;
        private const int MinimumCandidatesEvaluated = 32;
        private const int MaximumGridCellsScoredPerChoice = 96000;
        private const double ImmediateGainWeight = 1.0;
        private const double LookaheadGainWeight = 0.5;
        private const double MaximumTiebreakNoise = 0.25;

        private readonly Random random;
        private readonly int[] lifeAdjacentCellIndices;
        private readonly int[] isolatedCellIndices;
        private readonly CellGrid scratchGrid;
        private readonly ClusterAbsorber scratchAbsorber;

        /// <summary>Allocates the candidate buffers, scratch grid and scratch absorber for the largest allowed grid up front so choosing never allocates.</summary>
        public StrategicOpponentStrategy(Random random, int maximumRows, int maximumColumns)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            scratchGrid = new CellGrid(maximumRows, maximumColumns);
            scratchAbsorber = new ClusterAbsorber(scratchGrid.MaximumCellCount);
            lifeAdjacentCellIndices = new int[scratchGrid.MaximumCellCount];
            isolatedCellIndices = new int[scratchGrid.MaximumCellCount];
        }

        /// <summary>Picks the cell the region allows with the best score for the given side, or returns false if it allows none.</summary>
        public bool TryChooseCell(CellGrid grid, PlacementRegion region, CellOwner self, out int cellIndex)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (grid.CellCount > lifeAdjacentCellIndices.Length)
            {
                throw new ArgumentException($"A grid of {grid.CellCount} cells does not fit in {lifeAdjacentCellIndices.Length} cells.", nameof(grid));
            }

            if (self != CellOwner.Player && self != CellOwner.Opponent)
            {
                throw new ArgumentException("The strategy must play for the Player or the Opponent.", nameof(self));
            }

            var lifeAdjacentCount = CollectCandidates(grid, region, out var isolatedCount);
            if (lifeAdjacentCount == 0 && isolatedCount == 0)
            {
                cellIndex = NoCell;
                return false;
            }

            var candidateLimit = GetCandidateLimit(grid);
            var leadBefore = CountCellLead(grid, self);
            cellIndex = NoCell;
            var bestScore = double.NegativeInfinity;
            var anyLifeAdjacentCellIsKept = ScoreRandomSample(grid, self, lifeAdjacentCellIndices, lifeAdjacentCount, candidateLimit, leadBefore, ref cellIndex, ref bestScore);
            if (lifeAdjacentCount == 0 || (!anyLifeAdjacentCellIsKept && ShouldTryIsolatedCells(grid, region)))
            {
                ScoreRandomSample(grid, self, isolatedCellIndices, isolatedCount, candidateLimit, leadBefore, ref cellIndex, ref bestScore);
            }

            return true;
        }

        /// <summary>Sorts the region's empty cells into those next to a living cell, returned as the count, and the isolated rest, which a region requiring a living neighbour never offers.</summary>
        private int CollectCandidates(CellGrid grid, PlacementRegion region, out int isolatedCount)
        {
            var lifeAdjacentCount = 0;
            isolatedCount = 0;
            var endCellIndex = region.GetEndCellIndex(grid);
            for (var cellIndex = region.GetFirstCellIndex(grid); cellIndex < endCellIndex; cellIndex++)
            {
                if (grid.IsAlive(cellIndex))
                {
                    continue;
                }

                if (grid.HasLivingNeighbour(cellIndex))
                {
                    lifeAdjacentCellIndices[lifeAdjacentCount] = cellIndex;
                    lifeAdjacentCount++;
                }
                else if (!region.RequiresLivingNeighbour)
                {
                    isolatedCellIndices[isolatedCount] = cellIndex;
                    isolatedCount++;
                }
            }

            return lifeAdjacentCount;
        }

        /// <summary>Returns whether to try isolated cells once every cell next to life would be absorbed: only in a partial region, as in setup, where no generation runs and a kept cell stays.</summary>
        private static bool ShouldTryIsolatedCells(CellGrid grid, PlacementRegion region)
        {
            return !region.CoversWholeGrid(grid);
        }

        /// <summary>Returns how many candidates of one kind to score, fewer on large grids because every score costs several passes over the whole grid.</summary>
        private static int GetCandidateLimit(CellGrid grid)
        {
            var affordableCandidates = MaximumGridCellsScoredPerChoice / grid.CellCount;
            return Math.Max(MinimumCandidatesEvaluated, Math.Min(MaximumCandidatesEvaluated, affordableCandidates));
        }

        /// <summary>Scores a random sample of the candidates, keeping the best cell and score so far, and returns whether any scored placement keeps its cell instead of being absorbed.</summary>
        private bool ScoreRandomSample(CellGrid grid, CellOwner self, int[] candidateCellIndices, int candidateCount, int candidateLimit, int leadBefore, ref int bestCellIndex, ref double bestScore)
        {
            var sampleSize = Math.Min(candidateCount, candidateLimit);
            MoveRandomSampleToFront(candidateCellIndices, candidateCount, sampleSize);
            var anyPlacedCellIsKept = false;
            for (var position = 0; position < sampleSize; position++)
            {
                var candidateCellIndex = candidateCellIndices[position];
                var score = ScoreCandidate(grid, self, candidateCellIndex, leadBefore, out var isPlacedCellKept);
                anyPlacedCellIsKept |= isPlacedCellKept;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCellIndex = candidateCellIndex;
                }
            }

            return anyPlacedCellIsKept;
        }

        /// <summary>Moves a uniformly random sample of the candidates to the front of their buffer with a partial Fisher-Yates shuffle.</summary>
        private void MoveRandomSampleToFront(int[] candidateCellIndices, int candidateCount, int sampleSize)
        {
            if (sampleSize >= candidateCount)
            {
                return;
            }

            for (var position = 0; position < sampleSize; position++)
            {
                var swapPosition = random.Next(position, candidateCount);
                (candidateCellIndices[position], candidateCellIndices[swapPosition]) = (candidateCellIndices[swapPosition], candidateCellIndices[position]);
            }
        }

        /// <summary>Plays the candidate on the scratch grid and scores how much the side's cell lead grows straight away and one generation later.</summary>
        private double ScoreCandidate(CellGrid grid, CellOwner self, int candidateCellIndex, int leadBefore, out bool isPlacedCellKept)
        {
            scratchGrid.CopyFrom(grid);
            scratchGrid.SetOwner(candidateCellIndex, self);
            scratchAbsorber.ResolveAbsorptions(scratchGrid);
            var immediateGain = CountCellLead(scratchGrid, self) - leadBefore;
            isPlacedCellKept = scratchGrid.GetOwner(candidateCellIndex) == self;

            scratchGrid.AdvanceGeneration();
            scratchAbsorber.ResolveAbsorptions(scratchGrid);
            var lookaheadGain = CountCellLead(scratchGrid, self) - leadBefore;

            var tiebreakNoise = random.NextDouble() * MaximumTiebreakNoise;
            return ImmediateGainWeight * immediateGain + LookaheadGainWeight * lookaheadGain + tiebreakNoise;
        }

        /// <summary>Returns how many more living cells the side owns than its enemy, in one pass over the active area.</summary>
        private static int CountCellLead(CellGrid grid, CellOwner self)
        {
            var cellLead = 0;
            var cellCount = grid.CellCount;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var owner = grid.GetOwner(cellIndex);
                if (owner == self)
                {
                    cellLead++;
                }
                else if (owner != CellOwner.None)
                {
                    cellLead--;
                }
            }

            return cellLead;
        }
    }
}

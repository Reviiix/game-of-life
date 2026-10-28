using System;
using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>The Medium and Hard opponents: score every allowed cell by how likely its square is to stay alive (block building with breathing room, next-generation support, avoiding absorption), and Hard also plays its best match moves forward on a scratch board.</summary>
    public sealed class SurvivalOpponentStrategy : IOpponentStrategy
    {
        private const int NoCell = -1;
        private const int MinimumRelaxedClearance = 1;
        private const int TouchingDistance = 1;
        private const int NearbyLifeDistance = 2;
        private const double TouchingLifePenalty = 10.0;
        private const double NearbyLifePenalty = 1.0;
        private const int NextGenerationGainWeight = 1;
        private const int ImmediateLeadWeight = 1;
        private const int DurableOwnCellWeight = 1;
        private const int PlacedCellKeptWeight = 3;
        private const int MinimumLookaheadCandidates = 4;
        private const int LookaheadCellGenerationBudget = 20000;

        private readonly Random random;
        private readonly OpponentSkill skill;
        private readonly PlacementSurvey survey;
        private readonly BlockSites blockSites;
        private readonly int[] candidateCellIndices;
        private readonly double[] candidateScores;
        private readonly CellGrid scratchGrid;
        private readonly ClusterAbsorber scratchAbsorber;

        /// <summary>Allocates every buffer, and the scratch board only if the skill looks ahead, for the largest board up front so choosing never allocates.</summary>
        public SurvivalOpponentStrategy(Random random, OpponentSkill skill, int maximumRows, int maximumColumns)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.skill = skill;
            var maximumCellCount = maximumRows * maximumColumns;
            survey = new PlacementSurvey(maximumCellCount);
            blockSites = new BlockSites(maximumCellCount);
            candidateCellIndices = new int[maximumCellCount];
            candidateScores = new double[maximumCellCount];
            if (skill.LookaheadCandidates > 0)
            {
                scratchGrid = new CellGrid(maximumRows, maximumColumns);
                scratchAbsorber = new ClusterAbsorber(maximumCellCount);
            }
        }

        /// <summary>Picks the allowed cell whose square is most likely to stay alive for the side, or returns false if the region allows none.</summary>
        public bool TryChooseCell(CellGrid grid, PlacementRegion region, CellOwner self, out int cellIndex)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (grid.CellCount > candidateCellIndices.Length)
            {
                throw new ArgumentException($"A grid of {grid.CellCount} cells does not fit in {candidateCellIndices.Length} cells.", nameof(grid));
            }

            if (self != CellOwner.Player && self != CellOwner.Opponent)
            {
                throw new ArgumentException("The strategy must play for the Player or the Opponent.", nameof(self));
            }

            var isMatch = IsMatchRegion(grid, region);
            var candidateCount = CollectCandidates(grid, region, isMatch);
            if (candidateCount == 0)
            {
                cellIndex = NoCell;
                return false;
            }

            survey.Survey(grid, self);
            blockSites.Prepare(grid);
            var clearance = isMatch ? skill.BlockClearance : FindSetupClearance(grid, region, self, candidateCount);
            for (var position = 0; position < candidateCount; position++)
            {
                candidateScores[position] = ScoreLocally(grid, region, candidateCellIndices[position], self, isMatch, clearance) + random.NextDouble() * skill.MaximumScoreNoise;
            }

            var consideredCount = candidateCount;
            if (isMatch && skill.LookaheadCandidates > 0)
            {
                consideredCount = Math.Min(candidateCount, GetLookaheadShortlistSize(grid));
                MoveBestCandidatesToFront(candidateCount, consideredCount);
                var leadBefore = CountCellLead(grid, self);
                var ownCellsBefore = grid.CountCells(self);
                for (var position = 0; position < consideredCount; position++)
                {
                    candidateScores[position] += ScoreByLookahead(grid, candidateCellIndices[position], self, leadBefore, ownCellsBefore);
                }
            }

            cellIndex = candidateCellIndices[FindBestPosition(consideredCount)];
            return true;
        }

        /// <summary>Returns whether the region is a match placement rather than setup: VersusMatch gives a side only part of the board during setup, when no generations run.</summary>
        private static bool IsMatchRegion(CellGrid grid, PlacementRegion region)
        {
            return region.RequiresLivingNeighbour || region.CoversWholeGrid(grid);
        }

        /// <summary>Writes the cells to score into the candidate buffer: in setup every empty cell of the region, so new blocks can start in open space; in the match only empty cells touching life, unless there are none and the region allows any.</summary>
        private int CollectCandidates(CellGrid grid, PlacementRegion region, bool isMatch)
        {
            var firstCellIndex = region.GetFirstCellIndex(grid);
            var endCellIndex = region.GetEndCellIndex(grid);
            var candidateCount = 0;
            for (var cellIndex = firstCellIndex; cellIndex < endCellIndex; cellIndex++)
            {
                if (!grid.IsAlive(cellIndex) && (!isMatch || grid.HasLivingNeighbour(cellIndex)))
                {
                    candidateCellIndices[candidateCount] = cellIndex;
                    candidateCount++;
                }
            }

            if (candidateCount > 0 || !isMatch || region.RequiresLivingNeighbour)
            {
                return candidateCount;
            }

            for (var cellIndex = firstCellIndex; cellIndex < endCellIndex; cellIndex++)
            {
                if (!grid.IsAlive(cellIndex))
                {
                    candidateCellIndices[candidateCount] = cellIndex;
                    candidateCount++;
                }
            }

            return candidateCount;
        }

        /// <summary>Returns the skill's block clearance, relaxed a cell at a time down to one while no setup candidate could build a block with it, so a crowded half still gets blocks.</summary>
        private int FindSetupClearance(CellGrid grid, PlacementRegion region, CellOwner self, int candidateCount)
        {
            var clearance = skill.BlockClearance;
            while (clearance > MinimumRelaxedClearance && !CanAnyCandidateBuildABlock(grid, region, self, candidateCount, clearance))
            {
                clearance--;
            }

            return clearance;
        }

        /// <summary>Returns whether a square on any candidate would make block progress with this clearance.</summary>
        private bool CanAnyCandidateBuildABlock(CellGrid grid, PlacementRegion region, CellOwner self, int candidateCount, int clearance)
        {
            for (var position = 0; position < candidateCount; position++)
            {
                if (blockSites.Progress(grid, region, candidateCellIndices[position], self, clearance) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Scores a cell from local Life knowledge: block progress with breathing room and absorption balance, plus next-generation support in the match, or in setup a penalty for crowding life with a square that builds nothing.</summary>
        private double ScoreLocally(CellGrid grid, PlacementRegion region, int cellIndex, CellOwner self, bool isMatch, int clearance)
        {
            var score = blockSites.Progress(grid, region, cellIndex, self, clearance) + survey.ScoreAbsorption(cellIndex);
            if (isMatch)
            {
                score += NextGenerationGainWeight * survey.NextGenerationOwnGain(cellIndex);
            }
            else if (score <= 0)
            {
                score -= GetCrowdingPenalty(grid, cellIndex);
            }

            return score;
        }

        /// <summary>Returns how much a square that builds nothing would disturb nearby life: a lot for each living cell it touches and a little for each within two cells, which could still share a birth with it.</summary>
        private double GetCrowdingPenalty(CellGrid grid, int cellIndex)
        {
            return TouchingLifePenalty * blockSites.CountLivingAround(grid, cellIndex, TouchingDistance)
                + NearbyLifePenalty * blockSites.CountLivingAround(grid, cellIndex, NearbyLifeDistance);
        }

        /// <summary>Returns how many of the best local candidates to play forward: the skill's number, fewer on crowded boards where every simulated generation costs more.</summary>
        private int GetLookaheadShortlistSize(CellGrid grid)
        {
            var livingCells = Math.Max(1, grid.CountCells(CellOwner.Player) + grid.CountCells(CellOwner.Opponent));
            var affordable = LookaheadCellGenerationBudget / ((skill.LookaheadGenerations + 1) * livingCells);
            return Math.Max(MinimumLookaheadCandidates, Math.Min(skill.LookaheadCandidates, affordable));
        }

        /// <summary>Moves the best-scoring candidates to the front of the buffers with a partial selection sort.</summary>
        private void MoveBestCandidatesToFront(int candidateCount, int bestCount)
        {
            for (var position = 0; position < bestCount; position++)
            {
                var bestPosition = position;
                for (var other = position + 1; other < candidateCount; other++)
                {
                    if (candidateScores[other] > candidateScores[bestPosition])
                    {
                        bestPosition = other;
                    }
                }

                (candidateScores[position], candidateScores[bestPosition]) = (candidateScores[bestPosition], candidateScores[position]);
                (candidateCellIndices[position], candidateCellIndices[bestPosition]) = (candidateCellIndices[bestPosition], candidateCellIndices[position]);
            }
        }

        /// <summary>Plays the square on the scratch board for the skill's generations with absorption after every step, and scores the lead it wins at once, the own cells still standing at the end, and whether the placed square itself is still the side's.</summary>
        private double ScoreByLookahead(CellGrid grid, int cellIndex, CellOwner self, int leadBefore, int ownCellsBefore)
        {
            scratchGrid.CopyFrom(grid);
            scratchGrid.SetOwner(cellIndex, self);
            scratchAbsorber.ResolveAbsorptions(scratchGrid);
            var immediateLeadGain = CountCellLead(scratchGrid, self) - leadBefore;
            for (var generation = 0; generation < skill.LookaheadGenerations; generation++)
            {
                scratchGrid.AdvanceGeneration();
                scratchAbsorber.ResolveAbsorptions(scratchGrid);
            }

            var durableOwnCellGain = scratchGrid.CountCells(self) - ownCellsBefore;
            var isPlacedCellKept = scratchGrid.GetOwner(cellIndex) == self ? 1 : 0;
            return ImmediateLeadWeight * immediateLeadGain + DurableOwnCellWeight * durableOwnCellGain + PlacedCellKeptWeight * isPlacedCellKept;
        }

        /// <summary>Returns the position of the highest score among the first candidates; the first one wins a tie, and the score noise makes ties rare.</summary>
        private int FindBestPosition(int consideredCount)
        {
            var bestPosition = 0;
            for (var position = 1; position < consideredCount; position++)
            {
                if (candidateScores[position] > candidateScores[bestPosition])
                {
                    bestPosition = position;
                }
            }

            return bestPosition;
        }

        /// <summary>Returns how many more living cells the side owns than its enemy.</summary>
        private static int CountCellLead(CellGrid grid, CellOwner self)
        {
            var enemy = self == CellOwner.Player ? CellOwner.Opponent : CellOwner.Player;
            return grid.CountCells(self) - grid.CountCells(enemy);
        }
    }
}

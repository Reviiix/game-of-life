using System;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using GameOfLife.Versus;

namespace GameOfLife.Tests
{
    /// <summary>Plays seeded headless Versus matches through the real VersusMatch, ending them as the game does three seconds after a side is wiped out, and measures how long the app's squares stay alive and its own colour, and how often it wins.</summary>
    public sealed class VersusMatchSimulator
    {
        private const int Rows = 24;
        private const int Columns = 12;
        private const int SetupSquares = 20;
        private const int MatchSquares = 5;
        private const int Generations = 60;
        private const float SecondsPerGeneration = 1f;
        private const float EmptySideEndSeconds = 3f;
        private const int MaximumPlacements = SetupSquares + MatchSquares;

        private readonly int[] placedCells = new int[MaximumPlacements];
        private readonly int[] placedGenerations = new int[MaximumPlacements];
        private readonly int[] survivalAges;
        private readonly int[] survivors;
        private readonly int[] squaresReachingAge;
        private int placedCount;
        private int wins;
        private int matchesPlayed;

        /// <summary>Prepares to measure survival at the given ages in generations.</summary>
        public VersusMatchSimulator(params int[] survivalAges)
        {
            this.survivalAges = survivalAges;
            survivors = new int[survivalAges.Length];
            squaresReachingAge = new int[survivalAges.Length];
        }

        public double WinRate => matchesPlayed == 0 ? 0 : wins / (double)matchesPlayed;

        /// <summary>Returns the share of the app's squares that were alive and its own colour at the age with this position in the constructor's list.</summary>
        public double GetSurvival(int agePosition)
        {
            return squaresReachingAge[agePosition] == 0 ? 0 : survivors[agePosition] / (double)squaresReachingAge[agePosition];
        }

        /// <summary>Plays the given number of matches on the default 24x12 board, the app at one difficulty against a player strategy at another, with seeds derived from the seed base.</summary>
        public void PlayMatches(OpponentDifficulty appDifficulty, OpponentDifficulty playerDifficulty, int matches, int seedBase)
        {
            for (var match = 0; match < matches; match++)
            {
                PlayMatch(appDifficulty, playerDifficulty, seedBase + match);
            }
        }

        /// <summary>Plays one match: alternating setup, then generations with each side spending its match squares at seeded, evenly spread moments, until time runs out or a side has been wiped out for the game's wait.</summary>
        private void PlayMatch(OpponentDifficulty appDifficulty, OpponentDifficulty playerDifficulty, int seed)
        {
            var grid = new CellGrid(Rows, Columns);
            grid.Resize(Rows, Columns);
            var match = new VersusMatch(grid, new ClusterAbsorber(grid.MaximumCellCount));
            var app = OpponentStrategyFactory.Create(appDifficulty, new Random(seed * 31 + 1), Rows, Columns);
            var player = OpponentStrategyFactory.Create(playerDifficulty, new Random(seed * 31 + 2), Rows, Columns);
            var fallback = OpponentStrategyFactory.Create(OpponentDifficulty.Easy, new Random(seed * 31 + 3), Rows, Columns);
            var timing = new Random(seed * 31 + 4);
            placedCount = 0;
            match.Begin(new VersusMatchRules(SetupSquares, MatchSquares));
            while (!match.IsSetupComplete)
            {
                var placedAny = false;
                if (match.CanStillPlaceInSetup(CellOwner.Player))
                {
                    placedAny |= TryPlace(match, grid, player, fallback, CellOwner.Player, 0);
                }

                if (match.CanStillPlaceInSetup(CellOwner.Opponent))
                {
                    placedAny |= TryPlace(match, grid, app, fallback, CellOwner.Opponent, 0);
                }

                if (!placedAny)
                {
                    break;
                }
            }

            match.StartMatch();
            var emptySideCountdown = new EmptySideCountdown(EmptySideEndSeconds);
            RefreshEmptySides(match, emptySideCountdown);
            var appMoveGeneration = PickMoveGeneration(timing, 1);
            var playerMoveGeneration = PickMoveGeneration(timing, 1);
            var appMovesMade = 0;
            var playerMovesMade = 0;
            for (var generation = 1; generation <= Generations; generation++)
            {
                if (emptySideCountdown.Tick(SecondsPerGeneration))
                {
                    break;
                }

                match.AdvanceGeneration();
                RefreshEmptySides(match, emptySideCountdown);
                RecordSurvival(grid, generation);
                if (playerMovesMade < MatchSquares && generation >= playerMoveGeneration)
                {
                    TryPlace(match, grid, player, fallback, CellOwner.Player, generation);
                    RefreshEmptySides(match, emptySideCountdown);
                    playerMovesMade++;
                    playerMoveGeneration = PickMoveGeneration(timing, playerMovesMade + 1);
                }

                if (appMovesMade < MatchSquares && generation >= appMoveGeneration)
                {
                    TryPlace(match, grid, app, fallback, CellOwner.Opponent, generation);
                    RefreshEmptySides(match, emptySideCountdown);
                    appMovesMade++;
                    appMoveGeneration = PickMoveGeneration(timing, appMovesMade + 1);
                }
            }

            matchesPlayed++;
            wins += match.Finish() == VersusOutcome.OpponentWins ? 1 : 0;
        }

        /// <summary>Starts or drops each side's wait before the match ends, as the game does after every generation and placement.</summary>
        private static void RefreshEmptySides(VersusMatch match, EmptySideCountdown emptySideCountdown)
        {
            emptySideCountdown.Refresh(match.HasNoCells(CellOwner.Player), match.HasNoCells(CellOwner.Opponent));
        }

        /// <summary>Returns the generation of a side's next match square: its share of the match, jittered by up to 30 percent.</summary>
        private static int PickMoveGeneration(Random timing, int moveNumber)
        {
            var evenGeneration = Generations * moveNumber / (MatchSquares + 1.0);
            return Math.Max(1, (int)Math.Round(evenGeneration * (0.7 + 0.6 * timing.NextDouble())));
        }

        /// <summary>Places the strategy's choice for the side, or an Easy choice if it is not allowed, and records app squares; returns false if neither can place.</summary>
        private bool TryPlace(VersusMatch match, CellGrid grid, IOpponentStrategy strategy, IOpponentStrategy fallback, CellOwner side, int generation)
        {
            var region = match.GetPlacementRegion(side);
            var placed = strategy.TryChooseCell(grid, region, side, out var cellIndex) && match.TryPlace(side, cellIndex);
            if (!placed)
            {
                placed = fallback.TryChooseCell(grid, region, side, out cellIndex) && match.TryPlace(side, cellIndex);
            }

            if (placed && side == CellOwner.Opponent && placedCount < MaximumPlacements)
            {
                placedCells[placedCount] = cellIndex;
                placedGenerations[placedCount] = generation;
                placedCount++;
            }

            return placed;
        }

        /// <summary>Counts, for every app square that has just reached a measured age, whether its cell is still alive and the app's.</summary>
        private void RecordSurvival(CellGrid grid, int generation)
        {
            for (var placement = 0; placement < placedCount; placement++)
            {
                var age = generation - placedGenerations[placement];
                for (var agePosition = 0; agePosition < survivalAges.Length; agePosition++)
                {
                    if (survivalAges[agePosition] != age)
                    {
                        continue;
                    }

                    squaresReachingAge[agePosition]++;
                    survivors[agePosition] += grid.GetOwner(placedCells[placement]) == CellOwner.Opponent ? 1 : 0;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using GameOfLife.Versus;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace GameOfLife.Tests
{
    /// <summary>Checks the random placement planner: exact budgets inside the band, no living or repeated cells, whole catalogue patterns kept clear of life, repeatable seeds, clean re-planning, Versus setup moves, and which bounds have a square limit.</summary>
    [TestFixture]
    public sealed class RandomPlacementPlannerTests
    {
        private const int BoardRows = 24;
        private const int BoardColumns = 12;
        private const int BoardCellCount = BoardRows * BoardColumns;
        private const int HalfRows = BoardRows / 2;
        private const int SetupSquares = 20;
        private const int NoMatchSquares = 0;
        private const int ClassicSharePercent = 30;
        private const int ClassicCellBudget = BoardCellCount * ClassicSharePercent / 100;
        private const int RoomyBudget = 30;
        private const int GenerousBudget = 100;
        private const int ExtraPatternChances = 3;
        private const int NegativeBudget = -5;
        private const int CellsPerPatternChance = 10;
        private const int PatternChanceEveryCell = 1;
        private const int PatternChanceEveryThreeCells = 3;
        private const double DefaultPatternChance = 0.4d;
        private const double NeverPattern = 0d;
        private const double AlwaysPattern = 1d;
        private const double LivingShare = 0.2d;
        private const int SeedsTried = 20;
        private const int SmallBudgetSeedsTried = 5;
        private const int LargestTestPatternCells = 9;
        private const int FilledBandRows = 6;
        private const int ShallowBandRows = 2;
        private const int ShallowBandBudget = 12;
        private const int SmallBoardRows = 8;
        private const int SmallBoardHalfRows = SmallBoardRows / 2;
        private const int PoseCount = 8;
        private const int ScatterSeedOffset = 1000;
        private const int PlanSeed = 7;
        private const int EmptyBoardSeed = 11;

        private static readonly CellPattern[] Shapes = PatternTestShapes.CatalogueShapes();
        private static readonly CellOwner[] LivingOwners = { CellOwner.Player, CellOwner.Opponent, CellOwner.Third, CellOwner.Fourth };
        private static readonly (int row, int column)[] RoomyBoardLivingCells = { (2, 2), (6, 9), (11, 5), (15, 1), (20, 8) };
        private static readonly string[] ShallowBandShapeNames = { "Block", "Blinker" };

        /// <summary>The setup squares placed one at a time before a random move, alternating sides and hugging the half line from both sides.</summary>
        private static readonly (CellOwner side, int row, int column)[] SquaresPlacedFirst =
        {
            (CellOwner.Player, HalfRows - 1, 3),
            (CellOwner.Opponent, HalfRows, 4),
            (CellOwner.Player, 4, 7),
            (CellOwner.Opponent, HalfRows + 5, 2),
            (CellOwner.Player, HalfRows - 1, 9),
            (CellOwner.Opponent, HalfRows, 9),
        };

        /// <summary>On a Classic board with living cells of every colour, each seed plans exactly the budget: unique, empty cells anywhere on the board.</summary>
        [Test]
        public void Plan_OnAClassicBoard_PlansExactlyTheBudget()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var grid = CreateBoardWithLivingCells(seed);
                var planner = CreatePlanner(seed, DefaultPatternChance);

                var planned = planner.Plan(grid, bounds, ClassicCellBudget);

                Assert.That(planned, Is.EqualTo(ClassicCellBudget), $"Seed {seed}: the returned count.");
                AssertValidPlan(grid, bounds, planner, ClassicCellBudget, $"Seed {seed}");
            }
        }

        /// <summary>On either Versus half of a board with living cells on both halves, each seed plans exactly the setup squares left, all inside that half.</summary>
        [TestCase(CellOwner.Player)]
        [TestCase(CellOwner.Opponent)]
        public void Plan_OnAVersusHalf_PlansExactlyTheSquaresLeft(CellOwner side)
        {
            var bounds = CreateHalfBounds(BoardRows, side, SetupSquares);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var grid = CreateBoardWithLivingCells(seed);
                var planner = CreatePlanner(seed, DefaultPatternChance);

                var planned = planner.Plan(grid, bounds, bounds.SquaresAvailable);

                Assert.That(planned, Is.EqualTo(SetupSquares), $"Seed {seed}: the returned count.");
                AssertValidPlan(grid, bounds, planner, SetupSquares, $"Seed {seed}, {side}'s half");
            }
        }

        /// <summary>Planning only chooses cells: every owner stays as it was and no change is recorded.</summary>
        [Test]
        public void Plan_DoesNotChangeTheBoard()
        {
            var grid = CreateBoardWithLivingCells(PlanSeed);
            var ownersBefore = CopyOwners(grid);
            var planner = CreatePlanner(PlanSeed, AlwaysPattern);

            planner.Plan(grid, PlacementBounds.WholeBoard(BoardRows, BoardColumns), ClassicCellBudget);

            Assert.That(planner.PlannedCells.Count, Is.EqualTo(ClassicCellBudget), "Cells were planned.");
            Assert.That(CopyOwners(grid), Is.EqualTo(ownersBefore), "Every cell keeps its owner.");
            Assert.That(grid.ChangedCellIndices, Is.Empty, "No change is recorded.");
        }

        /// <summary>When the band has fewer empty cells than the budget, every empty cell in the band is planned and nothing else, using pattern borders when patterns took the room.</summary>
        [TestCase(NeverPattern)]
        [TestCase(AlwaysPattern)]
        public void Plan_WithFewerEmptyCellsThanTheBudget_PlansEveryEmptyCellInTheBand(double patternChance)
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            FillRows(grid, HalfRows, HalfRows + FilledBandRows, CellOwner.Opponent);
            var bounds = CreateHalfBounds(BoardRows, CellOwner.Opponent, GenerousBudget);
            var expectedCells = CollectEmptyCells(grid, bounds);
            var planner = CreatePlanner(PlanSeed, patternChance);

            var planned = planner.Plan(grid, bounds, GenerousBudget);

            Assert.That(expectedCells.Count, Is.LessThan(GenerousBudget), "The band has fewer empty cells than the budget.");
            Assert.That(planned, Is.EqualTo(expectedCells.Count), "Every empty cell in the band is planned.");
            Assert.That(planner.PlannedCells, Is.EquivalentTo(expectedCells), "Exactly the band's empty cells are planned.");
            AssertValidPlan(grid, bounds, planner, expectedCells.Count, "A nearly full band");
            if (patternChance > NeverPattern)
            {
                Assert.That(planner.PlannedPatternCount, Is.GreaterThan(0), "A pattern was planned, so its border cells had to be used too.");
            }
        }

        /// <summary>A budget beyond the band's empty cells plans exactly what a budget of every empty cell plans from the same seed, so pattern chances follow the cells actually planned, not the budget asked for.</summary>
        [Test]
        public void Plan_WithABudgetBeyondTheEmptyCells_PlansAsABudgetOfEveryEmptyCell()
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            FillRows(grid, HalfRows, HalfRows + FilledBandRows, CellOwner.Opponent);
            var bounds = CreateHalfBounds(BoardRows, CellOwner.Opponent, GenerousBudget);
            var emptyCellCount = CollectEmptyCells(grid, bounds).Count;
            var oversizedBudget = emptyCellCount + ExtraPatternChances * CellsPerPatternChance;
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var exact = CreatePlanner(seed, AlwaysPattern);
                var oversized = CreatePlanner(seed, AlwaysPattern);

                exact.Plan(grid, bounds, emptyCellCount);
                oversized.Plan(grid, bounds, oversizedBudget);

                Assert.That(Copy(oversized.PlannedCells), Is.EqualTo(Copy(exact.PlannedCells)), $"Seed {seed}: the planned cells.");
                Assert.That(oversized.PlannedPatternCount, Is.EqualTo(exact.PlannedPatternCount), $"Seed {seed}: the patterns planned.");
            }
        }

        /// <summary>A zero or negative budget plans nothing on an empty board.</summary>
        [TestCase(0)]
        [TestCase(NegativeBudget)]
        public void Plan_WithNoBudget_PlansNothing(int cellBudget)
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            var planner = CreatePlanner(PlanSeed, AlwaysPattern);

            var planned = planner.Plan(grid, PlacementBounds.WholeBoard(BoardRows, BoardColumns), cellBudget);

            Assert.That(planned, Is.Zero, "The returned count.");
            Assert.That(planner.PlannedCells, Is.Empty, "No cell is planned.");
            Assert.That(planner.PlannedPatternCount, Is.Zero, "No pattern is planned.");
        }

        /// <summary>A band with every cell alive plans nothing, even with empty cells just outside it.</summary>
        [Test]
        public void Plan_WithAFullBand_PlansNothing()
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            FillRows(grid, HalfRows, BoardRows, CellOwner.Opponent);
            var planner = CreatePlanner(PlanSeed, AlwaysPattern);

            var planned = planner.Plan(grid, CreateHalfBounds(BoardRows, CellOwner.Opponent, SetupSquares), SetupSquares);

            Assert.That(planned, Is.Zero, "The returned count.");
            Assert.That(planner.PlannedCells, Is.Empty, "No cell is planned.");
            Assert.That(planner.PlannedPatternCount, Is.Zero, "No pattern is planned.");
        }

        /// <summary>A budget with no limit, as Classic's whole-board square count would give, plans every empty cell once.</summary>
        [Test]
        public void Plan_WithAnUnlimitedBudget_PlansEveryEmptyCell()
        {
            var grid = CreateBoardWithLivingCells(PlanSeed);
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var emptyCellCount = grid.CountCells(CellOwner.None);
            var planner = CreatePlanner(PlanSeed, DefaultPatternChance);

            var planned = planner.Plan(grid, bounds, bounds.SquaresAvailable);

            Assert.That(planned, Is.EqualTo(emptyCellCount), "Every empty cell is planned.");
            AssertValidPlan(grid, bounds, planner, emptyCellCount, "An unlimited budget");
        }

        /// <summary>With a pattern chance for every cell, each small budget is met exactly, so a pattern bigger than the cells left is never chosen.</summary>
        [Test]
        public void Plan_WithSmallBudgets_NeverPlansMoreThanTheBudget()
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            for (var budget = 1; budget <= LargestTestPatternCells + 1; budget++)
            {
                for (var seed = 0; seed < SmallBudgetSeedsTried; seed++)
                {
                    var planner = CreatePlanner(seed, AlwaysPattern, PatternChanceEveryCell);

                    var planned = planner.Plan(grid, bounds, budget);

                    Assert.That(planned, Is.EqualTo(budget), $"Seed {seed}, budget {budget}: the returned count.");
                    AssertValidPlan(grid, bounds, planner, budget, $"Seed {seed}, budget {budget}");
                }
            }
        }

        /// <summary>With no pattern chance, no seed plans a pattern.</summary>
        [Test]
        public void Plan_WithNoPatternChance_PlansNoPattern()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var grid = CreateBoardWithLivingCells(seed);
                var planner = CreatePlanner(seed, NeverPattern, PatternChanceEveryCell);

                planner.Plan(grid, bounds, ClassicCellBudget);

                Assert.That(planner.PlannedPatternCount, Is.Zero, $"Seed {seed}: patterns planned.");
            }
        }

        /// <summary>With a certain pattern chance on a roomy board, every plan starts with one orientation of a catalogue shape that no living or other planned cell touches, and the shapes vary between seeds.</summary>
        [Test]
        public void Plan_WithACertainPatternChance_StartsWithACatalogueShapeKeptClear()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var shapesSeen = new HashSet<string>();
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var context = $"Seed {seed}";
                var grid = CreateRoomyBoard();
                var planner = CreatePlanner(seed, AlwaysPattern);

                planner.Plan(grid, bounds, RoomyBudget);

                Assert.That(planner.PlannedPatternCount, Is.GreaterThanOrEqualTo(1), $"{context}: patterns planned.");
                var firstPattern = FindPatternAt(grid, planner.PlannedCells, 0, out var shape);
                Assert.That(firstPattern, Is.Not.Null, $"{context}: the first planned cells form one orientation of a catalogue shape.");
                AssertNothingElseTouches(grid, planner.PlannedCells, firstPattern, context);
                AssertValidPlan(grid, bounds, planner, RoomyBudget, context);
                shapesSeen.Add(shape.Name);
            }

            Assert.That(shapesSeen.Count, Is.GreaterThan(1), "Different seeds choose different shapes.");
        }

        /// <summary>No pattern touches a living cell, including the other side's cells just across the half line, or another pattern.</summary>
        [TestCase(CellOwner.Player)]
        [TestCase(CellOwner.Opponent)]
        public void Plan_Patterns_NeverTouchLivingCellsEvenAcrossTheHalfLine(CellOwner side)
        {
            var bounds = CreateHalfBounds(SmallBoardRows, side, SetupSquares);
            var patternsPlanned = 0;
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var context = $"Seed {seed}, {side}'s half";
                var grid = CreateSmallBoardWithTheOtherHalfLineAlive(side);
                var planner = new RandomPlacementPlanner(grid.MaximumCellCount, Shapes, new Random(seed), PatternChanceEveryThreeCells, AlwaysPattern);

                planner.Plan(grid, bounds, SetupSquares);

                AssertValidPlan(grid, bounds, planner, SetupSquares, context);
                AssertPatternsKeepClear(grid, SplitPlannedPatterns(grid, planner, context), context);
                patternsPlanned += planner.PlannedPatternCount;
            }

            Assert.That(patternsPlanned, Is.GreaterThan(0), "Some patterns were planned, so the check is not vacuous.");
        }

        /// <summary>While there is room, single cells keep off the border around every planned pattern.</summary>
        [Test]
        public void Plan_SingleCells_AvoidPatternBordersWhileThereIsRoom()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var context = $"Seed {seed}";
                var grid = CreateRoomyBoard();
                var planner = CreatePlanner(seed, AlwaysPattern);

                planner.Plan(grid, bounds, RoomyBudget);

                var patterns = SplitPlannedPatterns(grid, planner, context);
                Assert.That(patterns, Is.Not.Empty, $"{context}: patterns planned.");
                AssertPatternsKeepClear(grid, patterns, context);
                AssertSinglesKeepClearOfPatterns(grid, planner, patterns, context);
            }
        }

        /// <summary>In a band two rows deep only patterns that fit are planned, turned flat when needed, and the oversized Pulsar is never tried.</summary>
        [Test]
        public void Plan_InAShallowBand_OnlyPlansPatternsThatFit()
        {
            var bounds = new PlacementBounds(0, ShallowBandRows, BoardRows, BoardColumns, ShallowBandBudget);
            var patternsPlanned = 0;
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var context = $"Seed {seed}";
                var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
                var planner = CreatePlanner(seed, AlwaysPattern, PatternChanceEveryThreeCells);

                planner.Plan(grid, bounds, ShallowBandBudget);

                AssertValidPlan(grid, bounds, planner, ShallowBandBudget, context);
                var position = 0;
                for (var pattern = 0; pattern < planner.PlannedPatternCount; pattern++)
                {
                    var cells = FindPatternAt(grid, planner.PlannedCells, position, out var shape);
                    Assert.That(cells, Is.Not.Null, $"{context}: pattern {pattern} is a catalogue shape.");
                    Assert.That(ShallowBandShapeNames, Does.Contain(shape.Name), $"{context}: pattern {pattern} fits two rows.");
                    position += cells.Length;
                }

                patternsPlanned += planner.PlannedPatternCount;
            }

            Assert.That(patternsPlanned, Is.GreaterThan(0), "Some patterns were planned in the shallow band.");
        }

        /// <summary>Two planners with the same seed plan the same cells in the same order on the same board.</summary>
        [Test]
        public void Plan_WithTheSameSeed_PlansTheSameCells()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var grid = CreateBoardWithLivingCells(seed);
                var first = CreatePlanner(seed, DefaultPatternChance);
                var second = CreatePlanner(seed, DefaultPatternChance);

                first.Plan(grid, bounds, ClassicCellBudget);
                second.Plan(grid, bounds, ClassicCellBudget);

                Assert.That(Copy(second.PlannedCells), Is.EqualTo(Copy(first.PlannedCells)), $"Seed {seed}: the planned cells.");
                Assert.That(second.PlannedPatternCount, Is.EqualTo(first.PlannedPatternCount), $"Seed {seed}: the patterns planned.");
            }
        }

        /// <summary>Planning again from the same seed on the same board repeats the plan exactly.</summary>
        [Test]
        public void Plan_AgainFromTheSameSeed_RepeatsThePlan()
        {
            var grid = CreateBoardWithLivingCells(PlanSeed);
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var random = new ReseedableRandom(PlanSeed);
            var planner = new RandomPlacementPlanner(BoardCellCount, Shapes, random, CellsPerPatternChance, AlwaysPattern);
            planner.Plan(grid, bounds, ClassicCellBudget);
            var firstCells = Copy(planner.PlannedCells);
            var firstPatternCount = planner.PlannedPatternCount;

            random.Reseed(PlanSeed);
            planner.Plan(grid, bounds, ClassicCellBudget);

            Assert.That(firstPatternCount, Is.GreaterThan(0), "The first plan included patterns.");
            Assert.That(Copy(planner.PlannedCells), Is.EqualTo(firstCells), "The planned cells.");
            Assert.That(planner.PlannedPatternCount, Is.EqualTo(firstPatternCount), "The patterns planned.");
        }

        /// <summary>A plan on an emptied board after a plan on a busy one matches a fresh planner's plan from the same seed, so nothing carries over between plans.</summary>
        [Test]
        public void Plan_OnAnEmptyBoardAfterABusyOne_MatchesAFreshPlanner()
        {
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var grid = CreateBoardWithLivingCells(seed);
                var random = new ReseedableRandom(seed);
                var reused = new RandomPlacementPlanner(BoardCellCount, Shapes, random, CellsPerPatternChance, AlwaysPattern);
                reused.Plan(grid, bounds, ClassicCellBudget);
                grid.KillAllCells();
                random.Reseed(EmptyBoardSeed);
                var fresh = new RandomPlacementPlanner(BoardCellCount, Shapes, new Random(EmptyBoardSeed), CellsPerPatternChance, AlwaysPattern);

                reused.Plan(grid, bounds, ClassicCellBudget);
                fresh.Plan(grid, bounds, ClassicCellBudget);

                Assert.That(Copy(reused.PlannedCells), Is.EqualTo(Copy(fresh.PlannedCells)), $"Seed {seed}: the planned cells.");
                Assert.That(reused.PlannedPatternCount, Is.EqualTo(fresh.PlannedPatternCount), $"Seed {seed}: the patterns planned.");
            }
        }

        /// <summary>After planning on a busy board, a plan on the emptied board with a budget of every cell plans every cell, so no cell stays taken.</summary>
        [Test]
        public void Plan_EveryCellAfterABusyBoard_PlansEveryCell()
        {
            var grid = CreateBoardWithLivingCells(PlanSeed);
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var planner = CreatePlanner(PlanSeed, AlwaysPattern);
            planner.Plan(grid, bounds, ClassicCellBudget);
            grid.KillAllCells();

            var planned = planner.Plan(grid, bounds, BoardCellCount);

            Assert.That(planned, Is.EqualTo(BoardCellCount), "Every cell of the emptied board is planned.");
            AssertValidPlan(grid, bounds, planner, BoardCellCount, "The emptied board");
        }

        /// <summary>Over many seeds, each side's random plan spends every setup square in one move the match accepts, completing setup.</summary>
        [Test]
        public void VersusSetup_RandomMoveForEachSide_IsAcceptedAndSpendsEverySquare()
        {
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var match = CreateMatch(out var grid);
                match.Begin(new VersusMatchRules(SetupSquares, NoMatchSquares));
                var planner = CreatePlanner(seed, DefaultPatternChance);

                PlanAndPlaceSetupMove(match, grid, planner, CellOwner.Player, $"Seed {seed}, Player");
                PlanAndPlaceSetupMove(match, grid, planner, CellOwner.Opponent, $"Seed {seed}, Opponent");

                Assert.That(match.IsSetupComplete, Is.True, $"Seed {seed}: setup is complete.");
            }
        }

        /// <summary>After single squares on both halves, hugging the half line, each side's random plan keeps its patterns clear of them and the match accepts it, spending every square left.</summary>
        [Test]
        public void VersusSetup_RandomMoveAfterSquaresOnBothHalves_IsAcceptedAndSpendsEverySquareLeft()
        {
            var patternsPlanned = 0;
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                var match = CreateMatch(out var grid);
                match.Begin(new VersusMatchRules(SetupSquares, NoMatchSquares));
                foreach (var (side, row, column) in SquaresPlacedFirst)
                {
                    Assert.That(match.TryPlace(side, grid.ToCellIndex(row, column)), Is.True, $"Seed {seed}: {side} places on ({row}, {column}).");
                }

                var planner = CreatePlanner(seed, AlwaysPattern);

                patternsPlanned += PlanAndPlaceSetupMove(match, grid, planner, CellOwner.Player, $"Seed {seed}, Player");
                patternsPlanned += PlanAndPlaceSetupMove(match, grid, planner, CellOwner.Opponent, $"Seed {seed}, Opponent");

                Assert.That(match.IsSetupComplete, Is.True, $"Seed {seed}: setup is complete.");
            }

            Assert.That(patternsPlanned, Is.GreaterThan(0), "Some patterns were planned, so the clearance check is not vacuous.");
        }

        /// <summary>Planning without a grid is refused.</summary>
        [Test]
        public void Plan_WithANullGrid_Throws()
        {
            var planner = CreatePlanner(PlanSeed, DefaultPatternChance);

            Assert.Throws<ArgumentNullException>(() => planner.Plan(null, PlacementBounds.WholeBoard(BoardRows, BoardColumns), SetupSquares));
        }

        /// <summary>Bounds made for a board of another size are refused, whether the rows or the columns differ.</summary>
        [Test]
        public void Plan_WithBoundsForAnotherBoardSize_Throws()
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            var planner = CreatePlanner(PlanSeed, DefaultPatternChance);

            Assert.Throws<ArgumentException>(() => planner.Plan(grid, PlacementBounds.WholeBoard(BoardRows + 1, BoardColumns), SetupSquares), "Bounds with one row too many.");
            Assert.Throws<ArgumentException>(() => planner.Plan(grid, PlacementBounds.WholeBoard(BoardRows, BoardColumns - 1), SetupSquares), "Bounds with one column too few.");
            Assert.Throws<ArgumentException>(() => planner.Plan(grid, new PlacementBounds(0, HalfRows, HalfRows, BoardColumns, SetupSquares), SetupSquares), "A half described as a whole board.");
        }

        /// <summary>A grid with more cells than the planner was sized for is refused.</summary>
        [Test]
        public void Plan_WithAGridBiggerThanItsBuffers_Throws()
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            var planner = new RandomPlacementPlanner(BoardCellCount - 1, Shapes, new Random(PlanSeed), CellsPerPatternChance, DefaultPatternChance);

            Assert.Throws<ArgumentException>(() => planner.Plan(grid, PlacementBounds.WholeBoard(BoardRows, BoardColumns), SetupSquares));
        }

        /// <summary>Negative sizes, fewer than one cell per pattern chance, chances outside 0 to 1 and missing patterns or randomness are refused; the chance limits themselves are allowed.</summary>
        [Test]
        public void Constructor_WithInvalidArguments_Throws()
        {
            var random = new Random(PlanSeed);

            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomPlacementPlanner(-1, Shapes, random, CellsPerPatternChance, DefaultPatternChance), "A negative cell count.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomPlacementPlanner(BoardCellCount, Shapes, random, 0, DefaultPatternChance), "No cells per pattern chance.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomPlacementPlanner(BoardCellCount, Shapes, random, CellsPerPatternChance, -0.1d), "A chance below 0.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomPlacementPlanner(BoardCellCount, Shapes, random, CellsPerPatternChance, 1.1d), "A chance above 1.");
            Assert.Throws<ArgumentNullException>(() => new RandomPlacementPlanner(BoardCellCount, null, random, CellsPerPatternChance, DefaultPatternChance), "No patterns.");
            Assert.Throws<ArgumentNullException>(() => new RandomPlacementPlanner(BoardCellCount, Shapes, null, CellsPerPatternChance, DefaultPatternChance), "No randomness.");
            Assert.DoesNotThrow(() => new RandomPlacementPlanner(0, Shapes, random, PatternChanceEveryCell, NeverPattern), "No cells, one cell per chance and a chance of 0.");
            Assert.DoesNotThrow(() => new RandomPlacementPlanner(BoardCellCount, Array.Empty<CellPattern>(), random, CellsPerPatternChance, AlwaysPattern), "No patterns at all and a chance of 1.");
        }

        /// <summary>With an empty catalogue a certain pattern chance still plans the whole budget as single cells.</summary>
        [Test]
        public void Plan_WithNoCataloguePatterns_PlansOnlySingleCells()
        {
            var grid = CreateBoardWithLivingCells(PlanSeed);
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var planner = new RandomPlacementPlanner(BoardCellCount, Array.Empty<CellPattern>(), new Random(PlanSeed), PatternChanceEveryCell, AlwaysPattern);

            var planned = planner.Plan(grid, bounds, ClassicCellBudget);

            Assert.That(planned, Is.EqualTo(ClassicCellBudget), "The returned count.");
            Assert.That(planner.PlannedPatternCount, Is.Zero, "No pattern is planned.");
            AssertValidPlan(grid, bounds, planner, ClassicCellBudget, "An empty catalogue");
        }

        /// <summary>Planning on a busy board allocates nothing once warmed up.</summary>
        [Test]
        public void Plan_DoesNotAllocate()
        {
            var grid = CreateBoardWithLivingCells(PlanSeed);
            var bounds = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var planner = CreatePlanner(PlanSeed, AlwaysPattern);
            planner.Plan(grid, bounds, ClassicCellBudget);

            Assert.That(() =>
            {
                planner.Plan(grid, bounds, ClassicCellBudget);
            }, Is.Not.AllocatingGCMemory());
        }

        /// <summary>Classic's whole-board bounds have no square limit, so Random spends a share of the board instead.</summary>
        [Test]
        public void HasSquareLimit_ForTheWholeBoard_IsFalse()
        {
            Assert.That(PlacementBounds.WholeBoard(BoardRows, BoardColumns).HasSquareLimit, Is.False, "Classic's whole board.");
        }

        /// <summary>Bounds with a finite square count have a limit, even when they span every row or no squares are left.</summary>
        [Test]
        public void HasSquareLimit_ForBoundsWithAFiniteSquareCount_IsTrue()
        {
            Assert.That(CreateHalfBounds(BoardRows, CellOwner.Player, SetupSquares).HasSquareLimit, Is.True, "The Player's half.");
            Assert.That(CreateHalfBounds(BoardRows, CellOwner.Opponent, SetupSquares).HasSquareLimit, Is.True, "The Opponent's half.");
            Assert.That(new PlacementBounds(0, BoardRows, BoardRows, BoardColumns, SetupSquares).HasSquareLimit, Is.True, "Every row with a finite count.");
            Assert.That(CreateHalfBounds(BoardRows, CellOwner.Player, 0).HasSquareLimit, Is.True, "A half with no squares left.");
        }

        /// <summary>HasEmptyCell finds an empty cell only inside the band: false once every band cell is alive, even with empty cells elsewhere, and true again when one band cell empties.</summary>
        [Test]
        public void HasEmptyCell_LooksOnlyInsideTheBand()
        {
            var grid = new CellGrid(BoardRows, BoardColumns);
            grid.Resize(BoardRows, BoardColumns);
            var playerHalf = CreateHalfBounds(BoardRows, CellOwner.Player, SetupSquares);
            Assert.That(RandomPlacementPlanner.HasEmptyCell(grid, playerHalf), Is.True, "An empty half has empty cells.");

            for (var row = playerHalf.FirstRow; row < playerHalf.EndRowExclusive; row++)
            {
                for (var column = 0; column < BoardColumns; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), CellOwner.Player);
                }
            }

            Assert.That(RandomPlacementPlanner.HasEmptyCell(grid, playerHalf), Is.False, "A full half has no empty cell, whatever the other half holds.");
            Assert.That(RandomPlacementPlanner.HasEmptyCell(grid, PlacementBounds.WholeBoard(BoardRows, BoardColumns)), Is.True, "The other half is still empty.");

            var lastBandCell = grid.ToCellIndex(playerHalf.EndRowExclusive - 1, BoardColumns - 1);
            grid.SetOwner(lastBandCell, CellOwner.None);
            Assert.That(RandomPlacementPlanner.HasEmptyCell(grid, playerHalf), Is.True, "Emptying the band's last cell gives it an empty cell again.");
        }

        /// <summary>Builds a planner for the default board over the test catalogue shapes with a seeded random source.</summary>
        private static RandomPlacementPlanner CreatePlanner(int seed, double patternChance, int cellsPerPatternChance = CellsPerPatternChance)
        {
            return new RandomPlacementPlanner(BoardCellCount, Shapes, new Random(seed), cellsPerPatternChance, patternChance);
        }

        /// <summary>Returns the bounds of a side's Versus setup half on a board of the given rows: the top half for the Player, the rest for the Opponent.</summary>
        private static PlacementBounds CreateHalfBounds(int boardRows, CellOwner side, int squaresAvailable)
        {
            var halfwayRow = boardRows / 2;
            return side == CellOwner.Player
                ? new PlacementBounds(0, halfwayRow, boardRows, BoardColumns, squaresAvailable)
                : new PlacementBounds(halfwayRow, boardRows, boardRows, BoardColumns, squaresAvailable);
        }

        /// <summary>Returns the default board with about a fifth of its cells alive in random colours, placed from the seed, and no changes recorded.</summary>
        private static CellGrid CreateBoardWithLivingCells(int seed)
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            var random = new Random(seed + ScatterSeedOffset);
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                if (random.NextDouble() < LivingShare)
                {
                    grid.SetOwner(cellIndex, LivingOwners[random.Next(LivingOwners.Length)]);
                }
            }

            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Returns the default board with a few living cells far apart, leaving lots of room for patterns.</summary>
        private static CellGrid CreateRoomyBoard()
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            for (var index = 0; index < RoomyBoardLivingCells.Length; index++)
            {
                var (row, column) = RoomyBoardLivingCells[index];
                grid.SetOwner(grid.ToCellIndex(row, column), LivingOwners[index % LivingOwners.Length]);
            }

            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Returns a small board whose other side's row at the half line is fully alive, plus two of the side's own cells in its half.</summary>
        private static CellGrid CreateSmallBoardWithTheOtherHalfLineAlive(CellOwner side)
        {
            var grid = OpponentTestBoards.CreateEmpty(SmallBoardRows, BoardColumns);
            var isPlayer = side == CellOwner.Player;
            var otherSide = isPlayer ? CellOwner.Opponent : CellOwner.Player;
            var otherHalfLineRow = isPlayer ? SmallBoardHalfRows : SmallBoardHalfRows - 1;
            var ownMiddleRow = isPlayer ? 1 : SmallBoardRows - 2;
            FillRows(grid, otherHalfLineRow, otherHalfLineRow + 1, otherSide);
            grid.SetOwner(grid.ToCellIndex(ownMiddleRow, BoardColumns / 3), side);
            grid.SetOwner(grid.ToCellIndex(ownMiddleRow, 2 * BoardColumns / 3), side);
            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Gives every cell in the rows to the owner and clears the change list.</summary>
        private static void FillRows(CellGrid grid, int firstRow, int endRowExclusive, CellOwner owner)
        {
            for (var row = firstRow; row < endRowExclusive; row++)
            {
                for (var column = 0; column < grid.Columns; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), owner);
                }
            }

            grid.ClearChangedCells();
        }

        /// <summary>Returns every empty cell inside the bounds' rows.</summary>
        private static List<int> CollectEmptyCells(CellGrid grid, in PlacementBounds bounds)
        {
            var cells = new List<int>();
            for (var row = bounds.FirstRow; row < bounds.EndRowExclusive; row++)
            {
                for (var column = 0; column < bounds.Columns; column++)
                {
                    var cellIndex = grid.ToCellIndex(row, column);
                    if (!grid.IsAlive(cellIndex))
                    {
                        cells.Add(cellIndex);
                    }
                }
            }

            return cells;
        }

        /// <summary>Returns a copy of every cell's owner.</summary>
        private static CellOwner[] CopyOwners(CellGrid grid)
        {
            var owners = new CellOwner[grid.CellCount];
            for (var cellIndex = 0; cellIndex < owners.Length; cellIndex++)
            {
                owners[cellIndex] = grid.GetOwner(cellIndex);
            }

            return owners;
        }

        /// <summary>Returns a copy of the cells, so a later plan cannot change them.</summary>
        private static int[] Copy(IReadOnlyList<int> cells)
        {
            var copy = new int[cells.Count];
            for (var position = 0; position < copy.Length; position++)
            {
                copy[position] = cells[position];
            }

            return copy;
        }

        /// <summary>Asserts the plan lists the expected number of cells, each once, all empty on the board and inside the bounds' rows.</summary>
        private static void AssertValidPlan(CellGrid grid, in PlacementBounds bounds, RandomPlacementPlanner planner, int expectedCount, string context)
        {
            var planned = planner.PlannedCells;
            Assert.That(planned.Count, Is.EqualTo(expectedCount), $"{context}: cells planned.");
            Assert.That(planned, Is.Unique, $"{context}: each cell is planned once.");
            for (var position = 0; position < planned.Count; position++)
            {
                var cellIndex = planned[position];
                Assert.That(cellIndex, Is.InRange(0, grid.CellCount - 1), $"{context}: planned cell {cellIndex} lies on the board.");
                Assert.That(grid.RowOf(cellIndex), Is.InRange(bounds.FirstRow, bounds.EndRowExclusive - 1), $"{context}: planned cell {DescribeCell(grid, cellIndex)} lies in the band.");
                Assert.That(grid.IsAlive(cellIndex), Is.False, $"{context}: planned cell {DescribeCell(grid, cellIndex)} was empty.");
            }
        }

        /// <summary>Splits the plan's leading cells into its whole patterns, failing if a run of cells forms no catalogue shape.</summary>
        private static List<int[]> SplitPlannedPatterns(CellGrid grid, RandomPlacementPlanner planner, string context)
        {
            var patterns = new List<int[]>();
            var position = 0;
            for (var pattern = 0; pattern < planner.PlannedPatternCount; pattern++)
            {
                var cells = FindPatternAt(grid, planner.PlannedCells, position, out _);
                Assert.That(cells, Is.Not.Null, $"{context}: the planned cells from position {position} form one orientation of a catalogue shape.");
                patterns.Add(cells);
                position += cells.Length;
            }

            return patterns;
        }

        /// <summary>Returns the run of planned cells from the position that forms an orientation of a catalogue shape, trying the largest shape first so a pattern's leading cells never pass for a smaller one, or null when none does.</summary>
        private static int[] FindPatternAt(CellGrid grid, IReadOnlyList<int> planned, int position, out CellPattern shape)
        {
            shape = null;
            var bestCellCount = 0;
            int[] bestCells = null;
            foreach (var candidate in Shapes)
            {
                if (candidate.CellCount <= bestCellCount || position + candidate.CellCount > planned.Count)
                {
                    continue;
                }

                var cells = new int[candidate.CellCount];
                for (var cell = 0; cell < cells.Length; cell++)
                {
                    cells[cell] = planned[position + cell];
                }

                if (FormsAnyOrientation(grid, cells, candidate))
                {
                    shape = candidate;
                    bestCellCount = candidate.CellCount;
                    bestCells = cells;
                }
            }

            return bestCells;
        }

        /// <summary>Returns whether the cells are exactly one of the shape's eight orientations, moved anywhere on the board.</summary>
        private static bool FormsAnyOrientation(CellGrid grid, int[] cells, CellPattern shape)
        {
            for (var poseIndex = 0; poseIndex < PoseCount; poseIndex++)
            {
                var pose = new PatternPose(poseIndex / 2, poseIndex % 2 == 1);
                if (FormsOrientation(grid, cells, shape.GetOrientation(pose)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns whether the cells are exactly the orientation with its box's top-left corner at the cells' top-left corner.</summary>
        private static bool FormsOrientation(CellGrid grid, int[] cells, PatternOrientation orientation)
        {
            if (cells.Length != orientation.CellCount)
            {
                return false;
            }

            var top = int.MaxValue;
            var left = int.MaxValue;
            foreach (var cellIndex in cells)
            {
                top = Math.Min(top, grid.RowOf(cellIndex));
                left = Math.Min(left, grid.ColumnOf(cellIndex));
            }

            var cellSet = new HashSet<int>(cells);
            for (var cell = 0; cell < orientation.CellCount; cell++)
            {
                var row = top + orientation.GetRowOffset(cell);
                var column = left + orientation.GetColumnOffset(cell);
                if (row >= grid.Rows || column >= grid.Columns || !cellSet.Contains(grid.ToCellIndex(row, column)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Asserts no living cell and no cell of another planned pattern touches any pattern cell, anywhere on the board.</summary>
        private static void AssertPatternsKeepClear(CellGrid grid, List<int[]> patterns, string context)
        {
            var patternOfCell = new Dictionary<int, int>();
            for (var pattern = 0; pattern < patterns.Count; pattern++)
            {
                foreach (var cellIndex in patterns[pattern])
                {
                    patternOfCell[cellIndex] = pattern;
                }
            }

            for (var pattern = 0; pattern < patterns.Count; pattern++)
            {
                foreach (var cellIndex in patterns[pattern])
                {
                    for (var slot = 0; slot < grid.GetNeighbourCount(cellIndex); slot++)
                    {
                        var neighbour = grid.GetNeighbourIndex(cellIndex, slot);
                        Assert.That(grid.IsAlive(neighbour), Is.False, $"{context}: pattern {pattern} cell {DescribeCell(grid, cellIndex)} touches living cell {DescribeCell(grid, neighbour)}.");
                        var touchesAnotherPattern = patternOfCell.TryGetValue(neighbour, out var otherPattern) && otherPattern != pattern;
                        Assert.That(touchesAnotherPattern, Is.False, $"{context}: pattern {pattern} cell {DescribeCell(grid, cellIndex)} touches pattern {otherPattern}.");
                    }
                }
            }
        }

        /// <summary>Asserts no living cell and no other planned cell touches any cell of the pattern.</summary>
        private static void AssertNothingElseTouches(CellGrid grid, IReadOnlyList<int> planned, int[] pattern, string context)
        {
            var patternCells = new HashSet<int>(pattern);
            var otherPlannedCells = new HashSet<int>(planned);
            otherPlannedCells.ExceptWith(patternCells);
            foreach (var cellIndex in pattern)
            {
                for (var slot = 0; slot < grid.GetNeighbourCount(cellIndex); slot++)
                {
                    var neighbour = grid.GetNeighbourIndex(cellIndex, slot);
                    Assert.That(grid.IsAlive(neighbour), Is.False, $"{context}: pattern cell {DescribeCell(grid, cellIndex)} touches living cell {DescribeCell(grid, neighbour)}.");
                    Assert.That(otherPlannedCells.Contains(neighbour), Is.False, $"{context}: pattern cell {DescribeCell(grid, cellIndex)} touches planned cell {DescribeCell(grid, neighbour)}.");
                }
            }
        }

        /// <summary>Asserts no single cell, planned after the patterns, touches a pattern cell.</summary>
        private static void AssertSinglesKeepClearOfPatterns(CellGrid grid, RandomPlacementPlanner planner, List<int[]> patterns, string context)
        {
            var patternCells = new HashSet<int>();
            foreach (var pattern in patterns)
            {
                patternCells.UnionWith(pattern);
            }

            var planned = planner.PlannedCells;
            for (var position = patternCells.Count; position < planned.Count; position++)
            {
                var cellIndex = planned[position];
                for (var slot = 0; slot < grid.GetNeighbourCount(cellIndex); slot++)
                {
                    var neighbour = grid.GetNeighbourIndex(cellIndex, slot);
                    Assert.That(patternCells.Contains(neighbour), Is.False, $"{context}: single cell {DescribeCell(grid, cellIndex)} touches pattern cell {DescribeCell(grid, neighbour)}.");
                }
            }
        }

        /// <summary>Describes a cell as its row and column.</summary>
        private static string DescribeCell(CellGrid grid, int cellIndex)
        {
            return $"({grid.RowOf(cellIndex)}, {grid.ColumnOf(cellIndex)})";
        }

        /// <summary>Builds a match on an empty default board, as VersusMatchTests does.</summary>
        private static VersusMatch CreateMatch(out CellGrid grid)
        {
            grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            return new VersusMatch(grid, new ClusterAbsorber(grid.MaximumCellCount));
        }

        /// <summary>Plans the side's move over its setup half and squares left, as the Versus mode's bounds give, checks the plan, places it as one setup move and returns how many patterns it held.</summary>
        private static int PlanAndPlaceSetupMove(VersusMatch match, CellGrid grid, RandomPlacementPlanner planner, CellOwner side, string context)
        {
            Assert.That(match.CanStillPlaceInSetup(side), Is.True, $"{context}: the side can still place.");
            var region = match.GetSetupRegion(side);
            var bounds = new PlacementBounds(region.FirstRow, region.EndRowExclusive, grid.Rows, grid.Columns, match.GetRemainingSetupSquares(side));
            Assert.That(bounds.HasSquareLimit, Is.True, $"{context}: Versus bounds have a square limit.");
            var squaresLeft = bounds.SquaresAvailable;

            var planned = planner.Plan(grid, bounds, squaresLeft);

            Assert.That(planned, Is.EqualTo(squaresLeft), $"{context}: every square left is planned.");
            AssertValidPlan(grid, bounds, planner, squaresLeft, context);
            AssertPatternsKeepClear(grid, SplitPlannedPatterns(grid, planner, context), context);
            Assert.That(match.TryPlaceSetupPattern(side, planner.PlannedCells), Is.True, $"{context}: the match accepts the plan as one setup move.");
            Assert.That(match.GetRemainingSetupSquares(side), Is.Zero, $"{context}: setup squares left.");
            return planner.PlannedPatternCount;
        }

        /// <summary>A random source that can restart from a new seed, so one planner can be asked to repeat what a fresh planner plans from that seed.</summary>
        private sealed class ReseedableRandom : Random
        {
            private Random source;

            /// <summary>Starts the sequence from the seed.</summary>
            public ReseedableRandom(int seed)
            {
                source = new Random(seed);
            }

            /// <summary>Restarts the sequence from the seed, exactly as a new Random with that seed would begin.</summary>
            public void Reseed(int seed)
            {
                source = new Random(seed);
            }

            /// <summary>Returns the next non-negative number from the current sequence.</summary>
            public override int Next()
            {
                return source.Next();
            }

            /// <summary>Returns the next number below the maximum from the current sequence.</summary>
            public override int Next(int maxValue)
            {
                return source.Next(maxValue);
            }

            /// <summary>Returns the next number in the range from the current sequence.</summary>
            public override int Next(int minValue, int maxValue)
            {
                return source.Next(minValue, maxValue);
            }

            /// <summary>Returns the next number between 0 and 1 from the current sequence.</summary>
            public override double NextDouble()
            {
                return source.NextDouble();
            }

            /// <summary>Fills the buffer from the current sequence.</summary>
            public override void NextBytes(byte[] buffer)
            {
                source.NextBytes(buffer);
            }

            /// <summary>Returns the next number between 0 and 1 from the current sequence.</summary>
            protected override double Sample()
            {
                return source.NextDouble();
            }
        }
    }
}

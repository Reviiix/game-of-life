using System;
using System.Collections.Generic;
using GameOfLife.Simulation;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks CellGrid's Conway rules in up to four colours (majority and missing-colour newborns, decided from the board before it changes), grid geometry and change tracking.</summary>
    [TestFixture]
    public sealed class CellGridTests
    {
        private const char EmptySymbol = '.';
        private const char PlayerSymbol = 'P';
        private const char OpponentSymbol = 'O';
        private const char ThirdSymbol = 'T';
        private const char FourthSymbol = 'F';
        private const char UnexpectedOwnerSymbol = '?';
        private const int RandomBoardSize = 20;
        private const int RandomSeed = 1234;
        private const double RandomFillChance = 0.35;
        private const int ManyGenerations = 100;
        private const int NeighboursNeededToSurviveMinimum = 2;
        private const int NeighboursNeededToSurviveMaximum = 3;
        private const int NeighboursNeededToBeBorn = 3;
        private const int ParentsNeededForAMajority = 2;

        /// <summary>Every colour a living cell can have.</summary>
        private static readonly CellOwner[] LivingOwners = { CellOwner.Player, CellOwner.Opponent, CellOwner.Third, CellOwner.Fourth };

        /// <summary>Versus's two colours.</summary>
        private static readonly CellOwner[] VersusOwners = { CellOwner.Player, CellOwner.Opponent };

        /// <summary>A blinker flips between horizontal and vertical every generation.</summary>
        [Test]
        public void Blinker_OscillatesWithPeriodTwo()
        {
            var grid = CreateGrid(
                ".....",
                ".....",
                ".PPP.",
                ".....",
                ".....");

            grid.AdvanceGeneration();
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                "..P..",
                "..P..",
                "..P..",
                "....."
            }));

            grid.AdvanceGeneration();
            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".....",
                ".PPP.",
                ".....",
                "....."
            }));
        }

        /// <summary>A block is a still life and never changes.</summary>
        [Test]
        public void Block_StaysStill()
        {
            var expected = new[]
            {
                "....",
                ".PP.",
                ".PP.",
                "...."
            };
            var grid = CreateGrid(expected);

            for (var generation = 0; generation < 5; generation++)
            {
                grid.AdvanceGeneration();
            }

            Assert.That(DescribeGrid(grid), Is.EqualTo(expected));
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>A glider reappears one cell down and one cell right after four generations.</summary>
        [Test]
        public void Glider_MovesOneCellDiagonallyAfterFourGenerations()
        {
            var grid = CreateGrid(
                ".P....",
                "..P...",
                "PPP...",
                "......",
                "......",
                "......");

            for (var generation = 0; generation < 4; generation++)
            {
                grid.AdvanceGeneration();
            }

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "......",
                "..P...",
                "...P..",
                ".PPP..",
                "......",
                "......"
            }));
        }

        /// <summary>A blinker on the top edge loses its upper birth instead of wrapping to the bottom row.</summary>
        [Test]
        public void TopAndBottomEdges_DoNotWrap()
        {
            var grid = CreateGrid(
                ".PPP.",
                ".....",
                ".....",
                ".....",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                "..P..",
                "..P..",
                ".....",
                ".....",
                "....."
            }));
        }

        /// <summary>A blinker on the left edge loses its left birth instead of wrapping to the right column.</summary>
        [Test]
        public void LeftAndRightEdges_DoNotWrap()
        {
            var grid = CreateGrid(
                ".....",
                "P....",
                "P....",
                "P....",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".....",
                "PP...",
                ".....",
                "....."
            }));
        }

        /// <summary>Corner cells have three neighbours, edge cells five and inner cells eight.</summary>
        [Test]
        public void GetNeighbourCount_IsThreeInCornersFiveOnEdgesAndEightInside()
        {
            var grid = CreateEmptyGrid(4, 5);

            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(0, 0)), Is.EqualTo(3));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(3, 4)), Is.EqualTo(3));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(0, 2)), Is.EqualTo(5));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(2, 0)), Is.EqualTo(5));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(1, 1)), Is.EqualTo(8));
        }

        /// <summary>The neighbour slots of a corner cell list exactly its three in-bounds neighbours.</summary>
        [Test]
        public void GetNeighbourIndex_ListsOnlyInBoundsNeighbours()
        {
            var grid = CreateEmptyGrid(3, 3);
            var cornerIndex = grid.ToCellIndex(0, 0);
            var neighbours = new int[grid.GetNeighbourCount(cornerIndex)];

            for (var neighbourSlot = 0; neighbourSlot < neighbours.Length; neighbourSlot++)
            {
                neighbours[neighbourSlot] = grid.GetNeighbourIndex(cornerIndex, neighbourSlot);
            }

            Assert.That(neighbours, Is.EquivalentTo(new[] { grid.ToCellIndex(0, 1), grid.ToCellIndex(1, 0), grid.ToCellIndex(1, 1) }));
        }

        /// <summary>Row, column and flat index conversions agree with each other.</summary>
        [Test]
        public void RowOfAndColumnOf_InvertToCellIndex()
        {
            var grid = CreateEmptyGrid(4, 7);
            var cellIndex = grid.ToCellIndex(3, 5);

            Assert.That(cellIndex, Is.EqualTo(26));
            Assert.That(grid.RowOf(cellIndex), Is.EqualTo(3));
            Assert.That(grid.ColumnOf(cellIndex), Is.EqualTo(5));
        }

        /// <summary>A newborn takes the colour of the majority of its three parents.</summary>
        [Test]
        public void NewbornCell_TakesMajorityColourOfItsParents()
        {
            var playerMajorityGrid = CreateGrid(
                ".....",
                ".....",
                ".PPO.",
                ".....",
                ".....");
            var opponentMajorityGrid = CreateGrid(
                ".....",
                ".....",
                ".OOP.",
                ".....",
                ".....");

            playerMajorityGrid.AdvanceGeneration();
            opponentMajorityGrid.AdvanceGeneration();

            Assert.That(DescribeGrid(playerMajorityGrid), Is.EqualTo(new[]
            {
                ".....",
                "..P..",
                "..P..",
                "..P..",
                "....."
            }));
            Assert.That(DescribeGrid(opponentMajorityGrid), Is.EqualTo(new[]
            {
                ".....",
                "..O..",
                "..O..",
                "..O..",
                "....."
            }));
        }

        /// <summary>A surviving cell keeps its colour even when its neighbours are the other colour.</summary>
        [Test]
        public void SurvivingCell_KeepsItsColour()
        {
            var grid = CreateGrid(
                ".....",
                ".....",
                ".POP.",
                ".....",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                "..P..",
                "..O..",
                "..P..",
                "....."
            }));
        }

        /// <summary>A board of only Player cells never produces an Opponent cell, so classic mode is unaffected by colours.</summary>
        [Test]
        public void PlayerOnlyBoard_NeverProducesOpponentCells()
        {
            var grid = CreateEmptyGrid(RandomBoardSize, RandomBoardSize);
            var random = new Random(RandomSeed);
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                if (random.NextDouble() < RandomFillChance)
                {
                    grid.SetOwner(cellIndex, CellOwner.Player);
                }
            }

            for (var generation = 0; generation < ManyGenerations; generation++)
            {
                grid.AdvanceGeneration();
                Assert.That(grid.CountCells(CellOwner.Opponent), Is.Zero);
            }
        }

        /// <summary>Counting by owner counts living cells per side and empty cells for None.</summary>
        [Test]
        public void CountCells_CountsEachOwnerSeparately()
        {
            var grid = CreateGrid(
                "PP.",
                ".O.",
                "..P");

            Assert.That(grid.CountCells(CellOwner.Player), Is.EqualTo(3));
            Assert.That(grid.CountCells(CellOwner.Opponent), Is.EqualTo(1));
            Assert.That(grid.CountCells(CellOwner.None), Is.EqualTo(5));
            Assert.That(grid.HasAnyLivingCell(), Is.True);
        }

        /// <summary>Setting a cell to the owner it already has records nothing.</summary>
        [Test]
        public void SetOwner_ToSameOwner_RecordsNoChange()
        {
            var grid = CreateEmptyGrid(3, 3);

            grid.SetOwner(4, CellOwner.None);
            Assert.That(grid.ChangedCellIndices, Is.Empty);

            grid.SetOwner(4, CellOwner.Opponent);
            grid.ClearChangedCells();
            grid.SetOwner(4, CellOwner.Opponent);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>A cell that changes several times appears once, in the order of its first change.</summary>
        [Test]
        public void ChangedCellIndices_ListsEachCellOnceInOrderOfFirstChange()
        {
            var grid = CreateEmptyGrid(3, 3);

            grid.SetOwner(5, CellOwner.Player);
            grid.SetOwner(2, CellOwner.Player);
            grid.SetOwner(5, CellOwner.Opponent);
            grid.SetOwner(5, CellOwner.None);

            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 5, 2 }));
        }

        /// <summary>Changes from placements and generations accumulate without duplicates until the caller clears them.</summary>
        [Test]
        public void ChangedCellIndices_AccumulatesAcrossGenerationsUntilCleared()
        {
            var grid = CreateEmptyGrid(5, 5);
            grid.SetOwner(grid.ToCellIndex(2, 1), CellOwner.Player);
            grid.SetOwner(grid.ToCellIndex(2, 2), CellOwner.Player);
            grid.SetOwner(grid.ToCellIndex(2, 3), CellOwner.Player);
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 11, 12, 13 }));

            grid.AdvanceGeneration();
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 11, 12, 13, 7, 17 }));

            grid.AdvanceGeneration();
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 11, 12, 13, 7, 17 }));

            grid.ClearChangedCells();
            Assert.That(grid.ChangedCellIndices, Is.Empty);

            grid.AdvanceGeneration();
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[] { 7, 11, 13, 17 }));
        }

        /// <summary>Killing every cell empties the board and the change list.</summary>
        [Test]
        public void KillAllCells_EmptiesBoardAndChangeList()
        {
            var grid = CreateGrid(
                "PO.",
                ".P.",
                "..O");
            grid.SetOwner(2, CellOwner.Player);

            grid.KillAllCells();

            Assert.That(grid.HasAnyLivingCell(), Is.False);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
        }

        /// <summary>Resizing changes the dimensions, kills every cell, rebuilds neighbours and clears the change list.</summary>
        [Test]
        public void Resize_KillsCellsRebuildsNeighboursAndClearsChanges()
        {
            var grid = new CellGrid(6, 6);
            grid.Resize(6, 6);
            grid.SetOwner(grid.ToCellIndex(1, 1), CellOwner.Player);

            grid.Resize(3, 4);

            Assert.That(grid.Rows, Is.EqualTo(3));
            Assert.That(grid.Columns, Is.EqualTo(4));
            Assert.That(grid.CellCount, Is.EqualTo(12));
            Assert.That(grid.MaximumCellCount, Is.EqualTo(36));
            Assert.That(grid.HasAnyLivingCell(), Is.False);
            Assert.That(grid.ChangedCellIndices, Is.Empty);
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(0, 3)), Is.EqualTo(3));
            Assert.That(grid.GetNeighbourCount(grid.ToCellIndex(1, 2)), Is.EqualTo(8));
        }

        /// <summary>Resizing beyond the preallocated maximum is rejected.</summary>
        [Test]
        public void Resize_BeyondMaximum_Throws()
        {
            var grid = new CellGrid(4, 4);

            Assert.Throws<ArgumentException>(() => grid.Resize(5, 4));
        }

        /// <summary>A copy matches its source's dimensions and cells, records no changes and evolves identically.</summary>
        [Test]
        public void CopyFrom_CopiesDimensionsCellsAndNeighboursWithoutRecordingChanges()
        {
            var source = CreateGrid(
                ".....",
                ".POP.",
                "..O..",
                ".P...",
                ".....");
            var copy = new CellGrid(6, 6);
            copy.Resize(6, 6);
            copy.SetOwner(0, CellOwner.Opponent);

            copy.CopyFrom(source);

            Assert.That(copy.Rows, Is.EqualTo(5));
            Assert.That(copy.Columns, Is.EqualTo(5));
            Assert.That(DescribeGrid(copy), Is.EqualTo(DescribeGrid(source)));
            Assert.That(copy.ChangedCellIndices, Is.Empty);

            for (var generation = 0; generation < 3; generation++)
            {
                source.AdvanceGeneration();
                copy.AdvanceGeneration();
            }

            Assert.That(DescribeGrid(copy), Is.EqualTo(DescribeGrid(source)));
        }

        /// <summary>A copy into a bigger grid full of stale cells uses the source's shape and never reads the stale cells past the copied area.</summary>
        [Test]
        public void CopyFrom_IntoALargerDirtyGrid_IgnoresCellsPastTheCopiedArea()
        {
            var source = CreateGrid(
                "........",
                ".PPP....",
                "........");
            var copy = CreateEmptyGrid(6, 6);
            for (var cellIndex = 0; cellIndex < copy.CellCount; cellIndex++)
            {
                copy.SetOwner(cellIndex, CellOwner.Opponent);
            }

            copy.CopyFrom(source);
            copy.AdvanceGeneration();

            Assert.That(DescribeGrid(copy), Is.EqualTo(new[]
            {
                "..P.....",
                "..P.....",
                "..P....."
            }));
            Assert.That(copy.CountCells(CellOwner.Opponent), Is.Zero);
        }

        /// <summary>Copying repeatedly from same-shaped and differently shaped sources keeps the neighbour table matching each source.</summary>
        [Test]
        public void CopyFrom_SwitchingBetweenShapes_KeepsNeighboursMatchingEachSource()
        {
            var wideSource = CreateGrid(
                "......",
                "PPP...",
                "......");
            var tallSource = CreateGrid(
                "..O",
                "..O",
                "..O",
                "...",
                "...",
                "...");
            var copy = new CellGrid(6, 6);

            foreach (var source in new[] { wideSource, wideSource, tallSource, tallSource, wideSource })
            {
                copy.CopyFrom(source);
                copy.AdvanceGeneration();
                var expected = CreateGrid(DescribeGrid(source));
                expected.AdvanceGeneration();

                Assert.That(DescribeGrid(copy), Is.EqualTo(DescribeGrid(expected)));
                Assert.That(copy.GetNeighbourCount(copy.CellCount - 1), Is.EqualTo(3));
            }
        }

        /// <summary>Resize reports an oversized request as an ArgumentException, even when rows times columns overflows an int, and keeps the old size.</summary>
        [TestCase(46341, 46341)]
        [TestCase(65536, 65536)]
        [TestCase(-1, 4)]
        [TestCase(4, -1)]
        public void Resize_ToAnImpossibleSize_ThrowsAndKeepsTheOldSize(int rows, int columns)
        {
            var grid = CreateEmptyGrid(3, 4);

            Assert.Throws<ArgumentException>(() => grid.Resize(rows, columns));
            Assert.That(grid.Rows, Is.EqualTo(3));
            Assert.That(grid.Columns, Is.EqualTo(4));
        }

        /// <summary>A grid whose buffers could not be allocated is rejected up front.</summary>
        [TestCase(-1, 4)]
        [TestCase(4, -1)]
        [TestCase(46341, 46341)]
        public void Constructor_WithAnImpossibleMaximum_Throws(int maximumRows, int maximumColumns)
        {
            Assert.Throws<ArgumentException>(() => new CellGrid(maximumRows, maximumColumns));
        }

        /// <summary>Copying a grid that is larger than this grid's maximum is rejected.</summary>
        [Test]
        public void CopyFrom_SourceLargerThanMaximum_Throws()
        {
            var source = CreateEmptyGrid(5, 5);
            var copy = new CellGrid(4, 4);

            Assert.Throws<ArgumentException>(() => copy.CopyFrom(source));
        }

        /// <summary>HasLivingNeighbour sees living cells of either colour on all eight sides, including diagonals, and ignores the cell itself.</summary>
        [Test]
        public void HasLivingNeighbour_SeesEitherColourOnAllEightSides()
        {
            var grid = CreateGrid(
                "O....",
                ".....",
                "..P..",
                ".....",
                ".....");

            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(1, 1)), Is.True);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(3, 3)), Is.True);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(0, 1)), Is.True);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(2, 2)), Is.False);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(4, 4)), Is.False);
            Assert.That(grid.HasLivingNeighbour(grid.ToCellIndex(0, 4)), Is.False);
        }

        /// <summary>A newborn takes the colour two of its three parents share, for every pair of the four colours and wherever the odd parent stands.</summary>
        [TestCase(CellOwner.Player, CellOwner.Opponent)]
        [TestCase(CellOwner.Player, CellOwner.Third)]
        [TestCase(CellOwner.Player, CellOwner.Fourth)]
        [TestCase(CellOwner.Opponent, CellOwner.Player)]
        [TestCase(CellOwner.Opponent, CellOwner.Third)]
        [TestCase(CellOwner.Opponent, CellOwner.Fourth)]
        [TestCase(CellOwner.Third, CellOwner.Player)]
        [TestCase(CellOwner.Third, CellOwner.Opponent)]
        [TestCase(CellOwner.Third, CellOwner.Fourth)]
        [TestCase(CellOwner.Fourth, CellOwner.Player)]
        [TestCase(CellOwner.Fourth, CellOwner.Opponent)]
        [TestCase(CellOwner.Fourth, CellOwner.Third)]
        public void NewbornCell_TakesTheColourTwoOfItsParentsShare(CellOwner majority, CellOwner minority)
        {
            AssertNewbornsOfParentRow(minority, majority, majority, majority);
            AssertNewbornsOfParentRow(majority, minority, majority, majority);
            AssertNewbornsOfParentRow(majority, majority, minority, majority);
        }

        /// <summary>A newborn whose three parents all differ takes the one living colour none of them has, whatever order the parents stand in.</summary>
        [TestCase(CellOwner.Player, CellOwner.Opponent, CellOwner.Third, CellOwner.Fourth)]
        [TestCase(CellOwner.Fourth, CellOwner.Player, CellOwner.Opponent, CellOwner.Third)]
        [TestCase(CellOwner.Third, CellOwner.Fourth, CellOwner.Player, CellOwner.Opponent)]
        [TestCase(CellOwner.Opponent, CellOwner.Third, CellOwner.Fourth, CellOwner.Player)]
        public void NewbornCell_WithThreeDifferentParents_TakesTheMissingColour(CellOwner first, CellOwner second, CellOwner third, CellOwner missing)
        {
            AssertNewbornsOfParentRow(first, second, third, missing);
            AssertNewbornsOfParentRow(second, third, first, missing);
            AssertNewbornsOfParentRow(third, first, second, missing);
        }

        /// <summary>Every cell is decided from the board as it was: parents that die this generation still count, and a cell born beside a newborn this generation is not one of its parents.</summary>
        [Test]
        public void NewbornCell_IsDecidedFromTheBoardBeforeAnyCellChanges()
        {
            // (2,2)'s parents are the two Third cells above it and the Fourth cell below it; all three die this generation.
            // (2,1), earlier in cell order, is born Fourth from one Third and two Fourth parents. Updating cells in place would
            // drop the dead Third parents and count the new Fourth cell instead, turning (2,2) Fourth.
            var grid = CreateGrid(
                ".....",
                ".T.T.",
                ".....",
                "F.F..",
                ".....");

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                ".....",
                ".FT..",
                ".....",
                "....."
            }), "(2,2) should be born Third from its two Third parents, which still count although they die this generation, and (2,1), born beside it, should not count as a parent.");
            Assert.That(grid.ChangedCellIndices, Is.EqualTo(new[]
            {
                grid.ToCellIndex(1, 1),
                grid.ToCellIndex(1, 3),
                grid.ToCellIndex(2, 1),
                grid.ToCellIndex(2, 2),
                grid.ToCellIndex(3, 0),
                grid.ToCellIndex(3, 2)
            }), "Every death and birth should be recorded once, in ascending cell order.");
        }

        /// <summary>An empty board has no colours, so nothing can absorb.</summary>
        [Test]
        public void HasLivingCellsOfMoreThanOneOwner_OnAnEmptyBoard_IsFalse()
        {
            var grid = CreateEmptyGrid(3, 3);

            Assert.That(grid.HasLivingCellsOfMoreThanOneOwner(), Is.False, "An empty board has no living owners at all.");
        }

        /// <summary>A board of one colour has nothing to absorb, however its cells are spread.</summary>
        [TestCase(CellOwner.Player)]
        [TestCase(CellOwner.Opponent)]
        [TestCase(CellOwner.Third)]
        [TestCase(CellOwner.Fourth)]
        public void HasLivingCellsOfMoreThanOneOwner_WithOneColour_IsFalse(CellOwner owner)
        {
            var grid = CreateEmptyGrid(3, 3);
            grid.SetOwner(0, owner);
            grid.SetOwner(grid.CellCount / 2, owner);
            grid.SetOwner(grid.CellCount - 1, owner);

            Assert.That(grid.HasLivingCellsOfMoreThanOneOwner(), Is.False, $"A board of only {owner} cells has one living owner.");
        }

        /// <summary>Two colours are found wherever they are, even when the second colour is only in the last cell.</summary>
        [TestCase(CellOwner.Player, CellOwner.Opponent)]
        [TestCase(CellOwner.Opponent, CellOwner.Player)]
        [TestCase(CellOwner.Player, CellOwner.Third)]
        [TestCase(CellOwner.Third, CellOwner.Fourth)]
        [TestCase(CellOwner.Fourth, CellOwner.Opponent)]
        public void HasLivingCellsOfMoreThanOneOwner_WithTwoColours_IsTrue(CellOwner firstOwner, CellOwner secondOwner)
        {
            var grid = CreateEmptyGrid(3, 3);
            grid.SetOwner(0, firstOwner);
            grid.SetOwner(grid.CellCount / 2, firstOwner);
            grid.SetOwner(grid.CellCount - 1, secondOwner);

            Assert.That(grid.HasLivingCellsOfMoreThanOneOwner(), Is.True, $"{firstOwner} cells and one {secondOwner} cell in the last cell are two living owners.");
        }

        /// <summary>Only the active area counts: stale cells of another colour past a smaller copied board are ignored.</summary>
        [Test]
        public void HasLivingCellsOfMoreThanOneOwner_IgnoresStaleCellsPastTheActiveArea()
        {
            var source = CreateGrid(
                "P..",
                ".P.");
            var copy = CreateEmptyGrid(4, 4);
            for (var cellIndex = 0; cellIndex < copy.CellCount; cellIndex++)
            {
                copy.SetOwner(cellIndex, CellOwner.Opponent);
            }

            copy.CopyFrom(source);

            Assert.That(copy.HasLivingCellsOfMoreThanOneOwner(), Is.False, "The copied board holds only Player cells; the stale Opponent cells past it are outside the active area.");
        }

        /// <summary>A board of only one of Classic's extra colours, or only Opponent, never produces any other colour, as a Player-only board never does.</summary>
        [TestCase(CellOwner.Opponent)]
        [TestCase(CellOwner.Third)]
        [TestCase(CellOwner.Fourth)]
        public void SingleColourBoard_NeverProducesAnotherColour(CellOwner owner)
        {
            var grid = CreateEmptyGrid(RandomBoardSize, RandomBoardSize);
            FillRandomly(grid, RandomSeed, owner);

            for (var generation = 1; generation <= ManyGenerations; generation++)
            {
                grid.AdvanceGeneration();
                Assert.That(grid.CountCells(owner) + grid.CountCells(CellOwner.None), Is.EqualTo(grid.CellCount), $"Generation {generation} of a {owner}-only board should hold only {owner} and empty cells.");
            }

            Assert.That(grid.HasAnyLivingCell(), Is.True, "The board should still be alive, or the check proved little.");
        }

        /// <summary>A random board in Versus's two colours evolves exactly like a plain reference implementation of Conway's rules with majority-colour newborns, cell for cell and change for change.</summary>
        [TestCase(RandomBoardSize, RandomBoardSize, RandomSeed)]
        [TestCase(13, 29, 42)]
        [TestCase(31, 7, 99)]
        public void RandomTwoColourBoard_EvolvesLikeAReferenceImplementation(int rows, int columns, int seed)
        {
            var birthsFromThreeColours = AssertEvolvesLikeTheReference(rows, columns, seed, VersusOwners);

            Assert.That(birthsFromThreeColours, Is.Zero, "Two colours can never give a newborn three different parents.");
        }

        /// <summary>A random board in all four colours evolves exactly like the reference implementation, including newborns of three different parents taking the missing colour.</summary>
        [TestCase(RandomBoardSize, RandomBoardSize, RandomSeed)]
        [TestCase(17, 23, 7)]
        public void RandomFourColourBoard_EvolvesLikeAReferenceImplementation(int rows, int columns, int seed)
        {
            var birthsFromThreeColours = AssertEvolvesLikeTheReference(rows, columns, seed, LivingOwners);

            Assert.That(birthsFromThreeColours, Is.GreaterThan(0), "Some newborn should have had three different parents, or the missing-colour rule went untested.");
        }

        /// <summary>Creates an empty grid sized exactly to the given dimensions.</summary>
        private static CellGrid CreateEmptyGrid(int rows, int columns)
        {
            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            return grid;
        }

        /// <summary>Builds a grid from rows of text where '.' is empty, 'P' is Player, 'O' is Opponent, 'T' is Third and 'F' is Fourth, with no recorded changes.</summary>
        private static CellGrid CreateGrid(params string[] rows)
        {
            var grid = CreateEmptyGrid(rows.Length, rows[0].Length);
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    grid.SetOwner(grid.ToCellIndex(row, column), ParseOwner(rows[row][column]));
                }
            }

            grid.ClearChangedCells();
            return grid;
        }

        /// <summary>Renders the grid as rows of text in the format CreateGrid reads.</summary>
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
                case ThirdSymbol:
                    return CellOwner.Third;
                case FourthSymbol:
                    return CellOwner.Fourth;
                case EmptySymbol:
                    return CellOwner.None;
                default:
                    throw new ArgumentException($"Unknown cell symbol '{symbol}'.", nameof(symbol));
            }
        }

        /// <summary>Converts an owner into its pattern character, or '?' for a value that is not a CellOwner, so a broken colour never looks empty.</summary>
        private static char ToSymbol(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player:
                    return PlayerSymbol;
                case CellOwner.Opponent:
                    return OpponentSymbol;
                case CellOwner.Third:
                    return ThirdSymbol;
                case CellOwner.Fourth:
                    return FourthSymbol;
                case CellOwner.None:
                    return EmptySymbol;
                default:
                    return UnexpectedOwnerSymbol;
            }
        }

        /// <summary>Places three parents side by side in the middle row of a 5x5 board, advances one generation and checks the newborns above and below the middle parent take the expected colour while the middle parent survives in its own.</summary>
        private static void AssertNewbornsOfParentRow(CellOwner left, CellOwner middle, CellOwner right, CellOwner expectedNewborn)
        {
            var grid = CreateGrid(
                ".....",
                ".....",
                new string(new[] { EmptySymbol, ToSymbol(left), ToSymbol(middle), ToSymbol(right), EmptySymbol }),
                ".....",
                ".....");
            var newborn = ToSymbol(expectedNewborn);

            grid.AdvanceGeneration();

            Assert.That(DescribeGrid(grid), Is.EqualTo(new[]
            {
                ".....",
                $"..{newborn}..",
                $"..{ToSymbol(middle)}..",
                $"..{newborn}..",
                "....."
            }), $"Parents {left}, {middle}, {right} should give newborns of {expectedNewborn} above and below, with the middle {middle} surviving.");
        }

        /// <summary>Fills a random board, advances it many generations beside the reference rules and asserts every board and change list match, returning how many reference births had three different parents.</summary>
        private static int AssertEvolvesLikeTheReference(int rows, int columns, int seed, CellOwner[] owners)
        {
            var grid = CreateEmptyGrid(rows, columns);
            FillRandomly(grid, seed, owners);
            var expected = ReadOwners(grid);
            var birthsFromThreeColours = 0;

            for (var generation = 1; generation <= ManyGenerations; generation++)
            {
                var previous = expected;
                expected = AdvanceByReference(previous, rows, columns, ref birthsFromThreeColours);
                grid.AdvanceGeneration();

                Assert.That(ReadOwners(grid), Is.EqualTo(expected), $"Generation {generation} of the {rows}x{columns} board (seed {seed}) differs from the reference rules.");
                Assert.That(grid.ChangedCellIndices, Is.EqualTo(ListChangedCells(previous, expected)), $"Generation {generation} should record exactly the cells that changed, in ascending order.");
                grid.ClearChangedCells();
            }

            Assert.That(grid.HasAnyLivingCell(), Is.True, "The board should still be alive, or the comparison proved little.");
            return birthsFromThreeColours;
        }

        /// <summary>Gives each cell, with the fill chance, a colour picked at random from the given owners, leaving no recorded changes.</summary>
        private static void FillRandomly(CellGrid grid, int seed, params CellOwner[] owners)
        {
            var random = new Random(seed);
            for (var cellIndex = 0; cellIndex < grid.CellCount; cellIndex++)
            {
                if (random.NextDouble() < RandomFillChance)
                {
                    grid.SetOwner(cellIndex, owners[random.Next(owners.Length)]);
                }
            }

            grid.ClearChangedCells();
        }

        /// <summary>Copies every cell's owner, row by row.</summary>
        private static CellOwner[] ReadOwners(CellGrid grid)
        {
            var owners = new CellOwner[grid.CellCount];
            for (var cellIndex = 0; cellIndex < owners.Length; cellIndex++)
            {
                owners[cellIndex] = grid.GetOwner(cellIndex);
            }

            return owners;
        }

        /// <summary>Returns the indices whose owner differs between two boards, in ascending order.</summary>
        private static List<int> ListChangedCells(CellOwner[] before, CellOwner[] after)
        {
            var changedCells = new List<int>();
            for (var cellIndex = 0; cellIndex < before.Length; cellIndex++)
            {
                if (before[cellIndex] != after[cellIndex])
                {
                    changedCells.Add(cellIndex);
                }
            }

            return changedCells;
        }

        /// <summary>Reference rules written plainly: each cell looks at its in-bounds neighbours on the old board; the living survive on two or three, the dead are born on exactly three with the colour at least two parents share, or else the colour none of them has.</summary>
        private static CellOwner[] AdvanceByReference(CellOwner[] owners, int rows, int columns, ref int birthsFromThreeColours)
        {
            var next = new CellOwner[owners.Length];
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var parents = new List<CellOwner>();
                    for (var neighbourRow = row - 1; neighbourRow <= row + 1; neighbourRow++)
                    {
                        for (var neighbourColumn = column - 1; neighbourColumn <= column + 1; neighbourColumn++)
                        {
                            var isSelf = neighbourRow == row && neighbourColumn == column;
                            var isInside = neighbourRow >= 0 && neighbourRow < rows && neighbourColumn >= 0 && neighbourColumn < columns;
                            if (!isSelf && isInside && owners[neighbourRow * columns + neighbourColumn] != CellOwner.None)
                            {
                                parents.Add(owners[neighbourRow * columns + neighbourColumn]);
                            }
                        }
                    }

                    var cellIndex = row * columns + column;
                    if (owners[cellIndex] != CellOwner.None)
                    {
                        var survives = parents.Count >= NeighboursNeededToSurviveMinimum && parents.Count <= NeighboursNeededToSurviveMaximum;
                        next[cellIndex] = survives ? owners[cellIndex] : CellOwner.None;
                    }
                    else if (parents.Count == NeighboursNeededToBeBorn)
                    {
                        next[cellIndex] = ChooseNewbornByReference(parents, ref birthsFromThreeColours);
                    }
                }
            }

            return next;
        }

        /// <summary>Returns the colour at least two parents share, or, when all three differ, the one living colour none of them has, counting that case.</summary>
        private static CellOwner ChooseNewbornByReference(List<CellOwner> parents, ref int birthsFromThreeColours)
        {
            foreach (var colour in LivingOwners)
            {
                var parentsOfColour = 0;
                foreach (var parent in parents)
                {
                    if (parent == colour)
                    {
                        parentsOfColour++;
                    }
                }

                if (parentsOfColour >= ParentsNeededForAMajority)
                {
                    return colour;
                }
            }

            birthsFromThreeColours++;
            foreach (var colour in LivingOwners)
            {
                if (!parents.Contains(colour))
                {
                    return colour;
                }
            }

            throw new InvalidOperationException("Three different parents always leave one of the four colours out.");
        }
    }
}

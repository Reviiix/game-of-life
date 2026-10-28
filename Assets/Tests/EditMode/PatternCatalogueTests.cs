using System;
using System.Collections.Generic;
using GameOfLife.Configuration;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using NUnit.Framework;
using UnityEditor;

namespace GameOfLife.Tests
{
    /// <summary>Loads the shipped PatternCatalogue.asset and proves every pattern has an expectation, fits the largest board and behaves as its category claims on the real CellGrid.</summary>
    [TestFixture]
    public sealed class PatternCatalogueTests
    {
        private const string CataloguePath = "Assets/Configuration/PatternCatalogue.asset";
        private const string SettingsPath = "Assets/Configuration/GameSettings.asset";
        private const int BehaviourMargin = 8;
        private const int GrowMargin = 12;
        private const int PersistGenerations = 3;
        private const int GrowGenerations = 12;
        private const int DefaultBoardRows = 24;
        private const int DefaultBoardColumns = 12;
        private const int DefaultSetupSquares = 20;
        private const char EmptySymbol = '.';
        private const char LivingSymbol = 'O';

        /// <summary>What each shipped pattern does on the engine: its category, its period in generations (1 for Persist, 0 for Grow) and, for Move, how far it shifts per period.</summary>
        private static readonly Dictionary<string, (PatternCategory category, int period, int shiftRows, int shiftColumns)> Expectations =
            new Dictionary<string, (PatternCategory category, int period, int shiftRows, int shiftColumns)>
            {
                { "Block", Persist() },
                { "Beehive", Persist() },
                { "Loaf", Persist() },
                { "Boat", Persist() },
                { "Tub", Persist() },
                { "Pond", Persist() },
                { "Ship", Persist() },
                { "Blinker", Pulsate(2) },
                { "Toad", Pulsate(2) },
                { "Beacon", Pulsate(2) },
                { "Clock", Pulsate(2) },
                { "Pulsar", Pulsate(3) },
                { "Mold", Pulsate(4) },
                { "Octagon", Pulsate(5) },
                { "Figure eight", Pulsate(8) },
                { "Pentadecathlon", Pulsate(15) },
                { "Glider", Move(4, 1, 1) },
                { "Lightweight spaceship", Move(4, 0, -2) },
                { "Middleweight spaceship", Move(4, 0, -2) },
                { "Heavyweight spaceship", Move(4, 0, -2) },
                { "R-pentomino", Grow() },
                { "B-heptomino", Grow() },
                { "Pi-heptomino", Grow() },
                { "Acorn", Grow() },
                { "Diehard", Grow() },
            };

        private CellPattern[] patterns;

        /// <summary>Loads the catalogue asset once for the whole fixture and builds its patterns.</summary>
        [OneTimeSetUp]
        public void LoadCatalogue()
        {
            patterns = LoadRequiredAsset<PatternCatalogue>(CataloguePath).BuildPatterns();
        }

        /// <summary>The asset ships one pattern per expectation, with distinct names, a description each and the expected number in every category.</summary>
        [Test]
        public void Asset_HasUniqueNamesDescriptionsAndTheExpectedCountPerCategory()
        {
            var names = new HashSet<string>();
            var countsByCategory = new Dictionary<PatternCategory, int>();
            foreach (var pattern in patterns)
            {
                Assert.That(names.Add(pattern.Name), Is.True, $"The name '{pattern.Name}' appears more than once in the catalogue.");
                Assert.That(pattern.Description.Trim(), Is.Not.Empty, $"{pattern.Name} has no description.");
                countsByCategory.TryGetValue(pattern.Category, out var count);
                countsByCategory[pattern.Category] = count + 1;
            }

            var expectedCountsByCategory = new Dictionary<PatternCategory, int>();
            foreach (var expectation in Expectations.Values)
            {
                expectedCountsByCategory.TryGetValue(expectation.category, out var count);
                expectedCountsByCategory[expectation.category] = count + 1;
            }

            Assert.That(patterns, Has.Length.EqualTo(Expectations.Count), "The catalogue should ship one pattern per expectation.");
            foreach (PatternCategory category in Enum.GetValues(typeof(PatternCategory)))
            {
                Assert.That(CountIn(countsByCategory, category), Is.EqualTo(CountIn(expectedCountsByCategory, category)), $"{category} patterns in the catalogue.");
            }
        }

        /// <summary>The expectations table and the asset name the same patterns, and agree on every pattern's category.</summary>
        [Test]
        public void EveryPattern_HasAnExpectationAndEveryExpectationNamesAPattern()
        {
            var catalogueNames = new HashSet<string>();
            foreach (var pattern in patterns)
            {
                catalogueNames.Add(pattern.Name);
                Assert.That(Expectations.Keys, Does.Contain(pattern.Name), $"{pattern.Name} is in the catalogue but has no expectation in {nameof(PatternCatalogueTests)}.");
                Assert.That(pattern.Category, Is.EqualTo(Expectations[pattern.Name].category), $"{pattern.Name} is not in the category its expectation names.");
            }

            foreach (var name in Expectations.Keys)
            {
                Assert.That(catalogueNames, Does.Contain(name), $"The expectation for '{name}' names no pattern in the catalogue.");
            }
        }

        /// <summary>Every pattern fits the largest board GameSettings allows, and the default Versus half accepts small patterns but refuses the Pulsar as too big for the board.</summary>
        [Test]
        public void EveryPattern_FitsTheLargestBoardInSomeOrientation()
        {
            var settings = LoadRequiredAsset<GameSettings>(SettingsPath);
            var largestBoard = PlacementBounds.WholeBoard(settings.MaximumRows, settings.MaximumColumns);
            foreach (var pattern in patterns)
            {
                Assert.That(pattern.CanFit(largestBoard), Is.True, $"{pattern.Name} cannot fit the largest board of {settings.MaximumRows} rows by {settings.MaximumColumns} columns in any orientation.");
            }

            var defaultPlayerHalf = new PlacementBounds(0, DefaultBoardRows / 2, DefaultBoardRows, DefaultBoardColumns, DefaultSetupSquares);
            Assert.That(FindPattern("Block").CanFit(defaultPlayerHalf), Is.True, "Block should fit the default player half.");
            Assert.That(FindPattern("Blinker").CanFit(defaultPlayerHalf), Is.True, "Blinker should fit the default player half.");
            Assert.That(FindPattern("Glider").CanFit(defaultPlayerHalf), Is.True, "Glider should fit the default player half.");
            var pulsar = FindPattern("Pulsar");
            Assert.That(pulsar.CanFit(defaultPlayerHalf), Is.False, "Pulsar should not fit the default player half.");
            Assert.That(pulsar.GetUnavailableReason(defaultPlayerHalf), Is.EqualTo(PatternUnavailableReason.TooBigForBoard), "Pulsar should be too big for the whole default board, not just the half.");
        }

        /// <summary>Every Persist pattern is a still life: three generations change nothing and record no changed cells.</summary>
        [Test]
        public void PersistPatterns_NeverChange()
        {
            foreach (var pattern in SelectPatterns(PatternCategory.Persist))
            {
                var grid = CreateStampedBoard(pattern, BehaviourMargin);
                var start = Describe(grid);
                for (var generation = 1; generation <= PersistGenerations; generation++)
                {
                    grid.AdvanceGeneration();
                    Assert.That(Describe(grid), Is.EqualTo(start), $"{pattern.Name} changed at generation {generation}.");
                }

                Assert.That(grid.ChangedCellIndices, Is.Empty, $"{pattern.Name} recorded changed cells although it is a still life.");
            }
        }

        /// <summary>Every Pulsate pattern differs from its start at every generation before its period and matches it exactly at its period.</summary>
        [Test]
        public void PulsatePatterns_ReturnAfterTheirPeriodAndNotBefore()
        {
            foreach (var pattern in SelectPatterns(PatternCategory.Pulsate))
            {
                var period = GetExpectation(pattern).period;
                Assert.That(period, Is.GreaterThan(1), $"{pattern.Name} needs a period above one to pulsate.");
                var grid = CreateStampedBoard(pattern, BehaviourMargin);
                var start = Describe(grid);
                for (var generation = 1; generation < period; generation++)
                {
                    grid.AdvanceGeneration();
                    Assert.That(Describe(grid), Is.Not.EqualTo(start), $"{pattern.Name} returned to its start at generation {generation}, before its period of {period}.");
                }

                grid.AdvanceGeneration();
                Assert.That(Describe(grid), Is.EqualTo(start), $"{pattern.Name} did not return to its start after its period of {period} generations.");
            }
        }

        /// <summary>Every Move pattern reappears unchanged, shifted by its stated rows and columns, after one period.</summary>
        [Test]
        public void MovePatterns_ShiftByTheirStatedOffsetAfterOnePeriod()
        {
            foreach (var pattern in SelectPatterns(PatternCategory.Move))
            {
                var (_, period, shiftRows, shiftColumns) = GetExpectation(pattern);
                Assert.That(shiftRows != 0 || shiftColumns != 0, Is.True, $"{pattern.Name} needs a non-zero shift to move.");
                var grid = CreateStampedBoard(pattern, BehaviourMargin);
                Advance(grid, period);
                Assert.That(Describe(grid), Is.EqualTo(DescribeShifted(pattern, BehaviourMargin, shiftRows, shiftColumns)), $"{pattern.Name} did not move by ({shiftRows}, {shiftColumns}) after {period} generations.");
            }
        }

        /// <summary>Every Grow pattern never reproduces its start within twelve generations and is still alive at the end of them.</summary>
        [Test]
        public void GrowPatterns_ChangeAndStayAliveForTwelveGenerations()
        {
            foreach (var pattern in SelectPatterns(PatternCategory.Grow))
            {
                var grid = CreateStampedBoard(pattern, GrowMargin);
                var start = Describe(grid);
                for (var generation = 1; generation <= GrowGenerations; generation++)
                {
                    grid.AdvanceGeneration();
                    Assert.That(Describe(grid), Is.Not.EqualTo(start), $"{pattern.Name} reproduced its start at generation {generation}, so it does not grow.");
                }

                Assert.That(CountLivingCells(grid), Is.GreaterThan(0), $"{pattern.Name} died out within {GrowGenerations} generations.");
            }
        }

        /// <summary>The expectation for a still life: unchanged every generation.</summary>
        private static (PatternCategory category, int period, int shiftRows, int shiftColumns) Persist()
        {
            return (PatternCategory.Persist, 1, 0, 0);
        }

        /// <summary>The expectation for an oscillator that returns to its start after the period.</summary>
        private static (PatternCategory category, int period, int shiftRows, int shiftColumns) Pulsate(int period)
        {
            return (PatternCategory.Pulsate, period, 0, 0);
        }

        /// <summary>The expectation for a spaceship that reappears shifted by the given rows and columns after the period.</summary>
        private static (PatternCategory category, int period, int shiftRows, int shiftColumns) Move(int period, int shiftRows, int shiftColumns)
        {
            return (PatternCategory.Move, period, shiftRows, shiftColumns);
        }

        /// <summary>The expectation for a methuselah: no period, it only keeps changing.</summary>
        private static (PatternCategory category, int period, int shiftRows, int shiftColumns) Grow()
        {
            return (PatternCategory.Grow, 0, 0, 0);
        }

        /// <summary>Loads an asset by its project path, failing clearly when it is missing.</summary>
        private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"No {typeof(T).Name} asset at {path}; create it from the Game of Life asset menu.");
            return asset;
        }

        /// <summary>Returns how many patterns were counted in a category, or zero when none were.</summary>
        private static int CountIn(Dictionary<PatternCategory, int> countsByCategory, PatternCategory category)
        {
            countsByCategory.TryGetValue(category, out var count);
            return count;
        }

        /// <summary>Returns the pattern's expectation, failing clearly when the table has no entry for it.</summary>
        private static (PatternCategory category, int period, int shiftRows, int shiftColumns) GetExpectation(CellPattern pattern)
        {
            Assert.That(Expectations.TryGetValue(pattern.Name, out var expectation), Is.True, $"{pattern.Name} has no expectation in {nameof(PatternCatalogueTests)}.");
            return expectation;
        }

        /// <summary>Returns every catalogue pattern in a category, failing clearly when there are none.</summary>
        private List<CellPattern> SelectPatterns(PatternCategory category)
        {
            var selected = new List<CellPattern>();
            foreach (var pattern in patterns)
            {
                if (pattern.Category == category)
                {
                    selected.Add(pattern);
                }
            }

            Assert.That(selected, Is.Not.Empty, $"The catalogue has no {category} patterns.");
            return selected;
        }

        /// <summary>Returns the catalogue pattern with a name, failing clearly when there is none.</summary>
        private CellPattern FindPattern(string name)
        {
            foreach (var pattern in patterns)
            {
                if (pattern.Name == name)
                {
                    return pattern;
                }
            }

            Assert.Fail($"The catalogue has no pattern named '{name}'.");
            return null;
        }

        /// <summary>Returns an empty board with the pattern's upright shape stamped inside a margin of empty cells on every side, with no changes recorded.</summary>
        private static CellGrid CreateStampedBoard(CellPattern pattern, int margin)
        {
            var shape = pattern.Upright;
            var grid = OpponentTestBoards.CreateEmpty(shape.Rows + 2 * margin, shape.Columns + 2 * margin);
            Stamp(grid, shape, margin, margin);
            return grid;
        }

        /// <summary>Describes the board a pattern should show after moving: its upright shape stamped at the margin plus the shift on an otherwise empty board of the usual size.</summary>
        private static string[] DescribeShifted(CellPattern pattern, int margin, int shiftRows, int shiftColumns)
        {
            var shape = pattern.Upright;
            var grid = OpponentTestBoards.CreateEmpty(shape.Rows + 2 * margin, shape.Columns + 2 * margin);
            Stamp(grid, shape, margin + shiftRows, margin + shiftColumns);
            return Describe(grid);
        }

        /// <summary>Sets every living cell of a shape as a Player cell with the shape's top-left corner at the given cell, then clears the change list.</summary>
        private static void Stamp(CellGrid grid, PatternOrientation shape, int originRow, int originColumn)
        {
            for (var cell = 0; cell < shape.CellCount; cell++)
            {
                grid.SetOwner(grid.ToCellIndex(originRow + shape.GetRowOffset(cell), originColumn + shape.GetColumnOffset(cell)), CellOwner.Player);
            }

            grid.ClearChangedCells();
        }

        /// <summary>Advances the grid the given number of generations.</summary>
        private static void Advance(CellGrid grid, int generations)
        {
            for (var generation = 0; generation < generations; generation++)
            {
                grid.AdvanceGeneration();
            }
        }

        /// <summary>Counts the living cells on the board, whoever owns them.</summary>
        private static int CountLivingCells(CellGrid grid)
        {
            return grid.CellCount - grid.CountCells(CellOwner.None);
        }

        /// <summary>Renders the board as rows of '.' for empty and 'O' for living cells, top row first.</summary>
        private static string[] Describe(CellGrid grid)
        {
            var rows = new string[grid.Rows];
            var symbols = new char[grid.Columns];
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var column = 0; column < grid.Columns; column++)
                {
                    symbols[column] = grid.IsAlive(grid.ToCellIndex(row, column)) ? LivingSymbol : EmptySymbol;
                }

                rows[row] = new string(symbols);
            }

            return rows;
        }
    }
}

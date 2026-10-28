using System;
using System.Collections.Generic;
using System.Linq;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using NUnit.Framework;
using UnityEditor;

namespace GameOfLife.Tests
{
    /// <summary>Checks that PulsatingFormationPicker only plans catalogue Pulsate patterns that keep pulsating centred on the board, varies its choice and avoids the last one shown when it can.</summary>
    [TestFixture]
    public sealed class PulsatingFormationPickerTests
    {
        private const string CataloguePath = "Assets/Configuration/PatternCatalogue.asset";
        private const int MaximumRows = 60;
        private const int MaximumColumns = 30;
        private const int SeedsTried = 200;
        private const int NoPattern = -1;
        private const float Middle = 0.5f;
        private const float MenuGap = 0.37f;

        private CellPattern[] patterns;
        private PulsatingFormationPicker picker;

        /// <summary>Builds the shipped catalogue and a picker sized for the largest board.</summary>
        [SetUp]
        public void SetUp()
        {
            patterns = AssetDatabase.LoadAssetAtPath<PatternCatalogue>(CataloguePath).BuildPatterns();
            picker = new PulsatingFormationPicker(patterns, MaximumRows, MaximumColumns);
        }

        /// <summary>On the default, largest and a narrow board every seed plans a Pulsate pattern that, replayed on a fresh board, changes and comes back to its start, so it pulsates forever.</summary>
        [TestCase(24, 12)]
        [TestCase(60, 30)]
        [TestCase(10, 5)]
        public void TryPlan_PlansAPatternThatKeepsPulsating(int rows, int columns)
        {
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                Assert.That(picker.TryPlan(rows, columns, Middle, NoPattern, new Random(seed)), Is.True, $"Seed {seed}: plans something.");
                Assert.That(patterns[picker.PlannedPatternIndex].Category, Is.EqualTo(PatternCategory.Pulsate), $"Seed {seed}: category.");
                Assert.That(picker.PlannedCells.Count, Is.EqualTo(patterns[picker.PlannedPatternIndex].CellCount), $"Seed {seed}: cell count.");
                AssertPulsates(rows, columns, picker.PlannedCells, $"Seed {seed}, {patterns[picker.PlannedPatternIndex].Name}");
            }
        }

        /// <summary>The plan is centred: the planned cells' bounding box sits in the middle of the board, rounded towards the top left.</summary>
        [Test]
        public void TryPlan_CentresThePattern()
        {
            const int rows = 24;
            const int columns = 12;
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                picker.TryPlan(rows, columns, Middle, NoPattern, new Random(seed));
                var cells = picker.PlannedCells;
                var top = cells.Min(cell => cell / columns);
                var bottom = cells.Max(cell => cell / columns);
                var left = cells.Min(cell => cell % columns);
                var right = cells.Max(cell => cell % columns);
                Assert.That(top, Is.EqualTo(rows - 1 - bottom).Or.EqualTo(rows - 2 - bottom), $"Seed {seed}: centred vertically.");
                Assert.That(left, Is.EqualTo(columns - 1 - right).Or.EqualTo(columns - 2 - right), $"Seed {seed}: centred horizontally.");
            }
        }

        /// <summary>At the menu's height the plan's middle row lands at that fraction of the board, still pulsating, and a height near an edge keeps the whole shape on the board.</summary>
        [Test]
        public void TryPlan_PlacesTheMiddleAtTheChosenHeight()
        {
            const int rows = 24;
            const int columns = 12;
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                picker.TryPlan(rows, columns, MenuGap, NoPattern, new Random(seed));
                var cells = picker.PlannedCells;
                var middleRow = (cells.Min(cell => cell / columns) + cells.Max(cell => cell / columns)) / 2f;
                Assert.That(middleRow, Is.EqualTo(MenuGap * rows).Within(1.5f), $"Seed {seed}: middle row of {patterns[picker.PlannedPatternIndex].Name}.");
                AssertPulsates(rows, columns, cells, $"Seed {seed} at the menu's height");

                Assert.That(picker.TryPlan(rows, columns, 0f, NoPattern, new Random(seed)), Is.True, $"Seed {seed}: plans at the top.");
                Assert.That(picker.PlannedCells.Min(cell => cell / columns), Is.GreaterThanOrEqualTo(0), $"Seed {seed}: on the board at the top.");
                AssertPulsates(rows, columns, picker.PlannedCells, $"Seed {seed} at the top");
            }
        }

        /// <summary>Across seeds the default board shows several different patterns, and never the avoided one while another fits.</summary>
        [Test]
        public void TryPlan_VariesAndAvoidsTheLastPattern()
        {
            var shown = new HashSet<int>();
            for (var seed = 0; seed < SeedsTried; seed++)
            {
                picker.TryPlan(24, 12, Middle, NoPattern, new Random(seed));
                shown.Add(picker.PlannedPatternIndex);
            }

            Assert.That(shown.Count, Is.GreaterThan(2), "Several patterns are shown on the default board.");
            foreach (var avoided in shown)
            {
                for (var seed = 0; seed < SeedsTried; seed++)
                {
                    picker.TryPlan(24, 12, Middle, avoided, new Random(seed));
                    Assert.That(picker.PlannedPatternIndex, Is.Not.EqualTo(avoided), $"Seed {seed}: avoids {patterns[avoided].Name}.");
                }
            }
        }

        /// <summary>When the avoided pattern is the only one that fits, as the Blinker is on a 3x3 board, it is shown anyway.</summary>
        [Test]
        public void TryPlan_WhenOnlyTheAvoidedPatternFits_ShowsItAnyway()
        {
            var blinker = Array.FindIndex(patterns, pattern => pattern.Name == "Blinker");
            Assume.That(blinker, Is.Not.EqualTo(NoPattern));

            Assert.That(picker.TryPlan(3, 3, Middle, blinker, new Random(1)), Is.True);
            Assert.That(picker.PlannedPatternIndex, Is.EqualTo(blinker));
            AssertPulsates(3, 3, picker.PlannedCells, "Blinker on 3x3");
        }

        /// <summary>A board too small for any Pulsate pattern plans nothing.</summary>
        [Test]
        public void TryPlan_OnABoardTooSmall_PlansNothing()
        {
            picker.TryPlan(24, 12, Middle, NoPattern, new Random(1));

            Assert.That(picker.TryPlan(2, 2, Middle, NoPattern, new Random(1)), Is.False);
            Assert.That(picker.PlannedPatternIndex, Is.EqualTo(NoPattern));
            Assert.That(picker.PlannedCells, Is.Empty);
        }

        /// <summary>Missing arguments are refused.</summary>
        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new PulsatingFormationPicker(null, MaximumRows, MaximumColumns));
            Assert.Throws<ArgumentNullException>(() => picker.TryPlan(24, 12, Middle, NoPattern, null));
        }

        /// <summary>Places the cells on a fresh board and asserts it differs from its start next generation and returns to it within thirty.</summary>
        private static void AssertPulsates(int rows, int columns, IReadOnlyList<int> cells, string context)
        {
            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            foreach (var cell in cells)
            {
                grid.SetOwner(cell, CellOwner.Player);
            }

            var start = Describe(grid);
            grid.AdvanceGeneration();
            Assert.That(Describe(grid), Is.Not.EqualTo(start), $"{context}: changes after one generation.");
            for (var generation = 2; generation <= 30; generation++)
            {
                grid.AdvanceGeneration();
                if (Describe(grid) == start)
                {
                    return;
                }
            }

            Assert.Fail($"{context}: did not return to its start within thirty generations.");
        }

        /// <summary>Describes which cells are alive, row by row.</summary>
        private static string Describe(CellGrid grid)
        {
            var cells = new char[grid.CellCount];
            for (var cell = 0; cell < grid.CellCount; cell++)
            {
                cells[cell] = grid.IsAlive(cell) ? 'O' : '.';
            }

            return new string(cells);
        }
    }
}

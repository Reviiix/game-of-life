using System;
using System.Collections.Generic;
using GameOfLife.HowToPlay;
using GameOfLife.Simulation;
using NUnit.Framework;
using UnityEditor;

namespace GameOfLife.Tests
{
    /// <summary>Loads the shipped HowToPlayGuide.asset and proves every step has its text, every demo plays on the real engine, and each rule's demo shows what its card says.</summary>
    [TestFixture]
    public sealed class HowToPlayGuideTests
    {
        private const string GuidePath = "Assets/Configuration/HowToPlayGuide.asset";
        private const string ClassicTabName = "Classic";
        private const string VersusTabName = "Versus";
        private const string LonelyCellsHeading = "LONELY CELLS DIE";
        private const string SurvivalHeading = "TWO OR THREE NEIGHBOURS KEEP A CELL ALIVE";
        private const string CrowdedCellsHeading = "CROWDED CELLS DIE";
        private const string BirthHeading = "THREE NEIGHBOURS MAKE A NEW CELL";
        private const string BlinkAndTravelHeading = "SHAPES THAT BLINK AND TRAVEL";
        private const string NewCellsTakeASideHeading = "NEW CELLS TAKE A SIDE";
        private const string AbsorptionHeading = "BIGGER CLUSTERS ABSORB SMALLER ONES";
        private const string MixingColoursHeading = "MIXING COLOURS";
        private const string ClassicAbsorptionHeading = "COLOURS ABSORB EACH OTHER";
        private const int LargestDemoSide = 12;
        private const int FrameAfterPlacement = 1;
        private const int BlockSide = 2;
        private const int BlockCellCount = BlockSide * BlockSide;
        private const int BlinkerCellCount = 3;
        private const int GliderCellCount = 5;

        private IReadOnlyList<GuideStep> classicSteps;
        private IReadOnlyList<GuideStep> versusSteps;

        /// <summary>Loads the guide asset once for the whole fixture.</summary>
        [OneTimeSetUp]
        public void LoadGuide()
        {
            var guide = AssetDatabase.LoadAssetAtPath<HowToPlayGuide>(GuidePath);
            Assert.That(guide, Is.Not.Null, $"No {nameof(HowToPlayGuide)} asset at {GuidePath}; create it from the Game of Life asset menu.");
            classicSteps = guide.ClassicSteps;
            versusSteps = guide.VersusSteps;
        }

        /// <summary>Both tabs have steps, and every step has a body and an upper-case heading that no other step in its tab shares.</summary>
        [Test]
        public void BothTabs_HaveStepsWithABodyAndUniqueUpperCaseHeadings()
        {
            foreach (var (tabName, steps) in Tabs())
            {
                Assert.That(steps, Is.Not.Empty, $"The {tabName} tab has no steps.");
                var headings = new HashSet<string>();
                for (var index = 0; index < steps.Count; index++)
                {
                    var step = steps[index];
                    Assert.That(step, Is.Not.Null, $"{tabName} step {index} is missing.");
                    Assert.That(string.IsNullOrWhiteSpace(step.Heading), Is.False, $"{tabName} step {index} has no heading.");
                    Assert.That(string.IsNullOrWhiteSpace(step.Body), Is.False, $"The {tabName} step '{step.Heading}' has no body.");
                    Assert.That(step.Heading, Is.EqualTo(step.Heading.ToUpperInvariant()), $"The {tabName} heading '{step.Heading}' is not in upper case.");
                    Assert.That(headings.Add(step.Heading), Is.True, $"The {tabName} heading '{step.Heading}' appears more than once.");
                }
            }
        }

        /// <summary>Every demo with a board builds, fits a 12 by 12 board, has positive timings and changes at least one cell on its way round the loop.</summary>
        [Test]
        public void EveryDemoWithABoard_BuildsFitsHasTimingsAndChangesInItsLoop()
        {
            foreach (var (tabName, steps) in Tabs())
            {
                foreach (var step in steps)
                {
                    if (!HasBoard(step))
                    {
                        continue;
                    }

                    var demo = step.Demo;
                    var label = $"The {tabName} demo for '{step.Heading}'";
                    var frames = BuildFrames(demo, label);
                    Assert.That(demo.Rows, Is.InRange(1, LargestDemoSide), $"{label} has {demo.Rows} rows.");
                    Assert.That(demo.Columns, Is.InRange(1, LargestDemoSide), $"{label} has {demo.Columns} columns.");
                    Assert.That(demo.SecondsPerStep, Is.GreaterThan(0f), $"{label} needs a positive time per step.");
                    Assert.That(demo.SecondsBeforeRepeat, Is.GreaterThan(0f), $"{label} needs a positive time before repeating.");
                    Assert.That(CountChangingTransitions(frames), Is.GreaterThan(0), $"{label} never changes a cell, so it shows a still picture.");
                }
            }
        }

        /// <summary>Classic has no halves, so its demos never shade one, they use Classic's colours, and every demo before 'MIXING COLOURS' is drawn in ink alone.</summary>
        [Test]
        public void ClassicDemos_NeverShadeAndUseOnlyInkBeforeColoursAreExplained()
        {
            var isColourExplained = false;
            foreach (var step in classicSteps)
            {
                isColourExplained |= step?.Heading == MixingColoursHeading;
                if (!HasBoard(step))
                {
                    continue;
                }

                var label = $"The Classic demo for '{step.Heading}'";
                Assert.That(step.Demo.ShadeAppHalf, Is.False, $"{label} shades the app's half, which Classic does not have.");
                Assert.That(step.Demo.VersusColours, Is.False, $"{label} uses Versus colours.");
                if (isColourExplained)
                {
                    continue;
                }

                var frames = BuildFrames(step.Demo, label);
                for (var frame = 0; frame < frames.Length; frame++)
                {
                    Assert.That(frames[frame], Has.All.Matches<CellOwner>(owner => owner == CellOwner.None || owner == CellOwner.Player), $"Frame {frame} of {label} shows a colour other than ink before '{MixingColoursHeading}' explains colours.");
                }
            }

            Assert.That(isColourExplained, Is.True, $"The Classic tab has no step headed '{MixingColoursHeading}'.");
        }

        /// <summary>'MIXING COLOURS': the generation gives one newborn the blue majority of its parents and another, whose three parents all differ, the fourth colour, yellow.</summary>
        [Test]
        public void MixingColoursDemo_ShowsAMajorityNewbornAndAFourthColourNewborn()
        {
            var frames = FindFrames(classicSteps, MixingColoursHeading);
            Assert.That(frames, Has.Length.GreaterThan(2), $"'{MixingColoursHeading}' needs placements and a generation.");
            var lastFrame = frames[frames.Length - 1];
            var beforeGeneration = frames[frames.Length - 2];

            AssertHasNewborn(beforeGeneration, lastFrame, CellOwner.Third, $"'{MixingColoursHeading}' should end with a blue newborn outvoting its red parent.");
            AssertHasNewborn(beforeGeneration, lastFrame, CellOwner.Fourth, $"'{MixingColoursHeading}' should end with a yellow newborn whose three parents all differ.");
        }

        /// <summary>'COLOURS ABSORB EACH OTHER': two touching clusters of equal size and different colours start side by side, and one placement turns the board a single colour.</summary>
        [Test]
        public void ClassicAbsorptionDemo_EqualClustersCoexistUntilAPlacementLetsOneAbsorbTheOther()
        {
            var frames = FindFrames(classicSteps, ClassicAbsorptionHeading);
            Assert.That(frames, Has.Length.GreaterThan(FrameAfterPlacement), $"'{ClassicAbsorptionHeading}' needs a placement.");
            Assert.That(CountColours(frames[0]), Is.EqualTo(2), $"'{ClassicAbsorptionHeading}' should start with two colours side by side.");
            Assert.That(CountOwner(frames[0], CellOwner.Player), Is.EqualTo(CountOwner(frames[0], CellOwner.Opponent)), $"'{ClassicAbsorptionHeading}' should start with an ink and a red cluster of the same size, which leave each other alone.");
            Assert.That(CountColours(frames[FrameAfterPlacement]), Is.EqualTo(1), $"The placement in '{ClassicAbsorptionHeading}' should let the bigger cluster absorb the other straight away.");
        }

        /// <summary>A shaded Versus demo shows setup, so in every frame the player's cells stay in the top half and the app's in the shaded bottom half.</summary>
        [Test]
        public void ShadedVersusDemos_KeepPlayerCellsTopAndAppCellsBottomInEveryFrame()
        {
            var shadedDemoCount = 0;
            foreach (var step in versusSteps)
            {
                if (!HasBoard(step) || !step.Demo.ShadeAppHalf)
                {
                    continue;
                }

                shadedDemoCount++;
                var demo = step.Demo;
                var firstAppRow = demo.Rows / 2;
                var frames = BuildFrames(demo, $"The shaded demo for '{step.Heading}'");
                for (var frame = 0; frame < frames.Length; frame++)
                {
                    for (var cellIndex = 0; cellIndex < frames[frame].Length; cellIndex++)
                    {
                        var owner = frames[frame][cellIndex];
                        if (owner == CellOwner.None)
                        {
                            continue;
                        }

                        var row = cellIndex / demo.Columns;
                        var ownHalfOwner = row < firstAppRow ? CellOwner.Player : CellOwner.Opponent;
                        Assert.That(owner, Is.EqualTo(ownHalfOwner), $"Frame {frame} of the shaded demo for '{step.Heading}' has a {owner} cell at row {row}, column {cellIndex % demo.Columns}, on the other side's half.");
                    }
                }
            }

            Assert.That(shadedDemoCount, Is.GreaterThan(0), "No Versus demo shades the app's half, so setup is never shown.");
        }

        /// <summary>'LONELY CELLS DIE': the lonely cells it places all die, leaving an empty board.</summary>
        [Test]
        public void LonelyCellsDemo_EndsWithAnEmptyBoard()
        {
            var frames = FindFrames(classicSteps, LonelyCellsHeading);

            Assert.That(frames, Has.Length.GreaterThan(FrameAfterPlacement + 1), $"'{LonelyCellsHeading}' needs a placement and a generation.");
            Assert.That(CountLiving(frames[FrameAfterPlacement]), Is.GreaterThan(0), $"'{LonelyCellsHeading}' places no cells.");
            Assert.That(CountLiving(frames[frames.Length - 1]), Is.Zero, $"'{LonelyCellsHeading}' should end with every cell dead.");
        }

        /// <summary>'TWO OR THREE NEIGHBOURS KEEP A CELL ALIVE': the placed block and beehive are unchanged in the last two frames.</summary>
        [Test]
        public void SurvivalDemo_LastTwoFramesMatchTheFrameAfterThePlacement()
        {
            var demo = FindDemo(classicSteps, SurvivalHeading);
            var frames = BuildFrames(demo, $"'{SurvivalHeading}'");

            Assert.That(frames, Has.Length.GreaterThan(FrameAfterPlacement + 2), $"'{SurvivalHeading}' needs a placement and two later frames.");
            Assert.That(CountLiving(frames[FrameAfterPlacement]), Is.GreaterThan(0), $"'{SurvivalHeading}' places no cells.");
            var placed = Describe(frames[FrameAfterPlacement], demo.Columns);
            for (var frame = frames.Length - 2; frame < frames.Length; frame++)
            {
                Assert.That(Describe(frames[frame], demo.Columns), Is.EqualTo(placed), $"Frame {frame} of '{SurvivalHeading}' differs from the placed shapes, which should never change.");
            }
        }

        /// <summary>'CROWDED CELLS DIE': the centre cell is alive before the generation and dead after it, while the four empty corners around it fill in.</summary>
        [Test]
        public void CrowdedCellsDemo_CentreCellDiesAndItsCornersFillIn()
        {
            var demo = FindDemo(classicSteps, CrowdedCellsHeading);
            var frames = BuildFrames(demo, $"'{CrowdedCellsHeading}'");
            Assert.That(frames, Has.Length.GreaterThan(FrameAfterPlacement + 1), $"'{CrowdedCellsHeading}' needs a placement and a generation.");
            var before = frames[FrameAfterPlacement];
            var after = frames[FrameAfterPlacement + 1];
            var centreRow = demo.Rows / 2;
            var centreColumn = demo.Columns / 2;
            var centre = centreRow * demo.Columns + centreColumn;

            Assert.That(before[centre], Is.Not.EqualTo(CellOwner.None), $"The centre cell of '{CrowdedCellsHeading}' should be alive before the generation.");
            Assert.That(after[centre], Is.EqualTo(CellOwner.None), $"The crowded centre cell of '{CrowdedCellsHeading}' should die in the generation.");
            for (var rowOffset = -1; rowOffset <= 1; rowOffset += 2)
            {
                for (var columnOffset = -1; columnOffset <= 1; columnOffset += 2)
                {
                    var corner = (centreRow + rowOffset) * demo.Columns + centreColumn + columnOffset;
                    Assert.That(before[corner], Is.EqualTo(CellOwner.None), $"The corner at ({rowOffset}, {columnOffset}) from the centre of '{CrowdedCellsHeading}' should start empty.");
                    Assert.That(after[corner], Is.Not.EqualTo(CellOwner.None), $"The corner at ({rowOffset}, {columnOffset}) from the centre of '{CrowdedCellsHeading}' should fill in.");
                }
            }
        }

        /// <summary>'THREE NEIGHBOURS MAKE A NEW CELL': the L fills in its fourth corner, ending as a 2x2 block of four cells.</summary>
        [Test]
        public void BirthDemo_EndsWithATwoByTwoBlock()
        {
            var demo = FindDemo(classicSteps, BirthHeading);
            var frames = BuildFrames(demo, $"'{BirthHeading}'");
            var lastFrame = frames[frames.Length - 1];

            Assert.That(CountLiving(lastFrame), Is.EqualTo(BlockCellCount), $"'{BirthHeading}' should end with {BlockCellCount} living cells.");
            var (height, width) = MeasureLivingCells(lastFrame, demo.Columns);
            Assert.That(height, Is.EqualTo(BlockSide), $"The living cells at the end of '{BirthHeading}' should span {BlockSide} rows.");
            Assert.That(width, Is.EqualTo(BlockSide), $"The living cells at the end of '{BirthHeading}' should span {BlockSide} columns.");
        }

        /// <summary>'SHAPES THAT BLINK AND TRAVEL': the blinker and the glider keep all eight of their cells alive in every frame.</summary>
        [Test]
        public void BlinkAndTravelDemo_KeepsTheBlinkerAndGliderCellsAlive()
        {
            var frames = FindFrames(classicSteps, BlinkAndTravelHeading);

            for (var frame = 0; frame < frames.Length; frame++)
            {
                Assert.That(CountLiving(frames[frame]), Is.EqualTo(BlinkerCellCount + GliderCellCount), $"Frame {frame} of '{BlinkAndTravelHeading}' should show the blinker's and the glider's cells.");
            }
        }

        /// <summary>'NEW CELLS TAKE A SIDE': the demo ends, and so holds before repeating, on the lone newborn cell, which was empty after the placement and is the player's colour.</summary>
        [Test]
        public void NewCellsTakeASideDemo_EndsOnALoneNewbornPlayerCell()
        {
            var frames = FindFrames(versusSteps, NewCellsTakeASideHeading);
            Assert.That(frames, Has.Length.GreaterThan(FrameAfterPlacement + 1), $"'{NewCellsTakeASideHeading}' needs a placement and a generation.");

            var lastFrame = frames.Length - 1;
            Assert.That(CountLiving(frames[lastFrame]), Is.EqualTo(1), $"'{NewCellsTakeASideHeading}' should end on the lone newborn cell, so it is what the demo holds.");
            var newborn = Array.FindIndex(frames[lastFrame], owner => owner != CellOwner.None);
            Assert.That(frames[lastFrame][newborn], Is.EqualTo(CellOwner.Player), $"The lone cell at the end of '{NewCellsTakeASideHeading}' should be blue.");
            Assert.That(frames[FrameAfterPlacement][newborn], Is.EqualTo(CellOwner.None), $"The lone cell at the end of '{NewCellsTakeASideHeading}' should be newborn, not placed.");
        }

        /// <summary>In each tab, no demo before the card that explains absorption turns a living cell another colour, so absorption is never shown before it is explained.</summary>
        [TestCase(ClassicTabName, ClassicAbsorptionHeading)]
        [TestCase(VersusTabName, AbsorptionHeading)]
        public void DemosBeforeAbsorptionIsExplained_NeverConvertACell(string tabName, string absorptionHeading)
        {
            var steps = tabName == ClassicTabName ? classicSteps : versusSteps;
            var absorptionIndex = -1;
            for (var index = 0; index < steps.Count; index++)
            {
                if (steps[index]?.Heading == absorptionHeading)
                {
                    absorptionIndex = index;
                    break;
                }
            }

            Assert.That(absorptionIndex, Is.GreaterThanOrEqualTo(0), $"The {tabName} tab has no step headed '{absorptionHeading}'.");
            for (var index = 0; index < absorptionIndex; index++)
            {
                var step = steps[index];
                if (!HasBoard(step))
                {
                    continue;
                }

                var frames = BuildFrames(step.Demo, $"'{step.Heading}'");
                for (var frame = 1; frame < frames.Length; frame++)
                {
                    for (var cellIndex = 0; cellIndex < frames[frame].Length; cellIndex++)
                    {
                        var before = frames[frame - 1][cellIndex];
                        var after = frames[frame][cellIndex];
                        var converted = before != CellOwner.None && after != CellOwner.None && before != after;
                        Assert.That(converted, Is.False, $"Frame {frame} of '{step.Heading}' turns cell {cellIndex} from {before} to {after}, showing absorption before '{absorptionHeading}' explains it.");
                    }
                }
            }
        }

        /// <summary>'BIGGER CLUSTERS ABSORB SMALLER ONES': the start has a red cluster, and the placement lets blue absorb every red cell in the same frame.</summary>
        [Test]
        public void AbsorptionDemo_PlacementAbsorbsEveryRedCell()
        {
            var frames = FindFrames(versusSteps, AbsorptionHeading);

            Assert.That(frames, Has.Length.GreaterThan(FrameAfterPlacement), $"'{AbsorptionHeading}' needs a placement.");
            Assert.That(frames[0], Has.Some.EqualTo(CellOwner.Opponent), $"'{AbsorptionHeading}' should start with a red cluster.");
            Assert.That(frames[FrameAfterPlacement], Has.None.EqualTo(CellOwner.Opponent), $"The placement in '{AbsorptionHeading}' should let the bigger blue cluster absorb every red cell straight away.");
        }

        /// <summary>Lists both tabs with their names, for checks that apply to each.</summary>
        private IEnumerable<(string tabName, IReadOnlyList<GuideStep> steps)> Tabs()
        {
            yield return (ClassicTabName, classicSteps);
            yield return (VersusTabName, versusSteps);
        }

        /// <summary>Returns whether a step has a demo with a board to play.</summary>
        private static bool HasBoard(GuideStep step)
        {
            return step?.Demo != null && step.Demo.HasBoard;
        }

        /// <summary>Returns the demo of the step with this heading, failing with the heading when the tab has no such step or the step has no board.</summary>
        private static GuideDemo FindDemo(IReadOnlyList<GuideStep> steps, string heading)
        {
            foreach (var step in steps)
            {
                if (step != null && step.Heading == heading)
                {
                    Assert.That(HasBoard(step), Is.True, $"The step '{heading}' has no demo board.");
                    return step.Demo;
                }
            }

            Assert.Fail($"The guide has no step headed '{heading}'.");
            return null;
        }

        /// <summary>Returns the frames of the demo of the step with this heading, failing with the heading when it is missing or invalid.</summary>
        private static CellOwner[][] FindFrames(IReadOnlyList<GuideStep> steps, string heading)
        {
            return BuildFrames(FindDemo(steps, heading), $"'{heading}'");
        }

        /// <summary>Builds a demo's frames, failing with the label and the builder's reason when the demo is invalid.</summary>
        private static CellOwner[][] BuildFrames(GuideDemo demo, string label)
        {
            try
            {
                return demo.BuildFrames();
            }
            catch (ArgumentException exception)
            {
                Assert.Fail($"{label} cannot be built: {exception.Message}");
                return null;
            }
        }

        /// <summary>Counts the steps round the loop, the last back to the first included, that change at least one cell.</summary>
        private static int CountChangingTransitions(CellOwner[][] frames)
        {
            var changingTransitions = 0;
            for (var frame = 0; frame < frames.Length; frame++)
            {
                var next = frames[(frame + 1) % frames.Length];
                for (var cellIndex = 0; cellIndex < next.Length; cellIndex++)
                {
                    if (frames[frame][cellIndex] != next[cellIndex])
                    {
                        changingTransitions++;
                        break;
                    }
                }
            }

            return changingTransitions;
        }

        /// <summary>Counts the living cells in a frame, whoever owns them.</summary>
        private static int CountLiving(CellOwner[] frame)
        {
            var living = 0;
            foreach (var owner in frame)
            {
                if (owner != CellOwner.None)
                {
                    living++;
                }
            }

            return living;
        }

        /// <summary>Returns how many rows and columns the frame's living cells span, or zero by zero when none are alive.</summary>
        private static (int height, int width) MeasureLivingCells(CellOwner[] frame, int columns)
        {
            var firstRow = int.MaxValue;
            var lastRow = int.MinValue;
            var firstColumn = int.MaxValue;
            var lastColumn = int.MinValue;
            for (var cellIndex = 0; cellIndex < frame.Length; cellIndex++)
            {
                if (frame[cellIndex] == CellOwner.None)
                {
                    continue;
                }

                firstRow = Math.Min(firstRow, cellIndex / columns);
                lastRow = Math.Max(lastRow, cellIndex / columns);
                firstColumn = Math.Min(firstColumn, cellIndex % columns);
                lastColumn = Math.Max(lastColumn, cellIndex % columns);
            }

            return firstRow > lastRow ? (0, 0) : (lastRow - firstRow + 1, lastColumn - firstColumn + 1);
        }

        /// <summary>Fails with the message unless some cell that was empty before the generation is alive with the given owner after it.</summary>
        private static void AssertHasNewborn(CellOwner[] before, CellOwner[] after, CellOwner owner, string message)
        {
            for (var cellIndex = 0; cellIndex < after.Length; cellIndex++)
            {
                if (after[cellIndex] == owner && before[cellIndex] == CellOwner.None)
                {
                    return;
                }
            }

            Assert.Fail(message);
        }

        /// <summary>Returns how many cells of a frame have the given owner.</summary>
        private static int CountOwner(CellOwner[] frame, CellOwner owner)
        {
            return Array.FindAll(frame, cell => cell == owner).Length;
        }

        /// <summary>Returns how many different living colours a frame shows.</summary>
        private static int CountColours(CellOwner[] frame)
        {
            var colours = new HashSet<CellOwner>();
            foreach (var owner in frame)
            {
                if (owner != CellOwner.None)
                {
                    colours.Add(owner);
                }
            }

            return colours.Count;
        }

        /// <summary>Renders a frame as rows of '.' for empty, 'B' for Player and 'R' for Opponent cells, top row first.</summary>
        private static string[] Describe(CellOwner[] frame, int columns)
        {
            var rows = new string[frame.Length / columns];
            var symbols = new char[columns];
            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var owner = frame[row * columns + column];
                    symbols[column] = owner == CellOwner.Player ? 'B' : owner == CellOwner.Opponent ? 'R' : '.';
                }

                rows[row] = new string(symbols);
            }

            return rows;
        }
    }
}

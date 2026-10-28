using System;
using System.Collections.Generic;
using GameOfLife.Patterns;
using GameOfLife.Simulation;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace GameOfLife.Tests
{
    /// <summary>Checks the positioning session: centring, grabbing, dragging, clamping, turning, mirroring, validity, committing, cancelling, and that none of it allocates.</summary>
    [TestFixture]
    public sealed class PatternPlacementSessionTests
    {
        private const int BoardRows = 6;
        private const int BoardColumns = 12;
        private const int MaximumPatternCells = 48;
        private const int LargeBoardRows = 60;
        private const int LargeBoardColumns = 30;

        /// <summary>A pattern that cannot fit the bounds, by size or by squares, is refused before the session starts.</summary>
        [Test]
        public void Begin_WithAPatternThatCannotFit_Throws()
        {
            var session = new PatternPlacementSession(MaximumPatternCells);
            var rules = new FakePlacementRules(BoardRows, BoardColumns);
            var wholeBoard = PlacementBounds.WholeBoard(BoardRows, BoardColumns);
            var threeSquares = new PlacementBounds(0, BoardRows, BoardRows, BoardColumns, 3);

            Assert.Throws<InvalidOperationException>(() => session.Begin(PatternTestShapes.Pulsar(), wholeBoard, rules), "the Pulsar on a 6x12 board");
            Assert.Throws<InvalidOperationException>(() => session.Begin(PatternTestShapes.Block(), threeSquares, rules), "the Block with 3 squares");
            Assert.That(session.IsActive, Is.False, "the session stays inactive");
            Assert.That(session.Pattern, Is.Null, "no pattern is kept");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(0), "the rules are not asked");
        }

        /// <summary>Beginning shows nothing and asks the rules nothing until the board is pressed.</summary>
        [Test]
        public void Begin_HasNoGhostUntilTheFirstPress()
        {
            var blinker = PatternTestShapes.Blinker();

            var session = BeginOnBoard(blinker, out var rules);

            Assert.That(session.IsActive, Is.True, "the session is active");
            Assert.That(session.HasGhost, Is.False, "no ghost before a press");
            Assert.That(session.IsValid, Is.False, "nothing is valid before a press");
            Assert.That(session.CellIndices, Is.Empty, "no cells before a press");
            Assert.That(session.Pattern, Is.SameAs(blinker), "the pattern being positioned");
            Assert.That(session.Pose.Index, Is.EqualTo(PatternPose.Upright.Index), "the Blinker starts upright");
            Assert.That(session.Orientation, Is.SameAs(blinker.Upright), "the upright shape is shown");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(0), "no validity check before a press");
            Assert.That(rules.PlaceCalls, Is.EqualTo(0), "no placement before a press");
        }

        /// <summary>The first press puts the pattern's centre cell on the pressed cell and shows the ghost.</summary>
        [Test]
        public void Press_CentresThePatternOnTheCell()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);

            Assert.That(session.Press(2, 5), Is.True, "the ghost appears");

            AssertOrigin(session, 2, 4, "a Blinker centred on (2,5)");
            Assert.That(session.HasGhost, Is.True, "the ghost is shown");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 28, 29, 30 }), "row 2, columns 4 to 6 on a 12-column board");
            Assert.That(session.IsValid, Is.True, "nothing blocks the cells");
            Assert.That(rules.CopyLastCheckedIndices(), Is.EqualTo(new[] { 28, 29, 30 }), "the rules were asked about the ghost's cells");
        }

        /// <summary>A press near an edge keeps the whole shape inside the band and the board.</summary>
        [Test]
        public void Press_NearAnEdge_ClampsInsideTheBounds()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out _);

            session.Press(0, 0);
            AssertOrigin(session, 0, 0, "a Blinker pressed in the top-left corner");

            session.Press(BoardRows - 1, BoardColumns - 1);
            AssertOrigin(session, BoardRows - 1, BoardColumns - 3, "a Blinker pressed in the bottom-right corner");

            const int bandEndRow = 3;
            var topBand = new PlacementBounds(0, bandEndRow, BoardRows, BoardColumns, int.MaxValue);
            var blockSession = Begin(PatternTestShapes.Block(), topBand, out _);

            blockSession.Press(BoardRows - 1, 5);
            AssertOrigin(blockSession, bandEndRow - 2, 5, "a Block pressed below a 3-row band");
        }

        /// <summary>Pressing inside the ghost grabs it where it was pressed, so a drag keeps that cell under the finger.</summary>
        [Test]
        public void Press_InsideTheGhost_GrabsWithoutMoving()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);
            session.Press(2, 5);
            var checksAfterFirstPress = rules.CanPlaceCalls;

            Assert.That(session.Press(2, 6), Is.False, "a press inside the ghost does not move it");
            AssertOrigin(session, 2, 4, "the ghost after being grabbed");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(checksAfterFirstPress), "grabbing does not re-check the rules");

            Assert.That(session.Drag(3, 6), Is.True, "a drag moves the grabbed ghost");
            AssertOrigin(session, 3, 4, "the grabbed cell (2,6) followed the finger to (3,6)");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 40, 41, 42 }), "row 3, columns 4 to 6");
        }

        /// <summary>Pressing outside the ghost recentres the pattern on the new cell.</summary>
        [Test]
        public void Press_OutsideTheGhost_Recentres()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out _);
            session.Press(2, 5);

            Assert.That(session.Press(5, 1), Is.True, "a press outside the ghost moves it");

            AssertOrigin(session, 5, 0, "a Blinker recentred on (5,1)");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 60, 61, 62 }), "row 5, columns 0 to 2");
        }

        /// <summary>A drag that arrives before any press centres the pattern like a press would.</summary>
        [Test]
        public void Drag_BeforeAnyPress_ActsAsAPress()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out _);

            Assert.That(session.Drag(2, 5), Is.True, "the ghost appears");

            Assert.That(session.HasGhost, Is.True, "the ghost is shown");
            AssertOrigin(session, 2, 4, "a Blinker centred on the dragged cell (2,5)");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 28, 29, 30 }), "row 2, columns 4 to 6");
        }

        /// <summary>A drag that leaves the origin where it is reports no move and asks the rules nothing.</summary>
        [Test]
        public void Drag_ReturnsFalseWhenTheOriginDoesNotChange()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);
            session.Press(2, 5);
            var checksAfterPress = rules.CanPlaceCalls;

            Assert.That(session.Drag(2, 5), Is.False, "dragged onto the pressed cell");
            Assert.That(session.Drag(2, 6), Is.True, "dragged one cell right");
            Assert.That(session.Drag(2, 6), Is.False, "dragged onto the same cell again");

            AssertOrigin(session, 2, 5, "the ghost after one move");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(checksAfterPress + 1), "only the real move re-checked the rules");
        }

        /// <summary>Dragging towards any edge slides the ghost to that edge and no further.</summary>
        [Test]
        public void Drag_ClampsAtEveryEdge()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out _);
            session.Press(2, 5);

            Assert.That(session.Drag(0, 0), Is.True, "dragged to the top-left corner");
            AssertOrigin(session, 0, 0, "the ghost at the top and left edges");

            Assert.That(session.Drag(BoardRows - 1, BoardColumns - 1), Is.True, "dragged to the bottom-right corner");
            AssertOrigin(session, BoardRows - 1, BoardColumns - 3, "the ghost at the bottom and right edges");

            Assert.That(session.Drag(0, BoardColumns - 1), Is.True, "dragged to the top-right corner");
            AssertOrigin(session, 0, BoardColumns - 3, "the ghost at the top and right edges");

            Assert.That(session.Drag(BoardRows - 1, 0), Is.True, "dragged to the bottom-left corner");
            AssertOrigin(session, BoardRows - 1, 0, "the ghost at the bottom and left edges");
        }

        /// <summary>A turn keeps the centre cell where it was, clamps the turned box inside the bounds and grabs the new centre.</summary>
        [Test]
        public void Rotate_KeepsTheCentreAndClamps()
        {
            var blinker = PatternTestShapes.Blinker();
            var session = BeginOnBoard(blinker, out _);
            session.Press(2, 5);

            Assert.That(session.CanRotate, Is.True, "a Blinker can turn on a 6x12 board");
            Assert.That(session.RotateClockwise(), Is.True, "the turn happens");
            Assert.That(session.Pose.QuarterTurns, Is.EqualTo(1), "the pose after one turn");
            Assert.That(session.Orientation.Rows, Is.EqualTo(3), "the turned Blinker is 3 rows tall");
            Assert.That(session.Orientation.Columns, Is.EqualTo(1), "the turned Blinker is 1 column wide");
            AssertOrigin(session, 1, 5, "the turned Blinker keeps its centre at (2,5)");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 17, 29, 41 }), "column 5, rows 1 to 3");

            Assert.That(session.Drag(4, 7), Is.True, "a drag after the turn");
            AssertOrigin(session, 3, 7, "the grab offset is the turned shape's centre (1,0)");

            var cornerSession = BeginOnBoard(blinker, out _);
            cornerSession.Press(0, 0);
            AssertOrigin(cornerSession, 0, 0, "a Blinker pressed in the corner");

            Assert.That(cornerSession.RotateClockwise(), Is.True, "the turn in the corner happens");
            AssertOrigin(cornerSession, 0, 1, "the turned Blinker keeps its centre at (0,1) and clamps its top row to 0");
            Assert.That(cornerSession.CellIndices, Is.EqualTo(new[] { 1, 13, 25 }), "column 1, rows 0 to 2");

            Assert.That(cornerSession.Drag(3, 3), Is.True, "a drag after the corner turn");
            AssertOrigin(cornerSession, 2, 3, "the grab offset is the new centre offset (1,0)");
        }

        /// <summary>A turn that would not fit the band is refused and changes nothing.</summary>
        [Test]
        public void Rotate_IsRefusedWhenItWouldNotFit()
        {
            const int bandFirstRow = 2;
            var oneRowBand = new PlacementBounds(bandFirstRow, bandFirstRow + 1, BoardRows, BoardColumns, int.MaxValue);
            var session = Begin(PatternTestShapes.Blinker(), oneRowBand, out var rules);
            session.Press(bandFirstRow, 5);
            var checksAfterPress = rules.CanPlaceCalls;

            Assert.That(session.CanRotate, Is.False, "a Blinker cannot turn on a 1-row band");
            Assert.That(session.RotateClockwise(), Is.False, "the turn is refused");

            Assert.That(session.Pose.Index, Is.EqualTo(PatternPose.Upright.Index), "the pose is unchanged");
            AssertOrigin(session, bandFirstRow, 4, "the origin is unchanged");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 28, 29, 30 }), "the cells are unchanged");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(checksAfterPress), "a refused turn does not re-check the rules");
        }

        /// <summary>A turn before any press changes the pose but shows no ghost and asks the rules nothing.</summary>
        [Test]
        public void Rotate_BeforeAnyPress_ChangesOnlyThePose()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);

            Assert.That(session.RotateClockwise(), Is.True, "the turn happens");

            Assert.That(session.Pose.QuarterTurns, Is.EqualTo(1), "the pose after one turn");
            Assert.That(session.Orientation.Rows, Is.EqualTo(3), "the turned Blinker is shown");
            Assert.That(session.HasGhost, Is.False, "still no ghost");
            Assert.That(session.CellIndices, Is.Empty, "still no cells");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(0), "no validity check before a press");

            session.Press(2, 5);
            AssertOrigin(session, 1, 5, "the turned Blinker centred on (2,5)");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 17, 29, 41 }), "column 5, rows 1 to 3");
        }

        /// <summary>Mirroring flips the shown shape left to right in place, before and after a turn.</summary>
        [Test]
        public void Mirror_MirrorsTheDisplayedShape()
        {
            var glider = PatternTestShapes.Glider();
            var session = BeginOnBoard(glider, out _);
            session.Press(2, 5);
            AssertOrigin(session, 1, 4, "a Glider centred on (2,5)");

            Assert.That(session.Mirror(), Is.True, "the mirror happens");

            Assert.That(session.Pose.IsMirrored, Is.True, "the pose is mirrored");
            AssertOrigin(session, 1, 4, "mirroring keeps the origin");
            Assert.That(DescribeGhost(session), Is.EqualTo(DescribeStamped(glider.Upright.MirroredHorizontally(), 1, 4)), "the mirrored Glider at the same origin");

            Assert.That(session.RotateClockwise(), Is.True, "the turn after mirroring happens");
            var poseBeforeMirror = session.Pose;
            var shownBeforeMirror = session.Orientation;

            Assert.That(session.Mirror(), Is.True, "the second mirror happens");

            Assert.That(session.Pose.QuarterTurns, Is.EqualTo(3), "mirroring a one-turn pose turns it the other way");
            Assert.That(session.Pose.IsMirrored, Is.False, "mirroring twice is no longer mirrored");
            AssertOrigin(session, 1, 4, "mirroring after a turn keeps the origin");
            Assert.That(DescribeGhost(session), Is.EqualTo(DescribeStamped(glider.GetOrientation(poseBeforeMirror.Mirrored()), 1, 4)), "the mirrored pose's shape at the same origin");
            Assert.That(DescribeGhost(session), Is.EqualTo(DescribeStamped(shownBeforeMirror.MirroredHorizontally(), 1, 4)), "the previously shown shape flipped left to right");
        }

        /// <summary>Validity is whatever the rules say about the ghost's current cells.</summary>
        [Test]
        public void Validity_FollowsTheRulesForTheCurrentCells()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);
            rules.Block(30);

            session.Press(2, 5);

            Assert.That(session.IsValid, Is.False, "cell 30 is blocked");
            Assert.That(rules.CopyLastCheckedIndices(), Is.EqualTo(new[] { 28, 29, 30 }), "the rules were asked about row 2, columns 4 to 6");

            Assert.That(session.Drag(2, 4), Is.True, "dragged one cell left");

            AssertOrigin(session, 2, 3, "the ghost one cell left");
            Assert.That(session.IsValid, Is.True, "cells 27 to 29 are free");
            Assert.That(rules.CopyLastCheckedIndices(), Is.EqualTo(new[] { 27, 28, 29 }), "the rules were asked about row 2, columns 3 to 5");
        }

        /// <summary>A valid commit places the ghost's cells exactly once and ends the session.</summary>
        [Test]
        public void TryCommit_WhenValid_PlacesOnceAndEndsTheSession()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);
            session.Press(2, 5);

            Assert.That(session.TryCommit(), Is.True, "the commit succeeds");

            Assert.That(rules.PlaceCalls, Is.EqualTo(1), "placed once");
            Assert.That(rules.CopyLastPlacedIndices(), Is.EqualTo(new[] { 28, 29, 30 }), "the ghost's cells were placed");
            Assert.That(session.IsActive, Is.False, "the session has ended");
            Assert.That(session.HasGhost, Is.False, "the ghost has gone");
            Assert.That(session.IsValid, Is.False, "nothing is valid after the session");
            Assert.That(session.CellIndices, Is.Empty, "the cells are cleared");
            Assert.That(session.Pattern, Is.Null, "the pattern is forgotten");
            Assert.That(session.Orientation, Is.Null, "no shape between sessions");

            Assert.That(session.TryCommit(), Is.False, "nothing to commit after the session ended");
            Assert.That(rules.PlaceCalls, Is.EqualTo(1), "still placed exactly once");
        }

        /// <summary>A refused commit keeps the session and the ghost, and asks the rules again whether the cells are valid.</summary>
        [Test]
        public void TryCommit_WhenRefused_KeepsTheSessionAndRevalidates()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);
            session.Press(2, 5);
            rules.RefuseEveryPlacement();
            var checksAfterPress = rules.CanPlaceCalls;

            Assert.That(session.TryCommit(), Is.False, "the commit is refused");

            Assert.That(rules.PlaceCalls, Is.EqualTo(1), "placement was attempted once");
            Assert.That(session.IsActive, Is.True, "the session continues");
            Assert.That(session.HasGhost, Is.True, "the ghost stays");
            Assert.That(session.CellIndices, Is.EqualTo(new[] { 28, 29, 30 }), "the cells are unchanged");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(checksAfterPress + 1), "validity was re-checked");
            Assert.That(session.IsValid, Is.True, "the cells are still free");

            rules.Block(29);

            Assert.That(session.TryCommit(), Is.False, "the second commit is refused");
            Assert.That(session.IsValid, Is.False, "the re-check sees the newly blocked cell");
        }

        /// <summary>Committing before any press does nothing and asks the rules nothing.</summary>
        [Test]
        public void TryCommit_BeforeAnyPress_ReturnsFalse()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);

            Assert.That(session.TryCommit(), Is.False, "nothing to commit");

            Assert.That(session.IsActive, Is.True, "the session continues");
            Assert.That(rules.PlaceCalls, Is.EqualTo(0), "no placement");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(0), "no validity check");
        }

        /// <summary>Cancelling ends the session without placing anything, after which every input is ignored.</summary>
        [Test]
        public void Cancel_EndsWithoutPlacing()
        {
            var session = BeginOnBoard(PatternTestShapes.Blinker(), out var rules);
            session.Press(2, 5);
            var checksAfterPress = rules.CanPlaceCalls;

            session.Cancel();

            Assert.That(rules.PlaceCalls, Is.EqualTo(0), "nothing was placed");
            Assert.That(session.IsActive, Is.False, "the session has ended");
            Assert.That(session.HasGhost, Is.False, "the ghost has gone");
            Assert.That(session.CellIndices, Is.Empty, "the cells are cleared");
            Assert.That(session.Pattern, Is.Null, "the pattern is forgotten");
            Assert.That(session.Press(2, 5), Is.False, "a press after cancelling");
            Assert.That(session.Drag(3, 5), Is.False, "a drag after cancelling");
            Assert.That(session.CanRotate, Is.False, "no turn after cancelling");
            Assert.That(session.RotateClockwise(), Is.False, "a turn after cancelling");
            Assert.That(session.Mirror(), Is.False, "a mirror after cancelling");
            Assert.That(session.TryCommit(), Is.False, "a commit after cancelling");
            Assert.That(rules.CanPlaceCalls, Is.EqualTo(checksAfterPress), "no rules call after cancelling");
        }

        /// <summary>Positioning the largest pattern on the largest board allocates nothing once warmed up.</summary>
        [Test]
        public void PressDragRotateAndMirror_DoNotAllocate()
        {
            const int pressRow = 30;
            const int pressColumn = 15;
            const int grabRow = 26;
            const int grabColumn = 12;
            const int dragRow = 27;
            const int dragColumn = 13;
            const int secondDragRow = 20;
            const int secondDragColumn = 10;
            var session = Begin(PatternTestShapes.Pulsar(), PlacementBounds.WholeBoard(LargeBoardRows, LargeBoardColumns), out _);

            session.Press(pressRow, pressColumn);
            session.Press(grabRow, grabColumn);
            session.Drag(dragRow, dragColumn);
            session.RotateClockwise();
            session.Mirror();
            session.Drag(secondDragRow, secondDragColumn);

            Assert.That(() =>
            {
                session.Press(pressRow, pressColumn);
                session.Press(grabRow, grabColumn);
                session.Drag(dragRow, dragColumn);
                session.RotateClockwise();
                session.Mirror();
                session.Drag(secondDragRow, secondDragColumn);
            }, Is.Not.AllocatingGCMemory());
        }

        /// <summary>Starts a session for the pattern on the whole 6x12 board with fresh fake rules.</summary>
        private static PatternPlacementSession BeginOnBoard(CellPattern pattern, out FakePlacementRules rules)
        {
            return Begin(pattern, PlacementBounds.WholeBoard(BoardRows, BoardColumns), out rules);
        }

        /// <summary>Starts a session for the pattern within the bounds with fresh fake rules sized to the board.</summary>
        private static PatternPlacementSession Begin(CellPattern pattern, in PlacementBounds bounds, out FakePlacementRules rules)
        {
            rules = new FakePlacementRules(bounds.BoardRows, bounds.Columns);
            var session = new PatternPlacementSession(MaximumPatternCells);
            session.Begin(pattern, bounds, rules);
            return session;
        }

        /// <summary>Asserts the ghost's top-left corner.</summary>
        private static void AssertOrigin(PatternPlacementSession session, int row, int column, string message)
        {
            Assert.That(session.OriginRow, Is.EqualTo(row), $"{message}: origin row");
            Assert.That(session.OriginColumn, Is.EqualTo(column), $"{message}: origin column");
        }

        /// <summary>Describes the ghost's cells drawn on an empty 6x12 board.</summary>
        private static string[] DescribeGhost(PatternPlacementSession session)
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            for (var cell = 0; cell < session.CellIndices.Count; cell++)
            {
                grid.SetOwner(session.CellIndices[cell], CellOwner.Player);
            }

            return PatternTestShapes.Describe(grid);
        }

        /// <summary>Describes a shape stamped at the origin on an empty 6x12 board.</summary>
        private static string[] DescribeStamped(PatternOrientation orientation, int originRow, int originColumn)
        {
            var grid = OpponentTestBoards.CreateEmpty(BoardRows, BoardColumns);
            PatternTestShapes.Stamp(grid, orientation, originRow, originColumn);
            return PatternTestShapes.Describe(grid);
        }

        /// <summary>Stands in for a game mode: blocks chosen cells, can refuse every placement and records the calls, allocating nothing after construction.</summary>
        private sealed class FakePlacementRules : IPatternPlacementRules
        {
            private readonly bool[] blockedCells;
            private readonly int[] lastCheckedIndices;
            private readonly int[] lastPlacedIndices;
            private int lastCheckedCount;
            private int lastPlacedCount;
            private int canPlaceCalls;
            private int placeCalls;
            private bool refusePlacement;

            /// <summary>How many times the session asked whether its cells may be placed.</summary>
            public int CanPlaceCalls => canPlaceCalls;

            /// <summary>How many times the session asked to place its cells.</summary>
            public int PlaceCalls => placeCalls;

            /// <summary>Sizes every buffer to the board so no call allocates.</summary>
            public FakePlacementRules(int rows, int columns)
            {
                var cellCount = rows * columns;
                blockedCells = new bool[cellCount];
                lastCheckedIndices = new int[cellCount];
                lastPlacedIndices = new int[cellCount];
            }

            /// <summary>Marks a cell that no pattern may cover.</summary>
            public void Block(int cellIndex)
            {
                blockedCells[cellIndex] = true;
            }

            /// <summary>Makes every placement fail from now on, whatever the cells.</summary>
            public void RefuseEveryPlacement()
            {
                refusePlacement = true;
            }

            /// <summary>Copies the cells the last validity check was asked about.</summary>
            public int[] CopyLastCheckedIndices()
            {
                return Copy(lastCheckedIndices, lastCheckedCount);
            }

            /// <summary>Copies the cells the last placement was asked to place.</summary>
            public int[] CopyLastPlacedIndices()
            {
                return Copy(lastPlacedIndices, lastPlacedCount);
            }

            /// <summary>The session never asks for bounds, so the fake has none to give.</summary>
            public bool TryGetPatternPlacementBounds(out PlacementBounds bounds)
            {
                bounds = default;
                return false;
            }

            /// <summary>Records the cells and allows them unless one is blocked.</summary>
            public bool CanPlacePattern(IReadOnlyList<int> cellIndices)
            {
                canPlaceCalls++;
                lastCheckedCount = Record(cellIndices, lastCheckedIndices);
                return AreAllFree(cellIndices);
            }

            /// <summary>Records the cells and places them unless refusing or one is blocked.</summary>
            public bool TryPlacePattern(IReadOnlyList<int> cellIndices)
            {
                placeCalls++;
                lastPlacedCount = Record(cellIndices, lastPlacedIndices);
                return !refusePlacement && AreAllFree(cellIndices);
            }

            /// <summary>Returns whether none of the cells is blocked.</summary>
            private bool AreAllFree(IReadOnlyList<int> cellIndices)
            {
                for (var cell = 0; cell < cellIndices.Count; cell++)
                {
                    if (blockedCells[cellIndices[cell]])
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>Copies the cells into the buffer and returns how many there were.</summary>
            private static int Record(IReadOnlyList<int> cellIndices, int[] buffer)
            {
                var count = cellIndices.Count;
                for (var cell = 0; cell < count; cell++)
                {
                    buffer[cell] = cellIndices[cell];
                }

                return count;
            }

            /// <summary>Returns a fresh array holding the first count entries of the buffer.</summary>
            private static int[] Copy(int[] buffer, int count)
            {
                var copy = new int[count];
                Array.Copy(buffer, copy, count);
                return copy;
            }
        }
    }
}

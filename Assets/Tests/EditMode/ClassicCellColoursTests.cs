using System;
using System.Collections.Generic;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using GameOfLife.Theming;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks ClassicCellColours: the colour button's order of four living owners starting with ink, how it wraps, and how each owner is painted on the board and themed off it.</summary>
    [TestFixture]
    public sealed class ClassicCellColoursTests
    {
        private const int InkPosition = 0;
        private const int ClassicColourCount = 4;

        /// <summary>The theme role each Classic paint is drawn in, so anything showing a colour off the board matches the cells.</summary>
        private static readonly Dictionary<CellPaint, ThemeColour> ThemeColourOfPaint = new Dictionary<CellPaint, ThemeColour>
        {
            { CellPaint.Ink, ThemeColour.Ink },
            { CellPaint.Player, ThemeColour.Player },
            { CellPaint.Opponent, ThemeColour.Opponent },
            { CellPaint.Accent, ThemeColour.Accent },
        };

        /// <summary>The colour button starts on ink, which is the Player owner.</summary>
        [Test]
        public void FirstPosition_IsInk()
        {
            var firstOwner = ClassicCellColours.GetOwner(InkPosition);

            Assert.That(firstOwner, Is.EqualTo(CellOwner.Player), "The first Classic colour should be Player.");
            Assert.That(ClassicCellColours.ToPaint(firstOwner), Is.EqualTo(CellPaint.Ink), "The first Classic colour should be painted in ink.");
        }

        /// <summary>The order lists four distinct living owners: every CellOwner except None, each once.</summary>
        [Test]
        public void Order_ListsFourDistinctLivingOwners()
        {
            var owners = ListOwnersInOrder();
            var livingOwners = new List<CellOwner>();
            foreach (CellOwner owner in Enum.GetValues(typeof(CellOwner)))
            {
                if (owner != CellOwner.None)
                {
                    livingOwners.Add(owner);
                }
            }

            Assert.That(ClassicCellColours.Count, Is.EqualTo(ClassicColourCount), "Classic should offer four colours.");
            Assert.That(owners, Is.Unique, "No owner should appear twice in the colour order.");
            Assert.That(owners, Has.None.EqualTo(CellOwner.None), "A dead cell is not a colour to choose.");
            Assert.That(owners, Is.EquivalentTo(livingOwners), "The colour order should list every living owner.");
        }

        /// <summary>The colour button steps through ink, blue, red and yellow, in that order.</summary>
        [Test]
        public void Order_StepsThroughInkBlueRedYellow()
        {
            var paints = new List<CellPaint>();
            foreach (var owner in ListOwnersInOrder())
            {
                paints.Add(ClassicCellColours.ToPaint(owner));
            }

            Assert.That(paints, Is.EqualTo(new[] { CellPaint.Ink, CellPaint.Player, CellPaint.Opponent, CellPaint.Accent }), "The colours should come in the order ink, blue, red, yellow.");
        }

        /// <summary>Each position steps to the next one, and the last wraps back to ink.</summary>
        [Test]
        public void GetNextPosition_StepsForwardAndWrapsAfterTheLast()
        {
            var lastPosition = ClassicCellColours.Count - 1;
            for (var position = InkPosition; position < lastPosition; position++)
            {
                Assert.That(ClassicCellColours.GetNextPosition(position), Is.EqualTo(position + 1), $"Position {position} should step to {position + 1}.");
            }

            Assert.That(ClassicCellColours.GetNextPosition(lastPosition), Is.EqualTo(InkPosition), "The last colour should wrap back to ink.");
        }

        /// <summary>Every listed owner is painted in its own colour, never Empty.</summary>
        [Test]
        public void EveryListedOwner_HasADistinctVisiblePaint()
        {
            var paints = new List<CellPaint>();
            foreach (var owner in ListOwnersInOrder())
            {
                var paint = ClassicCellColours.ToPaint(owner);
                Assert.That(paint, Is.Not.EqualTo(CellPaint.Empty), $"A living {owner} cell should not be painted Empty.");
                paints.Add(paint);
            }

            Assert.That(paints, Is.Unique, "Each Classic colour should have its own paint.");
        }

        /// <summary>Each listed owner's theme colour is the role its paint is drawn in: Ink for ink, Player for blue, Opponent for red and Accent for yellow.</summary>
        [Test]
        public void EveryListedOwner_HasAThemeColourMatchingItsPaint()
        {
            foreach (var owner in ListOwnersInOrder())
            {
                var paint = ClassicCellColours.ToPaint(owner);
                Assert.That(ThemeColourOfPaint.ContainsKey(paint), Is.True, $"{owner} is painted {paint}, which is not one of Classic's four colours.");
                Assert.That(ClassicCellColours.ToThemeColour(owner), Is.EqualTo(ThemeColourOfPaint[paint]), $"{owner} is painted {paint}, so its theme colour should be {ThemeColourOfPaint[paint]}.");
            }
        }

        /// <summary>A dead cell is painted Empty.</summary>
        [Test]
        public void None_IsPaintedEmpty()
        {
            Assert.That(ClassicCellColours.ToPaint(CellOwner.None), Is.EqualTo(CellPaint.Empty), "A dead cell should be painted Empty.");
        }

        /// <summary>Returns every owner in the colour button's order.</summary>
        private static List<CellOwner> ListOwnersInOrder()
        {
            var owners = new List<CellOwner>();
            for (var position = InkPosition; position < ClassicCellColours.Count; position++)
            {
                owners.Add(ClassicCellColours.GetOwner(position));
            }

            return owners;
        }
    }
}

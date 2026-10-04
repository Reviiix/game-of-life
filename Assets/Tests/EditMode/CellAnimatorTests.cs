using DG.Tweening;
using GameOfLife.Grid;
using GameOfLife.Motion;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace GameOfLife.Tests
{
    /// <summary>Checks that CellAnimator writes the right colour and tile size for each kind of cell change and settles afterwards.</summary>
    [TestFixture]
    public sealed class CellAnimatorTests
    {
        private const int CellCount = 4;
        private const float SquashScale = 0.5f;
        private const float ColourShare = 0.25f;
        private static readonly EasedMotion OneSecondLinear = new(1f, Ease.Linear);
        private static readonly Color32 Blue = new(0, 0, 255, 255);
        private static readonly Color32 Red = new(255, 0, 0, 255);

        private NativeArray<Color32> pixels;
        private CellAnimator animator;

        /// <summary>Creates an animator whose every motion is one second long and linear, over an empty board.</summary>
        [SetUp]
        public void SetUp()
        {
            pixels = new NativeArray<Color32>(CellCount, Allocator.Persistent);
            animator = new CellAnimator(CellCount, OneSecondLinear, OneSecondLinear, OneSecondLinear, SquashScale, ColourShare);
            animator.Clear(CellCount, pixels);
        }

        /// <summary>Releases the pixel buffer.</summary>
        [TearDown]
        public void TearDown()
        {
            pixels.Dispose();
        }

        /// <summary>A tile at its normal size is stored as two thirds of full alpha, leaving room for overshoot.</summary>
        [Test]
        public void ScaleEncoding_StoresNormalSizeAtTwoThirdsAlpha()
        {
            Assert.That(CellAnimator.FullSizeAlpha, Is.EqualTo(170));
            Assert.That(CellAnimator.DecodeScale(CellAnimator.EncodeScale(0.6f)), Is.EqualTo(0.6f).Within(0.01f));
            Assert.That(CellAnimator.EncodeScale(9f), Is.EqualTo(255));
            Assert.That(CellAnimator.EncodeScale(-1f), Is.EqualTo(0));
        }

        /// <summary>A cell coming alive grows from nothing in its new colour, then settles at full size.</summary>
        [Test]
        public void AppearingCell_GrowsInItsNewColourThenSettles()
        {
            animator.SetCell(0, Blue, true, 0f, pixels);

            Assert.That(animator.Advance(0.5f, pixels), Is.True);
            AssertPixel(pixels[0], Blue, 0.5f);

            Assert.That(animator.Advance(1f, pixels), Is.False);
            AssertPixel(pixels[0], Blue, 1f);
        }

        /// <summary>A dying cell keeps its old colour while it shrinks, then leaves an empty pixel.</summary>
        [Test]
        public void DisappearingCell_ShrinksInItsOldColourThenEmpties()
        {
            animator.SetCellImmediately(1, Red, true, pixels);
            animator.SetCell(1, default, false, 0f, pixels);

            animator.Advance(0.25f, pixels);
            AssertPixel(pixels[1], Red, 0.75f);

            animator.Advance(1f, pixels);
            Assert.That(pixels[1].a, Is.EqualTo(0));
        }

        /// <summary>An absorbed cell squashes, changes colour over the first share of the motion and springs back to full size.</summary>
        [Test]
        public void ConvertedCell_ChangesColourAndSpringsBackFromTheSquash()
        {
            animator.SetCellImmediately(2, Blue, true, pixels);
            animator.SetCell(2, Red, true, 0f, pixels);

            animator.Advance(0f, pixels);
            AssertPixel(pixels[2], Blue, SquashScale);

            animator.Advance(0.5f, pixels);
            AssertPixel(pixels[2], Red, 0.75f);

            Assert.That(animator.Advance(1f, pixels), Is.False);
            AssertPixel(pixels[2], Red, 1f);
        }

        /// <summary>A change that looks the same starts no animation.</summary>
        [Test]
        public void UnchangedLook_StartsNoAnimation()
        {
            animator.SetCellImmediately(3, Blue, true, pixels);
            animator.SetCell(3, Blue, true, 0f, pixels);
            animator.SetCell(0, default, false, 0f, pixels);

            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>The duration limit speeds animations up so they finish in time.</summary>
        [Test]
        public void DurationLimit_ShortensEveryAnimation()
        {
            animator.LimitDurationTo(0.5f);
            animator.SetCell(0, Blue, true, 0f, pixels);

            animator.Advance(0.25f, pixels);
            AssertPixel(pixels[0], Blue, 0.5f);
            Assert.That(animator.Advance(0.5f, pixels), Is.False);
        }

        /// <summary>Restarting cells that were settled at once many times never overflows the animating list.</summary>
        [Test]
        public void RepeatedImmediateAndAnimatedChanges_NeverOverflowTheList()
        {
            Assert.DoesNotThrow(() =>
            {
                for (var round = 0; round < CellCount * 4; round++)
                {
                    for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
                    {
                        animator.SetCell(cellIndex, round % 2 == 0 ? Blue : Red, true, round, pixels);
                        animator.SetCellImmediately(cellIndex, Red, round % 3 == 0, pixels);
                    }
                }

                animator.Advance(1000f, pixels);
            });
            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>A cell changed again mid-animation continues from the size and colour it shows, instead of jumping.</summary>
        [Test]
        public void ChangeMidAnimation_ContinuesFromTheDisplayedLook()
        {
            animator.SetCell(0, Blue, true, 0f, pixels);
            animator.Advance(0.5f, pixels);

            animator.SetCell(0, default, false, 0.5f, pixels);
            animator.Advance(0.5f, pixels);
            AssertPixel(pixels[0], Blue, 0.5f);

            animator.Advance(0.75f, pixels);
            AssertPixel(pixels[0], Blue, 0.375f);

            animator.SetCell(0, Red, true, 0.75f, pixels);
            animator.Advance(0.75f, pixels);
            AssertPixel(pixels[0], Red, 0.375f);
        }

        /// <summary>FinishAll jumps every animating cell to its final look.</summary>
        [Test]
        public void FinishAll_SettlesEveryAnimatingCell()
        {
            animator.SetCell(0, Blue, true, 0f, pixels);
            animator.SetCell(1, Red, true, 0f, pixels);

            animator.FinishAll(pixels);

            Assert.That(animator.HasActiveAnimations, Is.False);
            AssertPixel(pixels[0], Blue, 1f);
            AssertPixel(pixels[1], Red, 1f);
        }

        /// <summary>Checks a pixel's colour and decoded tile size.</summary>
        private static void AssertPixel(Color32 pixel, Color32 expectedColour, float expectedScale)
        {
            Assert.That(pixel.r, Is.EqualTo(expectedColour.r), "red");
            Assert.That(pixel.g, Is.EqualTo(expectedColour.g), "green");
            Assert.That(pixel.b, Is.EqualTo(expectedColour.b), "blue");
            Assert.That(CellAnimator.DecodeScale(pixel.a), Is.EqualTo(expectedScale).Within(0.01f), "scale");
        }
    }
}

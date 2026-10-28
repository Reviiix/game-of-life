using System;
using DG.Tweening;
using GameOfLife.Motion;
using GameOfLife.Theming;
using NUnit.Framework;
using UnityEngine;

namespace GameOfLife.Tests
{
    /// <summary>Checks palette lookups and blending, and that eased motions follow DOTween's curves.</summary>
    [TestFixture]
    public sealed class ThemeTests
    {
        /// <summary>Every role has a colour, so no interface element can ask for a missing one.</summary>
        [Test]
        public void Palette_HasAColourForEveryRole()
        {
            var palette = new ThemePalette();
            foreach (ThemeColour role in Enum.GetValues(typeof(ThemeColour)))
            {
                Assert.DoesNotThrow(() => palette.Get(role), role.ToString());
            }
        }

        /// <summary>Blending halfway gives the midpoint of every role, and copying matches the source exactly.</summary>
        [Test]
        public void Blend_MovesEveryRolePartWayAndCopyMatchesTheSource()
        {
            var black = CreateUniformPalette(Color.black);
            var white = CreateUniformPalette(Color.white);
            var blended = new ThemePalette();

            blended.Blend(black, white, 0.5f);
            foreach (ThemeColour role in Enum.GetValues(typeof(ThemeColour)))
            {
                Assert.That(blended.Get(role).r, Is.EqualTo(0.5f).Within(0.001f), role.ToString());
            }

            blended.CopyFrom(white);
            foreach (ThemeColour role in Enum.GetValues(typeof(ThemeColour)))
            {
                Assert.That(blended.Get(role), Is.EqualTo(Color.white), role.ToString());
            }
        }

        /// <summary>An eased motion starts at 0, ends at 1 and, for a Back ease, overshoots in between.</summary>
        [Test]
        public void EasedMotion_FollowsDOTweenCurves()
        {
            var motion = new EasedMotion(2f, Ease.OutBack);
            var largest = 0f;
            for (var step = 0; step <= 20; step++)
            {
                largest = Mathf.Max(largest, motion.Evaluate(step * 0.1f, 2f));
            }

            Assert.That(motion.Evaluate(0f, 2f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(motion.Evaluate(2f, 2f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(largest, Is.GreaterThan(1f));
        }

        /// <summary>Creates a palette with every role set to one colour.</summary>
        private static ThemePalette CreateUniformPalette(Color colour)
        {
            var palette = new ThemePalette();
            var json = "{";
            foreach (ThemeColour role in Enum.GetValues(typeof(ThemeColour)))
            {
                var field = char.ToLowerInvariant(role.ToString()[0]) + role.ToString().Substring(1);
                json += $"\"{field}\":{JsonUtility.ToJson(colour)},";
            }

            JsonUtility.FromJsonOverwrite(json.TrimEnd(',') + "}", palette);
            return palette;
        }
    }
}

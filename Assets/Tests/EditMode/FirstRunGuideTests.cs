using GameOfLife.UI.HowToPlay;
using NUnit.Framework;
using UnityEngine;

namespace GameOfLife.Tests
{
    /// <summary>Checks that FirstRunGuide reports a first run until the guide is marked seen, and remembers that on the device under its own key.</summary>
    [TestFixture]
    public sealed class FirstRunGuideTests
    {
        private const string TestStorageKey = "Tests.HowToPlaySeen";
        private const string OtherTestStorageKey = "Tests.HowToPlaySeen.Other";

        /// <summary>Starts every test with nothing saved under either test key.</summary>
        [SetUp]
        public void ClearSavedState()
        {
            PlayerPrefs.DeleteKey(TestStorageKey);
            PlayerPrefs.DeleteKey(OtherTestStorageKey);
        }

        /// <summary>Removes both test keys so tests never leave data behind.</summary>
        [TearDown]
        public void RemoveSavedState()
        {
            PlayerPrefs.DeleteKey(TestStorageKey);
            PlayerPrefs.DeleteKey(OtherTestStorageKey);
        }

        /// <summary>A device that never closed the guide is on its first run.</summary>
        [Test]
        public void IsFirstRun_WithNothingSaved_IsTrue()
        {
            Assert.That(new FirstRunGuide(TestStorageKey).IsFirstRun, Is.True, "A device with nothing saved should be on its first run.");
        }

        /// <summary>Marking the guide seen ends the first run straight away and saves it, so a new guide on the next launch agrees.</summary>
        [Test]
        public void MarkSeen_EndsTheFirstRun_AndSurvivesAReload()
        {
            var guide = new FirstRunGuide(TestStorageKey);

            guide.MarkSeen();

            Assert.That(guide.IsFirstRun, Is.False, "The guide should stop being a first run once marked seen.");
            Assert.That(new FirstRunGuide(TestStorageKey).IsFirstRun, Is.False, "A new guide with the same key should remember it was seen.");
        }

        /// <summary>Marking the guide seen under one key leaves a guide under a different key on its first run.</summary>
        [Test]
        public void MarkSeen_UnderOneKey_LeavesADifferentKeyOnItsFirstRun()
        {
            new FirstRunGuide(TestStorageKey).MarkSeen();

            Assert.That(new FirstRunGuide(OtherTestStorageKey).IsFirstRun, Is.True, "A guide under a different key should still be on its first run.");
        }
    }
}

using GameOfLife.Purchasing;
using NUnit.Framework;
using UnityEngine;

namespace GameOfLife.Tests
{
    /// <summary>Checks that ad pass ownership is saved, reloaded, revoked and reported exactly when it changes.</summary>
    [TestFixture]
    public sealed class AdPassOwnershipTests
    {
        private const string TestStorageKey = "Tests.AdPassOwned";

        /// <summary>Starts every test with no saved ownership.</summary>
        [SetUp]
        public void ClearSavedOwnership()
        {
            PlayerPrefs.DeleteKey(TestStorageKey);
        }

        /// <summary>Removes the test key so tests never leave data behind.</summary>
        [TearDown]
        public void RemoveSavedOwnership()
        {
            PlayerPrefs.DeleteKey(TestStorageKey);
        }

        /// <summary>A device that never bought the pass does not own it.</summary>
        [Test]
        public void NewOwnership_WithNothingSaved_IsNotOwned()
        {
            Assert.That(new AdPassOwnership(TestStorageKey).IsOwned, Is.False);
        }

        /// <summary>Granting saves straight away, so a fresh load on the next launch still owns the pass.</summary>
        [Test]
        public void Grant_SavesImmediately_AndSurvivesAReload()
        {
            new AdPassOwnership(TestStorageKey).Grant();

            Assert.That(PlayerPrefs.GetInt(TestStorageKey, 0), Is.EqualTo(1));
            Assert.That(new AdPassOwnership(TestStorageKey).IsOwned, Is.True);
        }

        /// <summary>Revoking removes ownership and saves that too.</summary>
        [Test]
        public void Revoke_AfterGrant_RemovesOwnershipAcrossReloads()
        {
            var ownership = new AdPassOwnership(TestStorageKey);
            ownership.Grant();

            ownership.Revoke();

            Assert.That(ownership.IsOwned, Is.False);
            Assert.That(new AdPassOwnership(TestStorageKey).IsOwned, Is.False);
        }

        /// <summary>The change event fires once per real change, so a purchase re-delivered by the store does not re-trigger the UI.</summary>
        [Test]
        public void OwnershipChanged_FiresOnlyWhenOwnershipActuallyChanges()
        {
            var ownership = new AdPassOwnership(TestStorageKey);
            var changes = 0;
            ownership.OwnershipChanged += owned => changes++;

            ownership.Grant();
            ownership.Grant();
            ownership.Revoke();
            ownership.Revoke();

            Assert.That(changes, Is.EqualTo(2));
        }
    }
}

using System.Text.RegularExpressions;
using GameOfLife.HowToPlay;
using GameOfLife.UI.HowToPlay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameOfLife.Tests
{
    /// <summary>Checks that a guide card with a broken demo shows as text only and logs why, instead of stopping start-up.</summary>
    [TestFixture]
    public sealed class GuideStepViewTests
    {
        private const string StepPrefabPath = "Assets/Prefabs/UI/HowToPlay/GuideStep.prefab";
        private const string BoardName = "Board";
        private const string BrokenStepJson = "{\"heading\":\"BROKEN\",\"body\":\"A ragged start board.\",\"demo\":{\"versusColours\":false,\"shadeAppHalf\":false,\"secondsPerStep\":0.5,\"secondsBeforeRepeat\":0.5,\"start\":[\"...\",\"..\",\"...\"],\"steps\":[{\"kind\":1,\"cells\":[]}]}}";

        private GameObject card;

        /// <summary>Destroys the card each test made.</summary>
        [TearDown]
        public void DestroyCard()
        {
            if (card)
            {
                Object.DestroyImmediate(card);
            }
        }

        /// <summary>A step whose demo cannot be built hides its board and logs an error naming the step, without throwing.</summary>
        [Test]
        public void Initialise_WithABrokenDemo_HidesTheBoardAndLogsAnError()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GuideStepView>(StepPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"No {nameof(GuideStepView)} prefab at {StepPrefabPath}.");
            var step = JsonUtility.FromJson<GuideStep>(BrokenStepJson);
            Assert.That(step.Demo.HasBoard, Is.True, "The broken step should still have a start board to fail on.");
            var view = Object.Instantiate(prefab);
            card = view.gameObject;

            LogAssert.Expect(LogType.Error, new Regex("BROKEN.*text only"));
            Assert.DoesNotThrow(() => view.Initialise(step, null));

            var board = view.transform.Find(BoardName);
            Assert.That(board, Is.Not.Null, $"The card has no '{BoardName}' child.");
            Assert.That(board.gameObject.activeSelf, Is.False, "A card with a broken demo should hide its board.");
            Assert.DoesNotThrow(view.PlayDemo, "Playing a card with a broken demo should do nothing.");
            Assert.DoesNotThrow(view.StopDemo, "Stopping a card with a broken demo should do nothing.");
        }
    }
}

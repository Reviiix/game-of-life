using GameOfLife.Opponents;
using NUnit.Framework;

namespace GameOfLife.Tests
{
    /// <summary>Checks the promise behind the difficulty setting: the harder the app, the more of its squares are still alive and its own later, and the more it wins.</summary>
    [TestFixture]
    public sealed class OpponentDifficultyOrderingTests
    {
        private const int Matches = 40;
        private const int SeedBase = 0;
        private const int FiveGenerations = 0;
        private const int TwentyGenerations = 1;

        /// <summary>Against the same Medium player, each tier keeps clearly more of its squares alive 5 and 20 generations later than the tier below, and wins more often.</summary>
        [Test]
        public void HarderTiers_KeepMoreSquaresAlive_AndWinMore()
        {
            var easy = Play(OpponentDifficulty.Easy);
            var medium = Play(OpponentDifficulty.Medium);
            var hard = Play(OpponentDifficulty.Hard);

            Assert.That(medium.GetSurvival(FiveGenerations), Is.GreaterThanOrEqualTo(easy.GetSurvival(FiveGenerations) + 0.2), "Medium vs Easy at 5 generations");
            Assert.That(hard.GetSurvival(FiveGenerations), Is.GreaterThanOrEqualTo(medium.GetSurvival(FiveGenerations) + 0.15), "Hard vs Medium at 5 generations");
            Assert.That(medium.GetSurvival(TwentyGenerations), Is.GreaterThanOrEqualTo(easy.GetSurvival(TwentyGenerations) + 0.08), "Medium vs Easy at 20 generations");
            Assert.That(hard.GetSurvival(TwentyGenerations), Is.GreaterThanOrEqualTo(medium.GetSurvival(TwentyGenerations) + 0.1), "Hard vs Medium at 20 generations");
            Assert.That(medium.WinRate, Is.GreaterThan(easy.WinRate), "Medium vs Easy win rate");
            Assert.That(hard.WinRate, Is.GreaterThan(medium.WinRate), "Hard vs Medium win rate");
        }

        /// <summary>Plays the app at the difficulty against a Medium player and returns the measurements.</summary>
        private static VersusMatchSimulator Play(OpponentDifficulty appDifficulty)
        {
            var simulator = new VersusMatchSimulator(5, 20);
            simulator.PlayMatches(appDifficulty, OpponentDifficulty.Medium, Matches, SeedBase);
            return simulator;
        }
    }
}

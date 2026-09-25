using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class ResourceBiasTests
    {
        [Test]
        public void PickBestScoringCandidate_NullOrEmpty_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, ResourceBias.PickBestScoringCandidate(null));
            Assert.AreEqual(-1, ResourceBias.PickBestScoringCandidate(new float[0]));
        }

        [Test]
        public void PickBestScoringCandidate_PicksTheHighestScore()
        {
            Assert.AreEqual(2, ResourceBias.PickBestScoringCandidate(new[] { 0.1f, 0.4f, 0.9f, 0.3f }));
        }

        [Test]
        public void PickBestScoringCandidate_OnATie_KeepsTheFirst()
        {
            Assert.AreEqual(1, ResourceBias.PickBestScoringCandidate(new[] { 0.2f, 0.9f, 0.9f }));
        }

        [Test]
        public void PickBestScoringCandidate_SingleCandidate_ReturnsIt()
        {
            Assert.AreEqual(0, ResourceBias.PickBestScoringCandidate(new[] { 0.5f }));
        }
    }
}

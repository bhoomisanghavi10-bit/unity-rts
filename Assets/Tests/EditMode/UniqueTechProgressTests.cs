using NUnit.Framework;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Tests
{
    // Repository-audit finding F08: UniqueTechProgress previously had no
    // reset method at all, so a faction's researched flag could leak into
    // the next match/test started in the same process - this is exactly
    // what made ScoreProgressTests.Technology_AddsFiftyPoints_
    // OnceUniqueTechResearched fail intermittently ("Expected: 50, But
    // was: 0"). These tests cover the registry directly, isolated from
    // ScoreProgress's own weighting.
    public class UniqueTechProgressTests
    {
        private const FactionId TestFaction = FactionId.Enemy2;

        [SetUp]
        public void SetUp()
        {
            UniqueTechProgress.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            UniqueTechProgress.ResetForTests();
        }

        [Test]
        public void HasResearched_IsFalse_ByDefault()
        {
            Assert.IsFalse(UniqueTechProgress.HasResearched(TestFaction));
        }

        [Test]
        public void MarkResearched_FlipsHasResearched_ToTrue()
        {
            UniqueTechProgress.MarkResearched(TestFaction);

            Assert.IsTrue(UniqueTechProgress.HasResearched(TestFaction));
        }

        [Test]
        public void MarkResearched_DoesNotAffectOtherFactions()
        {
            UniqueTechProgress.MarkResearched(TestFaction);

            Assert.IsFalse(UniqueTechProgress.HasResearched(FactionId.Player));
            Assert.IsFalse(UniqueTechProgress.HasResearched(FactionId.Enemy));
        }

        [Test]
        public void Reset_ClearsAPreviouslyResearchedFlag()
        {
            UniqueTechProgress.MarkResearched(TestFaction);
            Assert.IsTrue(UniqueTechProgress.HasResearched(TestFaction));

            UniqueTechProgress.Reset();

            Assert.IsFalse(UniqueTechProgress.HasResearched(TestFaction));
        }
    }
}

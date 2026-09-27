using NUnit.Framework;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Tests
{
    // Repository-audit finding F08: EconomyTechProgress previously had no
    // reset method at all, same class of leak as UniqueTechProgress (see
    // UniqueTechProgressTests). Covers the registry directly.
    public class EconomyTechProgressTests
    {
        private const FactionId TestFaction = FactionId.Enemy2;

        [SetUp]
        public void SetUp()
        {
            EconomyTechProgress.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            EconomyTechProgress.ResetForTests();
        }

        [Test]
        public void HasResearched_IsFalse_ByDefault()
        {
            Assert.IsFalse(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.ImprovedTools));
        }

        [Test]
        public void MarkResearched_FlipsOnlyThatFactionAndTech()
        {
            EconomyTechProgress.MarkResearched(TestFaction, EconomyTech.PackMules);

            Assert.IsTrue(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.PackMules));
            Assert.IsFalse(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.ImprovedTools));
            Assert.IsFalse(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.TradeDiscounts));
            Assert.IsFalse(EconomyTechProgress.HasResearched(FactionId.Player, EconomyTech.PackMules));
        }

        [Test]
        public void Reset_ClearsEveryPreviouslyResearchedTech()
        {
            EconomyTechProgress.MarkResearched(TestFaction, EconomyTech.ImprovedTools);
            EconomyTechProgress.MarkResearched(TestFaction, EconomyTech.TradeDiscounts);

            EconomyTechProgress.Reset();

            Assert.IsFalse(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.ImprovedTools));
            Assert.IsFalse(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.TradeDiscounts));
        }
    }
}

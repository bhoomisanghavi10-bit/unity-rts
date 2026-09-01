using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // AoE-parity Phase 3.2 (team-bonus/alliance economic stacking):
    // TeamBonus.HasAlly is the one piece of pure logic this feature adds -
    // the 5 actual bonus hooks live inside factories/Market and are
    // verified live in Play mode instead (no existing EditMode precedent
    // for testing factory-spawn output in this codebase). Uses
    // FactionId.Enemy/Enemy2 (not Player) and restores DiplomacyRegistry
    // state in TearDown, matching CivPassiveBonusTests.cs's documented
    // convention that these statics persist across tests in the same run.
    public class TeamBonusTests
    {
        [TearDown]
        public void TearDown()
        {
            DiplomacyRegistry.Reset();
        }

        [Test]
        public void HasAlly_ReturnsFalse_WhenNoAllianceSet()
        {
            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Maurya);

            Assert.IsFalse(TeamBonus.HasAlly(FactionId.Enemy2, CivilizationId.Maurya));
        }

        [Test]
        public void HasAlly_ReturnsTrue_WhenAlliedFactionHasMatchingCiv()
        {
            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Vijayanagara);
            DiplomacyRegistry.SetAllied(FactionId.Enemy, FactionId.Enemy2, true);

            Assert.IsTrue(TeamBonus.HasAlly(FactionId.Enemy2, CivilizationId.Vijayanagara));
        }

        [Test]
        public void HasAlly_ReturnsFalse_WhenAlliedFactionHasDifferentCiv()
        {
            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Rajput);
            DiplomacyRegistry.SetAllied(FactionId.Enemy, FactionId.Enemy2, true);

            Assert.IsFalse(TeamBonus.HasAlly(FactionId.Enemy2, CivilizationId.Maratha));
        }

        [Test]
        public void HasAlly_ReturnsFalse_ForOwnCivEvenThoughAreAlliedTreatsSelfAsAllied()
        {
            // DiplomacyRegistry.AreAllied(a, a) is true by design (same
            // faction), but a civ's own bonus is a different, already-
            // existing code path from its team bonus - HasAlly must not
            // conflate the two.
            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Chola);

            Assert.IsTrue(DiplomacyRegistry.AreAllied(FactionId.Enemy, FactionId.Enemy));
            Assert.IsFalse(TeamBonus.HasAlly(FactionId.Enemy, CivilizationId.Chola));
        }

        [Test]
        public void HasAlly_ReturnsFalse_WhenAlliedFactionIsAtWarInstead()
        {
            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Maurya);
            DiplomacyRegistry.SetAllied(FactionId.Enemy, FactionId.Enemy2, false);

            Assert.IsFalse(TeamBonus.HasAlly(FactionId.Enemy2, CivilizationId.Maurya));
        }
    }
}

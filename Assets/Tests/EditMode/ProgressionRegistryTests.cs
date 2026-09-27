using NUnit.Framework;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Tests
{
    // Repository-audit finding F08 ("Static progression can leak across
    // matches"): CivilizationSetup.BeginMatchCore reset only Diplomacy/
    // TeamColor/ScoreProgress; every other progression registry (flat
    // Attack/Armor tiers, per-unit-class tiers, unique tech, economy tech,
    // hero-trained flag, unique-unit elite tiers, every unit tier ladder)
    // had a test-only ResetForTests() that production code never called.
    //
    // This is the direct regression coverage for that gap: advance one
    // representative field per registry for a faction, call the exact
    // production entry point CivilizationSetup.BeginMatchCore now calls
    // (ProgressionRegistry.ResetAllForNewMatch), and confirm every one of
    // them reports fresh/default state again - the scene-free equivalent
    // of "research an upgrade, finish a match, choose Play Again, verify a
    // clean opening" from the audit's own F08 acceptance criteria (a real
    // BeginMatchCore run needs a live scene/MonoBehaviour context that
    // isn't available to an EditMode test, so this exercises the same
    // reset function it actually calls instead).
    public class ProgressionRegistryTests
    {
        private const FactionId TestFaction = FactionId.Enemy2;
        private const string EliteUnitId = "chola_naval_raider";

        [SetUp]
        public void SetUp()
        {
            ProgressionRegistry.ResetAllForNewMatch();
        }

        [TearDown]
        public void TearDown()
        {
            ProgressionRegistry.ResetAllForNewMatch();
        }

        [Test]
        public void ResetAllForNewMatch_ClearsEveryProgressionRegistry_ForTheAffectedFaction()
        {
            // Advance one representative piece of state per registry.
            // Every one of the 14 unit-tier ladders shares the identical
            // AdvanceTier(faction)/Tier(faction) shape (confirmed by
            // reading each file directly before writing this test), so
            // all 14 are exercised here, not just a sample - a registry
            // that ProgressionRegistry.ResetAllForNewMatch forgets to
            // reset would otherwise go undetected if only a few of the 14
            // near-identical classes were checked.
            UpgradeProgress.AdvanceAttack(TestFaction);
            UpgradeProgress.AdvanceArmor(TestFaction);
            UpgradeProgress.AdvanceClassAttack(TestFaction, UnitClass.Cavalry);
            UniqueTechProgress.MarkResearched(TestFaction);
            EconomyTechProgress.MarkResearched(TestFaction, EconomyTech.ImprovedTools);
            HeroProgress.MarkTrained(TestFaction);
            UniqueUnitEliteProgress.AdvanceTier(TestFaction, EliteUnitId);
            InfantryLineProgress.AdvanceTier(TestFaction);
            SpearmanLineProgress.AdvanceTier(TestFaction);
            ArcherLineProgress.AdvanceTier(TestFaction);
            CavalryLineProgress.AdvanceTier(TestFaction);
            SiegeLineProgress.AdvanceTier(TestFaction);
            NavalLineProgress.AdvanceTier(TestFaction);
            ScoutLineProgress.AdvanceTier(TestFaction);
            SkirmisherLineProgress.AdvanceTier(TestFaction);
            CavalryArcherLineProgress.AdvanceTier(TestFaction);
            CamelRiderLineProgress.AdvanceTier(TestFaction);
            ScorpionLineProgress.AdvanceTier(TestFaction);
            BatteringRamLineProgress.AdvanceTier(TestFaction);
            ElephantLineProgress.AdvanceTier(TestFaction);
            FireShipLineProgress.AdvanceTier(TestFaction);

            // Sanity-check the state actually advanced before resetting -
            // otherwise the assertions below could pass vacuously.
            Assert.Greater(UpgradeProgress.AttackTier(TestFaction), 0);
            Assert.Greater(UpgradeProgress.ArmorTier(TestFaction), 0);
            Assert.Greater(UpgradeProgress.ClassAttackTier(TestFaction, UnitClass.Cavalry), 0);
            Assert.IsTrue(UniqueTechProgress.HasResearched(TestFaction));
            Assert.IsTrue(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.ImprovedTools));
            Assert.IsTrue(HeroProgress.HasTrainedHero(TestFaction));
            Assert.IsTrue(UniqueUnitEliteProgress.IsElite(TestFaction, EliteUnitId));
            Assert.Greater(InfantryLineProgress.Tier(TestFaction), 0);
            Assert.Greater(SpearmanLineProgress.Tier(TestFaction), 0);
            Assert.Greater(ArcherLineProgress.Tier(TestFaction), 0);
            Assert.Greater(CavalryLineProgress.Tier(TestFaction), 0);
            Assert.Greater(SiegeLineProgress.Tier(TestFaction), 0);
            Assert.Greater(NavalLineProgress.Tier(TestFaction), 0);
            Assert.Greater(ScoutLineProgress.Tier(TestFaction), 0);
            Assert.Greater(SkirmisherLineProgress.Tier(TestFaction), 0);
            Assert.Greater(CavalryArcherLineProgress.Tier(TestFaction), 0);
            Assert.Greater(CamelRiderLineProgress.Tier(TestFaction), 0);
            Assert.Greater(ScorpionLineProgress.Tier(TestFaction), 0);
            Assert.Greater(BatteringRamLineProgress.Tier(TestFaction), 0);
            Assert.Greater(ElephantLineProgress.Tier(TestFaction), 0);
            Assert.Greater(FireShipLineProgress.Tier(TestFaction), 0);

            ProgressionRegistry.ResetAllForNewMatch();

            Assert.AreEqual(0, UpgradeProgress.AttackTier(TestFaction));
            Assert.AreEqual(0, UpgradeProgress.ArmorTier(TestFaction));
            Assert.AreEqual(0, UpgradeProgress.ClassAttackTier(TestFaction, UnitClass.Cavalry));
            Assert.IsFalse(UniqueTechProgress.HasResearched(TestFaction));
            Assert.IsFalse(EconomyTechProgress.HasResearched(TestFaction, EconomyTech.ImprovedTools));
            Assert.IsFalse(HeroProgress.HasTrainedHero(TestFaction));
            Assert.IsFalse(UniqueUnitEliteProgress.IsElite(TestFaction, EliteUnitId));
            Assert.AreEqual(0, InfantryLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, SpearmanLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, ArcherLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, CavalryLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, SiegeLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, NavalLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, ScoutLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, SkirmisherLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, CavalryArcherLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, CamelRiderLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, ScorpionLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, BatteringRamLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, ElephantLineProgress.Tier(TestFaction));
            Assert.AreEqual(0, FireShipLineProgress.Tier(TestFaction));
        }

        [Test]
        public void ResetAllForNewMatch_DoesNotThrow_WhenNothingWasEverAdvanced()
        {
            Assert.DoesNotThrow(() => ProgressionRegistry.ResetAllForNewMatch());
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 23 (docs/IMPLEMENTATION_ROADMAP.md): the Scorpion tier
    // ladder - a dedicated anti-infantry siege weapon. Mirrors
    // CamelRiderLineTests' coverage shape - ScorpionLineProgress's own
    // gating (age requirement, sequential tiers, only 1 research step),
    // Barracks.RequestResearchScorpionTier's request/cost/gate behavior,
    // ScorpionFactory actually baking the current tier's bonus/name in at
    // spawn - not retroactively - and its deliberate new UnitClass.Scorpion
    // classification with its 2 new CombatBonus pairings. The pierce-
    // through MECHANIC itself (MeleeAttacker.SetPierceThrough) is covered
    // separately in ScorpionPierceThroughTests.cs, mirroring
    // SiegeSplashTests' own split between line-progress coverage and
    // combat-mechanic coverage.
    public class ScorpionLineTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            ScorpionLineProgress.ResetForTests();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Barracks CreateBarracks(FactionId faction)
        {
            GameObject go = CreateGameObject("Barracks");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Barracks>();
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float gold = 1000f, float wood = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile-" + faction).AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Gold, gold);
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        // --- ScorpionLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_BanaYantra()
        {
            Assert.AreEqual(0, ScorpionLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Bana Yantra", ScorpionLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(ScorpionLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < ScorpionLineProgress.MaxTier; i++)
            {
                ScorpionLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(ScorpionLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsFalse(ScorpionLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Assert.IsTrue(ScorpionLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            ScorpionLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Bana Yantra", ScorpionLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchScorpionTier ---

        [Test]
        public void RequestResearchScorpionTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchScorpionTier();

            Assert.IsTrue(barracks.IsResearchingScorpionTier);
            Assert.AreEqual(1000f - ScorpionLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - ScorpionLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchScorpionTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchScorpionTier();

            Assert.IsFalse(barracks.IsResearchingScorpionTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchScorpionTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchScorpionTier();

            Assert.IsFalse(barracks.IsResearchingScorpionTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchScorpionTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchScorpionTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchScorpionTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchScorpionTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < ScorpionLineProgress.MaxTier; i++)
            {
                ScorpionLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchScorpionTier();

            Assert.IsFalse(barracks.IsResearchingScorpionTier);
        }

        // --- ScorpionFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void ScorpionFactory_BakesCurrentTierNameAndDamageAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = ScorpionFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            ScorpionLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = ScorpionFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Bana Yantra", upgraded.name);
            StringAssert.DoesNotContain("Maha Bana Yantra", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        // --- Deliberate new UnitClass.Scorpion classification ---

        [Test]
        public void ScorpionFactory_ClassifiesAsScorpion_OptedIntoUpgradeScaling()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = ScorpionFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.Scorpion, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void CombatBonus_ScorpionHardCountersInfantry_WeakVsCavalry()
        {
            // New CombatBonus pairings added for this item: 2x reuses the
            // closest existing "hard counter vs one class" precedent;
            // 1.5x reuses Cavalry->Infantry's own value directly (a fast
            // unit closes the gap on this unarmored, immobile engine).
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.Scorpion, UnitClass.Infantry));
            Assert.AreEqual(1.5f, CombatBonus.Multiplier(UnitClass.Cavalry, UnitClass.Scorpion));
        }
    }
}

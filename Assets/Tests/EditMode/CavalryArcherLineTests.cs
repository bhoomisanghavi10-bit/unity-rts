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
    // Wave 4 item (docs/IMPLEMENTATION_ROADMAP.md): the Cavalry Archer tier
    // ladder - a mobile ranged raider. Mirrors SkirmisherLineTests'/
    // ScoutLineTests' coverage shape - CavalryArcherLineProgress's own
    // gating (age requirement, sequential tiers, only 1 research step),
    // Barracks.RequestResearchCavalryArcherTier's request/cost/gate
    // behavior, CavalryArcherFactory actually baking the current tier's
    // bonus/name in at spawn - not retroactively - and its deliberate
    // Archer classification (see CavalryArcherFactory's own header comment)
    // keeping it inside the existing counter web for free.
    public class CavalryArcherLineTests
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
            CavalryArcherLineProgress.ResetForTests();
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

        // --- CavalryArcherLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_AshvaDhanurdhara()
        {
            Assert.AreEqual(0, CavalryArcherLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Ashva Dhanurdhara", CavalryArcherLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(CavalryArcherLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < CavalryArcherLineProgress.MaxTier; i++)
            {
                CavalryArcherLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(CavalryArcherLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsFalse(CavalryArcherLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Assert.IsTrue(CavalryArcherLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            CavalryArcherLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Ashva Dhanurdhara", CavalryArcherLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchCavalryArcherTier ---

        [Test]
        public void RequestResearchCavalryArcherTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryArcherTier();

            Assert.IsTrue(barracks.IsResearchingCavalryArcherTier);
            Assert.AreEqual(1000f - CavalryArcherLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - CavalryArcherLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchCavalryArcherTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryArcherTier();

            Assert.IsFalse(barracks.IsResearchingCavalryArcherTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCavalryArcherTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchCavalryArcherTier();

            Assert.IsFalse(barracks.IsResearchingCavalryArcherTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCavalryArcherTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryArcherTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchCavalryArcherTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCavalryArcherTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < CavalryArcherLineProgress.MaxTier; i++)
            {
                CavalryArcherLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryArcherTier();

            Assert.IsFalse(barracks.IsResearchingCavalryArcherTier);
        }

        // --- CavalryArcherFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void CavalryArcherFactory_BakesCurrentTierNameAndDamageAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = CavalryArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            CavalryArcherLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = CavalryArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Ashva Dhanurdhara", upgraded.name);
            StringAssert.DoesNotContain("Maha Ashva Dhanurdhara", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        // --- Deliberate Archer classification (see CavalryArcherFactory's own header) ---

        [Test]
        public void CavalryArcherFactory_ClassifiesAsArcher_OptedIntoUpgradeScaling()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = CavalryArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.Archer, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void CombatBonus_CavalryArcherClassInheritsExistingArcherPairings()
        {
            // Because CavalryArcherFactory classifies as UnitClass.Archer
            // (a deliberate design call, not a new class), it automatically
            // hard-counters Cavalry and is hard-countered by Skirmisher -
            // no new CombatBonus entries were needed for this unit.
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.Archer, UnitClass.Cavalry));
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.Skirmisher, UnitClass.Archer));
        }
    }
}

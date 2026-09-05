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
    // Wave 4 item 22 (docs/IMPLEMENTATION_ROADMAP.md): the Camel Rider tier
    // ladder - a mounted anti-cavalry specialist. Mirrors
    // CavalryArcherLineTests' coverage shape - CamelRiderLineProgress's own
    // gating (age requirement, sequential tiers, only 1 research step),
    // Barracks.RequestResearchCamelRiderTier's request/cost/gate behavior,
    // CamelRiderFactory actually baking the current tier's bonus/name in at
    // spawn - not retroactively - and its deliberate new UnitClass.Camel
    // classification (see CamelRiderFactory's own header comment) with its
    // 2 new CombatBonus pairings, reused directly from Spearman's own.
    public class CamelRiderLineTests
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
            CamelRiderLineProgress.ResetForTests();
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

        // --- CamelRiderLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Ushtrarohi()
        {
            Assert.AreEqual(0, CamelRiderLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Ushtrarohi", CamelRiderLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(CamelRiderLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < CamelRiderLineProgress.MaxTier; i++)
            {
                CamelRiderLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(CamelRiderLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsFalse(CamelRiderLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Assert.IsTrue(CamelRiderLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            CamelRiderLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Ushtrarohi", CamelRiderLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchCamelRiderTier ---

        [Test]
        public void RequestResearchCamelRiderTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCamelRiderTier();

            Assert.IsTrue(barracks.IsResearchingCamelRiderTier);
            Assert.AreEqual(1000f - CamelRiderLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - CamelRiderLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchCamelRiderTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCamelRiderTier();

            Assert.IsFalse(barracks.IsResearchingCamelRiderTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCamelRiderTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchCamelRiderTier();

            Assert.IsFalse(barracks.IsResearchingCamelRiderTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCamelRiderTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCamelRiderTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchCamelRiderTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCamelRiderTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < CamelRiderLineProgress.MaxTier; i++)
            {
                CamelRiderLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchCamelRiderTier();

            Assert.IsFalse(barracks.IsResearchingCamelRiderTier);
        }

        // --- CamelRiderFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void CamelRiderFactory_BakesCurrentTierNameAndDamageAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = CamelRiderFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            CamelRiderLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = CamelRiderFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Ushtrarohi", upgraded.name);
            StringAssert.DoesNotContain("Maha Ushtrarohi", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        // --- Deliberate new UnitClass.Camel classification (see CamelRiderFactory's own header) ---

        [Test]
        public void CamelRiderFactory_ClassifiesAsCamel_OptedIntoUpgradeScaling()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = CamelRiderFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.Camel, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void CombatBonus_CamelHardCountersCavalry_WeakVsInfantry()
        {
            // New CombatBonus pairings added for this item, values reused
            // directly from Spearman's own (the closest existing precedent
            // for "a unit built to counter Cavalry").
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.Camel, UnitClass.Cavalry));
            Assert.AreEqual(1.25f, CombatBonus.Multiplier(UnitClass.Infantry, UnitClass.Camel));
        }
    }
}

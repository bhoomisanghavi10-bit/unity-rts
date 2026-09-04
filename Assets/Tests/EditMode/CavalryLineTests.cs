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
    // Wave 3 item 12 (docs/IMPLEMENTATION_ROADMAP.md): the Cavalry/Knight
    // tier ladder. Mirrors SpearmanLineTests's coverage shape exactly -
    // CavalryLineProgress's own gating (age requirement, sequential
    // tiers), Barracks.RequestResearchCavalryTier's request/cost/gate
    // behavior, and CavalryFactory actually baking the current tier's
    // bonus/name in at spawn - not retroactively.
    public class CavalryLineTests
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
            CavalryLineProgress.ResetForTests();
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

        // --- CavalryLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Ashvarohi()
        {
            Assert.AreEqual(0, CavalryLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Ashvarohi", CavalryLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(CavalryLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < CavalryLineProgress.MaxTier; i++)
            {
                CavalryLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(CavalryLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsFalse(CavalryLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Assert.IsTrue(CavalryLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            CavalryLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Ashvarohi", CavalryLineProgress.Current(FactionId.Player).Name);
            CavalryLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Vir Ashvarohi", CavalryLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchCavalryTier ---

        [Test]
        public void RequestResearchCavalryTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryTier();

            Assert.IsTrue(barracks.IsResearchingCavalryTier);
            Assert.AreEqual(1000f - CavalryLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - CavalryLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchCavalryTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryTier();

            Assert.IsFalse(barracks.IsResearchingCavalryTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCavalryTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchCavalryTier();

            Assert.IsFalse(barracks.IsResearchingCavalryTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCavalryTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchCavalryTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCavalryTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < CavalryLineProgress.MaxTier; i++)
            {
                CavalryLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchCavalryTier();

            Assert.IsFalse(barracks.IsResearchingCavalryTier);
        }

        // --- CavalryFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void CavalryFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = CavalryFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            CavalryLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = CavalryFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Ashvarohi", upgraded.name);
            StringAssert.DoesNotContain("Maha Ashvarohi", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}

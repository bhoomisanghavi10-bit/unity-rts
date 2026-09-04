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
    // Wave 3 item 10 (docs/IMPLEMENTATION_ROADMAP.md): the Spearman tier
    // ladder. Mirrors InfantryLineTests's coverage shape exactly -
    // SpearmanLineProgress's own gating (age requirement, sequential
    // tiers), Barracks.RequestResearchSpearmanTier's request/cost/gate
    // behavior, and SpearmanFactory actually baking the current tier's
    // bonus/name in at spawn - not retroactively.
    public class SpearmanLineTests
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
            SpearmanLineProgress.ResetForTests();
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

        // --- SpearmanLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Bhaladhari()
        {
            Assert.AreEqual(0, SpearmanLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Bhaladhari", SpearmanLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(SpearmanLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < SpearmanLineProgress.MaxTier; i++)
            {
                SpearmanLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(SpearmanLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsFalse(SpearmanLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsTrue(SpearmanLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            SpearmanLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Trishuladhari", SpearmanLineProgress.Current(FactionId.Player).Name);
            SpearmanLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Trishuladhari", SpearmanLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchSpearmanTier ---

        [Test]
        public void RequestResearchSpearmanTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchSpearmanTier();

            Assert.IsTrue(barracks.IsResearchingSpearmanTier);
            Assert.AreEqual(1000f - SpearmanLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - SpearmanLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchSpearmanTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchSpearmanTier();

            Assert.IsFalse(barracks.IsResearchingSpearmanTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchSpearmanTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchSpearmanTier();

            Assert.IsFalse(barracks.IsResearchingSpearmanTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchSpearmanTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchSpearmanTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchSpearmanTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchSpearmanTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < SpearmanLineProgress.MaxTier; i++)
            {
                SpearmanLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchSpearmanTier();

            Assert.IsFalse(barracks.IsResearchingSpearmanTier);
        }

        // --- SpearmanFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void SpearmanFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = SpearmanFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            SpearmanLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = SpearmanFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Trishuladhari", upgraded.name);
            StringAssert.DoesNotContain("Trishuladhari", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}

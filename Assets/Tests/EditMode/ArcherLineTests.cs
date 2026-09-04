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
    // Wave 3 item 11 (docs/IMPLEMENTATION_ROADMAP.md): the Archer tier
    // ladder. Mirrors SpearmanLineTests's coverage shape exactly -
    // ArcherLineProgress's own gating (age requirement, sequential
    // tiers), Barracks.RequestResearchArcherTier's request/cost/gate
    // behavior, and ArcherFactory actually baking the current tier's
    // bonus/name in at spawn - not retroactively.
    public class ArcherLineTests
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
            ArcherLineProgress.ResetForTests();
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

        // --- ArcherLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Dhanurdhara()
        {
            Assert.AreEqual(0, ArcherLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Dhanurdhara", ArcherLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(ArcherLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < ArcherLineProgress.MaxTier; i++)
            {
                ArcherLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(ArcherLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsFalse(ArcherLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsTrue(ArcherLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            ArcherLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Yantra Dhanurdhara", ArcherLineProgress.Current(FactionId.Player).Name);
            ArcherLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Dhanurdhara", ArcherLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchArcherTier ---

        [Test]
        public void RequestResearchArcherTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchArcherTier();

            Assert.IsTrue(barracks.IsResearchingArcherTier);
            Assert.AreEqual(1000f - ArcherLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - ArcherLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchArcherTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchArcherTier();

            Assert.IsFalse(barracks.IsResearchingArcherTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchArcherTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchArcherTier();

            Assert.IsFalse(barracks.IsResearchingArcherTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchArcherTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchArcherTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchArcherTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchArcherTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < ArcherLineProgress.MaxTier; i++)
            {
                ArcherLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchArcherTier();

            Assert.IsFalse(barracks.IsResearchingArcherTier);
        }

        // --- ArcherFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void ArcherFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = ArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            ArcherLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = ArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Yantra Dhanurdhara", upgraded.name);
            StringAssert.DoesNotContain("Yantra Dhanurdhara", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}

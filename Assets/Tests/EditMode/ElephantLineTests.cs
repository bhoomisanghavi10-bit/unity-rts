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
    // Wave 3 item 13 (docs/IMPLEMENTATION_ROADMAP.md): the Elephant tier
    // ladder. Mirrors CavalryLineTests's coverage shape, except research
    // lives on Durg (where War Elephants train), not Barracks -
    // ElephantLineProgress's own gating (age requirement, sequential
    // tiers), Durg.RequestResearchElephantTier's request/cost/gate
    // behavior, Durg.TrainsElephant's civ-detection, and both War Elephant
    // factories actually baking the current tier's bonus/name in at spawn -
    // not retroactively.
    public class ElephantLineTests
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
            ElephantLineProgress.ResetForTests();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Durg CreateDurg(FactionId faction)
        {
            GameObject go = CreateGameObject("Durg");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Durg>();
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float gold = 1000f, float wood = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile-" + faction).AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Gold, gold);
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        // --- ElephantLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Gajaroha()
        {
            Assert.AreEqual(0, ElephantLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Gajaroha", ElephantLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(ElephantLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < ElephantLineProgress.MaxTier; i++)
            {
                ElephantLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(ElephantLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsFalse(ElephantLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Assert.IsTrue(ElephantLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            ElephantLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Gajaroha", ElephantLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void IsElephantUnitId_TrueForBothWarElephants_FalseForOthers()
        {
            Assert.IsTrue(ElephantLineProgress.IsElephantUnitId("maurya_war_elephant"));
            Assert.IsTrue(ElephantLineProgress.IsElephantUnitId("vijayanagara_war_elephant"));
            Assert.IsFalse(ElephantLineProgress.IsElephantUnitId("rajput_royal_guard"));
        }

        // --- Durg.TrainsElephant ---

        [Test]
        public void TrainsElephant_TrueForMauryaAndVijayanagara_FalseForRajput()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            Assert.IsTrue(CreateDurg(FactionId.Player).TrainsElephant);

            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Vijayanagara);
            Assert.IsTrue(CreateDurg(FactionId.Enemy).TrainsElephant);

            CivilizationRegistry.Assign(FactionId.Enemy2, CivilizationId.Rajput);
            Assert.IsFalse(CreateDurg(FactionId.Enemy2).TrainsElephant);
        }

        // --- Durg.RequestResearchElephantTier ---

        [Test]
        public void RequestResearchElephantTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            durg.RequestResearchElephantTier();

            Assert.IsTrue(durg.IsResearchingElephantTier);
            Assert.AreEqual(1000f - ElephantLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - ElephantLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchElephantTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            durg.RequestResearchElephantTier();

            Assert.IsFalse(durg.IsResearchingElephantTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchElephantTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            durg.RequestResearchElephantTier();

            Assert.IsFalse(durg.IsResearchingElephantTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchElephantTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            durg.RequestResearchElephantTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            durg.RequestResearchElephantTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchElephantTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < ElephantLineProgress.MaxTier; i++)
            {
                ElephantLineProgress.AdvanceTier(FactionId.Player);
            }
            Durg durg = CreateDurg(FactionId.Player);
            CreateStockpile(FactionId.Player);

            durg.RequestResearchElephantTier();

            Assert.IsFalse(durg.IsResearchingElephantTier);
        }

        // --- War Elephant factories: bake current tier in at spawn, not retroactive ---

        [Test]
        public void MauryaWarElephantFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = MauryaWarElephantFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            ElephantLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = MauryaWarElephantFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Gajaroha", upgraded.name);
            StringAssert.DoesNotContain("Maha Gajaroha", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        [Test]
        public void VijayanagaraWarElephantFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Vijayanagara);

            GameObject baseline = VijayanagaraWarElephantFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            ElephantLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = VijayanagaraWarElephantFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Gajaroha", upgraded.name);
            StringAssert.DoesNotContain("Maha Gajaroha", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 3 item 15 (docs/IMPLEMENTATION_ROADMAP.md): the Galley/Naval
    // tier ladder. Mirrors SiegeLineTests's coverage shape exactly -
    // NavalLineProgress's own gating (age requirement, sequential tiers),
    // Dock.RequestResearchNavalTier's request/cost/gate behavior, and
    // WarGalleyFactory actually baking the current tier's bonus/name in at
    // spawn - not retroactively.
    public class NavalLineTests
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
            NavalLineProgress.ResetForTests();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Dock CreateDock(FactionId faction)
        {
            GameObject go = CreateGameObject("Dock");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Dock>();
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float gold = 1000f, float wood = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile-" + faction).AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Gold, gold);
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        // --- NavalLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_RanaNauka()
        {
            Assert.AreEqual(0, NavalLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Rana Nauka", NavalLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(NavalLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < NavalLineProgress.MaxTier; i++)
            {
                NavalLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(NavalLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsFalse(NavalLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsTrue(NavalLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            NavalLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Rana Nauka", NavalLineProgress.Current(FactionId.Player).Name);
            NavalLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Samrat Nauka", NavalLineProgress.Current(FactionId.Player).Name);
        }

        // --- Dock.RequestResearchNavalTier ---

        [Test]
        public void RequestResearchNavalTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            dock.RequestResearchNavalTier();

            Assert.IsTrue(dock.IsResearchingNavalTier);
            Assert.AreEqual(1000f - NavalLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - NavalLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchNavalTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            dock.RequestResearchNavalTier();

            Assert.IsFalse(dock.IsResearchingNavalTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchNavalTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            dock.RequestResearchNavalTier();

            Assert.IsFalse(dock.IsResearchingNavalTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchNavalTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            dock.RequestResearchNavalTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            dock.RequestResearchNavalTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchNavalTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < NavalLineProgress.MaxTier; i++)
            {
                NavalLineProgress.AdvanceTier(FactionId.Player);
            }
            Dock dock = CreateDock(FactionId.Player);
            CreateStockpile(FactionId.Player);

            dock.RequestResearchNavalTier();

            Assert.IsFalse(dock.IsResearchingNavalTier);
        }

        // --- WarGalleyFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void WarGalleyFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);

            GameObject baseline = WarGalleyFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            NavalLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = WarGalleyFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Rana Nauka", upgraded.name);
            StringAssert.DoesNotContain("Maha Rana Nauka", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}

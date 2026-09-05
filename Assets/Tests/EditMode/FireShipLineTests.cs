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
    // Wave 4 item 25 (docs/IMPLEMENTATION_ROADMAP.md): the Fire Ship tier
    // ladder. Mirrors NavalLineTests's coverage shape exactly -
    // FireShipLineProgress's own gating (age requirement, sequential
    // tiers), Dock.RequestResearchFireShipTier's request/cost/gate
    // behavior (an independent track from RequestResearchNavalTier on the
    // same building), and FireShipFactory actually baking the current
    // tier's bonus/name in at spawn - not retroactively.
    public class FireShipLineTests
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
            FireShipLineProgress.ResetForTests();
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

        // --- FireShipLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_AgniNauka()
        {
            Assert.AreEqual(0, FireShipLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Agni Nauka", FireShipLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(FireShipLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < FireShipLineProgress.MaxTier; i++)
            {
                FireShipLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(FireShipLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsFalse(FireShipLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsTrue(FireShipLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            FireShipLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Agni Nauka", FireShipLineProgress.Current(FactionId.Player).Name);
            FireShipLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Vega Agni Nauka", FireShipLineProgress.Current(FactionId.Player).Name);
        }

        // --- Dock.RequestResearchFireShipTier ---

        [Test]
        public void RequestResearchFireShipTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            dock.RequestResearchFireShipTier();

            Assert.IsTrue(dock.IsResearchingFireShipTier);
            Assert.AreEqual(1000f - FireShipLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - FireShipLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchFireShipTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            dock.RequestResearchFireShipTier();

            Assert.IsFalse(dock.IsResearchingFireShipTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchFireShipTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            dock.RequestResearchFireShipTier();

            Assert.IsFalse(dock.IsResearchingFireShipTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchFireShipTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            dock.RequestResearchFireShipTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            dock.RequestResearchFireShipTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchFireShipTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < FireShipLineProgress.MaxTier; i++)
            {
                FireShipLineProgress.AdvanceTier(FactionId.Player);
            }
            Dock dock = CreateDock(FactionId.Player);
            CreateStockpile(FactionId.Player);

            dock.RequestResearchFireShipTier();

            Assert.IsFalse(dock.IsResearchingFireShipTier);
        }

        [Test]
        public void RequestResearchFireShipTier_IndependentFromNavalTier()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Dock dock = CreateDock(FactionId.Player);
            CreateStockpile(FactionId.Player);

            dock.RequestResearchFireShipTier();

            Assert.IsTrue(dock.IsResearchingFireShipTier);
            Assert.IsFalse(dock.IsResearchingNavalTier);
        }

        // --- FireShipFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void FireShipFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);

            GameObject baseline = FireShipFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            FireShipLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = FireShipFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Agni Nauka", upgraded.name);
            StringAssert.DoesNotContain("Maha Agni Nauka", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        [Test]
        public void FireShipFactory_ConfiguresFireShipClassAndFireDamageType()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);

            GameObject go = FireShipFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.FireShip, go.GetComponent<Attackable>().Class);
            Assert.IsNotNull(go.GetComponent<BoatAttacker>());
        }

        // --- CombatBonus: Fire Ship's anti-naval matchup ---

        [Test]
        public void CombatBonus_FireShipVsNaval_IsHardCounter()
        {
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.FireShip, UnitClass.Naval));
        }

        [Test]
        public void CombatBonus_NavalVsFireShip_PunishesTheGlassCannon()
        {
            Assert.AreEqual(1.5f, CombatBonus.Multiplier(UnitClass.Naval, UnitClass.FireShip));
        }
    }
}

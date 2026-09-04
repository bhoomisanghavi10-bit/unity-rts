using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 3 item 9 (docs/IMPLEMENTATION_ROADMAP.md): the Infantry tier
    // ladder. Covers InfantryLineProgress's own gating (age requirement,
    // sequential tiers), Barracks.RequestResearchInfantryTier's
    // request/cost/gate behavior (component-level, same reason
    // BarracksFactory.Place isn't exercised here - see KarmashalaTests'
    // identical note), and SoldierFactory actually baking the current
    // tier's bonus/name in at spawn - not retroactively.
    public class InfantryLineTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");

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
            InfantryLineProgress.ResetForTests();
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

        // --- InfantryLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Padati()
        {
            Assert.AreEqual(0, InfantryLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Padati", InfantryLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(InfantryLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < InfantryLineProgress.MaxTier; i++)
            {
                InfantryLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(InfantryLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            Assert.IsFalse(InfantryLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsTrue(InfantryLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            InfantryLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Senani", InfantryLineProgress.Current(FactionId.Player).Name);
            InfantryLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Khandayata", InfantryLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchInfantryTier ---

        [Test]
        public void RequestResearchInfantryTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchInfantryTier();

            Assert.IsTrue(barracks.IsResearchingInfantryTier);
            Assert.AreEqual(1000f - InfantryLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - InfantryLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchInfantryTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchInfantryTier();

            Assert.IsFalse(barracks.IsResearchingInfantryTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchInfantryTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchInfantryTier();

            Assert.IsFalse(barracks.IsResearchingInfantryTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchInfantryTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchInfantryTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchInfantryTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchInfantryTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < InfantryLineProgress.MaxTier; i++)
            {
                InfantryLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchInfantryTier();

            Assert.IsFalse(barracks.IsResearchingInfantryTier);
        }

        // --- SoldierFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void SoldierFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = SoldierFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            InfantryLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = SoldierFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Senani", upgraded.name);
            StringAssert.DoesNotContain("Senani", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}

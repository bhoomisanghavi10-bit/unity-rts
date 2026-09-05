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
    // Wave 4 item 19 (docs/IMPLEMENTATION_ROADMAP.md): the Skirmisher tier
    // ladder - the anti-archer specialist. Mirrors ScoutLineTests'/
    // SpearmanLineTests' coverage shape - SkirmisherLineProgress's own
    // gating (age requirement, sequential tiers - only 1 research step,
    // unlike every 3-tier Wave 3 line), Barracks.
    // RequestResearchSkirmisherTier's request/cost/gate behavior,
    // SkirmisherFactory actually baking the current tier's bonus/name in at
    // spawn - not retroactively, and CombatBonus's own new Skirmisher<->
    // Archer/Infantry pairings that complete the counter web.
    public class SkirmisherLineTests
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
            SkirmisherLineProgress.ResetForTests();
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

        // --- SkirmisherLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_PratirodhiDhanurdhara()
        {
            Assert.AreEqual(0, SkirmisherLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Pratirodhi Dhanurdhara", SkirmisherLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(SkirmisherLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < SkirmisherLineProgress.MaxTier; i++)
            {
                SkirmisherLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(SkirmisherLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsFalse(SkirmisherLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsTrue(SkirmisherLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            SkirmisherLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Pratirodhi Dhanurdhara", SkirmisherLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchSkirmisherTier ---

        [Test]
        public void RequestResearchSkirmisherTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchSkirmisherTier();

            Assert.IsTrue(barracks.IsResearchingSkirmisherTier);
            Assert.AreEqual(1000f - SkirmisherLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - SkirmisherLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchSkirmisherTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchSkirmisherTier();

            Assert.IsFalse(barracks.IsResearchingSkirmisherTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchSkirmisherTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchSkirmisherTier();

            Assert.IsFalse(barracks.IsResearchingSkirmisherTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchSkirmisherTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchSkirmisherTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchSkirmisherTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchSkirmisherTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            for (int i = 0; i < SkirmisherLineProgress.MaxTier; i++)
            {
                SkirmisherLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchSkirmisherTier();

            Assert.IsFalse(barracks.IsResearchingSkirmisherTier);
        }

        // --- SkirmisherFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void SkirmisherFactory_BakesCurrentTierNameAndDamageAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = SkirmisherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            SkirmisherLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = SkirmisherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Pratirodhi Dhanurdhara", upgraded.name);
            StringAssert.DoesNotContain("Maha Pratirodhi Dhanurdhara", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        [Test]
        public void SkirmisherFactory_ClassifiesAsSkirmisher_OptedIntoUpgradeScaling()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = SkirmisherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.Skirmisher, go.GetComponent<Attackable>().Class);
        }

        // --- CombatBonus: the counter web's last gap ---

        [Test]
        public void CombatBonus_SkirmisherHardCountersArcher()
        {
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.Skirmisher, UnitClass.Archer));
        }

        [Test]
        public void CombatBonus_InfantryCountersSkirmisher()
        {
            Assert.AreEqual(1.25f, CombatBonus.Multiplier(UnitClass.Infantry, UnitClass.Skirmisher));
        }
    }
}

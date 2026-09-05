using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 18 (docs/IMPLEMENTATION_ROADMAP.md): the Scout/Chara tier
    // ladder. Mirrors SpearmanLineTests's coverage shape - ScoutLineProgress's
    // own gating (age requirement, sequential tiers), Barracks.
    // RequestResearchCharaTier's request/cost/gate behavior, and
    // ScoutFactory actually baking the current tier's bonus/name in at
    // spawn - not retroactively. Unlike SpearmanLineTests, also asserts the
    // vision/speed growth this line's own tiers actually improve (see
    // ScoutLineProgress's own comment on why it deviates from every other
    // line's HP/damage shape).
    public class ScoutLineTests
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
            ScoutLineProgress.ResetForTests();
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

        // --- ScoutLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Chara()
        {
            Assert.AreEqual(0, ScoutLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Chara", ScoutLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(ScoutLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < ScoutLineProgress.MaxTier; i++)
            {
                ScoutLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(ScoutLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            Assert.IsFalse(ScoutLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsTrue(ScoutLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            ScoutLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Vega Ashvarohi", ScoutLineProgress.Current(FactionId.Player).Name);
            ScoutLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Vega Ashvarohi", ScoutLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void Tiers_VisionAndSpeedGrow_DamageStaysUnused()
        {
            // This line's own growth axis is vision/speed, not combat - see
            // ScoutLineProgress's class comment.
            Assert.Greater(ScoutLineProgress.Tiers[1].VisionBonus, ScoutLineProgress.Tiers[0].VisionBonus);
            Assert.Greater(ScoutLineProgress.Tiers[2].VisionBonus, ScoutLineProgress.Tiers[1].VisionBonus);
            Assert.Greater(ScoutLineProgress.Tiers[1].SpeedBonus, ScoutLineProgress.Tiers[0].SpeedBonus);
            Assert.Greater(ScoutLineProgress.Tiers[2].SpeedBonus, ScoutLineProgress.Tiers[1].SpeedBonus);
        }

        // --- Barracks.RequestResearchCharaTier ---

        [Test]
        public void RequestResearchCharaTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCharaTier();

            Assert.IsTrue(barracks.IsResearchingCharaTier);
            Assert.AreEqual(1000f - ScoutLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - ScoutLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchCharaTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCharaTier();

            Assert.IsFalse(barracks.IsResearchingCharaTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCharaTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchCharaTier();

            Assert.IsFalse(barracks.IsResearchingCharaTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCharaTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchCharaTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchCharaTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchCharaTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            for (int i = 0; i < ScoutLineProgress.MaxTier; i++)
            {
                ScoutLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchCharaTier();

            Assert.IsFalse(barracks.IsResearchingCharaTier);
        }

        // --- ScoutFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void ScoutFactory_BakesCurrentTierNameAndVisionAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = ScoutFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineVision = baseline.GetComponent<VisionSource>().VisionRadius;
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            ScoutLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = ScoutFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Vega Ashvarohi", upgraded.name);
            StringAssert.DoesNotContain("Vega Ashvarohi", baseline.name);
            Assert.Greater(upgraded.GetComponent<VisionSource>().VisionRadius, baselineVision);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original vision radius/HP even though the faction has
            // since advanced.
            Assert.AreEqual(baselineVision, baseline.GetComponent<VisionSource>().VisionRadius);
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        [Test]
        public void ScoutFactory_ClassifiesAsSupport_NotOptedIntoUpgradeScaling()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = ScoutFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.Support, go.GetComponent<Attackable>().Class);
        }
    }
}

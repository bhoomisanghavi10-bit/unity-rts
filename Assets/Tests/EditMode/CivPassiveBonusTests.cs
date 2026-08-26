using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Covers the per-civ passive bonus gap Roadmap Section 5 item 2 closes:
    // the 4 data-backed bonuses read via CivilizationProfile.
    // FindCategoryMultiplier, plus the hand-written structural mechanics
    // that aren't representable as passiveBonuses data at all. Prefers
    // FactionId.Enemy/Enemy2 (not Player) for anything that assigns a civ,
    // to avoid contaminating TrainingAndTradeTests.cs's Player-faction
    // assumptions - static registries persist across tests in the same run
    // (see that file's own comments on ResourceStockpile's identical
    // self-healing pattern). BuildingPlacer's helpers are the one exception
    // (hardcoded to FactionId.Player - it's an inherently player-only
    // tool), so those tests explicitly restore Player to Chola (the
    // registry's own default-fallback civ) before returning.
    public class CivPassiveBonusTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void FindCategoryMultiplier_MatchesAuditedValues()
        {
            Assert.AreEqual(0.85f, CivilizationProfile.FindCategoryMultiplier(CivilizationId.Rajput, StatType.ResourceCost, UnitCategory.Cavalry), 0.001f);
            Assert.AreEqual(1.15f, CivilizationProfile.FindCategoryMultiplier(CivilizationId.Maurya, StatType.MoveSpeed, UnitCategory.Support), 0.001f);
            Assert.AreEqual(1.2f, CivilizationProfile.FindCategoryMultiplier(CivilizationId.Maratha, StatType.MoveSpeed, UnitCategory.Cavalry), 0.001f);
            Assert.AreEqual(0.85f, CivilizationProfile.FindCategoryMultiplier(CivilizationId.Maratha, StatType.TrainTime, UnitCategory.Naval), 0.001f);
        }

        [Test]
        public void FindCategoryMultiplier_FallsBackToOneWhenNoMatchingBonus()
        {
            Assert.AreEqual(1f, CivilizationProfile.FindCategoryMultiplier(CivilizationId.Chola, StatType.MoveSpeed, UnitCategory.Cavalry), 0.001f);
            Assert.AreEqual(1f, CivilizationProfile.FindCategoryMultiplier(CivilizationId.Rajput, StatType.MoveSpeed, UnitCategory.Cavalry), 0.001f);
        }

        // ResourceStockpile's faction field defaults to Player and has no
        // public way to target another faction, so this test drives a real
        // Player-faction Barracks - CivilizationRegistry.Assign(Enemy2,
        // Rajput) above is deliberately NOT what's asserted against Player
        // here (Player's civ is whatever earlier tests left it, expected
        // Chola/no-bonus per this file's header). What's actually verified
        // is that FindCategoryMultiplier reports Rajput as a discount and
        // that a non-Rajput-assigned Barracks still charges the fixed
        // 70/50 Food/Gold costs unmodified - i.e. the bonus is real data
        // AND doesn't leak onto factions that were never assigned Rajput.
        [Test]
        public void RequestTrainCavalry_RajputBonusExistsAndDoesNotLeakToOtherFactions()
        {
            CivilizationRegistry.Assign(FactionId.Enemy2, CivilizationId.Rajput);

            float rajputMultiplier = CivilizationProfile.FindCategoryMultiplier(CivilizationId.Rajput, StatType.ResourceCost, UnitCategory.Cavalry);
            Assert.Less(rajputMultiplier, 1f, "Rajput's Cavalry gold-cost bonus should be a discount (<1x)");

            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Food, 1000f);
            stockpile.SetTotal(ResourceType.Gold, 1000f);

            GameObject barracksGo = CreateGameObject("Barracks");
            barracksGo.AddComponent<FactionMember>().Configure(FactionId.Player);
            Barracks barracks = barracksGo.AddComponent<Barracks>();

            barracks.RequestTrainCavalry();

            Assert.IsTrue(barracks.IsTraining);
            Assert.AreEqual(1000f - 70f, stockpile.GetTotal(ResourceType.Food), 0.01f);
            Assert.AreEqual(1000f - 50f, stockpile.GetTotal(ResourceType.Gold), 0.01f, "Player was never assigned Rajput, so it must pay the full Gold cost");
        }

        [Test]
        public void BuildingPlacer_WoodMultiplierFor_ZeroesHouseCostOnlyForMaurya()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            Assert.AreEqual(0f, BuildingPlacer.WoodMultiplierFor(BuildingPlacer.BuildingKind.House));
            Assert.AreEqual(1f, BuildingPlacer.WoodMultiplierFor(BuildingPlacer.BuildingKind.Barracks), "Only House is free, not every building");

            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
            Assert.AreEqual(1f, BuildingPlacer.WoodMultiplierFor(BuildingPlacer.BuildingKind.House), "Non-Maurya civs pay full House cost");
        }

        [Test]
        public void BuildingPlacer_StoneMultiplierFor_DiscountsFortificationsOnlyForVijayanagara()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Vijayanagara);
            Assert.AreEqual(0.8f, BuildingPlacer.StoneMultiplierFor(BuildingPlacer.BuildingKind.Wall), 0.001f);
            Assert.AreEqual(0.8f, BuildingPlacer.StoneMultiplierFor(BuildingPlacer.BuildingKind.Gate), 0.001f);
            Assert.AreEqual(0.8f, BuildingPlacer.StoneMultiplierFor(BuildingPlacer.BuildingKind.Tower), 0.001f);
            Assert.AreEqual(1f, BuildingPlacer.StoneMultiplierFor(BuildingPlacer.BuildingKind.Barracks), "Discount is fortifications-only, not civ-wide");

            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
            Assert.AreEqual(1f, BuildingPlacer.StoneMultiplierFor(BuildingPlacer.BuildingKind.Wall), "Non-Vijayanagara civs pay full Stone cost");
        }

        [Test]
        public void AgeProgress_Initialize_DefaultsToAncientAndAcceptsOverride()
        {
            AgeProgress.Initialize(FactionId.Enemy2);
            Assert.AreEqual(AgeId.Ancient, AgeProgress.CurrentAge(FactionId.Enemy2));

            AgeProgress.Initialize(FactionId.Enemy2, AgeId.Classical);
            Assert.AreEqual(AgeId.Classical, AgeProgress.CurrentAge(FactionId.Enemy2));
        }

        [Test]
        public void RajputDefianceHook_IsEligible_OnlyRajputCavalry()
        {
            CivilizationRegistry.Assign(FactionId.Enemy2, CivilizationId.Rajput);
            CivilizationRegistry.Assign(FactionId.Enemy, CivilizationId.Maurya);

            Assert.IsTrue(RajputDefianceHook.IsEligible(FactionId.Enemy2, UnitClass.Cavalry));
            Assert.IsFalse(RajputDefianceHook.IsEligible(FactionId.Enemy2, UnitClass.Infantry), "Rajput, but not Cavalry");
            Assert.IsFalse(RajputDefianceHook.IsEligible(FactionId.Enemy, UnitClass.Cavalry), "Cavalry, but not Rajput");
        }

        [Test]
        public void RajputDefianceHook_RollSucceeds_MatchesQuarterChance()
        {
            Assert.IsTrue(RajputDefianceHook.RollSucceeds(0f));
            Assert.IsTrue(RajputDefianceHook.RollSucceeds(0.2499f));
            Assert.IsFalse(RajputDefianceHook.RollSucceeds(0.25f));
            Assert.IsFalse(RajputDefianceHook.RollSucceeds(0.9f));
        }
    }
}

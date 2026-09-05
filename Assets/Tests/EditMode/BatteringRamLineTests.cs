using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    // Wave 4 item 20 (docs/IMPLEMENTATION_ROADMAP.md): the Battering Ram -
    // an anti-building specialist distinct from the existing Mangonel-like
    // Siege unit. Mirrors SkirmisherLineTests'/SiegeLineTests' coverage
    // shape for the tier ladder/Barracks research plumbing/factory-bakes-
    // in-at-spawn parts, plus new coverage for this item's own 3
    // distinguishing traits: MeleeAttacker.SetBuildingOnly (can't target
    // units at all, not just weakly), no splash (never calls
    // SetSplashRadius), and garrisonable (hosts a GarrisonPoint on itself).
    public class BatteringRamLineTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            BatteringRamLineProgress.ResetForTests();
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

        // --- BatteringRamLineProgress ---

        [Test]
        public void Tier_DefaultsToZero_Dwarabhanjaka()
        {
            Assert.AreEqual(0, BatteringRamLineProgress.Tier(FactionId.Player));
            Assert.AreEqual("Dwarabhanjaka", BatteringRamLineProgress.Current(FactionId.Player).Name);
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(BatteringRamLineProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < BatteringRamLineProgress.MaxTier; i++)
            {
                BatteringRamLineProgress.AdvanceTier(FactionId.Player);
            }
            Assert.IsFalse(BatteringRamLineProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsFalse(BatteringRamLineProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsTrue(BatteringRamLineProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void AdvanceTier_IsSequential()
        {
            BatteringRamLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Maha Dwarabhanjaka", BatteringRamLineProgress.Current(FactionId.Player).Name);

            BatteringRamLineProgress.AdvanceTier(FactionId.Player);
            Assert.AreEqual("Vajra Dwarabhanjaka", BatteringRamLineProgress.Current(FactionId.Player).Name);
        }

        // --- Barracks.RequestResearchBatteringRamTier ---

        [Test]
        public void RequestResearchBatteringRamTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchBatteringRamTier();

            Assert.IsTrue(barracks.IsResearchingBatteringRamTier);
            Assert.AreEqual(1000f - BatteringRamLineProgress.Tiers[1].GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - BatteringRamLineProgress.Tiers[1].WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchBatteringRamTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchBatteringRamTier();

            Assert.IsFalse(barracks.IsResearchingBatteringRamTier);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchBatteringRamTier_InsufficientResources_DoesNotDeductOrStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, gold: 10f, wood: 10f);

            barracks.RequestResearchBatteringRamTier();

            Assert.IsFalse(barracks.IsResearchingBatteringRamTier);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchBatteringRamTier_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Barracks barracks = CreateBarracks(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            barracks.RequestResearchBatteringRamTier();
            float afterFirst = stockpile.GetTotal(ResourceType.Gold);
            barracks.RequestResearchBatteringRamTier();

            Assert.AreEqual(afterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchBatteringRamTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < BatteringRamLineProgress.MaxTier; i++)
            {
                BatteringRamLineProgress.AdvanceTier(FactionId.Player);
            }
            Barracks barracks = CreateBarracks(FactionId.Player);
            CreateStockpile(FactionId.Player);

            barracks.RequestResearchBatteringRamTier();

            Assert.IsFalse(barracks.IsResearchingBatteringRamTier);
        }

        // --- BatteringRamFactory: bakes current tier in at spawn, not retroactive ---

        [Test]
        public void BatteringRamFactory_BakesCurrentTierNameAndDamageAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject baseline = BatteringRamFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            BatteringRamLineProgress.AdvanceTier(FactionId.Player);
            GameObject upgraded = BatteringRamFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Dwarabhanjaka", upgraded.name);
            StringAssert.DoesNotContain("Maha Dwarabhanjaka", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        [Test]
        public void BatteringRamFactory_ClassifiesAsBatteringRam_OptedIntoUpgradeScaling()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = BatteringRamFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            Assert.AreEqual(UnitClass.BatteringRam, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void BatteringRamFactory_AddsGarrisonPoint_WithCapacityFour()
        {
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = BatteringRamFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);

            GarrisonPoint garrisonPoint = go.GetComponent<GarrisonPoint>();
            Assert.IsNotNull(garrisonPoint, "A Battering Ram must host its own GarrisonPoint (garrisonable, per the roadmap spec).");
            Assert.AreEqual(4, garrisonPoint.Capacity);
        }

        [Test]
        public void BatteringRamFactory_NeverConfiguresSplash()
        {
            // No direct getter for MeleeAttacker's private splashRadius -
            // instead prove it behaviorally: a hit on the primary target
            // must not also damage a nearby hostile Building, unlike
            // Siege's own splash (see SiegeSplashTests).
            LogAssert.ignoreFailingMessages = true;
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            GameObject go = BatteringRamFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(go);
            MeleeAttacker attacker = go.GetComponent<MeleeAttacker>();

            GameObject buildingGo = CreateGameObject("PrimaryTargetBuilding");
            buildingGo.AddComponent<FactionMember>().Configure(FactionId.Enemy);
            Building building = buildingGo.AddComponent<Building>();
            Building.All.Add(building);
            Attackable primaryTarget = buildingGo.AddComponent<Attackable>();
            primaryTarget.Configure(1000f);
            primaryTarget.ConfigureClass(UnitClass.Building);

            GameObject nearbyBuildingGo = CreateGameObject("NearbyHostileBuilding");
            nearbyBuildingGo.transform.position = new Vector3(0.5f, 0f, 0f);
            nearbyBuildingGo.AddComponent<FactionMember>().Configure(FactionId.Enemy);
            Building nearbyBuilding = nearbyBuildingGo.AddComponent<Building>();
            Building.All.Add(nearbyBuilding);
            Attackable nearby = nearbyBuildingGo.AddComponent<Attackable>();
            nearby.Configure(1000f);
            nearby.ConfigureClass(UnitClass.Building);

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            attacker.AttackMove(primaryTarget);
            LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
            attacker.Tick(1f);

            Assert.Less(primaryTarget.Health, 1000f, "Primary target should still take damage.");
            Assert.AreEqual(1000f, nearby.Health, "With no splash configured, only the primary target should be hit.");
        }

        // --- MeleeAttacker.SetBuildingOnly: the "anti-building ONLY" trait ---

        [Test]
        public void SetBuildingOnly_RefusesAttackMove_AgainstUnitTarget()
        {
            GameObject go = CreateGameObject("BatteringRam");
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.BatteringRam);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetRange(1.5f);
            attacker.SetUnitClass(UnitClass.BatteringRam);
            attacker.SetBuildingOnly(true);

            GameObject targetGo = CreateGameObject("HostileUnit");
            targetGo.AddComponent<FactionMember>().Configure(FactionId.Enemy);
            Unit unit = targetGo.AddComponent<Unit>();
            Unit.All.Add(unit);
            Attackable target = targetGo.AddComponent<Attackable>();
            target.Configure(1000f);
            target.ConfigureClass(UnitClass.Infantry);

            attacker.AttackMove(target);

            Assert.IsFalse(attacker.IsAttacking, "A buildingOnly attacker must refuse an AttackMove order against a non-Building target.");
        }

        [Test]
        public void SetBuildingOnly_AcceptsAttackMove_AgainstBuildingTarget()
        {
            GameObject go = CreateGameObject("BatteringRam");
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.BatteringRam);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetRange(1.5f);
            attacker.SetUnitClass(UnitClass.BatteringRam);
            attacker.SetBuildingOnly(true);

            GameObject buildingGo = CreateGameObject("HostileBuilding");
            buildingGo.AddComponent<FactionMember>().Configure(FactionId.Enemy);
            Building building = buildingGo.AddComponent<Building>();
            Building.All.Add(building);
            Attackable target = buildingGo.AddComponent<Attackable>();
            target.Configure(1000f);
            target.ConfigureClass(UnitClass.Building);

            LogAssert.ignoreFailingMessages = true;
            attacker.AttackMove(target);

            Assert.IsTrue(attacker.IsAttacking, "A buildingOnly attacker must still accept an AttackMove order against a Building target.");
        }

        // --- CombatBonus: BatteringRam's steep anti-building specialty ---

        [Test]
        public void CombatBonus_BatteringRamStrongerThanSiegeAgainstBuildings()
        {
            Assert.AreEqual(4f, CombatBonus.Multiplier(UnitClass.BatteringRam, UnitClass.Building));
            Assert.Greater(
                CombatBonus.Multiplier(UnitClass.BatteringRam, UnitClass.Building),
                CombatBonus.Multiplier(UnitClass.Siege, UnitClass.Building));
        }
    }
}

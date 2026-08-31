using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Worker mechanics audit (2026-08-29) item: Repair was completely
    // missing. Covers Repairable's target-side tick logic directly via its
    // internal Tick(deltaTime) (mirrors ConstructionSite's own
    // EnsureInitialized test convention - Update() itself depends on
    // Time.deltaTime, which EditMode tests don't naturally advance).
    public class RepairableTests
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
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        // ResourceStockpile's faction field is Inspector-only (defaults to
        // Player) - every test here uses a Player-owned repair target, same
        // convention as TrainingAndTradeTests' CreateStockpile.
        private ResourceStockpile CreateStockpile(float wood = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        private Repairable CreateRepairable(UnitClass unitClass, float maxHealth, float currentHealth, bool underConstruction = false)
        {
            GameObject go = CreateGameObject("Target");
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(maxHealth);
            attackable.ConfigureClass(unitClass);
            if (currentHealth < maxHealth)
            {
                attackable.RestoreHealth(currentHealth);
            }

            if (underConstruction)
            {
                var site = go.AddComponent<ConstructionSite>();
                site.EnsureInitialized();
                // Deliberately left incomplete (IsComplete false) - a
                // foundation mid-build shouldn't be repairable at all,
                // that's Builder's job.
            }

            return go.AddComponent<Repairable>();
        }

        [Test]
        public void Tick_WithNoActiveRepairers_DoesNothing()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 100f);

            repairable.Tick(1f);

            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void Tick_HealsAndSpendsWoodAtBuildingRate()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 100f);
            var attackable = repairable.GetComponent<Attackable>();

            repairable.BeginRepair();
            repairable.Tick(1f); // 1 repairer, 1s -> 12 HP at SpeedMultiplier(1)=1x

            Assert.AreEqual(112f, attackable.Health, 0.01f);
            Assert.AreEqual(1000f - 12f * repairable.WoodCostPerHp(), stockpile.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Tick_ChargesDifferentRatesPerUnitClass()
        {
            CreateStockpile();
            Repairable building = CreateRepairable(UnitClass.Building, 300f, 100f);
            Repairable naval = CreateRepairable(UnitClass.Naval, 300f, 100f);
            Repairable siege = CreateRepairable(UnitClass.Siege, 300f, 100f);

            Assert.Less(building.WoodCostPerHp(), naval.WoodCostPerHp(),
                "Buildings should be the cheapest class to repair per HP");
            Assert.Less(naval.WoodCostPerHp(), siege.WoodCostPerHp(),
                "Siege should be the most expensive class to repair per HP");
        }

        [Test]
        public void Tick_WithMultipleRepairers_AppliesConstructionSiteSpeedMultiplier()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 100f);
            var attackable = repairable.GetComponent<Attackable>();

            repairable.BeginRepair();
            repairable.BeginRepair();
            repairable.BeginRepair(); // 3 active repairers -> SpeedMultiplier(3) = 1.9x
            repairable.Tick(1f);

            float expectedHeal = 12f * ConstructionSite.SpeedMultiplier(3);
            Assert.AreEqual(100f + expectedHeal, attackable.Health, 0.01f);
            Assert.AreEqual(1000f - expectedHeal * repairable.WoodCostPerHp(), stockpile.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Tick_NeverHealsPastMaxHealth_AndOnlyChargesForActualHealGiven()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 298f);
            var attackable = repairable.GetComponent<Attackable>();

            repairable.BeginRepair();
            repairable.Tick(1f); // would restore 12 HP but only 2 is missing

            Assert.AreEqual(300f, attackable.Health, 0.01f);
            Assert.AreEqual(1000f - 2f * repairable.WoodCostPerHp(), stockpile.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Tick_AtFullHealth_IsNotRepairable_AndDoesNothing()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 300f);
            var attackable = repairable.GetComponent<Attackable>();

            Assert.IsFalse(repairable.IsRepairable);

            repairable.BeginRepair();
            repairable.Tick(1f);

            Assert.AreEqual(300f, attackable.Health, 0.01f);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void Tick_WhenStockpileCannotAffordTheTick_StallsWithoutPartialHealOrSpend()
        {
            ResourceStockpile stockpile = CreateStockpile(wood: 0f);
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 100f);
            var attackable = repairable.GetComponent<Attackable>();

            repairable.BeginRepair();
            repairable.Tick(1f);

            Assert.AreEqual(100f, attackable.Health, 0.01f, "No Wood available - should not partially heal");
            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void IsRepairable_FalseWhileUnderConstruction_EvenIfDamaged()
        {
            Repairable repairable = CreateRepairable(UnitClass.Building, maxHealth: 300f, currentHealth: 100f, underConstruction: true);

            Assert.IsFalse(repairable.IsRepairable,
                "A foundation mid-build isn't repairable - that's Builder/ConstructionSite's job, not Repair's");
        }
    }
}

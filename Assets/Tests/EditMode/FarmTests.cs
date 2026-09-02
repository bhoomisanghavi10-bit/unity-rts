using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Item 5 (Renewable Resource, docs/PARTIAL_ELEMENTS_FIX_PLAN.md): Farm
    // used to produce Food forever with no cap - a genuinely different
    // economy model than AoE's actual finite/depleting/reseedable Farm.
    // Covers the retrofit directly via Farm's internal Tick(deltaTime),
    // same convention RepairableTests.cs already establishes for this
    // project's "restore-a-resource-with-Wood-over-time" mechanics.
    public class FarmTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");

        [SetUp]
        public void SetUp()
        {
            // The one integration test below spawns a real FarmWorker
            // (UnitMover) and calls StaffAt -> MoveTo -> NavMeshAgent.
            // SetDestination, which logs an Editor error with no baked
            // NavMesh in an EditMode test scene, same situation
            // SiegeSplashTests/GathererCombatResponseTests document.
            LogAssert.ignoreFailingMessages = true;
        }

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
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        // ResourceStockpile's faction field is Inspector-only (defaults to
        // Player) - matches RepairableTests' own CreateStockpile convention.
        private ResourceStockpile CreateStockpile(float wood = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        private Farm CreateFarm()
        {
            GameObject go = CreateGameObject("Farm");
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            return go.AddComponent<Farm>();
        }

        [Test]
        public void RemainingFood_StartsAtMaxFood()
        {
            Farm farm = CreateFarm();

            Assert.AreEqual(farm.MaxFood, farm.RemainingFood, 0.01f);
            Assert.IsFalse(farm.IsDepleted);
        }

        [Test]
        public void Tick_Harvesting_ConsumesRemainingFood_OneToOneWithStockpileCredit()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Farm farm = CreateFarm();
            float before = farm.RemainingFood;

            farm.BeginWorking();
            farm.Tick(1f); // 1 worker, 1s -> 0.6 Food (default foodPerSecondPerWorker)

            Assert.AreEqual(before - 0.6f, farm.RemainingFood, 0.01f);
            Assert.AreEqual(0.6f, stockpile.GetTotal(ResourceType.Food), 0.01f);
        }

        [Test]
        public void Tick_Harvesting_StopsExactlyAtZero_NeverNegative()
        {
            CreateStockpile();
            Farm farm = CreateFarm();
            farm.BeginWorking();

            // Far more than enough real time to fully drain a 175-cap Farm
            // at 0.6/s in one call - proves the per-tick clamp, not just
            // that it eventually stops.
            farm.Tick(10000f);

            Assert.AreEqual(0f, farm.RemainingFood, 0.001f);
            Assert.IsTrue(farm.IsDepleted);

            float foodAfterFirstDrain = ResourceStockpile.For(FactionId.Player).GetTotal(ResourceType.Food);
            farm.Tick(1f);
            Assert.AreEqual(foodAfterFirstDrain, ResourceStockpile.For(FactionId.Player).GetTotal(ResourceType.Food), 0.001f,
                "A depleted Farm must not credit any further Food even while still staffed.");
        }

        [Test]
        public void Tick_Reseeding_RestoresFoodAndChargesWoodAtTheRealRate()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Farm farm = CreateFarm();
            farm.BeginWorking();
            farm.Tick(10000f); // fully deplete first
            Assert.IsTrue(farm.IsDepleted);

            farm.StopWorking();
            farm.BeginReseed();
            farm.Tick(1f); // 1 reseeder, 1s -> 15 Food at SpeedMultiplier(1)=1x

            Assert.AreEqual(15f, farm.RemainingFood, 0.01f);
            Assert.AreEqual(1000f - 15f * farm.WoodCostPerFood, stockpile.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Tick_Reseeding_StallsSilently_WhenWoodInsufficient()
        {
            ResourceStockpile stockpile = CreateStockpile(wood: 0f);
            Farm farm = CreateFarm();
            farm.BeginWorking();
            farm.Tick(10000f);

            farm.StopWorking();
            farm.BeginReseed();
            farm.Tick(1f);

            Assert.AreEqual(0f, farm.RemainingFood, 0.01f, "No Wood available - reseeding must not proceed at all.");
            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Tick_Reseeding_NeverExceedsMaxFood()
        {
            CreateStockpile();
            Farm farm = CreateFarm();
            farm.BeginWorking();
            farm.Tick(1f); // drain a small, known amount (0.6 Food)
            farm.StopWorking();

            farm.BeginReseed();
            farm.Tick(10000f); // far more than enough to overshoot MaxFood if unclamped

            Assert.AreEqual(farm.MaxFood, farm.RemainingFood, 0.01f);
        }

        [Test]
        public void Tick_TwoReseeders_RestoreAtConstructionSiteSpeedMultiplier_NotNaiveDouble()
        {
            CreateStockpile();
            Farm farm = CreateFarm();
            farm.BeginWorking();
            farm.Tick(10000f);
            farm.StopWorking();

            farm.BeginReseed();
            farm.BeginReseed();
            farm.Tick(1f); // 2 reseeders, 1s -> 15 * SpeedMultiplier(2) Food

            float expected = 15f * ConstructionSite.SpeedMultiplier(2);
            Assert.AreEqual(expected, farm.RemainingFood, 0.01f);
            Assert.Less(expected, 30f, "2 reseeders must restore less than a naive 2x - diminishing returns, matching Repairable's own formula reuse.");
        }

        [Test]
        public void FarmWorker_AutonomouslySwitchesBetweenHarvestingAndReseeding()
        {
            CreateStockpile();
            Farm farm = CreateFarm();

            GameObject workerGo = CreateGameObject("Worker");
            workerGo.AddComponent<KingdomsOfBharat.Units.UnitMover>();
            var farmWorker = workerGo.AddComponent<FarmWorker>();
            workerGo.transform.position = farm.transform.position; // already in range

            // Unity doesn't guarantee Awake has run synchronously right
            // after AddComponent in EditMode (same "AddComponent ordering
            // hazard" documented across this project's own tests/comments,
            // e.g. Repairable/ConstructionSite) - FarmWorker.Awake() is
            // what resolves its own UnitMover reference, so force it here
            // before calling StaffAt (which uses that reference).
            var awakeMethod = typeof(FarmWorker).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(farmWorker, null);

            var updateMethod = typeof(FarmWorker).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            farmWorker.StaffAt(farm);
            updateMethod.Invoke(farmWorker, null);
            Assert.IsTrue(farmWorker.IsFarming, "Should start harvesting a full Farm.");
            Assert.IsFalse(farmWorker.IsReseeding);

            farm.Tick(10000f); // drain it out from under the worker
            updateMethod.Invoke(farmWorker, null);
            Assert.IsFalse(farmWorker.IsFarming, "Must stop harvesting once the Farm is depleted.");
            Assert.IsTrue(farmWorker.IsReseeding, "Must autonomously switch to reseeding a depleted Farm - no new order issued.");

            farm.Tick(10000f); // fully reseed it (plenty of Wood available)
            updateMethod.Invoke(farmWorker, null);
            Assert.IsTrue(farmWorker.IsFarming, "Must resume harvesting once the Farm is full again.");
            Assert.IsFalse(farmWorker.IsReseeding);
        }
    }
}

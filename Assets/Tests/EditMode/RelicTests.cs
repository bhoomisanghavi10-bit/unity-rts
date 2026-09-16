using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 35 (Relics + Monastery collection, economic only): covers
    // Relic's held-state flag, RelicCarrier's nearest-owned-Monastery
    // routing rule, and Monastery's relic-count bookkeeping/gold-trickle
    // formula directly - same "internal static, no scene dependency"
    // convention TraderTests/KarmashalaTests already establish. Full pickup/
    // walk/deposit state machine and RelicCarrier.PickUp's own MovingToRelic
    // -> SeekingMonastery transition are Play-mode/live-verification
    // concerns (needs a baked NavMesh), same as Trader's own scope.
    public class RelicTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    if (go.TryGetComponent(out Building building))
                    {
                        // Building.All registration/deregistration normally
                        // happens via OnEnable/OnDisable, which isn't
                        // guaranteed synchronous in EditMode - removed
                        // directly here, same convention as
                        // TraderTests/BuildingFootprintTests.
                        Building.All.Remove(building);
                    }
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

        private Monastery NewMonastery(Vector3 position, FactionId faction)
        {
            GameObject go = CreateGameObject("TestMonastery");
            go.transform.position = position;
            Monastery monastery = go.AddComponent<Monastery>();
            go.AddComponent<FactionMember>().Configure(faction);
            if (!Building.All.Contains(monastery))
            {
                Building.All.Add(monastery);
            }
            return monastery;
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float gold = 0f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.Configure(faction);
            stockpile.SetTotal(ResourceType.Gold, gold);
            return stockpile;
        }

        [Test]
        public void Relic_StartsUnheld()
        {
            Relic relic = CreateGameObject("Relic").AddComponent<Relic>();
            Assert.IsFalse(relic.IsHeld);
        }

        [Test]
        public void Relic_SetHeld_TogglesBothWays()
        {
            Relic relic = CreateGameObject("Relic").AddComponent<Relic>();

            relic.SetHeld(true);
            Assert.IsTrue(relic.IsHeld);

            relic.SetHeld(false);
            Assert.IsFalse(relic.IsHeld);
        }

        [Test]
        public void FindNearestOwnedMonastery_PicksNearestSameFaction()
        {
            Monastery near = NewMonastery(new Vector3(2f, 0f, 0f), FactionId.Player);
            Monastery far = NewMonastery(new Vector3(20f, 0f, 0f), FactionId.Player);

            Monastery result = RelicCarrier.FindNearestOwnedMonastery(Vector3.zero, FactionId.Player);

            Assert.AreEqual(near, result);
            Assert.AreNotEqual(far, result);
        }

        [Test]
        public void FindNearestOwnedMonastery_ExcludesOtherFactions()
        {
            NewMonastery(new Vector3(1f, 0f, 0f), FactionId.Enemy);

            Monastery result = RelicCarrier.FindNearestOwnedMonastery(Vector3.zero, FactionId.Player);

            Assert.IsNull(result);
        }

        [Test]
        public void FindNearestOwnedMonastery_ReturnsNull_WhenNoneExist()
        {
            Monastery result = RelicCarrier.FindNearestOwnedMonastery(Vector3.zero, FactionId.Player);

            Assert.IsNull(result);
        }

        [Test]
        public void AddRelic_IncrementsRelicCount()
        {
            Monastery monastery = NewMonastery(Vector3.zero, FactionId.Player);

            monastery.AddRelic();
            monastery.AddRelic();

            Assert.AreEqual(2, monastery.RelicCount);
        }

        [TestCase(0.5f, 0, 1f, 0f)]
        [TestCase(0.5f, 1, 1f, 0.5f)]
        [TestCase(0.5f, 3, 2f, 3f)]
        public void RelicGoldPerTick_ScalesWithCountAndTime(float goldPerSecond, int relicCount, float deltaTime, float expected)
        {
            Assert.AreEqual(expected, Monastery.RelicGoldPerTick(goldPerSecond, relicCount, deltaTime), 0.001f);
        }

        [Test]
        public void Tick_WithHeldRelics_AddsGoldToOwnStockpile()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Monastery monastery = NewMonastery(Vector3.zero, FactionId.Player);
            monastery.AddRelic();
            monastery.AddRelic();

            monastery.Tick(1f);

            float expected = Monastery.RelicGoldPerTick(0.5f, 2, 1f);
            Assert.AreEqual(expected, stockpile.GetTotal(ResourceType.Gold), 0.001f);
        }

        [Test]
        public void Tick_WithNoRelics_AddsNoGold()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Monastery monastery = NewMonastery(Vector3.zero, FactionId.Player);

            monastery.Tick(1f);

            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Gold));
        }
    }
}

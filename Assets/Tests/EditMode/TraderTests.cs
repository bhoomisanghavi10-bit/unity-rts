using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 26 (Trader/BoatTrader - Vanik + Trade Ship trade routes):
    // exercises the pure gold formula and the nearest-owned-building routing
    // rule directly, same "internal static, no scene dependency" convention
    // as GathererDropOffTests.cs's own AcceptsDropOff coverage.
    public class TraderTests
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
                        // BuildingFootprintTests/AgeUpRequirementTests.
                        Building.All.Remove(building);
                    }
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        // Registers directly in Building.All rather than relying on
        // OnEnable's own timing - same convention as
        // BuildingFootprintTests/AgeUpRequirementTests.
        private Market NewMarket(Vector3 position, FactionId faction)
        {
            var go = new GameObject("TestMarket");
            go.transform.position = position;
            Market market = go.AddComponent<Market>();
            go.AddComponent<FactionMember>().Configure(faction);
            if (!Building.All.Contains(market))
            {
                Building.All.Add(market);
            }
            _spawned.Add(go);
            return market;
        }

        private Dock NewDock(Vector3 position, FactionId faction)
        {
            var go = new GameObject("TestDock");
            go.transform.position = position;
            Dock dock = go.AddComponent<Dock>();
            go.AddComponent<FactionMember>().Configure(faction);
            if (!Building.All.Contains(dock))
            {
                Building.All.Add(dock);
            }
            _spawned.Add(go);
            return dock;
        }

        [TestCase(10f, 0.2f, 8f, 60f, 8f)]
        [TestCase(100f, 0.2f, 8f, 60f, 20f)]
        [TestCase(500f, 0.2f, 8f, 60f, 60f)]
        public void Trader_ComputeTradeGold_ClampsAndScales(float distance, float rate, float min, float max, float expected)
        {
            Assert.AreEqual(expected, Trader.ComputeTradeGold(distance, rate, min, max), 0.001f);
        }

        [TestCase(10f, 0.15f, 10f, 80f, 10f)]
        [TestCase(200f, 0.15f, 10f, 80f, 30f)]
        [TestCase(1000f, 0.15f, 10f, 80f, 80f)]
        public void BoatTrader_ComputeTradeGold_ClampsAndScales(float distance, float rate, float min, float max, float expected)
        {
            Assert.AreEqual(expected, BoatTrader.ComputeTradeGold(distance, rate, min, max), 0.001f);
        }

        [Test]
        public void FindNearestOwnedMarket_PicksNearestSameFaction_ExcludesDestination()
        {
            Market near = NewMarket(new Vector3(2f, 0f, 0f), FactionId.Player);
            Market far = NewMarket(new Vector3(20f, 0f, 0f), FactionId.Player);
            Market destination = NewMarket(new Vector3(1f, 0f, 0f), FactionId.Player);

            Market result = Trader.FindNearestOwnedMarket(Vector3.zero, FactionId.Player, destination);

            Assert.AreEqual(near, result);
            Assert.AreNotEqual(far, result);
        }

        [Test]
        public void FindNearestOwnedMarket_ExcludesOtherFactions()
        {
            Market enemyMarket = NewMarket(new Vector3(1f, 0f, 0f), FactionId.Enemy);
            Market destination = NewMarket(new Vector3(5f, 0f, 0f), FactionId.Player);

            Market result = Trader.FindNearestOwnedMarket(Vector3.zero, FactionId.Player, destination);

            Assert.IsNull(result);
            Assert.AreNotEqual(enemyMarket, result);
        }

        [Test]
        public void FindNearestOwnedMarket_ReturnsNull_WhenNoneExist()
        {
            Market result = Trader.FindNearestOwnedMarket(Vector3.zero, FactionId.Player, null);

            Assert.IsNull(result);
        }

        [Test]
        public void FindNearestOwnedDock_PicksNearestSameFaction_ExcludesDestination()
        {
            Dock near = NewDock(new Vector3(3f, 0f, 0f), FactionId.Player);
            Dock far = NewDock(new Vector3(30f, 0f, 0f), FactionId.Player);
            Dock destination = NewDock(new Vector3(1f, 0f, 0f), FactionId.Player);

            Dock result = BoatTrader.FindNearestOwnedDock(Vector3.zero, FactionId.Player, destination);

            Assert.AreEqual(near, result);
            Assert.AreNotEqual(far, result);
        }

        [Test]
        public void FindNearestOwnedDock_ExcludesOtherFactions()
        {
            Dock enemyDock = NewDock(new Vector3(1f, 0f, 0f), FactionId.Enemy);
            Dock destination = NewDock(new Vector3(5f, 0f, 0f), FactionId.Player);

            Dock result = BoatTrader.FindNearestOwnedDock(Vector3.zero, FactionId.Player, destination);

            Assert.IsNull(result);
            Assert.AreNotEqual(enemyDock, result);
        }
    }
}

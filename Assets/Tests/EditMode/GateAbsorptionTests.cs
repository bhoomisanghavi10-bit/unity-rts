using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wall mechanics audit (2026-09-29): AoE2 lets a Gate placement
    // absorb (delete + refund) pre-existing Wall segments its own
    // footprint overlaps, rather than being blocked by them - see
    // BuildingPlacer.IsClearForGate/FindAbsorbableWalls/AbsorbWalls.
    public class GateAbsorptionTests
    {
        private const FactionId Owner = FactionId.Enemy2;
        private const FactionId Other = FactionId.Player;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private GameObject _stockpileGo;

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();

            // AbsorbWalls (production code, exercised directly by some
            // tests here) destroys a Wall's GameObject itself, via
            // DestroyImmediate, without going through this class's own
            // _spawned-tracked removal above - and Building.OnDisable
            // isn't guaranteed to fire synchronously in EditMode (this
            // project's own documented gotcha), so a destroyed Wall can be
            // left as a stale (Unity fake-null) entry in the shared
            // static Building.All, breaking any OTHER test that later
            // iterates it. Purge defensively so nothing leaks forward.
            Building.All.RemoveAll(b => b == null);

            if (_stockpileGo != null)
            {
                Object.DestroyImmediate(_stockpileGo);
                _stockpileGo = null;
            }
        }

        private ResourceStockpile NewStockpile(FactionId faction)
        {
            _stockpileGo = new GameObject("Stockpile");
            var stockpile = _stockpileGo.AddComponent<ResourceStockpile>();
            stockpile.Configure(faction);
            return stockpile;
        }

        // Registers directly into Building.All (same convention
        // BuildingFootprintTests already established) rather than relying
        // on OnEnable's unreliable EditMode timing.
        private Wall NewWall(Vector3 position, FactionId faction, Vector2 footprint, float woodCost = 0f)
        {
            var go = new GameObject("TestWall");
            go.transform.position = position;
            Wall wall = go.AddComponent<Wall>();
            go.AddComponent<FactionMember>().Configure(faction);
            BuildingFootprint.Attach(go, footprint, carveObstacle: false);
            if (woodCost > 0f)
            {
                go.AddComponent<BuildingCost>().Record(ResourceType.Wood, woodCost);
            }
            if (!Building.All.Contains(wall))
            {
                Building.All.Add(wall);
            }
            _spawned.Add(go);
            return wall;
        }

        [Test]
        public void FindAbsorbableWalls_SameFactionWallUnderGateFootprint_IsFound()
        {
            NewWall(new Vector3(0f, 0f, 0f), Owner, new Vector2(2.4f, 2.4f));

            List<Wall> found = BuildingPlacer.FindAbsorbableWalls(Vector3.zero, new Vector3(7.2f, 6f, 2.6f), Owner);

            Assert.AreEqual(1, found.Count);
        }

        [Test]
        public void FindAbsorbableWalls_EnemyFactionWall_IsNotFound()
        {
            NewWall(new Vector3(0f, 0f, 0f), Other, new Vector2(2.4f, 2.4f));

            List<Wall> found = BuildingPlacer.FindAbsorbableWalls(Vector3.zero, new Vector3(7.2f, 6f, 2.6f), Owner);

            Assert.AreEqual(0, found.Count);
        }

        [Test]
        public void FindAbsorbableWalls_WallOutsideGateFootprint_IsNotFound()
        {
            NewWall(new Vector3(20f, 0f, 20f), Owner, new Vector2(2.4f, 2.4f));

            List<Wall> found = BuildingPlacer.FindAbsorbableWalls(Vector3.zero, new Vector3(7.2f, 6f, 2.6f), Owner);

            Assert.AreEqual(0, found.Count);
        }

        [Test]
        public void AbsorbWalls_DestroysTheWallAndRefundsItsFullRecordedCost()
        {
            ResourceStockpile stockpile = NewStockpile(Owner);
            Wall wall = NewWall(Vector3.zero, Owner, new Vector2(2.4f, 2.4f), woodCost: 15f);

            BuildingPlacer.AbsorbWalls(new List<Wall> { wall }, Owner);

            Assert.IsTrue(wall == null, "Absorbed walls should be destroyed");
            Assert.AreEqual(15f, stockpile.GetTotal(ResourceType.Wood), 0.001f);
        }

        [Test]
        public void IsClearForGate_SameFactionWallNearby_DoesNotBlock()
        {
            NewWall(new Vector3(1f, 0f, 0f), Owner, new Vector2(2.4f, 2.4f));

            bool clear = BuildingPlacer.IsClearForGate(Vector3.zero, clearance: 4f, faction: Owner);

            Assert.IsTrue(clear);
        }

        [Test]
        public void IsClearForGate_EnemyFactionWallNearby_StillBlocks()
        {
            NewWall(new Vector3(1f, 0f, 0f), Other, new Vector2(2.4f, 2.4f));

            bool clear = BuildingPlacer.IsClearForGate(Vector3.zero, clearance: 4f, faction: Owner);

            Assert.IsFalse(clear);
        }
    }
}

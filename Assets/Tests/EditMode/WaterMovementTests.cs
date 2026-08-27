using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Roadmap Section 1 "WaterMover has no obstacle avoidance": the real,
    // reachable-today bug was that WaterMover.MoveTo stored any destination
    // with zero bounds checking, so a move order/rally point past the
    // shoreline sailed the boat straight onto land. Fix is
    // WaterProximity.ClampToWater, applied in MoveTo - since the water
    // region is a single convex rectangle, clamping the endpoint is enough
    // to guarantee the whole straight-line path stays in water. Selects
    // MapId.Coastal (the only map with water) for setup and restores the
    // documented default (RiverValley) in TearDown, since MapRegistry is a
    // static registry shared across the whole EditMode test run.
    public class WaterMovementTests
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
            MapRegistry.Select(MapId.RiverValley);
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void ClampToWater_PointAlreadyInsideWater_IsUnchanged()
        {
            MapRegistry.Select(MapId.Coastal);
            MapDefinitionData map = MapRegistry.Current;
            Vector3 inside = map.WaterCenter;

            Vector3 result = WaterProximity.ClampToWater(inside);

            Assert.AreEqual(inside.x, result.x, 0.001f);
            Assert.AreEqual(inside.z, result.z, 0.001f);
        }

        [Test]
        public void ClampToWater_PointBeyondEachEdge_ClampsToThatEdge()
        {
            MapRegistry.Select(MapId.Coastal);
            MapDefinitionData map = MapRegistry.Current;

            Vector3 pastEast = new Vector3(map.WaterCenter.x + map.WaterHalfExtents.x + 50f, 0f, map.WaterCenter.z);
            Vector3 clampedEast = WaterProximity.ClampToWater(pastEast);
            Assert.AreEqual(map.WaterCenter.x + map.WaterHalfExtents.x, clampedEast.x, 0.001f);

            Vector3 pastWest = new Vector3(map.WaterCenter.x - map.WaterHalfExtents.x - 50f, 0f, map.WaterCenter.z);
            Vector3 clampedWest = WaterProximity.ClampToWater(pastWest);
            Assert.AreEqual(map.WaterCenter.x - map.WaterHalfExtents.x, clampedWest.x, 0.001f);

            Vector3 pastNorth = new Vector3(map.WaterCenter.x, 0f, map.WaterCenter.z + map.WaterHalfExtents.z + 50f);
            Vector3 clampedNorth = WaterProximity.ClampToWater(pastNorth);
            Assert.AreEqual(map.WaterCenter.z + map.WaterHalfExtents.z, clampedNorth.z, 0.001f);

            Vector3 pastSouth = new Vector3(map.WaterCenter.x, 0f, map.WaterCenter.z - map.WaterHalfExtents.z - 50f);
            Vector3 clampedSouth = WaterProximity.ClampToWater(pastSouth);
            Assert.AreEqual(map.WaterCenter.z - map.WaterHalfExtents.z, clampedSouth.z, 0.001f);
        }

        [Test]
        public void ClampToWater_PointBeyondBothAxes_ClampsBoth()
        {
            MapRegistry.Select(MapId.Coastal);
            MapDefinitionData map = MapRegistry.Current;
            Vector3 farCorner = new Vector3(
                map.WaterCenter.x + map.WaterHalfExtents.x + 100f,
                0f,
                map.WaterCenter.z + map.WaterHalfExtents.z + 100f);

            Vector3 result = WaterProximity.ClampToWater(farCorner);

            Assert.IsTrue(WaterProximity.IsInsideWater(result));
        }

        [Test]
        public void ClampToWater_NoWaterMap_ReturnsPointUnchanged()
        {
            MapRegistry.Select(MapId.RiverValley);
            Vector3 point = new Vector3(500f, 0f, -500f);

            Vector3 result = WaterProximity.ClampToWater(point);

            Assert.AreEqual(point, result);
        }

        [Test]
        public void WaterMover_MoveTo_DestinationFarInland_IsClampedInsideWater()
        {
            MapRegistry.Select(MapId.Coastal);
            GameObject go = CreateGameObject("TestBoat");
            WaterMover mover = go.AddComponent<WaterMover>();

            mover.MoveTo(new Vector3(-9999f, 0f, 9999f));

            Assert.IsTrue(WaterProximity.IsInsideWater(mover.Destination));
        }
    }
}

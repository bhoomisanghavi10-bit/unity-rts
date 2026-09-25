using System;
using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class MapDefinitionTests
    {
        private static readonly MapId[] NonRiverMaps =
        {
            MapId.RiverValley, MapId.Highlands, MapId.Coastal,
            MapId.SkirmishMedium, MapId.SkirmishMountainPass,
            MapId.SkirmishHighlandFoothills, MapId.SkirmishClearing,
        };

        [Test]
        public void EveryMapId_ResolvesInMapRegistry()
        {
            foreach (MapId id in Enum.GetValues(typeof(MapId)))
            {
                MapRegistry.Select(id);
                Assert.AreEqual(id, MapRegistry.CurrentId);
                Assert.IsNotNull(MapRegistry.Current);
            }

            MapRegistry.Select(MapId.RiverValley);
        }

        [Test]
        public void NonRiverMaps_HaveNoFords()
        {
            foreach (MapId id in NonRiverMaps)
            {
                MapRegistry.Select(id);
                Assert.IsTrue(MapRegistry.Current.FordCentersX == null || MapRegistry.Current.FordCentersX.Length == 0,
                    id + " should have no fords");
            }

            MapRegistry.Select(MapId.RiverValley);
        }

        [Test]
        public void NonClearingMaps_HaveNoForestLane()
        {
            foreach (MapId id in NonRiverMaps)
            {
                if (id == MapId.SkirmishClearing)
                {
                    continue;
                }

                MapRegistry.Select(id);
                Assert.AreEqual(0f, MapRegistry.Current.ForestLaneWidth, id + " should have no forest lane");
            }

            MapRegistry.Select(MapId.RiverValley);
        }

        [Test]
        public void DividedRiverbed_HasFordsAndAWaterBandThroughTheCenter()
        {
            MapRegistry.Select(MapId.SkirmishDividedRiverbed);
            MapDefinitionData map = MapRegistry.Current;

            Assert.Greater(map.FordCentersX.Length, 0);
            Assert.Greater(map.WaterHalfExtents.x, 0f);
            Assert.Greater(map.WaterHalfExtents.z, 0f);
            Assert.AreEqual(0f, map.WaterCenter.z, 0.001f);

            MapRegistry.Select(MapId.RiverValley);
        }

        // The exact bug found while planning this map: reusing every other
        // skirmish map's Enemy2 start (z=0) would put it inside the river
        // band running across the whole map at Z=0.
        [Test]
        public void DividedRiverbed_Enemy2Start_IsNotOnTheRiverCenterline()
        {
            MapRegistry.Select(MapId.SkirmishDividedRiverbed);
            MapDefinitionData map = MapRegistry.Current;

            float distanceFromRiverCenter = System.Math.Abs(map.Enemy2TownCenter.z - map.WaterCenter.z);
            Assert.Greater(distanceFromRiverCenter, map.WaterHalfExtents.z);

            MapRegistry.Select(MapId.RiverValley);
        }

        [Test]
        public void Clearing_HasADenseForestThresholdAndAForestLane()
        {
            MapRegistry.Select(MapId.SkirmishClearing);
            MapDefinitionData map = MapRegistry.Current;

            Assert.Greater(map.ForestLaneWidth, 0f);
            Assert.Less(map.ForestThreshold, 0.58f);

            MapRegistry.Select(MapId.RiverValley);
        }
    }
}

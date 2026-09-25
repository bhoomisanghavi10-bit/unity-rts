using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class WaterBasinFinderTests
    {
        private const float MapSize = SkirmishMapZones.MediumMapSize; // 158
        private const float HeightScale = 20f;

        // A flat 0.5*HeightScale plane everywhere except a low dip
        // (0.05*HeightScale) inside a known world-space rectangle.
        private static System.Func<float, float, float> FlatWithDip(float dipCenterX, float dipCenterZ, float dipHalfWidth, float dipHalfDepth)
        {
            return (worldX, worldZ) =>
            {
                bool inDip = Mathf.Abs(worldX - dipCenterX) <= dipHalfWidth && Mathf.Abs(worldZ - dipCenterZ) <= dipHalfDepth;
                return (inDip ? 0.05f : 0.5f) * HeightScale;
            };
        }

        [Test]
        public void FindLowestFlatRegion_FindsTheDip_WhenItIsLowEnough()
        {
            var heightAt = FlatWithDip(10f, -5f, 12f, 12f);

            WaterBasinFinder.Basin basin = WaterBasinFinder.FindLowestFlatRegion(
                heightAt, MapSize,
                halfWidth: 8f, halfDepth: 8f, gridSteps: 9, maxAllowedHeight: 0.2f * HeightScale);

            Assert.IsTrue(basin.Found);
            Assert.AreEqual(10f, basin.Center.x, 6f);
            Assert.AreEqual(-5f, basin.Center.y, 6f);
            Assert.LessOrEqual(basin.MaxHeightInRect, 0.2f * HeightScale);
        }

        [Test]
        public void FindLowestFlatRegion_ReturnsNotFound_WhenNothingIsLowEnough()
        {
            System.Func<float, float, float> heightAt = (x, z) => 0.9f * HeightScale; // uniformly high, no basin at all.

            WaterBasinFinder.Basin basin = WaterBasinFinder.FindLowestFlatRegion(
                heightAt, MapSize,
                halfWidth: 8f, halfDepth: 8f, gridSteps: 5, maxAllowedHeight: 0.2f * HeightScale);

            Assert.IsFalse(basin.Found);
        }

        [Test]
        public void FindLowestFlatRegion_StaysInsideTheContestedZone()
        {
            var heightAt = FlatWithDip(10f, -5f, 12f, 12f);

            WaterBasinFinder.Basin basin = WaterBasinFinder.FindLowestFlatRegion(
                heightAt, MapSize,
                halfWidth: 8f, halfDepth: 8f, gridSteps: 9, maxAllowedHeight: 0.2f * HeightScale);

            Assert.IsTrue(basin.Found);
            var rectCorner = new Vector3(
                basin.Center.x + basin.HalfExtents.x,
                0f,
                basin.Center.y + basin.HalfExtents.y);
            Assert.AreEqual(MapZone.Contested, SkirmishMapZones.Classify(rectCorner, MapSize));
        }

        [Test]
        public void FindLowestFlatRegion_NullOrDegenerateInput_ReturnsNotFound()
        {
            Assert.IsFalse(WaterBasinFinder.FindLowestFlatRegion(null, MapSize, 8f, 8f, 5, 10f).Found);

            var heightAt = FlatWithDip(0f, 0f, 12f, 12f);
            Assert.IsFalse(WaterBasinFinder.FindLowestFlatRegion(heightAt, MapSize, 0f, 8f, 5, 10f).Found);
        }

        [Test]
        public void FindLowestFlatRegion_IsDeterministic()
        {
            var heightAt = FlatWithDip(-8f, 12f, 10f, 10f);

            WaterBasinFinder.Basin a = WaterBasinFinder.FindLowestFlatRegion(heightAt, MapSize, 6f, 6f, 7, 0.2f * HeightScale);
            WaterBasinFinder.Basin b = WaterBasinFinder.FindLowestFlatRegion(heightAt, MapSize, 6f, 6f, 7, 0.2f * HeightScale);

            Assert.AreEqual(a.Found, b.Found);
            Assert.AreEqual(a.Center, b.Center);
        }
    }
}

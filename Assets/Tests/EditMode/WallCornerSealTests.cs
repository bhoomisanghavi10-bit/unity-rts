using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.Tests
{
    // Wall mechanics audit (2026-09-29): WallCornerSeal.TryComputeSeal is
    // the pure function behind the diagonal wall-gap block - see its own
    // class comment for why a geometric AABB gap check stands in for
    // AoE2's literal tile-grid diagonal rule.
    public class WallCornerSealTests
    {
        private const float MaxGap = 1.5f;

        [Test]
        public void DiagonalNeighbors_WithinMaxGap_ReturnsSealClosingTheGap()
        {
            var centerA = new Vector3(0f, 1f, 0f);
            var sizeA = new Vector2(2.4f, 2.4f);
            var centerB = new Vector3(3f, 1f, 3f);
            var sizeB = new Vector2(2.4f, 2.4f);

            bool result = WallCornerSeal.TryComputeSeal(centerA, sizeA, centerB, sizeB, MaxGap, out Vector3 sealCenter, out Vector2 sealSize);

            Assert.IsTrue(result);
            Assert.Greater(sealSize.x, 0f);
            Assert.Greater(sealSize.y, 0f);
        }

        [Test]
        public void DiagonalNeighbors_TouchingExactlyAtOneCorner_ReturnsNoSeal()
        {
            // Two 2.4x2.4 boxes offset by exactly (2.4, 2.4) touch at a
            // single point (zero-width gap on both axes) - not a positive
            // gap, so nothing to seal (a real NavMeshObstacle already has
            // no interior at that single point for an agent to occupy).
            var centerA = new Vector3(0f, 0f, 0f);
            var sizeA = new Vector2(2.4f, 2.4f);
            var centerB = new Vector3(2.4f, 0f, 2.4f);
            var sizeB = new Vector2(2.4f, 2.4f);

            bool result = WallCornerSeal.TryComputeSeal(centerA, sizeA, centerB, sizeB, MaxGap, out _, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void DiagonalNeighbors_WithGenuineGap_SealSizeMatchesTheGap()
        {
            // A: 2.4x2.4 box centered at origin -> spans [-1.2, 1.2] on
            // both axes. B: 2.4x2.4 box centered at (3.6, 0, 3.6) -> spans
            // [2.4, 4.8]. Gap on each axis = 2.4 - 1.2 = 1.2.
            var centerA = new Vector3(0f, 0f, 0f);
            var sizeA = new Vector2(2.4f, 2.4f);
            var centerB = new Vector3(3.6f, 0f, 3.6f);
            var sizeB = new Vector2(2.4f, 2.4f);

            bool result = WallCornerSeal.TryComputeSeal(centerA, sizeA, centerB, sizeB, MaxGap, out Vector3 sealCenter, out Vector2 sealSize);

            Assert.IsTrue(result);
            Assert.AreEqual(1.2f, sealSize.x, 0.001f);
            Assert.AreEqual(1.2f, sealSize.y, 0.001f);
            // The seal should sit exactly between the two closest corners:
            // A's corner at (1.2, 1.2), B's corner at (2.4, 2.4).
            Assert.AreEqual(1.8f, sealCenter.x, 0.001f);
            Assert.AreEqual(1.8f, sealCenter.z, 0.001f);
        }

        [Test]
        public void OrthogonalNeighbors_SharingAnEdge_ReturnsNoSeal()
        {
            // B sits directly beside A along X only (same Z range) -
            // already touching/overlapping on Z, so this is a normal
            // orthogonal adjacency, not a diagonal corner gap.
            var centerA = new Vector3(0f, 0f, 0f);
            var sizeA = new Vector2(2.4f, 2.4f);
            var centerB = new Vector3(2.4f, 0f, 0f);
            var sizeB = new Vector2(2.4f, 2.4f);

            bool result = WallCornerSeal.TryComputeSeal(centerA, sizeA, centerB, sizeB, MaxGap, out _, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void FarApart_BeyondMaxGap_ReturnsNoSeal()
        {
            var centerA = new Vector3(0f, 0f, 0f);
            var sizeA = new Vector2(2.4f, 2.4f);
            var centerB = new Vector3(20f, 0f, 20f);
            var sizeB = new Vector2(2.4f, 2.4f);

            bool result = WallCornerSeal.TryComputeSeal(centerA, sizeA, centerB, sizeB, MaxGap, out _, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void OverlappingFootprints_ReturnsNoSeal()
        {
            var centerA = new Vector3(0f, 0f, 0f);
            var sizeA = new Vector2(2.4f, 2.4f);
            var centerB = new Vector3(1f, 0f, 1f);
            var sizeB = new Vector2(2.4f, 2.4f);

            bool result = WallCornerSeal.TryComputeSeal(centerA, sizeA, centerB, sizeB, MaxGap, out _, out _);

            Assert.IsFalse(result);
        }
    }
}

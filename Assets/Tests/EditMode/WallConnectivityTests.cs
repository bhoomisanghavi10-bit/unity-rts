using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.Tests
{
    // Wall mechanics audit (2026-09-29): WallConnectivity.ClassifyPieceKind
    // is the pure function driving automatic wall-piece shape selection -
    // see its own class comment for why angle-between-neighbors replaces
    // AoE2's literal cardinal bitmask.
    public class WallConnectivityTests
    {
        private static readonly Vector3 Origin = Vector3.zero;

        [Test]
        public void ZeroNeighbors_ClassifiesAsEndPost()
        {
            var neighbors = new List<Vector3>();

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.EndPost, kind);
        }

        [Test]
        public void OneNeighbor_ClassifiesAsEndPost()
        {
            var neighbors = new List<Vector3> { new Vector3(2.4f, 0f, 0f) };

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.EndPost, kind);
        }

        [Test]
        public void TwoNeighborsRoughlyOpposite_ClassifiesAsStraight()
        {
            var neighbors = new List<Vector3>
            {
                new Vector3(2.4f, 0f, 0f),
                new Vector3(-2.4f, 0f, 0f),
            };

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.Straight, kind);
        }

        [Test]
        public void TwoNeighborsAtRightAngle_ClassifiesAsCorner()
        {
            var neighbors = new List<Vector3>
            {
                new Vector3(2.4f, 0f, 0f),
                new Vector3(0f, 0f, 2.4f),
            };

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.Corner, kind);
        }

        [Test]
        public void TwoNeighborsAtShallowAngle_OutsideTolerance_ClassifiesAsCornerNotStraight()
        {
            // 130 degrees apart - a 50-degree deviation from 180, clearly
            // outside the default 30-degree tolerance, so this must NOT be
            // misclassified as a straight continuation.
            float radians = 130f * Mathf.Deg2Rad;
            var neighbors = new List<Vector3>
            {
                new Vector3(2.4f, 0f, 0f),
                new Vector3(Mathf.Cos(radians) * 2.4f, 0f, Mathf.Sin(radians) * 2.4f),
            };

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.Corner, kind);
        }

        [Test]
        public void ThreeNeighbors_ClassifiesAsTJunction()
        {
            var neighbors = new List<Vector3>
            {
                new Vector3(2.4f, 0f, 0f),
                new Vector3(-2.4f, 0f, 0f),
                new Vector3(0f, 0f, 2.4f),
            };

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.TJunction, kind);
        }

        [Test]
        public void FourOrMoreNeighbors_ClassifiesAsXJunction()
        {
            var neighbors = new List<Vector3>
            {
                new Vector3(2.4f, 0f, 0f),
                new Vector3(-2.4f, 0f, 0f),
                new Vector3(0f, 0f, 2.4f),
                new Vector3(0f, 0f, -2.4f),
            };

            var kind = WallConnectivity.ClassifyPieceKind(Origin, neighbors);

            Assert.AreEqual(WallFactory.WallPieceKind.XJunction, kind);
        }
    }
}

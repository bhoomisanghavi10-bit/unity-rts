using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class ForestLanesTests
    {
        [Test]
        public void DistanceToNearestLane_NoLanes_IsMaxValue()
        {
            Assert.AreEqual(float.MaxValue, ForestLanes.DistanceToNearestLane(Vector3.zero, null));
            Assert.AreEqual(float.MaxValue, ForestLanes.DistanceToNearestLane(Vector3.zero, new Vector3[0]));
        }

        [Test]
        public void DistanceToNearestLane_OnTheSegment_IsZero()
        {
            var lane = new[] { new Vector3(0f, 0f, -50f), new Vector3(0f, 0f, 50f) };
            Assert.AreEqual(0f, ForestLanes.DistanceToNearestLane(new Vector3(0f, 0f, 0f), lane), 0.001f);
        }

        [Test]
        public void DistanceToNearestLane_OffToTheSide_IsPerpendicularDistance()
        {
            var lane = new[] { new Vector3(0f, 0f, -50f), new Vector3(0f, 0f, 50f) };
            Assert.AreEqual(7f, ForestLanes.DistanceToNearestLane(new Vector3(7f, 0f, 0f), lane), 0.001f);
        }

        [Test]
        public void DistanceToNearestLane_PastTheEndpoint_ClampsToEndpoint()
        {
            var lane = new[] { new Vector3(0f, 0f, -50f), new Vector3(0f, 0f, 50f) };
            Assert.AreEqual(10f, ForestLanes.DistanceToNearestLane(new Vector3(0f, 0f, 60f), lane), 0.001f);
        }

        [Test]
        public void DistanceToNearestLane_PicksTheNearestOfSeveralLanes()
        {
            var lanes = new[]
            {
                new Vector3(0f, 0f, 57f), new Vector3(0f, 0f, -57f),
                new Vector3(0f, 0f, 57f), new Vector3(-57f, 0f, 0f),
                new Vector3(0f, 0f, -57f), new Vector3(-57f, 0f, 0f),
            };
            // The midpoint of the third lane (Enemy <-> Enemy2) - distance
            // to it should read as ~0 even though the other two lanes exist.
            float d = ForestLanes.DistanceToNearestLane(new Vector3(-28.5f, 0f, -28.5f), lanes);
            Assert.Less(d, 1f);
        }

        [Test]
        public void DistanceToNearestLane_IgnoresY()
        {
            var lane = new[] { new Vector3(0f, 0f, -50f), new Vector3(0f, 0f, 50f) };
            Assert.AreEqual(0f, ForestLanes.DistanceToNearestLane(new Vector3(0f, 999f, 0f), lane), 0.001f);
        }
    }
}

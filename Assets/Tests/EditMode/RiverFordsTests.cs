using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class RiverFordsTests
    {
        [Test]
        public void FordFactor_EmptyArray_IsAlwaysZero()
        {
            Assert.AreEqual(0f, RiverFords.FordFactor(0f, new float[0], 4f, 10f));
            Assert.AreEqual(0f, RiverFords.FordFactor(0f, null, 4f, 10f));
        }

        [Test]
        public void FordFactor_AtCenter_IsOne()
        {
            Assert.AreEqual(1f, RiverFords.FordFactor(40f, new[] { -40f, 0f, 40f }, 4f, 10f), 0.001f);
        }

        [Test]
        public void FordFactor_WellInsideHalfWidth_IsOne()
        {
            Assert.AreEqual(1f, RiverFords.FordFactor(2f, new[] { 0f }, 4f, 10f), 0.001f);
        }

        [Test]
        public void FordFactor_FarPastFalloff_IsZero()
        {
            Assert.AreEqual(0f, RiverFords.FordFactor(30f, new[] { 0f }, 4f, 10f), 0.001f);
        }

        [Test]
        public void FordFactor_IsSymmetricAroundCenter()
        {
            float left = RiverFords.FordFactor(-3f, new[] { 0f }, 4f, 10f);
            float right = RiverFords.FordFactor(3f, new[] { 0f }, 4f, 10f);
            Assert.AreEqual(left, right, 0.0001f);
        }

        [Test]
        public void FordFactor_UsesNearestFord()
        {
            // Exactly at one ford's centre, far from the other two - should
            // read as fully in a ford, not averaged/blended across all three.
            Assert.AreEqual(1f, RiverFords.FordFactor(0f, new[] { -40f, 0f, 40f }, 4f, 10f), 0.001f);
        }

        [Test]
        public void FordFactor_MonotonicFalloff()
        {
            float atHalfWidth = RiverFords.FordFactor(4f, new[] { 0f }, 4f, 10f);
            float midFalloff = RiverFords.FordFactor(9f, new[] { 0f }, 4f, 10f);
            float atEdge = RiverFords.FordFactor(14f, new[] { 0f }, 4f, 10f);
            Assert.Greater(atHalfWidth, midFalloff);
            Assert.Greater(midFalloff, atEdge);
        }
    }
}

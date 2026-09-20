using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class SkirmishMapZonesTests
    {
        private const float Size = SkirmishMapZones.MediumMapSize; // 158

        [Test]
        public void ContestedWidth_OnMediumMap_Is78()
        {
            Assert.AreEqual(78f, SkirmishMapZones.ContestedWidth(Size), 0.001f);
        }

        [Test]
        public void Classify_Center_IsContested()
        {
            Assert.AreEqual(MapZone.Contested, SkirmishMapZones.Classify(Vector3.zero, Size));
        }

        [Test]
        public void Classify_ContestedHomeBoundary_IsAt39FromCenter()
        {
            Assert.AreEqual(MapZone.Contested, SkirmishMapZones.Classify(new Vector3(38.9f, 0f, 0f), Size));
            Assert.AreEqual(MapZone.HomeBase, SkirmishMapZones.Classify(new Vector3(39f, 0f, 0f), Size));
        }

        [Test]
        public void Classify_HomeDeadzoneBoundary_IsAt76FromCenter()
        {
            Assert.AreEqual(MapZone.HomeBase, SkirmishMapZones.Classify(new Vector3(0f, 0f, 75.9f), Size));
            Assert.AreEqual(MapZone.EdgeDeadzone, SkirmishMapZones.Classify(new Vector3(0f, 0f, 76f), Size));
            Assert.AreEqual(MapZone.EdgeDeadzone, SkirmishMapZones.Classify(new Vector3(0f, 0f, -78.9f), Size));
        }

        [Test]
        public void Classify_UsesConcentricSquares_NotCircles()
        {
            // 55 along both axes is ~78 by straight distance but only 55 by
            // Chebyshev, so it's still inside the home ring, not the dead zone.
            Assert.AreEqual(MapZone.HomeBase, SkirmishMapZones.Classify(new Vector3(55f, 0f, 55f), Size));
            // A corner sits in the dead zone once either axis reaches 76.
            Assert.AreEqual(MapZone.EdgeDeadzone, SkirmishMapZones.Classify(new Vector3(20f, 0f, 77f), Size));
        }

        [Test]
        public void Classify_IgnoresHeight()
        {
            Assert.AreEqual(MapZone.HomeBase, SkirmishMapZones.Classify(new Vector3(0f, 500f, 60f), Size));
        }

        [Test]
        public void IsSpawnable_FalseOnlyInTheDeadzone()
        {
            Assert.IsTrue(SkirmishMapZones.IsSpawnable(new Vector3(0f, 0f, 10f), Size));
            Assert.IsTrue(SkirmishMapZones.IsSpawnable(new Vector3(0f, 0f, 70f), Size));
            Assert.IsFalse(SkirmishMapZones.IsSpawnable(new Vector3(0f, 0f, 77f), Size));
        }

        [Test]
        public void IsWithinStartingResourceRange_Is10To15Units()
        {
            var tc = new Vector3(0f, 0f, 60f);

            Assert.IsFalse(SkirmishMapZones.IsWithinStartingResourceRange(tc, new Vector3(0f, 0f, 55f)));   // 5
            Assert.IsTrue(SkirmishMapZones.IsWithinStartingResourceRange(tc, new Vector3(0f, 0f, 48f)));    // 12
            Assert.IsTrue(SkirmishMapZones.IsWithinStartingResourceRange(tc, new Vector3(9f, 5f, 48f)));    // 15, Y ignored
            Assert.IsFalse(SkirmishMapZones.IsWithinStartingResourceRange(tc, new Vector3(0f, 0f, 40f)));   // 20
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.Tests
{
    public class ResourcePlacementTests
    {
        private const float MediumSize = SkirmishMapZones.MediumMapSize; // 158

        [Test]
        public void RandomPointNearTownCenter_IsAlways10To15UnitsAway()
        {
            var rng = new DeterministicRandom(777);
            var townCenter = new Vector3(0f, 0f, 57f);

            for (int i = 0; i < 200; i++)
            {
                Vector3 point = ResourcePlacement.RandomPointNearTownCenter(rng, townCenter);
                Assert.IsTrue(SkirmishMapZones.IsWithinStartingResourceRange(townCenter, point),
                    $"Point {point} was not within the guaranteed starting range of {townCenter}");
            }
        }

        [Test]
        public void RandomPointNearTownCenter_OffsetsFromTownCenter_NotFromWorldOrigin()
        {
            var rng = new DeterministicRandom(11);
            var townCenter = new Vector3(-57f, 0f, 0f);

            Vector3 point = ResourcePlacement.RandomPointNearTownCenter(rng, townCenter);

            // Distance from the world origin should be roughly 57 +/- 15,
            // not a small radius around (0,0,0) - i.e. this really is
            // relative to the town centre, not the map centre.
            float distanceFromOrigin = new Vector2(point.x, point.z).magnitude;
            Assert.Greater(distanceFromOrigin, 40f);
        }

        [Test]
        public void RandomPointInContestedZone_AlwaysClassifiesAsContested()
        {
            var rng = new DeterministicRandom(42);

            for (int i = 0; i < 200; i++)
            {
                Vector3 point = ResourcePlacement.RandomPointInContestedZone(rng, MediumSize, 15f, 40f, 20);
                Assert.AreEqual(MapZone.Contested, SkirmishMapZones.Classify(point, MediumSize),
                    $"Point {point} was not classified as Contested");
            }
        }

        [Test]
        public void RandomPointInContestedZone_FallsBackWhenRingNeverHitsContested()
        {
            // minRadius/maxRadius both sit entirely in the home buffer
            // (39..76), so no attempt can ever succeed - the fallback path
            // must still return a genuinely Contested point rather than
            // looping forever or returning a home-buffer point.
            var rng = new DeterministicRandom(9001);

            Vector3 point = ResourcePlacement.RandomPointInContestedZone(rng, MediumSize, 50f, 60f, 5);

            Assert.AreEqual(MapZone.Contested, SkirmishMapZones.Classify(point, MediumSize));
        }

        [Test]
        public void RandomPointInContestedZone_IsDeterministicForTheSameSeed()
        {
            var rngA = new DeterministicRandom(555);
            var rngB = new DeterministicRandom(555);

            Vector3 a = ResourcePlacement.RandomPointInContestedZone(rngA, MediumSize, 15f, 40f, 20);
            Vector3 b = ResourcePlacement.RandomPointInContestedZone(rngB, MediumSize, 15f, 40f, 20);

            Assert.AreEqual(a, b);
        }
    }
}

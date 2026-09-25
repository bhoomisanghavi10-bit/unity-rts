using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class SkirmishMapScalingTests
    {
        private const float MediumContestedWidth = 78f;

        [Test]
        public void ScaleCount_IsIdentity_AtTheMediumContestedWidth()
        {
            Assert.AreEqual(20, SkirmishMapScaling.ScaleCount(20, MediumContestedWidth));
        }

        [Test]
        public void ScaleCount_ScalesDown_ForANarrowerContestedZone()
        {
            int scaled = SkirmishMapScaling.ScaleCount(20, MediumContestedWidth * 0.5f);
            Assert.Less(scaled, 20);
            Assert.GreaterOrEqual(scaled, 1);
        }

        [Test]
        public void ScaleCount_ScalesUp_ForAWiderContestedZone()
        {
            int scaled = SkirmishMapScaling.ScaleCount(20, MediumContestedWidth * 2f);
            Assert.Greater(scaled, 20);
        }

        [Test]
        public void ScaleCount_NeverReturnsZeroOrNegative_EvenForATinyZone()
        {
            Assert.GreaterOrEqual(SkirmishMapScaling.ScaleCount(20, 0.01f), 1);
        }

        [Test]
        public void ScaleRadius_IsIdentity_AtTheMediumContestedWidth()
        {
            Assert.AreEqual(15f, SkirmishMapScaling.ScaleRadius(15f, MediumContestedWidth), 0.01f);
            Assert.AreEqual(40f, SkirmishMapScaling.ScaleRadius(40f, MediumContestedWidth), 0.01f);
        }

        [Test]
        public void ScaleRadius_ScalesProportionally()
        {
            float half = SkirmishMapScaling.ScaleRadius(40f, MediumContestedWidth * 0.5f);
            Assert.AreEqual(20f, half, 0.01f);

            float doubled = SkirmishMapScaling.ScaleRadius(40f, MediumContestedWidth * 2f);
            Assert.AreEqual(80f, doubled, 0.01f);
        }

        [Test]
        public void HomeBandMidpoint_MatchesTheDocumentedMediumValue()
        {
            // docs/SKIRMISH_MAP_SPEC.md: "|z|=57 is the centre of the 39..76 band" for the 158 map.
            Assert.AreEqual(57.5f, SkirmishMapScaling.HomeBandMidpoint(158f), 0.01f);
        }

        [Test]
        public void HomeBandMidpoint_StaysInsideTheHomeBaseZone_ForSmallAndLargeMaps()
        {
            foreach (float mapSize in new[] { 120f, 158f, 240f })
            {
                float midpoint = SkirmishMapScaling.HomeBandMidpoint(mapSize);
                var point = new UnityEngine.Vector3(0f, 0f, midpoint);
                Assert.AreEqual(MapZone.HomeBase, SkirmishMapZones.Classify(point, mapSize),
                    $"midpoint {midpoint} on a {mapSize} map was not classified HomeBase");
            }
        }
    }
}

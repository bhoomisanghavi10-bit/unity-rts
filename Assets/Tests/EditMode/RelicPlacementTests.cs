using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.Tests
{
    public class RelicPlacementTests
    {
        private const float MediumSize = SkirmishMapZones.MediumMapSize; // 158
        private const float MediumContestedWidth = 78f; // SkirmishMapZones.ContestedWidth(158)

        [Test]
        public void ComputeRelicCount_ReproducesFive_ForTodaysMediumMap()
        {
            Assert.AreEqual(5, RelicPlacement.ComputeRelicCount(MediumContestedWidth));
        }

        [Test]
        public void ComputeRelicCount_ScalesUp_ForALargerContestedZone()
        {
            int medium = RelicPlacement.ComputeRelicCount(MediumContestedWidth);
            int larger = RelicPlacement.ComputeRelicCount(MediumContestedWidth * 1.5f);

            Assert.Greater(larger, medium);
        }

        [Test]
        public void ComputeRelicCount_ScalesDown_ForASmallerContestedZone()
        {
            int medium = RelicPlacement.ComputeRelicCount(MediumContestedWidth);
            int smaller = RelicPlacement.ComputeRelicCount(MediumContestedWidth * 0.5f);

            Assert.Less(smaller, medium);
        }

        [Test]
        public void ComputeRelicCount_NeverGoesBelowTheMinimum_EvenForATinyZone()
        {
            Assert.GreaterOrEqual(RelicPlacement.ComputeRelicCount(1f), 3);
        }

        [Test]
        public void ComputeRelicCount_NeverExceedsTheMaximum_EvenForAHugeZone()
        {
            Assert.LessOrEqual(RelicPlacement.ComputeRelicCount(10000f), 12);
        }

        [Test]
        public void WedgeFor_PartitionsTheFullCircle_WithNoGapsOrOverlaps()
        {
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                RelicPlacement.WedgeFor(i, count, out float start, out float end);
                Assert.AreEqual(i * (Mathf.PI * 2f / count), start, 0.0001f);
                Assert.AreEqual(end - start, Mathf.PI * 2f / count, 0.0001f);
            }
        }

        [Test]
        public void WedgeFor_WrapsTheIndex_ForOutOfRangeValues()
        {
            RelicPlacement.WedgeFor(0, 4, out float start0, out float end0);
            RelicPlacement.WedgeFor(4, 4, out float start4, out float end4);

            Assert.AreEqual(start0, start4, 0.0001f);
            Assert.AreEqual(end0, end4, 0.0001f);
        }

        [Test]
        public void RandomPointInContestedZone_WithWedge_StaysWithinTheWedgeAngle()
        {
            var rng = new DeterministicRandom(2024);
            const int count = 5;

            for (int i = 0; i < count; i++)
            {
                RelicPlacement.WedgeFor(i, count, out float wedgeStart, out float wedgeEnd);
                for (int draw = 0; draw < 30; draw++)
                {
                    Vector3 point = ResourcePlacement.RandomPointInContestedZone(rng, MediumSize, 15f, 40f, 20, wedgeStart, wedgeEnd);
                    float angle = Mathf.Atan2(point.z, point.x);
                    if (angle < 0f)
                    {
                        angle += Mathf.PI * 2f;
                    }

                    // Allow a small epsilon for the fallback path, which can
                    // draw its own angle independently within the same range.
                    Assert.IsTrue(angle >= wedgeStart - 0.01f && angle <= wedgeEnd + 0.01f,
                        $"angle {angle} outside wedge [{wedgeStart}, {wedgeEnd}) for point {point}");
                    Assert.AreEqual(MapZone.Contested, SkirmishMapZones.Classify(point, MediumSize));
                }
            }
        }
    }
}

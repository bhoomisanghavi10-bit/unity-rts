using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class SkirmishTerrainCarvingTests
    {
        // 5x5 grid, mapSize=20 (half=10) -> world Z/X per index:
        // 0=-10, 1=-5, 2=0, 3=5, 4=10.
        private static float[,] UniformHeights(int size, float value)
        {
            var heights = new float[size, size];
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[z, x] = value;
                }
            }

            return heights;
        }

        [Test]
        public void ApplyRidgeWithPass_FarFromRidge_IsUnchanged()
        {
            var heights = UniformHeights(5, 0.3f);
            var result = SkirmishTerrainCarving.ApplyRidgeWithPass(
                heights, 20f, 10f,
                ridgeCenterZ: 0f, ridgeHalfWidth: 2f, ridgeFalloff: 2f, ridgeRaiseWorld: 4f,
                passCenterX: 0f, passHalfWidth: 1f, passFalloff: 1f, passFloorWorld: 1f);

            // z index 0 -> worldZ = -10, far outside the ridge band (0 +/- 4).
            Assert.AreEqual(0.3f, result[0, 2], 0.001f);
        }

        [Test]
        public void ApplyRidgeWithPass_InRidgeAwayFromPass_IsRaised()
        {
            var heights = UniformHeights(5, 0.3f);
            var result = SkirmishTerrainCarving.ApplyRidgeWithPass(
                heights, 20f, 10f,
                ridgeCenterZ: 0f, ridgeHalfWidth: 2f, ridgeFalloff: 2f, ridgeRaiseWorld: 4f,
                passCenterX: 0f, passHalfWidth: 1f, passFalloff: 1f, passFloorWorld: 1f);

            // z index 2 -> worldZ = 0 (ridge centre); x index 0 -> worldX = -10 (far from the pass).
            Assert.AreEqual(0.7f, result[2, 0], 0.001f);
        }

        [Test]
        public void ApplyRidgeWithPass_AtRidgeCentreAndPassCentre_IsCarvedToFloor()
        {
            var heights = UniformHeights(5, 0.3f);
            var result = SkirmishTerrainCarving.ApplyRidgeWithPass(
                heights, 20f, 10f,
                ridgeCenterZ: 0f, ridgeHalfWidth: 2f, ridgeFalloff: 2f, ridgeRaiseWorld: 4f,
                passCenterX: 0f, passHalfWidth: 1f, passFalloff: 1f, passFloorWorld: 1f);

            // z index 2 -> worldZ = 0, x index 2 -> worldX = 0: dead centre of both.
            Assert.AreEqual(0.1f, result[2, 2], 0.001f);
        }

        [Test]
        public void ApplyCornerMesas_AtAMesaCenter_IsRaisedToFullHeight()
        {
            var heights = UniformHeights(5, 0.3f);
            var mesas = new[] { new Vector2(0f, 0f) };
            var result = SkirmishTerrainCarving.ApplyCornerMesas(heights, 20f, 10f, mesas, 2f, 2f, 4f);

            // z/x index 2 -> worldX/Z = 0, dead centre of the one mesa.
            Assert.AreEqual(0.7f, result[2, 2], 0.001f);
        }

        [Test]
        public void ApplyCornerMesas_FarFromEveryMesa_IsUnchanged()
        {
            var heights = UniformHeights(5, 0.3f);
            var mesas = new[] { new Vector2(0f, 0f) };
            var result = SkirmishTerrainCarving.ApplyCornerMesas(heights, 20f, 10f, mesas, 2f, 2f, 4f);

            // z/x index 0 -> worldX/Z = -10, well past radius(2)+falloff(2).
            Assert.AreEqual(0.3f, result[0, 0], 0.001f);
        }

        [Test]
        public void ApplyCornerMesas_NoMesas_LeavesHeightsUnchanged()
        {
            var heights = UniformHeights(3, 0.4f);
            var result = SkirmishTerrainCarving.ApplyCornerMesas(heights, 20f, 10f, null, 2f, 2f, 4f);
            Assert.AreEqual(0.4f, result[1, 1], 0.0001f);
        }

        [Test]
        public void ApplyCornerMesas_OverlappingMesas_UseTheStrongestNotTheSum()
        {
            var heights = UniformHeights(5, 0.3f);
            // Two mesas both centred at the same point - if they summed,
            // this would exceed a single mesa's own raise.
            var mesas = new[] { new Vector2(0f, 0f), new Vector2(0f, 0f) };
            var result = SkirmishTerrainCarving.ApplyCornerMesas(heights, 20f, 10f, mesas, 2f, 2f, 4f);
            Assert.AreEqual(0.7f, result[2, 2], 0.001f);
        }

        [Test]
        public void LimitSlope_AlreadyGentle_IsUnchanged()
        {
            var heights = UniformHeights(5, 0.3f);
            var result = SkirmishTerrainCarving.LimitSlope(heights, 20f, 10f, 35f, 10);
            Assert.AreEqual(0.3f, result[2, 2], 0.0001f);
        }

        [Test]
        public void LimitSlope_ASpike_IsFlattenedBelowTheLimit()
        {
            // 5x5, mapSize 20 -> cellSize 5. A lone spike at the centre,
            // flat everywhere else.
            var heights = UniformHeights(5, 0.1f);
            heights[2, 2] = 1f;
            var result = SkirmishTerrainCarving.LimitSlope(heights, 20f, 10f, 35f, 40);

            float scale = 10f;
            float cellSize = 20f / 4f;
            float maxDelta = Mathf.Tan(35f * Mathf.Deg2Rad) * cellSize;
            float centerWorld = result[2, 2] * scale;
            float neighborWorld = result[2, 1] * scale;
            Assert.LessOrEqual(centerWorld - neighborWorld, maxDelta + 0.01f);
        }

        [Test]
        public void ApplyTerracing_ZeroStrength_LeavesHeightsUnchanged()
        {
            var heights = UniformHeights(3, 0.24f);
            var result = SkirmishTerrainCarving.ApplyTerracing(heights, 0.1f, 0f);
            Assert.AreEqual(0.24f, result[1, 1], 0.0001f);
        }

        [Test]
        public void ApplyTerracing_FullStrength_QuantizesToNearestStep()
        {
            var heights = UniformHeights(3, 0.24f);
            var result = SkirmishTerrainCarving.ApplyTerracing(heights, 0.1f, 1f);
            Assert.AreEqual(0.2f, result[1, 1], 0.0001f);

            heights = UniformHeights(3, 0.27f);
            result = SkirmishTerrainCarving.ApplyTerracing(heights, 0.1f, 1f);
            Assert.AreEqual(0.3f, result[1, 1], 0.0001f);
        }

        [Test]
        public void ApplyTerracing_PartialStrength_BlendsBetweenRawAndQuantized()
        {
            var heights = UniformHeights(3, 0.24f);
            var result = SkirmishTerrainCarving.ApplyTerracing(heights, 0.1f, 0.5f);
            // Raw 0.24, quantized 0.2 -> halfway is 0.22.
            Assert.AreEqual(0.22f, result[1, 1], 0.0001f);
        }
    }
}

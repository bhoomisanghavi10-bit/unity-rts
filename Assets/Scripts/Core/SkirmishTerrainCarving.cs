using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Pure height-array post-processing applied at bake time (see
    // Assets/Editor/Vista/VistaSpike.cs) to turn a stock Vista template's
    // base noise into a specific skirmish archetype - Mountain Pass's
    // ridge-and-corridor, Highland Foothills' terracing. Operates on the
    // same normalised (0..1) heights TerrainData.GetHeights/SetHeights
    // use, plus the map's world size and height scale, so it never needs
    // a live Terrain/Vista dependency and is directly unit-testable with
    // small synthetic arrays.
    public static class SkirmishTerrainCarving
    {
        // Raises a band across Z into a "mountain chain", then carves one
        // corridor through it around X = passCenterX - the corridor only
        // pulls height down where the ridge itself raised it, so terrain
        // far from the band is left completely untouched.
        public static float[,] ApplyRidgeWithPass(
            float[,] heights01,
            float mapSize,
            float heightScaleWorld,
            float ridgeCenterZ,
            float ridgeHalfWidth,
            float ridgeFalloff,
            float ridgeRaiseWorld,
            float passCenterX,
            float passHalfWidth,
            float passFalloff,
            float passFloorWorld)
        {
            int rz = heights01.GetLength(0);
            int rx = heights01.GetLength(1);
            var result = new float[rz, rx];
            float half = mapSize * 0.5f;
            float scale = Mathf.Max(heightScaleWorld, 0.0001f);

            for (int z = 0; z < rz; z++)
            {
                float worldZ = -half + (float)z / (rz - 1) * mapSize;
                float ridgeStrength = RidgeStrength(worldZ, ridgeCenterZ, ridgeHalfWidth, ridgeFalloff);

                for (int x = 0; x < rx; x++)
                {
                    float worldX = -half + (float)x / (rx - 1) * mapSize;
                    float baseWorld = heights01[z, x] * scale;
                    float raised = baseWorld + ridgeStrength * ridgeRaiseWorld;

                    float passStrength = PassStrength(worldX, passCenterX, passHalfWidth, passFalloff);
                    float carved = Mathf.Lerp(raised, passFloorWorld, passStrength * ridgeStrength);

                    result[z, x] = Mathf.Clamp01(carved / scale);
                }
            }

            return result;
        }

        private static float RidgeStrength(float worldZ, float ridgeCenterZ, float ridgeHalfWidth, float ridgeFalloff)
        {
            return 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(ridgeHalfWidth, ridgeHalfWidth + ridgeFalloff, Mathf.Abs(worldZ - ridgeCenterZ)));
        }

        private static float PassStrength(float worldX, float passCenterX, float passHalfWidth, float passFalloff)
        {
            return 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(passHalfWidth, passHalfWidth + passFalloff, Mathf.Abs(worldX - passCenterX)));
        }

        // Pure mask companion to ApplyRidgeWithPass: 1 where the ridge is
        // solid rock, 0 in the cleared pass corridor or far from the band -
        // used to splat the Rock terrain layer and to bias Gold/Stone
        // placement toward the mountain, not just to shape height.
        public static float[,] ComputeRidgeMask(
            float mapSize,
            int resolution,
            float ridgeCenterZ,
            float ridgeHalfWidth,
            float ridgeFalloff,
            float passCenterX,
            float passHalfWidth,
            float passFalloff)
        {
            var mask = new float[resolution, resolution];
            float half = mapSize * 0.5f;

            for (int z = 0; z < resolution; z++)
            {
                float worldZ = -half + (float)z / (resolution - 1) * mapSize;
                float ridgeStrength = RidgeStrength(worldZ, ridgeCenterZ, ridgeHalfWidth, ridgeFalloff);

                for (int x = 0; x < resolution; x++)
                {
                    float worldX = -half + (float)x / (resolution - 1) * mapSize;
                    float passStrength = PassStrength(worldX, passCenterX, passHalfWidth, passFalloff);
                    mask[z, x] = ridgeStrength * (1f - passStrength);
                }
            }

            return mask;
        }

        // Raises a flat-topped plateau with a steep (not gently smoothed)
        // edge at each centre - Crossroad Valleys' "mesa plateaus as
        // tactical corners" over its own open, rolling base. Additive and
        // per-plateau independent (unlike ApplyRidgeWithPass's carve, there
        // is no lowering), so overlapping plateaus simply combine.
        public static float[,] ApplyCornerMesas(
            float[,] heights01,
            float mapSize,
            float heightScaleWorld,
            Vector2[] mesaCentersXZ,
            float mesaRadius,
            float mesaFalloff,
            float mesaRaiseWorld)
        {
            int rz = heights01.GetLength(0);
            int rx = heights01.GetLength(1);
            var result = new float[rz, rx];
            float half = mapSize * 0.5f;
            float scale = Mathf.Max(heightScaleWorld, 0.0001f);

            for (int z = 0; z < rz; z++)
            {
                float worldZ = -half + (float)z / (rz - 1) * mapSize;
                for (int x = 0; x < rx; x++)
                {
                    float worldX = -half + (float)x / (rx - 1) * mapSize;
                    float raise = MesaStrength(worldX, worldZ, mesaCentersXZ, mesaRadius, mesaFalloff);
                    float raised = heights01[z, x] * scale + raise * mesaRaiseWorld;
                    result[z, x] = Mathf.Clamp01(raised / scale);
                }
            }

            return result;
        }

        private static float MesaStrength(float worldX, float worldZ, Vector2[] mesaCentersXZ, float mesaRadius, float mesaFalloff)
        {
            float best = 0f;
            if (mesaCentersXZ == null)
            {
                return best;
            }

            for (int i = 0; i < mesaCentersXZ.Length; i++)
            {
                float d = Vector2.Distance(new Vector2(worldX, worldZ), mesaCentersXZ[i]);
                // A steeper-than-smoothstep falloff (squared) so the top
                // reads flat and the edge reads as a cliff, not a gentle hill.
                float t = 1f - Mathf.Clamp01(Mathf.InverseLerp(mesaRadius, mesaRadius + mesaFalloff, d));
                float strength = t * t;
                if (strength > best)
                {
                    best = strength;
                }
            }

            return best;
        }

        // Pure mask companion to ApplyCornerMesas: 1 at the top of a mesa,
        // falling to 0 by its edge - used to splat the Sand terrain layer
        // and to bias Gold/Stone placement onto the mesas.
        public static float[,] ComputeCornerMesaMask(
            float mapSize,
            int resolution,
            Vector2[] mesaCentersXZ,
            float mesaRadius,
            float mesaFalloff)
        {
            var mask = new float[resolution, resolution];
            float half = mapSize * 0.5f;

            for (int z = 0; z < resolution; z++)
            {
                float worldZ = -half + (float)z / (resolution - 1) * mapSize;
                for (int x = 0; x < resolution; x++)
                {
                    float worldX = -half + (float)x / (resolution - 1) * mapSize;
                    mask[z, x] = MesaStrength(worldX, worldZ, mesaCentersXZ, mesaRadius, mesaFalloff);
                }
            }

            return mask;
        }

        // A simple thermal-erosion-style relaxation: repeatedly moves a
        // fraction of the height difference between each cell and its 4
        // neighbours from the higher to the lower one wherever the slope
        // exceeds maxSlopeDegrees, so no patch of the bake - not just the
        // area a specific carve touches - exceeds the NavMesh's own
        // walkable slope limit (docs/SKIRMISH_MAP_SPEC.md rule 5). A stock
        // Vista template's raw noise can exceed that limit in a random
        // patch anywhere on the map; this is the general safety net for
        // that, run after any feature-specific carve.
        public static float[,] LimitSlope(float[,] heights01, float mapSize, float heightScaleWorld, float maxSlopeDegrees, int iterations)
        {
            int rz = heights01.GetLength(0);
            int rx = heights01.GetLength(1);
            float scale = Mathf.Max(heightScaleWorld, 0.0001f);
            float cellSize = mapSize / Mathf.Max(rx - 1, 1);
            float maxDelta = Mathf.Tan(maxSlopeDegrees * Mathf.Deg2Rad) * cellSize;

            var world = new float[rz, rx];
            for (int z = 0; z < rz; z++)
            {
                for (int x = 0; x < rx; x++)
                {
                    world[z, x] = heights01[z, x] * scale;
                }
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int z = 0; z < rz; z++)
                {
                    for (int x = 0; x < rx; x++)
                    {
                        RelaxTowardNeighbor(world, z, x, z, x - 1, maxDelta);
                        RelaxTowardNeighbor(world, z, x, z, x + 1, maxDelta);
                        RelaxTowardNeighbor(world, z, x, z - 1, x, maxDelta);
                        RelaxTowardNeighbor(world, z, x, z + 1, x, maxDelta);
                    }
                }
            }

            var result = new float[rz, rx];
            for (int z = 0; z < rz; z++)
            {
                for (int x = 0; x < rx; x++)
                {
                    result[z, x] = Mathf.Clamp01(world[z, x] / scale);
                }
            }

            return result;
        }

        private static void RelaxTowardNeighbor(float[,] world, int z, int x, int nz, int nx, float maxDelta)
        {
            int rz = world.GetLength(0);
            int rx = world.GetLength(1);
            if (nz < 0 || nz >= rz || nx < 0 || nx >= rx)
            {
                return;
            }

            float delta = world[z, x] - world[nz, nx];
            if (delta > maxDelta)
            {
                float move = (delta - maxDelta) * 0.5f;
                world[z, x] -= move;
                world[nz, nx] += move;
            }
        }

        // Step-quantizes heights (a "terraced" look): each cell is pulled
        // toward the nearest multiple of stepSizeNormalized, by strength
        // (0 = untouched, 1 = fully quantized).
        public static float[,] ApplyTerracing(float[,] heights01, float stepSizeNormalized, float strength)
        {
            int rz = heights01.GetLength(0);
            int rx = heights01.GetLength(1);
            var result = new float[rz, rx];
            float step = Mathf.Max(stepSizeNormalized, 0.0001f);

            for (int z = 0; z < rz; z++)
            {
                for (int x = 0; x < rx; x++)
                {
                    float h = heights01[z, x];
                    float quantized = Mathf.Round(h / step) * step;
                    result[z, x] = Mathf.Clamp01(Mathf.Lerp(h, quantized, strength));
                }
            }

            return result;
        }
    }
}

using System;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Finds a natural place for a water body on an already-baked/carved
    // heightmap, instead of hand-picking MapDefinitionData.WaterCenter/
    // WaterHalfExtents by eye the way Divided Riverbed's river was. Scans
    // candidate rectangles across the map's Contested zone (SkirmishMapZones
    // - water belongs there per docs/SKIRMISH_MAP_SPEC.md rule 3, same as
    // large gold/stone) and keeps the one with the lowest maximum sampled
    // height: the flattest, lowest-lying basin a rectangle that size can fit
    // inside, so a discovered water body naturally lands in a valley/basin
    // the terrain carve already produced instead of floating over a
    // hillside.
    //
    // Pure (Vector2/Mathf only), driven by a plain world-height sampler
    // delegate rather than a specific array/heightmap type, so it's directly
    // unit-testable with a synthetic formula and equally reusable against a
    // live Vista bake grid or an already-loaded BakedHeightmap
    // (BakedHeightmap.SampleWorld already has this exact shape) from the
    // Vista bake tool (Assets/Editor/Vista/VistaSpike.cs).
    public static class WaterBasinFinder
    {
        public struct Basin
        {
            public bool Found;
            public Vector2 Center;
            public Vector2 HalfExtents;
            // World height (metres) of the highest point inside the chosen
            // rectangle - how flat/low the winning candidate actually was,
            // useful for a bake-time sanity log even when Found is false.
            public float MaxHeightInRect;
        }

        private const int SamplesPerAxis = 6;

        // heightAtWorld(worldX, worldZ): world height in metres at a point.
        //
        // halfWidth/halfDepth: half-size (world units) of the water
        // rectangle to place. gridSteps: how many candidate centres to test
        // along each axis of the Contested zone - a plain grid search, not
        // an optimizer, since this only ever runs offline in the Editor.
        //
        // maxAllowedHeight: the winning candidate's highest point must sit
        // at or below this (world metres) or Found is false - a genuine
        // "nothing here looks like a real basin" bail-out rather than
        // always forcing water somewhere.
        public static Basin FindLowestFlatRegion(
            Func<float, float, float> heightAtWorld, float mapSize,
            float halfWidth, float halfDepth, int gridSteps, float maxAllowedHeight)
        {
            var best = new Basin { Found = false, MaxHeightInRect = float.MaxValue };
            if (heightAtWorld == null || gridSteps < 1 || halfWidth <= 0f || halfDepth <= 0f)
            {
                return best;
            }

            float contestedHalf = SkirmishMapZones.ContestedWidth(mapSize) * 0.5f;
            float searchHalf = Mathf.Max(0f, contestedHalf - Mathf.Max(halfWidth, halfDepth));

            for (int iz = 0; iz < gridSteps; iz++)
            {
                float cz = GridLerp(-searchHalf, searchHalf, iz, gridSteps);
                for (int ix = 0; ix < gridSteps; ix++)
                {
                    float cx = GridLerp(-searchHalf, searchHalf, ix, gridSteps);

                    float maxHeight = MaxHeightInRect(heightAtWorld, cx, cz, halfWidth, halfDepth);
                    if (maxHeight < best.MaxHeightInRect)
                    {
                        best.MaxHeightInRect = maxHeight;
                        best.Center = new Vector2(cx, cz);
                        best.Found = true;
                    }
                }
            }

            if (best.Found && best.MaxHeightInRect <= maxAllowedHeight)
            {
                best.HalfExtents = new Vector2(halfWidth, halfDepth);
                return best;
            }

            return new Basin { Found = false, MaxHeightInRect = best.MaxHeightInRect };
        }

        private static float GridLerp(float min, float max, int step, int steps)
        {
            return steps <= 1 ? (min + max) * 0.5f : Mathf.Lerp(min, max, (float)step / (steps - 1));
        }

        private static float MaxHeightInRect(Func<float, float, float> heightAtWorld, float cx, float cz, float halfWidth, float halfDepth)
        {
            float max = float.MinValue;
            for (int sz = 0; sz < SamplesPerAxis; sz++)
            {
                float wz = cz + GridLerp(-halfDepth, halfDepth, sz, SamplesPerAxis);
                for (int sx = 0; sx < SamplesPerAxis; sx++)
                {
                    float wx = cx + GridLerp(-halfWidth, halfWidth, sx, SamplesPerAxis);
                    float worldHeight = heightAtWorld(wx, wz);
                    if (worldHeight > max)
                    {
                        max = worldHeight;
                    }
                }
            }

            return max;
        }
    }
}

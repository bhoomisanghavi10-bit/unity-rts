using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Scatters ground clutter over a Terrain as GPU-instanced meshes (drawn by
    // TerrainClutterRenderer; no colliders - so it never touches the NavMesh or
    // gameplay). Density comes from the terrain's
    // alphamap weights: grass tufts follow grass/dirt, rocks are sparse on
    // grass/dirt, and pebbles follow the wet pebble band along the
    // waterline. The density grid matches the alphamap grid 1:1, so the maps
    // index directly. Purely visual and deterministic (cell-hash noise), so
    // it's safe for lockstep multiplayer.
    public static class TerrainClutter
    {
        private const string ClutterSetPath = "Terrain/ClutterSet";
        public const float DrawDistance = 70f;

        // Layer indices of the terrain alphamap (see ProceduralTerrain).
        private const int GrassLayer = 0;
        private const int DirtLayer = 1;
        private const int RockLayer = 2;
        private const int SandLayer = 3;
        private const int PebbleLayer = 4;

        // Patch size in detail cells; each (patch, clutter type) pair is one
        // culled, instanced draw.
        private const int PatchCells = 16;

        public static void Apply(Terrain terrain, TerrainData data, float[,,] alpha, int resolution)
        {
            // Unity's built-in detail system isn't used (see
            // TerrainClutterRenderer); make sure no stale prototypes linger.
            data.detailPrototypes = new DetailPrototype[0];

            var renderer = terrain.GetComponent<TerrainClutterRenderer>();
            if (renderer == null)
            {
                renderer = terrain.gameObject.AddComponent<TerrainClutterRenderer>();
            }

            renderer.Clear();
            renderer.SetDrawDistance(DrawDistance);

            var set = Resources.Load<TerrainClutterSet>(ClutterSetPath);
            if (set == null || set.entries == null)
            {
                return;
            }

            Vector3 origin = terrain.transform.position;
            float cell = data.size.x / resolution;

            for (int entryIndex = 0; entryIndex < set.entries.Length; entryIndex++)
            {
                TerrainClutterSet.Entry e = set.entries[entryIndex];
                if (e == null || e.prefab == null)
                {
                    continue;
                }

                var mf = e.prefab.GetComponentInChildren<MeshFilter>();
                var mr = e.prefab.GetComponentInChildren<MeshRenderer>();
                if (mf == null || mf.sharedMesh == null || mr == null || mr.sharedMaterial == null)
                {
                    continue;
                }

                int[,] density = BuildLayer(e, alpha, resolution, entryIndex);
                Bounds meshBounds = mf.sharedMesh.bounds;
                float reach = Mathf.Max(meshBounds.extents.x, meshBounds.extents.z) * e.maxScale;
                float tall = meshBounds.size.y * e.maxScale;

                for (int pz = 0; pz < resolution; pz += PatchCells)
                {
                    for (int px = 0; px < resolution; px += PatchCells)
                    {
                        var matrices = new List<Matrix4x4>();
                        bool any = false;
                        Bounds bounds = default;

                        for (int z = pz; z < Mathf.Min(pz + PatchCells, resolution); z++)
                        {
                            for (int x = px; x < Mathf.Min(px + PatchCells, resolution); x++)
                            {
                                int count = density[z, x];
                                for (int k = 0; k < count; k++)
                                {
                                    float jx = Hash(x * 7 + k, z, entryIndex + 101);
                                    float jz = Hash(x, z * 7 + k, entryIndex + 211);
                                    float wx = origin.x + (x + jx) * cell;
                                    float wz = origin.z + (z + jz) * cell;
                                    float wy = origin.y + terrain.SampleHeight(new Vector3(wx, 0f, wz));
                                    float yaw = Hash(x + k, z + k, entryIndex + 307) * 360f;
                                    float scale = Mathf.Lerp(e.minScale, e.maxScale, Hash(x * 3 + k, z * 5, entryIndex + 409));
                                    var pos = new Vector3(wx, wy, wz);
                                    matrices.Add(Matrix4x4.TRS(pos, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale));

                                    var b = new Bounds(pos + Vector3.up * (tall * 0.5f), new Vector3(reach * 2f, tall, reach * 2f));
                                    if (!any)
                                    {
                                        bounds = b;
                                        any = true;
                                    }
                                    else
                                    {
                                        bounds.Encapsulate(b);
                                    }
                                }
                            }
                        }

                        if (any)
                        {
                            renderer.AddBatch(mf.sharedMesh, mr.sharedMaterial, matrices, bounds);
                        }
                    }
                }
            }
        }

        private static int[,] BuildLayer(TerrainClutterSet.Entry e, float[,,] alpha, int n, int salt)
        {
            var layer = new int[n, n];
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    float grass = alpha[z, x, GrassLayer];
                    float dirt = alpha[z, x, DirtLayer];
                    float pebble = alpha[z, x, PebbleLayer];
                    float sand = alpha[z, x, SandLayer];
                    float rock = alpha[z, x, RockLayer];
                    float h = Hash(x, z, salt);

                    switch (e.kind)
                    {
                        case TerrainClutterSet.ClutterKind.Grass:
                        {
                            float g = grass + dirt * 0.45f;
                            g *= 1f - Mathf.Clamp01(sand + pebble + rock);
                            if (g < 0.25f)
                            {
                                break;
                            }

                            // Patchy: a broad noise decides where tufts are
                            // dense, so lawns aren't uniform carpet.
                            float patch = 0.05f + 0.95f * Mathf.PerlinNoise(x * 0.09f + salt * 17f, z * 0.09f + 3.1f);
                            float amount = e.density * Mathf.Pow(g, 1.5f) * patch;
                            int whole = Mathf.FloorToInt(amount);
                            layer[z, x] = whole + (h < amount - whole ? 1 : 0);
                            break;
                        }
                        case TerrainClutterSet.ClutterKind.Rock:
                        {
                            float chance = 0.012f * e.density * (grass + dirt) * (1f - sand - pebble);
                            layer[z, x] = h < chance ? 1 : 0;
                            break;
                        }
                        case TerrainClutterSet.ClutterKind.Pebble:
                        {
                            float chance = 0.35f * e.density * pebble * pebble;
                            layer[z, x] = h < chance ? 1 + (h < chance * 0.3f ? 1 : 0) : 0;
                            break;
                        }
                    }
                }
            }

            return layer;
        }

        // Cheap deterministic per-cell hash in [0,1).
        private static float Hash(int x, int z, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(z * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}

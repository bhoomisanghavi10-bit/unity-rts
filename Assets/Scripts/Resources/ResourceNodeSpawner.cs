using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.ResourceGathering
{
    // Milestone-4/8 placeholder: scatters resource nodes around the map so
    // gathering is testable. Counts/radius/seed are overridden per-match
    // from the selected MapDefinition (see ApplyMapDefinition) - both
    // built-in maps use a -1 seed, meaning a fresh random layout every
    // match (item 44) rather than the old permanently-fixed one.
    public class ResourceNodeSpawner : MonoBehaviour
    {
        [SerializeField] private int treeCount = 8;
        [SerializeField] private int farmCount = 5;
        [SerializeField] private int goldCount = 5;
        [SerializeField] private int stoneCount = 5;
        [SerializeField] private int fruitBushCount = 6;
        [SerializeField] private float minRadius = 6f;
        [SerializeField] private float maxRadius = 16f;
        [SerializeField] private float startingAmount = 40f;
        [SerializeField] private float fruitBushAmount = 25f;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private int fishCount;
        [SerializeField] private float fishAmount = 60f;
        [SerializeField] private int relicCount = 5;

        [Header("Terrain foliage (Nature Renderer 6 / Terrain trees + details)")]
        [SerializeField] private Terrain targetTerrain;
        public int treePrototypeIndex = 0;
        public int grassDetailLayerIndex = 0;
        [Range(0, 12)] public int treesPerForestTile = 3;
        [Range(0, 16)] public int grassDensityPerTile = 6;
        [Range(0.05f, 0.95f)] [SerializeField] private float forestThreshold = 0.58f;
        [SerializeField] private float forestNoiseScale = 0.075f;

        // The fixed medium-map matrix: 158 x 158 tiles, 1 unit = 1 tile.
        [Header("Gatherable terrain trees")]
        [Tooltip("Terrain trees become gatherable Wood via invisible proxy nodes; the old model trees (treeCount) are then skipped.")]
        [SerializeField] private bool useTerrainTreesForWood = true;
        [Tooltip("Proxy cell size in tiles: 1 = one node per tile, 2 = per 2x2 tiles, 0.5 = ~4 nodes per tile.")]
        [Range(0.5f, 4f)] [SerializeField] private float proxyCellTiles = 2f;
        [SerializeField] private float woodPerTree = 10f;

        private const int GridSize = 158;
        private const float TileJitter = 0.35f;
        private const float HomeClearRadius = 12f;

        private enum TileType { Empty, Plains, Forest }

        // Item 51: a locally-owned DeterministicRandom rather than the
        // shared DeterministicRandom.Match singleton - this runs in Start(),
        // before SimClock's first tick reseeds Match for the new match, so
        // sharing it here would spawn resources against whatever seed was
        // left over from the previous match (or the app-launch default).
        // Same InitState/seed semantics as before (-1 = fresh layout every
        // match), just off the deterministic generator instead of Unity's
        // global Random state.
        private DeterministicRandom _rng;

        private void Start()
        {
            ApplyMapDefinition();

            _rng = new DeterministicRandom(randomSeed == -1 ? System.Environment.TickCount : randomSeed);

            bool forestWood = PopulateTerrainFoliage();

            for (int i = 0; !forestWood && i < treeCount; i++)
            {
                SpawnTree(RandomPointInRing());
            }

            for (int i = 0; i < farmCount; i++)
            {
                SpawnFarmland(RandomPointInRing());
            }

            for (int i = 0; i < goldCount; i++)
            {
                SpawnGoldMine(RandomPointInRing());
            }

            for (int i = 0; i < stoneCount; i++)
            {
                SpawnStoneQuarry(RandomPointInRing());
            }

            for (int i = 0; i < fruitBushCount; i++)
            {
                SpawnFruitBush(RandomPointInRing());
            }

            for (int i = 0; i < fishCount; i++)
            {
                SpawnFish(RandomPointInWater());
            }

            for (int i = 0; i < relicCount; i++)
            {
                SpawnRelic(RandomPointInRing());
            }
        }

        // Single coordinated pass over the 158x158 tile matrix. Forest tiles
        // become TreeInstances, Plains tiles feed the detail (grass) density
        // layer; both are pushed back into the TerrainData at the end and
        // flushed so Nature Renderer 6 picks them up. Managed collections
        // only (List / int[,]) - nothing native is allocated, so there is
        // nothing to dispose. Trees here are visual; gatherable Wood still
        // comes from SpawnTree above.
        private bool PopulateTerrainFoliage()
        {
            Terrain terrain = targetTerrain != null ? targetTerrain : FindFirstObjectByType<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                return false;
            }

            TerrainData data = terrain.terrainData;
            MapDefinitionData map = MapRegistry.Current;
            float worldSize = Mathf.Max(data.size.x, 0.0001f);
            float tileWorld = worldSize / GridSize;
            float halfWorld = worldSize * 0.5f;

            // Normalised terrain space is 0..1 across the whole 158x158 footprint.
            float invGrid = 1f / GridSize;

            bool haveTrees = data.treePrototypes != null && data.treePrototypes.Length > 0 && treesPerForestTile > 0;
            int treeProto = haveTrees ? Mathf.Clamp(treePrototypeIndex, 0, data.treePrototypes.Length - 1) : 0;
            bool treesPlaced = false;
            bool haveGrass = data.detailPrototypes != null && data.detailPrototypes.Length > 0 && grassDensityPerTile > 0;
            int grassLayer = haveGrass ? Mathf.Clamp(grassDetailLayerIndex, 0, data.detailPrototypes.Length - 1) : 0;

            if (!haveTrees && !haveGrass)
            {
                Debug.LogWarning("ResourceNodeSpawner: terrain has no tree/detail prototypes (or counts are 0) - skipping foliage.");
                return false;
            }

            // Classify every tile once; both consumers read this grid.
            var tiles = new TileType[GridSize, GridSize];
            float noiseOffset = _rng.Range(0f, 1000f);
            for (int tz = 0; tz < GridSize; tz++)
            {
                for (int tx = 0; tx < GridSize; tx++)
                {
                    float wx = -halfWorld + (tx + 0.5f) * tileWorld;
                    float wz = -halfWorld + (tz + 0.5f) * tileWorld;
                    tiles[tx, tz] = ClassifyTile(map, wx, wz, worldSize, noiseOffset);
                }
            }

            if (haveTrees)
            {
                var trees = new List<TreeInstance>(GridSize * GridSize / 4 * treesPerForestTile);
                float cellTiles = Mathf.Max(0.5f, proxyCellTiles);
                var cells = new Dictionary<int, List<int>>();
                for (int tz = 0; tz < GridSize; tz++)
                {
                    for (int tx = 0; tx < GridSize; tx++)
                    {
                        if (tiles[tx, tz] != TileType.Forest)
                        {
                            continue;
                        }

                        for (int i = 0; i < treesPerForestTile; i++)
                        {
                            float jx = _rng.Range(-TileJitter, TileJitter);
                            float jz = _rng.Range(-TileJitter, TileJitter);
                            float scale = _rng.Range(0.85f, 1.2f);
                            int cx = Mathf.FloorToInt((tx + 0.5f + jx) / cellTiles);
                            int cz = Mathf.FloorToInt((tz + 0.5f + jz) / cellTiles);
                            int cellKey = cx * 4096 + cz;
                            if (!cells.TryGetValue(cellKey, out List<int> members))
                            {
                                members = new List<int>();
                                cells[cellKey] = members;
                            }
                            members.Add(trees.Count);
                            trees.Add(new TreeInstance
                            {
                                prototypeIndex = treeProto,
                                position = new Vector3(
                                    Mathf.Clamp01((tx + 0.5f + jx) * invGrid),
                                    0f,
                                    Mathf.Clamp01((tz + 0.5f + jz) * invGrid)),
                                rotation = _rng.Range(0f, Mathf.PI * 2f),
                                widthScale = scale * _rng.Range(0.9f, 1.1f),
                                heightScale = scale,
                                color = Color.white,
                                lightmapColor = Color.white,
                            });
                        }
                    }
                }

                // Overrides terrainData.treeInstances; snaps Y to the heightmap.
                data.SetTreeInstances(trees.ToArray(), true);
                treesPlaced = trees.Count > 0;

                if (useTerrainTreesForWood && treesPlaced)
                {
                    BuildForestProxies(terrain, trees, cells, worldSize);
                }
            }

            if (haveGrass)
            {
                int res = data.detailResolution;
                if (res <= 0)
                {
                    data.SetDetailResolution(512, 16);
                    res = data.detailResolution;
                }

                var density = new int[res, res];
                int maxDensity = Mathf.Clamp(grassDensityPerTile, 0, 255);
                for (int dy = 0; dy < res; dy++)
                {
                    int tz = Mathf.Clamp((int)((dy + 0.5f) / res * GridSize), 0, GridSize - 1);
                    for (int dx = 0; dx < res; dx++)
                    {
                        int tx = Mathf.Clamp((int)((dx + 0.5f) / res * GridSize), 0, GridSize - 1);
                        if (tiles[tx, tz] == TileType.Plains)
                        {
                            // Slight per-cell thinning so tiles don't read as flat blocks.
                            density[dy, dx] = Mathf.Clamp(Mathf.RoundToInt(maxDensity * _rng.Range(0.6f, 1f)), 0, 255);
                        }
                    }
                }

                data.SetDetailLayer(0, 0, grassLayer, density);
            }

            terrain.Flush();
            return treesPlaced && useTerrainTreesForWood;
        }

        // One invisible Wood node per proxy cell. The node owns a box sized to
        // its member trees (so clicking the forest selects it) and thins the
        // rendered trees as it is harvested (see TerrainForest).
        private void BuildForestProxies(Terrain terrain, List<TreeInstance> trees, Dictionary<int, List<int>> cells, float worldSize)
        {
            TerrainForest forest = GetComponent<TerrainForest>();
            if (forest == null)
            {
                forest = gameObject.AddComponent<TerrainForest>();
            }
            forest.Initialize(terrain, trees);

            Vector3 origin = terrain.transform.position;
            foreach (KeyValuePair<int, List<int>> cell in cells)
            {
                List<int> members = cell.Value;
                float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                for (int i = 0; i < members.Count; i++)
                {
                    Vector3 p = trees[members[i]].position;
                    float wx = origin.x + p.x * worldSize;
                    float wz = origin.z + p.z * worldSize;
                    minX = Mathf.Min(minX, wx); maxX = Mathf.Max(maxX, wx);
                    minZ = Mathf.Min(minZ, wz); maxZ = Mathf.Max(maxZ, wz);
                }

                float cx = (minX + maxX) * 0.5f;
                float cz = (minZ + maxZ) * 0.5f;
                float groundY = terrain.SampleHeight(new Vector3(cx, 0f, cz)) + origin.y;

                var go = new GameObject("ForestNode");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(cx, groundY, cz);

                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 1.25f, 0f);
                box.size = new Vector3(Mathf.Max(maxX - minX + 1f, 1.5f), 2.5f, Mathf.Max(maxZ - minZ + 1f, 1.5f));

                var node = go.AddComponent<ResourceNode>();
                node.Configure(ResourceType.Wood, members.Count * woodPerTree);
                go.AddComponent<ForestProxy>().Initialize(forest, node, members, _rng);
            }
        }

        private TileType ClassifyTile(MapDefinitionData map, float wx, float wz, float worldSize, float noiseOffset)
        {
            if (!SkirmishMapZones.IsSpawnable(new Vector3(wx, 0f, wz), worldSize))
            {
                return TileType.Empty;
            }

            // Keep town centres clear.
            if (IsNear(map.PlayerTownCenter, wx, wz) || IsNear(map.EnemyTownCenter, wx, wz) || IsNear(map.Enemy2TownCenter, wx, wz))
            {
                return TileType.Plains;
            }

            // Water rectangle (plus shoreline wobble) is not ground.
            if (map.WaterHalfExtents.x > 0f && map.WaterHalfExtents.z > 0f &&
                Mathf.Abs(wx - map.WaterCenter.x) <= map.WaterHalfExtents.x + 1f &&
                Mathf.Abs(wz - map.WaterCenter.z) <= map.WaterHalfExtents.z)
            {
                return TileType.Empty;
            }

            // Clearing: a guaranteed-clear lane between every pair of
            // active town centres, regardless of the forest noise below.
            if (map.ForestLaneWidth > 0f)
            {
                float laneDistance = ForestLanes.DistanceToNearestLane(new Vector3(wx, 0f, wz), new[]
                {
                    map.PlayerTownCenter, map.EnemyTownCenter,
                    map.PlayerTownCenter, map.Enemy2TownCenter,
                    map.EnemyTownCenter, map.Enemy2TownCenter,
                });
                if (laneDistance <= map.ForestLaneWidth)
                {
                    return TileType.Plains;
                }
            }

            float n = Mathf.PerlinNoise(wx * forestNoiseScale + noiseOffset, wz * forestNoiseScale + noiseOffset);
            return n > forestThreshold ? TileType.Forest : TileType.Plains;
        }

        private static bool IsNear(Vector3 center, float wx, float wz)
        {
            float dx = wx - center.x;
            float dz = wz - center.z;
            return dx * dx + dz * dz < HomeClearRadius * HomeClearRadius;
        }

        // Item 44: same pattern as ProceduralGround.ApplyMapDefinition -
        // pulls counts/radius/seed from the selected map, overriding this
        // component's Inspector defaults. Also the fix for "basic random
        // resource placement": both built-in maps set ResourceSeed to -1,
        // so layout now actually varies match to match instead of the old
        // permanently-fixed seed 12345 (a map can still pin a specific
        // seed later if a reproducible layout is ever wanted again).
        private void ApplyMapDefinition()
        {
            MapDefinitionData map = MapRegistry.Current;
            treeCount = map.TreeCount;
            farmCount = map.FarmCount;
            goldCount = map.GoldCount;
            stoneCount = map.StoneCount;
            fruitBushCount = map.FruitBushCount;
            minRadius = map.ResourceMinRadius;
            maxRadius = map.ResourceMaxRadius;
            randomSeed = map.ResourceSeed;
            fishCount = map.FishCount;
            relicCount = map.RelicCount;
            forestThreshold = map.ForestThreshold;
        }

        private Vector3 RandomPointInRing()
        {
            Vector2 direction = _rng.InsideUnitCircleNormalized();
            float radius = _rng.Range(minRadius, maxRadius);
            return new Vector3(direction.x * radius, 0f, direction.y * radius);
        }

        // Item 49: a random point inside the current map's water
        // rectangle - only ever called fishCount times, itself only ever
        // non-zero on a map that actually has water (see
        // MapDefinitionData.FishCount), so this never runs on
        // RiverValley/Highlands.
        private Vector3 RandomPointInWater()
        {
            MapDefinitionData map = MapRegistry.Current;
            float x = map.WaterCenter.x + _rng.Range(-(map.WaterHalfExtents.x - WaterProximity.MaxShoreInset - 1f), map.WaterHalfExtents.x - WaterProximity.MaxShoreInset - 1f);
            float z = map.WaterCenter.z + _rng.Range(-map.WaterHalfExtents.z, map.WaterHalfExtents.z);
            return new Vector3(x, 0f, z);
        }

        private void SpawnTree(Vector3 position)
        {
            GameObject go = EnvironmentPropFactory.TrySpawn("Trees", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Tree";
                go.transform.position = position + Vector3.up;
                go.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                Colorize(go, new Color(0.25f, 0.45f, 0.2f));
            }

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Wood, startingAmount);
        }

        private void SpawnFarmland(Vector3 position)
        {
            GameObject go = EnvironmentPropFactory.TrySpawn("Farmland", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Farmland";
                go.transform.position = position + Vector3.up * 0.1f;
                go.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
                Colorize(go, new Color(0.75f, 0.65f, 0.25f));
            }

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Food, startingAmount);
        }

        private void SpawnGoldMine(Vector3 position)
        {
            GameObject go = EnvironmentPropFactory.TrySpawn("GoldMine", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "GoldMine";
                go.transform.position = position + Vector3.up * 0.5f;
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                Colorize(go, new Color(0.85f, 0.7f, 0.15f));
            }

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Gold, startingAmount);
        }

        private void SpawnStoneQuarry(Vector3 position)
        {
            GameObject go = EnvironmentPropFactory.TrySpawn("StoneQuarry", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "StoneQuarry";
                go.transform.position = position + Vector3.up * 0.4f;
                go.transform.localScale = new Vector3(1.4f, 0.8f, 1.4f);
                Colorize(go, new Color(0.55f, 0.55f, 0.55f));
            }

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Stone, startingAmount);
        }

        private void SpawnFruitBush(Vector3 position)
        {
            GameObject go = EnvironmentPropFactory.TrySpawn("Bushes", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "FruitBush";
                go.transform.position = position + Vector3.up * 0.3f;
                go.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
                Colorize(go, new Color(0.2f, 0.5f, 0.15f));
            }

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Food, fruitBushAmount);
        }

        // Wave 6 item 35: a Relic isn't a ResourceNode (no depletion, no
        // ResourceType, a held/unheld pick-up state instead - see Relic.cs/
        // RelicCarrier.cs), so this attaches a Relic component rather than
        // calling ResourceNode.Configure like every SpawnX above it. No
        // dedicated "Relics" art category exists yet - flagging per the
        // flag-asset-needs convention - so this always falls through to the
        // primitive fallback for now.
        private void SpawnRelic(Vector3 position)
        {
            GameObject go = EnvironmentPropFactory.TrySpawn("Relics", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Relic";
                go.transform.position = position + Vector3.up * 0.6f;
                go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                Colorize(go, new Color(0.9f, 0.8f, 0.3f));
            }

            go.AddComponent<Relic>();
        }

        // Item 49: unlike every other node, deliberately NOT run through
        // ResolveGroundPoint - the water region is a literal hole in the
        // ground mesh (see ProceduralGround), so there's no terrain height
        // to raycast against there. Floats at a fixed height near the
        // water surface instead.
        private void SpawnFish(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Fish";
            go.transform.position = position + Vector3.up * 0.3f;
            go.transform.localScale = new Vector3(0.5f, 0.2f, 0.9f);
            Colorize(go, new Color(0.55f, 0.6f, 0.65f));

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Food, fishAmount);
        }

        // Only used for the real-model path (EnvironmentPropFactory): a
        // detailed imported tree/bush mesh makes flat-Y placement mismatches
        // obvious the same way it did for buildings and human models
        // earlier this project, so this resolves the actual terrain height
        // before spawning one. The primitive fallback keeps its existing
        // flat-position behavior unchanged - untouched here deliberately,
        // since it isn't the reported problem and primitives hide the
        // mismatch anyway.
        private static Vector3 ResolveGroundPoint(Vector3 xzPoint)
        {
            return GroundReference.TryGetHeight(xzPoint, out float height)
                ? new Vector3(xzPoint.x, height, xzPoint.z)
                : xzPoint;
        }

        private static void Colorize(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(color);
        }
    }
}

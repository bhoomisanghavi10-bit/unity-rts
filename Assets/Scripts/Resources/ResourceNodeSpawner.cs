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

            for (int i = 0; i < treeCount; i++)
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
            float x = map.WaterCenter.x + _rng.Range(-map.WaterHalfExtents.x, map.WaterHalfExtents.x);
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

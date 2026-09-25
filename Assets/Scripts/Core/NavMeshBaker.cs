using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Core
{
    // Bakes a NavMesh at runtime over the ground only (not trees/farmland/
    // buildings, which spawn separately and would otherwise need to exist
    // before baking) using the legacy NavMeshBuilder runtime API, so
    // pathfinding works without an Editor-baked NavMesh asset. Must run
    // after ProceduralGround exists (Awake) and before any units are spawned
    // (UnitSpawner), hence the execution order.
    [DefaultExecutionOrder(-50)]
    public class NavMeshBaker : MonoBehaviour
    {
        [SerializeField] private Vector3 boundsSize = new Vector3(44f, 10f, 44f);
        [SerializeField] private string groundObjectName = "Ground";

        private void Start()
        {
            Bake();
        }

        // Phase 5 gap-close (2026-08-25): this component is always-active
        // from scene load, so the item-44 comment below ("bounds must
        // match the selected map") was never actually true in practice -
        // Start() fires before CivPicker's map choice exists, so
        // MapRegistry.Current was still the RiverValley default every
        // time, on every map. Same fix as ProceduralGround.Rebuild():
        // CivilizationSetup.BeginMatchCore calls this directly right after
        // ProceduralGround.Rebuild() (this bakes over that mesh's
        // collider, via CollectSources) and before activating any gated
        // content that needs real pathfinding (UnitSpawner, AiController).
        // Clears the stale scene-load bake first - AddNavMeshData doesn't
        // replace prior data, it stacks another NavMeshData on top of it.
        public void RebuildNavMesh()
        {
            NavMesh.RemoveAllNavMeshData();
            Bake();
        }

        // One thin not-walkable strip per metre of z, each following the
        // wobbling waterline (WaterProximity.ShoreInsetAt) so units are
        // blocked exactly where the terrain meets the water. Sides at the
        // map edge aren't wobbled (matches ProceduralTerrain). Uses
        // ModifierBox: a plain Box source is geometry and doesn't override
        // the area.
        private static void AddWaterModifiers(List<NavMeshBuildSource> sources)
        {
            MapDefinitionData map = MapRegistry.Current;
            float half = map.GroundSize * 0.5f;
            float xMin = map.WaterCenter.x - map.WaterHalfExtents.x;
            float xMax = map.WaterCenter.x + map.WaterHalfExtents.x;
            float zMin = map.WaterCenter.z - map.WaterHalfExtents.z;
            float zMax = map.WaterCenter.z + map.WaterHalfExtents.z;
            bool wobbleWest = xMin > -half + 0.01f;
            bool wobbleEast = xMax < half - 0.01f;

            const float strip = 1f;
            for (float z0 = zMin; z0 < zMax; z0 += strip)
            {
                float depth = Mathf.Min(strip, zMax - z0);
                float zc = z0 + depth * 0.5f;
                float wobble = WaterProximity.ShoreInsetAt(zc);
                float x0 = xMin + (wobbleWest ? wobble : 0f);
                float x1 = xMax - (wobbleEast ? wobble : 0f);
                if (x1 <= x0)
                {
                    continue;
                }

                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.ModifierBox,
                    size = new Vector3(x1 - x0, 20f, depth + 0.05f),
                    transform = Matrix4x4.TRS(new Vector3((x0 + x1) * 0.5f, 0f, zc), Quaternion.identity, Vector3.one),
                    area = 1,
                });
            }
        }

        // Divided Riverbed variant of AddWaterModifiers: strips run along X
        // (the river's length) instead of Z, covering the full water
        // depth (zMin..zMax) each - and any strip inside a ford (see
        // RiverFords.FordFactor) is skipped entirely, leaving that stretch
        // of the water rectangle with no not-walkable geometry at all, so
        // the NavMesh connects straight through it. Only used when the
        // map actually defines fords; every other map keeps using
        // AddWaterModifiers unchanged.
        private static void AddRiverFordModifiers(List<NavMeshBuildSource> sources)
        {
            MapDefinitionData map = MapRegistry.Current;
            float half = map.GroundSize * 0.5f;
            float xMin = Mathf.Max(map.WaterCenter.x - map.WaterHalfExtents.x, -half);
            float xMax = Mathf.Min(map.WaterCenter.x + map.WaterHalfExtents.x, half);
            float zMin = map.WaterCenter.z - map.WaterHalfExtents.z;
            float zMax = map.WaterCenter.z + map.WaterHalfExtents.z;
            float depth = zMax - zMin;
            if (depth <= 0f)
            {
                return;
            }

            const float strip = 1f;
            for (float x0 = xMin; x0 < xMax; x0 += strip)
            {
                float width = Mathf.Min(strip, xMax - x0);
                float xc = x0 + width * 0.5f;
                float fordFactor = RiverFords.FordFactor(xc, map.FordCentersX, map.FordHalfWidth, RiverFords.DefaultFalloff);
                if (fordFactor >= RiverFords.NavMeshWalkableThreshold)
                {
                    continue;
                }

                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.ModifierBox,
                    size = new Vector3(width + 0.05f, 20f, depth),
                    transform = Matrix4x4.TRS(new Vector3(xc, 0f, (zMin + zMax) * 0.5f), Quaternion.identity, Vector3.one),
                    area = 1,
                });
            }
        }

        private void Bake()
        {
            // Item 44: bounds must match the selected map's ground extent
            // or a larger map (e.g. Highlands) would bake a NavMesh that
            // doesn't cover its own edges.
            boundsSize = MapRegistry.Current.NavMeshBoundsSize;

            Transform groundRoot = GameObject.Find(groundObjectName)?.transform;

            var bounds = new Bounds(Vector3.zero, boundsSize);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                groundRoot, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);

            // The water bed is real terrain (sloped, sunken), so keep it
            // unwalkable with a Not Walkable (area 1) box over the water
            // rectangle - units can't wade. Its edge is the waterline.
            if (WaterProximity.HasWater)
            {
                if (MapRegistry.Current.FordCentersX != null && MapRegistry.Current.FordCentersX.Length > 0)
                {
                    AddRiverFordModifiers(sources);
                }
                else
                {
                    AddWaterModifiers(sources);
                }
            }

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                settings, sources, bounds, Vector3.zero, Quaternion.identity);

            NavMesh.AddNavMeshData(data);
        }
    }
}

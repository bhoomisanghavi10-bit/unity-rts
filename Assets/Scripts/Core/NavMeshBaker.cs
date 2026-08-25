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

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                settings, sources, bounds, Vector3.zero, Quaternion.identity);

            NavMesh.AddNavMeshData(data);
        }
    }
}

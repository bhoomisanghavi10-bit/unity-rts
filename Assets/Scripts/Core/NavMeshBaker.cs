using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Core
{
    // Bakes a NavMesh at runtime over the scene's static colliders (the
    // procedurally-built ground) using the legacy NavMeshBuilder runtime API,
    // so pathfinding works without an Editor-baked NavMesh asset. Must run
    // after ProceduralGround exists (Awake) and before any units are spawned
    // (UnitSpawner), hence the execution order.
    [DefaultExecutionOrder(-50)]
    public class NavMeshBaker : MonoBehaviour
    {
        [SerializeField] private Vector3 boundsSize = new Vector3(44f, 10f, 44f);

        private void Start()
        {
            Bake();
        }

        private void Bake()
        {
            var bounds = new Bounds(Vector3.zero, boundsSize);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                settings, sources, bounds, Vector3.zero, Quaternion.identity);

            NavMesh.AddNavMeshData(data);
        }
    }
}

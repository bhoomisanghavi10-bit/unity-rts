using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Buildings
{
    // AoE-style footprint rules (ad hoc roadmap request, 2026-08-28): every
    // building except Wall/Gate occupies a square footprint sized in whole
    // tiles (World()/Square() below), and - once built or under
    // construction - carves a NavMeshObstacle shrunk by Margin on every
    // side, so a thin strip along the building's real edge stays walkable
    // instead of the obstacle covering the full tile. Wall/Gate keep their
    // own pre-existing full-footprint (no margin) NavMeshObstacle, added
    // directly in WallFactory/GateFactory - untouched by this system, only
    // tagged here (carveObstacle:false) so the placement-legality overlap
    // check below still sees their real (non-square) shape.
    public static class BuildingFootprint
    {
        public const float TileWorldSize = 1f;
        public const float Margin = 0.5f;

        public const int HouseTiles = 2;
        public const int FarmTiles = 2;
        public const int TowerTiles = 2;
        public const int MarketTiles = 3;
        public const int BarracksTiles = 4;
        public const int TownCenterTiles = 6;

        private const float ObstacleHeight = 10f;
        internal const float MinObstacleSize = 0.1f;

        // Only reached for a Building with no BuildingFootprintTag at all -
        // shouldn't happen for anything spawned through a *Factory (every
        // one calls Attach below), but fails safe with the same generic
        // clearance every IsClear caller used before this system existed,
        // rather than silently ignoring an untagged building in the
        // overlap check.
        private const float FallbackClearance = 3f;

        public static float World(int tiles) => tiles * TileWorldSize;

        public static Vector2 Square(int tiles)
        {
            float world = World(tiles);
            return new Vector2(world, world);
        }

        // Tags root with its real-world (X,Z) footprint - every Factory
        // calls this, Wall/Gate included, so IsClear below can compute a
        // real overlap for every building regardless of shape. When
        // carveObstacle is true, also adds a NavMeshObstacle sized
        // footprint-minus-Margin on both axes (the "thin walkable edge"
        // every non-exempt building gets) with a generous fixed height -
        // carving only cares about horizontal overlap with the walkable
        // NavMesh surface, not matching the building's actual rendered
        // height.
        public static void Attach(GameObject root, Vector2 footprint, bool carveObstacle)
        {
            var tag = root.AddComponent<BuildingFootprintTag>();
            tag.Size = footprint;
            tag.CarvesObstacle = carveObstacle;

            if (!carveObstacle)
            {
                return;
            }

            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(
                Mathf.Max(MinObstacleSize, footprint.x - Margin * 2f),
                ObstacleHeight,
                Mathf.Max(MinObstacleSize, footprint.y - Margin * 2f));
            obstacle.carving = true;
        }

        // Square/rectangular AABB overlap against every existing Building's
        // tagged footprint - replaces the old center-distance-circle check
        // now that buildings have real footprints instead of an arbitrary
        // clearance radius. Wall/Gate's OWN placement legality still goes
        // through BuildingPlacer's separate, smaller wallClearance circle
        // check (unchanged) so they can still sit edge-to-edge in a chain;
        // this is only for what a new (non-Wall/Gate) building is allowed
        // to overlap.
        public static bool IsClear(Vector3 point, Vector2 footprint)
        {
            foreach (Building building in Building.All)
            {
                Vector2 otherFootprint = building.TryGetComponent(out BuildingFootprintTag tag)
                    ? tag.Size
                    : new Vector2(FallbackClearance, FallbackClearance);

                float halfWidthSum = (footprint.x + otherFootprint.x) * 0.5f;
                float halfDepthSum = (footprint.y + otherFootprint.y) * 0.5f;
                Vector3 otherPosition = building.transform.position;
                if (Mathf.Abs(point.x - otherPosition.x) < halfWidthSum
                    && Mathf.Abs(point.z - otherPosition.z) < halfDepthSum)
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Plain data holder - the real-world (X,Z) footprint size of whichever
    // building this is on, in world units. Added by every building Factory
    // via BuildingFootprint.Attach so BuildingFootprint.IsClear can compute
    // a real square/rectangle overlap instead of assuming a fixed clearance
    // radius for every building alike.
    public class BuildingFootprintTag : MonoBehaviour
    {
        public Vector2 Size;

        // Whether this building actually carves a NavMeshObstacle (see
        // BuildingFootprint.Attach) - Wall/Gate are tagged here but carve
        // their own obstacle outside this system, so GetNearestApproachPoint
        // below falls back to the raw footprint edge for them instead of
        // subtracting a Margin that was never actually carved.
        public bool CarvesObstacle;

        // The point on this building's own walkable boundary nearest
        // fromPosition, pushed outward by buffer (e.g. a worker's
        // NavMeshAgent radius, so the returned point isn't exactly on the
        // carved obstacle's edge where an agent's body would still clip
        // it). Driven by the real direction from this building's center to
        // fromPosition, not a hardcoded side, so it's correct from any
        // approach angle. Used by Gatherer's drop-off walk target instead
        // of a flat interactionRange guess against the building's raw
        // transform.position, which could sit deep inside unwalkable
        // carved space a NavMeshAgent can never physically reach.
        public Vector3 GetNearestApproachPoint(Vector3 fromPosition, float buffer)
        {
            float halfX = Size.x * 0.5f;
            float halfZ = Size.y * 0.5f;
            if (CarvesObstacle)
            {
                halfX = Mathf.Max(BuildingFootprint.MinObstacleSize * 0.5f, halfX - BuildingFootprint.Margin);
                halfZ = Mathf.Max(BuildingFootprint.MinObstacleSize * 0.5f, halfZ - BuildingFootprint.Margin);
            }

            Vector3 center = transform.position;
            Vector2 direction = new Vector2(fromPosition.x - center.x, fromPosition.z - center.z);
            if (direction.sqrMagnitude < 0.0001f)
            {
                // fromPosition is (almost) exactly on the building's own
                // center - no real direction to aim along, so pick an
                // arbitrary consistent one rather than dividing by zero.
                direction = Vector2.right;
            }
            direction.Normalize();

            // Distance along `direction` from center to where it exits the
            // (halfX, halfZ) box - the smaller of the two axis-aligned
            // exit distances, same as a standard ray-vs-AABB exit test.
            float exitX = Mathf.Abs(direction.x) > 0.0001f ? halfX / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float exitZ = Mathf.Abs(direction.y) > 0.0001f ? halfZ / Mathf.Abs(direction.y) : float.PositiveInfinity;
            float exitDistance = Mathf.Min(exitX, exitZ) + buffer;

            return new Vector3(
                center.x + direction.x * exitDistance,
                center.y,
                center.z + direction.y * exitDistance);
        }
    }
}

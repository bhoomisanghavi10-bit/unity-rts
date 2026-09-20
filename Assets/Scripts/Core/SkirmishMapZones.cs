using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // The zoning rules for a skirmish map, measured from the map centre (the
    // world origin - maps span -size/2..+size/2 on X and Z, 1 unit = 1 tile).
    // Medium is 158 tiles: the outer 40 tiles on every side is the home-base
    // buffer, the middle 78 x 78 is the contested zone, and the outermost 3
    // tiles (which sit inside the home buffer) are a dead zone where nothing
    // may spawn, so units can't get stuck on the edge of the world and the
    // camera can't clip. Zones use the Chebyshev distance from the centre
    // (max of |x| and |z|), so they are concentric squares.
    //
    //   3 (dead) | 37 (home) | 78 (contested) | 37 (home) | 3 (dead)  = 158
    //
    // Purely geometric and side-effect free: spawners, Vista mask baking and
    // the fairness checks all call this so there is one definition.
    public enum MapZone
    {
        // Outer border: nothing spawns here.
        EdgeDeadzone,
        // Outer ring minus the dead zone: player starts, guaranteed starting
        // woodline and primary gold near each Town Center.
        HomeBase,
        // Middle square: large gold veins, stone quarries, choke terrain,
        // relics / contestable sites.
        Contested,
    }

    public static class SkirmishMapZones
    {
        public const float MediumMapSize = 158f;
        public const float EdgeDeadzoneWidth = 3f;
        public const float HomeBufferWidth = 40f;

        // Starting-resource guarantee around each Town Center.
        public const float StartingResourceMinDistance = 10f;
        public const float StartingResourceMaxDistance = 15f;

        // Width of the contested square: 158 - 2 * 40 = 78 on the medium map.
        public static float ContestedWidth(float mapSize)
        {
            return Mathf.Max(0f, mapSize - 2f * HomeBufferWidth);
        }

        // Concentric-square distance from the map centre (ignores Y).
        public static float ChebyshevFromCenter(Vector3 worldPosition)
        {
            return Mathf.Max(Mathf.Abs(worldPosition.x), Mathf.Abs(worldPosition.z));
        }

        public static MapZone Classify(Vector3 worldPosition, float mapSize)
        {
            float half = mapSize * 0.5f;
            float d = ChebyshevFromCenter(worldPosition);

            if (d >= half - EdgeDeadzoneWidth)
            {
                return MapZone.EdgeDeadzone;
            }

            return d >= half - HomeBufferWidth ? MapZone.HomeBase : MapZone.Contested;
        }

        // Anything that spawns (resources, props, starts) must satisfy this.
        public static bool IsSpawnable(Vector3 worldPosition, float mapSize)
        {
            return Classify(worldPosition, mapSize) != MapZone.EdgeDeadzone;
        }

        // A starting resource (woodline, primary gold) belongs 10-15 units
        // from its Town Center.
        public static bool IsWithinStartingResourceRange(Vector3 townCenter, Vector3 resource)
        {
            float d = Vector2.Distance(new Vector2(townCenter.x, townCenter.z), new Vector2(resource.x, resource.z));
            return d >= StartingResourceMinDistance && d <= StartingResourceMaxDistance;
        }
    }
}

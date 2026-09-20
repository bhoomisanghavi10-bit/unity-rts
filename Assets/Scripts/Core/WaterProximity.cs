using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Item 49: small query helper over the current map's water rectangle
    // (see MapDefinitionData.WaterCenter/WaterHalfExtents) - used by Dock
    // placement (must be near water) and anything else that cares whether a
    // point is in/near water, without each caller re-deriving the same
    // rectangle math ProceduralGround already uses to punch its hole.
    public static class WaterProximity
    {
        // The visible shoreline wobbles: along each real (non-map-edge)
        // east/west edge the waterline sits 0..MaxShoreInset units INSIDE the
        // gameplay rectangle, varying smoothly with z. It only ever moves
        // inward, so water is always within the rectangle - buildings, boats
        // and fish keyed off the rectangle can never end up in the wet part
        // that is outside it. ProceduralTerrain (heights/beach) and
        // NavMeshBaker (unwalkable strips) both read this, so what you see,
        // what units can walk and what blocks them all agree.
        public const float MaxShoreInset = 2f;

        // World Y of the water surface on the current map (set by
        // ProceduralTerrain when it builds the water plane) - what anything
        // that floats on the water (boat wakes) needs to sit on.
        public static float SurfaceY { get; internal set; } = 0.25f;

        public static float ShoreInsetAt(float z)
        {
            return MaxShoreInset * Mathf.PerlinNoise(z * 0.09f + 31.7f, 7.3f);
        }

        public static bool HasWater => MapRegistry.Current.WaterHalfExtents.x > 0f && MapRegistry.Current.WaterHalfExtents.z > 0f;

        public static bool IsInsideWater(Vector3 point)
        {
            if (!HasWater)
            {
                return false;
            }

            MapDefinitionData map = MapRegistry.Current;
            return Mathf.Abs(point.x - map.WaterCenter.x) <= map.WaterHalfExtents.x
                && Mathf.Abs(point.z - map.WaterCenter.z) <= map.WaterHalfExtents.z;
        }

        // 0 when the point is inside the water rectangle already; the
        // straight-line distance to its nearest edge otherwise.
        public static float DistanceToWater(Vector3 point)
        {
            if (!HasWater)
            {
                return float.MaxValue;
            }

            MapDefinitionData map = MapRegistry.Current;
            float dx = Mathf.Max(0f, Mathf.Abs(point.x - map.WaterCenter.x) - map.WaterHalfExtents.x);
            float dz = Mathf.Max(0f, Mathf.Abs(point.z - map.WaterCenter.z) - map.WaterHalfExtents.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Horizontal (XZ) unit vector from `point` toward the nearest edge
        // of the water rectangle - a Dock can be built on any shore of the
        // water body (west edge, south edge, etc.), so "which way is the
        // water" depends on the Dock's own position, not a single fixed
        // direction. Falls back to Vector3.forward if the map has no water
        // (caller shouldn't be asking) or the point is already inside it
        // (degenerate zero-length direction).
        public static Vector3 DirectionToNearestWater(Vector3 point)
        {
            if (!HasWater)
            {
                return Vector3.forward;
            }

            Vector3 nearestOnWater = ClampToWater(point);
            Vector3 direction = new Vector3(nearestOnWater.x, 0f, nearestOnWater.z) - new Vector3(point.x, 0f, point.z);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        // WaterMover's fix for the "boat sails onto land" bug: the current
        // water region is a single axis-aligned rectangle (convex), so
        // clamping any destination point into it before storing it is
        // enough to guarantee a straight-line path never leaves water, for
        // every water shape the data model supports today. Returns `point`
        // unchanged if the map has no water at all.
        public static Vector3 ClampToWater(Vector3 point)
        {
            return ClampToWater(point, 0f);
        }

        // Same clamp pulled `inset` units in from every edge of the water
        // rectangle. The rectangle's edge is the waterline, where the
        // terrain bed is now sand at the surface - boats told to sail to the
        // very edge would sit on the beach, so WaterMover keeps them a hull
        // length off it.
        public static Vector3 ClampToWater(Vector3 point, float inset)
        {
            if (!HasWater)
            {
                return point;
            }

            MapDefinitionData map = MapRegistry.Current;
            float hx = Mathf.Max(map.WaterHalfExtents.x - inset, 0f);
            float hz = Mathf.Max(map.WaterHalfExtents.z - inset, 0f);
            float clampedX = Mathf.Clamp(point.x, map.WaterCenter.x - hx, map.WaterCenter.x + hx);
            float clampedZ = Mathf.Clamp(point.z, map.WaterCenter.z - hz, map.WaterCenter.z + hz);
            return new Vector3(clampedX, point.y, clampedZ);
        }
    }
}

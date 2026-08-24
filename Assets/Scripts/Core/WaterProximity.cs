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
    }
}

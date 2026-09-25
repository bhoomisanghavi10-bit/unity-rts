using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Pure math for the Clearing skirmish style: three straight lanes,
    // one between every pair of active town centres, kept clear inside a
    // dense forest. A lane is a line segment; distance is the usual
    // point-to-segment distance in the XZ plane (Y ignored).
    public static class ForestLanes
    {
        // Endpoint pairs, e.g. { playerTc, enemyTc, playerTc, enemy2Tc,
        // enemyTc, enemy2Tc } - every consecutive (0,1)/(2,3)/(4,5) pair is
        // one lane. Returns float.MaxValue if there are no lanes.
        public static float DistanceToNearestLane(Vector3 worldPosition, Vector3[] laneEndpoints)
        {
            if (laneEndpoints == null || laneEndpoints.Length < 2)
            {
                return float.MaxValue;
            }

            Vector2 p = new Vector2(worldPosition.x, worldPosition.z);
            float best = float.MaxValue;
            for (int i = 0; i + 1 < laneEndpoints.Length; i += 2)
            {
                Vector2 a = new Vector2(laneEndpoints[i].x, laneEndpoints[i].z);
                Vector2 b = new Vector2(laneEndpoints[i + 1].x, laneEndpoints[i + 1].z);
                float d = DistancePointToSegment(p, a, b);
                if (d < best)
                {
                    best = d;
                }
            }

            return best;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f)
            {
                return Vector2.Distance(p, a);
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }
    }
}

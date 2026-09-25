using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Pure math for a horizontal river band (see MapDefinitionData.
    // FordCentersX) with a handful of fordable gaps along its length.
    // Used by both ProceduralTerrain (rise the carved height back toward
    // dry ground near a ford, so it's visually shallow) and NavMeshBaker
    // (skip building the not-walkable strip there, so it's actually
    // crossable) - one shared definition so the two never disagree about
    // where a ford is.
    public static class RiverFords
    {
        // Shared falloff distance (world units) beyond FordHalfWidth,
        // used identically by ProceduralTerrain's height blend and
        // NavMeshBaker's walkability check so the two agree on where a
        // ford actually is.
        public const float DefaultFalloff = 10f;
        // NavMeshBaker treats a strip as crossable once FordFactor here
        // reaches this - roughly halfway through the falloff, not only at
        // the exact centre.
        public const float NavMeshWalkableThreshold = 0.5f;

        // 1 at the exact centre of any ford, smoothly falling to 0 by
        // fordHalfWidth + falloff away from it. Returns 0 everywhere when
        // fordCentersX is null/empty, so a map that never sets fords (every
        // map except Divided Riverbed) is completely unaffected.
        public static float FordFactor(float worldX, float[] fordCentersX, float fordHalfWidth, float falloff)
        {
            if (fordCentersX == null || fordCentersX.Length == 0 || falloff <= 0f)
            {
                return 0f;
            }

            float best = 0f;
            for (int i = 0; i < fordCentersX.Length; i++)
            {
                float d = Mathf.Abs(worldX - fordCentersX[i]);
                float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fordHalfWidth, fordHalfWidth + falloff, d));
                if (t > best)
                {
                    best = t;
                }
            }

            return best;
        }
    }
}

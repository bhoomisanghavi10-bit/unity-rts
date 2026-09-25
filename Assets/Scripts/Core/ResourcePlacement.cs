using UnityEngine;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.Core
{
    // Pure placement math for docs/SKIRMISH_MAP_SPEC.md's zoning rules:
    // guaranteed starting resources near each Town Center (rule 2) and
    // contested-zone-only placement for the general gold/stone/farm/relic
    // ring (rule 3). Kept separate from ResourceNodeSpawner (a
    // MonoBehaviour) so both are directly unit-testable without Play mode.
    public static class ResourcePlacement
    {
        // A point 10-15 units from townCenter in a random direction - the
        // guaranteed starting woodline/primary-gold range
        // (SkirmishMapZones.StartingResourceMinDistance/MaxDistance).
        public static Vector3 RandomPointNearTownCenter(DeterministicRandom rng, Vector3 townCenter)
        {
            Vector2 direction = rng.InsideUnitCircleNormalized();
            float radius = rng.Range(SkirmishMapZones.StartingResourceMinDistance, SkirmishMapZones.StartingResourceMaxDistance);
            return townCenter + new Vector3(direction.x * radius, 0f, direction.y * radius);
        }

        // A point drawn from the usual centre-relative ring (min/maxRadius)
        // but constrained to the Contested zone, per the spec's rule that
        // large gold/stone/farms/relics belong in the contested square, not
        // the home buffer. Retries a bounded number of times rather than
        // looping unboundedly, then falls back to a point clamped inside
        // the contested boundary so this always terminates and always
        // returns a Contested point.
        public static Vector3 RandomPointInContestedZone(DeterministicRandom rng, float mapSize, float minRadius, float maxRadius, int maxAttempts)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector2 direction = rng.InsideUnitCircleNormalized();
                float radius = rng.Range(minRadius, maxRadius);
                var candidate = new Vector3(direction.x * radius, 0f, direction.y * radius);
                if (SkirmishMapZones.Classify(candidate, mapSize) == MapZone.Contested)
                {
                    return candidate;
                }
            }

            float half = Mathf.Max(0f, SkirmishMapZones.ContestedWidth(mapSize) * 0.5f - 1f);
            Vector2 fallbackDirection = rng.InsideUnitCircleNormalized();
            float fallbackRadius = rng.Range(0f, half);
            return new Vector3(fallbackDirection.x * fallbackRadius, 0f, fallbackDirection.y * fallbackRadius);
        }
    }
}

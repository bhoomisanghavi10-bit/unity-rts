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
        // returns a Contested point. Draws from the full circle (0..2*PI).
        public static Vector3 RandomPointInContestedZone(DeterministicRandom rng, float mapSize, float minRadius, float maxRadius, int maxAttempts)
        {
            return RandomPointInContestedZone(rng, mapSize, minRadius, maxRadius, maxAttempts, 0f, Mathf.PI * 2f);
        }

        // Same as above, but the angle is drawn from [minAngle, maxAngle)
        // instead of the full circle - used to spread relics fairly across
        // wedges of the map instead of a purely uniform draw (see
        // RelicPlacement), rather than duplicating the retry/fallback logic
        // for that one caller.
        public static Vector3 RandomPointInContestedZone(DeterministicRandom rng, float mapSize, float minRadius, float maxRadius, int maxAttempts, float minAngle, float maxAngle)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float angle = rng.Range(minAngle, maxAngle);
                float radius = rng.Range(minRadius, maxRadius);
                var candidate = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (SkirmishMapZones.Classify(candidate, mapSize) == MapZone.Contested)
                {
                    return candidate;
                }
            }

            float half = Mathf.Max(0f, SkirmishMapZones.ContestedWidth(mapSize) * 0.5f - 1f);
            float fallbackAngle = rng.Range(minAngle, maxAngle);
            float fallbackRadius = rng.Range(0f, half);
            return new Vector3(Mathf.Cos(fallbackAngle) * fallbackRadius, 0f, Mathf.Sin(fallbackAngle) * fallbackRadius);
        }
    }
}

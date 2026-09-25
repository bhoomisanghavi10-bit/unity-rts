using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // docs/SKIRMISH_MAP_SPEC.md's "how the 40/3 tile rule scales to small
    // and large maps" open question, resolved: fixed widths, not
    // proportional. SkirmishMapZones.HomeBufferWidth/EdgeDeadzoneWidth stay
    // the same absolute tile count on every map size (a start's own
    // buffer/safety margin doesn't need to grow just because the map is
    // bigger - matches real AoE's own convention that starting areas stay
    // roughly the same size across map sizes) - so SkirmishMapZones.
    // Classify/ContestedWidth already generalize to any mapSize with zero
    // changes. What genuinely needs to scale with map size is everything
    // downstream of the Contested zone's own area: resource/relic counts,
    // the general-resource ring radii, and where a start sits inside the
    // home band - this is that scaling math, pure and unit-tested so a
    // future size doesn't need its own hand-tuned constants.
    public static class SkirmishMapScaling
    {
        public const float MediumMapSize = SkirmishMapZones.MediumMapSize; // 158
        public static readonly float MediumContestedWidth = SkirmishMapZones.ContestedWidth(MediumMapSize); // 78

        // Resource/tree/farm counts scale linearly with the Contested
        // zone's width (not its area) - matches the same "avoid drowning a
        // bigger map in clutter, avoid leaving a smaller one empty"
        // reasoning MapDefinition.cs's own Phase-5 map-scale-up note
        // already used for RiverValley/Highlands/Coastal (count scaled
        // ~1.5x for a ~2.5x linear/6.25x area jump, not the full area
        // ratio). Always at least 1.
        public static int ScaleCount(int mediumValue, float contestedWidth)
        {
            float ratio = contestedWidth / MediumContestedWidth;
            return Mathf.Max(1, Mathf.RoundToInt(mediumValue * ratio));
        }

        // Ring radii (ResourceMinRadius/MaxRadius, the general gold/stone/
        // farm/fruit/relic draw) scale linearly with the map's own
        // Contested half-width, so every size keeps the same relative
        // "how close to the Contested boundary does this ring reach" shape
        // medium's own 15-40 (against a 39 Contested half-width) already
        // established.
        public static float ScaleRadius(float mediumValue, float contestedWidth)
        {
            float mediumHalf = MediumContestedWidth * 0.5f;
            if (mediumHalf <= 0f)
            {
                return mediumValue;
            }

            return contestedWidth * 0.5f * (mediumValue / mediumHalf);
        }

        // The |z| (or |x|) a start sits at: the midpoint of the playable
        // home band - inside the Contested zone's edge, outside where the
        // edge dead zone begins - generalizing SkirmishMapZones' own
        // "|z|=57 is the centre of the 39..76 band" comment (tuned for the
        // 158 map specifically) to any mapSize.
        public static float HomeBandMidpoint(float mapSize)
        {
            float half = mapSize * 0.5f;
            float contestedHalf = SkirmishMapZones.ContestedWidth(mapSize) * 0.5f;
            float deadZoneStart = half - SkirmishMapZones.EdgeDeadzoneWidth;
            return (contestedHalf + deadZoneStart) * 0.5f;
        }
    }
}

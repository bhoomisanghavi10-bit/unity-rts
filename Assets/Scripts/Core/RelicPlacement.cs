using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Wave 6 item 35 (Relics + Monastery) shipped a fixed RelicCount = 5 for
    // every map, "deliberately NOT scaled with map area" per that item's own
    // note - a reasonable call when only one map size existed, but a real
    // gap now that the skirmish framework (docs/SKIRMISH_MAP_SPEC.md) is
    // meant to support multiple layouts/sizes: a future differently-sized
    // layout would silently inherit the same flat "5" unless someone
    // remembers to hand-retune it. This makes RelicCount a function of the
    // map's own Contested-zone area instead, so any future map size scales
    // automatically with zero extra tuning.
    public static class RelicPlacement
    {
        // Anchored so today's 158x158 skirmish maps (ContestedWidth 78,
        // area 6084) reproduce exactly the 5 that shipped with item 35 -
        // this change is a scaling formula, not a live balance change to
        // any already-tuned map.
        private const float ReferenceContestedArea = 78f * 78f;
        private const int ReferenceRelicCount = 5;
        private const int MinRelicCount = 3;
        private const int MaxRelicCount = 12;

        public static int ComputeRelicCount(float contestedWidth)
        {
            float area = Mathf.Max(0f, contestedWidth) * Mathf.Max(0f, contestedWidth);
            float density = ReferenceRelicCount / ReferenceContestedArea;
            int count = Mathf.RoundToInt(area * density);
            return Mathf.Clamp(count, MinRelicCount, MaxRelicCount);
        }

        // The angular [start, start+size) wedge that relic `index` of
        // `count` total relics should be drawn from - splits the full
        // circle into `count` equal wedges so relics spread fairly around
        // the map centre (and therefore around however many starts sit
        // around it) instead of a purely uniform draw, which with as few
        // as 3-5 points can clump by chance and unfairly favour whichever
        // start happens to be nearest the clump. Resolves
        // docs/SKIRMISH_MAP_SPEC.md's "should Relics be map-authored or
        // randomised" open question: randomised, but fairly spread, not
        // purely uniform.
        public static void WedgeFor(int index, int count, out float wedgeStart, out float wedgeEnd)
        {
            int safeCount = Mathf.Max(1, count);
            float wedgeSize = (Mathf.PI * 2f) / safeCount;
            int safeIndex = ((index % safeCount) + safeCount) % safeCount;
            wedgeStart = safeIndex * wedgeSize;
            wedgeEnd = wedgeStart + wedgeSize;
        }
    }
}

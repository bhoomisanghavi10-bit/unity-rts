using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Which Age each faction has currently reached - a bare static registry,
    // same shape as CivilizationRegistry (which does the identical job for
    // civ assignment): queryable from anywhere (UI, AI, gating checks) with
    // no scene-reference chasing. CivilizationSetup explicitly seeds both
    // factions at Ancient at scene start, same as it already does for civ
    // assignment, rather than relying on a dictionary-miss fallback.
    public static class AgeProgress
    {
        private static readonly Dictionary<FactionId, AgeId> Ages = new Dictionary<FactionId, AgeId>();

        // Phase 6 gap-close: startingAge defaults to Ancient (every call
        // site's prior unconditional behavior) - Maurya's "starts already
        // in the Classical Age" bonus is the one case that passes something
        // else, decided by the caller (CivilizationSetup already knows the
        // civ at this point).
        public static void Initialize(FactionId faction, AgeId startingAge = AgeId.Ancient)
        {
            Ages[faction] = startingAge;
        }

        public static AgeId CurrentAge(FactionId faction)
        {
            return Ages.TryGetValue(faction, out AgeId age) ? age : AgeId.Ancient;
        }

        public static bool HasNextAge(FactionId faction)
        {
            return CurrentAge(faction) != AgeId.Imperial;
        }

        public static AgeId NextAge(FactionId faction)
        {
            return CurrentAge(faction) + 1;
        }

        public static void Advance(FactionId faction, AgeId toAge)
        {
            Ages[faction] = toAge;
        }
    }
}

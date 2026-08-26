using System.Collections.Generic;

namespace KingdomsOfBharat.Core
{
    // Which civilization each faction is playing as - assigned once by
    // CivilizationSetup at scene start. A faction-level lookup rather than
    // a per-unit component: every spawner/system that needs a civ already
    // knows its FactionId, so this avoids adding yet another component to
    // every unit/building just to carry a civ tag.
    public static class CivilizationRegistry
    {
        private static readonly Dictionary<FactionId, CivilizationId> Assignments =
            new Dictionary<FactionId, CivilizationId>();

        public static void Assign(FactionId faction, CivilizationId civilization)
        {
            Assignments[faction] = civilization;
        }

        // Enemy2 in particular is only ever Assign()-ed when
        // CivilizationSetup's enableThirdFaction is on - any reader that
        // wants to show/use Enemy2 (DiplomacyMenu's faction list, most
        // likely, or anything similar added later) needs to check this
        // first rather than calling For() and getting back a real-looking
        // civ for a faction that was never actually assigned one. A silent
        // fallback to a real CivilizationId (see For() below) is exactly
        // what let a phantom "Enemy2" row read as a legitimate opponent.
        public static bool IsAssigned(FactionId faction)
        {
            return Assignments.ContainsKey(faction);
        }

        // Falls back to Chola rather than throwing so a stray read before
        // CivilizationSetup runs (or for a faction that's intentionally
        // never assigned, like Enemy2 with the 3rd faction off) doesn't
        // crash the whole game over a single UI label - but that fallback
        // is a real, playable-looking CivilizationId, not an obvious
        // "unset" sentinel, so any caller that can't guarantee the faction
        // was actually assigned should check IsAssigned() first instead of
        // trusting whatever this returns.
        public static CivilizationId For(FactionId faction)
        {
            return Assignments.TryGetValue(faction, out CivilizationId civilization)
                ? civilization
                : CivilizationId.Chola;
        }
    }
}

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

        public static CivilizationId For(FactionId faction)
        {
            return Assignments.TryGetValue(faction, out CivilizationId civilization)
                ? civilization
                : CivilizationId.Chola;
        }
    }
}

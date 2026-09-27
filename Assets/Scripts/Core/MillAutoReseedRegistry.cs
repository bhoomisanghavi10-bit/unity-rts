using System.Collections.Generic;

namespace KingdomsOfBharat.Core
{
    // Mill-queued auto-reseed (AoE reference: "Farms can be automatically
    // reseeded if they are queued in the Mill (or equivalent)"). A
    // deliberate simplification, disclosed: a per-faction toggle rather
    // than a literal per-Mill-radius queue - this project's Farms have no
    // "linked drop-off building" relationship to piggyback on, so a global
    // per-faction toggle is the closest faithful match without inventing a
    // new linkage system. Same shape/reset convention as DiplomacyRegistry.
    public static class MillAutoReseedRegistry
    {
        private static readonly HashSet<FactionId> Enabled = new HashSet<FactionId>();

        public static bool IsEnabled(FactionId faction)
        {
            return Enabled.Contains(faction);
        }

        public static void Toggle(FactionId faction)
        {
            if (!Enabled.Add(faction))
            {
                Enabled.Remove(faction);
            }
        }

        // Called by CivilizationSetup.BeginMatchCore so a new match doesn't
        // inherit the toggle from whatever the previous match ended with -
        // same "fresh start" convention DiplomacyRegistry/TeamColorBuildingTint
        // already follow at match start.
        public static void Reset()
        {
            Enabled.Clear();
        }
    }
}

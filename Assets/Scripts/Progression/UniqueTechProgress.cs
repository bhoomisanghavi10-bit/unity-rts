using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Whether each faction has researched its civ's unique tech
    // (UniqueTechDefinition) - a one-time flag, not a tiered track like
    // UpgradeProgress's Attack/Armor lines, matching AoE2's own unique
    // techs (researched once, no further tiers).
    public static class UniqueTechProgress
    {
        private static readonly Dictionary<FactionId, bool> Researched = new Dictionary<FactionId, bool>();

        public static bool HasResearched(FactionId faction)
        {
            return Researched.TryGetValue(faction, out bool researched) && researched;
        }

        public static void MarkResearched(FactionId faction)
        {
            Researched[faction] = true;
        }

        // Production reset: called from ProgressionRegistry.ResetAllForNewMatch
        // (itself called from CivilizationSetup.BeginMatchCore) so a
        // faction's unique-tech flag doesn't leak into the next match
        // started in the same Editor/Player process. Previously this class
        // had no reset at all - the gap that let a stale flag from one
        // match/test silently survive into the next (see
        // docs/SESSION_LOG.md / Kingdoms_of_Bharat_Repository_Audit.md
        // finding F08).
        internal static void Reset()
        {
            Researched.Clear();
        }

        internal static void ResetForTests() => Reset();
    }
}

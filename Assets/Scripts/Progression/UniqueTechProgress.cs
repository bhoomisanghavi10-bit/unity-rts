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
    }
}

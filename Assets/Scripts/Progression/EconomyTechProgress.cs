using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Phase 6 gap-close (deeper tech tree): which economic techs each
    // faction has researched - one-time flags like UniqueTechProgress, not
    // tiered like UpgradeProgress, matching AoE2's own Wheelbarrow/Loom-
    // style economic techs (each researched once, no further levels).
    // Unlike UniqueTechProgress (one tech per civ, effect differs by who
    // researches it), these are the same 3 techs and the same effect for
    // every civ - researched at TownCenter, deliberately a different
    // building than Barracks' military research, so "where do I invest
    // production time" is a real choice between the two buildings, not
    // just a longer menu at one.
    public enum EconomyTech
    {
        ImprovedTools,
        PackMules,
        TradeDiscounts,
    }

    public static class EconomyTechProgress
    {
        private static readonly Dictionary<(FactionId, EconomyTech), bool> Researched =
            new Dictionary<(FactionId, EconomyTech), bool>();

        public static bool HasResearched(FactionId faction, EconomyTech tech)
        {
            return Researched.TryGetValue((faction, tech), out bool researched) && researched;
        }

        public static void MarkResearched(FactionId faction, EconomyTech tech)
        {
            Researched[(faction, tech)] = true;
        }
    }
}

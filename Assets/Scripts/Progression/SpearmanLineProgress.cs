using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 3 item 10 (docs/IMPLEMENTATION_ROADMAP.md): the Spearman tier
    // ladder - Bhaladhari (base, no research needed) -> Trishuladhari ->
    // Maha Trishuladhari. Mirrors InfantryLineProgress's shape exactly
    // (same "baked in at spawn, not retroactive" convention, same
    // intentionally-hardcoded table) - only the tier count (3, not 5) and
    // the gated ages (Classical/Durg/Imperial, not
    // Ancient/Classical/Durg/Imperial/Imperial) differ, matching this
    // item's own roadmap spec.
    public readonly struct SpearmanTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public SpearmanTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
        {
            Name = name;
            RequiredAge = requiredAge;
            HpBonus = hpBonus;
            DamageBonus = damageBonus;
            GoldCost = goldCost;
            WoodCost = woodCost;
            ResearchTime = researchTime;
        }
    }

    public static class SpearmanLineProgress
    {
        // Tier 0 (Bhaladhari) is the current flat Spearman - trainable with
        // no research, so it carries no cost/research-time entry of its
        // own (never looked up as a "next tier" to research).
        public static readonly SpearmanTierData[] Tiers =
        {
            new SpearmanTierData("Bhaladhari", AgeId.Classical, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new SpearmanTierData("Trishuladhari", AgeId.Durg, hpBonus: 18f, damageBonus: 4f, goldCost: 120f, woodCost: 60f, researchTime: 25f),
            new SpearmanTierData("Maha Trishuladhari", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
        };

        public const int MaxTier = 2;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static SpearmanTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static SpearmanTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

        // Ordinal age comparison relies on AgeId's own declared order
        // (Ancient < Classical < Durg < Imperial) - same convention
        // InfantryLineProgress/AgeProgress.NextAge already rely on.
        public static bool NextTierAgeRequirementMet(FactionId faction) =>
            HasNextTier(faction) && AgeProgress.CurrentAge(faction) >= NextTierData(faction).RequiredAge;

        public static void AdvanceTier(FactionId faction)
        {
            FactionTiers[faction] = Tier(faction) + 1;
        }

        // Test-only: static state persists for the whole Editor/Test-Runner
        // domain, same isolation need as InfantryLineProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

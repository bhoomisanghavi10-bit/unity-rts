using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item 22 (docs/IMPLEMENTATION_ROADMAP.md): the Camel Rider tier
    // ladder - Ushtrarohi (base) -> Maha Ushtrarohi. Only 2 tiers total,
    // gated at Durg/Imperial per the roadmap's own item text - mirrors
    // CavalryArcherLineProgress's shape exactly (the closest existing
    // precedent for a 2-tier Durg/Imperial mounted-unit line). Tier 0's
    // RequiredAge is descriptive only, never enforced (same convention
    // CavalryLineProgress/ArcherLineProgress/CavalryArcherLineProgress
    // already established - RequestTrainCamelRider has no age gate of its
    // own). Tier 1's bonus/cost reuses every other line's own established
    // Imperial-gate growth exactly (+30 HP/+6 dmg/200 Gold/100 Wood/40s) -
    // the closest existing curve at a matching gate, not independently
    // balanced.
    public readonly struct CamelRiderTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public CamelRiderTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class CamelRiderLineProgress
    {
        // Tier 0 (Ushtrarohi) is the current flat Camel Rider - trainable
        // with no research, so it carries no cost/research-time entry of
        // its own (never looked up as a "next tier" to research).
        public static readonly CamelRiderTierData[] Tiers =
        {
            new CamelRiderTierData("Ushtrarohi", AgeId.Durg, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new CamelRiderTierData("Maha Ushtrarohi", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
        };

        public const int MaxTier = 1;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static CamelRiderTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static CamelRiderTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

        // Ordinal age comparison relies on AgeId's own declared order
        // (Ancient < Classical < Durg < Imperial) - same convention every
        // other line's NextTierAgeRequirementMet already relies on.
        public static bool NextTierAgeRequirementMet(FactionId faction) =>
            HasNextTier(faction) && AgeProgress.CurrentAge(faction) >= NextTierData(faction).RequiredAge;

        public static void AdvanceTier(FactionId faction)
        {
            FactionTiers[faction] = Tier(faction) + 1;
        }

        // Test-only: static state persists for the whole Editor/Test-Runner
        // domain, same isolation need as every other line's ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

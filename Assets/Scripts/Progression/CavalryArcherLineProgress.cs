using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item (docs/IMPLEMENTATION_ROADMAP.md): the Cavalry Archer tier
    // ladder - Ashva Dhanurdhara (base) -> Maha Ashva Dhanurdhara. Only 2
    // tiers total, same shape as SkirmisherLineProgress, but gated at
    // Durg/Imperial (not Classical/Durg) per the roadmap's own item text -
    // this is a later-game mobile raider, not an early-game unit. Tier 0's
    // RequiredAge is descriptive only, never enforced (same convention
    // CavalryLineProgress/ArcherLineProgress already established -
    // RequestTrainCavalryArcher has no age gate of its own). Tier 1's
    // bonus/cost reuses every other line's own established Imperial-gate
    // growth exactly (+30 HP/+6 dmg/200 Gold/100 Wood/40s, e.g.
    // CavalryLineProgress's Maha Ashvarohi step) - the closest existing
    // curve at a matching gate, not independently balanced.
    public readonly struct CavalryArcherTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public CavalryArcherTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class CavalryArcherLineProgress
    {
        // Tier 0 (Ashva Dhanurdhara) is the current flat Cavalry Archer -
        // trainable with no research, so it carries no cost/research-time
        // entry of its own (never looked up as a "next tier" to research).
        public static readonly CavalryArcherTierData[] Tiers =
        {
            new CavalryArcherTierData("Ashva Dhanurdhara", AgeId.Durg, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new CavalryArcherTierData("Maha Ashva Dhanurdhara", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
        };

        public const int MaxTier = 1;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static CavalryArcherTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static CavalryArcherTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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

using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item 20 (docs/IMPLEMENTATION_ROADMAP.md): the Battering Ram
    // tier ladder - Dwarabhanjaka (base, no research needed) -> Maha
    // Dwarabhanjaka -> Vajra Dwarabhanjaka. Mirrors ArcherLineProgress's
    // shape exactly (same 3-tier Classical/Durg/Imperial gate pattern, same
    // "baked in at spawn, not retroactive" convention, same intentionally-
    // hardcoded table) - tier bonuses/costs reuse ArcherLineProgress's own
    // values at matching age gates rather than independently balanced, the
    // same consistency choice every other 3-tier Wave 3/4 line has made.
    public readonly struct BatteringRamTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public BatteringRamTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class BatteringRamLineProgress
    {
        // Tier 0 (Dwarabhanjaka) is the current flat Battering Ram -
        // trainable with no research, so it carries no cost/research-time
        // entry of its own (never looked up as a "next tier" to research).
        public static readonly BatteringRamTierData[] Tiers =
        {
            new BatteringRamTierData("Dwarabhanjaka", AgeId.Classical, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new BatteringRamTierData("Maha Dwarabhanjaka", AgeId.Durg, hpBonus: 18f, damageBonus: 4f, goldCost: 120f, woodCost: 60f, researchTime: 25f),
            new BatteringRamTierData("Vajra Dwarabhanjaka", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
        };

        public const int MaxTier = 2;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static BatteringRamTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static BatteringRamTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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
        // domain, same isolation need as every other *LineProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

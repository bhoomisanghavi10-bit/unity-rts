using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 3 item 14 (docs/IMPLEMENTATION_ROADMAP.md): the Mangonel/Siege
    // tier ladder - Shilakshepaka (base, no research needed) -> Maha
    // Shilakshepaka -> Vajra Shilakshepaka. Mirrors CavalryLineProgress's
    // shape exactly (same "baked in at spawn, not retroactive" convention,
    // same intentionally-hardcoded table, same back-to-back Imperial-gated
    // last two tiers - Durg/Imperial/Imperial per the roadmap's own spec).
    // Tier bonuses/costs reuse the same growth every other line's own
    // back-to-back Imperial pair already uses (Cavalry's Maha Ashvarohi ->
    // Vir Ashvarohi values), not independently balanced.
    public readonly struct SiegeTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public SiegeTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class SiegeLineProgress
    {
        // Tier 0 (Shilakshepaka) is the current flat Siege - trainable with
        // no research, so it carries no cost/research-time entry of its
        // own (never looked up as a "next tier" to research).
        public static readonly SiegeTierData[] Tiers =
        {
            new SiegeTierData("Shilakshepaka", AgeId.Durg, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new SiegeTierData("Maha Shilakshepaka", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
            new SiegeTierData("Vajra Shilakshepaka", AgeId.Imperial, hpBonus: 45f, damageBonus: 9f, goldCost: 250f, woodCost: 125f, researchTime: 50f),
        };

        public const int MaxTier = 2;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static SiegeTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static SiegeTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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
        // domain, same isolation need as CavalryLineProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

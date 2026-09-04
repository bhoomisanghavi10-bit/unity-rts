using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 3 item 12 (docs/IMPLEMENTATION_ROADMAP.md): the Cavalry/Knight
    // tier ladder - Ashvarohi (base, no research needed) -> Maha Ashvarohi
    // -> Vir Ashvarohi. Mirrors SpearmanLineProgress/ArcherLineProgress's
    // shape exactly (same "baked in at spawn, not retroactive" convention,
    // same intentionally-hardcoded table). Unlike those two lines, this
    // one's last two tiers share the Imperial age gate (Durg/Imperial/
    // Imperial, per the roadmap's own spec) rather than one age per tier -
    // tier 0's RequiredAge (Durg) is descriptive only, same as every other
    // line here; NextTierAgeRequirementMet only ever checks the *next*
    // tier, and RequestTrainCavalry has no age gate of its own, so this
    // doesn't change when Cavalry itself becomes trainable. Tier bonuses/
    // costs reuse InfantryLineProgress's own back-to-back Imperial pair
    // (Maha Khandayata -> Vir Yodha) at matching gates, the closest
    // existing precedent for two successive Imperial-gated tiers.
    public readonly struct CavalryTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public CavalryTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class CavalryLineProgress
    {
        // Tier 0 (Ashvarohi) is the current flat Cavalry - trainable with
        // no research, so it carries no cost/research-time entry of its
        // own (never looked up as a "next tier" to research).
        public static readonly CavalryTierData[] Tiers =
        {
            new CavalryTierData("Ashvarohi", AgeId.Durg, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new CavalryTierData("Maha Ashvarohi", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
            new CavalryTierData("Vir Ashvarohi", AgeId.Imperial, hpBonus: 45f, damageBonus: 9f, goldCost: 250f, woodCost: 125f, researchTime: 50f),
        };

        public const int MaxTier = 2;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static CavalryTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static CavalryTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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
        // domain, same isolation need as SpearmanLineProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

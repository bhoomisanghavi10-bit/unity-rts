using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item 19 (docs/IMPLEMENTATION_ROADMAP.md): the Skirmisher tier
    // ladder - Pratirodhi Dhanurdhara (base, no research needed) -> Maha
    // Pratirodhi Dhanurdhara. Only 2 tiers total, not 3 like every Wave 3
    // line - the roadmap's own item text fixes this at Classical (base)/
    // Durg (the one research step), matching Wave 3 item 13's Elephant line
    // as the closest existing precedent for a 2-tier ladder. Tier 1's
    // bonus/cost reuses ArcherLineProgress's own Durg-gate growth exactly
    // (+18 HP/+4 dmg/120 Gold/60 Wood/25s) - the closest existing curve at
    // a matching gate, not independently balanced, same consistency choice
    // every prior Wave 3/4 line already made.
    public readonly struct SkirmisherTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public SkirmisherTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class SkirmisherLineProgress
    {
        // Tier 0 (Pratirodhi Dhanurdhara) is the current flat Skirmisher -
        // trainable with no research, so it carries no cost/research-time
        // entry of its own (never looked up as a "next tier" to research).
        public static readonly SkirmisherTierData[] Tiers =
        {
            new SkirmisherTierData("Pratirodhi Dhanurdhara", AgeId.Classical, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new SkirmisherTierData("Maha Pratirodhi Dhanurdhara", AgeId.Durg, hpBonus: 18f, damageBonus: 4f, goldCost: 120f, woodCost: 60f, researchTime: 25f),
        };

        public const int MaxTier = 1;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static SkirmisherTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static SkirmisherTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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

using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 3 item 13 (docs/IMPLEMENTATION_ROADMAP.md): the Elephant line -
    // Gajaroha (base, no research needed, the existing flat War Elephant)
    // -> Maha Gajaroha, gated Durg/Imperial per the roadmap's own spec.
    //
    // User-confirmed design decision #1: ONE shared ladder for both War
    // Elephant civs (Maurya, Vijayanagara) rather than two independently-
    // tuned ones - matches every other tier line's own precedent (a single
    // table read by whichever faction trains that unit); each civ's own
    // CivilizationProfile/AgeProfile multipliers plus each factory's own
    // already-distinct base stats/model still differentiate the two
    // outcomes, same as today. The single Imperial-gated step reuses the
    // same +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other line's
    // own first Imperial-gate step already uses (Archer's Maha
    // Dhanurdhara, Spearman's Maha Trishuladhari, Cavalry's Maha
    // Ashvarohi), not independently balanced.
    //
    // User-confirmed design decision #2: this line doubles as the "Elite
    // tier" item 16 (Unique-unit Elite tier, 7 units) would otherwise plan
    // for these same two War Elephant factories - when item 16 is picked
    // up, its own 7-unit list excludes MauryaWarElephantFactory/
    // VijayanagaraWarElephantFactory (5 remain), rather than stacking a
    // second Durg->Imperial upgrade on top of this one. See this file's
    // own roadmap entry.
    //
    // Research lives on Durg, not Barracks (unlike every other tier line)
    // - War Elephants train from Durg (UniqueUnitDefinition.Spawn via
    // Durg.RequestTrainUniqueUnit), so this is "research where the unit
    // trains," the same logic Barracks' own four lines already follow.
    public readonly struct ElephantTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public ElephantTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class ElephantLineProgress
    {
        // Tier 0 (Gajaroha) is the current flat War Elephant - already
        // Durg-gated by needing a Durg building at all to train one, so it
        // carries no cost/research-time entry of its own (never looked up
        // as a "next tier" to research).
        public static readonly ElephantTierData[] Tiers =
        {
            new ElephantTierData("Gajaroha", AgeId.Durg, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new ElephantTierData("Maha Gajaroha", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
        };

        public const int MaxTier = 1;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static ElephantTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static ElephantTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

        // Ordinal age comparison relies on AgeId's own declared order
        // (Ancient < Classical < Durg < Imperial) - same convention every
        // other line's NextTierAgeRequirementMet already relies on.
        public static bool NextTierAgeRequirementMet(FactionId faction) =>
            HasNextTier(faction) && AgeProgress.CurrentAge(faction) >= NextTierData(faction).RequiredAge;

        public static void AdvanceTier(FactionId faction)
        {
            FactionTiers[faction] = Tier(faction) + 1;
        }

        // Data-driven check for "does this unique-unit slot train a War
        // Elephant" - lets Durg gate its own Elephant-tier research button
        // without a hardcoded civ switch, matching UniqueUnitDefinition's
        // own "data-driven, not a civ switch" convention.
        public static bool IsElephantUnitId(string unitId) =>
            unitId == "maurya_war_elephant" || unitId == "vijayanagara_war_elephant";

        // Test-only: static state persists for the whole Editor/Test-Runner
        // domain, same isolation need as CavalryLineProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

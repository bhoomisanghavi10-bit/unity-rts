using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item 25 (docs/IMPLEMENTATION_ROADMAP.md): the Fire Ship tier
    // ladder - Agni Nauka (base, no research needed) -> Maha Agni Nauka ->
    // Vega Agni Nauka, Classical/Durg/Imperial. Mirrors NavalLineProgress's
    // shape exactly (same 3-tier Classical/Durg/Imperial gate pattern,
    // same "baked in at spawn, not retroactive" convention, same
    // intentionally-hardcoded table) - tier bonuses/costs reuse
    // NavalLineProgress's own values at matching age gates rather than
    // independently balanced, the same consistency choice every prior
    // Wave 3/4 line has made. Research lives on Dock (where Fire Ship
    // trains, alongside War Galley) as an independent track from
    // NavalLineProgress's own - the same "one building, several
    // independent tier tracks" convention Barracks already established
    // for Infantry/Spearman/Archer/Cavalry/Siege/Chara/Skirmisher/
    // BatteringRam/CavalryArcher/CamelRider/Scorpion.
    public readonly struct FireShipTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public FireShipTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class FireShipLineProgress
    {
        // Tier 0 (Agni Nauka) is the base Fire Ship - trainable with no
        // research, so it carries no cost/research-time entry of its own
        // (never looked up as a "next tier" to research).
        public static readonly FireShipTierData[] Tiers =
        {
            new FireShipTierData("Agni Nauka", AgeId.Classical, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new FireShipTierData("Maha Agni Nauka", AgeId.Durg, hpBonus: 18f, damageBonus: 4f, goldCost: 120f, woodCost: 60f, researchTime: 25f),
            new FireShipTierData("Vega Agni Nauka", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
        };

        public const int MaxTier = 2;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static FireShipTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static FireShipTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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

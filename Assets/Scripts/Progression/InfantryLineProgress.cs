using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 3 item 9 (docs/IMPLEMENTATION_ROADMAP.md): the Infantry tier
    // ladder - Padati (base, no research needed) -> Senani -> Khandayata ->
    // Maha Khandayata -> Vir Yodha. SoldierFactory reads the current tier
    // at spawn and bakes its bonus in, same "not retroactive" convention as
    // UpgradeProgress/AgeProfile/CivilizationProfile - researching a tier
    // only benefits Soldiers trained after, not ones already on the field.
    // Deliberately its own hardcoded table, not CSV-migrated - same
    // "intentionally hardcoded" reasoning this project already applies to
    // AgeProfile/UpgradeProgress (pure formula tuning, no natural per-row
    // CSV shape yet).
    public readonly struct InfantryTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public InfantryTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
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

    public static class InfantryLineProgress
    {
        // Tier 0 (Padati) is the current flat Soldier - trainable from
        // Ancient with no research, so it carries no cost/research-time
        // entry of its own (never looked up as a "next tier" to research).
        public static readonly InfantryTierData[] Tiers =
        {
            new InfantryTierData("Padati", AgeId.Ancient, hpBonus: 0f, damageBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new InfantryTierData("Senani", AgeId.Classical, hpBonus: 8f, damageBonus: 2f, goldCost: 100f, woodCost: 50f, researchTime: 20f),
            new InfantryTierData("Khandayata", AgeId.Durg, hpBonus: 18f, damageBonus: 4f, goldCost: 150f, woodCost: 75f, researchTime: 30f),
            new InfantryTierData("Maha Khandayata", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f),
            new InfantryTierData("Vir Yodha", AgeId.Imperial, hpBonus: 45f, damageBonus: 9f, goldCost: 250f, woodCost: 125f, researchTime: 50f),
        };

        public const int MaxTier = 4;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static InfantryTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static InfantryTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

        // Ordinal age comparison relies on AgeId's own declared order
        // (Ancient < Classical < Durg < Imperial) - same convention
        // AgeProgress.NextAge already relies on.
        public static bool NextTierAgeRequirementMet(FactionId faction) =>
            HasNextTier(faction) && AgeProgress.CurrentAge(faction) >= NextTierData(faction).RequiredAge;

        public static void AdvanceTier(FactionId faction)
        {
            FactionTiers[faction] = Tier(faction) + 1;
        }

        // Test-only: static state persists for the whole Editor/Test-Runner
        // domain, same isolation need as UpgradeProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionTiers.Clear();
        }
    }
}

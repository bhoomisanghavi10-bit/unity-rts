using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item 18 (docs/IMPLEMENTATION_ROADMAP.md): the Scout/Chara tier
    // ladder - Chara (base, no research needed) -> Vega Ashvarohi -> Maha
    // Vega Ashvarohi. Mirrors SpearmanLineProgress's shape exactly (same
    // "baked in at spawn, not retroactive" convention, same intentionally-
    // hardcoded table, same Ancient/Classical/Durg gate pattern), but with
    // VisionBonus/SpeedBonus in place of DamageBonus - "Vega" literally
    // translates to "speed," and this line is an exploration unit, not a
    // combat one (see ScoutFactory's own note on why it never opts into
    // UpgradeProgress damage/armor scaling). Tier 1/2 costs/research-time
    // reuse InfantryLineProgress's own Padati->Senani (Classical gate) and
    // Senani->Khandayata (Durg gate) values exactly, the closest existing
    // curve at a matching gate, not independently balanced.
    public readonly struct ScoutTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float VisionBonus;
        public readonly float SpeedBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public ScoutTierData(string name, AgeId requiredAge, float hpBonus, float visionBonus, float speedBonus, float goldCost, float woodCost, float researchTime)
        {
            Name = name;
            RequiredAge = requiredAge;
            HpBonus = hpBonus;
            VisionBonus = visionBonus;
            SpeedBonus = speedBonus;
            GoldCost = goldCost;
            WoodCost = woodCost;
            ResearchTime = researchTime;
        }
    }

    public static class ScoutLineProgress
    {
        // Tier 0 (Chara) is the current flat Scout - trainable from Ancient
        // with no research, so it carries no cost/research-time entry of
        // its own (never looked up as a "next tier" to research).
        public static readonly ScoutTierData[] Tiers =
        {
            new ScoutTierData("Chara", AgeId.Ancient, hpBonus: 0f, visionBonus: 0f, speedBonus: 0f, goldCost: 0f, woodCost: 0f, researchTime: 0f),
            new ScoutTierData("Vega Ashvarohi", AgeId.Classical, hpBonus: 8f, visionBonus: 2f, speedBonus: 1.0f, goldCost: 100f, woodCost: 50f, researchTime: 20f),
            new ScoutTierData("Maha Vega Ashvarohi", AgeId.Durg, hpBonus: 18f, visionBonus: 3f, speedBonus: 1.5f, goldCost: 150f, woodCost: 75f, researchTime: 30f),
        };

        public const int MaxTier = 2;

        private static readonly Dictionary<FactionId, int> FactionTiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return FactionTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static ScoutTierData Current(FactionId faction) => Tiers[Tier(faction)];

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static ScoutTierData NextTierData(FactionId faction) => Tiers[Tier(faction) + 1];

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

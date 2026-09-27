using System;
using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Which farming-upgrade tier each faction has researched - AoE II's own
    // Horse Collar/Heavy Plow/Crop Rotation, each adding Food to a Farm's
    // max capacity (base 175 + 3x125 = 550 at Imperial with all 3
    // researched, matching real AoE II's own known values). Strictly
    // sequential and age-gated, unlike EconomyTech's free-pick-any-of-3
    // shape - so this mirrors UpgradeProgress's single age-gated tier track
    // instead (same TierRequiredAges array shape), researched independently
    // at TownCenter alongside Worker training/Age-up/EconomyTech.
    //
    // Costs reuse this project's own established per-age-gate growth
    // numbers verbatim (the same values every other tier line reuses at
    // these gates), not independently balanced.
    public static class FarmTechProgress
    {
        public const int MaxTier = 3;
        private const float FoodPerTier = 125f;

        private static readonly AgeId[] TierRequiredAges = { AgeId.Classical, AgeId.Durg, AgeId.Imperial };
        private static readonly string[] TierNames = { "Horse Collar", "Heavy Plow", "Crop Rotation" };
        private static readonly (float gold, float wood, float time)[] TierCosts =
        {
            (120f, 60f, 25f),
            (200f, 100f, 40f),
            (250f, 125f, 50f),
        };

        private static readonly Dictionary<FactionId, int> Tiers = new Dictionary<FactionId, int>();

        public static int Tier(FactionId faction)
        {
            return Tiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static bool HasNextTier(FactionId faction) => Tier(faction) < MaxTier;

        public static AgeId NextTierRequiredAge(FactionId faction) =>
            TierRequiredAges[Math.Min(Tier(faction), TierRequiredAges.Length - 1)];

        public static bool NextTierAgeRequirementMet(FactionId faction) =>
            HasNextTier(faction) && AgeProgress.CurrentAge(faction) >= NextTierRequiredAge(faction);

        public static string NextTierName(FactionId faction) =>
            HasNextTier(faction) ? TierNames[Tier(faction)] : null;

        public static float NextTierGoldCost(FactionId faction) =>
            TierCosts[Math.Min(Tier(faction), TierCosts.Length - 1)].gold;

        public static float NextTierWoodCost(FactionId faction) =>
            TierCosts[Math.Min(Tier(faction), TierCosts.Length - 1)].wood;

        public static float NextTierResearchTime(FactionId faction) =>
            TierCosts[Math.Min(Tier(faction), TierCosts.Length - 1)].time;

        public static void Advance(FactionId faction)
        {
            Tiers[faction] = Tier(faction) + 1;
        }

        // Read by Farm.MaxFood/Farm.Tick's retroactive top-up - not baked
        // in at spawn like unit stat bonuses, deliberately: the reference
        // explicitly wants already-built Farms to benefit proportionally,
        // not just Farms built after the research completes.
        public static float MaxFoodBonus(FactionId faction)
        {
            return Tier(faction) * FoodPerTier;
        }

        // Production reset: same per-match static-registry-reset convention
        // every other Progression static class in this project uses.
        internal static void Reset()
        {
            Tiers.Clear();
        }

        internal static void ResetForTests() => Reset();
    }
}

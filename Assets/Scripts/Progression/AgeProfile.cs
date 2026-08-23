using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Progression
{
    public enum AgeId
    {
        Ancient,
        Classical,
        Imperial,
    }

    // Per-age incremental bonuses, same shape/spirit as CivilizationProfile -
    // stacks alongside it (both multipliers get combined at each read site),
    // never replaces it. Each profile's multipliers represent the TOTAL
    // bonus at that age relative to Ancient's baseline (not an incremental
    // delta on top of the previous age), so a read site only ever needs
    // AgeProfile.For(currentAge) - no compounding across skipped ages.
    public readonly struct AgeProfile
    {
        public readonly string DisplayName;
        public readonly float WoodCost;
        public readonly float StoneCost;
        public readonly float ResearchTime;
        public readonly float GatherRateMultiplier;
        public readonly float MaxHealthMultiplier;
        public readonly float TrainTimeMultiplier;

        public AgeProfile(
            string displayName, float woodCost, float stoneCost, float researchTime,
            float gatherRateMultiplier, float maxHealthMultiplier, float trainTimeMultiplier)
        {
            DisplayName = displayName;
            WoodCost = woodCost;
            StoneCost = stoneCost;
            ResearchTime = researchTime;
            GatherRateMultiplier = gatherRateMultiplier;
            MaxHealthMultiplier = maxHealthMultiplier;
            TrainTimeMultiplier = trainTimeMultiplier;
        }

        private static readonly Dictionary<AgeId, AgeProfile> Profiles = new Dictionary<AgeId, AgeProfile>
        {
            {
                AgeId.Ancient,
                new AgeProfile(
                    "Ancient Age", woodCost: 0f, stoneCost: 0f, researchTime: 0f,
                    gatherRateMultiplier: 1f, maxHealthMultiplier: 1f, trainTimeMultiplier: 1f)
            },
            {
                AgeId.Classical,
                new AgeProfile(
                    "Classical Age", woodCost: 150f, stoneCost: 100f, researchTime: 30f,
                    gatherRateMultiplier: 1.1f, maxHealthMultiplier: 1.1f, trainTimeMultiplier: 0.9f)
            },
            {
                AgeId.Imperial,
                new AgeProfile(
                    "Imperial Age", woodCost: 300f, stoneCost: 200f, researchTime: 50f,
                    gatherRateMultiplier: 1.25f, maxHealthMultiplier: 1.2f, trainTimeMultiplier: 0.8f)
            },
        };

        public static AgeProfile For(AgeId id)
        {
            return Profiles[id];
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    public enum CivilizationId
    {
        Chola,
        Vijayanagara,
        Rajput,
        Maurya,
        Maratha,
    }

    // Historically-grounded per-civilization stat multipliers and identity
    // color. Bonuses are deliberately modest (10-20%, one or two per civ) -
    // flavor and a reason to pick one, not a balance overhaul.
    public readonly struct CivilizationProfile
    {
        public readonly string DisplayName;
        public readonly Color PrimaryColor;
        public readonly float GatherRateMultiplier;
        public readonly float BuildCostMultiplier;
        public readonly float SoldierDamageMultiplier;
        public readonly float MaxHealthMultiplier;
        public readonly float TrainTimeMultiplier;

        public CivilizationProfile(
            string displayName, Color primaryColor,
            float gatherRateMultiplier, float buildCostMultiplier,
            float soldierDamageMultiplier, float maxHealthMultiplier,
            float trainTimeMultiplier)
        {
            DisplayName = displayName;
            PrimaryColor = primaryColor;
            GatherRateMultiplier = gatherRateMultiplier;
            BuildCostMultiplier = buildCostMultiplier;
            SoldierDamageMultiplier = soldierDamageMultiplier;
            MaxHealthMultiplier = maxHealthMultiplier;
            TrainTimeMultiplier = trainTimeMultiplier;
        }

        // Chola: renowned for extensive irrigation works, temple-building
        // projects, and trade networks - an economic identity (faster
        // gathering, cheaper construction).
        // Vijayanagara: renowned for Hampi's fortifications and large
        // standing armies - a military-tempo identity (faster training).
        // Rajput: renowned warrior clans and martial culture - a raw
        // combat-power identity (harder-hitting, tougher units).
        private static readonly Dictionary<CivilizationId, CivilizationProfile> Profiles =
            new Dictionary<CivilizationId, CivilizationProfile>
            {
                {
                    CivilizationId.Chola,
                    new CivilizationProfile(
                        "Chola", new Color(0.55f, 0.2f, 0.08f),
                        gatherRateMultiplier: 1.15f, buildCostMultiplier: 0.85f,
                        soldierDamageMultiplier: 1f, maxHealthMultiplier: 1f,
                        trainTimeMultiplier: 1f)
                },
                {
                    CivilizationId.Vijayanagara,
                    new CivilizationProfile(
                        "Vijayanagara", new Color(0.85f, 0.6f, 0.1f),
                        gatherRateMultiplier: 1f, buildCostMultiplier: 1f,
                        soldierDamageMultiplier: 1f, maxHealthMultiplier: 1f,
                        trainTimeMultiplier: 0.8f)
                },
                {
                    CivilizationId.Rajput,
                    new CivilizationProfile(
                        "Rajput", new Color(0.15f, 0.25f, 0.6f),
                        gatherRateMultiplier: 1f, buildCostMultiplier: 1f,
                        soldierDamageMultiplier: 1.2f, maxHealthMultiplier: 1.15f,
                        trainTimeMultiplier: 1f)
                },
            };

        public static CivilizationProfile For(CivilizationId id)
        {
            return Profiles[id];
        }
    }
}

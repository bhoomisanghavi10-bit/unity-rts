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
    //
    // Phase 2 migration: values are read from the CSV-generated
    // CivilizationDefinition assets (Assets/Editor/CsvToScriptableObject.cs
    // -> DataRegistry) rather than a hardcoded dictionary. Only the 5
    // broad, applyToAllCategories StatModifiers on each civ's
    // passiveBonuses map onto this struct's named fields - narrower,
    // category-specific bonuses (e.g. Rajput's Cavalry-only gold-cost
    // discount) live on the CivilizationDefinition itself and aren't
    // represented here; a future pass should have callers read those
    // directly from DataRegistry.GetCivilization(...).passiveBonuses
    // instead of growing this struct's field list indefinitely.
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

        // Identity color has no home in CivilizationDefinition's schema
        // (civ_bonus_template.csv never carried one) - kept here as a
        // small presentation-only lookup rather than extending the data
        // schema for a single cosmetic field. CivPicker card tinting is
        // the only consumer.
        private static readonly Dictionary<CivilizationId, Color> Colors = new Dictionary<CivilizationId, Color>
        {
            { CivilizationId.Chola, new Color(0.55f, 0.2f, 0.08f) },
            { CivilizationId.Vijayanagara, new Color(0.85f, 0.6f, 0.1f) },
            { CivilizationId.Rajput, new Color(0.15f, 0.25f, 0.6f) },
            { CivilizationId.Maurya, new Color(0.75f, 0.55f, 0.15f) },
            { CivilizationId.Maratha, new Color(0.15f, 0.45f, 0.2f) },
        };

        private static readonly Dictionary<CivilizationId, string> CivIds = new Dictionary<CivilizationId, string>
        {
            { CivilizationId.Chola, "chola" },
            { CivilizationId.Vijayanagara, "vijayanagara" },
            { CivilizationId.Rajput, "rajput" },
            { CivilizationId.Maurya, "maurya" },
            { CivilizationId.Maratha, "maratha" },
        };

        private static readonly Dictionary<CivilizationId, CivilizationProfile> Cache =
            new Dictionary<CivilizationId, CivilizationProfile>();

        public static CivilizationProfile For(CivilizationId id)
        {
            if (Cache.TryGetValue(id, out CivilizationProfile cached))
            {
                return cached;
            }

            CivilizationProfile profile = Build(id);
            Cache[id] = profile;
            return profile;
        }

        private static CivilizationProfile Build(CivilizationId id)
        {
            Color color = Colors[id];
            string civId = CivIds[id];
            CivilizationDefinition def = DataRegistry.GetCivilization(civId);

            if (def == null)
            {
                Debug.LogWarning($"CivilizationProfile: no generated CivilizationDefinition found for '{civId}' - falling back to neutral (1x) multipliers. Run BharatRTS/Generate Data Assets From CSV.");
                return new CivilizationProfile(id.ToString(), color, 1f, 1f, 1f, 1f, 1f);
            }

            return new CivilizationProfile(
                def.displayName, color,
                // GatherRateMultiplier only ever applies to Support-class
                // (worker) code paths regardless of scope, so a bonus the
                // parser correctly scoped to just Support (e.g. Chola's
                // "Workers gather 15% faster") is functionally identical
                // to a civ-wide one here and must count as a match too.
                gatherRateMultiplier: FindMultiplier(def, StatType.ResourceRate, allowCategory: UnitCategory.Support, fallback: 1f),
                buildCostMultiplier: FindMultiplier(def, StatType.ResourceCost, allowCategory: null, fallback: 1f),
                soldierDamageMultiplier: FindMultiplier(def, StatType.Attack, allowCategory: null, fallback: 1f),
                maxHealthMultiplier: FindMultiplier(def, StatType.HP, allowCategory: null, fallback: 1f),
                trainTimeMultiplier: FindMultiplier(def, StatType.TrainTime, allowCategory: null, fallback: 1f));
        }

        // Matches a civ-wide (applyToAllCategories) StatModifier, or - if
        // allowCategory is given - one scoped to exactly that category.
        // A category-specific bonus outside that allowance (e.g. Rajput's
        // Cavalry-only gold-cost discount) isn't a civ-wide multiplier and
        // would misrepresent every other unit type if picked up here; it
        // stays unrepresented in this struct, same as any other narrower
        // bonus (see the class-level comment).
        private static float FindMultiplier(CivilizationDefinition def, StatType stat, UnitCategory? allowCategory, float fallback)
        {
            foreach (StatModifier modifier in def.passiveBonuses)
            {
                if (modifier.stat != stat || modifier.operation != ModifierOp.Multiply)
                {
                    continue;
                }
                if (modifier.applyToAllCategories || (allowCategory.HasValue && !modifier.applyToAllCategories && modifier.targetCategory == allowCategory.Value))
                {
                    return modifier.value;
                }
            }
            return fallback;
        }
    }
}

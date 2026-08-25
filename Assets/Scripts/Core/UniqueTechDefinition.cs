using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Phase 6 (civ asymmetry): one unique, one-time tech per civilization -
    // AoE2's "castle-age unique tech" shape, not another tier of the
    // existing generic Attack/Armor tracks (UpgradeProgress). Delivered
    // through the same Barracks-research plumbing (RequestResearchUniqueTech,
    // same cost/timer/no-blocking shape as RequestResearchAttack/Armor) so
    // this doesn't need a whole new UI/research system - only the effect is
    // new, not the mechanism.
    //
    // Each civ's bonus lives in its own field here rather than a shared
    // "float bonusValue" - explicit fields make it obvious at a glance which
    // civ affects what (Chola's trade rates, Vijayanagara's fortification
    // HP, Rajput's cavalry damage) without a switch statement scattered
    // across every consumer, and every non-owning civ's fields are just the
    // neutral value (0 bonus / 1x multiplier) rather than meaningful.
    //  - Chola: trade-network identity (already gather/build-cost economic)
    //    deepened into their own Market - better buy/sell rates than anyone
    //    else's.
    //  - Vijayanagara: Hampi's actual historical claim to fame is its
    //    fortifications - defensive structures (Wall/Gate/Tower) get
    //    tougher, on top of their existing train-time tempo bonus.
    //  - Rajput: warrior-clan culture, mounted combat specifically - extra
    //    flat Cavalry damage on top of their existing general damage/HP
    //    bonus, since Cavalry is the unit most associated with Rajput
    //    warfare historically.
    public readonly struct UniqueTechDefinition
    {
        public readonly string Name;
        public readonly string Description;
        public readonly float GoldCost;
        public readonly float ResearchTime;

        // Chola: added to Market's sellRate and subtracted from its
        // buyRate - narrows the buy/sell spread symmetrically rather than
        // just making trading free money.
        public readonly float MarketRateBonus;

        // Vijayanagara: multiplies Wall/Gate/Tower max health.
        public readonly float FortificationHealthMultiplier;

        // Rajput: flat additional Cavalry damage, added alongside
        // UpgradeProgress's existing flat/per-class bonuses in
        // CavalryFactory.
        public readonly float CavalryDamageBonus;

        public UniqueTechDefinition(
            string name, string description, float goldCost, float researchTime,
            float marketRateBonus, float fortificationHealthMultiplier, float cavalryDamageBonus)
        {
            Name = name;
            Description = description;
            GoldCost = goldCost;
            ResearchTime = researchTime;
            MarketRateBonus = marketRateBonus;
            FortificationHealthMultiplier = fortificationHealthMultiplier;
            CavalryDamageBonus = cavalryDamageBonus;
        }

        // Bespoke per-civ bonus values (MarketRateBonus/FortificationHealth
        // Multiplier/CavalryDamageBonus) have no structured home in the
        // generated data: civ_bonus_template.csv's UniqueTech cell is one
        // free-text sentence per civ (e.g. "Chola Trade Networks (existing,
        // port as-is: Market spread 70/30 -> 85/15)") that
        // CsvToScriptableObject.cs deliberately keeps as flavor text rather
        // than parsing into a structured effect - these three fields mean
        // three different things per civ, not one generic StatModifier.
        // Name/GoldCost/ResearchTime DO come from the generated TechNode
        // (DataRegistry.GetTech) below; only the bonus values stay
        // hardcoded here, same "bespoke code hook" pattern this project
        // already uses for mechanics too specific for a generic schema.
        private static readonly Dictionary<CivilizationId, (string unitId, string description, float marketRateBonus, float fortificationHealthMultiplier, float cavalryDamageBonus)> Bonuses =
            new Dictionary<CivilizationId, (string, string, float, float, float)>
            {
                { CivilizationId.Chola, ("chola_unique_tech", "Market buy/sell spread narrowed by 15 points either way (70/30 -> 85/15).", 0.15f, 1f, 0f) },
                { CivilizationId.Vijayanagara, ("vijayanagara_unique_tech", "Wall, Gate, and Tower max health increased by 30%.", 0f, 1.3f, 0f) },
                { CivilizationId.Rajput, ("rajput_unique_tech", "Cavalry deal 3 additional damage per hit.", 0f, 1f, 3f) },
            };

        // Pre-migration hardcoded fallback for GoldCost/ResearchTime if the
        // generated TechNode is ever missing - same defensive pattern as
        // everywhere else in this phase. Both were already 150/30 for
        // every civ before this migration, so this is a no-op today.
        private const float FallbackGoldCost = 150f;
        private const float FallbackResearchTime = 30f;

        public static UniqueTechDefinition For(CivilizationId id)
        {
            (string unitId, string description, float marketRateBonus, float fortificationHealthMultiplier, float cavalryDamageBonus) bonus = Bonuses[id];
            TechNode tech = DataRegistry.GetTech(bonus.unitId);
            if (tech != null)
            {
                return new UniqueTechDefinition(
                    tech.displayName, bonus.description, tech.cost.gold, tech.researchTimeSeconds,
                    bonus.marketRateBonus, bonus.fortificationHealthMultiplier, bonus.cavalryDamageBonus);
            }

            Debug.LogWarning($"UniqueTechDefinition: no generated TechNode for '{bonus.unitId}' - using fallback cost/time. Run BharatRTS/Generate Data Assets From CSV.");
            string fallbackName = id switch
            {
                CivilizationId.Chola => "Chola Trade Networks",
                CivilizationId.Vijayanagara => "Hampi Fortifications",
                _ => "Rajput Warrior Clans",
            };
            return new UniqueTechDefinition(
                fallbackName, bonus.description, FallbackGoldCost, FallbackResearchTime,
                bonus.marketRateBonus, bonus.fortificationHealthMultiplier, bonus.cavalryDamageBonus);
        }
    }
}

using System.Collections.Generic;

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

        private static readonly Dictionary<CivilizationId, UniqueTechDefinition> Definitions =
            new Dictionary<CivilizationId, UniqueTechDefinition>
            {
                {
                    CivilizationId.Chola,
                    new UniqueTechDefinition(
                        "Chola Trade Networks",
                        "Market buy/sell spread narrowed by 15 points either way (70/30 -> 85/15).",
                        goldCost: 150f, researchTime: 30f,
                        marketRateBonus: 0.15f, fortificationHealthMultiplier: 1f, cavalryDamageBonus: 0f)
                },
                {
                    CivilizationId.Vijayanagara,
                    new UniqueTechDefinition(
                        "Hampi Fortifications",
                        "Wall, Gate, and Tower max health increased by 30%.",
                        goldCost: 150f, researchTime: 30f,
                        marketRateBonus: 0f, fortificationHealthMultiplier: 1.3f, cavalryDamageBonus: 0f)
                },
                {
                    CivilizationId.Rajput,
                    new UniqueTechDefinition(
                        "Rajput Warrior Clans",
                        "Cavalry deal 3 additional damage per hit.",
                        goldCost: 150f, researchTime: 30f,
                        marketRateBonus: 0f, fortificationHealthMultiplier: 1f, cavalryDamageBonus: 3f)
                },
            };

        public static UniqueTechDefinition For(CivilizationId id)
        {
            return Definitions[id];
        }
    }
}

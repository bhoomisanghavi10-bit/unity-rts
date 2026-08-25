using System.Collections.Generic;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Core
{
    // Metadata for each EconomyTechProgress.EconomyTech - AoE2-style
    // Wheelbarrow/Loom equivalents, all researched at TownCenter rather
    // than Barracks (see EconomyTechProgress). Bonus's meaning depends on
    // which tech it belongs to (a rate multiplier for ImprovedTools/
    // PackMules, a cost multiplier for TradeDiscounts) - each consumer
    // (WorkerFactory, BuildingPlacer) reads the one field relevant to it,
    // same "explicit per-purpose field over a generic value the reader has
    // to already know how to interpret" reasoning UniqueTechDefinition
    // used, just single-field here since each tech only ever affects one
    // thing.
    public readonly struct EconomyTechDefinition
    {
        public readonly string Name;
        public readonly string Description;
        public readonly float WoodCost;
        public readonly float GoldCost;
        public readonly float ResearchTime;
        public readonly float Bonus;

        public EconomyTechDefinition(string name, string description, float woodCost, float goldCost, float researchTime, float bonus)
        {
            Name = name;
            Description = description;
            WoodCost = woodCost;
            GoldCost = goldCost;
            ResearchTime = researchTime;
            Bonus = bonus;
        }

        private static readonly Dictionary<EconomyTech, EconomyTechDefinition> Definitions =
            new Dictionary<EconomyTech, EconomyTechDefinition>
            {
                {
                    EconomyTech.ImprovedTools,
                    new EconomyTechDefinition(
                        "Improved Tools", "Worker gather rate +20%.",
                        woodCost: 100f, goldCost: 50f, researchTime: 25f, bonus: 1.2f)
                },
                {
                    EconomyTech.PackMules,
                    new EconomyTechDefinition(
                        "Pack Mules", "Worker carry capacity +30% (fewer trips to drop off a load).",
                        woodCost: 120f, goldCost: 40f, researchTime: 25f, bonus: 1.3f)
                },
                {
                    EconomyTech.TradeDiscounts,
                    new EconomyTechDefinition(
                        "Trade Discounts", "Building costs -10% (stacks with any civ build-cost bonus).",
                        woodCost: 150f, goldCost: 60f, researchTime: 30f, bonus: 0.9f)
                },
            };

        public static EconomyTechDefinition For(EconomyTech tech)
        {
            return Definitions[tech];
        }
    }
}

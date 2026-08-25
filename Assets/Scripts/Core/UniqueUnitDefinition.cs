using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Core
{
    // Phase 6 (civ asymmetry): one exclusive unit per civilization -
    // AoE2's most common way civs feel distinct in actual play. Same
    // "code-authored delegate over a generic data-driven type" convention
    // ScenarioManager's MissionObjective (item 50) and this session's own
    // TrainCommand/AttackCommand (item 51) already established - Spawn is
    // a direct reference to each civ's own factory method rather than an
    // enum Barracks would need a switch statement to interpret, so adding
    // a 4th civ later needs one new factory + one new dictionary entry
    // here, not new Barracks/BuildMenu/AiController branches.
    public readonly struct UniqueUnitDefinition
    {
        public readonly string Name;
        public readonly float FoodCost;
        public readonly float GoldCost;
        public readonly Func<Vector3, FactionId, GameObject> Spawn;

        public UniqueUnitDefinition(string name, float foodCost, float goldCost, Func<Vector3, FactionId, GameObject> spawn)
        {
            Name = name;
            FoodCost = foodCost;
            GoldCost = goldCost;
            Spawn = spawn;
        }

        private static readonly Dictionary<CivilizationId, UniqueUnitDefinition> Definitions =
            new Dictionary<CivilizationId, UniqueUnitDefinition>
            {
                { CivilizationId.Chola, new UniqueUnitDefinition("Chola Naval Raider", foodCost: 55f, goldCost: 45f, spawn: CholaNavalRaiderFactory.Spawn) },
                { CivilizationId.Vijayanagara, new UniqueUnitDefinition("Vijayanagara War Elephant", foodCost: 120f, goldCost: 90f, spawn: VijayanagaraWarElephantFactory.Spawn) },
                { CivilizationId.Rajput, new UniqueUnitDefinition("Rajput Royal Guard", foodCost: 90f, goldCost: 70f, spawn: RajputRoyalGuardFactory.Spawn) },
            };

        public static UniqueUnitDefinition For(CivilizationId id)
        {
            return Definitions[id];
        }
    }
}

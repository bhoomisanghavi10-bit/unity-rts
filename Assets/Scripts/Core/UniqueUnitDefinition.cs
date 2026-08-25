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
        public readonly float TrainTimeSeconds;
        public readonly Func<Vector3, FactionId, GameObject> Spawn;

        public UniqueUnitDefinition(string name, float foodCost, float goldCost, float trainTimeSeconds, Func<Vector3, FactionId, GameObject> spawn)
        {
            Name = name;
            FoodCost = foodCost;
            GoldCost = goldCost;
            TrainTimeSeconds = trainTimeSeconds;
            Spawn = spawn;
        }

        // civId keys into DataRegistry.GetUnit(...) so cost/train time come
        // from unit_roster_template.csv - Phase 2 migration. Notably,
        // train time is NOT uniform across unique units the way base-
        // roster units are (Vijayanagara War Elephant is 6s vs. everyone
        // else's 5s), so this couldn't just reuse Barracks' shared
        // trainTime field the way the fallback constructor below implies -
        // each entry needs its own value.
        private static readonly Dictionary<CivilizationId, (string civId, string unitId, string name, Func<Vector3, FactionId, GameObject> spawn)> Sources =
            new Dictionary<CivilizationId, (string, string, string, Func<Vector3, FactionId, GameObject>)>
            {
                { CivilizationId.Chola, ("chola", "chola_naval_raider", "Chola Naval Raider", CholaNavalRaiderFactory.Spawn) },
                { CivilizationId.Vijayanagara, ("vijayanagara", "vijayanagara_war_elephant", "Vijayanagara War Elephant", VijayanagaraWarElephantFactory.Spawn) },
                { CivilizationId.Rajput, ("rajput", "rajput_royal_guard", "Rajput Royal Guard", RajputRoyalGuardFactory.Spawn) },
            };

        // Pre-migration hardcoded fallback (matches what shipped before
        // this pass) if the generated UnitDefinition is ever missing - see
        // CivilizationProfile/DataRegistry for the same defensive pattern.
        private static readonly Dictionary<CivilizationId, (float food, float gold, float trainTime)> Fallback =
            new Dictionary<CivilizationId, (float, float, float)>
            {
                { CivilizationId.Chola, (55f, 45f, 5f) },
                { CivilizationId.Vijayanagara, (120f, 90f, 5f) },
                { CivilizationId.Rajput, (90f, 70f, 5f) },
            };

        public static UniqueUnitDefinition For(CivilizationId id)
        {
            (string civId, string unitId, string name, Func<Vector3, FactionId, GameObject> spawn) = Sources[id];
            UnitDefinition def = DataRegistry.GetUnit(unitId);
            if (def != null)
            {
                return new UniqueUnitDefinition(name, def.cost.food, def.cost.gold, def.trainTimeSeconds, spawn);
            }

            Debug.LogWarning($"UniqueUnitDefinition: no generated UnitDefinition for '{unitId}' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            (float food, float gold, float trainTime) fallback = Fallback[id];
            return new UniqueUnitDefinition(name, fallback.food, fallback.gold, fallback.trainTime, spawn);
        }
    }
}

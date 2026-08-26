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
        //
        // Roadmap Section 5 item 3: each civ can have MORE than one unique
        // unit (Maurya/Maratha both have 2 in the CSV) - a plain
        // Dictionary<CivilizationId, single-entry> couldn't represent
        // that, so this is now a per-civ List, indexed by slot. Chola/
        // Vijayanagara/Rajput still only have 1 entry each - their
        // behavior (and Barracks' single unique-unit button) is unchanged.
        private static readonly Dictionary<CivilizationId, List<(string unitId, string name, Func<Vector3, FactionId, GameObject> spawn)>> Sources =
            new Dictionary<CivilizationId, List<(string, string, Func<Vector3, FactionId, GameObject>)>>
            {
                { CivilizationId.Chola, new List<(string, string, Func<Vector3, FactionId, GameObject>)>
                    { ("chola_naval_raider", "Chola Naval Raider", CholaNavalRaiderFactory.Spawn) } },
                { CivilizationId.Vijayanagara, new List<(string, string, Func<Vector3, FactionId, GameObject>)>
                    { ("vijayanagara_war_elephant", "Vijayanagara War Elephant", VijayanagaraWarElephantFactory.Spawn) } },
                { CivilizationId.Rajput, new List<(string, string, Func<Vector3, FactionId, GameObject>)>
                    { ("rajput_royal_guard", "Rajput Royal Guard", RajputRoyalGuardFactory.Spawn) } },
                { CivilizationId.Maurya, new List<(string, string, Func<Vector3, FactionId, GameObject>)>
                    {
                        ("maurya_war_elephant", "Maurya War Elephant", MauryaWarElephantFactory.Spawn),
                        ("pillar_edict_scholar", "Pillar Edict Scholar", PillarEdictScholarFactory.Spawn),
                    } },
                { CivilizationId.Maratha, new List<(string, string, Func<Vector3, FactionId, GameObject>)>
                    {
                        ("maratha_mavla_raider", "Maratha Mavla Raider", MarathaMavlaRaiderFactory.Spawn),
                        ("maratha_durg_garrison", "Maratha Durg Garrison", MarathaDurgGarrisonFactory.Spawn),
                    } },
            };

        // Pre-migration hardcoded fallback (matches what shipped before
        // this pass) if the generated UnitDefinition is ever missing - see
        // CivilizationProfile/DataRegistry for the same defensive pattern.
        // Keyed by (civId, slot) rather than nested per-civ lists - a flat
        // dictionary reads more directly against Sources' per-slot lookup
        // than a matching List<> would.
        private static readonly Dictionary<(CivilizationId, int), (float food, float gold, float trainTime)> Fallback =
            new Dictionary<(CivilizationId, int), (float, float, float)>
            {
                { (CivilizationId.Chola, 0), (55f, 45f, 5f) },
                { (CivilizationId.Vijayanagara, 0), (120f, 90f, 5f) },
                { (CivilizationId.Rajput, 0), (90f, 70f, 5f) },
                { (CivilizationId.Maurya, 0), (130f, 100f, 6f) },
                { (CivilizationId.Maurya, 1), (40f, 10f, 5f) },
                { (CivilizationId.Maratha, 0), (60f, 45f, 5f) },
                { (CivilizationId.Maratha, 1), (50f, 30f, 5f) },
            };

        // Number of unique units the given civ actually has - Barracks/
        // BuildMenu use this to decide whether a second training slot/
        // button should even exist for this civ.
        public static int CountFor(CivilizationId id)
        {
            return Sources.TryGetValue(id, out var list) ? list.Count : 0;
        }

        public static UniqueUnitDefinition For(CivilizationId id, int slot = 0)
        {
            List<(string unitId, string name, Func<Vector3, FactionId, GameObject> spawn)> list = Sources[id];
            (string unitId, string name, Func<Vector3, FactionId, GameObject> spawn) = list[slot];
            UnitDefinition def = DataRegistry.GetUnit(unitId);
            if (def != null)
            {
                return new UniqueUnitDefinition(name, def.cost.food, def.cost.gold, def.trainTimeSeconds, spawn);
            }

            Debug.LogWarning($"UniqueUnitDefinition: no generated UnitDefinition for '{unitId}' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            (float food, float gold, float trainTime) fallback = Fallback[(id, slot)];
            return new UniqueUnitDefinition(name, fallback.food, fallback.gold, fallback.trainTime, spawn);
        }
    }
}

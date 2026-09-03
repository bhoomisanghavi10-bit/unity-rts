using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Match
{
    // Item 50: the 3 built-in scripted missions, one per playable
    // civilization - each fixes the player's civ (a historical mission
    // isn't "pick your side," you play the side the mission is about) and
    // demonstrates a different objective type, so the objective/trigger
    // system in MissionObjective/MissionTrigger/ScenarioManager isn't just
    // infrastructure nobody exercises.
    public static class ScenarioRegistry
    {
        // Item 6 (Scenario Editor, docs/PARTIAL_ELEMENTS_FIX_PLAN.md), light
        // path: the 3 hand-coded missions below stay exactly as they are,
        // plus any CSV-authored missions MissionCsvLoader finds under
        // Resources/Data/Missions/ - MissionSelectMenu.cs already iterates
        // this list directly, so a CSV mission needs no UI change to appear.
        public static readonly List<ScenarioDefinition> All = BuildAll();

        private static List<ScenarioDefinition> BuildAll()
        {
            var list = new List<ScenarioDefinition>
            {
                CholaExpansion(),
                DefendHampi(),
                RajputFrontier(),
            };
            list.AddRange(MissionCsvLoader.LoadAll());
            return list;
        }

        // Objective type: destroy a specific scripted target. The target
        // Barracks is spawned here (in BuildObjectives, not a trigger) so
        // its Attackable reference can be captured directly by the
        // objective's closure - checking "faction has zero Barracks"
        // instead would be trivially true before the AI ever builds one,
        // which isn't what "destroy X" is supposed to mean.
        private static ScenarioDefinition CholaExpansion()
        {
            return new ScenarioDefinition
            {
                Id = "chola_expansion",
                Title = "The Chola Expansion",
                FlavorText = "The Chola fleet has landed. Break the Vijayanagara war machine at its source - raze their Barracks before it fields an army against you.",
                PlayerCivilization = CivilizationId.Chola,
                AiCivilization = CivilizationId.Vijayanagara,
                Map = MapId.RiverValley,
                BuildObjectives = () =>
                {
                    Vector3 targetPosition = new Vector3(3f, 1f, -11f); // just past the AI's default town center
                    GameObject targetGo = BarracksFactory.Place(targetPosition, FactionId.Enemy, 0.01f);
                    targetGo.TryGetComponent(out ConstructionSite site);
                    site.CompleteImmediately();
                    targetGo.TryGetComponent(out Attackable targetAttackable);

                    return new List<MissionObjective>
                    {
                        new MissionObjective(
                            "Destroy the Vijayanagara Barracks",
                            () => targetAttackable == null || targetAttackable.IsDead,
                            completeText: "The Barracks burns. Vijayanagara's war machine is broken before it could march."),
                    };
                },
                BuildTriggers = () =>
                {
                    float startTime = Time.time;
                    return new List<MissionTrigger>
                    {
                        // A one-time reinforcement grant at t=60s - proves
                        // the trigger system does something a player can
                        // actually observe (a real stockpile change), not
                        // just a log line.
                        new MissionTrigger(
                            "chola_reinforcement_gold",
                            () => Time.time - startTime >= 60f,
                            () => ResourceStockpile.For(FactionId.Player).Add(ResourceType.Gold, 150f)),
                    };
                },
                VictoryText = "The Chola banner flies over the ruins of the enemy camp. The expansion holds.",
                DefeatText = "The fleet is scattered and the landing is lost. The Chola expansion ends here.",
            };
        }

        // Objective type: survive N seconds. The simplest possible
        // objective - no target to track, just elapsed time - paired with
        // several one-time triggers to show a repeating-feeling effect
        // built from discrete one-shot triggers (MissionTrigger itself
        // only ever fires once per Id, by design - see its own doc
        // comment on why re-firing would be wrong for a "grant a bonus"
        // trigger).
        private static ScenarioDefinition DefendHampi()
        {
            return new ScenarioDefinition
            {
                Id = "defend_hampi",
                Title = "Defend Hampi",
                FlavorText = "Rajput raiders are probing the Vijayanagara frontier. Hold Hampi for three minutes and the raid will break against your walls.",
                PlayerCivilization = CivilizationId.Vijayanagara,
                AiCivilization = CivilizationId.Rajput,
                Map = MapId.Highlands,
                BuildObjectives = () =>
                {
                    float missionStart = Time.time;
                    const float surviveSeconds = 180f;
                    return new List<MissionObjective>
                    {
                        new MissionObjective(
                            "Survive for 3 minutes",
                            () => Time.time - missionStart >= surviveSeconds,
                            completeText: "The raid breaks against Hampi's walls. The frontier holds, for now."),
                    };
                },
                BuildTriggers = () =>
                {
                    float missionStart = Time.time;
                    var triggers = new List<MissionTrigger>();
                    // Five discrete grants at 30s intervals - "Hampi's
                    // wealth sustains the defense," a small trickle of
                    // Gold to offset the pressure of holding a static
                    // position for 3 minutes instead of expanding.
                    for (int i = 1; i <= 5; i++)
                    {
                        float fireAt = i * 30f;
                        triggers.Add(new MissionTrigger(
                            "hampi_gold_" + i,
                            () => Time.time - missionStart >= fireAt,
                            () => ResourceStockpile.For(FactionId.Player).Add(ResourceType.Gold, 40f)));
                    }
                    return triggers;
                },
                VictoryText = "Hampi stands. The raiders withdraw into the hills, their raid broken.",
                DefeatText = "Hampi falls. The Vijayanagara frontier is breached.",
            };
        }

        // Objective type: own N completed buildings of a given kind - a
        // muster/build-up objective rather than a combat one.
        private static ScenarioDefinition RajputFrontier()
        {
            return new ScenarioDefinition
            {
                Id = "rajput_frontier",
                Title = "The Rajput Frontier",
                FlavorText = "A desert fort is not built with soldiers alone. Muster your strength - raise two Barracks to answer the call to arms against the Chola incursion.",
                PlayerCivilization = CivilizationId.Rajput,
                AiCivilization = CivilizationId.Chola,
                Map = MapId.Coastal,
                BuildObjectives = () =>
                {
                    const int required = 2;
                    return new List<MissionObjective>
                    {
                        new MissionObjective(
                            $"Build {required} Barracks",
                            () => CountCompletedPlayerBarracks() >= required,
                            completeText: "Two Barracks stand at the frontier's edge. The Rajput answer the call to arms."),
                    };
                },
                BuildTriggers = () =>
                {
                    float startTime = Time.time;
                    return new List<MissionTrigger>
                    {
                        // "A supply caravan arrives" - a one-time Wood
                        // bonus at t=45s, offsetting the frontier's harsher
                        // starting position (Coastal splits usable land
                        // with the water rectangle - see item 49).
                        new MissionTrigger(
                            "rajput_supply_caravan",
                            () => Time.time - startTime >= 45f,
                            () => ResourceStockpile.For(FactionId.Player).Add(ResourceType.Wood, 100f)),
                    };
                },
                VictoryText = "The desert fort is manned and ready. The Chola incursion breaks against it.",
                DefeatText = "The frontier could not be armed in time. Chola banners advance unopposed.",
            };
        }

        private static int CountCompletedPlayerBarracks()
        {
            int count = 0;
            foreach (Building building in Building.All)
            {
                if (building is Barracks barracks
                    && barracks.IsComplete
                    && building.TryGetComponent(out FactionMember factionMember)
                    && factionMember.Faction == FactionId.Player)
                {
                    count++;
                }
            }
            return count;
        }
    }
}

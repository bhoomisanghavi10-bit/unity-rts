using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Core
{
    // Faction starting forces, independent of who controls the faction.
    // Previously the Player's Town Center came from TownCenterSpawner and
    // its Workers from UnitSpawner (dropped around the world origin, not the
    // Town Center), while an AI faction spawned both itself from inside
    // AiController - so a human joining as the "AI" slot got nothing, and
    // the two sides did not start equivalently. Now one plan, keyed only on
    // the map start, serves every Human and AI slot.
    public static class StartingForces
    {
        public const int WorkerCount = 4;
        public const float WorkerSpacing = 2f;
        public const float WorkerBehindTownCenter = -3f;

        public sealed class Plan
        {
            public Vector3 TownCenter;
            public readonly List<Vector3> Workers = new List<Vector3>();
        }

        public static Vector3 StartFor(MapDefinitionData map, FactionId faction)
        {
            switch (faction)
            {
                case FactionId.Enemy: return map.EnemyTownCenter;
                case FactionId.Enemy2: return map.Enemy2TownCenter;
                default: return map.PlayerTownCenter;
            }
        }

        // Pure: the Workers' positions relative to the Town Center are the
        // same for every faction.
        public static Plan PlanFor(Vector3 townCenter)
        {
            var plan = new Plan { TownCenter = townCenter };
            for (int i = 0; i < WorkerCount; i++)
            {
                float x = i * WorkerSpacing - (WorkerCount - 1) * WorkerSpacing * 0.5f;
                plan.Workers.Add(townCenter + new Vector3(x, 0f, WorkerBehindTownCenter));
            }

            return plan;
        }

        // Spawns the Town Center and Workers for every playable slot that a
        // custom scenario has not supplied its own placements for.
        public static void SpawnAll(MatchConfiguration configuration)
        {
            MapDefinitionData map = MapRegistry.Get(configuration.Map);
            foreach (MatchSlot slot in configuration.Slots)
            {
                if (slot.Type == SlotType.Closed || CustomScenarioContext.HasPlacementsFor(slot.Faction))
                {
                    continue;
                }

                Plan plan = PlanFor(StartFor(map, slot.Faction));
                TownCenterFactory.Place(plan.TownCenter, slot.Faction);
                foreach (Vector3 position in plan.Workers)
                {
                    WorkerFactory.Spawn(position, slot.Faction);
                }
            }
        }
    }
}

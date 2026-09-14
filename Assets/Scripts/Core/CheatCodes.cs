using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Core
{
    // Wave 6 item 39 (Cheat codes): executes an already-parsed CheatCommand
    // (see CheatCommandParser.cs) against real game state, always targeting
    // FactionId.Player - a single-player testing tool, not a command a
    // network peer could issue. Every mutation here bypasses CommandBus
    // entirely (same "cheat" precedent as SaveManager.SetTotal for save/
    // load, or AgeProgress.Advance itself), so the caller (CheatConsole) is
    // responsible for refusing to call this at all while
    // NetworkMatch.IsActive - lockstep determinism only holds if every peer
    // executes the identical command stream, and a local-only cheat breaks
    // that guarantee outright.
    public static class CheatCodes
    {
        public static string Execute(CheatCommand command)
        {
            if (!command.IsValid)
            {
                return command.Error;
            }

            switch (command.Kind)
            {
                case CheatCommandKind.Help:
                    return "Commands: resources <n>, wood/food/gold/stone <n>, age <name>, reveal, spawn <unitType> [count], win, lose";

                case CheatCommandKind.Resources:
                    return GrantResources(command);

                case CheatCommandKind.Age:
                    AgeProgress.Advance(FactionId.Player, command.Age);
                    return $"Age set to {command.Age}.";

                case CheatCommandKind.Reveal:
                    bool revealing = FogOfWarManager.ToggleRevealAll();
                    return revealing ? "Map revealed." : "Fog of war restored.";

                case CheatCommandKind.Spawn:
                    return SpawnUnits(command.UnitType, command.Count);

                case CheatCommandKind.Win:
                    MatchManager.ForceOutcome(MatchOutcome.Victory);
                    return "Victory forced.";

                case CheatCommandKind.Lose:
                    MatchManager.ForceOutcome(MatchOutcome.Defeat);
                    return "Defeat forced.";

                default:
                    return "Unknown command.";
            }
        }

        private static string GrantResources(CheatCommand command)
        {
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);
            if (stockpile == null)
            {
                return "No active match - resources unavailable.";
            }

            if (command.GrantAllResources)
            {
                foreach (ResourceType type in new[] { ResourceType.Wood, ResourceType.Food, ResourceType.Gold, ResourceType.Stone })
                {
                    stockpile.Add(type, command.Amount);
                }

                return $"Granted {command.Amount} of every resource.";
            }

            stockpile.Add(command.Resource, command.Amount);
            return $"Granted {command.Amount} {command.Resource}.";
        }

        private static string SpawnUnits(string unitType, int count)
        {
            Vector3 origin = FindPlayerSpawnOrigin();
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                if (EntitySpawner.SpawnUnit(unitType, FactionId.Player, origin + offset) != null)
                {
                    spawned++;
                }
            }

            return $"Spawned {spawned} {unitType}.";
        }

        private static Vector3 FindPlayerSpawnOrigin()
        {
            foreach (Building building in Building.All)
            {
                if (building is TownCenter && building.TryGetComponent(out FactionMember member) && member.Faction == FactionId.Player)
                {
                    return building.transform.position + new Vector3(6f, 0f, 0f);
                }
            }

            return Vector3.zero;
        }
    }
}

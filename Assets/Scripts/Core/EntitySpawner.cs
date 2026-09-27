using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Core
{
    // Shared save/scenario spawning boundary. Stable definition IDs use the catalog;
    // legacy type names retain the original factory dispatch during roster migration.
    // The legacy palette intentionally remains limited to its original roster.
    public static class EntitySpawner
    {
        public static readonly string[] BuildingTypes =
        {
            "TownCenter", "Barracks", "Farm", "House", "Wall", "Gate", "Tower", "Market",
        };

        public static readonly string[] UnitTypes =
        {
            "Worker", "Soldier", "Archer", "Cavalry", "Siege",
        };

        public static GameObject SpawnBuilding(string buildingType, FactionId faction, Vector3 position)
        {
            if (IsDefinitionId(buildingType))
                return SpawnDefinition(buildingType, DefinitionKind.Building, faction, position);
            return buildingType switch
            {
                "TownCenter" => TownCenterFactory.Place(position, faction),
                "Barracks" => BarracksFactory.Place(position, faction, 0.01f),
                "Farm" => FarmFactory.Place(position, faction, 0.01f),
                "House" => HouseFactory.Place(position, faction, 0.01f),
                "Wall" => WallFactory.Place(position, faction, 0.01f),
                "Gate" => GateFactory.Place(position, faction, 0.01f),
                "Tower" => TowerFactory.Place(position, faction, 0.01f),
                "Market" => MarketFactory.Place(position, faction, 0.01f),
                _ => null,
            };
        }

        public static GameObject SpawnUnit(string unitType, FactionId faction, Vector3 position)
        {
            if (IsDefinitionId(unitType))
                return SpawnDefinition(unitType, DefinitionKind.Unit, faction, position);
            return unitType switch
            {
                "Worker" => WorkerFactory.Spawn(position, faction),
                "Soldier" => SoldierFactory.Spawn(position, faction),
                "Archer" => ArcherFactory.Spawn(position, faction),
                "Cavalry" => CavalryFactory.Spawn(position, faction),
                "Siege" => SiegeFactory.Spawn(position, faction),
                _ => null,
            };
        }

        // Legacy names remain adapters during the roster migration. Namespaced IDs
        // are strict: a typo must never silently fall through to a generic factory.
        internal static bool IsDefinitionId(string id) => id != null && id.Contains(".");

        private static GameObject SpawnDefinition(string id, DefinitionKind kind, FactionId faction, Vector3 position)
        {
            ValidateDefinition(id, kind);
            return DefinitionCatalog.Default.Spawn(id, position, faction);
        }

        internal static void ValidateDefinition(string id, DefinitionKind kind)
        {
            EntityDefinition definition = DefinitionCatalog.Default.Get(id);
            if (definition.Kind != kind)
                throw new System.ArgumentException($"Definition '{id}' is {definition.Kind}, expected {kind}.", nameof(id));
        }
    }
}

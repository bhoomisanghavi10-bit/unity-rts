using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Core
{
    // Item 6 (Scenario Editor, heavy path session 1): the type-string ->
    // factory-call dispatch that used to live only inside SaveManager's
    // RestoreUnits/RestoreBuildings, extracted so both save/load and the new
    // custom-scenario placement system (ScenarioEditorMenu/
    // CivilizationSetup.BeginCustomScenarioMatch) share one source of truth
    // instead of two copies drifting apart. Pure extraction - same cases,
    // same factory calls, same behavior SaveManager already had.
    //
    // The type list here is deliberately the same (restricted) roster
    // SaveManager already supported, not the full building/unit roster this
    // project has elsewhere (e.g. no Dock/LumberCamp/MiningCamp/Mill, no
    // FishingBoat/WarGalley) - extending that roster is a separate,
    // pre-existing gap, not something this refactor silently expands.
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
    }
}

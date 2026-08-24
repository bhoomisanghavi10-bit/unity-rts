using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Item 44: which map layout a match uses. RiverValley matches every
    // value ProceduralGround/ResourceNodeSpawner/NavMeshBaker/
    // TownCenterSpawner/AiController already hardcoded before this file
    // existed, so it's the default and changes nothing for anyone who
    // doesn't pick Highlands. Highlands is the actual "second map": a
    // larger, hillier layout with stone/gold favored over food/wood and
    // starting bases pushed further apart.
    public enum MapId
    {
        RiverValley,
        Highlands,
    }

    // Plain data, not a MonoBehaviour/ScriptableObject - every field here
    // just mirrors a [SerializeField] that already existed on some other
    // component, collected in one place so a single MapId choice can drive
    // all of them consistently instead of editing five Inspector fields by
    // hand to change map.
    public class MapDefinitionData
    {
        public float GroundSize;
        public int GroundResolution;
        public float NoiseHeight;
        public float NoiseScale;

        public int TreeCount;
        public int FarmCount;
        public int GoldCount;
        public int StoneCount;
        public int FruitBushCount;
        public float ResourceMinRadius;
        public float ResourceMaxRadius;

        // -1 means "pick a fresh seed every match" (System.Environment.
        // TickCount) instead of the old fixed-seed-forever behavior - see
        // ResourceNodeSpawner. A map can still pin a specific seed if a
        // reproducible layout is ever wanted again.
        public int ResourceSeed;

        public Vector3 PlayerTownCenter;
        public Vector3 EnemyTownCenter;
        // Item 48: a 3rd starting position for an optional second AI
        // faction (Enemy2) - unused unless CivilizationSetup's
        // enableThirdFaction is on, same "additive, changes nothing by
        // default" pattern as everything else in this struct.
        public Vector3 Enemy2TownCenter;
        public Vector3 NavMeshBoundsSize;
    }

    // Faction civ choice has one assignment per match (CivilizationRegistry);
    // map choice has exactly one value for the whole match, so this is
    // simpler - a single Current definition, defaulted to RiverValley so
    // any code path that runs before CivilizationSetup.BeginMatch selects
    // one (or in a test/harness context that never calls it at all) still
    // gets today's exact numbers.
    public static class MapRegistry
    {
        private static readonly Dictionary<MapId, MapDefinitionData> Definitions = new Dictionary<MapId, MapDefinitionData>
        {
            [MapId.RiverValley] = new MapDefinitionData
            {
                GroundSize = 40f,
                GroundResolution = 40,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 8,
                FarmCount = 5,
                GoldCount = 5,
                StoneCount = 5,
                FruitBushCount = 6,
                ResourceMinRadius = 6f,
                ResourceMaxRadius = 16f,
                ResourceSeed = -1,
                PlayerTownCenter = new Vector3(0f, 1f, 8f),
                EnemyTownCenter = new Vector3(0f, 1f, -8f),
                Enemy2TownCenter = new Vector3(10f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(44f, 10f, 44f),
            },
            [MapId.Highlands] = new MapDefinitionData
            {
                GroundSize = 52f,
                GroundResolution = 52,
                NoiseHeight = 1.1f,
                NoiseScale = 0.12f,
                TreeCount = 6,
                FarmCount = 4,
                GoldCount = 8,
                StoneCount = 8,
                FruitBushCount = 4,
                ResourceMinRadius = 7f,
                ResourceMaxRadius = 21f,
                ResourceSeed = -1,
                PlayerTownCenter = new Vector3(0f, 1f, 11f),
                EnemyTownCenter = new Vector3(0f, 1f, -11f),
                Enemy2TownCenter = new Vector3(14f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(58f, 12f, 58f),
            },
        };

        public static MapId CurrentId { get; private set; } = MapId.RiverValley;
        public static MapDefinitionData Current { get; private set; } = Definitions[MapId.RiverValley];

        public static void Select(MapId id)
        {
            CurrentId = id;
            Current = Definitions[id];
        }
    }
}

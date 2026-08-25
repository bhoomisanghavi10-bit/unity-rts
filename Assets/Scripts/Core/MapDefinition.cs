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
        // Item 49: the first map with actual water - a strip along the
        // east edge, land (and every town center/resource) kept west of
        // it so existing gameplay assumptions (everyone starts on solid
        // ground) still hold; water is there for whoever chooses to build
        // a Dock and go naval, not a requirement.
        Coastal,
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

        // Item 49: an axis-aligned water rectangle in world XZ (Y unused).
        // Zero half-extents (the default, RiverValley/Highlands never set
        // this) means "no water" - ProceduralGround checks for exactly
        // that before doing anything water-related, so those two maps are
        // completely unaffected by this feature's existence.
        public Vector3 WaterCenter;
        public Vector3 WaterHalfExtents;
        // Item 49: how many Fish nodes (Food, gathered by Fishing Boats)
        // to scatter inside the water rectangle - 0 for RiverValley/
        // Highlands (no water to put them in anyway).
        public int FishCount;
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
            // Phase 5 map-scale-up (2026-08-25, user-directed: "raise map
            // size toward real RTS scale"). All 3 maps were tiny by
            // RTS-genre standards - AoE2's smallest selectable size
            // ("Tiny") is 120x120, bigger than this game's old largest map
            // (Highlands, 52). Every distance-scaling field below (ground/
            // NavMesh footprint, resource ring radii, town center/water
            // placement) is the old value x2.5, landing the 3 maps at
            // 100/130/125 - close to AoE2 Tiny while keeping each map's
            // existing relative sizing (Highlands still largest, Coastal
            // still in between). NoiseHeight/NoiseScale are deliberately
            // NOT scaled - they're vertical amplitude and horizontal
            // noise frequency, independent of footprint size; scaling
            // them would just stretch/flatten the existing terrain look
            // rather than adding more of it. Resource counts (not a
            // distance) are scaled x1.5, not x2.5 - map *area* scales by
            // 2.5^2=6.25x, and matching that would mean 6.25x the node
            // count (visual clutter, plus every AI worker-assignment scan
            // over ResourceNode.All getting proportionally slower) for no
            // real gameplay benefit; x1.5 keeps a bigger map from feeling
            // emptier than before without drowning it in nodes. Unit
            // movement speeds are deliberately left untouched - a genuine
            // separate balance question (how much longer a match *should*
            // take to walk across a bigger map), not a distance-scaling
            // one, and item 43's counter-triangle tuning already leans on
            // today's exact speed values; flagged as a follow-up for that
            // ongoing balance pass rather than changed blind here.
            [MapId.RiverValley] = new MapDefinitionData
            {
                GroundSize = 100f,
                GroundResolution = 100,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 12,
                FarmCount = 8,
                GoldCount = 8,
                StoneCount = 8,
                FruitBushCount = 9,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 40f,
                ResourceSeed = -1,
                PlayerTownCenter = new Vector3(0f, 1f, 20f),
                EnemyTownCenter = new Vector3(0f, 1f, -20f),
                Enemy2TownCenter = new Vector3(25f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(110f, 10f, 110f),
            },
            [MapId.Highlands] = new MapDefinitionData
            {
                GroundSize = 130f,
                GroundResolution = 130,
                NoiseHeight = 1.1f,
                NoiseScale = 0.12f,
                TreeCount = 9,
                FarmCount = 6,
                GoldCount = 12,
                StoneCount = 12,
                FruitBushCount = 6,
                ResourceMinRadius = 17.5f,
                ResourceMaxRadius = 52.5f,
                ResourceSeed = -1,
                PlayerTownCenter = new Vector3(0f, 1f, 27.5f),
                EnemyTownCenter = new Vector3(0f, 1f, -27.5f),
                Enemy2TownCenter = new Vector3(35f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(145f, 12f, 145f),
            },
            // Item 49: water is a strip along the east edge - land, every
            // town center, and the resource ring all stay west of it so
            // nothing about existing gameplay assumes water is there
            // unless a player actually goes looking for it (or builds a
            // Dock). Phase 5: water rect scaled x2.5 along with everything
            // else here, preserving the original 2-unit buffer between
            // ResourceMaxRadius and the water's inner edge (scaled to 5).
            [MapId.Coastal] = new MapDefinitionData
            {
                GroundSize = 125f,
                GroundResolution = 125,
                NoiseHeight = 0.7f,
                NoiseScale = 0.14f,
                TreeCount = 11,
                FarmCount = 8,
                GoldCount = 9,
                StoneCount = 9,
                FruitBushCount = 8,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 32.5f,
                ResourceSeed = -1,
                PlayerTownCenter = new Vector3(0f, 1f, 25f),
                EnemyTownCenter = new Vector3(0f, 1f, -25f),
                Enemy2TownCenter = new Vector3(-25f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(135f, 10f, 135f),
                WaterCenter = new Vector3(50f, 0f, 0f),
                WaterHalfExtents = new Vector3(12.5f, 0f, 62.5f),
                FishCount = 8,
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

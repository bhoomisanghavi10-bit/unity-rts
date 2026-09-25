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
        // First skirmish map built on baked Vista terrain: 158 x 158 tiles
        // with the home / contested / dead-zone layout of
        // docs/SKIRMISH_MAP_SPEC.md (see SkirmishMapZones). Re-baked from
        // the Mesa template to actually be the "Crossroad Valleys"
        // archetype (see docs/SKIRMISH_MAP_SPEC.md's Layout styles table) -
        // kept as this same enum member/resource path rather than renamed,
        // since nothing else needed to change to complete it.
        SkirmishMedium,
        // The other 4 of the 5 named skirmish archetypes - same 158x158
        // footprint/starts/economy as SkirmishMedium, differing only in
        // their baked heightmap and (Divided Riverbed/Clearing) a couple
        // of MapDefinitionData fields. See docs/SKIRMISH_MAP_SPEC.md.
        SkirmishDividedRiverbed,
        SkirmishMountainPass,
        SkirmishHighlandFoothills,
        SkirmishClearing,
        // Small/large map sizes item (docs/SKIRMISH_MAP_SPEC.md): the same
        // Crossroad Valleys recipe as SkirmishMedium, baked at a smaller/
        // larger footprint (VistaSpike.SmallMapSize/LargeMapSize) instead
        // of a new style - proves the zoning/resource/relic/water-mask
        // scaling math (SkirmishMapScaling, RelicPlacement.ComputeRelicCount,
        // WaterBasinFinder) generalizes to a genuinely different map size,
        // not just a genuinely different layout.
        SkirmishSmall,
        SkirmishLarge,
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
        // Wave 6 item 35 (Relics). RiverValley/Highlands/Coastal keep a
        // flat literal (5) - out of scope for the zoning framework below.
        // The 5 skirmish (UsesZoning) maps compute this via
        // RelicPlacement.ComputeRelicCount(ContestedWidth) instead of a
        // hand-picked constant, so a future differently-sized layout scales
        // automatically (docs/SKIRMISH_MAP_SPEC.md item 5) rather than
        // silently inheriting today's 158x158 value.
        public int RelicCount;

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

        // Optional: a baked heightmap (Resources path, no extension, see
        // BakedHeightmap) that ProceduralTerrain uses instead of its Perlin
        // ground. Null/empty keeps the procedural terrain.
        public string BakedHeightmapResource;

        // Divided Riverbed: world-X centres of fordable gaps in the water
        // band (see RiverFords.FordFactor), each FordHalfWidth wide before
        // falloff. Empty (every map except Divided Riverbed) means no
        // fords exist - ProceduralTerrain/NavMeshBaker's ford-aware code
        // paths are skipped entirely, so this is a no-op everywhere else.
        public float[] FordCentersX = System.Array.Empty<float>();
        public float FordHalfWidth = 4f;

        // Forest-tile classification (see ResourceNodeSpawner.ClassifyTile).
        // Default matches the component's own long-standing Inspector
        // default exactly, so every map that doesn't override this behaves
        // identically to before this field existed.
        public float ForestThreshold = 0.58f;
        // Clearing: half-width of a guaranteed-clear lane between every
        // pair of active town centres (see ForestLanes). 0 (every other
        // map) disables the lane check entirely.
        public float ForestLaneWidth = 0f;

        // Optional: a baked feature mask (same binary format/loader as
        // BakedHeightmapResource, via BakedHeightmap with heightScale=1) -
        // 1 where a style's signature terrain feature is strongest (the
        // Mountain Pass ridge, a Crossroad Valleys mesa top), 0 elsewhere.
        // Null/empty (every map without one) disables both consumers below
        // entirely. See SkirmishTerrainCarving's Compute*Mask functions.
        public string BakedMaskResource;
        // Which ProceduralTerrain layer index the mask boosts (0=Grass,
        // 1=Dirt, 2=Rock, 3=Sand, 4=Pebbles - see ProceduralTerrain.
        // ApplyLayers). Unused when BakedMaskResource is null/empty.
        public int MaskTerrainLayerIndex;
        // Whether ResourceNodeSpawner's Gold/Stone placement should bias
        // toward the mask (a "quarry in the mountains" feel) rather than a
        // plain uniform ring. Unused when BakedMaskResource is null/empty.
        public bool BiasResourcesToMask;

        // Whether this map follows docs/SKIRMISH_MAP_SPEC.md's home/
        // contested/dead-zone rules (SkirmishMapZones). True only for the 5
        // 158x158 skirmish maps - RiverValley/Highlands/Coastal are far
        // smaller and were never sized for that zoning (SkirmishMapZones'
        // fixed 3/40-tile bands would swallow most of a 100-unit map), so
        // ResourceNodeSpawner keeps its old unconstrained ring placement
        // for them. When true: ResourceNodeSpawner guarantees a starting
        // woodline + a small primary gold node 10-15 units from every town
        // centre, and confines the general gold/stone/farm/fruit/relic ring
        // to the Contested zone.
        public bool UsesZoning;
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
                RelicCount = 5,
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
                RelicCount = 5,
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
                RelicCount = 5,
                PlayerTownCenter = new Vector3(0f, 1f, 25f),
                EnemyTownCenter = new Vector3(0f, 1f, -25f),
                Enemy2TownCenter = new Vector3(-25f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(135f, 10f, 135f),
                WaterCenter = new Vector3(50f, 0f, 0f),
                WaterHalfExtents = new Vector3(12.5f, 0f, 62.5f),
                FishCount = 8,
            },
            // Medium skirmish map (158 x 158). Starts sit in the middle of
            // the 40-tile home ring (|z| = 57 is the centre of the 39..76
            // band); terrain is baked out of a Vista graph.
            [MapId.SkirmishMedium] = new MapDefinitionData
            {
                GroundSize = 158f,
                UsesZoning = true,
                GroundResolution = 158,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 20,
                FarmCount = 10,
                GoldCount = 12,
                StoneCount = 10,
                FruitBushCount = 12,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 40f,
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(158f)),
                PlayerTownCenter = new Vector3(0f, 1f, 57f),
                EnemyTownCenter = new Vector3(0f, 1f, -57f),
                Enemy2TownCenter = new Vector3(-57f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(170f, 30f, 170f),
                BakedHeightmapResource = "Maps/SkirmishMedium/height",
                BakedMaskResource = "Maps/SkirmishMedium/mask",
                MaskTerrainLayerIndex = 3, // Sand - mesas read as sandstone buttes.
                BiasResourcesToMask = true,
                // Water mask item (docs/SKIRMISH_MAP_SPEC.md): found by
                // VistaSpike.ReportWaterBasins scanning the already-baked
                // heightmap for the flattest, lowest Contested-zone
                // rectangle - a small lake tucked in the valley between the
                // 2 mesas on the -X side, not hand-picked.
                WaterCenter = new Vector3(-25f, 0f, 0f),
                WaterHalfExtents = new Vector3(14f, 0f, 10f),
                FishCount = 4,
            },
            // Divided Riverbed: same footprint/economy as SkirmishMedium,
            // a full-width water band across X at Z=0 (WaterHalfExtents.x
            // reaches the map edge, so ProceduralTerrain auto-extends it -
            // see ComputeWaterRect) with 3 fordable gaps. Enemy2's start is
            // moved off the river centreline (z=0 would otherwise sit
            // inside the water rect) - the 3-start layout on a themed map
            // is still an open question generally (see
            // docs/SKIRMISH_MAP_SPEC.md's Open questions), this is just the
            // one concrete fix this map needs.
            [MapId.SkirmishDividedRiverbed] = new MapDefinitionData
            {
                GroundSize = 158f,
                UsesZoning = true,
                GroundResolution = 158,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 20,
                FarmCount = 10,
                GoldCount = 12,
                StoneCount = 10,
                FruitBushCount = 12,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 40f,
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(158f)),
                PlayerTownCenter = new Vector3(0f, 1f, 57f),
                EnemyTownCenter = new Vector3(0f, 1f, -57f),
                Enemy2TownCenter = new Vector3(-57f, 1f, 30f),
                NavMeshBoundsSize = new Vector3(170f, 30f, 170f),
                BakedHeightmapResource = "Maps/SkirmishDividedRiverbed/height",
                WaterCenter = new Vector3(0f, 0f, 0f),
                WaterHalfExtents = new Vector3(79f, 0f, 6f),
                FishCount = 6,
                FordCentersX = new[] { -40f, 0f, 40f },
                FordHalfWidth = 4f,
            },
            // Mountain Pass: a mountain chain across the map (a raised Z
            // band, see SkirmishTerrainCarving.ApplyRidgeWithPass) with one
            // central corridor at X=0 - directly between Player and Enemy,
            // who both already sit at x=0.
            [MapId.SkirmishMountainPass] = new MapDefinitionData
            {
                GroundSize = 158f,
                UsesZoning = true,
                GroundResolution = 158,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 20,
                FarmCount = 10,
                GoldCount = 12,
                StoneCount = 10,
                FruitBushCount = 12,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 40f,
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(158f)),
                PlayerTownCenter = new Vector3(0f, 1f, 57f),
                EnemyTownCenter = new Vector3(0f, 1f, -57f),
                Enemy2TownCenter = new Vector3(-57f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(170f, 30f, 170f),
                BakedHeightmapResource = "Maps/SkirmishMountainPass/height",
                BakedMaskResource = "Maps/SkirmishMountainPass/mask",
                MaskTerrainLayerIndex = 2, // Rock - the chain reads as exposed stone.
                BiasResourcesToMask = true,
                // Water mask item: a small mountain lake found on the
                // Player's side of the map, off to one side of the ridge -
                // well clear of the pass corridor's own Z-band (the ridge/
                // pass only shapes terrain near Z=0; this lake sits at
                // Z=31, past even the ridge's falloff), confirmed via
                // VistaSpike.ReportWaterBasins against the real baked
                // heightmap, not placed by eye.
                WaterCenter = new Vector3(13f, 0f, 31f),
                WaterHalfExtents = new Vector3(8f, 0f, 8f),
                FishCount = 3,
            },
            // Highland Foothills: rolling terraced highlands (see
            // SkirmishTerrainCarving.ApplyTerracing).
            [MapId.SkirmishHighlandFoothills] = new MapDefinitionData
            {
                GroundSize = 158f,
                UsesZoning = true,
                GroundResolution = 158,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 20,
                FarmCount = 10,
                GoldCount = 12,
                StoneCount = 10,
                FruitBushCount = 12,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 40f,
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(158f)),
                PlayerTownCenter = new Vector3(0f, 1f, 57f),
                EnemyTownCenter = new Vector3(0f, 1f, -57f),
                Enemy2TownCenter = new Vector3(-57f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(170f, 30f, 170f),
                BakedHeightmapResource = "Maps/SkirmishHighlandFoothills/height",
                // Water mask item: a highland tarn found by
                // VistaSpike.ReportWaterBasins in a genuinely low terrace
                // of the baked terrain (max height 1.14m against an 11.10m
                // map peak), not hand-picked.
                WaterCenter = new Vector3(25f, 0f, 25f),
                WaterHalfExtents = new Vector3(14f, 0f, 10f),
                FishCount = 4,
            },
            // Clearing: dense forest (much lower ForestThreshold) with 3
            // guaranteed-clear lanes joining every pair of active town
            // centres (see ForestLanes, ResourceNodeSpawner.ClassifyTile).
            [MapId.SkirmishClearing] = new MapDefinitionData
            {
                GroundSize = 158f,
                UsesZoning = true,
                GroundResolution = 158,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = 20,
                FarmCount = 10,
                GoldCount = 12,
                StoneCount = 10,
                FruitBushCount = 12,
                ResourceMinRadius = 15f,
                ResourceMaxRadius = 40f,
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(158f)),
                PlayerTownCenter = new Vector3(0f, 1f, 57f),
                EnemyTownCenter = new Vector3(0f, 1f, -57f),
                Enemy2TownCenter = new Vector3(-57f, 1f, 0f),
                NavMeshBoundsSize = new Vector3(170f, 30f, 170f),
                BakedHeightmapResource = "Maps/SkirmishClearing/height",
                ForestThreshold = 0.3f,
                ForestLaneWidth = 7f,
                // Water mask item: a small clearing pond found by
                // VistaSpike.ReportWaterBasins - Clearing's Dunes base is
                // gentle everywhere (2.71m map peak), so this is the
                // genuinely flattest/lowest spot rather than a dramatic
                // carve, matching the style's own "dense forest, small
                // natural clearings" identity.
                WaterCenter = new Vector3(-20f, 0f, 25f),
                WaterHalfExtents = new Vector3(14f, 0f, 10f),
                FishCount = 4,
            },
            // Small/large map sizes item: the Crossroad Valleys recipe
            // (VistaSpike.CrossroadValleysSmall/Large) baked at a smaller/
            // larger footprint. Every count/radius/start-position field
            // below is *computed* from this map's own Contested-zone width
            // via SkirmishMapScaling/RelicPlacement, not hand-copied from
            // SkirmishMedium's numbers, so these two entries double as a
            // live proof the scaling math generalizes beyond one map size.
            [MapId.SkirmishSmall] = new MapDefinitionData
            {
                GroundSize = 120f,
                UsesZoning = true,
                GroundResolution = 120,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = SkirmishMapScaling.ScaleCount(20, SkirmishMapZones.ContestedWidth(120f)),
                FarmCount = SkirmishMapScaling.ScaleCount(10, SkirmishMapZones.ContestedWidth(120f)),
                GoldCount = SkirmishMapScaling.ScaleCount(12, SkirmishMapZones.ContestedWidth(120f)),
                StoneCount = SkirmishMapScaling.ScaleCount(10, SkirmishMapZones.ContestedWidth(120f)),
                FruitBushCount = SkirmishMapScaling.ScaleCount(12, SkirmishMapZones.ContestedWidth(120f)),
                ResourceMinRadius = SkirmishMapScaling.ScaleRadius(15f, SkirmishMapZones.ContestedWidth(120f)),
                ResourceMaxRadius = SkirmishMapScaling.ScaleRadius(40f, SkirmishMapZones.ContestedWidth(120f)),
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(120f)),
                PlayerTownCenter = new Vector3(0f, 1f, SkirmishMapScaling.HomeBandMidpoint(120f)),
                EnemyTownCenter = new Vector3(0f, 1f, -SkirmishMapScaling.HomeBandMidpoint(120f)),
                Enemy2TownCenter = new Vector3(-SkirmishMapScaling.HomeBandMidpoint(120f), 1f, 0f),
                NavMeshBoundsSize = new Vector3(132f, 30f, 132f),
                BakedHeightmapResource = "Maps/SkirmishSmall/height",
                BakedMaskResource = "Maps/SkirmishSmall/mask",
                MaskTerrainLayerIndex = 3, // Sand - mesas read as sandstone buttes, same as SkirmishMedium.
                BiasResourcesToMask = true,
                // Water mask item: a small pond found by VistaSpike.
                // ReportWaterBasins right in the gap between the 4
                // (correctly-proportioned) mesas - a first attempt at this
                // recipe kept mesa radius/falloff absolute while shrinking
                // only their centre offset, which made the 4 mesas overlap
                // at this size and left no room for a basin at all; fixed
                // by scaling mesa footprint proportionally too (see
                // VistaSpike.CrossroadValleysAtSize), which both fixed the
                // overlap and incidentally opened up this pond.
                WaterCenter = new Vector3(0f, 0f, 2.6f),
                WaterHalfExtents = new Vector3(7.2f, 0f, 5.1f),
                FishCount = 2,
            },
            [MapId.SkirmishLarge] = new MapDefinitionData
            {
                GroundSize = 240f,
                UsesZoning = true,
                GroundResolution = 240,
                NoiseHeight = 0.6f,
                NoiseScale = 0.15f,
                TreeCount = SkirmishMapScaling.ScaleCount(20, SkirmishMapZones.ContestedWidth(240f)),
                FarmCount = SkirmishMapScaling.ScaleCount(10, SkirmishMapZones.ContestedWidth(240f)),
                GoldCount = SkirmishMapScaling.ScaleCount(12, SkirmishMapZones.ContestedWidth(240f)),
                StoneCount = SkirmishMapScaling.ScaleCount(10, SkirmishMapZones.ContestedWidth(240f)),
                FruitBushCount = SkirmishMapScaling.ScaleCount(12, SkirmishMapZones.ContestedWidth(240f)),
                ResourceMinRadius = SkirmishMapScaling.ScaleRadius(15f, SkirmishMapZones.ContestedWidth(240f)),
                ResourceMaxRadius = SkirmishMapScaling.ScaleRadius(40f, SkirmishMapZones.ContestedWidth(240f)),
                ResourceSeed = -1,
                RelicCount = RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(240f)),
                PlayerTownCenter = new Vector3(0f, 1f, SkirmishMapScaling.HomeBandMidpoint(240f)),
                EnemyTownCenter = new Vector3(0f, 1f, -SkirmishMapScaling.HomeBandMidpoint(240f)),
                Enemy2TownCenter = new Vector3(-SkirmishMapScaling.HomeBandMidpoint(240f), 1f, 0f),
                NavMeshBoundsSize = new Vector3(252f, 30f, 252f),
                BakedHeightmapResource = "Maps/SkirmishLarge/height",
                BakedMaskResource = "Maps/SkirmishLarge/mask",
                MaskTerrainLayerIndex = 3, // Sand - mesas read as sandstone buttes, same as SkirmishMedium.
                BiasResourcesToMask = true,
                // Water mask item: a lake found by VistaSpike.
                // ReportWaterBasins, landing at almost exactly 2x
                // SkirmishMedium's own (-25, 0) lake position, matching
                // this map's own ~2.05x Contested-half-width ratio - the
                // scaling math reproducing a proportionally consistent
                // result, not a coincidence.
                WaterCenter = new Vector3(-51.3f, 0f, 0f),
                WaterHalfExtents = new Vector3(28.7f, 0f, 20.5f),
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

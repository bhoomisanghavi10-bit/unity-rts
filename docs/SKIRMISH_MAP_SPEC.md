# Skirmish map spec — medium (158 x 158)

Source: user-supplied design logic, confirmed 2026-09-21 ("this is the correct logic").
Applies first to the **medium** map; small and large sizes come after the medium
pipeline works.

## Coordinates
- 1 world unit = 1 tile. The medium map is **158 x 158 tiles** (units), a single Unity
  Terrain "Ground" centred on the world origin (spans -79..+79 on X and Z).
- Zones are concentric squares measured from the centre by Chebyshev distance
  `d = max(|x|, |z|)`.

```
 along one axis (158 total)
 |3 dead|  37 home  |        78 contested        |  37 home  |3 dead|
 |<-------- 40 home buffer -->|                   |<-- 40 home buffer -------->|
```

| Zone | Rule (d from centre) | Width | Purpose |
|---|---|---|---|
| Edge dead zone | d >= 76 | outer 3 tiles | Nothing spawns: no stuck units on the world edge, no camera clipping |
| Home base buffer | 39 <= d < 76 | outer 40 tiles per side (37 spawnable) | Player start positions; guaranteed starting woodline and a small primary gold node within 10-15 units of the Town Center |
| Central contested | d < 39 | middle 78 x 78 | High-risk / high-reward: large gold veins, stone quarries, choke mountains, contestable items (Relics, holy sites) |

The 3-tile dead zone sits *inside* the 40-tile home buffer (3 + 37 + 78 + 37 + 3 = 158).

## Rules the generators and spawners must obey
1. Player start coordinates are locked to the home base buffer.
2. Every start gets a **starting woodline** and a small **primary gold node** placed
   10-15 units from its Town Center (`SkirmishMapZones.IsWithinStartingResourceRange`).
3. Large gold veins, stone quarries, choke terrain and Relics go in the contested zone.
4. Nothing spawns in the dead zone (`SkirmishMapZones.IsSpawnable`).
5. Slopes must stay walkable for the NavMesh (agentSlope 45, agentClimb 0.75).

## Code
- `Assets/Scripts/Core/SkirmishMapZones.cs` — the single definition
  (`Classify`, `IsSpawnable`, `IsWithinStartingResourceRange`, constants), unit tested in
  `SkirmishMapZonesTests`. Nothing calls it from gameplay yet.

## Where it plugs in (planned, not built)
- Vista graph: derive the home / contested / dead-zone masks from the same numbers
  (start plateaus flattened in the home ring, falloff at the edge).
- `ResourceNodeSpawner` / a skirmish `MapDefinitionData`: start positions, woodline and
  primary gold from the home ring; large gold, stone, relics from the contested square.
- Fairness check: equal starting resources per player, walkable path between starts.

## Layout styles (proposed 2026-09-21, from the user's blueprint)
Five tactical archetypes on the same 158 x 158 canvas, meant for both Skirmish and Campaign
maps. Each becomes **one baked map** (a Vista graph + gameplay masks), not a runtime tile
grid - see "How the blueprint maps onto our architecture" below.

| Style | Idea |
|---|---|
| Crossroad Valleys | Open and balanced; mesa plateaus as tactical corners, wood clusters via noise |
| Divided Riverbed | Naval / crossings: a river across the middle with a few fords, woodlines on the banks |
| Mountain Pass | Chokepoint control: a mountain chain across the map with one central pass |
| Highland Foothills | Rolling terraced highlands, cliff ridges, dense basin woods |
| Clearing | Dense forest with three winding lanes joining the bases |

Blueprint start positions are grid cells (30, 30) and (128, 128) in a 0..157 corner-origin grid,
i.e. world (-49, -49) and (+49, +49) here: opposite diagonal corners of the home ring
(Chebyshev 49, inside 39..76). Grid -> world is `world = grid - 79`.

### How the blueprint maps onto our architecture
The pasted `LayoutGenerator.cs` targets types that don't exist here (`MapData`, `TileType`,
`RTSLevelDirector`, `RTSResourceNodeSpawner`) and a separate tile matrix. Ours:
- shape and walkability come from the **terrain** (baked Vista heights + NavMesh), not a tile array;
  "blocked" tiles become mountains / water / dense forest in the heightmap and masks;
- placement rules come from `SkirmishMapZones` and the map definition, not a separate director;
- generation is **baked offline** (deterministic for lockstep), so the blueprint's global
  `Random.InitState(seed)` isn't used at match time; seeds live in the Vista graph nodes;
- base protection = the start plateaus `ProceduralTerrain` already levels around each Town Center
  (flat to 14 units, easing out by 30).
Items worth keeping from the blueprint: the five archetypes, guaranteed base safe-zones, fords and
a single central pass as deliberate chokepoints.

## Vista spike result (2026-09-21)
One 158 x 158 map generated in Vista from code, baked, and played:
- `BharatRTS/Vista Spike/Generate And Bake Medium Map` (`Assets/Editor/Vista/VistaSpike.cs`)
  spawns the Mountain biome template on a 158 m terrain tile, shrinks every Noise node's world-space
  scale by 158/1000 (templates are authored for 1000 m), generates, and bakes a 513 x 513 16-bit
  heightmap to `Assets/Resources/Maps/SkirmishMedium/height.bytes`.
- `MapId.SkirmishMedium` (158, starts at (0,57), (0,-57), (-57,0)) has
  `BakedHeightmapResource`; `ProceduralTerrain` loads it via `BakedHeightmap`, levels a plateau at each
  Town Center, and keeps the procedural terrain when no baked map is set. Water, beach, pebbles, clutter
  and NavMesh all run unchanged on top.
- Result: about 10 m relief, 1% of ground over 30 deg and none over 45 deg; plateaus are flat (0.00 m range);
  the NavMesh connects all three starts (Player to Enemy path length 100 = straight line).
- Not done: the layout is the stock Mountain template, not one of the five styles; splat layers still come
  from our height/slope rules (Vista's 5 layers unused); resources still use the old ring logic (not the
  10-15 unit starting guarantee or the contested-zone placement); no Vista water mask; not authored graphs.

## All 5 layout styles baked (2026-09-25)
Closes this item: each style is now a real baked map, not the placeholder Mountain bake above.

- **Terrain shaping is deterministic C# post-processing on the sampled heightmap**, not hand-authored
  Vista graph nodes: `Assets/Scripts/Core/SkirmishTerrainCarving.cs`
  (`ApplyCornerMesas`/`ApplyRidgeWithPass`/`ApplyTerracing`/`LimitSlope`, all pure and unit-tested) runs
  inside `Assets/Editor/Vista/VistaSpike.cs`'s generalized `LayoutRecipe`/`Create`/`BakeHeightmap`, after
  Vista supplies a base template's noise. `LimitSlope` (a simple thermal-erosion relaxation) runs last on
  every recipe as a general safety net - a stock template's own noise can exceed the walkable slope limit
  in a random patch anywhere on the map, not only wherever a feature-specific carve looks.
- **Crossroad Valleys** (`MapId.SkirmishMedium`, unchanged resource path): Dunes base + `ApplyCornerMesas`
  (4 flat-topped plateaus at the contested zone's diagonal corners, (+-25,+-25)). The template's own paired
  "Mesa" biome (`Mesa/DunesAndMesa.asset`) was tried first but its two sibling `LocalProceduralBiome`s
  cancelled each other out to a completely flat bake (a real Vista paired-biome quirk with sibling
  anchors, not chased further) - every style now uses a single-biome base plus a code carve instead.
- **Divided Riverbed** (`MapId.SkirmishDividedRiverbed`): Dunes base, no height carve - the river is
  entirely a runtime `MapDefinitionData.WaterCenter/WaterHalfExtents` band across full X at Z=0 (reuses
  the existing water/shoreline system unchanged, auto-extended to the map edges). **Real fords**: new
  `MapDefinitionData.FordCentersX`/`FordHalfWidth` + `Assets/Scripts/Core/RiverFords.cs` (`FordFactor`,
  shared by `ProceduralTerrain`'s height blend and a new `NavMeshBaker.AddRiverFordModifiers`) carve 3
  literal walkable, visually-dry crossings at X = -40/0/40 - live-verified via `NavMesh.CalculatePath`
  connecting straight through the centre ford. Enemy2's start moved off the Z=0 centreline (z=30, was 0)
  since the shared 3-start layout would otherwise put it inside the river.
- **Mountain Pass** (`MapId.SkirmishMountainPass`): Mountain base + `ApplyRidgeWithPass` (a raised Z-band
  "chain" with one corridor at X=0, directly between Player and Enemy, who both sit at x=0) + `LimitSlope`.
- **Highland Foothills** (`MapId.SkirmishHighlandFoothills`): Mountain base + `ApplyTerracing` (partial
  strength 0.7, so some natural undulation survives the step-quantization) + `LimitSlope`.
- **Clearing** (`MapId.SkirmishClearing`): Dunes base, no height carve. New `MapDefinitionData.
  ForestThreshold`/`ForestLaneWidth` + `Assets/Scripts/Core/ForestLanes.cs` (`DistanceToNearestLane`) make
  `ResourceNodeSpawner.ClassifyTile` force Plains within `ForestLaneWidth` of the straight line between
  every pair of active town centres, and use a much denser forest threshold (0.3, was 0.58) everywhere
  else. **Not currently visible in play**: this logic never runs today, on ANY map, because the terrain
  has no tree/detail prototypes assigned (`ResourceNodeSpawner.PopulateTerrainFoliage` bails out before
  reaching `ClassifyTile` - the exact same pre-existing gap the terrain-foliage feature itself already
  flagged) - unit-tested in isolation (`ForestLanesTests.cs`) but not yet visually confirmed live; will
  activate once real tree/detail prototypes are wired onto the terrain.
- **In-game map picker**: `CivPicker` gained a `<`/`>` map-cycle row (built in code, not scene-wired) over
  all 8 `MapId`s with friendly names, defaulting to River Valley unchanged; `CivilizationSetup.SetMap`
  lets it override the Inspector's default before `BeginMatch`. Live-verified end to end through the real
  UI (cycled to each style, Confirmed, `MapRegistry.CurrentId` matched the picked style).
- All 4 new/rebaked maps live-verified via `NavMesh.CalculatePath` connecting Player's start to Enemy's
  straight through each style's signature feature (the pass corridor, the ford), not just "the bake
  succeeded" - a blocked path was treated as a hard failure, per rule 5 below.
- Still not done, per the spec's own scope (closed 2026-09-25, see below): the 10-15 unit starting-resource
  guarantee and contested-zone resource placement generally (closed the same day, see below), and the
  water mask item (closed 2026-09-26, see below). Small/large map sizes remain open.

## Resource placement per the zoning spec (2026-09-25)
Closes rules 1-4 above for real resource placement (rule 3's large-vein/contested requirement, rule 2's
starting-resource guarantee) - depended on the 5 baked layouts and the splat/mask work above existing.

- New `MapDefinitionData.UsesZoning` (true only for the 5 158x158 skirmish maps) gates all of this -
  RiverValley/Highlands/Coastal keep their old unconstrained ring exactly, since `SkirmishMapZones`'
  fixed 3/40-tile bands were never sized for a 100-130 unit map.
- New `Assets/Scripts/Core/ResourcePlacement.cs` (pure, unit-tested): `RandomPointNearTownCenter` draws a
  point 10-15 units from a given town centre in a random direction; `RandomPointInContestedZone` draws
  from `ResourceNodeSpawner`'s usual centre-relative ring but rejects (bounded retries, then a clamped
  fallback) anything outside `SkirmishMapZones`' Contested classification.
- `ResourceNodeSpawner.Start()` now spawns a guaranteed small primary Gold node (`startingGoldAmount`,
  smaller than a general Gold Mine) plus a starting woodline (`startingWoodlineTreeCount` trees) 10-15
  units from every town centre slot (Player/Enemy/Enemy2, unconditionally - same "a few wasted nodes near
  an unused slot is cheap" convention `ClassifyTile`'s own town-centre clearing already uses) before
  anything else spawns, on every `UsesZoning` map.
- The general gold/stone/farm/fruit-bush/relic ring (and the tree-loop fallback used when terrain foliage
  is inactive) now draws from `RandomPointForGeneralResource`, which routes through
  `RandomPointInContestedZone` on a `UsesZoning` map - large veins/quarries/farms/relics can no longer
  land in the home buffer. `RandomPointBiasedToMask` (Mountain Pass ridge / Crossroad Valleys mesa bias)
  draws its candidates from the same contested-respecting helper, so mask-biased placement stays inside
  the Contested zone too.
- Not addressed by this pass: general/background forest (terrain foliage, once tree/detail prototypes are
  wired) still covers the whole spawnable area including the home buffer - only the guaranteed starting
  woodline and the general tree-loop *fallback* are zone-aware; water-avoidance for the general ring (a
  pre-existing gap, not introduced here - only `ClassifyTile`'s terrain-foliage pass avoids water); and
  fairness (equal starting resources per player, still unverified beyond "the same code runs for each
  slot").

## Vista splat/masks (2026-09-25)
Closes this item: per-style feature masks now drive both terrain texture splatting and Gold/Stone
placement, not just height. Depended on the 5 baked maps above existing.

- **Masks are baked alongside the height**, at the same 513x513 resolution, reusing `BakedHeightmap`'s
  own binary format with `heightScale=1` (a mask value already *is* the 0..1 "height" that format
  expects - no new format needed). `Assets/Editor/Vista/VistaSpike.cs`'s `LayoutRecipe` gained an
  optional `ComputeMask` alongside `HeightPostProcess`; `BakeHeightmap` writes `mask.bytes` next to
  `height.bytes` when a recipe defines one.
- **Mask math is factored out of the existing height carves**, not re-derived: `SkirmishTerrainCarving`'s
  `RidgeStrength`/`PassStrength`/`MesaStrength` are now shared private helpers used by both
  `ApplyRidgeWithPass`/`ApplyCornerMesas` (height) and the new `ComputeRidgeMask`/`ComputeCornerMesaMask`
  (mask) - the mask always agrees with what the height carve actually did, at any resolution.
- **Mountain Pass** bakes a ridge-minus-pass mask (`Maps/SkirmishMountainPass/mask.bytes`), wired via
  `MapDefinitionData.MaskTerrainLayerIndex = 2` (Rock) - the ridge now visibly reads as exposed rock,
  with a clean green gap straight through the pass (screenshot-confirmed).
- **Crossroad Valleys** bakes a corner-mesa mask (`Maps/SkirmishMedium/mask.bytes`), wired to layer index
  3 (Sand) - the 4 mesa tops now read as sandstone buttes against the surrounding grass (screenshot-
  confirmed), with `TerrainClutter`'s rock/pebble decoration following for free (it already reads the
  same alphamap).
- **`ProceduralTerrain.ApplyAlphamaps`** blends the mask in as a final step: boosts the target layer's
  weight toward 1 by the sampled mask strength, scaling every other layer's weight down proportionally
  so the 5 weights still sum to 1. A no-op (`_mask == null`) on every map without one.
- **Gold/Stone placement now biases toward the mask** on maps that opt in
  (`MapDefinitionData.BiasResourcesToMask`): `ResourceNodeSpawner.RandomPointBiasedToMask` draws 8
  candidate points in the usual ring and keeps whichever scores highest against the mask
  (`ResourceBias.PickBestScoringCandidate`, pure and unit-tested) - a "quarry in the mountains" feel
  without an unbounded rejection-sampling loop. Live-verified against real spawned nodes: 22 Gold/Stone
  nodes on Crossroad Valleys averaged a 0.94 mask score (most landing exactly on a mesa top), against
  what would be close to 0 unbiased given how little of the ring the mesas actually cover.
- Highland Foothills/Divided Riverbed/Clearing deliberately got no mask this pass - Highland Foothills'
  existing generic slope-based Rock rule already emphasises terrace risers reasonably; Divided Riverbed's
  fords already read correctly via the existing water-proximity sand/pebble bands; Clearing's identity is
  a tile classification (`ForestLaneWidth`), not a height/splat feature.

## Water mask (2026-09-26)
Closes this item: real, terrain-derived water now exists on 4 of the 5 layout styles (Divided Riverbed
already had its own explicit river, untouched), feeding the existing water/shoreline/NavMesh/Dock/fish
systems (`ProceduralTerrain`, `WaterProximity`, `NavMeshBaker`, `WaterMover`) exactly as they already work
for Coastal/Divided Riverbed - no runtime code in any of those systems changed. What's new is *deriving
where a water rectangle sits* from the actual baked terrain instead of hand-placing one.

- **Kept the water/shoreline system's own rectangle-based data model** (`MapDefinitionData.WaterCenter`/
  `WaterHalfExtents`) rather than generalizing to an arbitrary raster lake shape - `WaterProximity.
  ClampToWater`'s own comment already documents that `WaterMover`'s "a boat's straight-line path never
  leaves water" guarantee depends on the water region being convex, which a hand-baked arbitrary mask
  shape could easily violate. A rectangle is exactly what every consumer (shoreline carve, NavMesh
  exclusion, Dock placement, boat pathing) already assumes and is proven safe for.
- New `Assets/Scripts/Core/WaterBasinFinder.cs` (pure, unit-tested, no UnityEngine.Object dependency):
  `FindLowestFlatRegion` grid-searches candidate rectangles across the Contested zone (`SkirmishMapZones`
  - water belongs there per rule 3, same as large gold/stone) and keeps whichever has the lowest maximum
  sampled height, i.e. the flattest/lowest basin a rectangle that size can actually fit inside on the real
  baked terrain - "derive it, don't guess it," the same principle `ComputeMask`'s feature masks already
  use. Returns not-found if even the best candidate doesn't sit in the terrain's own lowest quarter.
- New `Assets/Editor/Vista/VistaSpike.cs` menu item `BharatRTS/Vista Skirmish Layouts/Report Water
  Basins`: runs `WaterBasinFinder` against every style's already-baked `height.bytes` (no need to
  regenerate the Vista graph) and logs a candidate rectangle per style - a repeatable dev tool, not
  match-time code; re-run it any time a style's heightmap is rebaked.
- Ran it live via UnityMCP against the 5 real baked heightmaps. Results transcribed directly into
  `MapDefinition.cs` (no runtime file/mask format needed - the analysis is an offline decision aid, the
  same "baked once, hardcoded as constants" convention `SkirmishTerrainCarving`'s own mesa/ridge
  parameters already use):
  - **Crossroad Valleys**: a lake at (-25, 0), half-extents (14, 10) - the valley between the two -X mesas.
  - **Mountain Pass**: a small lake at (13, 31), half-extents (8, 8) - off to one side of the ridge, well
    clear of the pass corridor's own Z-band (the ridge/pass carve only shapes terrain near Z=0; this sits
    at Z=31, past even the ridge's falloff).
  - **Highland Foothills**: a highland tarn at (25, 25), half-extents (14, 10), in a genuinely low terrace
    (max height 1.14m against an 11.10m map peak).
  - **Clearing**: a small clearing pond at (-20, 25), half-extents (14, 10) - Clearing's Dunes base is
    gentle everywhere (2.71m map peak), so this is honestly the flattest/lowest spot rather than a
    dramatic carve, matching the style's "dense forest, small natural clearings" identity.
  - **Divided Riverbed**: left unchanged (its own explicit full-width river is the correct design, not
    something to derive from a basin search).
- `ResourceNodeSpawner` gained water avoidance (`GenerateLandPoint`, a bounded-retry wrapper): every
  general/starting resource draw now retries away from the map's water rectangle before falling back -
  a real gap this item's own change exposed (4 more maps now have water, quadrupling the odds a resource
  spawn point randomly lands underwater), fixed rather than shipped as a new regression. Fish placement
  (`RandomPointInWater`) is intentionally excluded - that's supposed to land in water.
- 5 new EditMode tests (`WaterBasinFinderTests.cs`), 675/675 total. Live-verified via UnityMCP through the
  real production path for all 4 newly-watered maps: a real match on each style, confirmed a real `Water`
  GameObject exists with the exact discovered center/half-extents, confirmed `NavMesh.CalculatePath`
  (start-position-snapped per this project's own documented gotcha) still connects Player to Enemy on
  every one (rule 5), screenshotted each lake (clean shoreline/beach blend, plausible placement - the
  Crossroad Valleys lake visibly sits between its two mesas, the Mountain Pass lake at the mountain's
  foot, the Highland tarn in a genuine basin with nearby rock outcrops), and confirmed a fresh match on
  each map has zero non-Fish resource nodes inside water (only the intentional Fish nodes are).

## Relics: starts-per-map + scaling to small/large maps (2026-09-26)
Closes this item. Wave 6 item 35 shipped `RelicCount = 5` as a flat literal on every map - a reasonable
call when only one map size existed, but a real gap now that the skirmish framework supports (and will
eventually add small/large variants of) multiple layouts/sizes: a future differently-sized layout would
silently inherit today's "5" unless someone remembered to hand-retune it. Also resolves this doc's own
"should Relics be map-authored or randomised" open question (below): randomised, but fairly spread
by angle around the map centre, not purely uniform.

- New `Assets/Scripts/Core/RelicPlacement.cs` (pure, unit-tested):
  - `ComputeRelicCount(contestedWidth)` derives the relic count from the map's own Contested-zone area
    (clamped 3-12), anchored so today's 158x158 skirmish maps (`ContestedWidth` 78) reproduce exactly 5 -
    this is a scaling formula, not a live balance change to any already-tuned map. A future small/large
    layout variant plugs its own `ContestedWidth` in and gets a proportional count automatically.
  - `WedgeFor(index, count, ...)` partitions the full circle into `count` equal angular wedges - "starts-
    per-map": since the map's starts (Player/Enemy/Enemy2) already sit spread around the map centre,
    drawing one relic per wedge keeps relics fairly spread relative to however many starts exist, instead
    of a purely uniform draw that - with as few as 3-5 points - can clump by chance and unfairly favour
    whichever start happens to be nearest the clump.
  - `MapDefinitionData.RelicCount` on the 5 skirmish (`UsesZoning`) maps is now computed via
    `RelicPlacement.ComputeRelicCount(SkirmishMapZones.ContestedWidth(158f))` instead of a hand-picked
    constant; RiverValley/Highlands/Coastal keep their flat `5` (out of scope - no Contested-zone concept
    there).
  - `ResourcePlacement.RandomPointInContestedZone` gained an angle-range overload (the existing 2-arg
    call is now a thin wrapper over the full 0..2*PI range, byte-identical RNG sequence to before) so
    `ResourceNodeSpawner`'s new `RandomPointForRelic` can draw each relic from its own wedge while reusing
    the exact same Contested-zone-safety-net/fallback every other general resource already gets.
  - Relics also now go through the existing water-avoidance (`GenerateLandPoint`, from the water mask
    item above) - a relic underwater would be as unreachable as a gold mine underwater.
- 8 new EditMode tests (`RelicPlacementTests.cs`), 683/683 total. Live-verified via UnityMCP: a real
  SkirmishMedium match confirmed `RelicCount` still reads 5 (no live balance change), and all 5 real
  spawned `Relic` GameObjects landed in 5 distinct 72-degree wedges (61.6, 135.8, 159.5, 256.8, 353.6
  degrees), every one classified Contested and outside water.

## Small and large map sizes (2026-09-26)
Closes this doc's own "how does the 40/3 tile rule scale to small and large maps" open question:
**fixed widths, not proportional.** `SkirmishMapZones.HomeBufferWidth`/`EdgeDeadzoneWidth` stay the same
absolute tile count on every map size (a start's own buffer doesn't need to grow just because the map is
bigger - matches real AoE's own convention that starting areas stay roughly the same size across map
sizes), so `SkirmishMapZones.Classify`/`ContestedWidth` already generalized to any `mapSize` with zero
code changes. What genuinely needed to scale is everything downstream of the Contested zone's own area.

- New `Assets/Scripts/Core/SkirmishMapScaling.cs` (pure, unit-tested): `ScaleCount` (resource/tree/farm
  counts scale linearly with Contested width, not area - same "avoid drowning a bigger map in clutter"
  reasoning `MapDefinition.cs`'s own Phase-5 map-scale-up note already used), `ScaleRadius` (the general
  resource ring's min/max radius scale linearly with the map's own Contested half-width, keeping the same
  relative ring shape at any size), `HomeBandMidpoint` (where a start sits - the centre of the playable
  home band - generalizing the "|z|=57 is the centre of the 39..76 band" comment beyond the 158 map).
- Two new `MapId`s, **not** two new styles: `SkirmishSmall`/`SkirmishLarge` bake the exact same Crossroad
  Valleys recipe (`VistaSpike.CrossroadValleysSmall`/`CrossroadValleysLarge`) at 120x120 and 240x240
  instead of 158x158 - proving the scaling math generalizes to a genuinely different map size, not just a
  genuinely different layout. `VistaSpike.LayoutRecipe` gained a `MapSize` field (was a single shared
  158 constant); `Create`/`BakeHeightmap`/`BakeMask`/`ReportWaterBasin` all read the active recipe's own
  size now, and the water-basin search's candidate rectangle scales with the recipe's own Contested
  half-width instead of a fixed absolute size.
- **Real bug caught by a live screenshot, not assumed correct from the numbers**: a first attempt scaled
  only the mesa centres' offset with map size and kept mesa radius/falloff absolute (a feature's physical
  size "shouldn't" need to shrink, or so the reasoning went) - on the 120-map this put all 4 mesas'
  radius+falloff reach *past* their own centre offset, so they visibly overlapped into one continuous
  blob instead of 4 separate plateaus. Fixed by scaling mesa radius/falloff by the same
  `contestedHalf / mediumContestedHalf` factor as the offset (`mesaRaiseWorld`, a world-space elevation
  rather than a footprint dimension, stays absolute) - re-baked and re-screenshotted, confirming 4
  cleanly separated mesas on both new sizes.
- Every count/radius/start-position field on the 2 new `MapDefinitionData` entries is *computed* from
  the map's own Contested width via `SkirmishMapScaling`/`RelicPlacement.ComputeRelicCount`, not
  hand-copied from `SkirmishMedium`'s numbers - Small: `RelicCount=3` (clamped to the minimum),
  `TreeCount=10`, `GoldCount=6`; Large: `RelicCount=12` (clamped to the maximum), `TreeCount=41`,
  `GoldCount=25`. Water: re-running `VistaSpike.ReportWaterBasins` after the mesa fix found a real basin
  on *both* new sizes this time (the small map's fixed mesa overlap had also swallowed the space a pond
  could have used) - Small gets a small pond nestled between its 4 mesas; Large gets a lake at almost
  exactly 2x Small map's own contested-half-width-scaled lake ratio to Medium's.
- `CivPicker`'s map-cycle row gained "Crossroad Valleys (Small)"/"(Large)" entries.
- New EditMode tests: `SkirmishMapScalingTests.cs` (8 tests) plus 3 more in `MapDefinitionTests.cs`
  confirming both new maps use zoning, their computed town centres land in the HomeBase zone (not
  Contested, not the dead zone), and Small/Large's resource counts are respectively fewer/more than
  Medium's. 694/694 EditMode tests pass (up from 683).
- Live-verified via UnityMCP through the real production path for both new maps: a real match on each,
  `NavMesh.CalculatePath` (start-position-snapped) connects Player to Enemy on both, zero general
  gold/stone found outside the Contested zone, relic counts match the map's own `RelicCount` exactly, and
  screenshots confirm 4 cleanly separated mesas with a pond/lake in the expected spot on both sizes.

## Open questions
- Number and placement of starts on the ring (2 players opposite each other, or up to 3 with
  the existing Enemy2 faction?).
- ~~Whether Relics / holy sites should be map-authored or randomised inside the contested square.~~
  Resolved 2026-09-26 (see "Relics" above): randomised, but fairly spread by angle around the map centre.
- ~~How the same 40 / 3 tile rule scales to small and large maps (fixed widths vs proportional).~~
  Resolved 2026-09-26 (see "Small and large map sizes" above): fixed widths.

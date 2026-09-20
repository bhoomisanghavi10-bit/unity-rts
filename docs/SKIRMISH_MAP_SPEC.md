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

## Open questions
- Number and placement of starts on the ring (2 players opposite each other, or up to 3 with
  the existing Enemy2 faction?).
- Whether Relics / holy sites should be map-authored or randomised inside the contested square.
- How the same 40 / 3 tile rule scales to small and large maps (fixed widths vs proportional).

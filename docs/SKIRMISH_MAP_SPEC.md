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

## Open questions
- Number and placement of starts on the ring (2 players opposite each other, or up to 3 with
  the existing Enemy2 faction?).
- Whether Relics / holy sites should be map-authored or randomised inside the contested square.
- How the same 40 / 3 tile rule scales to small and large maps (fixed widths vs proportional).

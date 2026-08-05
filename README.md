# Kingdoms of Bharat

A historical real-time strategy game set across Indian kingdoms and empires.

This repository holds the first playable prototype: **Age III — Age of Kingdoms
(c. 650–1526 CE)**. The prototype's only goal is to prove the core RTS loop —
gather resources, construct buildings, train units, fight — feels good before
mythology, other ages, multiplayer, or art are added.

## Scope of this prototype

- One small map (~40x40 units)
- One generic "Kingdom" civilization template (later split into Chola /
  Vijayanagara / Rajput variants)
- Resources: Food and Wood only (Gold/Stone come later)
- No naval mechanics, no fog of war, no AI opponent yet
- Placeholder primitive-shape art only — mechanics first, visuals later

## Tech stack

- Unity (latest LTS), C#
- Built-in NavMesh for pathfinding
- No multiplayer/netcode in this phase

## Project structure

```
Assets/
  Scenes/            The prototype map
  Scripts/
    Units/            Unit identity, movement (NavMesh), selection hooks
    Buildings/         Town Center, Barracks, shared building/construction logic
    Resources/         Resource nodes (trees/farmland), gathering, stockpiles
    Camera/            RTS camera controller (pan, edge-scroll, zoom)
    Selection/         Click / box-select input handling
    Combat/             Attack-move, melee combat, target dummy
    UI/                 Resource counters, selected-unit panel, build menu
    Core/               Game manager, per-player economy
  Prefabs/
  Materials/
```

Each folder is single-responsibility: a unit's movement, selection, gathering,
and combat behaviors are separate components composed onto one GameObject,
not one monolithic script.

## Running the prototype

Open `Assets/Scenes/Main.unity` and press Play. The ground is generated at
runtime by `ProceduralGround` (a flat 40x40 grid with mild Perlin-noise height
variation, so terrain has visible relief without an authored heightmap yet).
The camera uses `RTSCameraController` on Main Camera:

- **Pan:** WASD / arrow keys, or move the mouse to a screen edge
- **Zoom:** mouse scroll wheel

Four placeholder capsule "Worker" units (`UnitSpawner`) spawn on the map,
alongside scattered trees (Wood) and farmland patches (Food) from
`ResourceNodeSpawner`, and a single Town Center (`TownCenterSpawner`):

- **Select:** left-click a unit, or left-click-drag a box around several
- **Move:** right-click a point on open ground to send selected units there
  (pathfinding via a runtime-baked NavMesh — `NavMeshBaker`)
- **Gather:** right-click a tree or farmland patch instead — selected workers
  walk over, gather over time up to a carry cap, haul it to the Town Center,
  and repeat until the node is exhausted (`Gatherer`, `ResourceStockpile`)

Press **B** to start placing a Barracks foundation (costs 100 Wood). Move the
mouse to preview it — green if you can afford it and the spot is clear of
other buildings, red otherwise. **Left-click** to confirm, **right-click** or
**Esc** to cancel (`BuildingPlacer`). A placed foundation does **not** build
itself — select a worker and **right-click the foundation** to send it to
build (AoE-style); it rises from the ground over 8 seconds while a worker is
actively there, and multiple workers build proportionally faster
(`Builder`, `ConstructionSite`).

There's no building-selection UI yet, so press **T** to train a Soldier (50
Food, 5 seconds) at every completed Barracks — it appears next to the
building when ready. A red target dummy (`TargetDummySpawner`) sits at a
fixed spot on the map to fight:

- **Attack-move:** select a Soldier and right-click the dummy (or any other
  live unit) — it walks into range and starts hitting it on a cooldown until
  the target dies or you give a new order (`MeleeAttacker`, `Attackable`)

> Note: this scene was authored outside the Unity Editor. Milestones 1-3 have
> been opened and verified working in Unity 6.3 LTS; milestones 4-6 have gone
> through several rounds of real-Editor bug fixes already (see commit
> history) but each new addition is still worth a sanity check.

## Build milestones

1. Project scaffold
2. Terrain & camera
3. Unit selection & movement
4. Resource gathering
5. Building & construction
6. Basic combat *(this commit)*
7. Minimal UI

## Design docs

Full design context (age progression, mythology companion title) lives outside
this repo and is referenced during development but not checked in here.

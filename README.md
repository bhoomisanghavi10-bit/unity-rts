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
- Resources: Food, Wood, Gold, Stone
- No naval mechanics
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
    Core/               Faction/ownership tagging, per-player economy
    FogOfWar/           Vision grid, fog rendering, enemy hide/reveal
    AI/                 Scripted opponent (economy, building, combat)
    Wildlife/           Wild boars (hunt for Food), livestock (milk for Food)
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
alongside scattered trees (Wood), farmland patches (Food), fruit bushes
(small dark-green spheres, Food), gold mines (yellow spheres), and stone
quarries (gray blocks) from `ResourceNodeSpawner`, and a single Town Center
(`TownCenterSpawner`):

- **Select:** left-click a unit, or left-click-drag a box around several
- **Move:** right-click a point on open ground to send selected units there
  (pathfinding via a runtime-baked NavMesh — `NavMeshBaker`)
- **Gather:** right-click any resource node instead — selected workers walk
  over, gather over time up to a carry cap, haul it to the Town Center, and
  repeat until the node is exhausted (`Gatherer`, `ResourceStockpile`)

**Food also comes from wildlife.** A few wild boars (`WildBoarSpawner`,
dark capsules) wander the map and are dangerous — they'll break off to chase
and attack *any* unit, Player's or the AI's, that strays within range,
including unarmed workers (workers are fragile: 20 HP, no way to fight
back). Select a Soldier and attack-move onto one to hunt it down; killing it
leaves a Food carcass any worker can gather normally, same as any other
resource node.

A small herd of cows (`LivestockSpawner`, cream-colored, grazing near your
Town Center) is a separate, safe Food source — **cows cannot be attacked or
killed by anything**, reflecting the setting's food culture; they can only
be milked. Select a worker and right-click a cow to send it there — it
produces Food slowly for as long as it stays in range (`LivestockWorker`),
following the cow if it wanders off.

Press **B** to start placing a Barracks foundation (costs 100 Wood + 50
Stone), or **F** for a Farm (60 Wood) — a passive Food source you can build
anywhere, anytime, unlike the map's fixed resource scatter. Move the mouse
to preview it — green if you can afford it and the spot is clear of other
buildings, red otherwise. **Left-click** to confirm, **right-click** or
**Esc** to cancel (`BuildingPlacer`). Neither building builds itself —
select a worker and **right-click the foundation** to send it to build
(AoE-style); it rises from the ground over several seconds while a worker
is actively there, and multiple workers build proportionally faster
(`Builder`, `ConstructionSite`). Once a Farm is complete, right-click it
(instead of an incomplete foundation) to **staff** it — like Barracks, it
produces nothing without an assigned worker present, but a Farm's output is
deliberately slower than gathering directly from a resource node
(`Farm`, `FarmWorker`) — the tradeoff is that it's always available.

There's no building-selection UI yet, so press **T** to train a Soldier (50
Food + 20 Gold, 5 seconds) at every completed Barracks — it appears next to
the building when ready. A red target dummy (`TargetDummySpawner`) sits at a
fixed spot on the map to fight:

- **Attack-move:** select a Soldier and right-click the dummy (or any other
  live unit) — it walks into range and starts hitting it on a cooldown until
  the target dies or you give a new order (`MeleeAttacker`, `Attackable`)

The map starts under fog: unexplored areas are black, previously-explored
areas you've moved away from stay dimmed, and only the area around your own
units/buildings is fully visible (`FogOfWarManager`, `VisionSource`). Every
unit/building carries a `FactionMember` (`Player`/`Enemy`); the player can
only select/command their own side.

An AI opponent (`AiController`) starts its own base on the far side of the
map (mirrored Town Center + starting workers) and runs entirely on its own
timer — no input from you required. It gathers (automatically picking up
fruit bushes and hunted-boar carcasses too, since those are just resource
nodes like any other), eventually builds its own Barracks, Farm, and
Houses (sending its own workers to construct and staff each, same
AoE-style rules the player follows — Houses go up proactively once its
population gets close to the cap), trains Soldiers and replacement
Workers once Food/Gold allow and there's population room, and sends a
squad to attack-move toward the player once it has three or more idle
Soldiers. It has full internal knowledge of the map (no scouting logic — a
deliberate first-pass simplification) even though fog still hides its base
visually from you until you scout it — wild boars are hidden by fog the
same way (they wander like units), while static resource nodes stay
visible once explored, matching how AoE treats terrain features versus
units. It has its own separate resource stockpile
(`EnemyResourceStockpile`) — the top-left HUD only ever shows yours. It
doesn't have its own livestock herd, so milking isn't part of its economy.

Each side now plays as one of three historically-grounded civilizations
(`CivilizationSetup`, set via Inspector — no in-game picker yet): **Chola**
(economic — irrigation and trade, +15% gather rate, -15% build cost),
**Vijayanagara** (military tempo — Hampi's armies, -20% Soldier train time),
or **Rajput** (raw combat power — warrior clans, +20% Soldier damage, +15%
max health). You play Chola and the AI plays Vijayanagara by default. The
bonus is deliberately modest — flavor and a reason to pick one, not a
balance overhaul — and shows up as each civ's units/buildings sharing a
distinct identity color, plus a "Civilization: X" line in the top-left HUD.

The B/T hotkeys above still work, but there's now on-screen UI too
(`ResourceHUD`, `SelectedUnitPanel`, `BuildMenu` — plain IMGUI, not a Canvas;
see note below):

- **Top-left:** live Wood/Food/Gold/Stone counters, plus a Population: X/Y
  readout (X/Y — current headcount vs. cap; build Houses to raise the cap,
  AoE-style)
- **Bottom-left:** appears when something's selected — unit name, status
  (Idle/Gathering/Building/Farming/Milking), and HP if it's a Soldier or
  Worker; shows a headcount instead for a multi-unit selection
- **Bottom-right:** "Build Barracks", "Build Farm", "Build House" (all
  enabled only with a worker selected), "Train Worker", and "Train
  Soldier" buttons, calling the same code the hotkeys do
- **Bottom-right corner (below the build menu):** a live minimap
  (`MinimapController`) — a second top-down camera rendered into a small
  texture, so it automatically shows the same fog-of-war/units/buildings
  the main view does. Click or click-drag on it to jump the main camera
  there; panning/zooming (WASD/arrows, screen-edge scroll, mouse wheel)
  now eases smoothly instead of snapping instantly.

> Notes: this scene was authored outside the Unity Editor. Milestones 1-3
> have been opened and verified working in Unity 6.3 LTS; milestones 4-7 have
> gone through several rounds of real-Editor bug fixes already (see commit
> history) but each new addition is still worth a sanity check. The UI is
> built with Unity's immediate-mode `OnGUI` rather than a uGUI Canvas +
> TextMeshPro — it needed no scene-authored hierarchy or font-asset import
> step, both of which are awkward to get right without an Editor to verify
> against. Worth swapping for real uGUI once you're doing visual polish.

## Build milestones

1. Project scaffold
2. Terrain & camera
3. Unit selection & movement
4. Resource gathering
5. Building & construction
6. Basic combat
7. Minimal UI

### Phase 2 (in progress)

8. Multiple resources — Gold, Stone
9. Team/Faction + fog of war
10. AI opponent
11. Wildlife & Farms — hunted boars, milked livestock, buildable Farm
12. Civilization differentiation — Chola, Vijayanagara, Rajput
13. Lighting & post-processing — URP switch, Volume profile (bloom, tonemapping, vignette), softer shadows
14. Terrain overhaul — layered noise height, splat-blended grass/dirt/rock ground texture
15. Material/shader quality pass — shared matte PBR material helper, fixed the placement-ghost transparency bug
16. Camera & readability polish — smoothed pan/zoom, a real minimap (click to jump)
17. VFX pass — hit sparks, death poofs, gathering dust, construction dust
18. Worker combat + hover tooltips — Workers can now fight/hunt boars for Food; hovering anything shows its name/status/HP/progress
19. First real unit model — Soldiers use the "Axe Warrior" pack (Humanoid-rigged) instead of a capsule; Workers still primitives pending a civilian-style pack
19b. Shared human body + animation — Workers and Soldiers both use the "Human Character Dummy" (renamed from Kevin Iglesias to `human/`, Female/Male respectively), civ-tinted via the pack's own color palette, with real Idle/Walk/Gather/Mine/Farm/Build/Attack animation, terrain-aware ground alignment
20. Population cap + Houses — AoE-style: each faction starts with a base cap of 10, build Houses (30 Wood) to raise it by 5 each; Worker/Soldier training blocks once at the cap. Town Center now trains Workers directly (G hotkey, 50 Food) alongside Barracks training Soldiers; the AI opponent builds its own Houses and trains replacement Workers the same way
21. Real building models — TownCenter, Barracks, Farm, and House swap from procedural multi-part silhouettes (`BuildingModelFactory` scaffolding) to real authored models, plus a round of stability fixes this surfaced: giant/floating/unselectable units from bad model scale and stale ground-height raycasts, and a runaway-unit-launching bug traced to the Ground Physics Layer
23. Environment variety, part 1 — Trees and bushes load from multiple prefab variants at random per spawn instead of one repeated model (`EnvironmentPropFactory`); the Kevin Iglesias animation folder rename lands here too
24. Environment variety, part 2 — Gold mines, stone quarries, and farmland get the same multi-variant treatment; the 2D pixel-art rock pile pack is dropped in favor of proper 3D resource-node models. The multi-variant loading code shipped first with the target folders empty (silently falling back to the flat-primitive placeholders); now populated - GoldMine gets a gold-ore prop from the Human Crafting Animations pack, Farmland gets two variants (a tilled-plot prop from the same pack, plus a standalone downloaded farmland model), and StoneQuarry gets three rock-formation sizes (small/medium/large) from a rock asset pack
25. Wildlife polish — Wild boar and cow (livestock) models get real Idle/Walk/Eating/Attack/Death animation, calmer wander behavior (an idle-chance so they don't dart to a new spot every few seconds), manual facing control (NavMeshAgent's built-in rotation was causing spin/jitter), a fix for oversized colliders that broke map-wide selection, and — most recently — a stray `Rigidbody` left on the imported boar mesh that sent it tumbling under physics independent of its actual NavMesh-driven position

## Design docs

Full design context (age progression, mythology companion title) lives outside
this repo and is referenced during development but not checked in here.

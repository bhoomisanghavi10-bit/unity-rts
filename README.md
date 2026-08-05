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

> Note: this scene was authored outside the Unity Editor (no Editor available
> in this environment). It follows Unity's standard scene YAML format, but
> give it a quick sanity check the first time you open it.

## Build milestones

1. Project scaffold
2. Terrain & camera *(this commit)*
3. Unit selection & movement
4. Resource gathering
5. Building & construction
6. Basic combat
7. Minimal UI

## Design docs

Full design context (age progression, mythology companion title) lives outside
this repo and is referenced during development but not checked in here.

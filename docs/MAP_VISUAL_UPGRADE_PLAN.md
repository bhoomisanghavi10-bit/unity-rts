# Map/Terrain Visual Upgrade Plan — Toward AoE IV Fidelity

Status: planning only, nothing implemented yet. Written 2026-09-17 per user request
("create a plan to reach the visual level of AoE 4 for maps with all the asset
sourcing needed"). Follows this project's usual asset-brief convention
(`docs/UI_ART_BRIEF.md`, `docs/TEAM_COLOR_ART_BRIEF.md`) — a plan the user sources
art against, that Claude Code then wires in.

This is deliberately a *separate* initiative from the map-layout work discussed the
same session (new `MapId` presets inspired by the downloaded AoE II/III map names,
a skirmish map-select UI). That work is data/UI. This is rendering quality — closing
the gap between "a flat-shaded procedural blob" and something that reads as a real
battlefield.

## 1. Current state (confirmed against the actual code, not assumed)

- **Ground**: one procedural mesh (`Assets/Scripts/Core/ProceduralGround.cs`),
  textured by a single 256×256 texture painted at runtime with 3 flat colors
  (grass/dirt/rock) blended by height/slope. No photographic textures, no normal
  maps. At today's 100-130 unit maps this is ~2 texture pixels per world unit —
  reads as a soft color gradient, not ground detail.
- **Water**: a flat semi-transparent colored quad (`GameplayMaterial.CreateTransparent`,
  a generic URP/Lit material forced to Transparent mode). No reflection, refraction,
  waves, depth tinting, or shoreline foam.
- **Ground clutter**: none. `EnvironmentPropFactory.cs` only spawns from
  `Resources/Environment/Trees/` (4 prefabs) and `Resources/Environment/bushes/`
  (5 prefabs), plus resource-node categories (GoldMine/Farmland/Livestock/
  StoneQuarry/WildBoar). No rocks, grass tufts, or general scatter category exists.
- **Lighting/post-processing**: URP 17.3.0 is installed (this is a **Unity 6**
  project — confirmed by the URP version), so post-processing ships bundled, no
  separate package needed. `Assets/DefaultVolumeProfile.asset` exists but nothing
  in `Main.unity`'s Camera actually references a Volume — post-processing is
  available but completely unconfigured today.
- **Reusable assets already owned**: `Assets/PolishedSurfaces/System_RockSet_Sample/`
  (Asset Store package) has real PBR rock textures (Albedo/Metallic/Normal),
  matching URP materials, and rock meshes/prefabs — including a
  `M_RockSet_01_Master_5_Ground_Sample.mat` already meant for ground use. This is
  directly reusable for rock texturing and rock-clutter props; nothing else
  reusable was found.

## 2. Target: what AoE IV terrain actually has

- Tiled, high-resolution PBR ground textures (grass/dirt/rock/sand/mud/snow
  depending on biome) blended smoothly by height/slope/moisture, not a single
  low-res runtime gradient.
- A real water shader: animated wave normals, reflections, depth-based color
  falloff (shallow = lighter/greener, deep = darker/bluer), shoreline foam.
- Dense instanced ground clutter — grass tufts, pebbles, small rocks, fallen
  branches — giving the terrain a "populated," non-sterile look.
- Real directional lighting with shadows, ambient occlusion, subtle color
  grading/bloom, and distance fog for depth.
- Enough mesh/texture resolution that the ground doesn't look blurry up close.

## 3. Phased plan

Each phase below is scoped to be its own session (or two), per this project's
"one item per session" protocol. Order is priority-ranked by visual impact per
effort, not by dependency — they're mostly independent and could be reordered.

### Phase T1 — Real ground textures + multi-layer terrain shader (highest impact)

**Code work**: replace `ProceduralGround.BuildSplatTexture`'s runtime flat-color
painting with a real multi-texture blend. Two implementation options, decide before
starting:
- **Option A (cheaper, keeps the current architecture)**: a custom URP shader
  that samples 3-4 tiled PBR texture sets (grass/dirt/rock, +sand if a
  water/beach transition is wanted) and blends them by height/slope, computed the
  same way the current splat texture is, but sampling real textures instead of
  flat colors. Keeps `ProceduralGround`'s procedural-mesh-at-runtime approach
  untouched.
- **Option B (bigger, but also solves the "import a real map" question from
  earlier)**: migrate to Unity's actual `Terrain`/`TerrainData` system, which has
  this texture-layer blending built in natively (paintable layers, built-in
  height/slope masks) and would also let real heightmap data be imported later.
  Bigger lift: `NavMeshBaker`, resource placement, and water-cell carving all
  currently assume the procedural mesh directly.

  **Recommendation: Option A first.** It's additive to what exists, ships the
  visual improvement fast, and doesn't force a decision on the bigger
  Terrain-system migration until there's a real reason (e.g. wanting actual
  imported heightmap geography, which isn't blocked on anything today).

**Assets needed** (not owned yet — PolishedSurfaces' rock set covers rock only):
- Grass, dirt/mud, and sand PBR texture sets — each ideally Albedo + Normal +
  Roughness (or a packed Metallic/Smoothness map, matching PolishedSurfaces'
  existing convention), tileable, 1-2K resolution (2K is plenty for RTS camera
  distance — no need for 4K).
- **Sourcing**: free, commercial-safe PBR texture sites —
  [ambientCG.com](https://ambientcg.com) (CC0) and
  [Poly Haven](https://polyhaven.com/textures) (CC0) both have grass/dirt/sand/mud
  sets in exactly this Albedo+Normal+Roughness format, free for commercial use, no
  attribution required. This is the same kind of self-serve sourcing that already
  worked for the SFX/music pass (Kenney.nl) — a genuinely doable "go download 3-4
  texture sets" task, not a blocker.

### Phase T2 — Real water shader

**Code work**: replace `GameplayMaterial.CreateTransparent`'s flat quad with a
proper water material/shader on the existing water-rectangle mesh in
`ProceduralGround.BuildWaterPlane`.

**Important finding**: since this project is on **Unity 6 (URP 17.3.0)**, Unity
ships a first-party **Water System** sample (ocean/river/pool presets with real
wave/foam/reflection shading) installable via Package Manager → Universal RP →
Samples, at no extra sourcing cost. Check this before sourcing anything — it may
close this entire phase with zero new art needed, just wiring. Fall back to a
hand-built shader (tiling animated normal map + Fresnel reflection + depth fade)
only if the sample doesn't fit this project's flat-water-rectangle setup.

**Assets needed** (only if the built-in Water System doesn't fit): a tileable
water normal map and a foam texture — both easily CC0-sourceable from the same
ambientCG/Poly Haven sites above.

### Phase T3 — Ground clutter (grass tufts, rocks, debris)

**Code work**: a new lightweight scatter system — GPU-instanced (`Graphics.
DrawMeshInstanced` or a `Rendering.RenderMeshUtility` batch) small props
scattered across the ground mesh at spawn/rebuild time, density-varied by
biome/slope similar to how the splat texture already varies. New
`Resources/Environment/Rocks/` and `Resources/Environment/GrassClumps/`
categories feeding `EnvironmentPropFactory`'s existing random-variant pattern —
or, since instanced clutter needs different rendering (no per-instance
GameObject/NavMeshObstacle overhead), likely a dedicated small `TerrainClutter.cs`
rather than reusing `EnvironmentPropFactory` (which spawns real interactive
GameObjects) — worth a design decision at the start of this phase.

**Assets needed**:
- Small rock meshes — PolishedSurfaces' `System_RockSet_Sample` already has these,
  reusable directly, no new sourcing.
- Grass-tuft/clump meshes (simple cross-plane or low-poly billboard clusters, the
  standard RTS/game clutter technique) — free packs exist on
  [Poly Haven](https://polyhaven.com/models) (search "grass") and on the Unity
  Asset Store (many free grass/foliage packs). Also worth checking whether
  Kenney.nl (already used for audio) has a nature/foliage asset pack, since it's
  a known-good, license-clean source for this project.

### Phase T4 — Lighting & post-processing pass

**Code work**: wire an actual `Volume`/`VolumeProfile` into the scene's Camera
(the unused `DefaultVolumeProfile.asset` is a starting point), tune shadow
settings on the main directional light, add a subtle color-grading/bloom stack,
and consider a light distance fog for depth. Since URP's post-processing is
bundled (no package to install), this is pure configuration/tuning work, not
asset sourcing — the cheapest phase in this whole plan.

**Assets needed**: none. Possibly a skybox/HDRI if the current sky reads flat,
sourceable free from Poly Haven's HDRI section if wanted — optional, not
required for the core visual jump.

### Phase T5 — Mesh/texture resolution check

**Code work**: verify `ProceduralGround`'s current `GroundResolution` values
(100-130, matching `GroundSize`) don't look faceted up close once real textures
are in place from Phase T1 — may need bumping resolution or adding a simple
tessellation/normal-detail trick rather than raw vertex count, to avoid tanking
performance on larger maps. Decide after T1 ships and is visible, not blind
up front.

**Assets needed**: none.

## 4. Recommended execution order

1. **T1 (ground textures/shader)** — biggest single visual jump, unblocks the
   most obvious "this doesn't look like a game" complaint.
2. **T4 (lighting/post-processing)** — cheapest phase (zero new assets, pure
   config), and makes T1's new textures actually look good (better light/shadow
   response). Worth doing right after T1, maybe even the same session if small.
3. **T2 (water shader)** — likely fast if Unity 6's built-in Water System fits;
   check that first before assuming it needs the fuller custom-shader path.
4. **T3 (ground clutter)** — biggest new-code lift (a scatter/instancing system
   that doesn't exist at all today), do once the base ground/water already look
   right so clutter placement can be judged against the final look.
5. **T5 (resolution check)** — a follow-up sanity pass after the above, not a
   standalone need right now.

This is independent of, and can interleave freely with, the separately-discussed
map-layout content work (new `MapId` presets, skirmish map-select UI,
multi-region water for "7 Islands"-style maps) — neither blocks the other.

## 5. Asset sourcing checklist (what the user needs to go get)

| Need | Source | License | Already owned? |
|---|---|---|---|
| Grass/dirt/sand PBR texture sets (Albedo+Normal+Roughness, 1-2K) | ambientCG.com or Poly Haven | CC0 | No |
| Rock PBR texture + meshes | — | — | **Yes** — `Assets/PolishedSurfaces/System_RockSet_Sample/` |
| Water shader | Unity's own URP Water System sample (Package Manager) | Bundled w/ Unity 6 | Check first — likely yes |
| Water normal/foam textures (fallback only, if Water System doesn't fit) | ambientCG.com or Poly Haven | CC0 | No |
| Grass-tuft/clump clutter meshes | Poly Haven, Unity Asset Store free packs, or Kenney.nl | CC0 / free | No |
| Skybox/HDRI (optional) | Poly Haven HDRI section | CC0 | No |

## 6. Open decisions before starting T1

- Confirm Option A (shader-only, keep procedural mesh) vs. Option B (migrate to
  Unity `Terrain`) — recommendation is Option A, but this is the one real
  architectural fork in the whole plan and worth a deliberate yes/no.
- How many ground texture layers/biomes to support at launch (e.g. does every
  map share one grass/dirt/rock/sand set, or do future maps want distinct biomes
  like snow or desert) — affects how many texture sets to source now vs. later.

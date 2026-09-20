# Map/Terrain Visual Upgrade Plan — Toward AoE IV Fidelity

Status (updated 2026-09-20): **T1 (Option B, Unity Terrain) and T2 (water + shoreline)
are implemented and committed; T3/T4/T5 and the polish list in section 7 remain.**
Originally written 2026-09-17 per user request ("create a plan to reach the visual level
of AoE 4 for maps with all the asset sourcing needed"). Follows this project's usual
asset-brief convention (`docs/UI_ART_BRIEF.md`, `docs/TEAM_COLOR_ART_BRIEF.md`) — a plan
the user sources art against, that Claude Code then wires in. Sections 1-2 are the
original 2026-09-17 baseline, kept for history; section 0 is the current state.

This is deliberately a *separate* initiative from the map-layout work discussed the
same session (new `MapId` presets inspired by the downloaded AoE II/III map names,
a skirmish map-select UI). That work is data/UI. This is rendering quality — closing
the gap between "a flat-shaded procedural blob" and something that reads as a real
battlefield.


## 0. Current state and progress (2026-09-20)

Commits (branch `claude/scaffold-kingdoms-of-bharat`, **not yet pushed** — GitHub auth
failed with a 403 on the token; needs a token with write access to the repo):
`3a1917c` Terrain migration, `ecc0fc4` riverbed/beach/unwalkable water, `c692168`
water shader, `6fd8174` curved shore/refraction/waves/quality tiers. 601/601 EditMode
tests at every step.

**Done**
- **T1, Option B chosen and built**: `ProceduralGround` replaced by `ProceduralTerrain`
  (`Assets/Scripts/Core/ProceduralTerrain.cs`): a real `Terrain` + `TerrainCollider` on the
  "Ground" object, 257 heightmap from the old `HeightAt` noise, 4 TerrainLayers
  (Grass/Dirt/Rock/Sand) from `Resources/Terrain/<Layer>/{Albedo,Normal}.png`
  (ambientCG Grass005/Ground103/Ground093C + the owned PolishedSurfaces rock set).
  tileSize 4 looked fine, no tuning was needed. Mask maps deliberately not wired
  (channel packing unconfirmed). PNGs are Git LFS-tracked (`.gitattributes`).
- **T2, water and shoreline**:
  - Riverbed is real terrain, no hole: land eases to the waterline over 5 units, drops
    1.5 below it over 8; terrain transform sits at negative Y. Sand beach layer of noisy
    ~3.5-unit width; grass/dirt/rock weights come from the base noise so the bank slope
    doesn't paint as rock.
  - **Curved shoreline**: `WaterProximity.ShoreInsetAt(z)` (0..2 units, Perlin) moves the
    east/west waterline inward only, so water always stays inside the gameplay rectangle.
    Terrain, beach, NavMesh strips, boat clamp and fish spawns all read this one function.
  - **Units can't wade**: `NavMeshBaker` adds one `NavMeshBuildSourceShape.ModifierBox`
    (area 1) strip per metre of z following the shoreline. Non-Dock buildings are rejected
    inside the water rectangle (`BuildingPlacer.IsClearForKind`).
  - **Water shader** `Resources/Shaders/KobWater.shader` (`KingdomsOfBharat/Water`):
    depth-fade colour and opacity, two scrolling ripple normal layers, sun glint,
    flat-normal fresnel sky tint, foam line at the waterline, soft edge fade, vertex swell
    on a 2.5-unit grid mesh, refraction via the opaque texture (falls back to the
    undistorted sample when the bent one lands on something in front of the water).
  - Boats/docks play-tested; `WaterMover` keeps boats `MaxShoreInset + 1` off the sand
    (`WaterProximity.ClampToWater(point, inset)`, 1-arg overload unchanged).
  - Shipped in `Always Included Shaders`: URP Terrain/Lit.

- **T4, lighting and post-processing** (`Assets/Settings/MapPostProcess.asset` + `MapSky.mat`,
  `MapPostProcessVolume` in `Main.unity`): ACES tonemapping, bloom, colour adjustments
  (exposure +0.1, contrast +6, saturation -4), split-toned shadows/highlights, vignette,
  SMAA; sun at 38 deg pitch, warm, intensity 1.15; trilight ambient; tuned procedural sky;
  exp2 fog 0.0035; shadow distance 90; SSAO on the top-tier renderer only. Tuned from
  before/after screenshots (the first, stronger grade made grass neon and the horizon mustard).

- **Wet pebble band**: 5th terrain layer (`Resources/Terrain/Pebbles`, Poly Haven
  `floor_pebbles_01`, CC0, pre-darkened, smoothness 0.55) painted in a noisy band from ~1.6
  units above the waterline to ~5 under it, so the shallows show a pebbly bed.

**Findings worth keeping (each cost real debugging time)**
1. URP has **no default terrain material**: the terrain renders solid magenta until
   `terrain.materialTemplate` is set to `Universal Render Pipeline/Terrain/Lit`.
2. **ambientCG has no water material** (a "water" query returns Ice/Ground/
   SurfaceImperfections). The ripple normal map (`Resources/Terrain/Water/Normal.png`) is
   procedurally generated (band-limited FFT noise, tileable) — no licence question.
3. A plain `NavMeshBuildSourceShape.Box` is treated as **geometry and does NOT override
   the area**. Use `ModifierBox` with `area = 1` for not-walkable volumes.
4. The URP renderer's **Copy Depth Mode was `AfterTransparents`**, so transparent shaders
   read "far" depth everywhere. Now `AfterOpaques` on every renderer; Depth Texture and
   Opaque Texture are on for every quality-tier URP asset. Any transparent shader that
   reads scene depth/colour relies on this. (Diagnosed by outputting depth as colour.)
5. Depth-fade only works if there is ground under the water — the reason the hole was
   replaced with a real carved bed.
6. Fog of war (`FogOfWarManager`, plus the Canvases) hides the terrain in screenshots;
   disable both for verification shots.

## 7. Remaining work toward the AoE IV reference (prioritised)

Gap analysis against the user's AoE IV shoreline screenshot (translucent shallows showing
a pebbly bed, dark wet pebble strip, breaking foam, sky/building reflections, warm
lighting with depth haze). The biggest remaining gap is **lighting and post-processing**,
not water code. In payoff order:

| # | Item | Needs new art? | Notes |
|---|---|---|---|
| 1 | ~~**Lighting + post-processing (T4)**~~ DONE 2026-09-20: URP Volume with tone mapping, colour grading, bloom, SSAO; sun colour/angle/soft shadows; distance haze | No | Unused `DefaultVolumeProfile.asset` is the start; cheapest and biggest visual change |
| 2 | ~~**Wet-sand band + pebble layer**~~ DONE 2026-09-20 (Poly Haven floor_pebbles_01) | Pebble/gravel PBR set (ambientCG Gravel/Rocks) | Darken the sand just above the waterline via the same distance function; 5th terrain layer |
| 3 | ~~**Shore clutter (T3)**~~ DONE 2026-09-20/21 (reeds and driftwood added as procedural placeholders; tuft sway + distance LOD done): pebbles, reeds, driftwood, grass tufts, rocks | Meshes (Poly Haven / Kenney / Asset Store) | New instancing/scatter system; `EnvironmentPropFactory` has no rock/tuft category |
| 4 | ~~**Water reflections**~~ DONE 2026-09-20 (sky gradient) and 2026-09-21 (planar reflection of buildings/boats) | No | Reflection probe/skybox cubemap with fresnel, or planar reflection on the single water plane |
| 5 | **Caustics on the seabed** | Caustic texture | Animated projection in the shallows |
| 6 | ~~**Better foam**~~ DONE 2026-09-20 (procedural breakers; user's Asset Store foam pack could still be swapped in) | Foam texture | Advancing/retreating breakers; current foam is one static line |
| 7 | **Curve the north/south edges** | No | Same wobble idea as east/west; today only the x-edges wobble |
| 8 | ~~**Boat wakes and splashes**~~ DONE 2026-09-21 (stern trail + bow splash) | No (particles) | Trail on `WaterMover` |
| 9 | **River flow direction** | No | Scroll ripples along a flow vector instead of two fixed directions |
| 10 | ~~**Terrain macro variation / anti-tiling**~~ DONE 2026-09-20 | No | The "blocky tiling" was rock bleeding through on gentle slopes, fixed; added mid-frequency grass/dirt variation |
| 11 | **T5 resolution check** | No | Re-check the 257 heightmap/256 alphamap density up close now that it's a real Terrain |

Also open: profile the refraction/opaque-texture cost and put refraction behind a
quality tier; the map-layout work (new `MapId` presets, skirmish map-select UI,
multi-region water) is still separate and unstarted.

**Updated recommended order** (T4 now done): (2) wet-sand band, then
source the pebble set and do (2)'s pebble layer, (3) T3 clutter, (4) reflections and
foam, (5) the small items 7-9, (6) T5.

**Decisions still open**: source pebble/gravel textures and clutter meshes (user's job,
per project convention); whether reflections should be planar (better, costs a render)
or probe-based (cheaper); which quality tier gets refraction/reflection.

## 1. Original baseline, 2026-09-17 (historical — superseded by section 0)

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

### Phase T1 — Real ground textures + multi-layer terrain shader (highest impact) — DONE 2026-09-20 (Option B, see section 0)

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

### Phase T2 — Real water shader — DONE 2026-09-20 (custom shader, not the Unity Water System; see section 0)

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

### Phase T3 — DONE 2026-09-20 (tufts/rocks/pebbles, custom instanced renderer; see section 0) — Ground clutter (grass tufts, rocks, debris)

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

### Phase T4 — Lighting & post-processing pass — DONE 2026-09-20 (see section 0)

**Code work**: wire an actual `Volume`/`VolumeProfile` into the scene's Camera
(the unused `DefaultVolumeProfile.asset` is a starting point), tune shadow
settings on the main directional light, add a subtle color-grading/bloom stack,
and consider a light distance fog for depth. Since URP's post-processing is
bundled (no package to install), this is pure configuration/tuning work, not
asset sourcing — the cheapest phase in this whole plan.

**Assets needed**: none. Possibly a skybox/HDRI if the current sky reads flat,
sourceable free from Poly Haven's HDRI section if wanted — optional, not
required for the core visual jump.

### Phase T5 — Mesh/texture resolution check — OPEN (now about the Terrain heightmap, not a mesh)

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

## 6. Open decisions before starting T1 (resolved: Option B, one shared Grass/Dirt/Rock/Sand set)

- Confirm Option A (shader-only, keep procedural mesh) vs. Option B (migrate to
  Unity `Terrain`) — recommendation is Option A, but this is the one real
  architectural fork in the whole plan and worth a deliberate yes/no.
- How many ground texture layers/biomes to support at launch (e.g. does every
  map share one grass/dirt/rock/sand set, or do future maps want distinct biomes
  like snow or desert) — affects how many texture sets to source now vs. later.

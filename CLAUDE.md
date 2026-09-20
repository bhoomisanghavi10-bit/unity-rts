# Project: Kingdoms of Bharat

Historical RTS across Indian kingdoms/empires, targeting AoE II/IV-level systemic
depth. Full roadmap: `docs/KingdomsOfBharat_Master_Reference.xlsx` — see its
"Dev Status Overview" sheet first, then the "Roadmap - Open Items & Priority",
"Roadmap - Architecture Notes", "Roadmap - Process Note", "Roadmap - Art Direction",
and "Implementation Waves 0-6" sheets (docs/Roadmap.md and
docs/IMPLEMENTATION_ROADMAP.md are retired — their content lives on those sheets).

## Current status (keep current — update every session)
- **Planar water reflection closed (2026-09-21)** — `PlanarReflection` mirror camera into a 0.4x RT sampled by `KobWater`; new **Clutter** layer (slot 9, `Ground`=8 restored), Runtime asmdef now references URP. **Gotchas**: `manage_editor add_layer` overwrote the Ground layer — verify layers after any add; ProjectSettings edits need `File/Save Project` to persist. 611/611. Open: Settings/quality toggle for reflections (`PlanarReflection.Enabled`), profiling on real hardware, map-layout work.
- **Reeds, driftwood, sway and distance LOD closed (2026-09-21)** — `KobFoliage` shader (wind sway + smooth distance shrink) on tufts and reeds, half-density grass beyond 38 units, procedural reed/driftwood placeholders (`BharatRTS/Build Reed and Driftwood Clutter`; swap real prefabs into `ClutterSet.asset`). 607/607. Open: real reed/driftwood art, planar reflection, profiling on real hardware.
- **Boat wakes closed (2026-09-21)** — `BoatWake` (stern foam trail + bow splash, distance-driven, render queue 3100) attached by `WaterMover.Start`; `WaterProximity.SurfaceY` added. 603/603. Open: Kelvin V-wake, planar reflection, reeds/driftwood, tuft sway/LOD, profiling.
- **Shore pebbles now from the user's FBX (2026-09-21)** — 8 baked/decimated oval stones on 3 stone-colour materials, variants of the Pebble entry (`BharatRTS/Build Pebble Clutter`). Gotcha: each stone's oval shape lives in its node scale — bake the node transform into the mesh. **Licence of that FBX is unknown — confirm before shipping.** 601/601. Open: reeds/driftwood, sway/LOD, profiling.
- **Small rocks now Poly Haven `rock_moss_set_02` (2026-09-20)** — 7 decimated mossy rock variants scattered as the Rock clutter (`TerrainClutterSet.Entry.variants`, menu `BharatRTS/Build Moss Rock Clutter`). FBX gotchas: tagged cm but data in metres (`useFileScale=false`), Z-up mesh data (rotate 90 deg X in the builder). 601/601. Open: the Shore pebble entry is still the old placeholder — needs a rounded pebble mesh.
- **Rock terrain layer swapped (2026-09-20)** — Poly Haven `aerial_rocks_02` (mossy cliff rock) replaces the slab texture; only visible on steep slopes. 601/601. Leftover unused `Terrain/Rock/Mask.png`.
- **T3 ground clutter closed (2026-09-20)** — grass tufts, sparse rocks and shore pebbles scattered from alphamap weights and drawn with `Graphics.RenderMeshInstanced` (`TerrainClutter`/`TerrainClutterRenderer`/`TerrainClutterSet`, built by menu `BharatRTS/Build Terrain Clutter Set`). Unity's Terrain detail system draws nothing for meshes here, hence the custom renderer. 601/601. Rocks/pebbles are placeholders from the PolishedSurfaces set — user is sourcing a new rock asset (swap it into `Resources/Terrain/ClutterSet.asset`). User's `aerial_rocks_02` texture set could replace the slab-like Rock terrain layer. Open: reeds/driftwood, sway/LOD, profiling.
- **Water reflections + foam closed (2026-09-20)** — vertical-depth shader, breaker foam, analytic sky reflection, beach berm + gentler bed shelf. 601/601. Gotchas: `unity_SpecCube0` is black at runtime here and `RenderToCubemap` captures nothing under URP (use analytic sky); write `UnityEngine.Camera` inside `KingdomsOfBharat.*`. User added Asset Store foam/water VFX packages (not imported yet) — integrate once present. Open: planar reflection of buildings/boats, wakes, T3 clutter (needs meshes).
- **Ground blockiness fixed (2026-09-20)** — the blocky patches were the rock layer bleeding through on gentle hills (per-texel slope * 6), not texture tiling; rock now only on real steep slopes. Added mid-frequency grass/dirt variation, tighter grass/dirt tiles, fog on the water shader. 601/601. Next per `docs/MAP_VISUAL_UPGRADE_PLAN.md` section 7: reflections, better foam, T3 clutter (needs meshes).
- **Wet-sand/pebble band closed (2026-09-20)** — 5th terrain layer (Poly Haven `floor_pebbles_01`, pre-darkened, glossy) in a noisy band around the waterline, continuing under the surface. 601/601. Restart Play after changing terrain layer counts (cached `_layers`). Next per `docs/MAP_VISUAL_UPGRADE_PLAN.md` section 7: T3 clutter (needs meshes), reflections/foam, grass/dirt macro-tiling fix.
- **T4 lighting + post-processing closed (2026-09-20)** — global Volume (`Assets/Settings/MapPostProcess.asset`: ACES, bloom, grading, vignette), SMAA, warm low sun, trilight ambient, tuned sky, light haze, SSAO on the top-tier renderer only. 601/601. Next per `docs/MAP_VISUAL_UPGRADE_PLAN.md` section 7: wet-sand band + pebble layer (needs a sourced texture), then T3 clutter. Open: a few blocky notches in the beach edge (uninvestigated), SSAO cost not profiled.
- **Shoreline follow-ups closed (2026-09-20)** — curved shoreline (`WaterProximity.ShoreInsetAt`, shared by terrain + NavMesh strips), boat inset off the beach, water grid mesh with vertex swell, refraction (needs URP Opaque Texture), depth/opaque/CopyDepth=AfterOpaques on every quality tier. 601/601. Open: N/S edges straight, profile refraction cost.
- **Shoreline session 2 closed (2026-09-20)** — depth-fade water shader `KingdomsOfBharat/Water` (translucent shallows, ripples, glint, foam). ambientCG has no water material, so the ripple normal map is procedurally generated. Renderer `Copy Depth Mode` changed to After Opaques (transparents couldn't read depth otherwise). Terrain/Lit added to Always Included Shaders. 601/601. Open: refraction, wave motion, curved shoreline, boat/dock play-test on the new water.
- **Shoreline session 1 closed (2026-09-20)** — Coastal water is now a carved riverbed (sloped bank + sunken bed, no terrain hole), with a Sand terrain layer beach and an unwalkable water area via a NavMesh `ModifierBox` (plain `Box` sources do NOT override area — use `ModifierBox`). Non-Dock buildings can't be placed in the water rect. 601/601. **Next: session 2** — depth-fade translucent water shader, CC0 ambientCG water normal map, foam line, add URP Terrain/Lit to Always Included Shaders. Water is still the flat opaque quad until then.
- **Terrain migration (Option B) closed (2026-09-20)** — `ProceduralGround` replaced by `ProceduralTerrain` (Terrain + TerrainCollider, heightmap 257 from the old `HeightAt`, water hole via `SetHoles`, 3 TerrainLayers Grass/Dirt/Rock, same water quad). Verified live: 601/601 EditMode; grass/dirt blend renders; worker aligns to ground; NavMesh bakes and agents walk; Coastal water hole clean and unwalkable; RiverValley->Coastal rebuild has no errors. Fixed a real bug found live: terrain rendered magenta because URP has no default terrain material — now assigns `Universal Render Pipeline/Terrain/Lit`. **Flagged**: add that shader to Always Included Shaders before any player build; Sand layer/beach and mask maps unused. Terrain PNGs tracked via Git LFS. Out of scope still: clutter (T3), water shader (T2), skirmish map-select UI.
- **Wave 6 item 35 (Relics + Monastery collection, economic only) closed
  (2026-09-17)** — picked up per the user's "Let's do Relics + Monastery"
  request, following the design decision resolved 2026-09-15 (build it,
  economic only, no new victory condition; Monastery, Wave 4 item 27,
  already exists as the carrier building). New `Resources/Relic.cs`
  (neutral world prop, a plain held/unheld flag) and `Resources/
  RelicCarrier.cs` (walk/act one-shot pickup-then-deliver, same shape as
  `Trader`'s own walk/act/walk-back state machine but not an endless
  shuttle — a Relic is consumed into whichever Monastery it's delivered
  to). `RelicCarrier` was added to every land unit factory that already
  carries `GarrisonSeeker` (20 factories: Worker, every Barracks/Durg
  combat unit, Vaidya/Purohita/Vanik, every civ's unique unit except the
  two War Elephant factories and every Siege-class unit) — mirrors that
  item's own "any eligible land unit, not a dedicated collector type"
  precedent, matching this item's own "carried by any land unit" wording.
  Picking up reparents the Relic onto the carrying unit (no dedicated
  carried-Relic art exists yet — flagging per the flag-asset-needs
  convention — so this is a reparent-not-hide stand-in, same idiom
  `WeaponAttachment` already uses) rather than hiding it; a carrier that
  dies mid-carry drops the Relic at its exact death position
  (`RelicCarrier.OnDestroy` unparents it, live-verified). `Monastery.cs`
  gained `RelicCount`/`AddRelic()`/a passive Gold trickle (0.5 Gold/sec per
  Relic while complete — a first-pass rate, not independently balanced,
  same disclosed convention as `Trader.ComputeTradeGold`'s own rate) via a
  new `internal Tick(float deltaTime)` (same testability convention as
  `Farm.Tick`) and a pure `RelicGoldPerTick` formula. Relics spawn on the
  map via a new `ResourceNodeSpawner.SpawnRelic` (reuses that spawner's
  existing RNG/ring-placement machinery — no dedicated "Relics" art
  category exists yet either, so this always falls through to a plain
  primitive-sphere fallback, also flagged); new `MapDefinitionData.
  RelicCount = 5` on every map, deliberately NOT scaled with map area the
  way resource counts are — Relics are meant to stay scarce and contested
  regardless of map size. `SelectionManager` gained a `hitRelic` branch
  (neutral — no friendly-gate, any faction's unit can grab an unclaimed
  Relic first, matching AoE's own "first to reach it" rule) plus
  `relicCarrier?.CancelCarry()` wired into every other order branch's
  existing cancel chain, same convention every prior Wave 4 ability (Heal/
  Convert/TradeRoute) already established there. 10 new EditMode tests
  (`RelicTests.cs`: `Relic`'s held-state flag, `RelicCarrier.
  FindNearestOwnedMonastery`'s same-faction/exclude-other-factions/
  none-exist routing rule — mirroring `TraderTests.cs`'s own pattern for
  `FindNearestOwnedMarket` — the pure `RelicGoldPerTick` formula, and
  `Monastery.Tick`'s actual gold-deposit behavior against a real
  `ResourceStockpile` — 601/601 total, up from 591, all pass). Live-
  verified via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`) spawned 5 real Relics on
  RiverValley (matching the new `RelicCount`); a real `MonasteryFactory.
  Place`-spawned, `ConstructionSite.CompleteImmediately()`-completed
  Monastery; a real Player Worker's `RelicCarrier.PickUp()` reparented a
  real Relic onto it (`IsHeld=true`, `parent=Worker`) — then, left
  entirely to its own real `Update()`/`NavMeshAgent` loop with no further
  scripted intervention between tool calls, the worker auto-walked ~38
  real world units to the Monastery and deposited on its own
  (`Monastery.RelicCount` 0→1, the Relic `GameObject` destroyed, 4 Relics
  correctly remaining in the scene); the real Player Gold stockpile was
  independently confirmed climbing on its own between unrelated tool calls
  with no code driving it in that window (17.6 → 21.0 → 24.2 over
  consecutive real wall-clock gaps, consistent with 0.5 Gold/sec for the 1
  held Relic and no other active Gold source in that fresh match) —
  proving the live gold-trickle `Update()` loop actually runs, not just
  the isolated unit test. Separately live-verified the death-drop case: a
  second Relic was picked up, the carrying Worker was destroyed mid-route,
  and the Relic correctly reappeared unparented and `IsHeld=false` at the
  Worker's exact death position. Full EditMode suite re-confirmed 601/601
  after exiting Play mode. **No AI-side use of Relics** (no AI
  pickup/delivery hook) — new capability, not existing behavior moved off
  something the AI used before, matching every other Wave 4/6 item's own
  precedent for why skipping the AI hook isn't a regression. One scoped
  commit (`Relic.cs`/`RelicCarrier.cs` new, `Monastery.cs`,
  `ResourceNodeSpawner.cs`, `MapDefinition.cs`, `SelectionManager.cs`, the
  20 factory files, `RelicTests.cs` new, docs) — deliberately excludes
  the unrelated concurrent-session work already sitting in the tree
  (`TeamColorUnitTint.cs`, `corner_ornament.png`, `docs/PROJECT_TRACKER.html`,
  `.mcp.json`, `ProjectSettings/ProjectSettings.asset`), left untouched via
  targeted `git add`. Next: Deathmatch mode (item 38), King of the Hill
  zone-control victory (items 37/38), unit-side team colour once Blender
  masks are sourced, or UI/art polish/balance passes, user's call.
- **Shared gearless combat body sourced, decimated, rigged, and gear-attachment
  pipeline proven end-to-end (2026-09-16/17)** — ad hoc, not a numbered roadmap
  item, picked up from a design correction to the "Art Generation Prompts"
  sheet's new-unit sourcing prompts (a Skirmisher/Maharaja prompt review found
  they baked helmet/shield/weapon directly onto the character mesh, which
  defeats the whole point of item 5's per-civ gear-attachment system — fixed
  the prompt logic first: every new humanoid unit body must be sourced
  GEARLESS, with helmet/shield/weapon always separate equippable props
  attached via bone sockets, matching a fuller AoE modular-construction/
  rigging convention the user supplied and asked to be kept in memory — see
  `feedback_modular_unit_modeling_rigging.md`). Also corrected the per-civ
  gear table's Ancient tier to weapon-only (no helmet/shield until Classical),
  per a same-day user decision.

  User then supplied 2 real sourced assets to validate the corrected pipeline
  against: a gearless "Anatomical Figure" infantry body (`Meshy_AI_
  Anatomical_Figure_Running.glb`, misleadingly named — its bind pose is a
  clean standing T-pose, confirmed live via UnityMCP screenshot, not a frozen
  running pose) and a 3-piece generic gear set (helmet/shield/weapon,
  identified by bounding-box shape + actual baked PBR texture content, not
  filename — riveted metal + leather, matching the sheet's "Shared - Classical"
  tier exactly, no reclassification needed).

  **Built the real pipeline, not just inspected the assets**: imported the
  infantry glb (`Assets/importedmodels/InfantryBase/`, outside Resources per
  this project's own established raw-source convention), confirmed via Unity
  reflection it was 6,515 tris / 27 bones / no Avatar yet (glTFast doesn't
  auto-build one). Reused `HumanoidGltfRigImporter.DirectHumanBoneMap`
  **unmodified** — this rig's bone names (Hips/Chest/UpperChest/Neck/Head/
  LeftShoulder/LeftUpperArm/LeftLowerArm/LeftHand/...) matched that existing
  Vaidya/Purohita bone map exactly — to build a valid Humanoid Avatar
  (`isHuman=true`) and scale to the established 1.902692 worker-height
  convention, via a scratch `[MenuItem]` script (same "CodeDom can't
  round-trip a ValueTuple[] argument" workaround prior sessions already
  documented, deleted after use). Then decimated the saved prefab's
  `SkinnedMeshRenderer` mesh 6,515 → 2,199 tris via the same
  `UnityMeshSimplifier` package `BuildingMeshDecimator` already uses for
  buildings — confirmed bone weights (3,276) and bindposes (27) both survive
  the simplification intact, not just the triangle count. Saved to
  `Assets/Resources/human/SharedCombatBody/SharedCombatBody.prefab` (+ its
  `_Avatar.asset` and decimated mesh asset). The 3 gear pieces moved to
  `Assets/Resources/Gear/Shared_Classical/{Helmet,Shield,Weapon}.glb` —
  simple static props, correctly kept directly under Resources (no
  `Resources.LoadAll` ambiguous-collision risk the way character/building
  raw sources have, since `WeaponAttachment.AttachToBone`'s `Resources.Load`
  is an exact single-path lookup).

  Live-verified via UnityMCP through the real production path: a real
  `HumanModelFactory.Spawn(..., prefabPathOverride: "human/SharedCombatBody/
  SharedCombatBody", applyPaletteMaterial: false)` spawn, real
  `WeaponAttachment.AttachToBone` calls for all 3 gear pieces (RightHand/
  LeftLowerArm/Head sockets — the exact sockets this session's own
  gearless-body design assumes), and the real shared `HumanAnimationSet`
  Idle/Walk clips forced onto the spawned rig's own `PlayableGraph` —
  confirmed genuine retargeting (real mid-stride/idle articulation, not
  T-posed). Found and fixed 2 real placement bugs live, not shipped blind:
  the helmet's initial zero-offset attachment left it floating at neck
  height (measured the real bone-to-head-top delta, ~0.245-0.267 world
  units, and corrected the socket offset so it now sits on top of the
  skull); and the sword's blade pointed up out of the hand until the user
  flagged it from a live screenshot mid-session — fixed with a 180°
  X-axis rotation on the weapon's socket offset, tip now hangs down
  correctly. 590/590 EditMode tests pass unmodified (asset-pipeline-only
  session, no script changes survive - the temp Editor script was deleted
  after use). One scoped commit (`docs/KingdomsOfBharat_Master_Reference.xlsx`,
  `Assets/Resources/Gear/`, `Assets/Resources/human/SharedCombatBody/`,
  `Assets/importedmodels/InfantryBase/`) — deliberately excludes unrelated
  concurrent-session work already sitting in the tree
  (`MarathaMavlaRaiderFactory.cs`, `TeamColorUnitTint.cs`,
  `corner_ornament.png`, `ProjectSettings.asset`, `.mcp.json`,
  `docs/PROJECT_TRACKER.html`), left untouched via targeted `git add`. **Not
  done yet, flagged directly**: no factory (e.g. a `SoldierFactory`-style
  wiring) yet reads `SharedCombatBody` instead of the original Human
  Character Dummy — this session proved the body+gear pipeline works via
  direct `HumanModelFactory`/`WeaponAttachment` calls, not through any real
  unit's spawn path; only the Shared-Classical gear tier has real sourced
  art (Shared-Ancient weapon-only, and 10 civ-specific Durg/Imperial sets,
  remain unsourced per the gear table). Next: source/wire the remaining gear
  tiers, decide which real unit factories should switch to
  `SharedCombatBody`, or any other item, user's call.
- **Wall system, Session B (auto-tiling corner/end pieces) IN PROGRESS
  (2026-09-16)** — second session of the 4-part wall epic (see Session A
  below). User is generating Meshy AI corner-piece GLBs one tier at a time;
  each fused corner mesh (a right-angle turn combining a post/pillar with two
  wall arms extending away from it) needs splitting into reusable Pillar +
  Arm sub-meshes so the auto-tiling logic (not yet written) can compose
  arbitrary wall runs from a small kit of parts instead of needing a bespoke
  mesh per corner configuration. New reusable
  `Assets/Editor/WallPieceMeshSplitter.cs` — real geometric plane-clipping
  (`SplitCorner(sourceMeshResourcePath, pillarMinX, pillarMaxZ, destFolder,
  baseName)`), not naive triangle-vote bucketing (an early attempt at the
  latter produced spike artifacts on a low-poly test file). **Durg tier
  split, done and verified**: `Wall_Durg_Corner.glb` (583 verts/920 tris)
  split at `pillarMinX=0.28, pillarMaxZ=-0.28` — found by scanning per-Y-band
  XZ bounding boxes (the taller turret-cap geometry, y>0.08, is confined to
  that XZ square; the flat wall-arm bodies extend the rest of the length at
  y<0.08). Screenshots confirmed a clean octagonal turret Pillar and two Arms
  with clean flat cut faces, no spikes. **Ancient (wooden palisade) tier
  split, done and verified this session**: `Wall_Ancient_Corner.glb` (1788
  verts/3024 tris — a notably more detailed mesh than Durg's, so Durg's
  threshold was NOT reused blind). This asset's geometry doesn't cleanly
  separate by Y-band the way Durg's did — a continuous rubble base spans the
  *entire* tile footprint at low Y, with sharpened-stake tips of varying
  height scattered across both arms and the pillar alike (no single "tall
  cap" cluster) — so the split boundary was instead derived by scanning
  vertex density directly in the XZ plane: Arm A (extending in -X) is
  confined to `x<0.15`, z-band `[-0.5,-0.22]`; Arm B (extending in +Z) is
  confined to `z>0.25`, x-band `[0.21,0.48]`; the pillar's own post cluster
  sits at roughly `(x=0.35, z=-0.35)`, diagonally between the two arms.
  Split at `pillarMinX=0.18, pillarMaxZ=0.22` (both values sit inside the
  confirmed-empty gaps between each arm's stake band and the pillar
  cluster). Screenshot-verified: Pillar is a bundled cluster of sharpened
  stakes with rope lashings on a rubble base; both Arms are clean fence-line
  sections with sharpened stakes, rope rails, and clean angled cut faces
  where they'd join the pillar — no spike artifacts on any piece. Output
  assets for both tiers live alongside their source GLB under
  `Assets/Resources/buildings/_Source/Wall_{Tier}_Corner/` as
  `{baseName}_Pillar_split.asset`/`_ArmA_split.asset`/`_ArmB_split.asset`.
  **End-Cap reuse question, checked (2026-09-16)**: yes, both tiers' Pillar
  pieces double as a usable End-Cap (post + one Arm attached, other side
  left bare) — no separate End-Cap art needed. Durg's Pillar is unconditionally
  clean from any angle (its split boundary was a pure height cut that never
  touches the turret's own surface, so the piece is a complete radially-
  symmetric standalone tower regardless of how many arms attach). Ancient's
  Pillar is clean from every realistic gameplay-camera angle (top-down,
  elevated 3/4 outward view); a session first *mis-flagged* a diagonal
  element visible only from an atypical ground-level interior angle as a
  splitter defect, added real cap-filling geometry to
  `WallPieceMeshSplitter.cs` to fix it (chains each cut's open boundary loop
  into closed per-cross-section loops, ear-clip triangulates, seals with a
  flat outward-facing cap — a genuine correctness improvement, kept), then
  found the "artifact" was completely unchanged after that fix and, on
  re-inspection, present identically in the *original unsplit* source
  mesh — it's a real diagonal cross-brace strut baked into the asset's own
  design (architecturally sensible for a wooden palisade), not a splitting
  defect at all. Lesson for future tiers: compare against the unclipped
  source mesh at the same camera angle before diagnosing an odd-looking
  feature as a split artifact — the model may just be visually confusing
  from an angle no gameplay camera will ever actually use.
  **Classical tier ("Weathered Stone Corner") split and verified this
  session**: `Wall_Classical_Corner.glb` (1735 verts/2958 tris, Y extent
  only 0.24 vs. Ancient's 0.41 - a lower masonry wall profile). Same
  "continuous base spans the full tile" shape as Ancient (doesn't separate
  by Y-band), so the split boundary was again found via XZ vertex-density
  scanning: Arm A (extending in -X) confined to `x<0.25`, z-band
  `[-0.46,0.10]`; Arm B (extending in +Z) confined to `z>0.10`, x-band
  `[0.25,0.44]`. Unlike the wooden-stake tiers, this asset's pillar block
  is a substantial masonry buttress (not a compact post) that itself spans
  most of the negative-Z half of the tile, and each arm's far end carries
  its own small crenellated tower-cap rather than being a plain straight
  run - split at `pillarMinX=0.25, pillarMaxZ=0.10` (both exact, found by
  binary-searching the threshold where the confined side's opposite-axis
  range jumps discontinuously). Screenshot-verified both individually and
  composed (Pillar+ArmA+ArmB reassembled at the origin): a clean 90-degree
  crenellated stone corner with continuous battlements across all 3 pieces,
  no gaps/overlaps/spikes. One candidate "artifact" (a thin pole near Arm
  A's far end) was checked against the unsplit source mesh at the identical
  camera angle before flagging anything, per the lesson just above -
  confirmed present identically in the original source (a genuine
  flagpole/torch-mount detail on the tower cap, not a splitting defect).
  **Not yet done**: the 5 civs' Imperial-tier corners, the actual
  auto-tiling placement logic that consumes these split pieces, and
  End-Cap-reuse confirmation for Classical specifically (Durg's and
  Ancient's Pillars already confirmed reusable as End-Caps - Classical's
  Pillar looks even more clearly like a complete standalone structure given
  its own crenellated-tower-cap shape, but hasn't been explicitly spawned
  and checked that way yet). This session's work (mid-flight) is committed
  alongside Session A per explicit user confirmation, rather than held
  until more tiers land. Next: split whichever tier the user generates next
  (most likely the 5 civs' Imperial corners), following the same "inspect
  geometry per-asset, don't reuse a prior tier's thresholds blind" method -
  then eventually the auto-tiling placement logic itself, gate-interlocking
  (Session C), and the construction-rise shader (Session D).
- **Wall system, Session A (free-angle drag placement) closed (2026-09-16)** —
  first session of a new 4-part epic (drag-placement, auto-tiling corner/end
  pieces, gate-interlocking, a construction-rise shader), scoped in response
  to the user asking for AoE IV-style click-drag wall building. Investigated
  first: wall placement was click-once-per-segment only
  (`WallFactory.cs`'s own header comment already said so), with zero grid
  system, zero drag/multi-segment support, and no corner/end-piece art at
  all — auto-tiling/gate-interlocking are explicitly deferred to later
  sessions pending sourced art (user's own call via AskUserQuestion). This
  session delivers the prerequisite: mouse-down/drag/mouse-up placement of a
  free-angle (not axis-snapped — user's explicit choice over the cheaper
  axis-aligned-only option) chain of the *existing* Wall segment prefab, no
  new art needed. One pure function drives everything:
  `BuildingPlacer.ComputeWallChain(anchor, current, segmentSpacing,
  maxSegments)` — before the mouse is even pressed, anchor==current, which
  trivially degrades to a single segment at the cursor, so a plain
  click-without-drag is completely unaffected (same behavior as before this
  session). Rotation uses `Quaternion.FromToRotation(Vector3.right, dir)`,
  not `LookRotation`, since the Wall model's long axis is local +X (its
  `Size.x = 2.4`, the largest horizontal dimension). New `WallFactory.Place`
  overload takes an explicit rotation (the 3-arg original becomes a thin
  wrapper passing `Quaternion.identity` - every other call site untouched);
  `BuildingModelFactory.Spawn`'s root is confirmed (via direct code reading)
  to always return at identity rotation, so setting it afterward in
  `WallFactory.Place` doesn't fight anything Spawn itself does, and needed
  zero changes to `BuildingModelFactory.cs`. `BuildingPlacer.IsClearForKind`
  already used a rotation-agnostic circular-distance check for Wall (not the
  AABB-based `BuildingFootprint.IsClear` every other kind uses), and
  `NavMeshObstacle`'s Box shape inherits orientation from its own transform
  automatically — both needed zero changes for rotated segments to work
  correctly. **Known, accepted limitation, not fixed this session**:
  `BuildingFootprint` (used by every *other* building kind's own placement
  check to avoid overlapping a Wall) only stores an axis-aligned size with
  no rotation awareness — a building placed near a diagonal wall segment
  could get a slightly wrong overlap result. Pre-existing limitation of the
  whole footprint system (nothing today supports oriented-bounding-box
  checks), not newly introduced here. Multiplayer: `NetMessageEnvelope`
  gained one new field (`buildRotationY`, a single Y-axis Euler angle - a
  full quaternion over the wire is unnecessary for a ground-plane
  structure), `CommandSerializer.ForBuild` gained an additive overload -
  every other BuildingKind's existing call sites are untouched, passing 0.
  Per-segment affordability preview (a long drag's tail reddens once the
  running cost would exceed the live stockpile, mirroring AoE's own
  "greys out what you can't afford" chain cue) reuses the exact same cost
  formula `CanAfford` already computes for one Wall - no new pricing logic.
  9 new EditMode tests (`WallChainPlacementTests.cs`, 590 total, up from
  581, all pass) covering the zero-distance/click-without-drag degenerate
  case, exact segment spacing, diagonal rotation alignment, and the
  max-chain-length clamp. Live-verified via UnityMCP through the real
  production path (not just the unit tests): a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real diagonal drag (10 units
  along both X and Z) confirmed via reflection into
  `BuildingPlacer.ComputeWallChain`/`ConfirmWallChain` produced exactly the
  predicted 7 segments, each spawned via the real `CommandBus`-deferred
  `WallFactory.Place` path at the correct evenly-spaced positions with the
  correct shared 315° rotation and each segment's own real per-point ground
  height (not a flat approximation - confirmed by the Y coordinates varying
  slightly per segment, matching actual terrain); screenshotted both a
  top-down and an oblique view showing a clean, correctly-oriented diagonal
  wall line, crenellations up, no upside-down/sideways issues. Separately
  confirmed a plain click-without-drag still places exactly one identity-
  rotated Wall segment, byte-for-byte the same as before this session. Hit
  and worked around one real test-setup mistake along the way (not a code
  bug): a first live-drag attempt was anchored at the exact corner of the
  map's 100x100 ground collider, so most of the chain's segments landed
  off the playable ground entirely and were correctly skipped by
  `ConfirmWallChain`'s own ground-check guard - re-anchored from a properly
  central point and it worked as expected. Full EditMode suite re-confirmed
  590/590 after exiting Play mode. **This closes Session A** — corner/
  end-piece auto-tiling (Session B, needs sourced or generated art matching
  the per-age Wall reference images already delivered this project),
  gate-interlocking (Session C), and the construction-rise shader
  (Session D, fully independent, could be done anytime) remain open, per
  the scoped 4-session plan. Next: user's call among those, or any other
  item.
- **Age-tiered building mesh decimation closed (2026-09-16)** — follow-up to
  the 2026-09-15 critical-regression fix's own flagged gap: the original
  2026-09-02 mesh-decimation pass only ever covered the 45 Imperial-tier
  civ-specific building prefabs, never the age-tiered TownCenter/Tower/Wall
  variants (Ancient/Classical/Durg — a wholly separate system, resolved via
  `AgeTieredBuildingVisual`/`BuildingModelFactory`'s age-suffixed Resources
  paths) — those shipped at their original ~1.9-3.0M un-decimated triangles
  even after the Imperial-tier pass landed, and got restored at that same
  raw triangle count again during the 2026-09-15 regression fix. Picked up
  mid-flight: this session found substantial uncommitted work already in
  the tree from a concurrent session (`BuildingMeshDecimator.cs` extended
  with a new `DecimateAllAgeTiered`/`DecimateBuildingAt` path, decimated
  assets already generated for 5 of 13 age-tiered targets) — flagged to the
  user via AskUserQuestion rather than guessed at; user confirmed finishing
  it. Extended `BuildingMeshDecimator`'s existing per-building decimation
  method into a shared `DecimateBuildingAt` used by both the original
  civ-rooted Imperial-tier call sites and a new `AgeTieredTargets` table (13
  entries: TownCenter/Tower/Wall × Ancient/Classical, plus TownCenter Durg
  per civ and one shared Tower/Wall Durg) — same 500,000-tri target the
  original pass already established as the real, screenshot-verified-clean
  ceiling. Source-folder scanning generalized to handle the `_v2`-suffixed
  filenames the September regression fix's cache-corruption workaround left
  behind, and a real corrupted-source case (Wall_Durg's shared FBX reports
  0 vertices/bounds while `triangles.Length` still claims ~3.08M stale
  indices — reproduced identically on a fresh-GUID copy and under forced
  `isReadable=true`/`indexFormat=UInt32`, ruling out every cache/import-
  setting explanation) is caught explicitly and skipped rather than crashing
  the whole batch — that one building correctly still spawns un-decimated
  via `BuildingModelFactory`'s existing fallback chain, flagged as a real
  asset-sourcing gap, not silently patched over. Extended
  `BuildingPolycountTests.cs` with a matching
  `AgeTieredBuildingModels_StayUnderTriangleCeiling_ForEveryCivAndAge` test
  (deliberately excluding Wall/Durg, with the reason documented inline) so a
  future regression back to multi-million-triangle age-tiered models fails
  loudly the same way the Imperial-tier one already does. 581/581 EditMode
  tests pass (the 2 previously-standing `BuildingModelFactoryTests`
  failures are gone too — apparently fixed by the concurrent session's own
  restore-and-repair work, not by anything touched here). Live-verified via
  UnityMCP through the real `BuildingModelFactory.Spawn` production path
  (not just the test suite): all 12 reachable age-tiered targets across all
  5 civs now measure ~500,000 tris (was ~1.9-3.0M), and Wall_Durg correctly
  still spawns at its original ~3.08M tris with no exception thrown — exact
  numbers logged in `docs/SESSION_LOG.md`. One scoped commit
  (`BuildingMeshDecimator.cs`, `BuildingPolycountTests.cs`, the 12 modified
  age-tiered prefabs + their now-decimated meshes, 3 incidentally-touched
  `.mat` LFS pointers on Chola/Maurya/Vijayanagara's `TownCenter_Durg`
  materials from the same reimport) — deliberately excludes unrelated
  concurrent-session work already sitting in the tree
  (`MarathaMavlaRaiderFactory.cs`, `TeamColorUnitTint.cs`,
  `corner_ornament.png`, `docs/PROJECT_TRACKER.html`, `.mcp.json`,
  `ProjectSettings/ProjectSettings.asset`'s unrelated Cloud-project-ID
  change), left untouched via targeted `git add`. This closes the
  building-visual-regression saga's last flagged follow-up item. Next:
  Wave 6's 3 remaining design-decided-but-unbuilt items (Relics + Monastery
  collection, Deathmatch, King of the Hill), unit-side team colour once
  Blender masks are sourced, or UI/art/balance polish — user's call.
- **Worker (villager) Idle/Walk/Gather/Farm/Attack clips swapped to a new
  shared UAL clip pack (2026-09-16)** — ad hoc, not a numbered roadmap item.
  Imported `UAL1_Standard.fbx` (Idle_Loop, Walk_Loop) and `UAL2_Standard.fbx`
  (TreeChopping_Loop → Gather, Farm_Harvest → Farm, Sword_Regular_A → Attack)
  into `Assets/Resources/human/Human Animations/`, both set to Humanoid/
  non-root-motion (`lockRoot*`/`keepOriginal*` all true) — both produced a
  valid `isHuman=true` Avatar on import, confirmed live, not assumed. Unlike
  every existing clip in that folder (each shipped as its own single-clip
  FBX, so a plain `Resources.Load<AnimationClip>(path)` just works), these
  two source files each bundle 43 takes into one FBX — `Resources.Load`
  on the FBX path resolves ambiguously (picks whichever clip happens to load
  first, not the one wanted), so `HumanAnimationSet.cs` gained a new
  `LoadNamed(path, clipName)` helper (`Resources.LoadAll<AnimationClip>` +
  exact-name filter) instead. Mine and Build stay on the original per-gender
  Kevin Iglesias clips (no equivalent in the UAL pack) — genuinely shared
  across both Male/Female dummy bodies now for the other 5, since this
  project's retargeting is Avatar-based, not skeleton-name-based, confirmed
  by live-forcing each clip onto a real spawned Worker. 581/581 EditMode
  tests pass (0 pre-existing failures this run — an improvement on the prior
  session's own 2-failure `BuildingModelFactoryTests` baseline, unrelated to
  this change either way). Live-verified via UnityMCP: spawned a real Worker
  via `WorkerFactory.Spawn`, force-set each of the 5 new clips directly on
  its `AnimationDriver`'s `PlayableGraph` and screenshotted. **Found and
  worked around a real verification trap along the way**: forcing a clip via
  reflection while the graph's default `DirectorUpdateMode` (GameTime) was
  still active looked like it silently failed — the pose kept reading as
  Idle no matter which clip/time was forced — because the graph auto-
  advances every real frame independent of the `AnimationDriver`
  MonoBehaviour's own `enabled`/`Update()` state; disabling the driver only
  stops the C# script's own per-frame `ResolveClip()` override, it doesn't
  freeze the underlying Playables graph. Fixed by calling
  `graph.SetTimeUpdateMode(DirectorUpdateMode.Manual)` before forcing a time,
  which is what actually let the screenshots hold a specific frame. Once
  fixed, all 5 clips confirmed genuinely posing (not T-posed): Idle (relaxed
  stance), Walk (mid-stride leg crossing), Gather (chopping swing, braced
  stance), Farm (bent-forward harvest reach), Attack (dynamic sword lunge).
  One scoped commit (`HumanAnimationSet.cs`, the 2 new FBX + `.meta` files) —
  deliberately excludes substantial unrelated uncommitted work already
  sitting in the tree from other sessions (`BuildingMeshDecimator.cs`, 6
  `TownCenter_Durg` prefabs/materials, `MarathaMavlaRaiderFactory.cs`, the
  new `_Decimated/` assets, `corner_ornament.png`, `.mcp.json`), left
  untouched via targeted `git add`. Next: whatever the user directs.
- **Critical regression fixed (2026-09-15): every building's visual model was
  broken project-wide — not just starting-age TownCenter as first reported.**
  Root cause: commit `a5d2b15` ("Remove leftover raw _Source FBX exports...",
  2026-09-13) deleted the entire `_Source/` folder tree (raw FBX + materials +
  textures) for every civ/age-tier building, on the mistaken assumption that
  `BuildingMeshDecimator`'s decimated meshes were already baked directly into
  the consumed prefabs. They weren't — every consumed building prefab
  (TownCenter/Tower/Wall/Barracks/Market/Farm/House/Gate/Dock, all 5 civs, all
  age tiers) is a **Prefab Variant** built on top of those now-deleted source
  files, so deleting `_Source` turned every one of them into a "Missing
  Prefab" with zero geometry — confirmed live via UnityMCP across all 56
  civ/building/age-tier combinations, not assumed from the one symptom
  reported (starting-age TownCenter). The `_Decimated/*.asset` mesh assets
  (45 of them, one per civ-specific Imperial building) exist independently
  but were never wired into `BuildingModelFactory`'s spawn path at all —
  a separate, still-open gap (see below).

  Fixed by reverting `a5d2b15`'s asset deletion (the files are Git LFS-
  tracked and the LFS blobs were still cached locally, so no re-download was
  needed — confirmed byte-identical via checksum against the original LFS
  oid). Kept `a5d2b15`'s own screenshot-cleanup/`.gitignore` change intact
  (that part was legitimate, unrelated to the bug) rather than blanket-
  reverting. Disk space was the real obstacle throughout this fix — the
  ~6.5GB restore plus Unity's own Library-cache growth during reimport hit
  actual `ENOSPC` twice (145MB and then 217MB free), causing 7 of the
  restored FBX imports to genuinely crash mid-import and produce a "default
  asset" placeholder (0 vertices) that **persisted through repeated plain
  reimports** — traced to a corrupted import-cache entry keyed to that exact
  filename (confirmed by reimporting a byte-identical copy under a new
  filename, which produced correct geometry immediately). Fixed those 7 by
  renaming the affected source FBX to a fresh filename (forcing a new cache
  key) and repointing the one consuming prefab's GUID reference via text
  edit — `TownCenter_Ancient_model.fbx`/`Tower_Ancient_model.fbx`/
  `Wall_Ancient_model.fbx`/`TownCenter_Classical_model.fbx` (all now suffixed
  `_v2` on disk — cosmetic only, deliberately not renamed back to avoid
  re-triggering the same cache corruption) and Chola/Maurya/Maratha's
  `TownCenter_Durg_model.fbx` (same fix, same reason).

  Live-verified via UnityMCP through the real `BuildingModelFactory.Spawn`
  path (not just raw `Resources.Load`) across all 5 civs × all 4 ages for
  TownCenter specifically (the reported symptom) and all 56 civ/building/
  age-tier combinations generally — 56/56 now resolve real geometry (verts
  in the hundreds of thousands to ~2M range, matching pre-regression
  expectations). **EditMode test suite was NOT run this session** — disk
  space stayed critical (as low as ~210MB free) throughout, and the user
  explicitly chose to skip the test run rather than risk another crash;
  verification relied on live in-Unity geometry checks only. Flagging this
  directly: run the EditMode suite first thing next session, once there's
  real disk headroom, to catch anything the live checks didn't cover.

  **Still open, not attempted this session**: wiring the 45 existing
  `_Decimated/*.asset` meshes into `BuildingModelFactory`'s actual spawn path
  so buildings render at their intended ~500,000-tri decimated count instead
  of the raw ~1-2M-tri source meshes now back in play — this was the original
  intent of the 2026-09-02 decimation session and is the right permanent fix,
  but is real follow-up scope, not a same-session fix. Also: the
  `_Source` folders for Durg-tier Rajput/Vijayanagara TownCenter never had a
  matching git-history deletion to revert from (they were restored cleanly,
  no cache-corruption fix needed for those two). Next: wire `_Decimated`
  meshes into the spawn path, or run the EditMode suite once disk space is
  comfortably free, user's call.
- **Relics/Wonder/game-modes design decision RESOLVED (2026-09-15) — no code
  written this session, decision-only per the user's explicit "resolve the
  Relics/Wonder/game-modes design decision" request.** This was the one
  remaining blocker on Wave 6 items 35/37/38 (all 3 needed a design decision
  before any implementation, per their own roadmap text). Resolved via 2
  rounds of AskUserQuestion: **(1) Relics + Monastery collection: build it,
  economic only.** Relics spawn on the map (or drop from destroyed scripted
  targets), carried by any land unit to a Monastery (the existing Wave 4 item
  27 building — no new building needed) for a passive Gold trickle, AoE II's
  own base-case mechanic. Explicitly declined a Relic-count victory condition
  (user picked "Economic only" over "also add a Relic-count victory option")
  — keeps this item's scope smaller, no new win-condition plumbing needed.
  **(2) Wonder is declined entirely** — the user did not pick "Wonder +
  King-of-the-Hill-style victory" from the first round's option set. No
  Wonder building, no Wonder-triggered victory countdown; item 37's own
  roadmap text is now closed-as-declined for the Wonder/Relic-victory half
  (Time Limit + Regicide were already closed). **(3) Game modes: build
  Deathmatch + King of the Hill.** Deathmatch is an opt-in `GameSettings`
  toggle (same pattern as `RegicideEnabled`) — match starts with large
  stockpiles of every resource and a higher starting Age, reusing existing
  resource-grant/age-set code paths, skipping the early economy game. King of
  the Hill is a genuinely new victory condition (a map-marked zone; the
  faction holding it uncontested for N minutes wins — needs a new
  zone-control tracker and countdown, similar in shape to the existing
  `SurviveSeconds` mission-objective kind but faction-vs-faction and
  area-based rather than tied to a single scripted mission). Updated
  `docs/KingdomsOfBharat_Master_Reference.xlsx`'s "Dev Status Overview" and
  "Implementation Waves 0-6" sheets (items 35/37/38, rows 39/41/42) to
  reflect all of the above — docs-only session, no code/tests touched, no
  commit needed (nothing to build/test yet). Each of Relics+Monastery,
  Deathmatch, and King of the Hill is now unblocked and is its own future
  one-item-per-session pickup, per this project's own "one roadmap item per
  session" protocol — not attempted together in one session. Next: user's
  call among those 3, plus item 36's already-closed Score system giving KotH
  and Deathmatch something to interact with once built, unit-side team
  colour once Blender masks are sourced, or UI/art/balance polish.
- **Wave 6 item 40 (Tutorial content) closed (2026-09-15) — this closes every
  decision-free Wave 6 item.** Picked right after item 36 closed, per the
  user's "start tutorial content" request. New CSV-authored mission "The
  First Lesson" (`tutorial_basics`), riding the existing `MissionCsvLoader`/
  `MissionObjective`/`MissionTrigger`/`ScenarioManager` system per this
  item's own roadmap text ("no new system needed") — purely 3 new rows
  added to the existing `mission_definitions`/`mission_objectives`/
  `mission_triggers` CSVs, no new C# game logic. 5 objectives, deliberately
  one of each existing `ObjectiveKind` (teaching gather/build/grow-
  population/hold-ground/attack in that order): `ResourceThreshold`
  ("Gather 100 Wood"), `BuildingCountThreshold` ("Build a House"),
  `PopulationThreshold` ("Grow your population to 6"), `SurviveSeconds`
  ("Hold your ground for 90 seconds"), `DestroyScriptedTarget` (a practice
  Barracks spawned near the *player's own* base, not the enemy's — an easy
  first combat lesson, not a raid into enemy territory). Player=Maurya
  (its "Houses cost no Wood" bonus and Classical-age start both pair
  naturally with the lesson content), AI=Maratha, Map=RiverValley. One
  `GrantResourceAtTime` trigger (100 Gold at t=20s) exercises the trigger
  pipeline too, matching every other mission's own precedent.

  **Found and fixed a real, previously-unnoticed cosmetic bug while
  authoring this, not before it**: `ObjectivePanel.cs`'s box was a fixed
  140-tall rect, sized for the 3 original hand-coded missions' own
  single-objective checklists — this tutorial is the first mission in the
  project with more than 1 objective, and 5 objectives would have
  overflowed the box by ~22 units with no mask to clip it (confirmed by
  the exact row-position math before touching anything). Fixed by having
  the panel grow to fit its actual row count
  (`Mathf.Max(140, header + rows×26 + margin)`), the same "resize to
  content" precedent `SelectedUnitPanel`/`BuildMenu` already established
  elsewhere in this project — a small, generic, low-risk UI fix that
  benefits any future multi-objective mission, not new game logic.

  3 new EditMode tests (`TutorialMissionTests.cs`) — unlike
  `MissionCsvLoaderTests.cs`/`MissionRowsTests.cs` (which drive the shared
  interpreter with small synthetic in-memory CSV strings), these are the
  project's first tests to exercise the *real* Resources-loaded mission
  CSV files directly (`MissionCsvLoader.LoadAll()`/`ReadCsv` against the
  actual files on disk), catching the class of real formatting mistake —
  a bad quote, a stray comma, a mis-typed enum value — that synthetic
  tests structurally can't. 580/580 EditMode tests pass (2 pre-existing,
  unrelated `BuildingModelFactoryTests` failures, the standing baseline).

  Live-verified via UnityMCP through the real production path: a real
  match via `CivilizationSetup.BeginScenarioMatch` against the *real*
  `tutorial_basics` `ScenarioDefinition` loaded from
  `ScenarioRegistry.All` (not a synthetic one) — the real `ObjectivePanel`
  rendered all 5 lessons with the grown box and no overflow, the real
  `MissionToast` showed the FlavorText on mission start; granting 100
  Wood, spawning 2 Workers, completing a real House, and killing a real
  spawned "practice Barracks" via a real `Attackable.TakeDamage` call each
  correctly flipped their objective to complete (strikethrough,
  screenshot-confirmed) and fired the matching toast `CompleteText`; the
  real trigger correctly granted 100 Gold at t=20s; `MatchManager.Outcome`
  correctly stayed `Ongoing` with 4/5 objectives complete — the first live
  confirmation that `ScenarioManager.EvaluateOutcome`'s AND-across-all-
  objectives victory logic (previously only ever exercised by single-
  objective missions) works correctly for a genuinely multi-objective
  mission. The 5th objective (`SurviveSeconds`) was deliberately not run
  to its full 90-second completion live — not a gap, since it shares the
  exact code path `DefendHampi`'s own `SurviveSeconds` objective already
  uses (previously live-verified in an earlier session) and has its own
  dedicated passing unit test; a full real-time wait wouldn't have added
  meaningful confidence for the budget it would cost.

  One scoped commit (`ObjectivePanel.cs`, the 3 mission CSVs, the new
  `TutorialMissionTests.cs`, docs). **This closes every decision-free
  Wave 6 item** — Town Bell, idle-worker indicator, cheat codes, Score
  system, and now Tutorial content are all done. Next: the Relics/Wonder-
  KotH-victory-conditions/game-modes design decision (the only remaining
  Wave 6 items, all gated on a design decision first), unit-side team
  colour once Blender masks are sourced, or further UI/art polish/balance
  work — user's call.
- **Wave 6 item 36 (Score system) closed (2026-09-15)** — picked immediately
  after closing item 39's live-verification gap in the same session. Four
  weighted categories modeled on AoE II's own Military/Economy/Technology/
  Society split — not a byte-for-byte port of AoE II's real (undocumented)
  weights, just the same shape, same "reuse the closest precedent, not
  independently balanced" convention every tier line in this project
  already follows: **Military** = kills×3 + buildings razed×5 + living
  combat units×1; **Economy** = current stockpile total×0.05 + worker
  count×3 (a live-computed proxy, not a lifetime-gathered total — no
  single choke point exists across Gatherer/Farm/LivestockWorker/Trader/
  BoatTrader/scripted mission grants for that, and adding one would be
  real new bookkeeping scope beyond this item); **Technology** = Age
  ordinal×30 + flat Attack/Armor tiers×15 + unique tech researched?50:0
  (deliberately doesn't also sum the 11 separate per-unit-class tier-line
  classes — CavalryLineProgress, ArcherLineProgress, etc. — no shared
  interface exists across them, flagged as a real future refinement, not
  attempted here to keep this session-sized); **Society** = Population×4
  + complete buildings×3 (Wonders/Relics don't exist yet — item 35 is
  unbuilt — so these are the closest available proxies to AoE II's own
  Society score).

  New `Progression/ScoreProgress.cs` follows this project's established
  "recompute, don't incrementally track" convention (same as `Population`'s
  own header comment) for every category except kills/razings — the one
  genuine exception, since a dead unit can't be recounted after the fact.
  Those two are credited from a new hook added to `Attackable.TakeDamage`'s
  existing death branch: `attacker`'s faction (not the victim's) gets
  `ScoreProgress.RecordKill`, crediting a kill or a razing depending on
  `unitClass == Building`. New `ScoreProgress.Reset()` wired into
  `CivilizationSetup.BeginMatchCore` alongside `DiplomacyRegistry.Reset()`/
  `TeamColorBuildingTint.Reset()`. `GameOverScreen.cs` gained a code-built
  score-breakdown label (Player vs Enemy, not the optional 3rd faction)
  shown alongside the existing Victory/Defeat/Draw title — built entirely
  in `Awake()`, no new scene-wired `[SerializeField]`, avoiding the
  recurring "new field null in the scene" gotcha several other sessions
  have hit.

  **Found and fixed a real, previously-unnoticed bug while live-verifying,
  not caused by this item**: `GameOverScreen`'s own root GameObject was
  saved `m_IsActive: 0` in the scene — confirmed directly by reading
  `Assets/Scenes/Main.unity`'s raw YAML, not assumed — meaning its
  `Awake()`/`Update()` never ran at all (only its child "Panel" was ever
  meant to toggle via `panelRoot.SetActive(false)`), so the entire
  Victory/Defeat/Draw screen had apparently never actually shown in a real
  match before this session, in spite of the status log describing it as
  an existing working feature. Fixed via `manage_gameobject`
  (`set_active: true`) against the live Editor scene object — not a raw
  text edit, since a direct `.unity` file edit while Unity has the scene
  open doesn't reliably take effect (tried first, confirmed it silently
  didn't stick) — then `manage_scene(action: save)` to persist it.

  22 new EditMode tests (`ScoreProgressTests.cs`, mirroring
  `IdleWorkerFinderTests.cs`/`AgeUpRequirementTests.cs`'s own
  register-directly-into-`Unit.All`/`Building.All` pattern for the OnEnable-
  timing gotcha, and `BuildingAttackerTests.cs`'s `TickIgnoringVfxLogs`
  pattern for `Attackable.TakeDamage`'s Editor-only VFX-destroy log). Hit
  and fixed one real cross-test-leakage issue during the first test run:
  `UniqueTechProgress.MarkResearched` has no unmark/reset anywhere in this
  project, so two Technology tests using absolute expected values failed
  once a different test's `MarkResearched(TestFaction)` call leaked into
  the same domain — fixed by switching all three Technology tests to
  before/after delta assertions instead of absolute ones, immune to that
  leak regardless of NUnit's undefined test execution order. 577/577
  EditMode tests pass after the fix (2 pre-existing, unrelated
  `BuildingModelFactoryTests` failures, the standing baseline). Also hit,
  a second time this same session, this project's own documented "exiting
  Play Mode doesn't itself trigger a domain reload, so static state
  briefly bleeds into the next EditMode run" gotcha (a large cascade of
  unrelated-looking resource-deduction test failures appeared right after
  exiting Play mode) — resolved the same documented way, forcing a real
  domain reload (`refresh_unity(mode=force, compile=request)`) before the
  final, definitive run.

  Live-verified via UnityMCP through the real production path: a real
  match (`CivilizationSetup.BeginMatch(Maurya)`), `ScoreProgress.Compute`
  matched hand-computed expectations exactly against real starting state
  (Maurya's Classical-age start bonus → Technology 30, 4 starting workers
  → Economy 12, Population×4 + TownCenter×3 → Society 19); a real lethal
  `Attackable.TakeDamage` call (via `SoldierFactory.Spawn`-spawned units)
  correctly credited the attacker's faction with exactly +1 kill, and a
  separate lethal hit on a real `TowerFactory.Place`-spawned, completed
  Tower correctly credited +1 razing instead; forcing
  `MatchManager.ForceOutcome(Victory)` after that combat now shows a real,
  correctly laid-out score panel (Military 4 vs 0, Economy 12 vs 12,
  Technology 30 vs 0, Society 23 vs 19, Total 69 vs 31 — every number
  independently verified against the live game state) with no clipping or
  overlap against the title/Play Again button, screenshot-confirmed. One
  scoped commit (`ScoreProgress.cs` new, `Attackable.cs`,
  `CivilizationSetup.cs`, `GameOverScreen.cs`, `Main.unity`, the new
  `ScoreProgressTests.cs`, docs). Next: tutorial content (40, the last
  decision-free Wave 6 item), the Relics/Wonder/game-modes design
  decision, or unit-side team colour once Blender masks are sourced.
- **Wave 6 item 39 (Cheat codes) live-verification gap closed (2026-09-15,
  same-day follow-up session)** — the prior session flagged that both
  `unity`/`UnityMCP` MCP servers were unreachable despite a real Editor +
  bridge process running, so the EditMode suite was never run and nothing
  was checked live. Root cause of that gap, confirmed this session: it was
  this session's own MCP *client* connection, not the bridge — a raw
  `curl -X POST http://127.0.0.1:8080/mcp` (the bridge's real HTTP
  endpoint, read from `.mcp.json`) answered `initialize` cleanly, and
  reconnecting the `unity` tools via `ToolSearch` + a `read_console` call
  worked immediately. Ran the real EditMode suite: 528/528 pass (2
  pre-existing, unrelated `BuildingModelFactoryTests` failures, the
  standing baseline — confirmed the 17 new Cheat* tests from the prior
  session are included and green). **Found and fixed a second, real gap
  while live-verifying in Play mode, not before it**: `Cheat*` types
  weren't resolving via reflection in a running Play session at all —
  `KingdomsOfBharat.Runtime.dll` on disk was timestamped ~14 hours before
  the `Cheat*.cs` source files, meaning the prior session's own edits had
  never actually triggered a real Unity recompile (the EditMode test
  runner apparently compiles its own test-context assembly independently,
  which is why 528/528 could pass while Play mode ran a stale DLL) — fixed
  by forcing one via `refresh_unity(mode=force, compile=request)`,
  confirmed via the DLL's changed timestamp/size and the types then
  resolving. Live-verified via UnityMCP through the real production path
  from there: a real match (`CivilizationSetup.BeginMatch(Maurya)`), the
  real `CheatConsole` panel force-opened and every command invoked through
  the real private `OnSubmit` method (not a shortcut, and not the raw
  `CheatCodes.Execute` call): `wood 500` correctly added to the real
  `ResourceStockpile` (0→500), `help` printed the command list, `reveal`
  flipped `FogOfWarManager`'s real `_revealAll` static flag true,
  `spawn worker 3` added 3 real units to `Unit.All` (8→11), `age imperial`
  advanced `AgeProgress.CurrentAge(Player)` from Classical to Imperial,
  `win`/`lose` both correctly set `MatchManager.Outcome` via
  `ForceOutcome`, an unrecognized command returned "Unknown command: ...",
  and — the safety-critical case — forcing `NetworkMatch.IsActive` true
  via reflection correctly refused a `wood` grant with zero stockpile
  mutation and the disabled-during-LAN message, confirming the console
  really does refuse to run during a real LAN match. Screenshotted the
  real in-game console panel rendering cleanly against the HUD (resource
  bar, Idle Worker indicator, minimap frame, Town Bell button) with no
  overlap. Updated `docs/KingdomsOfBharat_Master_Reference.xlsx`'s
  "Implementation Waves 0-6" and "Dev Status Overview" sheets to reflect
  the closure — no code changes this pass beyond what the prior session
  already committed (the stale-DLL issue was an environment state problem,
  not a source bug). **This closes item 39 the same way every other Wave 6
  item has been closed.** Immediately followed by starting Wave 6 item 36
  (Score system) in the same session — see the entry below.
- **Wave 6 item 39 (Cheat codes) closed (2026-09-15) — not live-verified,
  see below.** Picked per the user's "take wave 6 next item" request;
  confirmed against the Master Reference workbook that items 35/37/38 all
  need a design decision first (Relics, Wonder/KotH, game modes), leaving
  36 (Score)/39/40 as the only decision-free Wave 6 items — user picked
  Cheat codes via AskUserQuestion. New `Core/CheatCommandParser.cs` (pure
  string→command parser, zero scene dependency) + `Core/CheatCodes.cs`
  (executes against real state, always targeting `FactionId.Player`):
  `resources <n>`, `wood/food/gold/stone <n>`, `age <name>`, `reveal`,
  `spawn <unitType> [count]`, `win`/`lose`, `help`. Two small additive
  public hooks added since neither existed: `FogOfWarManager.
  ToggleRevealAll()` (a static flag `Recompute()` checks before its normal
  vision-source logic — once all cells are forced Visible, the existing
  enemy-visibility code already shows everything correctly with no further
  change) and `MatchManager.ForceOutcome(MatchOutcome)` (wraps the
  existing private `Declare`). New `UI/CheatConsole.cs`: same
  self-bootstrapping runtime-Canvas + `GameSettings`-hotkey pattern as
  `SettingsMenu`/`HotkeyOverlay`, toggled by a new BackQuote hotkey
  (confirmed unused via grep), reuses `ScenarioEditorMenu.CreateInputField`'s
  shape for the `TMP_InputField`. Refuses to execute anything while
  `NetworkMatch.IsActive` — every cheat bypasses `CommandBus`, which would
  desync a real LAN match. 17 new EditMode tests
  (`CheatCommandParserTests.cs`/`CheatCodesTests.cs`) — Spawn execution
  deliberately not exercised in EditMode (this project's own documented
  `EntitySpawner`/factory hard-error-outside-Play-mode limitation), covered
  by parser tests only. **Not verified this session, flagged directly, not
  glossed over**: both `unity`/`UnityMCP` MCP servers were unreachable
  (confirmed via `ps aux` that a real Editor + its MCP bridge process were
  actually running against this exact project, and the bridge's HTTP port
  answered — the failure was this session's own MCP client, not a dead
  server) — so no EditMode test run and no live Play-mode verification
  happened, breaking the pattern every other closed Wave 6 item followed.
  Code was read back carefully for compile-correctness against the real
  source of every API it touches (`ResourceStockpile`/`AgeProgress`/
  `EntitySpawner`/`NetworkMatch`/`GameSettings`/`UIStyleTheme`/
  `TMP_InputField.onSubmit`, each confirmed by reading the actual file, not
  guessed) but this is not a substitute for running the suite. One scoped
  commit (`CheatCommandParser.cs`/`CheatCodes.cs`/`CheatConsole.cs` new,
  `FogOfWarManager.cs`/`MatchManager.cs`/`HotkeyOverlay.cs`/
  `SettingsMenu.cs`, 2 new test files) — deliberately excludes the
  unrelated concurrent-session work already sitting in the tree
  (`MarathaMavlaRaiderFactory.cs`, `CivilizationSetup.cs`,
  `TeamColorUnitTint.cs`, `corner_ornament.png`,
  `docs/PROJECT_TRACKER.html`, `.mcp.json`,
  `ProjectSettings/ProjectSettings.asset`), left untouched via targeted
  `git add`. Next: **run the EditMode suite and live-verify the cheat
  console once Unity/UnityMCP is reachable** before treating this as fully
  closed the way every other Wave 6 item was; otherwise Wave 6 item 40
  (Tutorial content) or item 36 (Score system), the Relics/Wonder/
  game-modes design decision, or unit-side team colour once Blender masks
  are sourced.
- **Wave 6 item 34 (idle-worker indicator) closed (2026-09-14)** — picked
  up per the user's "idle-worker indicator" request, right after the
  README refresh. Decision-free per the roadmap's own note ("a small UI
  addition near the minimap"). New `UI/IdleWorkerFinder.cs`: a pure,
  directly-testable `FindAll(FactionId)` that scans `Unit.All` for units
  with a `Gatherer` component (the same "Worker" marker `TownBell.Ring`
  already uses — exclusive to `WorkerFactory`'s spawn) owned by that
  faction and currently `UnitStatus.IsIdle` — a new bool helper added to
  `UnitStatus.cs` reusing its existing `Describe` priority chain rather
  than a second, potentially-drifting definition of "doing nothing" (a
  worker mid-walk to a resource node is correctly NOT idle, since
  `Gatherer.IsWorking` already covers the walk). A garrisoned worker is
  automatically excluded — `Unit.OnDisable` already removes it from
  `Unit.All` the instant `GarrisonPoint` deactivates it, no special-case
  needed.

  New `UI/IdleWorkerIndicator.cs`: built entirely in code (no
  `[SerializeField]`s) and self-attached via
  `MinimapController.gameObject.AddComponent<IdleWorkerIndicator>()` —
  same "avoid the recurring new-field-null-in-the-scene gotcha" convention
  the Age/research readout row already used. Shows a live "Idle Workers:
  N" panel with the `train_worker` icon, positioned directly above the
  minimap; click (or the new F6 hotkey, "SelectIdleWorker") selects the
  next idle worker and pans the camera to it, round-robin through the
  current idle set — repeated presses cycle through every idle worker in
  turn rather than always reselecting the first, same as AoE's own
  idle-villager button; restarts from the front of the list if the
  last-selected unit is no longer idle. Wired into
  `SettingsMenu.Actions`/`HotkeyOverlay`'s `GlobalGroup` (F6, next to
  TownBell's F8) — a local-only, un-networked convenience like Town Bell,
  no `CommandBus` needed since it only changes local selection/camera
  state, not game state.

  **Found and fixed a real bug live, not before it**: the first
  implementation attempted to position the new panel by reading
  `transform` on the same GameObject `IdleWorkerIndicator` self-attaches
  to (`MinimapController`'s own) — but
  `MinimapController.ApplyDiamondFrame()` (the 2026-09-12 ornate-HUD-
  reskin session) reparents and re-stretches that SAME RectTransform under
  a new "MinimapDiamondMask" GameObject as part of building the diamond
  mask, so by the time `AddComponent` ran, `transform` no longer described
  the minimap's real bottom-right position/size at all (read back live as
  `anchorMin=(0,0)/anchorMax=(1,1)/sizeDelta=(0,0)`, stretched to fill the
  mask instead) — the panel landed at `(0,0)` with zero width. Fixed by
  having `MinimapController.Awake()` snapshot its own anchor/position/size
  *before* calling `ApplyDiamondFrame()` and passing that snapshot into a
  new `IdleWorkerIndicator.Configure(...)` explicitly, rather than the
  indicator ever reading `transform` itself. Caught by comparing the
  live runtime `RectTransform` values against the exact numbers read
  directly from `Assets/Scenes/Main.unity`'s own YAML
  (`anchorMin/Max=(1,0)`, `anchoredPosition=(-10,10)`,
  `sizeDelta=(220,220)`) before assuming the fix was already right.

  6 new EditMode tests (`IdleWorkerFinderTests.cs`, mirroring
  `TownBellTests.cs`'s own Gatherer/FactionMember test-setup pattern —
  528 total, up from 522, all pass; the 2 pre-existing, unrelated
  `BuildingModelFactoryTests` failures are the same baseline as every
  recent session). Live-verified via UnityMCP through the real production
  path, twice (before and after the position fix): a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), pre-match overlays
  deactivated, the real panel screenshot-confirmed rendering cleanly
  directly above the minimap with no overlap ("Idle Workers: 4" for the 4
  real starting Workers); 5 real `Button.onClick.Invoke()` calls correctly
  cycled through all 4 distinct real Worker GameObjects and wrapped back
  to the first on the 5th; a real `Gatherer.GatherFrom()` call on one
  worker correctly dropped the live count from 4 to 3. One scoped commit
  (`UnitStatus.cs`, `MinimapController.cs`, `SettingsMenu.cs`,
  `HotkeyOverlay.cs`, the 2 new `IdleWorkerFinder.cs`/
  `IdleWorkerIndicator.cs` files, and `IdleWorkerFinderTests.cs`) —
  deliberately excludes the unrelated concurrent-session work already
  sitting in the tree (`MarathaMavlaRaiderFactory.cs`,
  `CivilizationSetup.cs`, `TeamColorUnitTint.cs`, `corner_ornament.png`,
  `docs/PROJECT_TRACKER.html`, `.mcp.json`,
  `ProjectSettings/ProjectSettings.asset`), left untouched via targeted
  `git add`. Next: another Wave 6 decision-free item (cheat codes,
  tutorial content), the Relics/Wonder/game-modes design decision, or
  unit-side team colour once Blender masks are sourced.
- **README.md refresh (2026-09-14)** — picked up per the reconciliation
  session's own "what's next" list (README drift, confirmed stale
  2026-09-03). The prior README described the original single-map/one-
  generic-civ/no-naval prototype and stopped its own milestone list at item
  26. Rewrote it from scratch, grounded in CLAUDE.md's current-status log
  and the Master Reference workbook's "Dev Status Overview" sheet: current
  scope (5 civs/4 Ages/~25 unit types with tier ladders/15 building types/
  naval/economy/meta systems/LAN multiplayer/UI), tech stack, a
  project-structure tree confirmed against the real `Assets/Scripts` folder
  list via `ls` (added `Multiplayer/`, `Match/`, `Progression/`, `Audio/`,
  `Data/`, which the old tree never listed), a "Running the game" section
  reflecting real current controls (civ picker, F1 hotkey overlay, F11
  diplomacy, scenario editor entry point) instead of the old capsule-worker/
  OnGUI walkthrough, a "Testing" section, and a pointer to the workbook/
  SESSION_LOG/CLAUDE.md as the source of truth rather than duplicating their
  content. Docs-only, no code/tests touched. One scoped commit (`README.md`,
  `CLAUDE.md`, `docs/SESSION_LOG.md`). Next: user's call — Wave 6's
  decision-free items, the Relics/Wonder/game-modes design decision, or
  unit-side team colour once Blender masks are sourced.
- **Master reference doc reconciliation (2026-09-14)** — session opened per
  protocol by reading the "Dev Status Overview" sheet; its own "next steps"
  pointer named 2 candidates that turned out already closed once checked
  directly against `Assets/Scripts` rather than trusted from the sheet: (1)
  SelectedUnitPanel portrait display wiring — already done, commit `42a5a2c`
  (same day, see the entry immediately below); confirmed live in the repo
  (`SelectedUnitPanel.cs`'s `SetUpPortrait`/`SetPortrait`, `TradeShipFactory.cs`
  stamping `IconKey = "train_tradeship"`), not just claimed. (2) The
  `UniqueTechDefinition.Bonuses` `KeyNotFoundException` for Maurya/Maratha
  (flagged 2026-09-04 as `task_55dbb0cc`) — already fixed, commit `6c3d21c`
  ("Fix KeyNotFoundException in UniqueTechDefinition for Maurya/Maratha"),
  an ancestor of HEAD with its own regression test
  (`UniqueTechDefinitionTests.For_DoesNotThrow_ForEveryCivilizationId`)
  already in the suite — but that fix was apparently never logged here or in
  the sheet, so both stayed reachable as "open" for at least 10 sessions'
  worth of narrative text. Also spot-checked the other per-civ lookup
  tables most likely to hide the same class of bug
  (`CivilizationProfile.Colors`/`CivIds`, `WorkerCombatResponseDefaults.
  Defaults`) — all 5 civs present in each, no further instances found.
  Corrected the "Dev Status Overview" sheet's "WHAT IS IN FLIGHT"/"KNOWN
  OPEN BUGS"/"WHAT'S NEXT" sections to drop both resolved items and point at
  what's genuinely still open (Wave 6's decision-free items, the Relics/
  Wonder/game-modes design decision, the stale README, unit-side team
  colour). Docs-only session, no code changes, no tests affected. **Lesson
  for future sessions, worth repeating**: a status doc's "still open" claim
  is not evidence on its own — grep/read the actual file before starting
  work a doc says is needed, the same standing instruction this project's
  own "Single-session discipline" gotcha already gives for peer-relayed
  claims. Next: user's call among the items listed above.
- **UI_ART_BRIEF.md Tier 4: SelectedUnitPanel portrait display wiring closed
  (2026-09-14)** — closes the display-wiring gap the 2026-09-13 staging
  session left pending (Unity/UnityMCP was unreachable for two sessions in
  a row; reachable this session). Root cause found before writing any
  placement code: all 21 staged portrait PNGs had `textureType: 0`
  (Default Texture2D) in their `.meta`s, not Sprite — `Resources.
  Load<Sprite>` returned null for every one regardless of any placement
  code, since the staging pass alpha-keyed/cropped the pixels but never
  set the Unity import type. Fixed for all 21 via `TextureImporter.
  textureType = Sprite`. The portrait notch's real position was measured
  directly off `panel_selected_unit.png` (1729x806) by scanning alpha
  transitions, not estimated — notch at x=[45,321)/y=[213,506), bottom-
  left origin (matches Unity's anchor convention with no axis flip
  needed) — expressed as `anchorMin`/`anchorMax` fractions
  (`(0.036,0.2743)`-`(0.1757,0.6178)`) so the portrait tracks correctly
  even though the panel's own height is dynamic (synced to `BuildMenu`'s,
  per the 2026-09-12 fix). New `SelectedUnitPanel.SetUpPortrait()`/
  `SetPortrait(iconKey)`: a plain `Image` child of `Panel` (renders over
  the transparent notch since it's a later sibling in the same
  GameObject), wired from `DrawSingle`'s `unit.IconKey`, explicitly
  cleared in `DrawBuilding` and the group-selection branch (buildings have
  no `IconKey` yet; a stale portrait behind "N units selected" would
  repeat the exact stale-HP-bar bug class from 2026-09-12). Also stamped
  `TradeShipFactory.cs`'s spawned unit with `IconKey = "train_tradeship"`
  — the portrait file already existed from the Tier 4 delivery but nothing
  had ever set the key. 522/522 EditMode tests pass unmodified. Live-
  verified via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), pre-match overlays
  deactivated, a real spawned "Maurya Worker" selected — the portrait
  renders cleanly inside the ring with no overflow, screenshot-confirmed;
  selecting a real TownCenter confirmed the portrait correctly clears (no
  stale leftover). **Not done**: only Worker's own composition was
  individually re-screenshotted against the live notch (the other 20 share
  the same staged pipeline, already visually verified by composition type
  in the prior session, but not re-checked one-by-one here); building
  portraits and the 10 civ-exclusive unique-unit portraits remain
  unsourced; `TradeShipFactory`'s 3D model still has no dedicated art
  (unrelated to this pass). One scoped commit
  (`SelectedUnitPanel.cs`/`TradeShipFactory.cs`/21 `.meta` files/docs) —
  deliberately excludes the unrelated concurrent-session work already
  sitting in the tree (`MarathaMavlaRaiderFactory.cs`, `CivilizationSetup.
  cs`, `TeamColorUnitTint.cs`, `corner_ornament.png`, `docs/
  PROJECT_TRACKER.html`, `.mcp.json`, `ProjectSettings/ProjectSettings.
  asset`). Next: whatever the user directs.
- **UI_ART_BRIEF.md Tier 4: 21 generic-unit portraits identified/alpha-keyed/
  cropped/staged, display wiring pending (2026-09-14)** — user delivered a
  framed-card portrait set for all 21 generic units at `~/Downloads/generic
  unit icon/`. The delivery's numbering only matched the prior session's
  prompt-list order through image 12 — 13-21 were scrambled (e.g. `20.png`
  was the Camel Rider, not the Maharaja) — identified all 21 individually by
  content rather than trusted by number. All 21 were RGB with a baked
  checkerboard background (same defect as every prior UI art delivery); the
  art itself is a full framed card (arch + lotus corners), not the plain bust
  the brief's template asked for, which would double-frame inside
  `SelectedUnitPanel`'s existing circular notch — user confirmed (via
  AskUserQuestion) center-cropping to a plain circle over enlarging the
  display area. Alpha-keyed with the existing `Tools/ui_art_alpha_key.py`
  plus a new center-crop-to-circle step, verified by eye across every
  composition type (foot/mounted/vehicle) before batching all 21. Committed
  unwired at `Assets/Resources/UI/Portraits/train_<IconKey>.png`, named to
  match each unit's existing `Unit.IconKey` exactly. **Unity/UnityMCP was
  unreachable all session** (same as the prior Tier 4 session) — asked the
  user whether to wire the actual display code (a new portrait `Image` in
  `SelectedUnitPanel`'s notch) on best-effort measurements anyway or hold for
  a live pixel check; user chose to hold the wiring, but confirmed staging
  the self-verifiable asset files (no code/scene touched) was fine now.
  `TradeShipFactory` still stamps no `IconKey` at all (no art existed for it
  before this delivery) and needs one added as part of that same future
  wiring pass. **Also found and flagged, not caused by this session**: mid-
  session, `git log` showed the branch jump from 53 to 61 commits ahead of
  origin — a second, independent session was actively committing to this
  same branch concurrently (Town Bell, Gold Mine models, Vaidya/Purohita
  model wiring, an Age-up HUD meter), confirmed by the previously-flagged
  stale `Assets/Resources/UniqueUnits/Purohita/` delivery turning into a real
  compiled prefab+avatar mid-session, and a `.meta` file appearing under the
  new `Portraits/` folder that this session never created (a live Unity
  Editor belonging to that other session was watching this exact directory).
  Flagged directly to the user; confirmed proceeding carefully (new files
  only, re-check git status before every commit). Building portraits and the
  10 civ-exclusive unique-unit portraits remain fully unsourced. Two commits
  (`1a15b2d` + this doc update). Next: the `SelectedUnitPanel.cs` display
  wiring once Unity/UnityMCP is reachable, or whatever else the user directs.
- **Vanik's real pack-ox model wired (2026-09-14)** — ad hoc, user-supplied
  3D model (`Meshy_AI_Character_output.glb`, "the trader animal character
  rig model... an ox back supply model"), not a numbered roadmap item.
  Closes the flag-asset-needs gap VanikFactory's own header comment carried
  since Wave 4 item 26 ("no dedicated model exists yet... a Vanik currently
  looks like a generic soldier, not a merchant"). The model shipped with a
  rigged Generic (non-humanoid) quadruped skeleton — a Meshy "UniRig"
  auto-rig with 47 generically-named bones (`Bone_000`..`Bone_046`) — but no
  animation at all. Per the user's own instruction ("use cow walking
  animation for this model"), retargeted the existing Shepherd Valley cow
  pack's `A_Cow_Walk_01` clip onto this rig entirely in Blender (driven
  headlessly via `/Applications/Blender.app/Contents/MacOS/Blender
  --background`, no Blender MCP session was open): inspected both
  skeletons' bone world positions/hierarchy directly (no bone-name overlap
  at all between the two independently-authored rigs) to identify the 4 leg
  chains and tail by geometry alone, then mapped Ox chain bones to Cow chain
  bones by proportional position-along-chain (chain lengths differ — Ox legs
  are 5 bones, Cow's are 3-4) and transferred each mapped pair's rotation as
  a **world-space delta from bind pose** (`sourceRestWorld⁻¹ · sourceAnimWorld`
  reapplied onto `targetRestWorld`) rather than a naive local-quaternion
  copy — robust to the two rigs' unrelated bone roll/axis conventions, since
  the delta is computed and reapplied in a shared world frame. Deliberately
  left the spine/head/ears/horn unretargeted (kept at rest) — no clean
  correspondence exists for those on the Ox side (a Zebu hump and what
  looks like the cargo pack's own dangling-strap bones, neither of which
  the Cow rig has an equivalent for), and guessing risked a visibly twisted
  neck; only the 4 legs and tail actually animate, which is enough to read
  clearly as walking. Verified the retarget's own output by rendering
  preview frames of the walk cycle in Blender before touching Unity at all
  (a real correctness check, not just "the script ran") — confirmed a
  plausible alternating 4-beat gait with proper forward/back leg swing.
  Exported baked Walk + a synthesized single-frame Idle (rest pose) as FBX,
  then in Unity **rewrote each clip's curve paths** (`AnimationUtility.
  GetCurveBindings`/`SetEditorCurve` into new standalone `.anim` assets) to
  strip the FBX exporter's extra `UniRigArmature/` path segment — the
  displayed prefab (imported straight from the original `.glb` via glTFast,
  for correct embedded textures/materials) has `Bone_000` etc. as *direct*
  children with no such intermediate node, so the raw FBX-baked clips would
  have silently failed to bind at all. Confirmed 47/47 bone paths resolve
  against the real display prefab before considering this done. New
  `Units/OxModelFactory.cs` (root/child split mirroring `HumanModelFactory`,
  but a `BoxCollider` fit to rendered bounds instead of a `CapsuleCollider`
  — a quadruped's footprint is wider than it is tall, `AnimalModelFactory`'s
  own convention, not a biped's), `Units/OxAnimationSet.cs`/
  `OxAnimationDriver.cs` (mirror `CowAnimationSet`/`CowAnimationDriver`'s
  shape, but keyed purely on `NavMeshAgent` velocity — no `Livestock`-style
  "being milked" state, since a Trader has nothing analogous). Built the
  prefab's `Animator`+Generic `Avatar` by hand
  (`AvatarBuilder.BuildGenericAvatar`) since glTFast doesn't auto-attach
  either for a mesh with no embedded animation (unlike Unity's own FBX
  importer, which does this automatically — confirmed by checking Cow's own
  SK_Cow.fbx import for comparison). Scaled the prefab to 2.7 world units
  tall (measured against the established ~1.9 worker-height convention —
  bigger than a human, proportionate for a laden draft animal, not
  guessed). `VanikFactory.cs` now spawns via `OxModelFactory`/
  `OxAnimationDriver` instead of `HumanModelFactory`/`AnimationDriver`+
  `HumanAnimationSet`; `NavMeshAgent` radius/height retuned for a
  quadruped's footprint (0.6/1.6, was 0.4/2 for the biped body). 520/522
  EditMode tests pass (2 pre-existing, unrelated `BuildingModelFactoryTests`
  failures, same baseline as every recent session). Live-verified via
  UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real `VanikFactory.Spawn()`
  produced a "Maurya Vanik" with a valid `Animator`/`Avatar` and an
  `OxAnimationDriver`; forced the driver's own Walk clip onto its live
  `PlayableGraph` and evaluated it at t=0/0.5/1.0 on the real spawned
  rig — a rear-leg bone's local rotation swung ~9° at the midpoint and
  returned to within a fraction of a degree of its start value after one
  full loop, confirming the retargeted clip genuinely drives the bone
  hierarchy end-to-end on the real spawn path (not just in the isolated
  Blender preview) and loops cleanly. A full visual screenshot of the
  spawned unit in Play mode was attempted but inconclusive this session —
  fog-of-war/menu-overlay/camera-framing round trips kept colliding with an
  unrelated Editor instability that repeatedly, silently exited Play mode
  between MCP calls (not caused by this work); the functional bone-rotation
  proof above was used instead since it's a stronger, more direct check of
  the actual deliverable (the retargeted animation driving the real rig)
  and isn't sensitive to that instability. **Found and fixed one real
  bug while verifying, not shipped blind**: the corrected `.anim` assets
  initially shared a base filename with their now-redundant source FBX
  files (`A_Ox_Walk_01.fbx` + `.anim`, `A_Ox_Idle_01.fbx` + `.anim`) —
  `Resources.Load<AnimationClip>` resolved Idle to the FBX's own
  wrongly-pathed embedded clip instead of the corrected `.anim` (Walk
  happened to resolve correctly, by luck of load order) — fixed by
  deleting the now-purely-intermediate FBX files from `Resources` entirely,
  leaving only the corrected standalone `.anim` assets, and reconfirmed
  both `OxAnimationSet.Load()` slots resolve to the right asset by name.
  **Deliberately out of scope**: no team-color pennant/tint for this unit
  (out of scope for a model-wiring pass, same as Purohita's own session),
  spine/head/ear/horn/pack-strap motion during the walk cycle (flagged
  above, would need a real correspondence or hand-authored motion, not a
  retarget). One scoped commit (`VanikFactory.cs`, the 3 new `Units/Ox*.cs`
  files, the new `Vanik.glb`/`.prefab`/`_Avatar.asset` under
  `Resources/UniqueUnits/Vanik/`, the 2 corrected `.anim` assets under
  `Resources/AnimalAnimations/Ox/`, and the raw source glb under
  `Assets/importedmodels/Vanik/`) — deliberately excludes unrelated
  uncommitted work already sitting in the tree from a concurrent session
  (`MarathaMavlaRaiderFactory.cs`/`CivilizationSetup.cs` modifications, a
  `TeamColorUnitTint.cs` pilot, `corner_ornament.png`,
  `docs/PROJECT_TRACKER.html`, `.mcp.json`), left untouched via targeted
  `git add`. Next: whatever the user directs.
- **Wave 6 item 33 (Town Bell) closed (2026-09-14) — first Wave 6 item.**
  Picked up per the user's "start wave 6 items" request; Wave 6 is an
  explicitly parallel-safe backlog with no fixed order, so asked which
  no-design-decision item to start with (33/34/39/40) — user picked Town
  Bell. New `Buildings/TownBell.cs`: a static `Ring(FactionId)` that finds
  every TownCenter and every Worker (identified by `Gatherer` presence, the
  one component exclusive to `WorkerFactory`) owned by that faction, then
  orders each worker to the nearest TownCenter with room via the existing
  `GarrisonSeeker.GarrisonAt` — the same call a manual right-click garrison
  order already makes — cancelling every other in-progress worker task
  first (gather/build/repair/farm/livestock/attack), matching
  `SelectionManager`'s own cancel list. Wired as a new, genuinely global
  `BuildMenu.townBellButton` — deliberately outside `_allGridButtons`/
  `LayoutCommandGrid` since every other grid button is gated on a specific
  selected building and this one isn't — plus a new F8 hotkey. **Found and
  fixed a real, previously-latent bug**: `GarrisonSeeker` cached its
  `UnitMover` sibling eagerly in `Awake()` instead of lazily, unlike every
  sibling class in this exact situation (`Gatherer.Mover`/`GarrisonPoint.
  Attackable`/`Repairable`/`MeleeAttacker.Self`) — no prior test had ever
  exercised `GarrisonAt` directly, so this NRE-in-EditMode gap went
  unnoticed since the General Garrisoning system shipped (2026-09-01); fixed
  by converting `_mover` to the same lazy-property pattern as its siblings.
  7 new EditMode tests (`TownBellTests.cs`), 522/522 total pass (2
  pre-existing, unrelated `BuildingModelFactoryTests` failures, same
  baseline as every recent session). Live-verified via UnityMCP through the
  real production path: a real match, 5 real spawned Player Workers, a real
  TownCenter (`GarrisonPoint` capacity 8) — the real `TownBellButton`'s own
  `onClick.Invoke()` sent all 5 workers walking via their own real
  `NavMeshAgent` to garrison (`GarrisonPoint.Count` 0→5, each deactivated
  exactly like a manual order), then a real `UngarrisonAll()` correctly
  ejected all 5 — full round trip confirmed. Hit a real environment trap:
  exiting Play Mode doesn't itself trigger a C# domain reload, so static
  state from the Play session (age/civ assignments) briefly bled into the
  next EditMode run, producing a large cascade of unrelated-looking
  failures — resolved via `EditorUtility.RequestScriptReload()` before the
  final run; flagging this for any future session that live-verifies via
  Play Mode right before a definitive EditMode check. Also found (and left
  entirely untouched, excluded via targeted `git add`) substantial unrelated
  uncommitted work already sitting in the tree from a concurrent session —
  `MarathaMavlaRaiderFactory.cs`/`CivilizationSetup.cs`/`VanikFactory.cs`
  modified, new `TeamColorUnitTint.cs`, new Ox animation files, a new Vanik
  model delivery, `corner_ornament.png`, `docs/PROJECT_TRACKER.html`,
  `.mcp.json`. One scoped commit (`TownBell.cs`, `GarrisonSeeker.cs`,
  `BuildMenu.cs`, `HotkeyOverlay.cs`, `SettingsMenu.cs`, `TownBellTests.cs`,
  `Main.unity`, docs). Next: another Wave 6 item (idle-worker indicator,
  cheat codes, tutorial content are also design-decision-free; Relics/
  victory conditions/game modes need a design decision first), user's call.
- **Vaidya rigged model wired, and a real latent scale bug in
  `HumanoidGltfRigImporter` found and fixed for both Vaidya and Purohita
  (2026-09-14)** — ad hoc, user-supplied 3D rig ("Meshy AI Wandering Sage
  biped", 2 `.glb` files: Walking/Running variants of the same mesh), not a
  numbered roadmap item. Vaidya (Wave 4 item 27) previously reused the
  generic shared Human Character Dummy body; this replaces it with a real
  rigged model, the same day's Purohita session's own visual-closure pattern
  applied to its sibling unit. The new GLB's skeleton uses the same
  "literal `HumanBodyBones` names" convention as Purohita's own delivery
  (confirmed by reading the raw glTF node names directly before importing,
  not assumed from the filename) — `HumanoidGltfRigImporter.DirectHumanBoneMap`
  applied unchanged, no importer changes needed for the bone map itself.
  Built the Avatar + saved prefab at
  `Assets/Resources/UniqueUnits/Vaidya/Vaidya.prefab` at the established
  worker-height convention (1.902692); `VaidyaFactory.cs` now spawns via
  `prefabPathOverride`, keeping the rig's own embedded material as-is (no
  `ApplyCustomTexture`, no team-color mask — none authored for this mesh's
  UV layout yet, flagged directly, same gap as Purohita's). The archived
  "Running" GLB's baked clip wasn't extracted/wired, matching Purohita's own
  precedent (full model replacement, not animation extraction).
  **Found and fixed a real, previously-unnoticed bug live via UnityMCP, not
  assumed away**: `HumanoidGltfRigImporter.BuildAndSavePrefab` baked its
  height-scale correction onto the same root Transform that carries the
  Animator — but `HumanModelFactory.Spawn` unconditionally resets that same
  top-level instantiated object's `localScale` to `Vector3.one` at runtime
  (a guard against the shared dummy body's own scale getting corrupted, per
  its own comment) — silently discarding the correction for ANY
  `prefabPathOverride` caller. This affected Vaidya visibly (0.86 correction
  → spawned at 2.22 world units instead of the intended 1.9) and had
  already shipped silently in Purohita too (0.93 correction, close enough to
  1 that the ~7% oversize went unnoticed in that session's own live
  verification). Root-caused by comparing the prefab asset's own
  `Animator.avatar` (valid) against the same object once spawned at runtime
  (avatar read back `null` at first — a related but separate discovery: the
  raw source `.glb` and the built `.prefab` shared the exact same
  `Resources.Load` key (`UniqueUnits/Vaidya/Vaidya`), an ambiguous-resource
  collision that resolved to the wrong asset for Vaidya specifically — the
  same class of bug the Gold Mine session already flagged and fixed for
  `EnvironmentPropFactory`'s `Resources.LoadAll`. Fixed by moving both raw
  source `.glb`s (Vaidya's and, while already touching this, Purohita's
  too) out of the `Resources` tree entirely, to
  `Assets/importedmodels/{Unit}/{Unit}_Source.glb`, `AssetDatabase.MoveAsset`-
  preserving GUIDs so the built prefabs' mesh/material references stayed
  intact — confirmed live post-move). Fixed the scale bug itself by having
  `BuildAndSavePrefab` apply the correction to the instantiated root's
  direct children instead of the root's own transform (composes correctly
  with whatever baked scale a child already carries, e.g. this rig's own
  `target_character` node at 0.01 from the source armature) — the root's
  own scale now stays 1, matching what `HumanModelFactory.Spawn` already
  assumes/enforces. Rebuilt both Vaidya's and Purohita's prefabs with the
  fixed tool (via a scratch `[MenuItem]` Editor script, deleted before
  committing — `execute_code`'s CodeDom compiler can't reliably round-trip
  `ValueTuple[]` arguments across its dynamic-compilation boundary, so the
  existing `HumanoidGltfRigImporter.BuildAndSavePrefab`/`DirectHumanBoneMap`
  couldn't be invoked directly that way). 513/515 EditMode tests pass (the
  same 2 pre-existing, unrelated `BuildingModelFactoryTests` failures as
  every recent session, confirmed unchanged before and after this session's
  Play mode run). Live-verified via UnityMCP through the real production
  path: a real match, real "Chola Vaidya" and "Chola Purohita" spawns both
  now measure exactly 1.902692 world units tall (was 2.22/2.05 respectively
  before the fix) with `Avatar.isHuman=true` and a valid avatar resolved at
  runtime (not just on the asset), a real NavMeshAgent walk order completed
  end to end (`PathComplete`, position updated), and screenshots confirm
  both stand in a natural pose (not T-posed) and read as visually distinct
  from each other (Vaidya: cream/tan robe, visible beard/hood; Purohita:
  reddish robe) and from the shared dummy body. **Deliberately out of
  scope**: no team-color mask, no hand-held prop (this rig has none), the
  archived Running clip. One scoped commit (`HumanoidGltfRigImporter.cs`,
  `VaidyaFactory.cs`, the new `Vaidya.prefab`/`Vaidya_Avatar.asset`, the
  rebuilt `Purohita.prefab`, and both units' archived raw source `.glb`s
  moved under `Assets/importedmodels/`). Next: whatever the user directs.
- **Wave 5 item 32 (Age/research always-visible readout) closed (2026-09-14) —
  closes Wave 5.** Picked up per the user's "start Age/research always-visible
  readout" request, with scope already narrowed in their own prompt:
  Civilization/Population/Age labels were already always-on (folded into
  `ResourceHUD` during the 2026-09-12 HUD pass) — only the research-in-progress
  meter itself was missing. **Scoped deliberately to Age-up only**, not every
  concurrent research track this project supports (Karmashala Attack/Armor,
  every Barracks/Dock tier line, Durg's Elephant/Elite tiers, TownCenter's
  Economy Tech) — matches real AoE II's own top-center "torch" readout (Age-up
  specific, not a general tech-progress display) and keeps this a genuinely
  small item as labeled; every other research track stays visible only on its
  own building's selection UI, unchanged. New
  `TownCenter.FindAgingUp(FactionId)` scans `Building.All` for a TownCenter
  owned by that faction currently aging up (mirrors `AgeUpRequirement.IsMet`'s
  own registry-scan convention), plus a new public `AgeUpTarget` getter
  (previously private). `ResourceHUD.cs` gained a second row, built entirely
  in code in `Awake()` (no scene wiring, avoiding the recurring "new
  `[SerializeField]` null in the scene" gotcha) — a label ("Researching:
  {Age} ({percent}%)") plus a thin fill bar reusing `SelectedUnitPanel`'s own
  `hp_bar_frame`/`hp_bar_fill` art (no new art needed) — hidden by default,
  shown and the panel resized to fit only while a TownCenter is actually
  aging up, snapping back down the instant it completes. 4 new EditMode tests
  (`AgeResearchReadoutTests.cs`, mirroring `AgeUpRequirementTests.cs`'s own
  pattern), 515/515 total pass (2 pre-existing, unrelated
  `BuildingModelFactoryTests` failures, same baseline as every recent
  session). Live-verified via UnityMCP through the real production path: a
  real match, pre-match overlays deactivated to see the live HUD, granted
  real Wood/Stone (both started at 0 - the first `RequestAgeUp()` attempt
  correctly no-op'd until funded), a real `RequestAgeUp()` started a real
  countdown, screenshotted mid-research ("Researching: Classical Age (41%)"
  with a visibly filling bar, panel correctly grown), forced the countdown to
  completion via reflection and re-screenshotted - `IsAgingUp` flipped false,
  `AgeProgress.CurrentAge` advanced to Classical, the research row and the
  panel's extra height both cleanly disappeared. **This closes item 32, the
  last open item in Wave 5** — Wave 5's own exit criteria (every unit/building
  has a working team-colour slot; the HUD reads as one coherent AoE-style
  bottom-bar layout) are both now met. Next: Wave 6 (Economy/meta backlog —
  Town Bell, idle-worker indicator, Relics, Score system, victory conditions
  beyond Conquest, game modes, cheat codes, tutorial content), or any other
  item, user's call.
- **Purohita rigged model wired (2026-09-14)** — ad hoc, user-supplied 3D rig
  ("Meshy AI Sacred Pilgrim biped", 2 `.glb` files: Walking/Running variants of the
  same mesh), not a numbered roadmap item. Purohita (Wave 4 item 27) previously
  reused the generic shared Human Character Dummy body; this replaces it with a
  real rigged model. The repo already had a *different*, unrelated Purohita
  delivery sitting uncommitted (`Purohita.fbx` + albedo + team mask, a 61-bone
  "B-" Rigify rig with finger bones and staff/bell/vessel props) — its Humanoid
  Avatar was fixed first (Unity's auto bone-mapper mis-mapped UpperLeg/LowerLeg/
  Foot for that rig), then the user was asked directly which delivery to use;
  they picked the new GLB, so that FBX work was set aside (moved, not deleted, to
  `Assets/importedmodels/Purohita_OldDelivery/`). The new GLB's skeleton uses bone
  names that are literally Unity's own `HumanBodyBones` names (a different, simpler
  convention than the Mixamo-style naming `Assets/Editor/HumanoidGltfRigImporter.cs`
  already had hardcoded from the female/male Worker glTF swap) — extended that
  shared tool with a second bone map (`DirectHumanBoneMap`) via a new optional
  parameter, the one existing call shape unaffected. Built the Avatar + saved
  prefab at `Assets/Resources/UniqueUnits/Purohita/Purohita.glb`/`.prefab` at the
  established worker-height convention (1.902692); `PurohitaFactory.cs` now spawns
  via `prefabPathOverride`, keeping the rig's own embedded material as-is (no
  `ApplyCustomTexture`, no team-color mask — none authored for this mesh's UV
  layout yet, flagged directly). 509/511 EditMode tests pass (2 pre-existing,
  unrelated `BuildingModelFactoryTests` failures, same baseline as every recent
  session). Live-verified via UnityMCP through the real production path: a real
  match (`CivilizationSetup.BeginMatch(Maurya)`), a real spawned "Maurya Purohita"
  with every expected component and a confirmed `Avatar.isHuman`, screenshotted
  standing in a natural idle pose and again mid-walk-order showing a genuine
  retargeted walking stride (not T-posed, correctly grounded either way).
  **Deliberately out of scope**: no team-color mask, no hand-held prop (this rig
  has none, unlike the discarded delivery), the archived "Running" GLB's baked
  clip wasn't extracted/wired — the user picked full model replacement, not
  animation extraction. One scoped commit (`HumanoidGltfRigImporter.cs`,
  `PurohitaFactory.cs`, the new `Purohita.glb`/`.prefab`/`_Avatar.asset`, and the
  archived old FBX delivery + Running-glb reference under `Assets/importedmodels/`).
  Next: whatever the user directs.
- **Gold Mine art delivery wired (2026-09-14)** — ad hoc, user-supplied 3D model
  (`Gold mine 1.glb`), not a numbered roadmap item. Replaced the old
  `GoldOre1.prefab` placeholder with two new variants under
  `Assets/Resources/Environment/GoldMine/`: `GoldMineSmall.prefab` (one unit) and
  `GoldMineLarge.prefab` (a 3x3, 9-unit organic cluster), per the user's own
  sizing instruction. Both spawn through `EnvironmentPropFactory.TrySpawn`'s
  existing random-variant mechanism with zero code changes — the multi-renderer
  aggregate-bounds ground-alignment/collider-sizing logic already generalized to a
  9-piece cluster correctly. Found and fixed a real gotcha: `EnvironmentPropFactory`
  uses `Resources.LoadAll` (recursive), so staging the raw source `.glb` under a
  `_Source` subfolder inside the category folder — the convention
  `MeshyBuildingImporter` safely uses for buildings, which read by exact path —
  made it spuriously appear as a 3rd variant; moved the raw source to
  `Assets/importedmodels/GoldMine/` (outside any Resources tree) via
  `AssetDatabase.MoveAsset`, confirmed GUID-preserving so both prefabs' nested
  references stayed intact. 509/511 EditMode tests pass (2 pre-existing, unrelated
  `BuildingModelFactoryTests` failures, same as every recent session). Live-
  verified via UnityMCP through the real production path:
  `EnvironmentPropFactory.TrySpawn`/`ResourceNodeSpawner.SpawnGoldMine` both called
  directly, confirming correct collider sizing, renderer counts, and
  `ResourceNode(Gold)` attachment for both variants. **Deliberately out of scope**:
  gold *quantity* per deposit is untouched (still map-wide `startingAmount`) — this
  was read as a visual-sizing request only, not a request to also scale Gold yield
  by deposit size; flagged rather than assumed. No commit made (not asked to
  commit this session; repo already has unrelated pre-existing uncommitted work in
  the tree per the Tier 4 session note below). Next: whatever the user directs.
- **UI_ART_BRIEF.md Tier 4 reconciled (2026-09-13)** — picked up per the user's "start
  tier 4" request. Checked the repo's actual state rather than trusting the doc's own
  checklist: the Minimap frame item was already fully shipped (`panel_minimap_frame.png`/
  `panel_minimap_frame_border.png`, wired via `MinimapController.ApplyDiamondFrame()`)
  back in the 2026-09-12 Ornate HUD Reskin session (commit `db9b3b4`) — that work
  predates the Tier 4 write-up as its own checklist, so the box just never got checked.
  Fixed the doc rather than re-doing the work. The only item left open in the entire
  `UI_ART_BRIEF.md` doc is unit/building portraits — explicitly optional, and blocked on
  new art (Canva/commissioned) that doesn't exist yet; nothing to wire until it's
  sourced. **Also found, flagged, and left untouched**: unrelated uncommitted work
  already sitting in the working tree at session start — a new
  `Assets/Resources/UniqueUnits/Purohita/` model delivery (fbx/albedo/teammask), a new
  `Core/TeamColorUnitTint.cs` (unit-side team-color tint pilot, wired into
  `CivilizationSetup.cs` and `MarathaMavlaRaiderFactory.cs`, the latter pointing at a
  `MavlaRaider_teammask` resource that doesn't exist on disk yet), and an untracked
  `corner_ornament.png` — none of this documented anywhere in this file's own status.
  Asked the user directly rather than acting on it; confirmed it's their own stale/
  in-progress work, safe to leave as-is. Docs-only commit (`docs/UI_ART_BRIEF.md`,
  `docs/SESSION_LOG.md`, this file). Next: portrait art if/when sourced, the
  still-uncommitted Purohita/team-color-unit-tint work whenever the user wants to pick
  it back up, or any other roadmap item.
- **UI_ART_BRIEF.md Tier 3 art delivery wired and its real rendering bug fixed
  (2026-09-13)** — picked up mid-task from a prior session's own carryover note
  (9 Tier 3 files already alpha-keyed/wired into `Assets/Resources/UI/Menu/` with
  corrected 9-slice border values, but never live-verified — both `unity`/
  `UnityMCP` were unreachable that session). This session verified live via
  UnityMCP and found the prior session's border-value fix, while correct, wasn't
  sufficient on its own: `SettingsMenu`'s modal frame rendered as a warped,
  crowded mess with no flat interior — the temple/tower ornament appeared to
  fill almost the entire panel regardless of `pixelsPerUnitMultiplier`. Root-
  caused (not guessed) by directly inspecting the generated `CanvasRenderer`
  mesh via reflection (`img.canvasRenderer.GetMesh()`) and the texture importer
  settings: all 4 new Tier 3 sprites (`modal_frame`/`menu_button_normal`/
  `menu_button_hover`/`menu_button_pressed`) imported with **Sprite Mesh Type =
  Tight** (Unity's alpha-hugging mesh), which breaks `Image.Type.Sliced`'s
  9-slice math outright — confirmed by switching to `Image.Type.Simple` (a
  clean full-texture stretch, no 9-slice math involved) and seeing the frame
  render exactly as intended. Fixed by setting **Mesh Type = Full Rect** on all
  4 via `TextureImporterSettings`/`SaveAndReimport`. **Found a second, real bug
  while re-verifying post-fix**: `UIStyleTheme.ApplyPanel` (the shared helper
  every code-built menu panel routes through) never set
  `Image.pixelsPerUnitMultiplier` at all — stayed at Unity's default 1, which
  renders a 9-slice border at a literal 1:1 texture-pixel-to-UI-unit size,
  correct only when a panel happens to display at the frame texture's own
  native 1264×1237 size. Every real panel is a different, smaller size, so the
  border rendered wildly disproportionate. Fixed centrally, not per-panel: new
  `UIStyleTheme.FitBorderToRect` computes `pixelsPerUnitMultiplier` from the
  panel's own live RectTransform height ÷ the frame texture's native height,
  called automatically from `ApplyPanel` — this fixes all 5 real call sites at
  once (`SettingsMenu`/`HotkeyOverlay`/`DiplomacyMenu`/`ObjectivePanel`/
  `ScenarioEditorMenu`), including the last one, which this session never even
  got to screenshot directly. Required reordering each call site so the Image's
  RectTransform is sized (anchors + `sizeDelta`) *before* `ApplyPanel` runs, not
  after (the order all 5 had it in, since `ApplyPanel`'s border-fitting has to
  read a real size to compute against) — `ObjectivePanel.cs` already carried an
  unrelated, pre-existing uncommitted anchor-repositioning edit from a
  concurrent session when this session started; reordered around it rather than
  reverting it, and it rides along in this session's commit since the two
  edits share the same few lines and can't be cleanly split. Also hit and
  worked through a real environment trap mid-session, unrelated to the actual
  bug: a `manage_editor(action="pause")` call left Play mode stuck
  (`is_paused=true, is_changing=true`) for a long stretch, during which
  *nothing* newly-activated rendered in screenshots — including a from-scratch
  test `Canvas`, which is what proved it was a frozen-game artifact and not a
  code or screenshot-tooling problem. Calling `pause` again (it toggles, not
  sets) unstuck it and every panel rendered correctly immediately after. 509/511
  EditMode tests pass (the 2 failures are pre-existing and unrelated —
  `BuildingModelFactoryTests` for Chola's TownCenter model, broken by the
  already-committed `a5d2b15` "Remove leftover raw _Source FBX exports" cleanup
  from before this session started, not by anything touched here). Live-
  verified via UnityMCP through the real code path (not just reflection
  overrides): `SettingsMenu`, `HotkeyOverlay`, and `DiplomacyMenu` all render
  cleanly post-fix — a properly framed panel with a genuine flat interior, no
  ornament bleeding into text, at their 3 quite different sizes (560×700,
  900×620, 800×260) — confirming `FitBorderToRect`'s formula generalizes rather
  than needing per-panel tuning. `ObjectivePanel`/`ScenarioEditorMenu` weren't
  directly screenshotted (the former only shows with a real active scenario
  objective, which a skirmish match doesn't have; the latter wasn't reachable
  in this session's match flow) but get the identical fix through the same
  shared `ApplyPanel` code path. `CivPicker`'s 5 crests confirmed rendering
  correctly with `preserveAspect` (the prior session's fix, unaffected by this
  one). `MissionSelectMenu`'s buttons confirmed clean both before and after the
  mesh-type fix (their alpha shape happened to be forgiving enough not to show
  the bug visibly, but they got the same underlying fix for correctness).
  One commit covering `Assets/Resources/UI/Menu/*.png`+`.meta` (4 files),
  `UIStyleTheme.cs`, `SettingsMenu.cs`, `HotkeyOverlay.cs`, `DiplomacyMenu.cs`,
  `ObjectivePanel.cs` (incl. the unrelated riding-along anchor fix),
  `ScenarioEditorMenu.cs`, `CivPicker.cs`, and `docs/UI_ART_BRIEF.md`'s Tier 3
  checklist. **This closes Tier 3** — Tier 4 (minimap frame, optional unit/
  building portraits) is the only remaining tier in that doc. Next: Tier 4, or
  whatever the user directs.
- **New feature: SelectedUnitPanel group-selection icon row (2026-09-13)**
  — not a bug fix, a real feature the user asked for: when a group of units
  is selected, show a per-unit icon row so the player can pick a specific
  unit out of the group, matching AoE's own multi-select portrait row.
  Investigated first (an Explore agent survey) rather than assumed:
  confirmed no icon lookup tied to a live `Unit` existed anywhere (only
  hardcoded per-button icon keys in `BuildMenu.cs`'s own training buttons),
  and `SelectionManager` had no way to narrow a multi-unit selection down
  to one. Both needed building. 3 design decisions confirmed via
  AskUserQuestion before writing anything: clicking an icon narrows the
  whole selection to that unit (not just a highlight); icons are sourced
  from a **new `Unit.IconKey` field stamped by each factory at spawn**
  (not derived from the coarse `UnitClass` enum, which conflates e.g.
  Worker/Soldier/Spearman all as "Infantry"), reusing the *exact* icon-key
  strings `BuildMenu.ApplyTheme()`'s own `SetupGridCell` calls already
  establish (`"train_worker"`, `"train_unique_chola"`, etc.) so the two
  stay in sync for free; and the group row is a **single horizontal
  scrollable strip** (not a wrapping grid), the user's own choice over
  wrapping - new territory for this codebase (no prior horizontal-scroll
  precedent; `SettingsMenu`'s own scroll list is vertical).
  - `Unit.cs` gained one public field, `IconKey` (string, null by
    default - falls back to a flat placeholder square in the UI, same
    convention `BuildMenu.cs` itself already uses for its own icon-less
    buttons).
  - **26 unit-spawning factories** each gained one line
    (`unit.IconKey = "train_X";`) right after their own existing
    `AddComponent<Unit>()` call, using the identical key string
    `BuildMenu.cs` already uses for that unit's own training button -
    confirmed by grepping every `SetupGridCell(...)` call in
    `BuildMenu.cs` first rather than inventing new key strings.
    `TradeShipFactory` deliberately stamps nothing (matches
    `tradeShipButton`'s own `null` icon key - no art exists for it yet).
  - `SelectionManager.cs` gained one new public method,
    `SelectOnly(Unit)` - thin wrapper over the existing private
    `ClearSelection()`+`Select()` pair (unchanged), so indicator state
    stays correct for every unit, not just the kept one.
  - `SelectedUnitPanel.cs`: new `SetUpGroupIconRow()` (built once in
    `Awake()`, a `ScrollRect`+`Viewport`(`RectMask2D`)+`Content` row
    positioned right below `groupCountLabel`, horizontal-only scroll) and
    `RefreshGroupIconRow()` (rebuilds the icon buttons only when the
    actual set of selected units changes - not every frame - comparing
    the live `Selected` list against a cached snapshot by reference;
    avoids both wasted GameObject churn and the row silently rescrolling
    to the start on every single frame). Each icon's `onClick` calls the
    new `SelectionManager.SelectOnly`. Group row is explicitly hidden in
    every other branch (single-unit, building, no-selection) so it can
    never linger stale, same lesson as this session's earlier
    stale-HP-bar fix.
  511/511 EditMode tests pass unmodified - `SelectionManager` itself has
  no existing EditMode test coverage at all (its own box-select/raycast
  logic is Play-mode-only), and `SelectOnly` is a thin 2-call wrapper over
  already-untested-but-already-live-relied-upon methods, so this was
  live-verified instead rather than forcing new test infrastructure onto
  an otherwise-untested class. Live-verified via UnityMCP through the real
  production path: selected 4 real "Maratha Worker" units, confirmed
  `IconKey` read back as `"train_worker"` on the live `Unit` instance,
  screenshotted the real icon row (4 worker icons, correctly positioned
  clear of the portrait notch), invoked a real icon button's `onClick`
  and confirmed `SelectionManager.Selected.Count` dropped 4->1 with the
  panel correctly switching to the single-unit view ("Maratha Worker",
  HP 20/20) and the group row hidden. One scoped commit (`Unit.cs`,
  `SelectionManager.cs`, `SelectedUnitPanel.cs`, and the 26 factory
  files only - `ObjectivePanel.cs` showed modified in the working tree
  from a concurrent session, not touched here). Next: whatever the user
  directs.
- **Fixed a real BuildMenu/SelectedUnitPanel overlap found via a resolution/
  alignment re-check (2026-09-13)** — the user asked to re-verify the HUD
  for panel overlap; measured both panels' actual world-space corners via
  `RectTransform.GetWorldCorners` (not eyeballed) and found `BuildMenu`'s
  full 8-column width (428 units) reached ~51 units past
  `SelectedUnitPanel`'s own left edge for ANY context using 2+ rows - not a
  rare edge case, a plain 13-button Worker placement menu already triggers
  it. Fixed per the user's own suggested direction (smaller icons + fewer
  columns, rather than repositioning either panel): `BuildMenu`'s grid
  dropped 8->7 columns with `GridCellSize` 48->44, landing the full-width
  grid at 348 units - comfortably inside the ~377-unit budget before
  `SelectedUnitPanel`'s left edge (confirmed live: 44-unit gap at the
  absolute worst case, a forced 21-button/3-row grid, the new 7x3
  capacity's own maximum). Capacity drops slightly again (24->21);
  pagination (already built, untouched) remains the safety valve. 511/511
  EditMode tests pass unmodified. One scoped commit (`BuildMenu.cs` only).
  Resolution testing itself wasn't re-attempted this session - the prior
  session's own environment limitation (`Screen.width`/`height` stay
  pinned regardless of any Editor Game-view resize technique tried) still
  applies and wasn't re-litigated; this pass focused on alignment/overlap
  at the current resolution instead, which is where the real bug was.
  Next: whatever the user directs.
- **Two more HUD bugs found and fixed during a full-selection-state sweep
  (2026-09-12)** — the user asked to re-check the whole HUD "with
  everything selected," which surfaced two real, previously-unnoticed
  bugs neither prior session had exercised (group selection, and the two
  bottom-bar panels' now-dynamic heights drifting apart):
  1. **Group selection left a stale HP bar/label showing** -
     `SelectedUnitPanel.Update()`'s group-selection branch (`Selected.Count
     != 1`) never toggled `hpLabel`/the HP bar at all - only
     `DrawSingle`/`DrawBuilding` do that, and neither runs for a group.
     Confirmed live: selecting a TownCenter (HP 500/500) then a group of 4
     workers left "HP: 500/500" and a full green-red bar rendering behind
     "4 units selected", entirely unrelated to the actual group. Fixed by
     explicitly hiding both in that branch.
  2. **Bottom-left (`BuildMenu`) and bottom-center (`SelectedUnitPanel`)
     panels drifted out of height sync** - a direct consequence of the
     prior session's own "fit BuildMenu to its content" change: BuildMenu's
     height now varies per context (1-3 rows) but `SelectedUnitPanel`
     stayed a fixed 110 tall, so the two panels' bottom-bar heights no
     longer matched once BuildMenu grew for a busy context. Fixed by
     caching a `BuildMenu` reference in `SelectedUnitPanel.Awake()` and
     copying its live `sizeDelta.y` onto this panel's own RectTransform
     every `Update()` - `Panel` (the framed background) is already anchor-
     stretched to fill this GameObject, so no second resize call is
     needed. Live-verified both directions: selecting a worker (13-button,
     2-row placement grid) grows both panels to match; reselecting the
     TownCenter (5-button, 1-row) shrinks both back down together, bottom
     edges staying aligned throughout. 511/511 EditMode tests pass
     unmodified (pure UI logic, no test-relevant behavior touched). One
     scoped commit (`SelectedUnitPanel.cs` only). Next: whatever the user
     directs.
- **Wave 5 item 29 follow-up: full 45-combo visual pass on the building
  team-color trim tint (2026-09-12)** — direct continuation of the
  metallic-trim shipping session immediately below, per the user's
  explicit "do a visual pass on the other 44 buildings" request. Spawned
  every civ/building combination (`BuildingModelFactory.Spawn` directly,
  forcing each onto its own real civ) and screenshotted representative
  samples across all 5 civs — 0 exceptions, mechanism runs cleanly
  everywhere. Hit and corrected a real self-caught misdiagnosis mid-
  session: a first look at Vijayanagara's TownCenter read as "over-tinted"
  (a broad pink wash), and after asking the user how to proceed they
  picked "tune the over-tinted ones" — but before touching the shader,
  directly measured the metallic maps' actual pixel data rather than
  trusting the screenshot read, and found the diagnosis was wrong:
  Vijayanagara's metallic maps are essentially all-zero (mean 0.0, 0.00%
  bright pixels for TownCenter/Barracks), so this mechanism does virtually
  nothing there — the pink patch is baked directly into Vijayanagara
  TownCenter's own albedo texture, confirmed by viewing it directly, and
  is entirely unrelated to team color. Went back to the user with the
  corrected finding rather than shipping a fix for a bug that didn't
  exist. The real, measured pattern: Chola's buildings get clean localized
  highlighting (real metallic content, 1.72% bright pixels on TownCenter);
  Maurya's TownCenter dome recolors correctly despite a near-zero average
  because its handful of bright pixels sit exactly on the gilded dome (a
  genuinely metallic surface); Vijayanagara's whole set and Maurya's
  Tower/Barracks have no usable metallic signal at all and fall back
  entirely to the existing pennant. User's second decision, given the
  corrected diagnosis: accept as-is — a shader-side clamp would only
  suppress signal further, not add signal that isn't there; fixing it for
  real needs new metallic-map art, now tracked as a content gap in
  `docs/TEAM_COLOR_ART_BRIEF.md` rather than attempted in code. No code
  changes this pass (docs-only: `docs/TEAM_COLOR_ART_BRIEF.md`'s checklist
  and findings table, `docs/IMPLEMENTATION_ROADMAP.md` item 29, this
  status section, `docs/SESSION_LOG.md`). Next: whatever the user
  directs — the unit-side Blender masks once sourced, Wave 5 item 32, or
  the Wave 6 backlog.
- **Fixed SelectedUnitPanel text overlapping the portrait-notch decoration
  (2026-09-12)** — ad hoc user bug report from the live bottom-center panel
  ("TownCenter/Complete/HP: 500/500"). Root cause, measured via direct pixel
  sampling of `panel_selected_unit.png` at the exact rows the 3 text lines
  render on (not guessed): the panel's circular portrait notch occupies
  roughly the first 21% of the texture's width, but `NameLabel`/
  `StatusLabel`/`HpLabel`/`GroupCountLabel` were only inset 8 units from the
  panel's left edge - deep inside the notch/ring art (for the 2 rows that
  cut through the portrait circle itself, that's the fully transparent
  interior; for the row above the circle, it's the ornate ring border
  itself). The panel's own `Image.Type.Simple` (from the 2026-09-12 Tier 2
  art-delivery session, chosen specifically to keep this notch a real
  circle) meant the notch scales uniformly with the panel, so a flat
  inset in scene-authored units was never going to clear it - it needed
  measuring, not assuming. Fixed by moving all 4 labels' left inset
  8->50 units (`sizeDelta.x` correspondingly 204->162, right edge
  unchanged) - the flat "safe to write on" parchment consistently starts
  around 21% of the panel width at every text row sampled. The HP bar's
  own `Frame`/`Fill` (code-created in `SelectedUnitPanel.SetUpHealthBar`)
  needed no separate fix - they copy `hpLabel`'s own RectTransform values
  at Awake, so they inherited the corrected position automatically. 511/511
  EditMode tests pass unmodified (pure scene RectTransform data, no script
  changed). Live-verified via UnityMCP: a real TownCenter selection now
  shows all 3 lines clear of the portrait ring, the circle itself fully
  visible and unobstructed. One scoped commit (`Assets/Scenes/Main.unity`
  only). Next: whatever the user directs.
- **Wave 5 item 29 follow-up: building team-color trim tint shipped, no new
  art needed (2026-09-12)** — direct continuation of the "asset-blocked"
  investigation immediately below, same session. Opening the actual texture
  files (not just checking renderer/material counts) found
  `TownCenter_metallicSmoothness.png` already exists, at the same UV layout
  as the albedo, and already separates gilded/metal trim (bright) from
  plain stone (dark) — standard PBR output every Meshy-imported civ
  building already carries, not something that needed sourcing. New
  `Core/TeamColorBuildingTint.cs` + `Assets/Resources/Shaders/
  TeamColorTrimBlit.shader`: an offscreen blit shader bakes
  `lerp(albedo, teamColor, metallicMask)` into a cached `RenderTexture`
  (one per albedo+metallic+faction combo, capped 1024x1024, reused across
  every instance of that combo in a match) and applies it via a
  `MaterialPropertyBlock` — no material duplication, composes cleanly with
  the existing civ-color tint (`TintMaterials`, a separate `Material.color`
  channel). Wired into `BuildingModelFactory.BuildVisual` right after the
  existing civ-tint call; no-ops safely for buildings with no metallic map.
  Cache released/cleared via a new `TeamColorBuildingTint.Reset()`, called
  from `CivilizationSetup.BeginMatch` alongside the existing
  `DiplomacyRegistry.Reset()` (same per-match static-registry-reset
  convention that file already established). 1 new EditMode test (511
  total, up from 510, all pass) covering the one piece of this logic
  that's GPU-independent (no-op when no metallic map is present). Live-
  verified via UnityMCP: 3 real Chola TownCenters spawned for
  Player/Enemy/Enemy2 (forced onto the same civ to isolate the team-color
  variable) all show the same gilded/ornament bands tinted to that
  faction's exact `TeamColor` while plain stone stays untouched, confirmed
  by direct screenshot comparison; separately confirmed `Reset()` actually
  clears the cache (7->0 entries). **Piloted on Chola's TownCenter only** —
  a full visual pass on the other 44 civ/building combinations is real
  follow-up work, not claimed done here; buildings with no metallic map
  (Durg/Karmashala/Monastery/drop-off buildings) still rely solely on the
  pennant. Separately found (checking the actual unit texture files, not
  assumed) that the earlier "wait for real art, source via Canva" guidance
  for units was wrong in a way that needed correcting, not just waiting
  on: those textures are UV atlases with no 2D spatial coherence, so no
  2D/AI image tool can ever produce an aligned mask — corrected
  `docs/TEAM_COLOR_ART_BRIEF.md` to specify Blender (paints directly on
  the visible 3D model, bakes to UV space automatically) instead. User
  will source unit masks that way; no unit-side code this session. Docs
  updated: `docs/TEAM_COLOR_ART_BRIEF.md`, `docs/IMPLEMENTATION_ROADMAP.md`
  item 29, this status section, `docs/SESSION_LOG.md`. Next: whatever the
  user directs — a full-roster visual pass on the building trim tint, the
  unit Blender masks once sourced, Wave 5 item 32, or the Wave 6 backlog.
- **Top bar made truly horizontal + command grid fitted to content
  (2026-09-12)** — direct follow-up to the bottom-bar reshape below, per
  the user's fresh screenshot feedback: the top bar was still a
  vertically-stacked column (not horizontal like the reference), and the
  command grid panel was a big mostly-empty rectangle for low-button-count
  contexts (confirmed live via screenshot: TownCenter's 5 buttons sitting
  inside the same 444x220 box a 24-button Barracks would need, with no
  frame art at all - just a flat semi-transparent rectangle, unlike every
  sibling HUD panel). Two changes:
  1. **`ResourceHUD.cs` rewritten for a true horizontal row** - every prior
     row's fixed scene-authored Y-stacked position is now computed in code
     instead (`LayoutResourceItem`/`LayoutTextItem`, called once from
     `Awake()` with a running x-cursor): icon+number pairs for Wood/Food/
     Gold/Stone/Population left to right, then Civilization/Age as wider
     text-only items continuing the same row - matches both reference
     screenshots, which show a single left-to-right strip, not a column.
     Resource labels also dropped their "Wood: "/"Food: "/etc. text prefix
     (now just the number, icon carries the meaning) - matches the
     references exactly and keeps the row's total width sane regardless of
     stockpile size. Panel's own `sizeDelta` is computed from the final
     cursor position rather than a fixed scene value, so it always exactly
     fits its own content. `pixelsPerUnitMultiplier` retuned 12.5->40 for
     the new ~44-tall single-row display rect (was tuned for the old
     120-tall 4-row block).
  2. **`BuildMenu.cs`'s command grid now sizes itself to however many rows/
     columns the current context actually uses**, instead of always
     reserving the full 8x3 capacity: `LayoutCommandGrid` computes
     `rowsUsed`/`colsUsed` from the visible button count (capped at
     `GridRows`/`GridColumns`), resizes the panel's `sizeDelta` and moves
     the page-nav row to sit directly under whatever height that
     produces. Width only shrinks for a genuinely single-row context (2+
     rows keeps the full 8-column width, since a partial last row under
     full rows above it is the expected shape, not something to also
     shrink). Also gave the panel real frame art for the first time -
     it had none before (a flat semi-transparent color, the one HUD panel
     without proper 9-slice art) - reusing `panel_resource_bar` like every
     sibling panel already does, since no bespoke command-panel frame
     exists yet (flagging that directly, not silently reusing without
     noting it). Live-verified via UnityMCP: a real TownCenter selection
     (5 buttons, 1 row) now renders as a tight, properly-framed rectangle
     sized to exactly 5 slots instead of a mostly-empty 444x220 box;
     force-activating 20 buttons and re-invoking `LayoutCommandGrid`
     directly confirmed the multi-row path computes the expected
     428x204 (3 rows, full 8-column width) before the real per-frame
     context logic reasserted the true 5-button state a moment later.
  Also worked through this session's own recurring fog-of-war-looks-like-
  a-render-bug trap for real this time: disabled `FogOfWarManager`'s own
  `MeshRenderer` directly for clean screenshots, rather than fighting
  camera framing around it as earlier attempts this session did - noting
  the technique here since it'll recur in any future HUD verification
  pass. 510/510 EditMode tests pass unmodified (pure UI layout, no
  test-relevant logic touched). One scoped commit (`BuildMenu.cs`/
  `ResourceHUD.cs` only - no scene changes needed this round, everything
  is computed at runtime now). Next: whatever the user directs.
- **Wave 5 item 29 (Player/team colour system) re-investigated, asset-blocked
  (2026-09-12)** — the user reopened this item from live play and supplied 3
  real AoE II: Definitive Edition reference screenshots showing the target:
  team color painted onto architectural trim (dome/roofline/banner cloth) on
  buildings and as a large, dominant cloth/tunic area on units, not the small
  pennant this item originally shipped. Investigated before any code, per this
  item's own note to design-check first: both units and buildings turned out to
  render from exactly one flat-color material for the whole model — units via
  `HumanModelFactory.ApplyPaletteMaterial` (the same mechanism that drives civ
  identity, no separate cloth region to isolate), buildings confirmed live via
  UnityMCP (Chola's TownCenter/Tower/Barracks each have exactly 1 `Renderer`/1
  `Material`) and in `Assets/Editor/MeshyBuildingImporter.cs`'s import-time
  material-slot collapse. Neither has an existing "trim"/"cloth" region a code
  change alone could isolate and tint. Put both findings to the user directly
  (AskUserQuestion, twice) rather than shipping a silent compromise; user chose
  "wait for real art" for both, declining the offered code-only fallbacks (a
  bigger cloth accessory for units, bigger/multi-banner treatment for
  buildings). Wrote `docs/TEAM_COLOR_ART_BRIEF.md` — a mask-texture +
  shader-lerp spec with a full per-unit/per-building checklist and a
  recommended pilot scope (Soldier body + TownCenter) before committing to the
  full ~65-asset roster. No gameplay/rendering code changed this session;
  existing banner/pennant code (`Core/TeamColorAccent.cs`) is untouched and
  keeps running as the fallback. Docs-only commit
  (`docs/TEAM_COLOR_ART_BRIEF.md` new, `docs/IMPLEMENTATION_ROADMAP.md`/
  `CLAUDE.md`/`docs/SESSION_LOG.md` updated). Next: whatever the user
  directs — this item needs new mask-texture art sourced before more code can
  land on it; otherwise Wave 5 item 32 or the Wave 6 backlog.
- **HUD bottom bar reshaped to match AoE reference layouts (2026-09-12)** —
  direct follow-up to the 4-bug fix immediately below; the user shared 2 AoE
  II/III reference screenshots and asked for the HUD reshaped to match, then
  confirmed 2 concrete decisions via AskUserQuestion before any scene edit:
  the command grid becomes 8 columns x 3 rows (not the recommended-but-
  unconfirmed alternative), and the always-on "Civilization: X" label stays
  but folds into the top resource bar (neither reference shows a separate
  civ/pop/age box - both merge it into the top strip). Two changes:
  1. **`BuildMenu.cs`'s command grid: 4 columns x 7 rows -> 8 x 3**
     (`GridColumns`/`GridRows` only - `LayoutCommandGrid`'s positioning
     math and `ComputeGridPage`'s pagination are both column-count-driven
     and needed no other change). Capacity drops slightly (28->24, close
     to today's real worst case of ~24 Barracks buttons); paging (already
     built) is the safety valve for whatever pushes past it. Scene:
     `BuildMenu` panel resized 220x490 (tall vertical sidebar) -> 444x220
     (wide, short), still bottom-left anchored; `GridPrevButton`/
     `GridNextButton`/`GridPageLabel` repositioned from y=-450 to y=-184 to
     sit below the now-3-row-tall grid instead of the old 7-row one.
     Live-verified via UnityMCP: selecting a building shows its active
     buttons landing at exactly x=8/60/112/164/216 (52-unit spacing =
     48 cell + 4 gap), y=-8 - precisely the 8-column formula, confirmed
     numerically since a fog-of-war/camera-framing issue (unrelated to
     this change - normal unexplored-fog black, not a bug) made a clean
     screenshot of the grid's icons hard to get this session.
  2. **Civilization/Population/Age folded back into `ResourceHUD`'s own
     top-left panel as rows 5-7**, undoing the 2026-09-05 "item 30" split
     that gave them their own separate `MatchStatus` box lower on screen -
     that split predates both reference images and doesn't match either.
     `civLabel`/`populationLabel`/`ageLabel` reparented from the now-
     deleted `MatchStatus` GameObject directly onto `ResourceHUD`,
     positioned as 3 more 24-unit-spaced rows continuing the existing
     Wood/Food/Gold/Stone pattern; `ResourceHUD` grown 200x120 -> 200x180
     to fit all 7 rows under one shared frame/background (no separate
     background needed anymore - removed the now-dead
     `matchStatusBackground` field and its wiring from `ResourceHUD.cs`
     entirely rather than leave a dangling reference). `InfoPanel`
     shrunk 220x202 -> 220x110 (now holds only `SelectedUnitPanel`, its
     own height) since `MatchStatus` no longer lives under it.
     Live-verified via UnityMCP screenshot: all 7 rows (Wood/Food/Gold/
     Stone/Civilization/Population/Age) render cleanly inside one frame
     with correct spacing, no overlap, no empty gap.
  `MinimapController` (bottom-right, 220x220) needed no change - already
  bottom-aligned at a compatible height. 510/510 EditMode tests pass
  unmodified (pure UI layout, no test-relevant logic touched). One scoped
  commit (`BuildMenu.cs`/`ResourceHUD.cs`/`Main.unity` only). Next:
  whatever the user directs - if the command grid or top bar still don't
  read right against the references once fully visible in a real match,
  the exact pixel numbers above are the levers to retune.
- **Fixed 4 more UI layout bugs found from a live screenshot review
  (2026-09-12)** — direct follow-up to the CivPicker sibling-order fix
  immediately below, per the user's "check the game for any other similar
  bugs" request plus a live screenshot showing 2 new symptoms. All 4
  confirmed live via UnityMCP before touching code, not assumed:
  1. **`HoverTooltip.cs` showed a tooltip for bare ground** — the raycast
     unconditionally set `line1 = go.name` for whatever it hit, so hovering
     open terrain showed a tooltip reading literally "Ground" on every
     frame the cursor wasn't over a real entity (not "stuck", just firing
     correctly per the old, wrong logic almost all the time). Fixed by
     gating the whole block on the hit object actually having a
     `Unit`/incomplete `ConstructionSite`/`ResourceNode`/`Attackable`
     component first. Live-verified: `panelRoot.activeSelf` is now `false`
     while hovering ground (was unconditionally `true` before).
  2. **Same tooltip panel was a fixed size regardless of content** — a
     1-line hover ("Ground", or now any single-line case) got the same
     tall 3-line box as a full unit tooltip (name+status+HP), mostly
     empty. Fixed with a small formula
     (`38 + (lines-1)*16 + 18 + 17`) derived directly from the existing
     3-line layout's own numbers (validates exactly against the current
     105-tall 3-line case), computed each time the tooltip shows - the
     panel's top-pivoted anchor means only the bottom edge moves, so the
     crown-ornament art fit from the 2026-09-12 Tier 2 session is
     untouched. Live-verified both a 1-line (73 tall) and 3-line (105
     tall, unchanged) tooltip render cleanly with no art
     stretching/clipping.
  3. **`ResourceHUD.cs`'s civLabel wrapped onto 2 lines for long civ
     names, overlapping the Population row below it** — confirmed by
     setting the label to "Civilization: Vijayanagara" (the longest civ
     name) and measuring: `preferredWidth=206` against a 184-unit box,
     `overflowMode=Overflow` with word-wrap on meant the 2nd line rendered
     straight into the population row 26 units below it (screenshotted,
     visibly overlapping). Fixed by switching `civLabel`/`ageLabel` to TMP
     auto-sizing (word-wrap off, `fontSizeMin=10`) instead of wrapping -
     every civ name now shrinks to fit on one line rather than breaking
     into a second; short names (Chola, Maratha, ...) render unaffected at
     the max size. Live-verified "Civilization: Vijayanagara" now renders
     on one line, no overlap.
  4. **New bug found while testing #3: `LanMatchMenu`'s own Canvas
     z-fought with `ResourceHUD`** — both occupy the same top-left corner;
     `LanMatchMenuCanvas.sortingOrder` had never been explicitly set
     (defaulted to 0, tied with `UICanvas`), so for
     equal-`sortingOrder` `ScreenSpaceOverlay` canvases there's no
     reliable winner - confirmed live, the two panels' content rendered
     interleaved with each other. Fixed by setting
     `sortingOrder = 50` (beats `UICanvas`=0, stays below
     `SettingsMenu`/`HotkeyOverlay`=200, `DiplomacyMenu`=190,
     `MissionSelectMenu`=300). Live-verified: the LAN Match panel now
     renders as one clean, fully-readable block on top of the resource
     bar instead of the two interleaving.
  510/510 EditMode tests pass unmodified for all 4 (pure UI logic/values,
  no test-relevant logic touched). One scoped commit
  (`HoverTooltip.cs`/`ResourceHUD.cs`/`LanMatchMenu.cs` only). **Separately
  flagged, not yet started**: the user shared 2 real AoE II/III reference
  screenshots and wants the bottom HUD reshaped to match — `BuildMenu` is
  currently a tall 220×490 vertical sidebar (not a wide bottom bar as the
  references show), and Civilization/Population/Age should likely fold
  back into the top resource bar rather than sitting in the separate
  bottom-center `MatchStatus` panel the 2026-09-05 "item 30" session
  deliberately split it into. This needs a concrete redesign plan agreed
  with the user before touching `BuildMenu.cs`/`ResourceHUD.cs`/
  `Main.unity` - not started this session.
- **Fixed MatchStatus (Civilization/Population/Age) bleeding through the
  CivPicker screen (2026-09-12)** — ad hoc user bug report from a live
  screenshot, not caused by any of this session's own earlier fixes. Real
  root cause, confirmed by reading `UICanvas`'s actual sibling order (not
  guessed): `CivPicker` — its own header comment calls it "a pre-match,
  full-screen blocking overlay" — sat at sibling index 4, while `InfoPanel`
  (holding `MatchStatus`, which shows the Civilization/Population/Age
  labels `ResourceHUD.cs` drives) sat at index 6, **after** it. Unity draws
  Canvas children in ascending sibling order, so `InfoPanel` painted on
  top of CivPicker's backdrop instead of being hidden behind it —
  `ResourceHUD`/`BuildMenu`/`HoverTooltip`/`MinimapController` (indices
  0-3, all before CivPicker) were correctly hidden the whole time; only
  `InfoPanel` was ever misplaced. Secondary, non-bug detail noted while
  investigating: the specific values shown ("Chola"/"0/10"/"Ancient Age")
  are `CivilizationRegistry.For`'s own documented pre-match fallback
  defaults (Chola, since no civ is assigned yet) — correct, harmless
  behavior once the panel is actually hidden again. Fixed with a pure
  scene-hierarchy reorder (`CivPicker.transform.SetAsLastSibling()`, no
  code change) — confirmed via AskUserQuestion before touching the scene.
  510/510 EditMode tests pass unmodified. Live-verified via UnityMCP: a
  fresh CivPicker screen no longer shows the Civilization/Population/Age
  card, and selecting a civ (Rajput) still correctly highlights the card
  and enables Confirm, with nothing else regressed. One scoped commit
  (`Assets/Scenes/Main.unity` only). Next: whatever the user directs.
- **Swept the rest of the menu screens for the same overflow bug, found and
  fixed 3 more real, independent ones (2026-09-12)** — direct follow-up to
  the CivPicker fix immediately below, per the user's "check the other menu
  screens for the same issue" request. Went through every screen-filling UI
  overlay in the project (MissionSelectMenu, ScenarioEditorMenu,
  GameOverScreen, SettingsMenu, DiplomacyMenu, LanMatchMenu, HotkeyOverlay)
  live in Play mode via UnityMCP, not by inspection alone — CivPicker/
  MissionSelectMenu/ScenarioEditorMenu/GameOverScreen were all already
  clean. Found 3 real, pre-existing overflow/clipping bugs, **none of them
  caused by the CanvasScaler change** — each had its own independent root
  cause, just the same visible symptom (missing/cut-off text):
  1. **`SettingsMenu.cs`'s Key Bindings list** — every row's action label
     (`Cycle Stance`, `Place Barracks`, etc.) was positioned 10 UI units
     past its own `ScrollRect` viewport's left `RectMask2D` clip edge
     (label center `x=-100` with a 300-wide box put its left edge at -250,
     10 past the 480-wide viewport's own -240 edge), chopping the first
     letter off every single row ("ycle Stance", "lace Barracks", ...).
     Fixed by moving the offset to `x=-80` (10-unit margin, matching the
     value button's own 10-unit margin on the opposite edge). Pure math
     fix, one line.
  2. **`LanMatchMenu.cs`'s whole panel** — two separate bugs on the same
     screen. (a) The panel root was anchored `(0.5,0.5)` with a fixed
     `anchoredPosition=(-620,260)` on a **Constant Pixel Size** Canvas (raw
     screen pixels, no `referenceResolution`) — correct on whatever wide
     monitor it was tuned against, but overflowing off the LEFT edge on a
     real ~1484px-wide window ("LAN Match (Phase 5 MVP)" clipped down to
     "(Phase 5 MVP)"). Fixed by switching to a top-left corner anchor
     (`anchorMin/Max/pivot=(0,1)`, `anchoredPosition=(20,-20)`) instead of
     a center-relative offset — correct at any window size above roughly
     440px wide, not just the one size this was tuned against. (b) The
     Civilization/Scenario rows' `<`/`>` cycle buttons were children of the
     SAME GameObject as the row's own `Text` (not a mask-clip issue this
     time — a genuine z-order overlap): a left-aligned `HorizontalLayoutGroup`
     stacked the buttons flush against the row's left edge, directly on top
     of the label's own left-aligned text, covering its first ~60px
     ("Civilization: Chola" rendered as "on: Chola"). Fixed by setting
     `childAlignment = TextAnchor.MiddleRight` so the buttons dock to the
     right instead, clear of the label.
  3. **`HotkeyOverlay.cs`'s 3rd column** — a real, older, structural bug
     that's been growing for months, not a one-line fix (confirmed via
     AskUserQuestion before touching it — user picked "make it scrollable"
     over a quick rebalance or just flagging it). The Town
     Center/Barracks/Durg/Karmashala column has organically grown to ~46
     rows (every new trainable unit/tech across many sessions — Trebuchet,
     Scorpion, Camel Rider, Cavalry Archer, Hero, ...) against a static,
     **unmasked**, fixed-position 3-column layout inside a 900×620 box —
     content was simply drawn past the box's own bottom edge with nothing
     to clip it, spilling all the way off the bottom of the real window
     (confirmed live: `Content.sizeDelta.y` = 884 units for a box with only
     490 units of intended row space). Fixed with the exact same
     `ScrollRect`+`Viewport`(`RectMask2D`)+`Content` pattern
     `SettingsMenu`'s own Key Bindings list already established: content
     sized to the TALLEST column (computed at build time, not hardcoded),
     top-pivoted rows using the same distance-from-top positioning
     convention, only vertical scroll needed since `columnX` values stay
     valid unchanged (still relative to content's own center — content
     width still matches the box). Live-verified scrolling all the way to
     the bottom lands cleanly on `Karmashala Selected` with the `Close`
     button still visible and un-overlapped, nothing spilling past the
     mask. 510/510 EditMode tests pass unmodified for all 3 fixes (pure UI
     layout/code, no test-relevant logic touched). Live-verified every fix
     via UnityMCP screenshots through the real runtime-built panels (these
     3 menus are code-generated at Awake, not scene assets, so verification
     needed forcing each panel active via reflection and comparing
     before/after screenshots — not just re-reading the numbers). One
     scoped commit (`SettingsMenu.cs`/`LanMatchMenu.cs`/`HotkeyOverlay.cs`
     only). Next: whatever the user directs.
- **Fixed CivPicker card overflow on the "Choose Your Civilization" screen
  (2026-09-12)** — not a numbered roadmap item, an ad hoc user-reported sizing
  bug, fixed the same way as the Tier 2 overlap fix immediately below (root-
  caused via live measurement, pure scene-data change, no code). Root cause:
  `CivPicker`'s 5 civ cards (`Card_Chola`/`Card_Vijayanagara`/`Card_Rajput`/
  `Card_Maurya`/`Card_Maratha`) have scene-authored, non-stretched
  `RectTransform`s (fixed `sizeDelta`/`anchoredPosition`, not driven by any
  layout group or C# positioning code — confirmed via a full read of
  `CivPicker.cs`, which does no card-layout math at all) — 240-wide cards at
  270-unit spacing, a 1320-unit-wide span, authored back when `UICanvas` was
  in `Constant Pixel Size` mode against the actual (large) screen resolution.
  The `Scale With Screen Size` HUD-responsiveness fix logged immediately
  below this session correctly fixed the HUD, but as an unflagged side
  effect it also shrank every OTHER UI element sharing that same canvas
  (`CivPicker` included, confirmed via its own `RectTransform.rect` reading
  ~991×606) down to the new `referenceResolution` of only 1000×600 UI
  units — so `CivPicker`'s untouched 1320-wide card row started overflowing
  both edges of the now-narrower canvas, exactly matching the user's
  screenshot (Chola and Maratha's cards visibly clipped at the left/right
  edges). Fixed by proportionally shrinking the cards to fit the new
  reference width, not by touching `CanvasScaler` again (that's shared,
  global, and already correctly serving the HUD): card width 240→170,
  spacing 270→190 (same 30-unit gap ratio preserved), centers now
  ±380/±190/0 instead of ±540/±270/0 — total span 1320→930, fits inside the
  ~991-wide canvas with ~30 units of margin each side. Height (300) and the
  crest/Name/Blurb inner layout untouched — `Name`/`Blurb` are already
  stretch-anchored to their parent card, so they resized automatically with
  no extra edit needed (confirmed by inspecting their `RectTransform`s
  before editing anything). 510/510 EditMode tests pass unmodified (pure
  `RectTransform` data change, no script touched). Live-verified via
  UnityMCP: entered Play mode, screenshotted the real `CivPicker` panel —
  all 5 cards now render fully on-screen with clean margins on both sides
  and the Confirm button fully visible, matching every other civ's card
  exactly (previously only the 3 center cards were fully visible). One
  scoped commit (`Assets/Scenes/Main.unity` only). Next: whatever the user
  directs.
- **Fixed the Tier 2 text/crown-ornament overlap (2026-09-12)** — same-day
  follow-up to the Tier 2 art delivery session immediately below, picked up once
  the user started the flagged background task (`task_9e3bd382`). Root cause,
  measured precisely via pixel-centerline sampling (not re-guessed): the new
  frames' ornament bands are bigger than the original session estimated —
  `panel_selected_unit.png` ~32%/25% top/bottom (only 43% flat), `panel_tooltip.png`
  ~36%/32% (only ~33% flat) — so the old label Y-positions (tuned for the thinner
  placeholder art) put the top text row under the crown ornament. Fixed with pure
  scene-data changes, no code: grew `SelectedUnitPanel` 70→110 and `HoverTooltip`'s
  `Panel` 56→105 (`RectTransform.sizeDelta`), repositioned every label row to fit
  the new flat zone, and shifted/grew `MatchStatus`/`InfoPanel` by the same delta
  to absorb `SelectedUnitPanel`'s growth with zero ripple onto `BuildMenu`/minimap
  (independently anchored siblings) — `HoverTooltip`'s own panel needed no sibling
  adjustment at all, confirmed via reflection it's a self-contained floating panel
  repositioned to the cursor every frame, not part of any static layout.
  Live-verified via UnityMCP: all 3 text rows in both panels now render fully
  clear of both ornaments (not just reduced — the initially-considered "accept
  minor residual overlap" compromise turned out unnecessary once heights matched
  the correctly-measured fractions). 510/510 EditMode tests pass unmodified (pure
  scene-data change). One scoped commit. This closes `task_9e3bd382` — no more
  open Tier 2 follow-ups. Next: whatever the user directs — Tier 3 (modal frame,
  menu buttons, civ-select crests) is the next unchecked tier, or any other
  roadmap item.
- **UI_ART_BRIEF.md Tier 2 art delivery wired (2026-09-12)** — not a numbered
  roadmap item. User supplied a real Canva delivery at `/Users/bhoome/Downloads/
  tier 2/` matching Tier 2's checklist (Selected-unit panel frame, HP bar frame +
  fill, Hover-tooltip panel frame) plus a bonus: purpose-made replacements for the
  4 originally-spec'd cursor states (superseding the 2026-08-28 Asset Store
  approximate matches) and 4 NEW per-resource gather cursors (Wood/Food/Gold/Stone)
  splitting the single spec'd "Gather" icon. User confirmed (AskUserQuestion) wiring
  all 4 gather cursors with real per-resource switching. Same defect as Tier 1: all
  12 files were RGB with no alpha, checkerboard baked into pixels — fixed by reusing
  `Tools/ui_art_alpha_key.py` unchanged, plus one manual seed-pixel addition (in the
  invocation script only) for `selected unit panel frame.png`'s circular portrait
  notch, which sat fully enclosed by the frame ring and so wouldn't otherwise clear
  under the tool's border-connectivity preservation logic. **Found and fixed a real
  9-slice bug live, not assumed correct**: the portrait notch sits in the *vertical
  middle* of the left edge — inside 9-slice's stretchable middle band, not a
  non-stretching corner — so `Image.Type.Sliced` squished it into a thin sliver at
  this panel's fixed 220×70 size; switched to `Image.Type.Simple` (same fix
  `BuildMenu.cs`'s command-card buttons used previously for an analogous reason),
  re-verified live: the notch now reads as a recognizable circle. Retuned
  `SelectedUnitPanel.cs`'s HP-bar-frame multiplier (13→9.35) and `HoverTooltip.cs`'s
  tooltip multiplier (44→13.125), both freshly computed from the new art's real
  dimensions against the live display rect (queried via UnityMCP), never reusing
  the old placeholder's stale values. Code change: `HoverTooltip.cs`'s
  `HoverCursorState.Gather` split into `GatherWood/Food/Gold/Stone`;
  `ResolveCursorState` (the pure/tested function) gained a `ResourceType` parameter
  and switches on it; `Update()` passes the hovered node's real resource type
  through. 3 new EditMode tests (510 total, up from 507, all pass). Live-verified
  via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real damaged Soldier force-selected
  showing the new frame/HP-bar/notch all rendering cleanly; the real `HoverTooltip`
  panel showing its new frame with clean, un-clipped text; `ResolveCursorState`/
  `TextureFor` invoked directly against all 4 real `ResourceType` values, each
  correctly resolving to its own distinct, successfully-loaded cursor texture; the
  other 4 cursor states confirmed still loading correctly post-overwrite. **Found,
  flagged, not fixed**: the new frames' taller decorative top crown ornament now has
  the top text row (unit name) crossing under it in both panels — text stays
  readable, but it's a real cosmetic regression from the old cleaner layout; fixing
  it needs a panel-height increase or repositioned text rows, genuinely separate
  layout work from this session's art-wiring scope. Flagged via `spawn_task`
  (`task_9e3bd382`). `docs/UI_ART_BRIEF.md`'s Tier 2 checklist fully checked off.
  Next: whatever the user directs — Tier 3 (modal frame, menu buttons, civ-select
  crests) is the next unchecked tier in that doc, the flagged text/ornament overlap
  follow-up, or any other roadmap item.
- **HUD scaling made responsive: CanvasScaler switched to Scale With Screen
  Size (2026-09-12)** — not a numbered roadmap item, a direct follow-up to
  the `scaleFactor 1→1.6` fix immediately below. That fix made the HUD
  readable at one specific window size, but per the user's follow-up report
  (with screenshots) it was still reading small, and — the real ask — they
  wanted the UI to actually **track the game window's size**: shrink the
  HUD when the window shrinks, grow it when the window grows, rather than
  sit at one fixed multiplier tuned for a single observed resolution.
  `Constant Pixel Size` mode (even with a `scaleFactor`) can never do this —
  it renders every element at a literal fixed pixel count regardless of
  actual resolution, by design. Switched `UICanvas`'s `CanvasScaler` to
  `Scale With Screen Size` (`uiScaleMode=1`) with `referenceResolution
  ={1000,600}` (deliberately smaller than the HUD's real design canvas —
  chosen so the computed scale lands close to the same ~1.5x the manual fix
  had already proven readable, at the window size actually observed live)
  and `screenMatchMode=MatchWidthOrHeight` at 0.5 (blend width/height
  equally, this project's existing default). `scaleFactor` reset to 1 (the
  field this mode ignores). Live-verified via UnityMCP: entered Play mode
  through a real match, read `Canvas.scaleFactor` directly at runtime —
  computed to 1.498 (matching the hand-calculated
  `sqrt(1484/1000 * 907/600)` for the observed window's actual
  `renderingDisplaySize`), confirming the scale is now genuinely derived
  from the live window size rather than a hardcoded constant — resizing the
  window will recompute it accordingly. Screenshotted the resource bar,
  command-card grid, and info/HP panel at this size: all read the same
  clear, legible size the prior fixed-multiplier fix already established.
  507/507 EditMode tests pass unmodified (pure scene-data change — 3 fields
  in one `CanvasScaler`, confirmed via diff). One scoped commit
  (`Assets/Scenes/Main.unity` only). Next: whatever the user directs — if
  a specific window size still doesn't look right, the reference resolution
  is the one further lever to retune (smaller reference = larger UI at any
  given window size, and vice versa).
- **HUD readability fix: CanvasScaler scaleFactor 1→1.6 (2026-09-12)** — not
  a numbered roadmap item, a direct follow-up to the Tier 1 art delivery
  session immediately below, per the user's report (with 2 screenshots)
  that "everything is too small for humans to read... even in full screen."
  Root cause: `UICanvas`'s `CanvasScaler` is in `Constant Pixel Size` mode
  (`uiScaleMode=0`), which renders every UI element at its literal authored
  pixel size regardless of actual screen/window resolution — with a
  `scaleFactor` of 1, a 200x120 resource panel or 48x48 command-card button
  stays exactly that size in real screen pixels on any display, including a
  large one, which is what made the user's fullscreen HUD read as tiny.
  Fixed by raising `CanvasScaler.scaleFactor` to 1.6 — a single canvas-level
  multiplier that scales every UI element (RectTransform sizes, anchored
  offsets, and font sizes together) uniformly, so relative layout is
  unaffected and nothing needed touching per-panel. Live-verified via
  UnityMCP through a real match: resource bar, command-card grid, the
  `MatchStatus`/`SelectedUnitPanel` info stack, and the minimap frame are
  all visibly and proportionally larger with text clearly readable, with no
  clipping or off-screen elements at the corners (anchor-relative offsets
  scale together with content, so corner-anchored panels stay put). 507/507
  EditMode tests pass unmodified (pure scene-data change — one field,
  confirmed via diff). One scoped commit (`Assets/Scenes/Main.unity` only —
  a single-line diff). **Superseded by the Scale With Screen Size fix
  immediately above** — this fixed-multiplier approach couldn't respond to
  window-size changes, which turned out to be part of what the user
  actually wanted.
- **UI_ART_BRIEF.md Tier 1 art delivery wired (2026-09-12)** — not a numbered
  roadmap item. User supplied a complete, self-consistent Canva delivery for
  `docs/UI_ART_BRIEF.md`'s Tier 1 spec at `/Users/bhoome/Downloads/tier 1/`
  (19 action icons, 4 command-card button states, 4 resource icons, 1
  resource-bar frame) and asked to wire it in ("all icons are ready for tier
  1"). Since this delivery's command-card frame and resource-bar frame
  overlap with what the Ornate HUD reskin session (immediately below) had
  already shipped, asked the user directly (AskUserQuestion) whether to
  overwrite — confirmed "replace everything." Identification: 18/19 action
  icons matched the spec by filename directly; `build archer.png` is
  visually a bow-and-arrow (Train Archer, just mislabeled "build"); the 4
  unlabeled command-card states and 4 unlabeled resource icons were
  identified by direct visual inspection, not filename-trusted (command-card
  1=Normal/2=Pressed/3=Disabled/4=Hover by brightness+saturation; resource
  icons 1=Wood/2=Stone/3=Food/4=Gold by subject). **Found a real defect
  before wiring, not caught by trusting the export**: all 28 raw Canva PNGs
  had their checkerboard "transparency" baked into literal RGB pixel values,
  not real alpha (confirmed via direct pixel sampling) — would have rendered
  a visible gray checkerboard in-game. Fixed with a scratch Python script
  (border-connected-component flood-fill on near-white/near-gray pixels,
  via `scipy.ndimage.label`, so genuine light content inside the art like a
  white turban isn't eaten), feathered for anti-aliasing, cropped to content
  for icons. Measured fresh 9-slice borders for both new frames via
  pixel-centerline sampling (command-card: 100px uniform on a 1264x1264
  square; resource bar: 180/120/180/200 asymmetric on a 1776x578 image) —
  per this project's own prior lesson (see the layout-bug-fix entry below)
  never to guess/reuse stale border values. `ResourceHUD.cs`'s
  `pixelsPerUnitMultiplier` for both `background`/`matchStatusBackground`
  needed real retuning (the new source art is far larger than the old
  213x80 ornate-reskin source those values were tuned for) — first attempt
  (6/5) still showed live text overlapping the top border in a screenshot;
  iterated live via UnityMCP to 12.5/16 before the resource bar and
  Civilization/Population/Age panel both read cleanly with no overlap.
  507/507 EditMode tests pass unmodified (pure asset + import-setting
  change). Live-verified via UnityMCP through the real production path: a
  real match (`CivilizationSetup.BeginMatch(Maurya)`, via reflection), the
  pre-match menu overlays deactivated directly to see the live HUD,
  screenshotted and zoom-cropped the resource bar (4 icons + text, clean),
  the `MatchStatus` panel (3 lines, clean after the multiplier retune), and
  the command-card grid on a real selected TownCenter (5 buttons, 2 with
  real icons rendering the new frame correctly, 3 correctly on the
  placeholder square). `docs/UI_ART_BRIEF.md`'s Tier 1 checklist fully
  checked off. Next: whatever the user directs — Tier 2 (selected-unit
  panel frame, HP bar, tooltip frame — cursors already done) is the next
  unchecked tier in that doc, or any other roadmap item.
- **Ornate HUD reskin layout bug fixed (2026-09-12)** — not a numbered roadmap
  item, a direct follow-up to the ornate-HUD-reskin session above/before it, per
  the user's bug report ("the dimensions of the panels are not matching the
  screen size") with a screenshot showing garbled/overlapping text on the
  bottom-center info panel and flat command-card buttons. User first renamed the
  8 sourced Canva files to their descriptive names (`/Users/bhoome/Downloads/
  iloveimg-resized/`) — byte-diffed every one against what was already wired and
  confirmed **all 8 were already correctly identified/mapped** (including the 4
  `CommandCardButton` states in brightness order: plate=hover, plate 2=normal,
  plate 3=pressed, plate 4=disabled) — so this was a real layout bug, not a
  mis-mapped asset. Root-caused via direct pixel measurement (a small Python/
  Pillow script sampling color transitions along each sprite's horizontal/
  vertical centerline) rather than guessing: `panel_resource_bar.png`/
  `panel_selected_unit.png`/the 4 `CommandCardButton` sprites all carried
  `spriteBorder` values that **exceeded the actual image dimensions**
  (`panel_selected_unit.png`'s border summed to 1300px against a 191px-wide
  image) — stale leftovers from a differently-sized pre-resize source, never
  recalculated when the final art was wired in. Fixed via `manage_asset modify`
  with border values measured from each sprite's real frame/flat-area boundary.
  That alone wasn't sufficient — 3 more real bugs surfaced during live re-
  verification, each requiring a proper before/after screenshot comparison, not
  assumed fixed: (1) `SelectedUnitPanel.cs`'s `pixelsPerUnitMultiplier = 65f`
  was tuned for the *old* 2752x1536 placeholder art's border scale, not this
  session's 191x192 replacement — recalculated to 2.74 (the actual native-height
  ÷ display-height ratio, 192/70); (2) `BuildMenu.cs`'s command-card grid buttons
  are always exactly 48x48 with square 128x128 source art, so `Image.Type.Sliced`
  (fighting a 9-slice border against a small square target, either overlapping
  or washing out all ornate detail depending on the multiplier) was the wrong
  choice entirely — switched to `Image.Type.Simple`, a clean uniform downscale
  that keeps full carved detail crisp, no border math needed; (3) `BuildMenu.
  ApplyTheme()`'s themed buttons were still carrying a leftover
  `Image.color ≈ (0.25,0.25,0.25,0.9)` dark placeholder tint authored before
  this art ever existed — silently muddying every themed button's real gold/
  brown coloring — now reset to `Color.white` alongside the sprite assignment.
  `ResourceHUD.cs`'s two `matchStatusBackground`/`background` multipliers
  (12f→1f) needed no further tuning once the border was fixed — that panel's
  display size (200x120/220x84) is *larger* than its native art (213x80), so no
  downscale-compensation was needed there, unlike the two undersized panels
  above. 507/507 EditMode tests pass unmodified (pure visual/import-setting
  change). Live-verified via UnityMCP through the real production path at every
  step (not just the final state) — a real match
  (`CivilizationSetup.BeginMatch`, invoked via reflection since it's an instance
  method on the scene's own component, not static), a real selected TownCenter,
  cropped/zoomed screenshots of each panel before and after every individual
  fix to confirm which specific change actually mattered (the border fix alone
  wasn't enough; the multiplier fix alone wasn't enough either — all had to land
  together): the bottom-center info panel now shows clean, non-overlapping
  "Civilization/Population/Age" and "TownCenter/Complete/HP" text on properly
  carved scroll frames; every command-card button shows full ornate gold trim
  with visible corner ornaments instead of a flat tan square; and — directly
  reproducing the user's original bug report — the pre-match `CivPicker` card
  (visible dimmed behind `MissionSelectMenu`, unrelated pre-existing behavior)
  now renders its "Civilization: Chola / Population: 0/10 / Age: Ancient Age /
  Confirm" text cleanly instead of the garbled/torn look the corrupted 9-slice
  rendering originally produced. One scoped commit — hit the same recurring
  "pre-existing staged doc deletions get swept into an unrelated commit" mistake
  as the prior ornate-HUD-reskin session's own commits (this project's index
  still had `docs/BRING_IN_ART_1-4.md`/`docs/ICON_PLAN.md` staged for deletion
  from before this session), caught and fixed the same way (restore from the
  prior commit, amend, `git rm` again to preserve the original pending-deletion
  state) before finalizing. Next: whatever the user directs — the ornate HUD
  reskin delivery is now genuinely closed, code and layout both.
- **FLAGGED (2026-09-12), not yet worked: Wave 5 item 29 (Player/team colour
  system) is REOPENED.** User judgment call from live play — the shipped
  banner/pennant approach (small colored cloth flags on units/buildings,
  closed 2026-09-07) does not read correctly as a team-color system
  visually. This is not a claim of a mechanical bug in the existing banner
  code (scale-compensation, Age-up rebuild-safety, etc. are still believed
  correct) — it's a design miss: the visual/UI itself needs to be
  re-engineered with a different method. Full reopened note with candidate
  directions (per-pixel accent-mask shader, rim-light/outline shader, larger/
  more prominent banner) added directly under item 29 in
  `docs/IMPLEMENTATION_ROADMAP.md`. Not started — next session on this
  should open with AskUserQuestion to pin down what "correct" looks like
  before writing any code, per that item's own note.
- **Ornate HUD reskin closed (2026-09-12)** — not a numbered roadmap item, a follow-on
  to the icon-art-delivery session below (picked up right where that session's own
  UnityMCP disconnect left off, per the user's explicit continuation request). User
  supplied 8 Canva-generated HUD frame files sourced from the "Ornate HUD reskin" spec
  this session's own predecessor added to `docs/UI_ART_BRIEF.md`. Wired: `panel_resource_bar.png`/
  `panel_selected_unit.png` (overwritten backgrounds), all 4 `CommandCardButton` states
  (normal/hover/pressed/disabled), and a new diamond-shaped minimap frame — the first
  genuinely new visual shape in this HUD pass, not just a reskinned rectangle. New
  `MinimapController.ApplyDiamondFrame()` wraps the existing render-texture `RawImage`
  in a `Mask` component (using `panel_minimap_frame.png` as the mask shape, its
  interior opaque/exterior transparent) and layers `panel_minimap_frame_border.png`
  (interior punched transparent) on top as the visible ornate border — two crops of
  one source image, not two separate assets; the render texture itself is untouched,
  only how it's displayed. Set Sprite import type on both new panel files via
  `manage_asset`. Forced a recompile and checked `~/Library/Logs/Unity/Editor.log`
  directly per this project's own documented "console bridge can miss real errors"
  gotcha (confirmed clean — zero `error CS` lines, and `KingdomsOfBharat.Runtime.dll`'s
  timestamp postdated the script edit). 507/507 EditMode tests pass unmodified (pure
  visual/UI change, no new test expected). Live-verified via UnityMCP through a real
  match (`CivilizationSetup.BeginMatch(Maurya)`, invoked via reflection on the scene's
  `CivilizationSetup` component — it's an instance method, not static, despite how
  some earlier session-log entries phrase the call): screenshotted the real resource
  bar (ornate scroll frame, Wood/Food/Gold/Stone), the real bottom info panel
  (Civilization/Population/Age plus, once a TownCenter was selected via
  `SelectionManager` reflection, the reskinned `SelectedUnitPanel` name/status/HP bar
  stacked below it), the real command-card grid (5 buttons on a selected TownCenter,
  2 with real icons showing the new tan/gold ornate button frame, 3 correctly on the
  placeholder — matches the documented "35 icon-less buttons" baseline, not a
  regression), and the real minimap — confirmed the diamond mask genuinely clips the
  live camera feed (not just a static image) with the gold border overlay sitting
  correctly on top, no misalignment. Also explicitly checked (not assumed) that
  `HandleInput()`'s click-to-jump still works after the `RawImage` was reparented
  under the new mask GameObject: read `display.rectTransform.GetWorldCorners()` live
  and confirmed it occupies the exact same screen rect (1254,10)-(1474,230) as before
  the reparent, since every anchor/offset was copied from the original rect — the
  raycast/local-point math in `HandleInput` is geometry-driven off that same
  RectTransform and is unaffected. One scoped commit (11 files: the 8 asset files +
  `MinimapController.cs`) — hit and fixed a real process mistake mid-session: an
  `amend` used to add the required commit-attribution trailer accidentally picked up
  two unrelated pre-existing staged doc deletions
  (`docs/BRING_IN_ART_1-4.md`/`docs/ICON_PLAN.md`, leftover uncommitted state from an
  earlier session, not this session's work to claim) from the index; caught before
  finalizing, restored their exact original pending-deletion state (removed from disk,
  staged as deleted, uncommitted) via `git rm`, and re-verified the final commit
  touches only the 11 intended files. Next: whatever the user directs — no further
  ornate-HUD-reskin work is outstanding from this delivery.
- **Icon art delivery closed (2026-09-07)** — not a numbered roadmap item, picked up at
  the user's explicit request ("load them to the game before starting item 32"). User had 98
  AI-generated icons ready on an external drive with meaningless generator filenames; each was
  visually identified (multimodal read, category by category) and mapped against `BuildMenu.
  cs`'s existing icon-key slots before any Unity changes, with the full mapping table reviewed
  by the user first (caught 2 real swapped building identifications this way — Lumber Camp vs.
  Gate). Imported 75 new + 5 overwritten Sprite assets (`Assets/Resources/UI/Icons/`,
  `Assets/Resources/UI/Menu/crest_*.png`), wired ~50 `BuildMenu.SetupGridCell` icon keys
  (all 14 building types, 12 of ~15 previously-placeholder Wave 4/5 units, tier buttons, Elite
  badge), added a new `resource_population.png` to `ResourceHUD` (its own comment had
  explicitly flagged this as missing since item 30), and added dynamic per-civ icon swapping
  for `uniqueUnitButton`/`uniqueUnitButton2`/`uniqueTechButton` (the only 3 grid cells whose
  correct icon depends on the currently-selected civ, not a fixed unit) via a new `SetGridIcon`
  helper keyed by `UniqueUnitDefinition.UnitId`/`CivilizationRegistry.For(...)`. **Found and
  fixed a real, previously-silent compile error** (`barracks.Faction` is `private` — used the
  file's own existing `BuildingFaction(Component)` helper instead) that had been blocking all
  compilation for roughly an hour without `read_console` ever reporting it — Unity kept running
  a stale pre-edit assembly through several successful-looking `refresh_unity`/EditMode-test
  calls; only caught by checking `~/Library/Logs/Unity/Editor.log` directly per this project's
  own long-documented "console bridge can miss real errors" gotcha, and confirmed via the
  `ScriptAssemblies/*.dll` file timestamp. 507/507 EditMode tests pass (against the real
  recompiled assembly). Live-verified via UnityMCP: a real Maurya match, a real Durg/Barracks
  correctly swapped `uniqueUnitButton`/`uniqueTechButton` to `train_unique_maurya_elephant`/
  `uniquetech_maurya_base` on selection; every checked static grid icon resolved correctly;
  `monasteryButton`/`tradeShipButton` correctly stayed on placeholder (no art delivered for
  either, confirmed by the user). Screenshotted the real `CivPicker` screen confirming
  Vijayanagara's and Maurya's crests render as the correct new art. **UnityMCP disconnected
  right after that screenshot**, ending live verification for the session — nothing further was
  checked. **Not done, flagged directly**: Monastery and Trade Ship still have no icon (no art
  in this delivery); 11 command/UI icons (Attack-Move/Rally/Stop/Garrison/Repair/Patrol/Guard/
  Cancel) are imported but wired to nothing — no BuildMenu slot represents them yet; ~15 unused
  upgrade-tier portrait duplicates and 3 Karmashala tier-art variants sit unused on disk since
  `BuildMenu`'s tier buttons only support one static icon each, not a per-tier swap. See
  `docs/SESSION_LOG.md`'s matching entry for the full per-category identification writeup.
  Next: Wave 5 item 32 (Age/research always-visible readout, the last open Wave 5 item), or
  wiring the still-missing pieces above, user's call.
- **Wave 5 item 29 (Player/team colour system) closed (2026-09-07) — first
  Wave 5 item.** Picked up per the user's "start wave 5 item 29" request,
  right after Wave 4 closed. Research (an Explore agent survey) confirmed no
  accent-region mask/second-color channel exists anywhere in this project's
  models/shaders — `HumanModelFactory` swaps a trim-sheet texture *offset*
  per civ, `BuildingModelFactory.TintMaterials` does a flat single-tint lerp
  — so the item's own literal "tunic/shield/roof-trim mask" spec would need
  new texture/shader authoring. Flagged this back to the user (via
  AskUserQuestion) rather than assumed away; **user supplied real AoE
  reference screenshots** showing the actual convention is discrete
  geometry, not a painted mask — small flat-colored cloth banners/pennants
  draped on buildings and mounted on units. Built with zero new art: new
  `Core/TeamColor.cs` (`FactionId`→Color: Player=blue/Enemy=red/Enemy2=green,
  deliberately independent of `CivilizationProfile.PrimaryColor` so two
  Player-controlled units of different civs read as the same team) and
  `Core/TeamColorAccent.cs` (hand-built double-sided quad banners/pennants,
  same idiom `ProceduralBuildingFactory.BuildPyramidMesh`/`RallyPoint.
  BuildFlag` already use). Wired via one optional `FactionId? faction = null`
  param on the 3 shared spawn choke points (`HumanModelFactory.Spawn`,
  `BuildingModelFactory.Spawn`/`Refresh`/`BuildVisual`,
  `BoatModelFactory.Spawn`) rather than touching each factory's own logic —
  ~42 individual call sites (23 human units incl. both War Elephant
  factories, 15 buildings + `AgeTieredBuildingVisual`, 4 boats) each needed
  only one added argument, their already-in-scope `faction` parameter.
  Building banners parent under the "Visual" child, so
  `BuildingModelFactory.Refresh`'s existing Age-up destroy/rebuild
  automatically cleans up and re-creates the banner too. **Found and fixed 2
  real bugs live, not assumed away**: an `Object.Destroy` (not
  `DestroyImmediate`) on the pennant pole's collider broke several
  previously-passing EditMode tests the moment `faction` started flowing
  through in production code paths those tests exercise
  (`EntitySpawnerTests`/`DesyncRecoveryTests`) — fixed via `DestroyImmediate`;
  and `Transform.SetParent(parent, worldPositionStays: true)` only preserves
  world scale onto a UNIFORMLY-scaled parent — a non-uniform building visual
  (Farm's own squashed-Y look) squashed its banner the same way, and a
  villager rig's tiny baked bone scale (0.01) left Worker pennants nearly
  invisible — both confirmed live via UnityMCP reflection
  (`Transform.lossyScale`) before fixing, resolved with a new
  `TeamColorAccent.CompensateParentScale` that forces each accent's world
  scale back to exactly 1 unconditionally (idempotent for both the uniform
  and non-uniform cases). 6 new EditMode tests (`TeamColorTests.cs`, 507
  total, up from 501, all pass) — pure `GameObject`/`Mesh` construction, no
  `MonoBehaviour` lifecycle timing, directly testable without Play mode.
  Live-verified via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), reflection-driven spawns of a
  Soldier/Barracks/Tower/Dock/War Galley/Maurya War Elephant (the one
  factory bypassing `HumanModelFactory.Spawn`) for both Player and Enemy —
  every accent's color matched `TeamColor.For` exactly and every accent's
  `lossyScale` read exactly `(1,1,1)` after the fix; screenshotted a real
  TownCenter door banner and a real Farm roofline banner, both correctly
  sized/colored. No `MinimapController.cs` change needed — it renders the
  live scene through a second camera, so banners read there automatically.
  Next: Wave 5 item 32 (Age/research always-visible readout, the last Wave 5
  item) or any other item, user's call.
- **Fixed (2026-09-07, follow-up to `task_71f5649c`): `BuildMenu.ApplyTheme()`'s
  button-theming array was missing `cavalryArcherButton`/`camelRiderButton`/
  `scorpionButton` and their 3 tier buttons** (flagged, not fixed, by the item
  24/Trebuchet session below) — those 6 buttons never got the command-card
  4-state sprite theme, unlike every sibling button. Added all 6 to the
  array. Pure UI-wiring, no new tests (501/501 unchanged). Live-verified via
  UnityMCP: a real match, a real Barracks selected, screenshotted the real
  command grid — all buttons share the themed tan look; confirmed via
  reflection all 6 report `sprite=normal`/`type=Sliced`/`transition=SpriteSwap`
  (the themed state). See `docs/SESSION_LOG.md`'s matching entry.
- **Wave 4 item 24 (Trebuchet, 1 tier) closed (2026-09-07) — this closes
  Wave 4: all 11 items (18-28) are now done.** Picked up per the user's
  "start item 24 wave 4" request, right after item 28 closed. No design
  decision needed — the roadmap already fixed scope (1 tier, Imperial
  only, long-range with a minimum range). An Explore agent survey ahead
  of Plan Mode found the naive "copy an existing siege unit" approach
  wouldn't fit: BatteringRam and Scorpion both turned out to actually be
  2-tier lines despite reading like flat units at a glance, so
  **Trebuchet is genuinely the first single-tier trainable combat unit in
  this project** — `Barracks.RequestTrainChara()` (Scout) was the closest
  training-shape precedent, not BatteringRam/Scorpion. Also found **no
  existing `RequestTrainX` method has an inline age-gate check** — every
  prior age gate lives on a tier ladder's own `RequiredAge`, which a
  single-tier unit has no tier button to hang that on — so
  `Barracks.RequestTrainTrebuchet()` is the first `RequestTrainX` with its
  own inline `AgeProgress.CurrentAge(Faction) != AgeId.Imperial` check.
  **Minimum range is a wholly new mechanic** — new
  `MeleeAttacker.SetMinRange(float)`/`minAttackRange` field (defaults
  0/disabled, every existing user unaffected): a target closer than this
  can't be fired on at all (refused, not backed away from — matching
  AoE's own trebuchet counterplay). New `UnitClass.Trebuchet` (not folded
  into `Siege` — avoids colliding with `Attackable.SiegeImmune`'s
  Siege-specific check) with 2 new `CombatBonus` pairings continuing the
  established per-siege-archetype escalation: `Trebuchet→Building = 5x`
  (Siege 3x → BatteringRam 4x → Trebuchet 5x) and
  `Cavalry→Trebuchet = 1.5x` (reuses `Cavalry→Scorpion`'s own value). New
  `Combat/TrebuchetFactory.cs` mirrors `ScorpionFactory.cs`'s shape but:
  `SetRange(10)` (exceeds the prior game-wide ceiling of 7) +
  `SetMinRange(4)`, and — **the first live consumer of `DamageType.Siege`**,
  declared in Wave 0 item 4 but unused by any attacker until now
  (`Attackable.UsesPierceArmor` already routes `Siege` to `meleeArmor`
  identically to `Melee`, so this needed zero engine change). Cost: 200
  Wood/150 Gold, no Food, base train time 8s. Full
  `NetTrainKind.Trebuchet`/`CommandSerializer`/`BuildMenu` wiring: new
  `trebuchetButton`/`trebuchetLabel` — a train button with deliberately no
  matching tier button (same shape as `charaButton`) and the first whose
  own `interactable` state depends on the current Age directly — hotkey
  Q, wired into `SettingsMenu.Actions`/`HotkeyOverlay.BarracksGroup`. 8
  new EditMode tests (`TrebuchetTests.cs`: `SetMinRange`'s
  inside/between/beyond-range cases, both new `CombatBonus` pairings, and
  `RequestTrainTrebuchet`'s Imperial-only age gate/no-double-deduct — 501
  total, up from 493, all pass). Hit the same recurring "new
  `[SerializeField]` field null in the scene" gotcha every Wave 2/3/4
  session has hit (duplicated `ScorpionButton` into a real
  `TrebuchetButton` scene object via UnityMCP). **Also flagged, not
  fixed, a real pre-existing gap found while wiring this button**:
  `BuildMenu.ApplyTheme()`'s own button array is missing
  `cavalryArcherButton`/`camelRiderButton`/`scorpionButton` and their 3
  tier buttons — likely still rendering with Unity's default button skin.
  Spawned as a background task (`task_71f5649c`) rather than silently
  expanding this item's scope. Live-verified via UnityMCP through the
  real production path: a real match (`CivilizationSetup.
  BeginMatch(Maurya)`), a real Barracks, `RequestTrainTrebuchet()`
  correctly refused at Classical age with zero deduction, then deducted
  exactly 200 Wood/150 Gold at Imperial and spawned a real "Maurya Maha
  Yantra" (`Attackable.Class == Trebuchet`, `damageType == Siege`, range
  10, min range 4); a real hostile Tower at distance ~3 (inside the
  4-unit minimum range) took zero damage over 2.5 real seconds, then
  moved to distance ~7 (between min and max) took real damage at the
  expected rate (300→108 HP across 2 real hits, matching 20 base × 5x
  minus the Tower's own armor); the real `TrebuchetButton`'s
  label/interactable correctly flipped at the Imperial age boundary, and
  its own `onClick.Invoke()` confirmed `CommandBus` lockstep routing.
  Full EditMode suite re-confirmed 501/501 after exiting Play mode. **No
  AI-side use of Trebuchet** (no AI training hook) — matches the
  established Wave 4 precedent. **This closes Wave 4 — all 11 items
  (18-28) are done.** Next: Wave 5 (cross-cutting systems, item 29
  Player/team colour system or item 32 Age/research readout) or any other
  item, user's call.
- **Wave 4 item 28 (Hero unit — Maharaja, per civ) closed (2026-09-07),
  together with Regicide (pulled forward from Wave 6 item 37).** Picked up
  per the user's "start item 28 wave 4" request, right after item 27
  closed. Item 28 was explicitly gated in its own roadmap text ("only
  build this if a Regicide-style victory condition is wanted... confirm
  the victory condition it serves BEFORE building 5 hero units") —
  resolved via 3 AskUserQuestion rounds before any code: build Regicide +
  Hero together this session; **Maharaja is the strongest unit in the
  game**, not AoE's own defenseless King (the user explicitly corrected
  away from the initially-recommended "defenseless symbol" option);
  Regicide is an opt-in `GameSettings.RegicideEnabled` toggle, off by
  default (mirrors `TimeLimitMinutes`' own "0/false = old behavior"
  convention); a dead Maharaja is retrainable once Regicide is off
  (population-cap-of-1-alive, not a one-shot). Research confirmed
  `MatchManager` is fully poll-based with zero event subscriptions
  anywhere (`EvaluateSkirmishOutcome(bool)` is `internal static`, the same
  directly-testable seam Time Limit's own session established) and
  `Attackable` has no `OnDeath` event at all (death handled inline in
  `TakeDamage`) — Regicide reuses that same poll convention rather than
  adding new event plumbing. New `Progression/HeroProgress.cs`: tiny
  compared to `UniqueUnitEliteProgress` — `IsAlive(faction)` is a pure
  live scan of `Unit.All` for a `UnitClass.Hero` Attackable matching that
  faction (same "zero bookkeeping, just recount" idiom
  `Population.Current`/`MatchManager.FactionHasForces` already use — a
  dead hero is simply gone from the registry), while
  `HasTrainedHero`/`MarkTrained` is the one genuinely persistent bit,
  needed so a faction that never built a Maharaja can't spuriously
  win/lose Regicide. New `Combat/MaharajaFactory.cs`: unlike every other
  unique unit (`UniqueUnitDefinition`, a per-civ list), Maharaja is
  stat-identical across all 5 civs — one shared factory read via
  `CivilizationRegistry.For(faction)`, matching `SoldierFactory`'s "one
  factory, civ read at spawn" shape rather than
  `RajputRoyalGuardFactory`'s one-civ-per-file shape. Stats (fallback: 220
  HP/6-5 armor/24 dmg) sit clearly above the prior ceiling (Elephant
  line's own Imperial-elite tier, ~130 HP/~17 dmg), per the user's
  "strongest unit" call. `UnitClass.Hero` (not Cavalry) — `CombatBonus`
  has zero Hero entries in either direction, falls through to the 1x
  default, same as Vaidya/Purohita's own `Support` class. No dedicated
  model exists — **flagging directly per the flag-asset-needs
  convention**: reuses the same Human Character Dummy + Horse + Sword
  combo `RajputRoyalGuardFactory` already uses, so a Maharaja is visually
  identical to a Royal Guard/Cavalry hybrid, no distinct regalia or
  silhouette. New `Durg.RequestTrainHero`/`IsTrainingHero`/
  `TickHeroTraining`: a genuinely independent countdown track
  (`_heroTrainRemaining`) alongside the existing unique-unit queue and the
  elephant/elite research tracks — training a Maharaja doesn't block or
  get blocked by unique-unit training. The population-cap-of-1 gate is
  `!HeroProgress.IsAlive(Faction)` alone (no Regicide check in
  `RequestTrainHero` at all) — this is what makes retraining after death
  "just work" independent of the Regicide setting. New
  `MatchManager.EvaluateSkirmishOutcome` Regicide branch, checked
  **before** the existing `FactionHasForces` elimination logic (deliberate
  — Regicide should end the match on hero death even while the loser's
  army is still standing): Player's hero trained-but-dead → `Defeat`;
  every hostile faction that ever trained a hero now has none alive →
  `Victory`; reuses the existing `Victory`/`Defeat` enum values rather
  than adding a new `MatchOutcome.Regicide` — `GameOverScreen`'s switch
  already treats the enum as closed (`default` → Defeat), so
  `GameOverScreen.cs` needed zero changes. New `GameSettings.
  RegicideEnabled` (PlayerPrefs-backed, same shape as `ColorblindMode`), a
  new Settings row (`SettingsMenu.BuildUi()` constructs its own UI
  entirely in code, so this needed no scene-wiring gotcha). Full
  `NetTrainKind.Hero`/`CommandSerializer`/`BuildMenu` wiring (new
  `heroButton`/`heroLabel`, hotkey M, wired into
  `SettingsMenu.Actions`/`HotkeyOverlay.DurgGroup`, added to
  `_allGridButtons`/`ApplyTheme`'s button lists for item 31's grid
  layout). New `unit_roster_template.csv` "maharaja" row (Age 3/Durg-
  gated, 220 Food/180 Gold/45s — priced above every existing unique
  unit's own ~90-130 Food/70-100 Gold range). 13 new EditMode tests
  (`HeroTests.cs`: `HeroProgress`'s live-scan/persistent-flag split,
  `Durg.RequestTrainHero`'s cap-of-1 refusal and independence from the
  unique-unit queue, and `MatchManager.EvaluateSkirmishOutcome`'s new
  Regicide branch across 5 cases — 493 total, up from 480, all pass). One
  test was written with a wrong assumption (that a lone living hero counts
  as "zero forces" for elimination purposes) and correctly failed on the
  first real run — removed as redundant with a different, correct test,
  rather than papered over. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maurya)`),
  a real Durg, `RequestTrainHero()` deducted exactly 220 Food/180 Gold and
  spawned a real "Maurya Maharaja" (242 HP, clearly above the elite War
  Elephant's ~130 HP ceiling); a 2nd `RequestTrainHero()` call was
  correctly refused with zero deduction while that Maharaja was alive;
  killing it via a real lethal `TakeDamage` hit then calling
  `RequestTrainHero()` again correctly succeeded (retraining-after-death
  confirmed live, Regicide off); toggling `GameSettings.RegicideEnabled`
  on and re-evaluating with the Player's hero dead flipped
  `MatchManager.Outcome` to `Defeat` even though the Player's Durg
  (non-hero forces) was still standing; the real `SettingsMenu` Regicide
  toggle button correctly flipped the setting and its label; the real
  scene-wired `HeroButton` (duplicated from `UniqueUnitButton2` via
  UnityMCP — the exact recurring "new `[SerializeField]` field null in
  the scene" gotcha every Wave 2/3/4 session has hit) correctly showed
  "Maharaja (Already Trained)"/`interactable=false` while a hero was
  alive, and its own `onClick.Invoke()` — once the hero was dead again —
  left the stockpile unchanged immediately and deducted the exact cost
  ~1 tick later, confirming `CommandBus` lockstep routing. Full EditMode
  suite re-confirmed 493/493 after exiting Play mode. **No AI-side use of
  Maharaja** (no AI training hook) — matches item 27's own "new
  capability, not a regression" precedent, doubly reinforced by the AI's
  existing Durg hook already only ever using unique-unit slot 0, never
  slot 1. **This closes Wave 4 item 28 and pulls Regicide forward out of
  Wave 6 item 37's original list** (Wonder/Relic remain unbuilt there).
  Next: Wave 4 item 24 (Trebuchet, 1 tier — the last open Wave 4 item) or
  Wave 5 (cross-cutting systems), user's call.
- **Wave 4 item 27 (Support units — Vaidya + Purohita, splitting AoE's
  Monk) closed (2026-09-06).** Picked up per the user's "start wave 4 item
  27" request, right after item 26 closed. The roadmap flagged one open
  question — "resolve which building houses these in Plan Mode" — plus a
  second real decision surfaced during research (how deep Purohita's
  conversion mechanic should go). Both resolved via AskUserQuestion before
  planning: **a new Monastery building** trains both units (Durg's own
  header comment calls it "narrower than Barracks... a structural unlock,
  not a second Barracks," so a third generic unit type doesn't fit there;
  Karmashala is pure research with zero training-queue code at all — a
  Monastery also sets up the Wave 5 Relic system, which the roadmap already
  earmarks as reusing whichever unit/building this item picked), and
  **Purohita's conversion is a full AoE-style chance roll** — a per-second
  chance scaled by the target's missing-HP% (lower-HP targets convert
  faster), excluding Buildings/Siege/other Support-Hero units entirely —
  rather than a simpler guaranteed conversion. Used Plan Mode given the
  size (new building + 2 new units + 2 new ability mechanics + full wiring
  across ~10 files). Research ahead of planning confirmed the underlying
  mechanics were all already safe to build on: `Attackable.Heal(float)`
  already existed (used only by `Repairable` before this item) and needed
  zero changes; `FactionMember.Faction` is a trivial settable property with
  every one of its ~76 read sites reading it live, never cached as a stale
  enum, so reassigning it on a live enemy unit is structurally safe;
  `Population.Current` is a live scan of `Unit.All`, so a converted unit's
  population counts update automatically with no bookkeeping at all;
  `DeterministicRandom.Match.NextFloat01()` already existed specifically
  for this class of problem — `RajputDefianceHook.cs` (a 25%-survival-
  chance-on-death roll) is the established precedent for "use this, not
  `UnityEngine.Random`, so a gameplay-affecting roll replays identically
  under the lockstep `CommandBus`," mirrored exactly here.
  New `Buildings/Monastery.cs`/`MonasteryFactory.cs` mirror
  `Karmashala.cs`/`KarmashalaFactory.cs`'s exact shape (single-slot queue
  for two unit kinds, same 3-tile/Market-sized footprint, same "raidable"
  220 HP/1-2 armor), Gold-only costs (no Food/Wood — "a religious
  specialist, not a fed worker," reusing the cost-model precedent several
  other Wave 4 units already established), gated to Durg Age same as Durg
  itself (`BuildingPlacer.CanPlaceMonastery`). New `Resources/
  VaidyaHealer.cs` calls `Attackable.Heal` directly with **no target-side
  component at all** — so nothing had to be added to any of the ~15
  existing unit factories — same "chase while out of range, act while in
  range" shape as `MeleeAttacker.Tick`, just healing instead of damaging.
  New `Resources/PurohitaConverter.cs`: same chase-then-act shape, but the
  "act" is `DeterministicRandom.Match.NextFloat01() < ChanceForTick(...)`
  (a pure, directly-testable per-second-to-per-frame probability
  conversion) instead of a guaranteed effect; on success, calls
  `FactionMember.Configure(myFaction)` on the target once (a one-shot state
  transition, like `GarrisonPoint.TryGarrison`, not repeated once it
  lands); `internal static CanConvert` excludes Building/Siege/Support/Hero
  targets and dead ones. New `Units/VaidyaFactory.cs`/`PurohitaFactory.cs`
  mirror `VanikFactory.cs`'s shape (item 26's own land-Trader factory) —
  both units **completely unarmed**, no `MeleeAttacker` at all, matching
  Vanik/Trade Ship's own "can't fight back" utility-unit precedent, though
  both still carry `Attackable` so they're valid, killable targets. No
  dedicated model exists for either — **flagging directly per the
  flag-asset-needs convention**: both reuse the shared Human Character
  Dummy body, civ-tinted, and are visually identical to each other and to a
  generic soldier. New `Multiplayer/AbilityCommand.cs` — a small generic
  delegate command (identical shape to `TrainCommand`/`TradeRouteCommand`)
  shared by both Heal and Convert orders rather than two near-duplicate
  classes; `NetMessageKind.Heal`/`Convert` both deliberately reuse
  `Attack`'s own existing `attackerNetId`/`targetNetId` fields (both sides
  are already Units, so no new envelope fields needed at all) — the same
  field-reuse convention item 26's `TradeRoute` established for
  `Market`/`Dock`. New `SelectionManager` `hitHealable` top-level flag
  (friendly + damaged, inserted before `hitAttackable`, same "friendly-
  only, falls through to attack otherwise" gating shape as `hitRepairable`)
  plus a third `hitAttackable` arm alongside the existing melee/boat-
  attacker ones for Purohita's convert order (`purohita != null &&
  IsHostileTarget(...) && PurohitaConverter.CanConvert(attackable)`); every
  other order branch gained `healer?.CancelHeal(); purohita?.
  CancelConvert();` alongside its existing cancel calls, same convention
  item 26 already extended for `trader`/`boatTrader`. Full `BuildMenu`/
  hotkey (`G` places Monastery; `H`/`C` train Vaidya/Purohita, the first
  hotkeys in a brand-new Monastery-selected context)/`NetTrainKind` wiring.
  18 new EditMode tests (`SupportUnitTests.cs`: `VaidyaHealer.Tick` heals
  in range/stops at full health/chases when out of range;
  `PurohitaConverter.CanConvert`'s exclusion rules; `ChanceForTick`'s
  missing-HP/deltaTime scaling; a live `Tick` test forcing
  `baseChancePerSecond` high enough that `ChanceForTick` returns exactly
  1.0 — guaranteeing success regardless of `DeterministicRandom`'s actual
  draw, since `NextFloat01()` never returns exactly 1.0 — to prove a landed
  roll actually reassigns `FactionMember.Faction` — 480 total, up from 462,
  all pass). Hit the same recurring "`Attackable.TakeDamage` unconditionally
  spawns a VFX particle burst with an Editor-only 'Destroy may not be
  called from edit mode' log outside Play mode" gotcha `SiegeSplashTests`/
  `BuildingAttackerTests` already document — a killing blow needed **three**
  `LogAssert.Expect` calls per hit (the per-hit VFX burst, the death VFX
  burst, and `Attackable`'s own `Destroy(gameObject)`), found empirically
  by re-running against the real `~/Library/Logs/Unity/Editor.log` stack
  traces rather than guessing the count. Live-verified via UnityMCP through
  the real production path: a real match (`CivilizationSetup.
  BeginMatch(Maurya)`), a real `MonasteryFactory.Place`-spawned Monastery
  correctly gated `false`→`true` on `CanPlaceMonastery` across the
  Classical→Durg age transition, real `RequestTrainVaidya`/
  `RequestTrainPurohita` (invoked through the real scene-wired
  `VaidyaButton`/`PurohitaButton` — duplicated from `KarmashalaButton`/
  `VanikButton` via UnityMCP, the exact recurring "new `[SerializeField]`
  field null in the scene" gotcha every Wave 2/3/4 session has hit) each
  spawning a real unit through `Monastery.TickTraining`'s real single-slot
  queue (confirmed Purohita's own click correctly no-op'd while Vaidya's
  training was still in progress, then succeeded once the slot freed up); a
  real Vaidya's own `VaidyaHealer` healed a real damaged
  `SoldierFactory`-spawned Soldier to full HP entirely through its own live
  `Update()` loop, no forced ticks; a real Purohita (with
  `baseChancePerSecond` forced high via reflection for a fast, deterministic
  success within a real running match) flipped a real enemy Soldier's
  `FactionMember.Faction` from Enemy to Player, confirmed
  `Population.Current(Player)` reflected the new unit with zero explicit
  bookkeeping call anywhere. Full EditMode suite re-confirmed 480/480 after
  exiting Play mode and reloading the scene. **Flagged directly, not solved
  this session**: no live re-tint system exists anywhere in the project, so
  a converted unit keeps its original owner's civ color after switching
  sides — tied to the not-yet-built Wave 5 item 29 (Player/team colour
  system). No AI-side use of Monastery/Vaidya/Purohita (no AI training/
  heal/convert hook) — this is new capability the AI never had before, not
  existing behavior being moved off Barracks, so skipping it isn't a
  regression the way Durg/Karmashala's own AI hooks were required to avoid.
  Next: Wave 4 item 24 (Trebuchet, 1 tier), item 28 (Hero unit — needs a
  victory-condition decision first), or Wave 5 (cross-cutting systems),
  user's call.
- **Wave 4 item 26 (Trader — Vanik + Trade Ship) closed (2026-09-06), full
  scope.** Picked up per the user's "start wave 4 item 26" request, right
  after item 31 closed. Item 26 was flagged in the roadmap as "design
  decision first" — whether trade routes are wanted at all, since it's
  genuinely new economic machinery (a Market-to-Market/Dock-to-Dock route
  system), not just another trainable unit. Asked the user directly via
  AskUserQuestion before planning — confirmed "build both land and naval
  variants, full scope." Plan Mode used before implementation (touches 9
  files across combat/buildings/UI/multiplayer). New `Resources/Trader.cs`/
  `BoatTrader.cs` mirror `Gatherer`/`BoatGatherer`'s exact walk/act/walk-back
  state-machine shape, but shuttle endlessly between two owned/allied
  Markets/Docks instead of depleting a resource node — paying
  `Mathf.Clamp(distance * ratePerUnit, min, max)` Gold into the owner's
  stockpile on every leg's arrival (twice per round trip), not just once at
  the end. New `Units/VanikFactory.cs`/`TradeShipFactory.cs`: both units are
  **completely unarmed** — no `MeleeAttacker`/`BoatAttacker` at all, not even
  Worker's own weak self-defense one — matching AoE's real Trade Cart/Cog
  (fragile, must be escorted, can't fight back), though both still carry
  `Attackable` so they're valid, killable targets. No dedicated model exists
  for either — **flagging directly per the flag-asset-needs convention**:
  Vanik reuses the shared Human Character Dummy body (civ-tinted, same as
  Scout/CavalryArcher) and will look like a generic soldier, not a merchant;
  Trade Ship reuses the same hull FishingBoat/WarGalley use and will look
  identical to a Fishing Boat in the field. **Vanik trains at Market, not
  Barracks** — a deliberate design call (Market's own unit) that required
  adding a whole single-slot training queue to `Market.cs` from scratch
  (mirroring `Dock`'s exact shape: `_remaining`/`IsTraining`/
  `RequestTrainVanik`/`TickTraining`/a `RallyPoint` in `Awake`), since Market
  previously had zero training-queue code at all (only `Sell`/`Buy`, both
  left untouched). Trade Ship trains at Dock, added as a 4th `TrainingUnit`
  case alongside FishingBoat/WarGalley/FireShip — the smaller addition,
  since Dock's queue infrastructure already existed. New `SelectionManager`
  right-click branch (`hitMarket`/`hitDock`, inserted before `hitAttackable`
  so a friendly Market/Dock doesn't fall through to an attack order — same
  "friendly-only, falls through to attack otherwise" gating convention
  `hitGarrison`/`hitRepairable` already established) calls
  `Trader.SetTradeRoute`/`BoatTrader.SetTradeRoute`, which resolves the
  route's "home" leg to the nearest OTHER Market/Dock owned by the trader's
  own faction (never an ally's, even though the clicked destination itself
  can be an ally's) — **deliberately out of scope for v1: trading with
  enemy/unallied Markets**, AoE's real "most profitable" case, flagged not
  built. New `NetMessageKind.TradeRoute` deliberately reuses `Attack`'s own
  two existing `attackerNetId`/`targetNetId` int fields (trader unit +
  destination building) rather than adding new envelope fields, matching
  this file's own "every field exists on every message, only the one
  matching Kind is meaningful" convention; new `Multiplayer/
  TradeRouteCommand.cs` mirrors `TrainCommand.cs`'s exact captured-delegate
  shape; `CommandSerializer` gained `ForTradeRoute`/`ToTradeRouteCommand`
  plus a new `Market` arm on `ToTrainCommand`'s building-type switch. Full
  `BuildMenu`/hotkey/`NetTrainKind` wiring: `V` on Market (the very first
  Market-context hotkey — no Market hotkeys existed before this item) and
  `T` on Dock, both reused freely per this file's own established
  mutually-exclusive-context convention; a new `MarketGroup` added to
  `HotkeyOverlay.cs`. 11 new EditMode tests (`TraderTests.cs`: the gold
  formula's clamp/scale behavior, and the nearest-owned-building routing
  rule's same-faction/exclude-destination/no-candidates cases — 462 total,
  up from 451, all pass). Hit and fixed the same recurring
  `Building.OnEnable`-isn't-synchronous-in-EditMode-tests gotcha this
  project has hit before (fixed the same way `BuildingFootprintTests`/
  `AgeUpRequirementTests` already do: register test buildings directly into
  `Building.All` rather than relying on `OnEnable`'s own timing). Live-
  verified via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), two real `MarketFactory.Place`-
  spawned Markets 30 units apart (completed via
  `ConstructionSite.CompleteImmediately()`, since the factory's third
  parameter is a real build time, not a completion fraction — caught this
  directly rather than assuming, after a first `SetTradeRoute` attempt
  correctly no-op'd against a still-under-construction Market), a real
  `VanikFactory.Spawn`-spawned Vanik given a real `Trader.SetTradeRoute()`
  call walked the real distance via its own real `NavMeshAgent` and paid
  Gold into the real stockpile on every leg's arrival (confirmed cycling
  `MovingToDestination`/`MovingToHome` over several real round trips, Gold
  climbing in `ComputeTradeGold`-clamped increments each time); the
  identical proof repeated for Trade Ship with two real `DockFactory.Place`-
  spawned Docks and a real `BoatTrader`; the real scene-wired
  `VanikButton`/`TradeShipButton` (duplicated from `SellWoodButton`/
  `FireShipButton` via UnityMCP — the exact recurring "new
  `[SerializeField]` field null in the scene" gotcha every Wave 2/3/4
  session has hit) each correctly trained a real second unit through
  `CommandBus`'s lockstep queue when clicked with the matching building
  selected (confirmed a real 2nd "Maurya Vanik"/"Maurya Trade Ship"
  GameObject existed afterward, Wood/Gold deducted only once the queued
  command actually executed, not synchronously at the click). Full EditMode
  suite re-confirmed 462/462 after exiting Play mode. No AI-side use of
  Trader/Trade Ship (no AI training hook), matching every other Wave 3/4
  unit's own explicitly-out-of-scope call. Next: Wave 4 item 24 (Trebuchet,
  1 tier), item 27 (Support units — Vaidya/Purohita), item 28 (Hero unit —
  needs a victory-condition decision first), or Wave 5 (cross-cutting
  systems), user's call.
- **`docs/IMPLEMENTATION_ROADMAP.md` item 31 (BuildMenu command-panel grid
  redesign) closed (2026-09-06).** Picked up per the user's "start item 31"
  request, right after item 30 closed. Item 30 had deliberately left
  `BuildMenu`'s internal content untouched (60 buttons stacked at fixed
  absolute Y positions, big empty gaps when only a context's subset was
  active) — this item replaces that with a real icon grid. An Explore agent
  surveyed `BuildMenu.cs` first: 60 buttons/7 contexts, Barracks worst case
  23 simultaneous; only 25/60 have a real icon (the other 35 are text-only
  with live cost/percent/age-gate state); no UI tooltip system exists
  (`HoverTooltip.cs` is 3D-raycast-only); no grid/pagination pattern exists
  anywhere in the project. Two design decisions confirmed via
  AskUserQuestion before planning: true icon-only grid + a new hover
  tooltip (not a smaller "pack the text rows tighter" option), migrating
  all 60 buttons/7 contexts in one session; paging (Prev/Next), not a
  ScrollRect, for overflow. **Key decision that kept the diff bounded**:
  none of the ~40 existing `Update*` label-generation methods changed — each
  button's `TMP_Text` label is disabled (`enabled = false`, still receives
  `.text =` writes every frame) and a new `TooltipTrigger` reads that live
  text on hover; a new `LayoutCommandGrid()` (called at the very end of
  `Update()`, after every context branch has decided `activeSelf` for the
  frame) repositions/resizes only the current page's active buttons into a
  4-column grid in code, every frame — so none of the 60 buttons needed
  scene RectTransform edits either. New `TooltipTrigger.cs`/`ButtonTooltip.cs`
  (a separate class from `HoverTooltip.cs`, pointer-event-triggered instead
  of raycast-triggered — same "don't merge mechanically-different systems"
  precedent as `CombatBonus`/`CounterMatrix`). New `BuildMenu.SetupGridCell`/
  `PlaceholderIcon` (a flat generated `Texture2D`/`Sprite`, cached after
  first build, for the 35 icon-less buttons — the same "generic procedural
  shape" fallback convention this project already uses for buildings with
  no model, applied to 2D icons — **flagging directly: real per-unit/
  per-tech icon art is still needed eventually**) replace the old
  `AddCommandIcon`. New `internal static BuildMenu.ComputeGridPage` is pure
  pagination math, fully unit-testable with no scene dependency — 6 new
  tests in `CommandGridLayoutTests.cs` (451 total, all pass). New scene
  objects (`ButtonTooltip` panel, `GridPrevButton`/`GridNextButton`/
  `GridPageLabel`) wired via UnityMCP; the 60 existing buttons needed zero
  scene edits. Live-verified via UnityMCP through the real production path:
  a real match (`CivilizationSetup.BeginMatch(Maurya)`), a real Builder
  selected showed the real 13-button Placement context as a clean icon
  grid (8 real icons, 5 placeholders, visually distinct), a real
  `BarracksFactory.Place`-spawned Barracks selected showed the real
  worst-case 23-button context on a single page with Prev/Next/page-label
  correctly hidden, a real `TooltipTrigger.OnPointerEnter` on `DurgButton`
  showed the tooltip with the exact live text `Update()` had already
  computed ("Build Durg (Requires Durg Age)"), confirmed hidden again on
  `OnPointerExit`. **Not live-verified by design**: pagination's actual
  page-2 behavior, since no context today has enough buttons to force a
  second page (worst case 23 &lt; capacity 28) — covered instead by
  `CommandGridLayoutTests.cs`'s synthetic multi-page cases. Next: item 32
  (Age/research readout) or any other item, user's call.
- **`docs/IMPLEMENTATION_ROADMAP.md` item 30 (UI layout re-anchor — bottom bar)
  closed (2026-09-05).** Picked up per the user's explicit item request. Re-anchored
  `BuildMenu`, `SelectedUnitPanel`, and a slice of `ResourceHUD` into one shared
  bottom-docked bar (command panel / info panel / minimap, left to right), replacing
  the prior split (BuildMenu floating mid-right, resource ticker top-left, selection
  info bottom-left, minimap bottom-right). Two design decisions confirmed with the
  user via AskUserQuestion before touching anything: which `ResourceHUD` rows move
  down (Civilization + Population + Age relocate into the new bottom info panel;
  Wood/Food/Gold/Stone stay put as the top-left ticker), and whether to also redesign
  `BuildMenu`'s internal ~56-button vertical stack into a grid while re-anchoring it
  (no — flagged as a new, separate item 31 instead, per the user's own instruction not
  to bundle bigger work into this item's stated scope). None of the 3 scripts set
  their own root anchor in code (all Inspector/scene data, this project's established
  convention), so this was primarily a scene edit via UnityMCP: new `InfoPanel` root
  (bottom-center) holding `SelectedUnitPanel` (reparented, internal layout untouched)
  with a new `MatchStatus` child stacked above it holding the 3 relocated labels
  (reparented out of `ResourceHUD`, repacked to sequential rows); `ResourceHUD`
  shrunk to its remaining 4 rows and repacked; `BuildMenu` re-anchored from floating
  mid-right to bottom-left; `MinimapController` needed no change (already
  bottom-right, already correct). One small code addition:
  `ResourceHUD.matchStatusBackground` (new `[SerializeField] Image` field, wired in
  `Awake()` the same way the existing `background` field already is, reusing
  `panel_resource_bar` art — `MatchStatus` lives under a different root than
  `ResourceHUD` so needs its own background wiring). `SelectedUnitPanel.cs`/
  `BuildMenu.cs` got doc-comment updates only, no functional change. No new EditMode
  tests (pure layout change, matching this project's precedent for prior UI-wiring
  sessions); full suite confirmed 445/445 unchanged. Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), screenshotted the live HUD confirming the
  exact left-to-right order with no overlap, a real selected TownCenter showed
  `SelectedUnitPanel`'s name/status/HP bar correctly stacked below `MatchStatus` with
  no clipping, `ResourceHUD.Update()` correctly live-updated Food (0→150, top-left)
  and Population (4→5, inside `MatchStatus`), and the real `WorkerButton`'s own
  `onClick.Invoke()` at its new bottom-left position correctly routed through
  `CommandBus`'s lockstep queue (stockpile unchanged immediately, deducted exactly 50
  Food ~2s later) — hit-testing unaffected by the re-anchor. New item 31 added to
  `docs/IMPLEMENTATION_ROADMAP.md` (BuildMenu command-panel grid redesign), flagged
  rather than implemented this session as item 31 (bumping the old items 31-39 to
  32-40 to make room; one stale internal cross-reference, item 38's own "Depends on"
  note, fixed to match). Next: item 31 (BuildMenu grid redesign, flagged this
  session) or item 32 (Age/research readout, explicitly designed to bundle with item
  30), user's call.
- **Wave 4 item 25 (Fire Ship, 3-tier naval anti-ship specialist) fully closed
  (2026-09-05) — code, tests, scene wiring, and live verification all done.**
  Picked up per the user's "start wave 4 item 25" request, right after item 23
  (Scorpion) closed. First real consumer of `DamageType.Fire` (declared Wave 0
  item 4, unused by any live attacker until now — `Attackable.TakeDamage`
  already resolved Fire against pierceArmor since that item). New
  `UnitClass.FireShip` (a genuinely new class, not folded into `Naval` — needs
  an asymmetric matchup: FireShip→Naval 2x hard counter reusing the project's
  repeated "hard counter vs one class" precedent, Naval→FireShip 1.5x reusing
  Cavalry→Scorpion's "counter-specialist vulnerable to what it counters"
  precedent). New `Progression/FireShipLineProgress.cs` mirrors
  `NavalLineProgress.cs`'s exact 3-tier Classical/Durg/Imperial shape (Agni
  Nauka → Maha Agni Nauka → Vega Agni Nauka), reusing its own growth values at
  matching gates. Research lives on Dock as a fully independent second track
  alongside `NavalLineProgress`'s own (`Dock` now ticks two separate
  research timers in the same `Update()`). New `Combat/FireShipFactory.cs`
  mirrors `WarGalleyFactory.cs`'s shape (`BoatModelFactory`/`WaterMover`/
  `BoatAttacker`, not `MeleeAttacker` — a boat, not a land unit);
  `BoatAttacker` gained `SetDamageType`/`SetUnitClass` setters that
  WarGalleyFactory never needed (previously hardcoded to Pierce/Naval).
  `Dock.RequestTrainFireShip` is Wood-only (70 Wood, no Food/Gold — reuses
  Scorpion's own "crafted vessel, not a fed crew" cost-model precedent). No
  dedicated Fire Ship model exists yet — reuses the same "CombatShip" hull
  War Galley uses, given a deliberate fire-orange tint in place of the civ's
  own color so it at least reads as visually distinct from a same-civ War
  Galley despite the shared mesh — **flagging directly per the
  flag-asset-needs convention: a partial/cheap differentiator, not a
  substitute for real art.** Full `NetTrainKind.FireShip`/`CommandSerializer`
  wiring, new `fireShipButton`/`fireShipTierButton`/`fireShipTierLabel` in
  `BuildMenu.cs`, hotkeys Y/Z (unused within the Dock context specifically),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `DockGroup`. New
  `unit_roster_template.csv` "fire_ship" row. 16 new EditMode tests
  (`FireShipLineTests.cs` mirroring `NavalLineTests.cs`, plus 1 in
  `TrainingAndTradeTests.cs`; 445 total, all pass). **Found and fixed a real,
  serious pre-existing regression while live-verifying, not caused by this
  session's own changes**: `scorpionButton`/`scorpionTierButton`/
  `scorpionTierLabel` were still `null` in the scene (item 23's own session
  flagged this gap; it was never closed by a follow-up, unlike Camel Rider's
  equivalent gap). Because `BuildMenu.Awake()` wires every command-card
  button's `onClick.AddListener` in one long sequential block and
  `scorpionButton`'s call sat partway through it, the resulting
  `NullReferenceException` silently aborted the *rest* of `Awake()` —
  meaning **every button wired after it, project-wide, in every match** never
  got a runtime click listener at all: `uniqueUnitButton`, `ungarrisonButton`,
  `fishingBoatButton`, `warGalleyButton`, `navalTierButton`, all 6 Market
  trade buttons, `attackUpgradeButton`/`armorUpgradeButton`,
  `uniqueTechButton`, every tier-research button from `infantryTierButton`
  onward, and this item's own new `fireShipButton`/`fireShipTierButton`.
  Caught directly (not assumed) by reflecting into
  `UnityEventBase.m_Calls.m_RuntimeCalls` after a real button click produced
  zero effect, confirming 0 listeners rather than a selection mismatch.
  Fixed the same way every prior session's version of this gotcha was fixed —
  duplicated `CamelRiderButton`/`CamelRiderTierButton` (+ label child) into
  real `ScorpionButton`/`ScorpionTierButton`/`ScorpionTierLabel` scene
  objects and wired them onto `BuildMenu`'s previously-null fields — then
  re-verified the listener count went 0→1 and the full click chain started
  working project-wide. Live-verified via UnityMCP through the real
  production path, after that fix: a real match
  (`CivilizationSetup.BeginMatch(Rajput)`), a real Dock, a real hostile War
  Galley — a live Fire Ship hit resolved for exactly 28 damage (14 base × 2x
  `CombatBonus`) and the reverse hit for exactly 12 damage (8 base × 1.5x),
  both `CombatBonus` directions confirmed live; the real `FireShipButton`'s
  own `onClick.Invoke()` left the stockpile unchanged immediately (confirming
  the `CommandBus` lockstep queue) and deducted exactly 70 Wood about a
  second later, spawning a real "Rajput Agni Nauka"; the real
  `FireShipTierButton`'s own `onClick.Invoke()` deducted exactly 120 Gold/60
  Wood and started research, confirmed independent of that same Dock's Naval
  tier track. Full EditMode suite re-run after the Scorpion fix: still
  445/445. Next: Wave 4 item 24 (Trebuchet, 1 tier) or any other Wave 4 item,
  user's call.
- **Wave 4 item 23 (Scorpion, 2-tier anti-infantry siege weapon) closed
  (2026-09-05) — code and tests only, Unity-side steps blocked this
  session, see below.** Picked up per the user's "start item 23 wave 4"
  request, right after item 22 (Camel Rider) closed. No design decision
  needed — the roadmap already fixed tier names/ages and flagged the one
  real technical gap directly: "`DamageType.Pierce` already exists — this
  needs a raycast-through code path for pass-through damage, not a new
  mechanic type." New `MeleeAttacker.SetPierceThrough(depth)`: a hit
  continues past the primary target in the same straight line for `depth`
  further, hitting anything else hostile standing in that narrow line
  (`PierceLineHalfWidth` 0.6) at FULL damage — deliberately not reduced
  like `SetSplashRadius`'s own multiplier, since a piercing bolt doesn't
  lose force the way a fragmentation blast does. Implemented as a pure
  line-segment distance test over the same `Unit.All`/`Building.All`
  registries `ResolveSplash` already scans (`ResolveHit`/`CombatBonus`
  reused unchanged per pierced victim) rather than a real `Physics`
  raycast — this project's combat resolution has never used PhysX for hit
  detection (splash doesn't either), and a deterministic math test keeps
  it that way for lockstep. New `UnitClass.Scorpion` (a genuinely new
  class, not folded into `Siege` — Siege's own identity is anti-BUILDING
  via its 3x `CombatBonus`, Scorpion's is anti-INFANTRY, a different
  target entirely) with 2 new `CombatBonus` pairings: Scorpion→Infantry
  2x (reusing the closest existing "hard counter vs one class" precedent
  value, same as Archer→Cavalry/Skirmisher→Archer/Camel→Cavalry) and
  Cavalry→Scorpion 1.5x (reusing Cavalry→Infantry's own value directly —
  a fast unit closes the gap on this unarmored, slow-moving engine before
  it fires twice, the same vulnerability real AoE Scorpions have to
  cavalry raids). New `Progression/ScorpionLineProgress.cs` mirrors
  `CamelRiderLineProgress.cs`'s 2-tier Durg/Imperial shape exactly (Bana
  Yantra → Maha Bana Yantra); tier 1 reuses every other line's own
  established Imperial-gate growth (+30 HP/+6 dmg/200 Gold/100
  Wood/40s). New `Combat/ScorpionFactory.cs` mirrors `SiegeFactory.cs`'s
  shape (slow, no `GarrisonSeeker` — siege units don't garrison, same
  exclusion `Siege` itself already has) but with `DamageType.Pierce` and
  Archer-style range/cost instead — a fresh Wood+Gold-only cost model (no
  Food), since a craft-built machine doesn't need feeding; calls
  `SetPierceThrough(3f)`, chosen so the bolt reaches a second rank
  standing directly behind the primary target in a default Line
  formation (`FormationDefinition`'s default `unitSpacing` 1.5) without
  reaching a third. No dedicated siege-engine model exists yet ("machine
  only, no crew" per `docs/YOUR_ACTION_ITEMS.md` item 23) — reuses the
  same Male Human Character Dummy body plus the Bow prop like Archer, no
  wheeled/tripod mesh — **flagging directly per the flag-asset-needs
  convention: a Scorpion currently looks identical to an Archer in the
  field.** New `Barracks.RequestTrainScorpion`/`RequestResearchScorpionTier`
  (independent research track alongside every other Barracks tier line),
  new `scorpionButton`/`scorpionTierButton`/`scorpionTierLabel` in
  `BuildMenu.cs`, hotkeys W/X (both unused within the Barracks context
  specifically — already claimed elsewhere, TrainWarGalley/
  ResearchNavalTier on Dock, a mutually-exclusive selection context, this
  file's own established convention), wired into
  `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.Scorpion`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "scorpion" row (0 Food/100 Wood/60 Gold/35
  HP/12 dmg/Pierce/6 range/1.6 speed). 19 new EditMode tests
  (`ScorpionLineTests.cs` mirroring `CamelRiderLineTests.cs`, plus a
  dedicated `ScorpionPierceThroughTests.cs` mirroring `SiegeSplashTests.cs`'s
  own split between line-progress coverage and combat-mechanic coverage —
  hits-behind-target/misses-past-depth/misses-off-to-side/misses-in-front/
  no-friendly-fire/Building-CombatBonus/full-not-reduced-damage/
  zero-depth-disables-feature). **Found and fixed one real adjacent bug
  while wiring `scorpionButton`'s own visibility**: `cavalryArcherButton`/
  `camelRiderButton` and their own tier buttons were never added to
  `Update()`'s `SetActive(barracks != null)` visibility block by their own
  sessions (only `.interactable` was gated) — so either button could stay
  visible with no Barracks selected at all. Fixed alongside this item's
  own wiring, in the same file, rather than left in place. **Could not run
  `BharatRTS/Generate Data Assets From CSV`, run the EditMode suite, wire
  the new scene buttons, or live-verify via UnityMCP this session**: both
  the `unity`/`UnityMCP` MCP servers failed to connect all session
  (`ConnectionRefused`) — same blocker item 22's own first session hit.
  **Flagging directly, not glossing over it**: the new `scorpion` CSV row
  has not yet been baked into a generated `UnitDefinition` asset (so
  `ScorpionFactory` will log its fallback-stats warning and use the
  hardcoded fallback values until regenerated), the new
  `scorpionButton`/`scorpionTierButton`/`scorpionTierLabel`
  `[SerializeField]` fields are almost certainly null in the scene right
  now (the exact recurring gotcha every Wave 2/3/4 session has hit — needs
  duplicating `CamelRiderButton`/`CamelRiderTierButton` into real scene
  objects), and the full EditMode suite has not been re-run to confirm a
  new total (411 + 19 = 430 expected). Next: run `BharatRTS/Generate Data
  Assets From CSV`, wire the 3 new scene fields, run the EditMode suite,
  and live-verify via UnityMCP through the real production path once
  Unity/UnityMCP is reachable again — then Wave 4 item 24 (Trebuchet, 1
  tier) or any other Wave 4 item, user's call.
- **Wave 4 item 22 (Camel Rider, 2-tier mounted anti-cavalry specialist)
  fully closed (2026-09-05) — code, tests, scene wiring, and live
  verification all done.** A follow-up session picked up exactly where the
  prior one left off (Unity/UnityMCP unreachable that whole session, so
  none of its 4 flagged outstanding steps could be done) and closed all 4:
  ran `BharatRTS/Generate Data Assets From CSV` (camel_rider `UnitDefinition`
  now baked, confirmed present under `Resources/Data/Generated`); wired
  `camelRiderButton`/`camelRiderTierButton`/`camelRiderTierLabel` onto real
  scene objects (duplicated `CavalryArcherButton`/`CavalryArcherTierButton`
  via UnityMCP, same recurring gotcha every Wave 2/3/4 session has hit);
  full EditMode suite now 411/411 (399 + 12 new `CamelRiderLineTests.cs`),
  matching the exact predicted count; live-verified via UnityMCP through the
  real production path — a real match (`CivilizationSetup.BeginMatch(Maurya)`),
  a real Barracks, `RequestTrainCamelRider()` deducted exactly 35 Food/15
  Wood and spawned a real "Maurya Ushtrarohi" (`Attackable.Class == Camel`,
  HP 44, real `MeleeAttacker`/`GarrisonSeeker` present), `CombatBonus`
  pairings (Camel→Cavalry 2x, Infantry→Camel 1.25x) confirmed live,
  `RequestResearchCamelRiderTier()` correctly age-gated (refused at
  Classical, deducted 200 Gold/100 Wood at Imperial) and not retroactive
  (already-spawned units stayed at 44 HP while a new one came out "Maurya
  Maha Ushtrarohi" at 84 HP), and the real `CamelRiderButton`'s own
  `onClick.Invoke()` confirmed routing through `CommandBus`'s lockstep queue
  (stockpile unchanged immediately, deducted ~2.5s later). See
  `docs/SESSION_LOG.md`'s matching 2026-09-05 entry for full detail. Next:
  see the newer Wave 4 item 23 (Scorpion) bullet above for what followed
  this item.
  Original design-decision context, preserved below: item 22 was flagged in
  the roadmap as "design decision first" — whether it ships at all. Asked
  the user directly via
  AskUserQuestion before writing any code — confirmed "build it." A second
  design call was then made explicitly (the roadmap fixes tier
  names/ages, not what class this counts as for combat purposes): new
  `UnitClass.Camel`, a genuinely new class rather than folded into
  Spearman or Cavalry, since it needs both traits at once — Spearman's
  hard-counter-vs-Cavalry combat role plus Cavalry's own mounted move
  speed. New `CombatBonus` pairings (Camel→Cavalry 2x, Infantry→Camel
  1.25x) reuse Spearman's own pairing values directly — the closest
  existing precedent for "a unit built to counter Cavalry," not
  independently balanced. New `Progression/CamelRiderLineProgress.cs`
  mirrors `CavalryArcherLineProgress.cs`'s 2-tier Durg/Imperial shape
  exactly (Ushtrarohi → Maha Ushtrarohi); tier 1 reuses every other
  line's own established Imperial-gate growth (+30 HP/+6 dmg/200
  Gold/100 Wood/40s). New `Combat/CamelRiderFactory.cs` combines
  `CavalryFactory`'s mount/speed/melee setup with `SpearmanFactory`'s
  Food+Wood-only cost model and Spear weapon prop — reuses the same Male
  Human Character Dummy body plus both the Spear (RightHand) and Horse
  (AttachBeside) props, no dedicated camel mount exists yet —
  **flagging directly per the flag-asset-needs convention: a Camel
  Rider currently looks identical to a Cavalry/Spearman hybrid using
  existing props, no distinct silhouette.** New
  `Barracks.RequestTrainCamelRider`/`RequestResearchCamelRiderTier`
  (independent research track alongside every other Barracks tier
  line), new `camelRiderButton`/`camelRiderTierButton`/
  `camelRiderTierLabel` in `BuildMenu.cs`, hotkeys U/B (both otherwise
  unused within the Barracks context specifically — U is bound to
  Ungarrison/ResearchAttack in the mutually-exclusive GarrisonPoint/
  Karmashala contexts, B to TrainDockUnit on Dock — this file's own
  established convention), wired into `SettingsMenu`/`HotkeyOverlay`'s
  `BarracksGroup`. Full `NetTrainKind.CamelRider`/`CommandSerializer`
  wiring. Added a `unit_roster_template.csv` "camel_rider" row (35
  Food/15 Wood/40 HP/6 dmg/Melee/6.5 speed). 12 new EditMode tests
  (`CamelRiderLineTests.cs`, mirroring `CavalryArcherLineTests.cs`). All 4
  outstanding steps from that session (data asset generation, scene wiring,
  EditMode suite, live verification) were closed in the follow-up session
  described at the top of this bullet.
- **Wave 4 item 21 (Cavalry Archer, 2-tier mobile ranged raider) closed
  (2026-09-05).** Picked up after being offered a choice between this and item
  22 (Camel Rider, which needs a ship-or-not design decision first) — user
  picked Cavalry Archer as the no-open-questions option, right after item 20
  (Battering Ram) closed. Design call made explicitly (the roadmap fixes tier
  names/ages/count, not what class this counts as for combat purposes):
  `CavalryArcherFactory` classifies as `UnitClass.Archer`, not a new class — a
  mounted Archer, not a new counter archetype, keeping it inside the existing
  counter web for free (`CombatBonus`): it still hard-counters Cavalry
  (Archer→Cavalry 2x) and is still hard-countered by Skirmisher
  (Skirmisher→Archer 2x), taking Infantry→Archer's 1.5x penalty too, exactly
  like a foot Archer — only its move speed (borrowed from Cavalry's own
  value, 6.0) and cost differ. Deliberately does NOT read
  `CivilizationProfile.FindCategoryMultiplier` with `UnitClass.Cavalry` (the
  Maratha cavalry-speed/Rajput cavalry-damage civ bonuses) — those are scoped
  to units whose actual combat class is Cavalry, and this one's is Archer, so
  it correctly falls outside them; it just happens to ride a horse. New
  `Progression/CavalryArcherLineProgress.cs` mirrors
  `SkirmisherLineProgress.cs`'s 2-tier shape exactly, but gated at
  Durg/Imperial (not Classical/Durg) per the roadmap's own item text — tier
  0's Durg `RequiredAge` is descriptive only, never enforced
  (`RequestTrainCavalryArcher` has no age gate of its own, same convention
  `CavalryLineProgress`/`ArcherLineProgress` already established). Tier 1
  (Maha Ashva Dhanurdhara) reuses every other line's own established
  Imperial-gate growth exactly (+30 HP/+6 dmg/200 Gold/100 Wood/40s), not
  independently balanced. New `Combat/CavalryArcherFactory.cs` combines
  `ArcherFactory`'s ranged-attack setup with `CavalryFactory`'s mount/speed
  setup — reuses the same Male Human Character Dummy body plus both the Bow
  (`LeftHand`) and Horse (`AttachBeside`) props, no dedicated mounted-archer
  model exists yet — **flagging directly per the flag-asset-needs convention:
  a Cavalry Archer currently looks identical to a mounted Archer/Cavalry
  hybrid using existing props, no distinct silhouette.** New
  `Barracks.RequestTrainCavalryArcher`/`RequestResearchCavalryArcherTier`
  (independent research track alongside every other Barracks tier line), new
  `cavalryArcherButton`/`cavalryArcherTierButton`/`cavalryArcherTierLabel` in
  `BuildMenu.cs`, hotkeys K/P (unused within the Barracks context
  specifically — both already reused across mutually-exclusive
  TownCenter/Karmashala contexts, this file's own established convention),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.CavalryArcher`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "cavalry_archer" row (60 Food/40 Gold/30 HP/5
  dmg/Pierce/6 range/6.0 speed) and regenerated data assets via
  `BharatRTS/Generate Data Assets From CSV`. 12 new EditMode tests
  (`CavalryArcherLineTests.cs`, 399 total, all pass). Hit the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3/4 session
  has hit (duplicated `BatteringRamButton`/`BatteringRamTierButton` into real
  `CavalryArcherButton`/`CavalryArcherTierButton` scene objects via
  UnityMCP). Live-verified via UnityMCP through the real production path: a
  real match (`CivilizationSetup.BeginMatch(Rajput)`), a real
  `BarracksFactory.Place` Barracks — `RequestTrainCavalryArcher()` correctly
  trained even at Ancient Age (confirming tier 0's Durg `RequiredAge` is
  descriptive only, same as Cavalry's own base) and deducted exactly 60
  Food/40 Gold; a forced tick spawned a real "Rajput Ashva Dhanurdhara"
  (`Attackable.Class == Archer`, HP 34.5, move speed 6, a real
  `GarrisonSeeker` and `VisionSource` both present);
  `RequestResearchCavalryArcherTier()` correctly refused at Durg with zero
  deduction, then deducted exactly 200 Gold/100 Wood at Imperial; after a
  forced tick completed it, the two already-spawned Ashva Dhanurdhara units
  stayed at 34.5 HP while a new one trained afterward came out "Rajput Maha
  Ashva Dhanurdhara" at 82.8 HP — not retroactive, confirmed live. Then
  through the real scene UI path specifically: the real `CavalryArcherButton`'s
  own `onClick.Invoke()` left the stockpile unchanged immediately (confirming
  it goes through `CommandBus`'s lockstep queue, not a synchronous
  deduction) and deducted the exact cost ~2 real seconds later; the real
  `CavalryArcherTierButton`'s label correctly read "Cavalry Archer (Max
  Tier)" once that faction's tier was already maxed from the earlier test.
  No AI-side training hook, same explicitly-out-of-scope call as every other
  Wave 3/4 item. Next: Wave 4 item 22 (Camel Rider — needs a design-decision
  Plan Mode session first per its own roadmap text) or any other Wave 4
  item, user's call — all are parallel-safe once Wave 0/1 are done.
- **Wave 4 item 20 (Battering Ram, 3-tier anti-building specialist) closed
  (2026-09-05).** Picked up per the user's "start wave 4 item 20" request,
  right after item 19 (Skirmisher) closed. This project already had one
  anti-building specialist (Siege/Mangonel, `CombatBonus.Multiplier(Siege,
  Building) = 3x`), but it can still fight units (unremarkably, flat 1x)
  and has splash — item 20 specs a second, genuinely distinct siege unit:
  anti-building ONLY (structurally unable to target units, not just weak
  against them), no splash, and garrisonable. New
  `MeleeAttacker.SetBuildingOnly(bool)` reads "anti-building only"
  literally — when set, `AttackMove` flatly refuses any target whose
  `Attackable.Class` isn't `Building` (every other `MeleeAttacker` user
  unaffected, defaults false), matching AoE II's own ram (can't even be
  given an attack-move onto a unit). New `Progression/
  BatteringRamLineProgress.cs` mirrors `ArcherLineProgress.cs`'s 3-tier
  Classical/Durg/Imperial shape exactly (Dwarabhanjaka → Maha
  Dwarabhanjaka → Vajra Dwarabhanjaka), reusing Archer's own growth curve
  at matching gates, not independently balanced. New `CombatBonus.
  Multiplier(BatteringRam, Building) = 4x` — steeper than Siege's own 3x
  since a Ram's entire kit is "hit buildings." New `Combat/
  BatteringRamFactory.cs`: `SetBuildingOnly(true)`, never calls
  `SetSplashRadius` (no splash), and — re-reading `GarrisonPoint`/
  `GarrisonSeeker` (the General Garrisoning system from Wave 1) before
  coding — resolves "garrisonable" as "hosts friendly units for
  protection," not "enters a building": adds a `GarrisonPoint` to itself
  (capacity 4, matching Tower's), which generalizes to a non-Building host
  with **zero changes needed** to either existing class
  (`GarrisonSeeker.ComputeApproachPoint` already falls back to the
  target's raw `transform.position` with no `BuildingFootprintTag`, and
  `GarrisonPoint.OnDestroy` already ungarrisons everyone on death).
  Deliberately no `StanceController` (an Aggressive/Defensive auto-engage
  scan would "target" nearby units it can structurally never hit — worse
  than requiring an explicit order, matching real AoE II). New
  `Barracks.RequestTrainBatteringRam`/`RequestResearchBatteringRamTier`
  (independent research track alongside every other Barracks tier line),
  new `batteringRamButton`/`batteringRamTierButton`/`batteringRamTierLabel`
  in `BuildMenu.cs`, hotkeys D/R (unused within the Barracks context
  specifically — both already reused across the mutually-exclusive
  TownCenter/Durg contexts, this file's own established convention), wired
  into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.BatteringRam`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "battering_ram" row (60 Food/120 Wood/80
  HP/18 dmg/Melee/3 melee armor/1.3 speed/1.5 range) and regenerated data
  assets via `BharatRTS/Generate Data Assets From CSV`. 16 new EditMode
  tests (`BatteringRamLineTests.cs`, 387 total, all pass). No dedicated
  Battering Ram model exists yet — reuses the same Human Character Dummy
  body + Kanabo weapon as `SiegeFactory` (whose own comment already
  flagged this exact prop as a better long-term fit for a real battering
  ram) — **flagging directly per the flag-asset-needs convention: a
  Battering Ram currently looks identical to a Siege unit in the field.**
  Hit the same recurring "console bridge shows zero errors while new code
  fails to compile" gotcha every session eventually hits (the new test
  file was missing `using KingdomsOfBharat.Units;`, silently leaving the
  suite at the old 371-test count with `read_console` showing nothing —
  only `~/Library/Logs/Unity/Editor.log` directly showed the real
  `CS0246`/`CS0103` errors); fixed, then 387/387 passed. Hit the same
  recurring "new `[SerializeField]` null in the scene" gotcha every Wave
  2/3/4 session has hit (duplicated `SkirmisherButton`/
  `SkirmisherTierButton` into real `BatteringRamButton`/
  `BatteringRamTierButton` scene objects via UnityMCP). Live-verified via
  UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real `BarracksFactory.Place`
  Barracks — directly via the component API, `RequestTrainBatteringRam()`
  deducted exactly 60 Food/120 Wood and spawned a real "Maurya
  Dwarabhanjaka" (`Attackable.Class == BatteringRam`, HP 88, a real
  `GarrisonPoint` with `Capacity == 4`); a real `MeleeAttacker.AttackMove`
  call against a real hostile Soldier was refused (`IsAttacking` stayed
  false) while the identical call against a real hostile Tower was
  accepted (`IsAttacking` became true) — the anti-building-only trait
  confirmed live, not just in the isolated unit test; a real Worker
  garrisoned into and back out of the Ram correctly. Then through the real
  scene UI path specifically: the real `BatteringRamButton`'s own
  `onClick.Invoke()` deducted the exact cost and spawned a real Battering
  Ram through `CommandBus`'s lockstep queue (confirmed the delayed, not
  synchronous, execution directly); the real `BatteringRamTierButton`'s
  label correctly read "Upgrade to Maha Dwarabhanjaka (120 Gold, 60 Wood)"
  at Durg and its own `onClick.Invoke()` deducted exactly that. No
  AI-side training hook, same explicitly-out-of-scope call as every other
  Wave 3/4 item. Next: any other Wave 4 item (Cavalry Archer, Scorpion,
  Trebuchet, Fire Ship, or the design-decision items — Camel Rider,
  Trader), user's call — all are parallel-safe once Wave 0/1 are done.
- **Wave 4 item 19 (Skirmisher, anti-archer counter-archer, 2 tiers) closed
  (2026-09-05).** Picked up per the user's "start wave 4 item 19" request,
  right after item 18 (Scout) closed as the first Wave 4 item. Completes
  the counter web's last gap: this project's `CombatBonus` already had
  Infantry→Archer (1.5x), Archer→Cavalry (2x), Cavalry→Infantry (1.5x),
  and Spearman's own Cavalry/Infantry pair, but nothing hard-countered
  Archer's own counter-pick — Skirmisher is the dedicated anti-archer
  specialist that closes it. New `UnitClass.Skirmisher` plus two new
  `CombatBonus` pairings (Skirmisher→Archer 2x, Infantry→Skirmisher
  1.25x) — both values reuse Spearman's own pairing exactly (the closest
  existing precedent for "a unit built to counter one other class"), not
  independently balanced. New `Progression/SkirmisherLineProgress.cs` —
  only 2 tiers total (Pratirodhi Dhanurdhara at Classical, Maha
  Pratirodhi Dhanurdhara at Durg), not 3 like every Wave 3 line, matching
  this item's own roadmap spec; tier 1's bonus/cost reuses
  `ArcherLineProgress`'s own Durg-gate growth exactly (+18 HP/+4 dmg/120
  Gold/60 Wood/25s). New `Combat/SkirmisherFactory.cs` mirrors
  `ArcherFactory.cs`'s shape almost exactly (ranged/Pierce attack,
  `UnitClass.Skirmisher` the actual differentiator) — reuses the shared
  bow model/animation since no dedicated Skirmisher model exists yet
  (`docs/YOUR_ACTION_ITEMS.md` item 19 specs a quilted-armor archer with
  a forearm buckler, not delivered) — **flagging directly per the
  flag-asset-needs convention: a Skirmisher currently looks identical to
  an Archer in the field.** New `Barracks.RequestTrainSkirmisher`/
  `RequestResearchSkirmisherTier` (independent research track alongside
  every other Barracks tier line), new `skirmisherButton`/
  `skirmisherTierButton`/`skirmisherTierLabel` in `BuildMenu.cs`, hotkeys
  C/G (Train Skirmisher = C, Upgrade Skirmisher Tier = V — the last two
  unused letters in `BuildMenu`'s Barracks-context hotkey map), wired
  into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.Skirmisher`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "skirmisher" row (35 Food/25 Gold/20 HP/3
  dmg/Pierce/5 range/3.8 speed) and regenerated data assets via
  `BharatRTS/Generate Data Assets From CSV`. 13 new EditMode tests
  (`SkirmisherLineTests.cs`, 371 total, all pass). Hit the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3/4
  session has hit (duplicated `CharaButton`/`CharaTierButton` into real
  `SkirmisherButton`/`SkirmisherTierButton` scene objects via UnityMCP).
  Live-verified via UnityMCP through the real production path: a real
  match (`CivilizationSetup.BeginMatch(Maurya)`), a real
  `BarracksFactory.Place`-spawned Barracks selected via
  `SelectionManager`, the real scene button's own `onClick.Invoke()`
  deducted exactly 35 Food/25 Gold and trained a real "Maurya Pratirodhi
  Dhanurdhara" (23 HP, `Attackable.Class == Skirmisher`); the real tier
  button correctly showed "Upgrade to Maha Pratirodhi Dhanurdhara (120
  Gold, 60 Wood)" and its own `onClick.Invoke()` deducted exactly that; a
  forced tick advanced the tier and the real button settled on
  "Skirmisher (Max Tier)"; a second Skirmisher trained afterward came out
  "Maurya Maha Pratirodhi Dhanurdhara" at 43.7 HP while the first,
  already-spawned Skirmisher stayed at 23 HP — not retroactive, confirmed
  live; `CombatBonus.Multiplier(Skirmisher, Archer)` and
  `CombatBonus.Multiplier(Infantry, Skirmisher)` both confirmed live at
  2x/1.25x. No AI-side training hook, same explicitly-out-of-scope call
  as every other Wave 3/4 item. Next: Wave 4 item 20 (Battering Ram, 3
  tiers) or any other Wave 4 item, user's call — all are parallel-safe
  once Wave 0/1 are done.
- **Wave 3 item 17 (Blacksmith-style stat upgrade steps, Karmashala) closed
  (2026-09-05).** Picked up per the user's explicit "start now" request right
  after item 16 closed Wave 3's own numbered list (item 17 doesn't block
  Wave 3's exit criteria — it's the last remaining item in that wave's own
  numbering, now also done). Investigated first, per protocol, rather than
  assuming the roadmap text was still accurate: `UpgradeProgress.MaxTier`
  was already 3 (set by a prior session), but **none of the 3 tiers were
  age-gated at all** — a Karmashala only needs Classical Age to exist, and
  once it did, all 3 tiers were researchable back-to-back in the same Age,
  purely gold-limited. New `UpgradeProgress.TierRequiredAges`
  (`{Classical, Durg, Imperial}`) plus `NextAttackTierRequiredAge`/
  `NextArmorTierRequiredAge`/`NextAttackTierAgeRequirementMet`/
  `NextArmorTierAgeRequirementMet` — same array-indexed-by-current-tier +
  ordinal `AgeId` comparison shape every other tier line's own
  `NextTierAgeRequirementMet` already uses. `Karmashala.RequestResearchAttack`/
  `RequestResearchArmor` both gained the age check; `BuildMenu.cs`'s shared
  `UpdateUpgradeButton` (used by both Attack/Armor tracks) gained the same
  "Upgrade to X (needs Y Age)" branch every other tier button already has.
  **Found and fixed one real edge-case bug before it shipped**: the new
  `NextAttackTierRequiredAge`/`NextArmorTierRequiredAge` calls are evaluated
  unconditionally at `BuildMenu`'s call site (C# evaluates all arguments
  before a method call — `UpdateUpgradeButton`'s own `!hasNextTier`
  early-return happens too late to matter) — at max tier this would index
  a 3-element array at position 3 and throw `IndexOutOfRangeException` every
  single frame once any track maxed out; fixed by clamping the index, with
  a dedicated regression test. 7 new/updated EditMode tests in
  `KarmashalaTests.cs` — the 6 pre-existing tests needed `AgeProgress.Initialize`
  added, since they'd implicitly relied on the previously-ungated behavior
  (358 total, all pass). Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maurya)`), a
  real `KarmashalaFactory.Place` Karmashala correctly refused tier 1 at
  Ancient with zero deduction (1000 Gold on hand), started tier 1 at
  Classical (80 Gold), refused tier 2 at Classical with zero deduction,
  started tier 2 at Durg (160 Gold), the real scene button's own label
  correctly read "Upgrade Attack (needs Imperial Age)"/`interactable=false`
  at Durg for tier 3 and its real `onClick.Invoke()` correctly no-op'd, then
  at Imperial the same real button's `onClick.Invoke()` deducted exactly 240
  Gold and started tier 3, settling on "Attack (Max)"/`interactable=false`
  once complete. No scene-wiring gotcha this session (attackUpgradeButton/
  armorUpgradeButton were already wired from Wave 2 item 8 — only their
  underlying gating logic and label text changed). **This closes item 17,
  the last item in Wave 3's own numbered list** — everything Wave 3 and its
  own numbering set out to do is now done. Next: Wave 4 (new units — item 18
  already closed by a concurrent session, see below; Skirmisher/Battering
  Ram/Cavalry Archer/etc. remain, user's call), or any other Section 5
  priority.
- **Wave 3 item 16 (Unique-unit Elite tier, 2 tiers × 5 units) closed
  (2026-09-05) — this closes Wave 3.** Picked up right after item 15 per the
  user's "your call" hand-off. No new design decisions needed — item 16's own
  roadmap text already fixes scope (every unique unit except the two War
  Elephant factories, which item 13's own Gajaroha ladder already covers as
  their one elite step) and the mechanism (research on Durg, baked in at
  spawn, not retroactive) mirrors item 13's own "research where the unit
  trains" deviation exactly. New `Progression/UniqueUnitEliteProgress.cs`
  keyed by unitId (not CivilizationId, since these 5 units — Chola Naval
  Raider, Rajput Royal Guard, Maurya's Pillar Edict Scholar, Maratha's Mavla
  Raider and Durg Garrison — are genuinely civ-exclusive, unlike every other
  Wave 3 line's shared table); single Imperial-gated step reusing the same
  +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other line's own first
  Imperial-gate step already uses, not independently balanced. New
  `Durg.RequestResearchEliteTier(slot)`/`TrainsEliteEligible(slot)`/
  `IsResearchingEliteTier(slot)`/`EliteTierResearchProgress(slot)` — two
  independent per-slot tracks (Maurya/Maratha each have 2 unique-unit slots)
  gated by unitId eligibility, not slot index — Maurya's slot 0 (War
  Elephant) is NOT eligible, only slot 1 (Pillar Edict Scholar) is; Maratha's
  both slots are. All 5 factories now read `UniqueUnitEliteProgress` at spawn
  for name + HP/damage bonus (baked in, not retroactive, matching every
  other tier line). New `eliteTierButton`/`eliteTierButton2` in
  `BuildMenu.cs` (hotkeys F/G, reused from other mutually-exclusive contexts
  per this file's own established convention — safe since Durg is never
  selected at the same time as the Barracks/TownCenter contexts those
  letters are also bound in, including item 18's own new Chara F/G below),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `DurgGroup`. 16 new EditMode
  tests (`UniqueUnitEliteTests.cs`, 351 total, all pass). **Hit a real
  concurrent-session file collision mid-session**: `BuildMenu.cs` was being
  actively edited by a separate session building Wave 4 item 18 (Scout,
  below) — per this project's own documented "single-session discipline"
  gotcha, stopped and asked the user before touching that file further,
  reverted the one edit already made, and waited until the other session's
  changes stabilized (`Barracks.cs`/`SettingsMenu.cs`/`HotkeyOverlay.cs`
  were also concurrently dirty) before resuming — no work was lost or
  clobbered on either side. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maratha)`), a
  real `DurgFactory.Place`-spawned Durg confirmed both slots elite-eligible,
  the age gate correctly deducted exactly 200 Gold/100 Wood per slot at
  Imperial for both tracks running concurrently, forced real ticks completed
  both and a Mavla Raider/Durg Garrison trained afterward through the real
  factory paths came out "Maratha Maha Mavla Raider"/"Maratha Maha Durg
  Garrison" at correctly boosted HP, the real scene buttons correctly showed
  "Elite (Max Tier)"/`interactable=false` once maxed; a second real Durg
  (Maurya) confirmed slot 0 (War Elephant) ineligible/slot 1 (Pillar Edict
  Scholar) eligible exactly as designed; a third real Durg (Rajput) confirmed
  the real scene button's own `onClick.Invoke()` deducted the exact cost and
  started research for its single eligible slot, with the 2nd-slot button
  correctly hidden. **This closes Wave 3** — every currently-flat unit type
  now has a real tier ladder. Next: Wave 4 (item 18 already closed by a
  concurrent session, below; otherwise Skirmisher, Battering Ram, Cavalry
  Archer, etc., user's call), or item 17 (Blacksmith-style stat upgrade
  steps on Karmashala, still open within Wave 3's own numbering but not
  blocking Wave 3's exit criteria).
- **Wave 4 item 18 (Scout/Chara, 3-tier line) closed (2026-09-05) — first Wave 4
  item.** Picked up per the user's "start wave 4 item 18" request, since it depends
  only on Wave 0/1 (both closed), not Wave 2/3 — the roadmap's own item text also
  recommends doing this one first in Wave 4 (highest player-facing value). Unlike
  every Wave 3 item, this is a wholly new unit, not a tier upgrade to an existing
  one — Spearman's addition (Phase 2) was the closest precedent, mirrored directly.
  Two design calls made explicitly (the roadmap fixes tier names/ages, not what
  each tier improves): tier names literally translate "Vega" = speed, so
  `Progression/ScoutLineProgress.cs` grows vision radius and move speed per tier
  (`VisionBonus`/`SpeedBonus` fields replacing every other line's `DamageBonus`)
  rather than combat stats — Chara/Vega Ashvarohi/Maha Vega Ashvarohi at
  Ancient/Classical/Durg, tier 1/2 costs reusing InfantryLineProgress's own
  Padati→Senani/Senani→Khandayata values at the matching Classical/Durg gates; and
  `Combat/ScoutFactory.cs` deliberately does NOT opt into
  `EnableUpgradeArmorScaling`/`EnableUpgradeDamageScaling` or add a
  `StanceController`, mirroring `WorkerFactory`'s own "utility unit, not a combat
  unit" choice rather than the 13 combat factories that do opt in. Uses
  `UnitClass.Support` for `Attackable`/`MeleeAttacker` classification — the first
  live unit to do so for real combat resolution (Worker uses `Infantry` there,
  `Support` only for its own move-speed multiplier lookup), safe since
  `CombatBonus`/`CounterMatrix` have zero Support entries. `VisionSource` base
  radius 12 (vs. every other unit's 8) plus the tier's own bonus (14/15 at tiers
  1/2) — the actual point of the unit, needing zero `FogOfWarManager` changes
  since vision radius has always been per-instance. New independent research
  track on `Barracks.cs` (`RequestResearchCharaTier`, alongside the existing
  Infantry/Spearman/Archer/Cavalry/Siege tracks) plus `RequestTrainChara` (the
  second Barracks unit read from `DataRegistry` rather than fixed Inspector
  fields, same as Spearman). New `charaButton`/`charaTierButton`/`charaTierLabel`
  in `BuildMenu.cs`, hotkeys F/G, wired into `SettingsMenu`/`HotkeyOverlay`'s
  `BarracksGroup`. Full `NetTrainKind.Chara`/`CommandSerializer` wiring (every
  other trainable unit has this; skipping it would silently no-op the train
  command for a LAN peer). No dedicated Scout-horse model exists yet
  (`docs/YOUR_ACTION_ITEMS.md` item 18 specs one, not delivered) — reuses
  `CavalryFactory`'s own "Mounts/Horse/scene" placeholder, same
  primitive-until-a-real-pack-lands convention as everywhere else; **flagging
  directly per the flag-asset-needs convention: Scout currently looks identical
  to Cavalry in the field and needs a real, visually distinct mount eventually.**
  No AI-side training hook, same explicitly-out-of-scope call as every Wave 3
  item. 12 new EditMode tests (`ScoutLineTests.cs` + 1 in
  `TrainingAndTradeTests.cs`, 335 total, all pass). Hit the same recurring "new
  `[SerializeField]` null in the scene" gotcha every Wave 2/3/4 session has hit
  (duplicated `SpearmanButton`/`SiegeTierButton` into real `CharaButton`/
  `CharaTierButton` scene objects via UnityMCP). Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real Barracks, the real scene
  button's `onClick.Invoke()` deducted exactly 50 Food and trained a real
  "Maurya Chara" with `VisionSource` radius 12 and move speed 8.625 (7.5 base ×
  Maurya's 1.15 civ multiplier); the real tier button correctly showed "Upgrade
  to Vega Ashvarohi (100 Gold, 50 Wood)" and its own `onClick.Invoke()` deducted
  exactly that; a forced tick advanced the tier and a second Chara trained
  afterward came out "Maurya Vega Ashvarohi" with vision 14, speed 9.775, HP
  30.8, while the first, already-spawned Chara stayed at vision 12/speed 8.625 —
  not retroactive, confirmed live. Next: Wave 4 item 19 (Skirmisher, 2 tiers) or
  any other Wave 4 item, user's call — all are parallel-safe once Wave 0/1 are
  done.
- **Wave 3 item 15 (Galley/Naval line, 3 tiers) closed (2026-09-05).** Picked up
  right after item 14, per the user's "start wave 3 item 15" request. No new design
  decisions needed — item 15's own roadmap text already fixes tier count/names/ages
  (Rana Nauka → Maha Rana Nauka → Samrat Nauka, Classical/Durg/Imperial), and the
  mechanism exactly mirrors `ArcherLineProgress.cs`'s own shape from item 11 (same
  3-tier Classical/Durg/Imperial gate pattern). New `Progression/NavalLineProgress.cs`
  mirrors `ArcherLineProgress.cs` exactly; tier bonuses/costs reuse Archer's own values
  at matching gates (+18 HP/+4 dmg/120 Gold/60 Wood/25s at Durg, +30 HP/+6 dmg/200
  Gold/100 Wood/40s at Imperial), not independently balanced. Unlike every land-line
  item, research lives on **Dock**, not Barracks — the same "research lives where the
  unit trains" deviation item 13's Elephant line already established for Durg, since
  War Galley trains from Dock (`Dock.RequestTrainWarGalley`), not Barracks. New
  `Dock.RequestResearchNavalTier`/`IsResearchingNavalTier`/`NavalTierResearchProgress`/
  `TickNavalTierResearch` mirror Barracks' own tier-research shape exactly.
  `WarGalleyFactory.cs` now reads `NavalLineProgress.Current(faction)` at spawn
  (previously always named the unit literal "War Galley" — now `"{civ} {tier.Name}"`,
  matching every other tier-ladder factory) and adds the tier's HP/damage bonus. New
  `navalTierButton`/`navalTierLabel` in `BuildMenu.cs` (identical shape to
  `siegeTierButton`, gated on a selected Dock), hotkey X (checked every other letter
  was already claimed across the project's contextual hotkey map via
  `GameSettings.GetKey` call sites before picking it), wired into
  `SettingsMenu`/`HotkeyOverlay`'s existing `DockGroup`. Same explicitly-out-of-scope
  call as items 9-14: no AI-side research hook (future balance work, not a
  regression); Chola's Naval Raider unique unit is untouched, out of scope. 10 new
  EditMode tests (`NavalLineTests.cs`, 322 total, all pass). Hit and fixed the same
  recurring "new `[SerializeField]` null in the scene" gotcha every Wave 2/3 session
  has hit (duplicated `WarGalleyButton` into a real `NavalTierButton` scene object via
  UnityMCP). Live-verified via UnityMCP through the real production path: a real
  match (`CivilizationSetup.BeginMatch(Chola)`), a real `DockFactory.Place`-spawned
  Dock selected via `SelectionManager`, the age gate correctly refused research at
  Ancient with 1000 Gold/Wood on hand (zero deduction), then at Durg deducted exactly
  120 Gold/60 Wood and completed via a forced tick, a War Galley trained afterward
  through the real `WarGalleyFactory.Spawn` path came out "Chola Maha Rana Nauka" at
  72.45 HP, the real scene button's own `onClick.Invoke()` deducted exactly 200
  Gold/100 Wood at Imperial for the second tier, and after that tier completed a War
  Galley spawned "Chola Samrat Nauka" at 90 HP with the real button's label settling
  on "Naval (Max Tier)" and `interactable=false`. Next: Wave 3 item 16 (Unique-unit
  Elite tier, 2 tiers × 5 units), user's call.
- **Wave 3 item 14 (Mangonel/Siege line, 3 tiers) closed (2026-09-05).** Picked up
  right after item 13, per the user's "start wave 3 item 14" request. No new design
  decisions needed — item 14's own roadmap text already fixes tier count/names/ages
  (Shilakshepaka → Maha Shilakshepaka → Vajra Shilakshepaka, Durg/Imperial/Imperial),
  and the mechanism exactly mirrors `CavalryLineProgress.cs`'s own shape from item 12
  (research on Barracks, baked in at spawn, not retroactive, back-to-back
  Imperial-gated last two tiers). New `Progression/SiegeLineProgress.cs` mirrors
  `CavalryLineProgress.cs` exactly; tier bonuses/costs reuse the same growth every
  other line's own back-to-back Imperial pair already uses (Cavalry's Maha Ashvarohi
  → Vir Ashvarohi values: +30 HP/+6 dmg/200 Gold/100 Wood/40s, then +45 HP/+9
  dmg/250 Gold/125 Wood/50s), not independently balanced. New independent research
  track on `Barracks.cs` (`RequestResearchSiegeTier`, alongside the existing
  Infantry/Spearman/Archer/Cavalry tier tracks), `SiegeFactory.cs` now reads
  `SiegeLineProgress.Current(faction)` at spawn (previously always named the unit
  literal "Siege" — now `"{civ} {tier.Name}"`, matching every other tier-ladder
  factory). New `siegeTierButton`/`siegeTierLabel` in `BuildMenu.cs` (identical
  shape to `cavalryTierButton`), hotkey O, wired into `SettingsMenu`/`HotkeyOverlay`'s
  `BarracksGroup`. Same explicitly-out-of-scope call as items 9-13: no AI-side
  research hook (future balance work, not a regression). 10 new EditMode tests
  (`SiegeLineTests.cs`, 312 total, all pass). Hit and fixed the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3 session has hit
  (duplicated `CavalryTierButton` into a real `SiegeTierButton` scene object via
  UnityMCP). Live-verified via UnityMCP through the real production path in Play
  mode (needed for `BuildMenu.Awake()`'s `_selectionManager` resolution to actually
  run): a real match (`CivilizationSetup.BeginMatch(Maurya)`), a real
  `BarracksFactory.Place`-spawned Barracks selected via `SelectionManager`, the age
  gate correctly showed the "(needs Imperial Age)" label at Durg with zero
  deduction, the real scene button's own `onClick.Invoke()` deducted exactly 200
  Gold/100 Wood at Imperial and started research, a forced tick advanced the tier
  and a Siege unit trained afterward through the real `SiegeFactory.Spawn` path came
  out "Maurya Maha Shilakshepaka" at 96 HP, and after researching the second tier
  the real button's label correctly settled on "Siege (Max Tier)". Next: Wave 3
  item 15 (Galley/Naval line, 3 tiers), user's call.
- **Wave 3 item 13 (Elephant line, 2 tiers) closed (2026-09-05).** Picked up right
  after item 12's live-verification follow-up, per the user's "start wave 3 item 13"
  request. Two design decisions resolved via AskUserQuestion before coding — item 13's
  own text already flagged one (one shared ladder vs. two divergent), and re-reading
  Wave 3 item 16 (Unique-unit Elite tier, not yet started) surfaced a second, real
  overlap not previously caught: item 16 separately planned a Durg→Imperial "elite"
  upgrade for the same two War Elephant factories item 13 also upgrades. Resolved:
  (1) one shared `ElephantLineProgress.cs` table (Gajaroha → Maha Gajaroha,
  Durg/Imperial) read by both `MauryaWarElephantFactory`/
  `VijayanagaraWarElephantFactory` — matches every other tier line's own precedent;
  (2) item 13 IS the elephant elite tier, so item 16's own roadmap text now excludes
  both War Elephant factories from its 7-unit list (5 remain). Research lives on
  **Durg**, not Barracks — the one tier line that deviates from every prior line's
  convention, since War Elephants train from Durg
  (`UniqueUnitDefinition.Spawn`/`Durg.RequestTrainUniqueUnit`), not Barracks: new
  `Durg.RequestResearchElephantTier`/`IsResearchingElephantTier`/
  `ElephantTierResearchProgress`/`TrainsElephant` (the last gates the new
  `elephantTierButton` so Chola/Rajput/Maratha's Durg never shows a button that does
  nothing — added a `UnitId` field to `UniqueUnitDefinition` for this, a data-driven
  check rather than a hardcoded civ list). Single Imperial-gated tier reuses the same
  +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other line's own first Imperial
  step already uses, not independently balanced. 13 new EditMode tests
  (`ElephantLineTests.cs`, 302 total, all pass). Hit and fixed the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3 session has hit
  (duplicated `CavalryTierButton` into a real `ElephantTierButton` scene object).
  Live-verified via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real Durg confirmed
  `TrainsElephant=true`, the age gate correctly refused research at both Classical and
  Durg (this line's tier 1 gates on Imperial, not Durg) with zero deduction, then at
  Imperial deducted exactly 200 Gold/100 Wood; a War Elephant trained through the real
  slot-0 unique-unit path spawned as "Maurya Maha Gajaroha" at 156 HP; the real scene
  button correctly showed "Elephant (Max Tier)" on the real selected Durg; a second
  real Durg built for a Rajput-assigned faction confirmed the button correctly stays
  hidden for a civ with no elephant. No AI-side research hook (future balance work,
  same as every other tier line). Next: Wave 3 item 14 (Mangonel/Siege line, 3 tiers),
  user's call.
- **Age-aware building visuals + new asset import closed (2026-09-04)** —
  `docs/YOUR_ACTION_ITEMS.md` items 1-4. User supplied a fresh art delivery
  (`/Volumes/US/3D MODELS/`): TownCenter Ancient/Classical (2 shared) + 5 civ-specific
  Durg models, Tower/Wall Ancient/Classical/Durg (3 shared each), Lumber Camp/Mining
  Camp/Mill. Villager art was already fully wired from a prior session (staged files
  confirmed byte-identical via `md5`) — nothing to do there. **User-confirmed design
  decision**: Age-up re-skins ARE retroactive (every standing TownCenter/Tower/Wall a
  faction owns rebuilds its visual mesh in place the instant Age-up completes,
  AoE-style) — a deliberate one-off exception to this project's usual
  "baked in at spawn, not retroactive" convention. `BuildingModelFactory.Spawn`
  gained an optional `AgeId?` param (all 11 other, non-age-tiered factories
  unaffected) and a new `Refresh` method (destroys/rebuilds only the visual mesh +
  collider on an already-live building, leaving every gameplay component untouched —
  a pure re-skin, not a re-spawn); new `AgeTieredBuildingVisual.cs` marker + static
  `RefreshAllForFaction`, called from `TownCenter.TickAgeUp()` (the sole call site of
  `AgeProgress.Advance`) right after a faction's Age actually advances — covers
  Tower/Wall too, since they react to the same faction-wide event.
  **Resource-path convention** (load-bearing for future sessions — see
  `docs/SESSION_LOG.md`'s matching entry for the full spec): shared non-civ age tiers
  at `Buildings/{resourceName}_{ageId}`; civ-specific age tiers (TownCenter Durg only)
  at `Buildings/{civId}/{resourceName}_{ageId}`; Imperial keeps its original unsuffixed
  path, untouched. Extended `MeshyBuildingImporter.cs` with a new
  `ImportSharedBuilding` entry point for the non-civ assets. **Found and fixed a real
  import bug**: source folders on the external volume carry macOS AppleDouble shadow
  files (`._<name>`, same extension, ~4KB) that `Directory.GetFiles(...).
  FirstOrDefault()` could silently pick over the real 76MB+ asset with zero
  compile/console errors (a 4096-byte, 0-mesh prefab was the tell) — fixed by
  excluding `._`-prefixed filenames from every glob in the importer, permanently.
  Asset identification required real visual verification twice over, not name-trust:
  the 4 unlabeled Durg-Age TownCenter folders were matched to civs by their raw UV-atlas
  texture's dominant color/motifs (confirmed live against each civ's existing Imperial
  art); Tower's "Stonewatch_Tower"/"Stonewatch_Bastion" folder names turned out
  **backwards** from their actual tier content once screenshotted against the real
  Ancient/Classical/Durg reference art. Also re-hit (and re-fixed, same algebraic
  method as prior sessions) the documented Tower-specific
  `ImportRotationCorrections` runtime-stomp gotcha — baking a raw visually-verified
  rotation into a new Tower prefab without accounting for the civ-blind stomp
  produces a double-rotated result that only shows up through the real
  `BuildingModelFactory.Spawn` path, not a raw `Resources.Load` probe. All 289
  EditMode tests pass unmodified (pure asset-pipeline + visual-only code, no new
  test, matching every prior building-import session's convention). Live-verified via
  UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Rajput)`), a real Tower + Wall spawned for Player,
  then a real `TownCenter.RequestAgeUp()` forced through Classical→Durg→Imperial —
  `GetInstanceID()` on both confirmed unchanged at every step (re-skinned in place,
  not respawned), `Attackable`/`GarrisonPoint`/`FactionMember` intact, exactly one
  `BoxCollider` each (no duplicate leftover), and the correct model rendered at every
  tier via screenshot. Also confirmed a real `WorkerFactory.Spawn` villager still
  plays its Idle clip correctly through the existing `PlayableGraph`-based
  `AnimationDriver` (untouched this session). See `docs/SESSION_LOG.md`'s matching
  entry for full detail, including the exact scale/rotation numbers per building.
  Next: item 5 (per-civ unit gear, 12 sets × 3 pieces) or item 6 (Wave 4 new-unit
  models) whenever the user sources that art, or back to
  `docs/IMPLEMENTATION_ROADMAP.md`'s wave order for code-only work, user's call.
- **Wave 3 item 12 live-verification follow-up closed (2026-09-04)** — picked up
  exactly where the prior item 12 session left off (both `unity`/`UnityMCP` MCP
  servers were unreachable that whole session, so the code+tests shipped without any
  live verification). UnityMCP reachable again this session. Scene-wired
  `CavalryTierButton`/`CavalryTierLabel` (duplicated from `ArcherTierButton`, the
  exact predicted "new `[SerializeField]` null in the scene" gotcha every Wave 2/3
  session has hit), wired both fields on `BuildMenu`'s component, scene saved. Full
  EditMode suite: 289/289 pass. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Rajput)`), the age
  gate correctly refused research at both Ancient and Durg (this line's tier 1 gates
  on Imperial, not Durg, confirmed against `CavalryLineProgress.cs`'s own table), a
  real `RequestResearchCavalryTier()` deducted exactly 200 Gold/100 Wood at Imperial
  and completed via a forced real tick, a Cavalry trained afterward came out "Rajput
  Maha Ashvarohi" at 96.6 HP, and the real scene button's label ("Upgrade to Vir
  Ashvarohi (250 Gold, 125 Wood)") and `onClick.Invoke()` correctly paid tier 2's
  cost through `SelectionManager`-selected Barracks — the scene-wired button itself
  works end to end, not just the underlying method. No script changes this session
  (scene-only + docs). Next: Wave 3 item 13 (Elephant line, 2 tiers — needs a design
  decision first per its own roadmap text: one shared ladder for
  Maurya/Vijayanagara or two divergent ones), user's call.
- **Female + male Worker body swap closed (2026-09-04)** — not a queued roadmap
  item, picked up at the user's explicit direction (2 real rigged Meshy AI
  "Harvest Guardian" villager glTF models supplied directly, one female one
  male). `HumanModelFactory.Gender.Female` was used by exactly one factory
  (`WorkerFactory.cs`), so this is a Worker-body swap across all 5 civs, not a
  general asset drop — every combat unit still uses the original shared Human
  Character Dummy body via `Gender.Male`, untouched. New reusable
  `Assets/Editor/HumanoidGltfRigImporter.cs` builds a real Humanoid `Avatar`
  for a Mixamo-style-named glTF rig (glTFast's own import doesn't auto-build
  one, unlike native FBX) via `AvatarBuilder.BuildHumanAvatar` + a
  hand-authored `HumanDescription`, with the skeleton bone array read directly
  off the model's own instantiated transform hierarchy (never hand-
  transcribed). Both models imported to `Assets/Resources/human/
  FemaleVillager|MaleVillager/`, scaled to the same measured 1.902692 worker-
  height convention every prior session has used. `WorkerFactory.cs` now
  randomly picks the female or male body per spawn (`Random.value < 0.5f`, no
  gameplay difference — pure crowd variety), `applyPaletteMaterial: false`
  (each model keeps its own painted identity texture matching its own concept
  art, same convention as the 3 existing Meshy-sourced unique units, not a
  civ-palette trim sheet) with `HumanAnimationSet.LoadFor(villagerGender)`
  picking the matching clip set. Confirmed live via UnityMCP that the
  project's existing shared 7-clip human animation library (Idle/Walk/Gather/
  Mine/Farm/Build/Attack) retargets cleanly onto both new rigs with zero new
  animation authoring (Mecanim retargeting is Avatar-based, not
  skeleton-name-based) — so the models' own bundled walk/run clips turned out
  unnecessary and were left unwired. All 289 pre-existing EditMode tests pass
  unmodified (no new tests — pure asset-pipeline + a small factory change,
  matching every prior `HumanModelFactory` session's own convention).
  Live-verified through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), 4 real spawned Workers (2 female
  body, 2 male body, confirmed via reflection), correct scale/ground
  alignment/texture next to a real TownCenter and livestock, a real Walk-clip
  pose showing genuine leg articulation (not T-posed), and a real
  reflection-forced Gather-clip pose showing a correctly bent reaching stance
  — both proving retargeting actually works, not just that the Avatar
  reports valid. This session's own scratch investigation import deleted once
  the real Resources-path prefabs were confirmed working. The
  `HumanoidGltfRigImporter.cs` utility is directly reusable for the still-
  blocked Crusader Knight combat-unit body swap, or any other future
  glTF-rigged body, once new source files exist. Next: back to
  `docs/IMPLEMENTATION_ROADMAP.md`'s wave order, or whatever the user directs.
- **Now working from `docs/IMPLEMENTATION_ROADMAP.md`'s wave order (the new
  AoE-parity execution plan), strictly in wave order, one item per session —
  see that file's own ground rules. `docs/ROADMAP.md` Section 5's priority
  order is the prior plan; items already closed under it stay closed, but new
  work picks up from `IMPLEMENTATION_ROADMAP.md` instead.**
- **Wave 3 item 12 (Knight/Cavalry line, 3 tiers) closed (2026-09-04) - code and
  tests only, live UnityMCP verification still outstanding.** Picked up right
  after item 11 per the user's "wave 3 item 12 start" request. No new design
  decisions needed - item 12's own roadmap text already fixes tier count/names/
  ages (Ashvarohi → Maha Ashvarohi/Durg → Vir Ashvarohi/Imperial - note both
  upgrade tiers gate on Imperial, per the roadmap's own "Durg/Imperial/Imperial"
  spec), and the mechanism (research on Barracks, baked in at spawn, not
  retroactive) was already established by items 9-11. Confirmed tier 0's
  `RequiredAge` is descriptive only (never enforced), so `RequestTrainCavalry`
  keeps its existing no-age-gate behavior - only the tier ladder above it is
  new. New `Progression/CavalryLineProgress.cs` mirrors `SpearmanLineProgress.cs`'s
  shape exactly; tier bonuses/costs reuse `InfantryLineProgress`'s own
  back-to-back Imperial pair (Maha Khandayata → Vir Yodha) at matching gates -
  the closest existing precedent for two successive Imperial-gated tiers - rather
  than independently balanced (Maha Ashvarohi = +30 HP/+6 dmg/200 Gold/100
  Wood/40s; Vir Ashvarohi = +45 HP/+9 dmg/250 Gold/125 Wood/50s). New independent
  research track on `Barracks.cs` (`RequestResearchCavalryTier`, alongside the
  existing Infantry/Spearman/Archer tracks), `CavalryFactory.cs` now reads
  `CavalryLineProgress.Current(faction)` at spawn. New `cavalryTierButton`/
  `cavalryTierLabel` in `BuildMenu.cs`, hotkey M, wired into
  `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope
  call as items 9-11: no AI-side research hook; Rajput Royal Guard untouched. 10
  new EditMode tests (`CavalryLineTests.cs`, mirroring `SpearmanLineTests.cs`).
  **Could not live-verify via UnityMCP this session**: both the `unity` and
  `UnityMCP` MCP servers failed to connect (`ConnectionRefused`) for this entire
  session despite a real Unity Editor instance actively running and compiling
  the new files - checked `~/Library/Logs/Unity/Editor-prev.log` and the
  project's own `Logs/` directory directly (this project's own "console bridge
  can miss real errors" convention) and found no compile errors, but could not
  run the Unity Test Runner or drive a real match/button click the way every one
  of items 9-11 did. **Flagging directly, not glossing over it**: the new
  `cavalryTierButton`/`cavalryTierLabel` `[SerializeField]` fields are almost
  certainly null in the scene right now (the exact recurring gotcha every Wave
  2/3 session has hit) and still need the scene-wiring fix (duplicate
  `ArcherTierButton` into a real `CavalryTierButton` scene object) plus a real
  live-verification pass once UnityMCP reconnects. Next: retry live
  verification for this item once Unity/UnityMCP is reachable, then Wave 3 item
  13 (Elephant line, 2 tiers - needs a design decision first per its own
  roadmap text: one shared ladder for Maurya/Vijayanagara or two divergent
  ones), user's call.
- **Wave 3 item 11 (Archer line, 3 tiers) closed (2026-09-04).** Picked up right
  after item 10 per the user's "start wave 3 item 11" request. No new design
  decisions needed — item 11's own roadmap text already fixes tier count/names/ages
  (Dhanurdhara → Yantra Dhanurdhara/Durg → Maha Dhanurdhara/Imperial), and the
  mechanism (research on Barracks, baked in at spawn, not retroactive) was already
  established by items 9/10 in the same wave. New `Progression/ArcherLineProgress.cs`
  mirrors `SpearmanLineProgress.cs`'s shape exactly (same Classical/Durg/Imperial
  gate pattern); tier bonuses/costs reuse SpearmanLineProgress's own values at
  matching age gates rather than independently balanced (Yantra Dhanurdhara =
  Trishuladhari's Durg-gate growth: +18 HP/+4 dmg/120 Gold/60 Wood/25s; Maha
  Dhanurdhara = Maha Trishuladhari's Imperial-gate growth: +30 HP/+6 dmg/200
  Gold/100 Wood/40s). New independent research track on `Barracks.cs`
  (`RequestResearchArcherTier`, runs alongside the existing Infantry/Spearman tier
  tracks without blocking them), `ArcherFactory.cs` now reads
  `ArcherLineProgress.Current(faction)` at spawn. New `archerTierButton`/
  `archerTierLabel` in `BuildMenu.cs` (identical shape to
  `infantryTierButton`/`spearmanTierButton`), hotkey H, wired into
  `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope
  call as items 9/10: no AI-side research hook (future balance work, not a
  regression). 10 new EditMode tests (`ArcherLineTests.cs`, mirroring
  `SpearmanLineTests.cs` — 279 total, all pass). Hit the same environment gotcha
  every Wave 2/3 session has documented (new `[SerializeField]` fields null in the
  scene) — fixed the same way, duplicating `SpearmanTierButton` into a real
  `ArcherTierButton` scene object via UnityMCP. Live-verified via UnityMCP through
  the real production path: a real match (`CivilizationSetup.BeginMatch(Rajput)`,
  deliberately not Maurya/Maratha — see item 9's own flagged `UniqueTechDefinition`
  bug, `task_55dbb0cc`, still open and unrelated to this item), a real
  `RequestResearchArcherTier()` correctly refused at Ancient age even with 1000
  Gold/Wood on hand, then deducted exactly 120 Gold/60 Wood at Durg; forcing the real
  `Update()` tick advanced the tier and an Archer spawned afterward came out
  "Rajput Yantra Dhanurdhara" at 47.61 HP (base 18 + 18 bonus, scaled by Rajput's own
  profile/age multipliers; not retroactive, matching the established convention); the
  real button's label and `onClick.Invoke()` correctly showed and paid the Maha
  Dhanurdhara cost (200 Gold/100 Wood) once Player reached Imperial. Next: Wave 3
  item 12 (Knight/Cavalry line, 3 tiers), user's call.
- **Wave 3 item 10 (Spearman line, 3 tiers) closed (2026-09-04).** Picked up right
  after item 9 per the user's "start wave 3 remaining item" request. No new design
  decisions needed — item 10's own roadmap text already fixes tier count/names/ages
  (Bhaladhari → Trishuladhari/Durg → Maha Trishuladhari/Imperial), and the mechanism
  (research on Barracks, baked in at spawn, not retroactive) was already established
  by item 9 in the same wave. New `Progression/SpearmanLineProgress.cs` mirrors
  `InfantryLineProgress.cs`'s shape exactly; tier bonuses/costs scaled off Infantry's
  own curve at matching age gates rather than independently balanced
  (Trishuladhari = Khandayata's Durg-gate growth: +18 HP/+4 dmg/120 Gold/60 Wood/25s;
  Maha Trishuladhari = Maha Khandayata's Imperial-gate growth: +30 HP/+6 dmg/200
  Gold/100 Wood/40s). New independent research track on `Barracks.cs`
  (`RequestResearchSpearmanTier`, runs alongside the existing Infantry tier track
  without blocking it), `SpearmanFactory.cs` now reads
  `SpearmanLineProgress.Current(faction)` at spawn. New `spearmanTierButton`/
  `spearmanTierLabel` in `BuildMenu.cs` (identical shape to `infantryTierButton`),
  hotkey L, wired into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same
  explicitly-out-of-scope call as item 9: no AI-side research hook (future balance
  work, not a regression). 10 new EditMode tests (`SpearmanLineTests.cs`, mirroring
  `InfantryLineTests.cs` — 269 total, all pass). Hit the same environment gotcha
  every Wave 2/3 session has documented (new `[SerializeField]` fields null in the
  scene) — fixed the same way, duplicating `InfantryTierButton` into a real
  `SpearmanTierButton` scene object via UnityMCP. Live-verified via UnityMCP through
  the real production path: a real match (`CivilizationSetup.BeginMatch(Rajput)`,
  deliberately not Maurya/Maratha — see item 9's own flagged `UniqueTechDefinition`
  bug, `task_55dbb0cc`, still open and unrelated to this item), a real
  `RequestResearchSpearmanTier()` correctly refused at Ancient age even with funds
  on hand, then deducted exactly 120 Gold/60 Wood at Durg; forcing the real
  `Update()` tick advanced the tier and a Spearman spawned afterward came out
  "Rajput Trishuladhari" at 70.0925 HP (not retroactive to already-spawned units,
  matching the established convention); the real button's label and
  `onClick.Invoke()` correctly showed and paid the Maha Trishuladhari cost (200
  Gold/100 Wood) once Player reached Imperial. Next: Wave 3 item 11 (Archer line, 3
  tiers), user's call.
- **Wave 3 item 9 (Infantry line, 5 tiers) closed (2026-09-04) — first Wave 3
  item.** Picked up at the user's "start wave 3" request. Resolved 3 design
  decisions via AskUserQuestion before coding: research lives on Barracks
  (not Karmashala), each tier renames the unit and improves stats but reuses
  the existing Human Character Dummy model (no new art needed), and progress
  is NOT retroactive (matches every other progression system here). New
  `Progression/InfantryLineProgress.cs` (hardcoded tier table: name/required
  age/HP+damage bonus/gold+wood cost/research time), a new independent
  research track on `Barracks.cs` (`RequestResearchInfantryTier`), and
  `SoldierFactory.cs` now reads the current tier at spawn — genuinely "tier 1
  of a ladder." No CommandBus/network wiring needed (Barracks' existing
  `RequestTrain()` is untouched — only what's baked in at spawn changes). New
  `infantryTierButton`/`infantryTierLabel` in `BuildMenu.cs` (hotkey I),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. No AI-side
  research hook added — not a regression (nothing existing broke), matches
  the "never wired" status this project's own per-class Attack/Armor tracks
  already have; flagged as future balance work, not fixed. 10 new EditMode
  tests (`InfantryLineTests.cs`, 255 total, up from 245, all pass). Hit the
  same "new SerializeField null in scene" gotcha Durg/Karmashala's sessions
  already documented — fixed by duplicating `UniqueTechButton` into a real
  `InfantryTierButton` scene object via UnityMCP. Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), the age gate correctly blocked
  research at Classical ("needs Durg Age") and unblocked after
  `AgeProgress.Advance` to Durg, a real `RequestResearchInfantryTier()`
  deducted the exact Gold/Wood cost and completed via the real `Update()`
  tick (forced via reflection, not a test shortcut), a Soldier spawned
  *before* completion stayed unchanged ("Maurya Padati", 33 HP) while one
  spawned *after* came out "Maurya Senani" at 41.8 HP — not retroactive,
  confirmed live — and the real button's `onClick.Invoke()` correctly routed
  through to the same method. **Found, not fixed (real pre-existing bug,
  unrelated to this item)**: selecting a real Maurya Barracks and driving
  `BuildMenu.Update()` throws `KeyNotFoundException` inside
  `UniqueTechDefinition.For` — its `Bonuses` dictionary only covers
  Chola/Vijayanagara/Rajput, not Maurya/Maratha. Flagged via `spawn_task`
  (`task_55dbb0cc`) for a dedicated follow-up rather than silently left
  unnoticed; worked around it in this session's own verification by invoking
  the new `UpdateInfantryTierButton` method directly via reflection instead
  of through the crashing `Update()`. See `docs/SESSION_LOG.md`'s matching
  entry for full detail. Next: Wave 3 item 10 (Spearman line, 3 tiers),
  user's call — or the newly-flagged Maurya/Maratha UniqueTechDefinition bug.
- **Wave 2 item 8 (Karmashala, the Blacksmith-equivalent building) closed
  (2026-09-04) — this closes Wave 2.** Picked up right after item 7 per the
  user's "start wave 2 item 8" request. Resolved the item's two design
  decisions via AskUserQuestion before coding: gated to Classical Age (same
  as Barracks, not a late-game unlock like Durg), and the flat Attack/Armor
  research tracks (`RequestResearchAttack`/`RequestResearchArmor`) move OFF
  `Barracks` onto Karmashala entirely — mirrors how Durg took unique-unit
  training off Barracks last session. Barracks' per-class Attack/Armor
  tracks (item 40, never wired to any UI button) and civ UniqueTech stay on
  Barracks/Durg untouched. New `Buildings/Karmashala.cs`/`KarmashalaFactory.cs`
  (`MillFactory.cs` as the factory template, Barracks' own research code as
  the component template): 150 Wood only, 10s build, 220 HP/1-2 armor
  (deliberately "raidable," between Mill's 200 HP and Barracks' 300),
  3-tile/Market-sized footprint. Wired through the full stack:
  `BuildingPlacer`'s `CanPlaceKarmashala`, `NetBuildKind.Karmashala`
  (build-only — research itself was already un-networked before this
  session, a pre-existing gap not fixed here), `BuildMenu`'s
  `attackUpgradeButton`/`armorUpgradeButton` re-gated from a selected
  Barracks to a selected Karmashala (new `UpdateKarmashalaButtons`, split
  out of `UpdateBarracksButtons` the same way `UpdateDurgButtons` split out
  unique-unit training last session) plus one new placement button
  (`karmashalaButton`, added via UnityMCP scene editing — see gotcha
  below), `SettingsMenu`/`HotkeyOverlay` updated to match (new
  `KarmashalaGroup`, mirroring `DurgGroup`'s own split from
  `BarracksGroup`). **A real regression was caught and fixed before it
  shipped, same class of bug Durg's session hit**: the AI opponent
  researched Attack/Armor via `_barracks.RequestResearchAttack()/
  RequestResearchArmor()` in `TryResearchUpgrades()` — moving those off
  Barracks with no AI-side Karmashala would have silently ended the AI's
  flat Attack/Armor research forever. Fixed with a `TryBuildKarmashala()`/
  `AssignKarmashalaBuilderIfNeeded()` pair mirroring `TryBuildDurg()`'s own
  shape, and `TryResearchUpgrades()` now checks `_karmashala` first,
  independently of the `_barracks` guard below it — no Karmashala yet means
  the AI simply skips flat Attack/Armor research for now, same
  "silently no-ops if unavailable" convention `TryTrainSoldiers`' Durg
  fallback already established. 9 new EditMode tests (`KarmashalaTests.cs`,
  245 total, up from 236, all pass) — building factories deliberately not
  exercised there, same documented NRE-outside-Play-mode limitation as
  `BarracksFactory.Place`/`DurgFactory.Place`. **Hit the exact same
  environment gotcha Durg's session already documented**: the new
  `karmashalaButton`/`karmashalaLabel` `[SerializeField]` fields were null
  in the scene (added to the C# class but never wired to a GameObject) —
  `BuildMenu.Update()` NRE'd every frame with zero errors surfaced by the
  MCP console bridge; only reflection-invoking `Update()` directly inside a
  try/catch surfaced the real stack trace pointing at
  `SetPlacementButtonsActive`. Fixed the same way: duplicated `DurgButton`
  into a real `KarmashalaButton` scene GameObject via UnityMCP and wired
  the component fields to it. Live-verified via UnityMCP through the real
  production path after that fix: a real match
  (`CivilizationSetup.BeginMatch(Chola)`, which starts at Ancient — Maurya's
  own Classical-start convention would have made the Ancient-age gate check
  meaningless), `CanPlaceKarmashala` false in Ancient/true in Classical, a
  real spawned Karmashala + a real spawned Barracks selected in turn showed
  exactly the right button sets (`attackUpgradeButton`/`armorUpgradeButton`
  active only on Karmashala, `soldierButton` active only on Barracks), a
  real button click on `attackUpgradeButton` deducted Gold and started
  research through the real `RequestResearchAttack` path, a real
  `karmashalaButton` click correctly entered `BuildingPlacer.IsPlacing`, and
  the AI's own `TryBuildKarmashala`/`AssignKarmashalaBuilderIfNeeded`/
  `TryResearchUpgrades` chain built a real Karmashala, deducted Wood, then
  deducted Gold and started research once complete — all through the real
  production path, not test shortcuts. **Flagged, not fixed**: `Karmashala`
  has no bespoke 3D model yet (falls back to the generic procedural shape,
  same as Durg/Lumber Camp/Mining Camp/Mill before their models existed) —
  needs real art sourced later. **This closes Wave 2** — both structural
  buildings every later unique-unit and upgrade-line item implicitly
  assumes exist, now actually exist. See `docs/SESSION_LOG.md`'s matching
  entry for full detail. Next: Wave 3 (upgrade ladders, one line per
  session), user's call.
- **Wave 2 item 7 (the Durg building) closed (2026-09-04).** Picked up at the
  user's explicit "START WAVE 2" request. Resolved the item's two flagged
  open design decisions via AskUserQuestion before coding: it trains unique
  units (relocated off `Barracks` — matches AoE's Castle-trains-uniques
  convention), and it's the strongest defensive building in the game,
  strictly above `TownCenter` on every axis (`GarrisonCapacity` 12 vs. 8, 6
  max bonus shots vs. 4, 700 HP/4-6 armor vs. 500/3-5, 12 dmg/range 9/1.2s
  interval vs. 8/8/1.4). New `Buildings/Durg.cs`/`DurgFactory.cs`
  (`Barracks.cs`/`TownCenterFactory.cs` templates); `Barracks.cs`'s
  `RequestTrainUniqueUnit`/`UniqueUnitCount`/`UniqueUnitAt`/`UniqueUnit`
  deleted (everything else on Barracks untouched); `BuildingPlacer.cs` gained
  a `Durg` kind (Age-gated to Durg+, 200 Wood/150 Stone, 25s, hotkey D);
  `NetMessage.cs`/`CommandSerializer.cs` gained matching wire support;
  `BuildMenu.cs` re-gated its existing unique-unit buttons from `Barracks` to
  `Durg` and gained one genuinely new placement button (`durgButton`, wired
  via UnityMCP scene editing — see gotcha below). **A real regression was
  caught and fixed before it shipped**: the AI opponent trained its unique
  unit via `_barracks.RequestTrainUniqueUnit()`; moving that off Barracks
  with no AI-side Durg would have silently ended the AI's unique-unit
  training forever — fixed with a `TryBuildDurg()`/
  `AssignDurgBuilderIfNeeded()` pair mirroring `TryBuildBarracks()`'s own
  shape, plus a Soldier fallback in the AI's training rotation until its own
  Durg is complete. 4 new EditMode tests (`DurgTests.cs`) plus 2 existing
  unique-unit tests updated to build a `Durg` instead of a `Barracks` (236
  total, up from 232, all pass). **Hit a real environment gotcha mid-session,
  worked through not around**: the new `durgButton`/`durgLabel`
  `[SerializeField]` fields were null in the scene (added to the C# class but
  never wired to a GameObject), which made `BuildMenu.Update()` NRE every
  frame with **zero errors surfaced by the MCP console bridge** — same
  "console bridge can miss real errors" gotcha this project has hit before
  for compile errors, now also seen for a runtime exception; only
  reflection-invoking `Update()` directly inside a try/catch surfaced the
  real stack trace. Fixed by actually duplicating `MillButton` into a new
  `DurgButton` scene GameObject via UnityMCP and wiring the component fields
  to it — a real scene edit, not a code-only fix; the lesson: a new
  `[SerializeField]` UI field on an existing hand-wired class needs a
  matching scene edit in the same session. Live-verified via UnityMCP through
  the real production path after that fix: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), `CanPlaceDurg` false pre-Durg-age
  and true after `AgeProgress.Advance`, a real spawned Durg's live stats
  matched every constant exactly via reflection, selecting a real Barracks
  vs. a real Durg showed exactly the right button sets with correct
  civ-specific unique-unit labels, a real button click enqueued training
  through `CommandBus` and spent/spawned correctly a couple of ticks later,
  and a real placement-button click correctly entered
  `BuildingPlacer.IsPlacing`. **Flagged, not fixed**: `Durg` has no bespoke
  3D model yet (falls back to the generic procedural shape, same as Lumber
  Camp/Mining Camp/Mill before their models existed) — needs real art
  sourced later. Wave 2 item 8 (Karmashala) remains open; Wave 2 isn't fully
  closed yet. See `docs/SESSION_LOG.md`'s matching entry for full detail.
  Next: Wave 2 item 8 (Karmashala), user's call.
- **Wave 1 item 6 (age-up building-count requirement) closed (2026-09-04) —
  this closes Wave 1.** Resolved the design question flagged when item 5
  closed via AskUserQuestion: "2 buildings" means 2 completed, non-TownCenter
  buildings currently owned (no per-age building taxonomy — that stays
  materially out of scope), and the gate applies from Classical onward only
  (Ancient→Classical stays cost-only). New `Buildings/AgeUpRequirement.cs`
  wired into `TownCenter.RequestAgeUp()` alongside the existing Wood/Stone
  check, and into `BuildMenu`'s Age-up button label. 11 new EditMode tests
  (232 total, up from 221, all pass) — hit this project's own documented
  `Building.OnEnable`-isn't-synchronous-in-EditMode-tests gotcha, fixed the
  same way `BuildingFootprintTests` already does (register directly into
  `Building.All`). Live-verified via UnityMCP through the real production
  path: a real match (`CivilizationSetup.BeginMatch(Rajput)`), a real
  `TownCenter.RequestAgeUp()` correctly exempted Ancient→Classical with zero
  extra buildings, then correctly refused Classical→Durg with zero extra
  buildings (no resources spent) — then real `HouseFactory.Place`/
  `BarracksFactory.Place` + `ConstructionSite.CompleteImmediately()` brought
  the Player to 2 real buildings and the identical request immediately
  succeeded. Wave 1's exit criteria are all met. Next: Wave 2 (the Durg
  building / Karmashala), user's call.
- **Wave 1 item 5 (add `AgeId.Durg`) closed (2026-09-04).** `AgeId` now has 4
  values (Ancient/Classical/Durg/Imperial), `age_profile_template.csv` has a
  new interpolated Durg row (250 Wood/150 Stone, 40s research, gather x1.18,
  HP x1.15, train x0.85 — deliberately no Gold cost, since the schema has
  never had one for any age), `AgeProfile.cs`'s `Fallback`/`AgeIds`
  dictionaries updated to match. 6 new EditMode tests (221 total, up from
  215, all pass). Live-verified via UnityMCP through the real production
  path: a real match, a real Player `TownCenter.RequestAgeUp()` correctly
  spent Durg's exact cost and, after its real 40s `Update()`-ticked
  countdown, landed `AgeProgress.CurrentAge(Player)` on `Durg`; a second real
  age-up from Durg correctly targeted Imperial (cost/progress both
  confirmed). **Item 6 (age-up building-count requirement) explicitly
  deferred, not started** — the user chose to skip its design decision
  (what "2 buildings" should mean, given this codebase has no per-age
  building taxonomy) rather than force a definition this session; see
  `docs/IMPLEMENTATION_ROADMAP.md`'s matching item for the open question.
  Wave 1's exit criteria are otherwise met (a player can reach Durg/Imperial
  in a real match). Next: item 6 if you want the design question resolved,
  or Wave 2 (the Durg building / Karmashala), user's call.
- **Wave 0 item 4 (wire `DamageType.Trample`/`Fire`) closed (2026-09-04) — this
  closes Wave 0 (items 1-4 all done).** Trample wired, Fire correctly deferred
  to its real Wave 4 consumer (the Fire Ship). First resolved the enum
  duplication item 1 flagged as this item's own prerequisite:
  `Combat/Attackable.cs`'s `DamageType` (previously Melee/Pierce only) is now
  the single shared enum (Melee/Pierce/Siege/Fire/Trample) — the second,
  incompatible global `DamageType` on `UnitDefinition.cs` (unread CSV
  metadata) is deleted, `UnitDefinition.attackType` now references the shared
  type directly. `Attackable.TakeDamage` gained an explicit
  `UsesPierceArmor(DamageType)` helper (Pierce/Fire → pierceArmor;
  Melee/Trample/Siege → meleeArmor) so the armor lookup and the
  upgrade-scaling-applies check can't drift apart. `MauryaWarElephantFactory.cs`/
  `VijayanagaraWarElephantFactory.cs` now call `SetDamageType(DamageType.Trample)`
  + `SetSplashRadius(1.4f, 0.4f)`, reusing `CavalryFactory`'s own already-proven
  trample-splash mechanism (a shade larger/heavier than Cavalry's 1.25/0.35,
  still below `GroupFormation`'s 1.5 default spacing). `unit_roster_template.csv`'s
  `AttackType` column updated Melee→Trample for both war elephants, regenerated
  via `BharatRTS/Generate Data Assets From CSV`. 2 new EditMode tests
  (`TrampleDamageTests.cs`, 215 total, up from 213, all pass): Trample resolves
  against meleeArmor not pierceArmor; a Trample-tagged splash attacker damages a
  nearby hostile but not a far one. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maurya)`), real
  `MauryaWarElephantFactory`/`VijayanagaraWarElephantFactory`-spawned units both
  confirmed (via reflection) to carry `damageType=Trample`/`splashRadius=1.4` at
  spawn, and a real Maurya War Elephant attacking 3 real `SoldierFactory`-spawned
  Soldiers dealt full damage to the primary target (30→20 HP), reduced splash
  damage to one placed inside the trample radius (30→26.6 HP), and zero damage
  to one placed outside it (30→30 HP). See `docs/SESSION_LOG.md`'s matching
  entry for full detail. Wave 0 is fully closed — next is Wave 1, user's call.
- **Wave 0 item 3 (confirm the minimum-damage clamp) closed (2026-09-04)** —
  verify-only, no bug found: `Attackable.TakeDamage` (`Combat/Attackable.cs:160`,
  `Mathf.Max(1f, amount - armor)`) already applies its 1-damage floor as the
  *last* step, after armor subtraction, and every real call site
  (`MeleeAttacker.ResolveHit`/`BoatAttacker.Tick`/`BuildingAttacker.Tick`)
  passes the already counter-multiplied `baseDamage * CombatBonus.Multiplier(...)`
  into `TakeDamage` — so a stacked armor value plus a sub-1.0 `CombatBonus`
  multiplier can't silently floor a hit at 0, structurally, not just for
  today's stat ranges. 4 new EditMode tests (`DamageClampTests.cs`, 213
  total, up from 209, all pass) — hit this project's own documented "two
  `DamageType` enums" ambiguous-reference gotcha along the way (fixed by
  fully qualifying `KingdomsOfBharat.Combat.DamageType.Melee`/`.Pierce`,
  same fix as Wave 0 item 2/`WildBoar.cs`), and confirmed again that the MCP
  console bridge can report zero errors while a new file is fully failing to
  compile — the real `CS1503` errors were only visible in
  `~/Library/Logs/Unity/Editor.log` directly. Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch`), a real `CavalryFactory`-spawned Cavalry
  unit (0.4x hard-countered vs. Archer, `CombatBonus`'s real matchup)
  attacking a real `ArcherFactory`-spawned Archer configured with 500 melee
  armor — the real `MeleeAttacker.ResolveHit`/`Attackable.TakeDamage` path
  dealt exactly 1 damage, not 0. See `docs/SESSION_LOG.md`'s matching entry
  for full detail, including a first verification attempt that (correctly)
  failed to clamp because it stacked the wrong armor field against a
  Melee-type attacker — confirming the test setup itself was sensitive
  enough to catch a real miss. Next: Wave 0 item 4 (wire
  `DamageType.Trample`/`Fire` — also needs the separate `DamageType` enum
  duplication resolved first, per item 1's own note).
- **Wave 0 item 2 (verify the retroactive upgrade rule) closed (2026-09-04)**
  — confirmed `Progression/UpgradeProgress.cs` did NOT promote already-
  spawned units; its own comment said so explicitly ("baked in at spawn,
  not retroactive"), and the bonus was in fact read once per unit at spawn
  time by 13 combat-unit factories. Fixed: `Attackable`/`MeleeAttacker`/
  `BoatAttacker` now live-read `UpgradeProgress`'s bonus at damage-
  resolution time (using each unit's already-stored `FactionMember` +
  `unitClass`) instead of a value baked in once. Deliberately **opt-in**,
  not automatic — new `EnableUpgradeArmorScaling(melee, pierce)`/
  `EnableUpgradeDamageScaling()` calls, made only by the same 13 factories
  that already baked this bonus in before the fix (Soldier/Archer/Cavalry/
  Spearman/Siege/CholaNavalRaider/MauryaWarElephant/VijayanagaraWarElephant/
  RajputRoyalGuard/MarathaMavlaRaider/MarathaDurgGarrison/PillarEdictScholar/
  WarGalley) — buildings and Workers never baked this in and still don't
  opt in, so they correctly stay untouched by Blacksmith-style research,
  same as before. Preserved (not "fixed") an existing asymmetry:
  Archer/CholaNavalRaider apply the armor bonus to `pierceArmor` only,
  matching AoE's own pierce-specific "Archer Armor" line — the new
  `EnableUpgradeArmorScaling` takes independent melee/pierce flags for
  this. `CavalryFactory`'s Rajput unique-tech damage bonus
  (`UniqueTechProgress`, a separate mechanic, already documented as its
  own "not retroactive" convention) was left untouched, out of scope. 5
  new EditMode tests (209 total, up from 204, all pass) — hit and fixed
  this project's own documented "two `DamageType` enums" ambiguous-
  reference gotcha along the way (same class of bug `WildBoar.cs` hit
  before), same fix (fully-qualify
  `KingdomsOfBharat.Combat.DamageType.Melee`/`.Pierce`). Live-verified via
  UnityMCP through the real production path: a real match, a real
  `SoldierFactory`-spawned Soldier took 9 damage from a fixed 10-damage
  melee hit pre-research, then 8 damage from the identical hit on the SAME
  GameObject post-`UpgradeProgress.AdvanceArmor` (no respawn) — exactly
  `ArmorPerTier`'s 1-point improvement; a real Worker spawned after that
  same tier was already researched still took the full 10 damage (the
  opt-in scope guard holds live, not just in test fixtures); a real
  Archer's melee hit ignored the researched tier while its pierce hit
  reflected it, confirming the preserved asymmetry live. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Next: Wave 0
  item 3 (confirm the minimum-damage clamp) or item 4 (wire
  `DamageType.Trample`/`Fire`).
- **Wave 0 item 1 (reconcile UnitClass vs UnitCategory) closed (2026-09-04)**
  — collapsed the two overlapping "what kind of combatant is this" enums into
  one: `KingdomsOfBharat.Combat.UnitClass` now carries all 9 values (added
  `Support`/`Hero`), the former global `UnitCategory` enum is deleted, and
  every call site (`CounterMatrix`/`TechNode`/`FormationDefinition`/
  `CivilizationProfile`/`Barracks`/`Dock`/`WorkerFactory`/`CavalryFactory`/
  `CsvToScriptableObject`) reads the shared type. `FormationController`'s
  lossy translation shim (`MapUnitClass`) is gone — `CategoryOf` now reads
  `Attackable.Class` directly. `CombatBonus.Multiplier`'s own hand-tuned
  pairings are untouched (Support/Hero simply fall through to the existing 1x
  default, same as any other unlisted pairing) — this is not a merge of the
  CombatBonus/CounterMatrix *systems* (still deliberately separate, see this
  file's own gotcha below), just their shared vocabulary type. All 204
  EditMode tests pass unmodified; live-verified via UnityMCP: regenerated
  every CSV-driven asset (`BharatRTS/Generate Data Assets From CSV`, zero
  parse warnings, `worker` unit's `category` correctly reads `Support`),
  confirmed `CombatBonus.Multiplier(Archer, Cavalry)` still resolves to the
  audited 2.0x, and confirmed a real `FormationController.ComputeOffsets`
  call still places an Infantry unit at the front rank and an Archer unit at
  the back rank under the simplified direct read. **Flagged, not fixed**: a
  separate, adjacent enum duplication noticed while reading `Attackable.cs`
  — `KingdomsOfBharat.Combat.DamageType` (2 values: Melee/Pierce, what
  `Attackable.TakeDamage` actually uses) vs. the global `DamageType` in
  `UnitDefinition.cs` (5 values, including `Trample`/`Fire`) — is a different
  divergence, directly relevant to Wave 0 item 4 (wiring `Trample`/`Fire`),
  not this item. See `docs/SESSION_LOG.md`'s matching entry for full detail.
  Next: Wave 0 item 2 (verify the retroactive upgrade rule), item 3 (confirm
  the minimum-damage clamp), or item 4 (wire `DamageType.Trample`/`Fire` —
  now also needs the `DamageType` duplication resolved first).
- Prior plan's status (kept for history — see below for the full detail):
  working from Roadmap Section 5's priority order.
- **Fixed (2026-09-03): the Objectives tab `RectMask2D` over-culling bug
  flagged (not fixed) at the end of session 6 below, via `task_545a0590`'s
  follow-up.** Root cause: not an engine bug — every row/label/field under
  the growing `_objectivesScrollContent` was anchored to that rect's
  shifting CENTER `(0.5,0.5)` instead of its fixed TOP edge, so as content
  grew past ~7 rows the anchor drift pushed rows genuinely outside the
  scroll viewport's clip rect (RectMask2D was correctly culling them, just
  not where anyone intended). Fixed via a new `anchorTop` parameter on
  `CreateLabel`/`CreateButton`/`CreateInputField` (default `false`,
  every other call site unaffected), threaded through every creator call
  parented to `_objectivesScrollContent`. 204 EditMode tests pass
  unmodified (pure anchor-data change). Live-verified via UnityMCP: real
  10-row Objectives tab, confirmed early rows land at their exact intended
  `anchoredPosition` and render, confirmed the rows still reading
  `culled=true` at default scroll are legitimately below the viewport
  (scrolling to the bottom correctly un-culls exactly those) — i.e. what's
  left is real scroll clipping, not the bug. Screenshotted the real Game
  View. See `docs/SESSION_LOG.md`'s matching entry for full detail. Next:
  another Roadmap Section 5 item, user's call.
- **Scenario Editor heavy path, session 6 (per-kind bespoke input
  widgets) closed (2026-09-03) — this closes the entire Scenario Editor
  heavy-path epic.** Picked up at the user's explicit request ("start
  item on per-kind bespoke input widgets"), the last named item from the
  epic's original deferred list. Replaced session 2's generic "Param1"/
  "Param2" text fields with a per-Kind `ParamFieldSpec[]` table
  (`ObjectiveFieldSpecs`/`TriggerFieldSpecs`) driving a real widget per
  param: `FactionId`/`ResourceType`/building-type slots render as a new
  `BindEnumCycleField` cycle-on-click button (mirroring the Kind-cycle
  button's own idiom); genuinely free values (seconds, counts, position)
  stay plain text fields. `DestroyScriptedTarget`'s target-building
  widget is deliberately narrower than `BuildingCountThreshold`'s
  (`{"Barracks","TownCenter"}` only) — matches
  `MissionCsvLoader.SpawnScriptedTarget`'s real supported switch, so the
  widget can never offer a value that silently no-ops at play time. No
  changes needed to `ObjectiveRow`/`TriggerRow`/`MissionCsvLoader.cs`/the
  save format — widgets write the same canonical strings a correctly
  hand-typed value already would. 204 tests pass unchanged (pure UI
  change). **Found, but explicitly not fixed this session**: a real
  `RectMask2D` over-culling bug — the Objectives tab renders blank once
  it has ~7+ rows, a pre-existing latent bug from session 2 (not caused
  by this session's own widget change), root cause not isolated despite
  several ruled-out attempts (deferred-Destroy staleness, forced canvas
  updates, mask toggling, this project's own documented stuck-frame fix).
  Flagged via `spawn_task` (`task_545a0590`) for a dedicated follow-up
  session rather than left silently unnoticed. Live-verified the actual
  widget deliverable at row counts proven to render correctly: a real
  button click cycled a Faction field Player→Enemy, Save→Load
  round-tripped it intact, and Play correctly resolved a
  `PopulationThreshold` objective against real
  `Population.Current(FactionId.Enemy)` — not silently defaulting to
  Player — proving the widget-selected value flows through the real
  production path end to end. See `docs/SESSION_LOG.md`'s matching entry
  for full detail. Next: another Roadmap Section 5 item, or the newly-
  flagged RectMask2D follow-up.
- **Scenario Editor heavy path, session 5 (multiplayer LAN play of a
  custom scenario) closed (2026-09-03)** — picked up at the user's
  explicit request ("start item on multiplayer play of a custom
  scenario"), the last item deferred across sessions 1-4. Investigated
  first: `CivilizationSetup.BeginCustomScenarioMatch` was already
  network-safe as-is (every match-start entry point funnels through the
  same `BeginMatchCore`, already rewired project-wide to
  `NetworkMatch.LocalFaction`), so no changes were needed to
  `CivilizationSetup.cs`/`EntitySpawner.cs`/`ScenarioManager.cs`. Two
  real findings surfaced: **(1)** a genuine pre-existing bug —
  `AiController.cs` had zero `NetworkMatch` awareness, so the Enemy
  faction's AI kept fighting for control of units a real 2nd human LAN
  player already commanded. Fixed as a necessary prerequisite:
  `AiController.Start()` now disables itself when
  `myFaction == FactionId.Enemy && NetworkMatch.IsActive` (scoped to
  Enemy only — `Enemy2` untouched, matches the 2-human-only LAN scope;
  zero effect on local/offline matches). **(2)** a disclosed, not
  blocking, determinism caveat — `ScenarioManager`'s trigger closures use
  wall-clock `Time.time`, not `SimClock` ticks, so triggers could fire on
  a different tick per peer; this project's existing live-verified
  `NetworkDesyncMonitor`/`DesyncRecovery` resync safety net already
  covers this class of divergence. New `NetMessageEnvelope.scenarioJson`
  mirrors the existing `snapshotJson` convention, carried on `HostHello`;
  `LanMatchMenu` gained a scenario cycle row (`<`/`>`, "(None -
  Skirmish)" default) and routes to `BeginCustomScenarioMatch` instead of
  `BeginNetworkMatch` when a scenario was exchanged. 4 new EditMode tests
  (204 total, all pass). Live-verified via UnityMCP through the real
  production path (two real sockets within one process, the same
  disclosed single-machine limitation the original Phase 5 session
  flagged): a real `HostHello` carrying an in-memory scenario transmitted
  correctly over the wire, `CompleteHandshake` correctly invoked
  `BeginCustomScenarioMatch` with `ScenarioManager.ActiveScenario`
  matching exactly, and the real Enemy `AiController` GameObject showed
  `enabled=false` while `Enemy2`'s stayed untouched. Explicitly deferred:
  trigger-timing precision beyond the resync safety net, >2-human LAN,
  joiner-side scenario preview, per-scenario civ/map picker (session-1
  gap). See `docs/SESSION_LOG.md`'s matching entry for full detail. This
  closes the Scenario Editor heavy-path epic's last deferred item — next
  is the user's call on another Roadmap Section 5 item.
- **Scenario Editor heavy path, session 4 (richer palette icons) closed
  (2026-09-03)** — picked up at the user's explicit request ("start item
  on richer palette art for scenario editor"), the last cosmetic item
  session 1 flagged as deferred. This project already has real command-
  card icon assets for almost every placeable type (`BuildMenu.cs`'s own
  `build_*`/`train_*` icons under `Resources/UI/Icons/`), so this was a
  wiring task, not new asset sourcing. New
  `ScenarioEditorMenu.AddPaletteIcon` (adapted from `BuildMenu.
  AddCommandIcon`'s own "icon + inset label" shape, retuned for this
  file's smaller 260×24 palette rows) wires icons onto 12 of 13
  `EntitySpawner` types. **`TownCenter` is the one confirmed gap** — no
  `build_towncenter.png` exists anywhere in the project (TownCenter is
  normally auto-spawned, never player-built through any other menu) —
  stays text-only, the same disclosed fallback `BuildMenu.cs` itself
  already uses for Dock/LumberCamp/MiningCamp/Mill. Pure UI-wiring, no new
  branching logic, so no new test (matching `BuildMenu`'s own equivalent);
  full suite confirmed 200/200 unchanged. Live-verified via UnityMCP:
  screenshotted the real palette, all 12 icons render correctly with no
  text overlap, TownCenter renders cleanly text-only. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Next: the user's
  call among the remaining deferred items (per-kind input widgets,
  multiplayer play of a custom scenario), or another Roadmap Section 5
  item.
- **Scenario Editor heavy path, session 3 (saved-scenario browse list)
  closed (2026-09-03)** — picked up at the user's explicit request ("start
  a saved-scenario browse list on MissionSelectMenu"), the last item
  session 1/2 both flagged as deferred. New
  `Assets/Scripts/Core/SavedScenarioLibrary.cs` extracts the scenario-file
  I/O `ScenarioEditorMenu` had inline (`ScenarioFolder`/
  `ListSavedScenarioNames`/`Load`) into one shared source of truth, so both
  the editor and `MissionSelectMenu` read the exact same saved files —
  verified behavior-preserving by re-running the editor's own Save/Load
  flow live after the refactor. `MissionSelectMenu` gained a "Custom
  Scenarios" section (a `ScrollRect`-based row list, same pattern
  `SettingsMenu`'s Key Bindings list and session 2's own Objectives tab
  already establish, since the list is unbounded) with an explicit
  "No saved scenarios yet" empty state. New `ChooseCustomScenario(string)`
  mirrors `ChooseScenario`'s existing shutdown sequence, calling the same
  `CivilizationSetup.BeginCustomScenarioMatch` the editor's own Play
  button already uses. 3 new EditMode tests (200 total, all pass).
  Live-verified via UnityMCP through the real production path: saved 2
  scenarios through the real editor (one with a `SurviveSeconds`
  objective, one placements-only), screenshotted the real Mission Select
  panel confirming both appear correctly, then invoked the real
  `ChooseCustomScenario` and confirmed `ScenarioManager.ActiveScenario`'s
  title/objective matched the saved scenario exactly. Explicitly still out
  of scope: deleting/renaming a saved scenario from this list, row
  metadata (civ/map/placement count), thumbnail art. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Next: the user's
  call among the remaining deferred items (per-kind input widgets, richer
  palette art, multiplayer play of a custom scenario), or another Roadmap
  Section 5 item.
- **Scenario Editor heavy path, session 2 (Objective/Trigger authoring)
  closed (2026-09-03)** — picked up directly from the offer at the end of
  session 1 ("objective/trigger authoring for custom scenarios," the
  user's explicit next request). `MissionCsvLoader`'s existing CSV
  interpreter (`BuildObjectives`/`BuildTriggers`) was split into a typed,
  JsonUtility-serializable row layer (new `ObjectiveRow`/`TriggerRow` +
  `internal BuildObjectivesFromRows`/`BuildTriggersFromRows`), so the CSV
  (light) path and the in-game editor now share one interpreter for the
  same 5-objective/2-trigger-kind vocabulary — zero behavior change for
  existing CSV missions (full pre-existing suite passed unmodified).
  `CustomScenarioData` gained `objectives`/`triggers`/`victoryText`/
  `defeatText`; `ScenarioEditorMenu` gained a second **Objectives** tab
  (a `ScrollRect`-based row list, reusing `SettingsMenu`'s own Key Bindings
  scroll pattern) where an author cycles each row's Kind and fills generic
  Param1-5/Description fields, each with a live hint describing that
  kind's param meaning. `CivilizationSetup.BeginCustomScenarioMatch` now
  calls `ScenarioManager.Begin` with a real `ScenarioDefinition` built
  from the authored rows, but **only when `data.objectives.Count > 0`** —
  calling it unconditionally would have made every session-1 placements-
  only scenario resolve to an instant Victory, since
  `ScenarioManager.EvaluateOutcome()` treats an empty objective list as
  "already complete." 7 new EditMode tests (197 total, all pass). Live-
  verified via UnityMCP through the real production path: authored a
  `PopulationThreshold` objective + `GrantResourceAtTime` trigger through
  the actual editor UI state, Played it, confirmed
  `ScenarioManager.ActiveScenario` correctly wired and `MatchManager.
  Outcome` resolved to Victory via the scripted-mission branch (not
  elimination); separately confirmed the regression case the new gate is
  designed to prevent (a zero-objective scenario stays `Ongoing`,
  `ActiveScenario` stays null, exactly matching session 1's original
  behavior); separately proved Save→Close→re-Open→Load round-trips
  objective/trigger rows and victory text through the real file-based UI
  methods. Explicitly still deferred: per-kind bespoke input widgets, a
  saved-scenario browse list on `MissionSelectMenu`, richer palette art,
  multiplayer/LAN play of a custom scenario. See `docs/SESSION_LOG.md`'s
  matching entry for full detail. Next: the user's call among the
  remaining deferred items, or another Roadmap Section 5 item.
- **Scenario Editor heavy path, session 1 (Placements) closed (2026-09-03)**
  — picked up at the user's explicit request ("start the heavy scenario
  path") right after the light-path session closed. Confirmed 2 real
  forks via AskUserQuestion before coding: in-game runtime editor (real
  UGC, not a Unity EditorWindow), and placements first (starting units/
  buildings per faction — the part that genuinely didn't exist anywhere).
  New `EntitySpawner.cs` (pure extraction of `SaveManager`'s own type-
  string→factory dispatch, shared by both), `CustomScenarioData.cs`/
  `CustomScenarioContext.cs`, `CivilizationSetup.BeginCustomScenarioMatch`
  (deliberately skips `ScenarioManager.Begin` — v1 has no custom
  objectives, so standard Conquest just runs), and a new in-game
  `ScenarioEditorMenu.cs` (palette, click-place/drag-move/right-click-
  delete markers, Save/Load/Play) opened via a new "Create Scenario"
  button on `MissionSelectMenu` — needed zero other UI changes. **A real
  gap caught live, not left unnoticed**: the plan only accounted for 2 of
  3 gated default-spawn components (`TownCenterSpawner`/`AiController`) —
  a live population mismatch (6 instead of 2) after Playing a saved
  scenario traced to a 3rd, `UnitSpawner.cs`, unconditionally dropping 4
  default Workers; fixed with the same opt-out pattern. 12 new EditMode
  tests (190 total, all pass) plus two disclosed EditMode-only limitations
  (building factories and `SpawnUnit("Soldier")` both NRE/hard-error
  outside Play mode for pre-existing reasons unrelated to this session —
  covered by live UnityMCP verification instead). Live-verified the full
  editor→Save→Load→Play loop through the real UI end to end. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Explicitly
  deferred: objective/trigger authoring for custom scenarios, a saved-
  scenario browse list on `MissionSelectMenu`, richer palette art,
  multiplayer/LAN play of a custom scenario — any is a reasonable next
  session on this same epic.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 6 (Scenario Editor) light path
  closed (2026-09-03)** — picked up right after item 5 per the user's
  "start item 6". Reading `ScenarioDefinition.cs` directly (not trusting
  the plan's premise) found a real architecture correction:
  `BuildObjectives`/`BuildTriggers` are `Func<>` delegates, which Unity
  can't serialize into a ScriptableObject — so unlike Tech/Unit/Civ data,
  missions can't be Editor-time-baked; they're parsed into real closures
  **at runtime** instead. New `Assets/Scripts/Match/MissionCsvLoader.cs`:
  a small fixed vocabulary (5 objective kinds, 2 trigger kinds — sized off
  the 3 real hand-coded missions plus 2 shapes the Tutorial roadmap item
  already wants), reading 3 new CSVs as `Resources`-loaded `TextAsset`s.
  `ScenarioRegistry.All` merges CSV-loaded missions with the 3 existing
  hand-coded ones; `MissionSelectMenu.cs` needed **zero changes** (already
  iterates that list). One new real sample mission ("The Muster") proves
  the pipeline through the actual UI. 8 new EditMode tests (180 total, all
  pass) plus live UnityMCP verification of every kind, including the one
  (`DestroyScriptedTarget`) that can't run in EditMode at all
  (`BarracksFactory.Place` NREs outside Play mode — a pre-existing
  factory limitation, not new). **Hit a real environment problem
  mid-session**: new code silently stopped compiling into the assembly
  with zero errors from `read_console`; the user restarted the Unity
  Editor, which surfaced (via `~/Library/Logs/Unity/Editor.log` directly,
  not the MCP console bridge) 2 real compile errors in the new code
  (a missing `using`, then the same "two `DamageType` enums" class of bug
  `WildBoar.cs` hit once before) — both fixed. **New lesson for future
  sessions: if `read_console` shows zero errors but new code isn't taking
  effect, check the real Editor.log file directly before assuming a
  tooling problem** — the console bridge can miss real compile errors.
  See `docs/SESSION_LOG.md`'s matching entry for full detail. This closes
  the last item in the Partial-Elements Fix Plan's own recommended order
  — items 1-6 are all done; Fish Trap (item 5) and the heavy Scenario
  Editor path (item 6) remain explicitly deferred, tracked in
  `docs/Roadmap.md`.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 5 (Renewable Resource — Farm
  depletion) Farm half closed (2026-09-03)** — picked up right after item
  4 per the user's "start item 5". Step 1 (verify, don't assume) found a
  third case the plan doc's own Step 2a/2b binary didn't anticipate: a
  staffed Farm produced Food forever, no cap, no depletion at all —
  categorically different from AoE's real finite/depleting/reseedable
  Farm. Put this finding to the user directly (AskUserQuestion) rather
  than picking a fix silently; confirmed: retrofit real depletion + reseed
  (the larger option, bigger than the item's own "Small" estimate).
  `Farm.cs` now has a real 175-Food capacity (AoE II's own Dark-Age
  value) that depletes as it's worked, plus a Wood-costed reseed (60 Wood
  full cost, matching the Farm's own build cost) using the same
  `ConstructionSite.SpeedMultiplier` diminishing-returns curve
  `Repairable` already reuses. **No `SelectionManager` change needed**:
  `FarmWorker` autonomously switches between harvesting and reseeding
  based on the Farm's own live depleted state, so the existing right-click
  order just does the right thing on its own. 8 new EditMode tests (172
  total, all pass), live-verified via UnityMCP through the real production
  `Tick`/`Update` path — including an unplanned but convincing proof that
  the real system cycled through a full harvest→deplete→reseed→harvest
  loop entirely on its own between verification calls, with nothing
  forcing it. See `docs/SESSION_LOG.md`'s matching entry for the exact
  numbers. Fish Trap stays deferred/asset-blocked, untouched this session.
  Next per the plan doc's own recommended order: item 6 (Scenario Editor —
  recommend the lightweight CSV-authoring path), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 4 (Diplomacy — Tribute) closed
  (2026-09-03)** — picked up right after item 3 per the user's "start item
  4". That item's own first instruction (check the live UI before writing
  a new panel) found the stance UI already fully existed
  (`DiplomacyMenu.cs`, F11, real War/Allied toggle per faction) — only
  Tribute itself was missing. Asked the user directly (AskUserQuestion)
  whether Tribute should be alliance-gated or open to any faction (real
  AoE II's actual rule); confirmed: any faction. New
  `Assets/Scripts/Core/Tribute.cs` (`Tribute.Send`, 20% tax, deliberately
  not gated by `DiplomacyRegistry`), 4 new tribute icon buttons per row in
  `DiplomacyMenu.cs` (flat 50 per click, affordability-gated like
  `BuildMenu`'s Market buttons). 5 new EditMode tests (164 total, all
  pass), live-verified via UnityMCP through the real button `onClick` (not
  just the isolated method) — Player Wood 200→150, Enemy Wood +40 (50 ×
  0.8 tax) — see `docs/SESSION_LOG.md`'s matching entry. Next per the plan
  doc's own recommended order: item 5 (Renewable resource / Farms — verify
  first, then fix if needed), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 3 (Area of Effect / Trample)
  closed (2026-09-03)** — picked up right after item 2 per the user's
  "start item 3". The plan doc flagged a real design decision (reuse
  Siege's full-damage splash as-is, or reduce trample's secondary damage)
  — asked the user directly rather than picking silently; confirmed:
  reduced secondary damage. `MeleeAttacker.SetSplashRadius` gained an
  optional `damageMultiplier` parameter (default 1f — Siege's existing
  single-arg call stays byte-for-byte unchanged, verified by its own test
  suite passing unmodified). `CavalryFactory` wires
  `SetSplashRadius(1.25f, 0.35f)` — a radius below formation spacing (only
  catches units clumped tight around the impact point, not a full
  adjacent rank) at 35% secondary damage (stays minor even stacked on
  Cavalry's existing 1.5x hard-counter bonus vs. Infantry). No new VFX
  needed — the existing per-hit particle burst already fires for trample
  hits, confirmed live. 4 new EditMode tests (159 total, all pass),
  live-verified via UnityMCP through the real `CavalryFactory`/
  `SoldierFactory`/`MeleeAttacker.Tick` production path (real 30-HP
  Soldiers, real armor/CSV stats) — see `docs/SESSION_LOG.md`'s matching
  entry for the exact numbers. Next per the plan doc's own recommended
  order: item 4 (Diplomacy — tribute), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 2 (Victory Conditions) closed
  (2026-09-03)** — picked up right after item 1 per the user's "start item 2".
  That item's own premise was half wrong once checked against the actual
  repo rather than trusted: **Conquest was already fully implemented**
  (`MatchManager.Evaluate`, pre-existing) and the plan's "new art" Victory/
  Defeat splash already existed too (`GameOverScreen.cs`) — neither needed
  building. **Time Limit was the only real gap**, now closed:
  `GameSettings.TimeLimitMinutes` (Off/15/30/45/60, cycled via a new
  Settings row), `MatchManager.EvaluateSkirmishOutcome`/
  `ResolveTimeLimitOutcome` (population tiebreaker, ally-aware, `Draw` on a
  tie — a new 4th `MatchOutcome` value), `GameOverScreen` extended for
  Draw. 9 new EditMode tests (155 total, all pass), live-verified via
  UnityMCP through the real `Update()`/`Time.unscaledTime` path (not just
  the isolated functions) — see `docs/SESSION_LOG.md`'s matching entry for
  the exact repro. **Also fixed a real regression found while touching the
  same file**: `SettingsMenu`'s Key Bindings list had silently overflowed
  its panel background since item 1's own session grew it from 12 to 34
  rows with no layout resize — fixed with a proper `ScrollRect`-based
  scrollable list, screenshot-verified at both scroll extremes. Next per
  the plan doc's own recommended order: item 3 (Area of Effect / Trample
  damage), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 1 (Hotkeys) closed (2026-09-03)**
  — picked up per the user's instruction to read that plan and start item 1.
  The real gap was bigger than "missing keys": only 3 of ~18 `BuildMenu`
  actions had a hotkey at all, and those 3 (`Barracks`/`Dock`/`TownCenter`'s
  own `Update()`-based `trainKey` checks) had a genuine, previously
  unflagged bug — gated only on faction ownership, not on whether that
  specific instance was the *selected* building, so pressing e.g. G trained
  a Worker at every idle Player TownCenter at once. Fixed by centralizing
  hotkey dispatch into `BuildMenu.Update()` (which already tracks
  `SelectionManager.SelectedBuilding`), added 15 new hotkeys covering the
  rest of Barracks/TownCenter/Dock training and research plus Ungarrison,
  registered every binding (including 4 already-functional-but-unlisted
  placement keys and Save/Load/Diplomacy, the latter of which turned out to
  not even be routed through `GameSettings` at all) in `SettingsMenu.Actions`,
  and built the optional F1 hotkey-reference overlay panel (new
  `Assets/Scripts/UI/HotkeyOverlay.cs`) — its own live-verification pass
  caught and fixed a real column-overlap layout bug before calling it done.
  User-confirmed scope via AskUserQuestion/Plan Mode: fix the bug now (not
  deferred), Ungarrison gets a hotkey but the 6 Market buy/sell buttons stay
  click-only (no real AoE-like hotkeys specific trade amounts), include the
  F1 overlay. All 146 EditMode tests pass unmodified; live-verified via
  UnityMCP against the real production path (`CivilizationSetup.BeginMatch`,
  same technique prior sessions established) — the concrete regression repro
  for the bug (2 real Player TownCenters, select one, invoke the hotkey's
  exact handler method, confirm only the selected one trains) is in
  `docs/SESSION_LOG.md`'s matching entry, along with the full per-context
  hotkey table. Next per the plan doc's own recommended order: item 2
  (Victory conditions — Conquest + Time Limit), not started.
- **"Everything else" items scoped (2026-09-03), not implemented** — at the
  user's explicit request ("plan out the items in everything else"), the
  5 lower-priority Roadmap Section 1 stub bullets (Music, Tutorial,
  Profiling, Store/marketing assets, README drift) were each expanded into a
  concrete scope grounded in a fresh read of the actual repo (`SfxPlayer.cs`,
  `MissionObjective.cs`/`MissionTrigger.cs`/`MissionSelectMenu.cs`,
  `README.md` itself), not assumed from prior session-log claims. Key
  findings: 4 of the 5 are genuinely doable now with **no user blocker** —
  Music can self-serve CC0 tracks from Kenney.nl the same way the existing
  SFX pass did (internet access already confirmed working); Tutorial rides
  the already-proven mission-objective system, pure content authoring, no
  new system needed; Profiling has a ready tool
  (`mcp__UnityMCP__manage_profiler`) and just needs someone to actually run
  it against a realistic large-match scenario; README drift is pure
  doc-writing (confirmed the README is badly stale — still describes the
  original single-map/3-civ/no-naval prototype scope and stops its milestone
  list at 26, missing everything since including 5-civ art, naval, LAN
  multiplayer, diplomacy, and the full worker-mechanics-audit feature set).
  **Only Store/marketing assets is a genuine blocker**, and not on
  sourcing/code — it needs an explicit user decision on whether public
  release is even a goal before any scoping can proceed further.
  Recommended order: README drift + Music first (cheapest, zero blocker),
  Tutorial next, Profiling after that, Store assets last pending the
  release-intent conversation. Full per-item detail in Roadmap Section 1's
  "Lower priority" subsection and Section 5 item 6. Docs-only session, no
  code/asset changes, no tests affected.
- **Maurya Tower re-sourced and wired (2026-09-03, Section 1/5 item 7)** —
  closes the last civ-specific-building gap: **45/45 civ-specific buildings now
  complete** across all 5 civs. User supplied a fresh delivery at
  `/Volumes/US/all civ buildings/Maurya/` (folder `Meshy_AI_Ivory_Sentinel_Tower_...`,
  identified unambiguously by its internal Meshy filename). Packed the raw
  separate metallic/roughness textures into a `_metallicSmoothness.png` via a
  scratchpad Python/Pillow script (same approach as the 2026-09-02 Rajput
  session), then wired via the existing `MeshyBuildingImporter`/
  `BuildingMeshDecimator` pipeline unchanged, no code changes needed.
  **Rotation determined empirically, not guessed**: tested all 6 cardinal-axis
  candidates against the raw FBX's own world bounds first (per
  `feedback_tower_rotation_correction.md`); this asset ties Y-tallest on
  `X+90`/`X-90` (its raw up-axis is local Z, unlike prior Towers' local Y) —
  screenshotted both tying candidates against the folder's own Watchtower
  concept art before committing: `X+90` was upside-down (flared cap at the
  visual bottom), `X-90` matched the concept art exactly (stepped lion-guarded
  base, pillared shaft, crenellated parapet, domed cap), also confirmed via a
  true top-down shot (compact square footprint, not the elongated one the wrong
  candidate gives). Solved algebraically for the prefab's baked
  `modelRotationCorrection` given the fixed civ-blind
  `ImportRotationCorrections["Tower"]` parent stomp
  (`Quaternion.Inverse(parentStomp) * Euler(-90,0,0)` = `Euler(0,-90,90)`), then
  verified via the real `BuildingModelFactory.Spawn` path post-import (world
  height matched the computed target to the last decimal). Scale: worker height
  re-measured fresh (1.960884, vs. the previously-documented 1.902692 baseline)
  and the established Maurya ratio hierarchy's Tower value (8.00) scaled
  proportionally to 8.245 rather than reused blind. Decimated 1,979,816 →
  500,000 tris via `BuildingMeshDecimator`, matching every other building's
  already-validated target. All 146 EditMode tests pass unmodified — pure
  asset-pipeline work, no test changes needed. See `docs/SESSION_LOG.md`'s
  matching entry for full detail.
- **Rajput TownCenter/Barracks re-sourced, Rajput Tower rotation fixed
  (2026-09-02/03, Section 1/5 item 7)** — closes the 2 findings the mesh-
  decimation session flagged but didn't fix (see this file's own note below).
  User supplied correct source deliveries for both buildings in an external
  folder; wired via the existing `MeshyBuildingImporter`/`BuildingMeshDecimator`
  pipeline unchanged, no code changes needed. Both were lying on their back at
  import (Meshy's usual export convention) — orientation confirmed via 3/4-view
  and top-down screenshots against each building's own concept art before
  committing to `Quaternion.Euler(-90,0,0)` for both, not assumed. **TownCenter
  needed a second pass**: the folder's first delivery turned out to be the
  wrong file (the user's own upload mistake), caught and corrected mid-session
  before this was logged as done — re-imported/re-verified from scratch once
  the correct delivery was in place (final: Barracks height 4.34, TownCenter
  height 11.28, matching the established Rajput ratio hierarchy). Both
  decimated to ~500,000 tris (from ~1.9-2.0M raw), screenshot-confirmed clean.
  **Also found and fixed a real, pre-existing adjacent bug, flagged live by the
  user from the running scene**: Rajput's Tower (untouched by this session's
  own changes) was spawning upside-down — its 2026-08-31-session-baked child
  rotation correction (`Euler(0,90,0)`) turned out to be the wrong one of the
  Y+90/Y-90 tying-bounds pair `feedback_tower_rotation_correction.md` already
  flags as a real trap. Root-caused (tested the raw FBX standalone against the
  Watchtower concept art, then solved algebraically for the required child
  rotation given `BuildingModelFactory`'s fixed civ-blind parent stomp) and
  fixed directly on `Tower.prefab`, re-verified via the real
  `BuildingModelFactory.Spawn` path. All 146 EditMode tests pass unmodified
  throughout. Rajput is now genuinely 9/9 (model + correct orientation);
  44/45 civ-specific buildings complete overall (Maurya Tower is the one
  remaining gap, a separate pre-existing 0-byte-source issue). See
  `docs/SESSION_LOG.md`'s matching entry for full detail.
- **Building mesh decimation pass closed (2026-09-02, Section 1/5 item 17)**
  — `UnityMeshSimplifier` package + new `Assets/Editor/BuildingMeshDecimator.cs`
  decimated 43/45 civ-specific buildings (Rajput TownCenter/Maurya Tower's
  pre-existing 0-byte-source gaps correctly skipped) from ~1.7-2.0M
  un-decimated triangles down to ~500,000 each. **The real finding**: the
  spec's literal 8,000-20,000 tri target, and even the 30,000-60,000
  fallback, both proved unreachable on this raw un-retopologized Meshy
  geometry without either visible carved-relief artifacts (proven via
  screenshot on Chola TownCenter at the tighter target) or computationally
  impractical simplifier settings — one tuning attempt (raising
  `VertexLinkDistance`) pegged the Editor at 99% CPU for 25+ minutes with no
  completion and no way to cancel a synchronous call, resolved by killing
  and relaunching Unity at the user's explicit instruction. 500,000 tris/
  building is the real, evidence-backed, screenshot-verified-clean target
  (~3.65x reduction, a full base drops from ~17M+ to ~4.5M triangles). Also
  fixed a genuine Unity `AssetDatabase` caching bug hit along the way
  (`Resources.Load`/`AssetDatabase.LoadAssetAtPath` served a stale prefab
  graph with a null mesh after a re-run, even though the saved prefab was
  correct on disk — fixed via an explicit `AssetDatabase.ImportAsset(...,
  ForceUpdate)` after each save). New `BuildingPolycountTests.cs` regression
  test (146 EditMode tests total, up from 145), all pass. **Two findings
  flagged, not fixed (out of scope for this item)**: spawning Rajput
  TownCenter's shared-fallback model produces a `MeshFilter` with a null
  `sharedMesh` (pre-existing, unrelated to mesh decimation); and — raised
  mid-session by the user with a reference image — Rajput Barracks' actual
  sourced model (a small boxy shape, confirmed already present in the
  original un-decimated FBX, so not caused by this session) doesn't match
  the grand multi-turret courtyard-fort concept art the user expects —
  looks like a wrong/mismatched asset was sourced/identified in an earlier
  session, a real asset-sourcing gap for a future session, not a rotation
  or mesh-processing bug (a separate "tilted sideways" concern raised in the
  same exchange was checked directly and ruled out — every transform in the
  hierarchy is identity, confirmed upright via a true ground-level
  front-elevation shot; the original angled screenshot's steep camera angle
  was just foreshortening the roofline, the same parallax illusion this
  project's history has hit before). Also found 6 additional
  `Maurya/_Source/*.mat` files plus the already-documented Cow/Palm2 fix
  were pending-but-unsaved from an earlier session and got flushed to disk
  by this session's own `AssetDatabase.SaveAssets()` calls — left unstaged,
  not bundled into this session's commit (not this session's work to claim
  or decide about). See `docs/SESSION_LOG.md`'s matching entry for full
  detail.
- **Crusader Knight body-swap sourcing spec written (2026-09-02), not started**
  — user picked up this item after the mesh-decimation one was scoped by a
  concurrent session; asked to scope only, not implement (nothing to
  implement yet — it's still blocked on new source model files). Spec
  written into Roadmap Section 1's matching item, derived directly from the
  2026-08-28 rig-compatibility verification's own findings so a replacement
  doesn't repeat the same problems blind: FBX preferred (glTF works too, per
  that session's proof `AvatarBuilder.BuildHumanAvatar` builds a valid Avatar
  from a hand-authored `HumanDescription` with no Blender step), any standard
  Humanoid biped rig, modeled in meters near the scene's ~1.9-unit worker
  height (the deleted models' `(2.54,2.54,2.54)` baked Hips scale was the
  actual root cause of the ~247x `humanScale` anomaly — cleaner sourcing
  avoids that fix entirely), no embedded animation needed, and either no
  sculpted hand-held weapons (reuse `WeaponAttachment.AttachToBone`, the
  precedent already used for the 3 unique units) or weapon meshes that are
  separable/re-parentable to a hand bone rather than static scene-root props.
  Also flagged a normal real-time polycount range, tying back to the same
  day's building-polycount audit finding. Docs-only session, no code/asset
  changes. Still needs the user to actually source a replacement file before
  any wiring work can start.
- **Building mesh decimation pass scoped (2026-09-02), not started** — the
  visual-audit session's headline finding (every civ-specific building is
  ~1.7-2.0M un-decimated triangles) needs its own dedicated session per the
  user's explicit request rather than being folded into the audit itself.
  Full plan in Roadmap Section 1/5 item 17: no Blender available in this
  environment (checked directly), no built-in Unity mesh-simplification API
  either (checked via reflection) — plan is to add the
  `UnityMeshSimplifier` package (MIT, pure C#, git-fetchable — internet
  access confirmed working) and a new `Assets/Editor/BuildingMeshDecimator.cs`,
  proof-of-concept on Chola TownCenter first to pick a real target ratio
  (the spec's 8,000-20,000 tri target may be too aggressive for the ornate
  carved-relief buildings — a judgment call, not a fixed number), then batch
  the remaining 44 with a screenshot-verified sample per civ, plus a new
  EditMode regression test asserting building polycount stays under a
  ceiling going forward (zero such coverage existed before this finding).
- **Civ-by-civ visual quality audit (2026-09-02)**, user-requested, against
  Roadmap Section 4.1's AoE IV visual standard. Live-measured (not estimated)
  via UnityMCP: every civ-specific building's real triangle count/texture
  resolution/shader, plus side-by-side style comparisons. **Headline finding:
  every civ-specific building is ~1.7-2.0 million un-decimated triangles**
  (100-250x over the 8,000-20,000 tri spec) — texture resolution and shader
  workflow both pass cleanly, polycount is the one real failure, and it was
  never caught by any prior per-civ import session (those checked rotation/
  scale, not polycount). Style differentiation mostly works (Rajput/Maratha
  read as clearly distinct traditions) but Chola/Vijayanagara's TownCenters
  read as the same architectural family, and Maurya's gilded-dome TownCenter
  is visually striking but reads more Mughal/colonial than authentically
  Mauryan. Confirmed live: the shared Human Character Dummy soldier body is
  the single most visually obvious gap (5 civs = identical mannequin, tint
  only). Full findings in Roadmap Section 4.2 (rewritten, replacing stale
  pre-civ-art text) and `docs/SESSION_LOG.md`. Pure investigation, no code
  changes — a mesh-decimation pass for the building polycount problem is a
  real candidate for a future session (flagged, not started).
- **Ad hoc bug fix (2026-09-02, same day as the LAN transport session below,
  reported from a live Play mode screenshot)**: livestock Cow rendered fully
  pink and one Palm2 tree rendered grey/flat. Two different root causes, not
  one: the Cow's FBX had a dangling material GUID remap (fixed by creating a
  real `M_Cow_URP.mat` on the pack's own textures); the Palm2 materials had
  the correct URP shader but empty texture slots, because a same-day "remove
  unused assets" cleanup deleted 5 texture files a still-in-use Palm2
  material actually needed (recovered via `git checkout` from the deleting
  commit's parent — unlike the Crusader Knight files, these were still
  reachable). Live-verified via UnityMCP screenshots; no test changes needed
  (pure asset data). See `docs/SESSION_LOG.md`'s matching entry.
- **This session (2026-09-02) built a real LAN transport MVP for AoE-Parity
  Phase 5 (multiplayer determinism)**, at the user's explicit instruction not
  to leave it deferred any longer (confirmed scope: LAN-only, 2 human
  players; online play/matchmaking deferred to "end of the project"). New
  `Assets/Scripts/Multiplayer/` files: `NetworkId` (deterministic
  spawn-order integer identity for units/buildings, hooked into
  `Unit.OnEnable`/`Building.OnEnable`), `Wire/NetMessage.cs` (pure-data DTOs
  for Move/Train/Build/Attack/StateHash/ResyncSnapshot/Heartbeat/Hello),
  `CommandSerializer` (real `Command` ↔ wire DTO conversion), `LanTransport`
  (raw TCP host/join, length-prefixed JSON, background threads → main-thread
  queue), `NetworkDriver` (drains that queue into `CommandBus`/
  `NetworkDesyncMonitor`/`DesyncRecovery`), `NetworkMatch` (`LocalFaction` —
  the "who am I" seam that didn't exist before; `RemoteMaxAckedTick` — the
  actual lockstep gate), `NetworkDesyncMonitor` (real cross-peer `StateHash`
  exchange/comparison, host-authoritative resync), and `LanMatchMenu`
  (minimal runtime-built uGUI Host/Join panel — a disclosed visual-only
  compromise, not a functional gap). `SimClock` now genuinely gates tick
  advancement on the remote peer's acknowledged tick when a network match is
  active (single-player completely unaffected —
  `NetworkMatch.IsActive` stays false). Every hardcoded `FactionId.Player`
  "who's clicking" reference across `SelectionManager`/`BuildMenu`/
  `BuildingPlacer` (~40 occurrences) now reads `NetworkMatch.LocalFaction`
  instead, and each of the 4 order-origination sites (Move/Attack/Train/
  Build) sends the matching wire message when a network match is active. 18
  new EditMode tests (145 total, up from 127), including real two-socket TCP
  loopback tests (`LanTransportTests.cs`). **Live-verified far beyond the
  EditMode tests**, via UnityMCP: two real `LanTransport` TCP peers (a real
  host + a real second socket standing in for the remote human) proved the
  lockstep gate genuinely stalls/unblocks on real elapsed time, a local Move
  order serializes correctly to the wire, a remote-originated Move order is
  received/resolved/enqueued/executed against a real spawned unit (moving it
  to the exact remote-specified destination), the real `StateHash` is
  exchanged, and — the key proof — a **genuinely forced desync** (a
  deliberately wrong hash sent from the "remote" socket) was correctly
  detected and triggered a real `SaveManager.Capture()` snapshot (3987 bytes)
  sent back over the actual TCP connection. This closes 2 of Phase 5's 3
  remaining transport-blocked checklist items (real cross-peer desync
  detection; validating resync under real network conditions) with genuine
  live evidence, not the earlier single-process synthetic test. **Not done,
  explicitly still open**: true cross-machine NavMeshAgent/physics
  determinism testing — this session's own verification used two real
  sockets within one machine/process, not two separate physical
  machines/OSes; that needs the user's own second machine to actually run.
  Not built (explicit user instruction to defer): online play/matchmaking/
  NAT traversal, reconnect-after-drop, >2 players, spectators. See
  `docs/SESSION_LOG.md`'s 2026-09-02 "Phase 5: real LAN transport MVP" entry
  and `docs/AOE_PARITY_EXECUTION_PLAN.md`'s Phase 5 section for full detail.
- **Earlier the same day (2026-09-02), started on the Crusader Knight
  body-swap item
  (Section 1/5.10 — fix the ~247x `Animator.humanScale` anomaly and re-parent
  sword/shield/staff props via `WeaponAttachment`) but found it genuinely
  blocked**: its source glTF files (`Assets/importedmodels/Item47/TemplarKnight`,
  `.../HospitalierKnight`) were deleted the same day in the "Remove
  confirmed-unused asset scrap" commit — not recoverable from git history in
  usable form. Flagged to the user rather than silently restoring or
  proceeding; user chose to stop that item and picked two unblocked follow-ups
  instead, both closed this session (see `docs/SESSION_LOG.md`'s matching
  entry for full detail): **(1)** removed the 2 other pre-existing broken civ
  building models — Rajput TownCenter and Maurya Tower, whose raw source FBX
  files were confirmed 0 bytes in every commit that ever touched them (no good
  version to recover) — so both now fall through to the shared model via
  `BuildingModelFactory`'s existing fallback chain instead of silently
  rendering nothing (43/45 civ-specific models complete, was stated as 45/45;
  see Roadmap Section 5 item 7). Live-verified via UnityMCP: spawned both,
  confirmed the civ-specific `Resources.Load` paths are now null, and the
  spawned models are real clones of the shared imported assets
  (`TownCenter(Clone)`/`scene(Clone)`), not the procedural-shape last-resort
  fallback. **(2)** Closed both adjacent findings from the 2026-08-28 Naval
  balance session: `WarGalleyFactory` now applies
  `UpgradeProgress.ClassArmorBonus`/`ClassDamageBonus(UnitClass.Naval)`,
  matching every land factory's convention (live-verified via reflection:
  armor +0.5/+0.5, damage bonus +1 once a Naval class tier is advanced —
  currently inert in live play since nothing yet wires up Naval per-class
  research UI, a separate pre-existing gap); `BoatAttacker`'s 1.5s vs
  `MeleeAttacker`'s 1.0s attack interval was reviewed and left unchanged,
  now documented as a deliberate tradeoff for Naval's range/speed edge
  rather than an unexplained inconsistency. All 127 EditMode tests pass
  unmodified for both parts (additive-only changes, no existing test
  asserted the old values). The Crusader Knight item itself remains open,
  now explicitly blocked on re-sourcing the 2 model files — not resumable
  as pure code/wiring work until new assets exist.
- **Earlier session (2026-09-02) closed AoE-parity Phase 2.3 (Siege splash/area
  damage, Roadmap Section 1)** — the item logged by the prior Phase 2 combat
  audit ("formations are cosmetic against Siege, it has no splash damage").
  Implementation (`MeleeAttacker.SetSplashRadius`, new `HostileFilter.cs`,
  `SiegeFactory` wiring 2.25) was already on disk at session start; this
  session verified it via Unity MCP. EditMode: 5 new `SiegeSplashTests.cs`
  needed a fix mid-session — mixing `LogAssert.Expect` (pre-existing
  SetDestination error) with `ignoreFailingMessages` stopped the latter from
  suppressing `Attackable.TakeDamage`'s VFX-destroy log in this Unity Test
  Framework version, needing an explicit `Expect` per hit instead. 127
  EditMode tests total, all pass. **Live Play Mode verification (via
  UnityMCP, real `SoldierFactory`/`GroupFormation`/`SiegeFactory`, one real
  attack cycle through the production `Tick()` path) found a second real
  bug, not in this item's own diff**: the acceptance check (Staggered should
  take fewer splash casualties than Line) initially showed the opposite —
  Staggered took double Line's casualties (4/8 hit vs 2/8) at the shipped
  2.25 splash radius. Root cause was a pre-existing geometry bug in
  `GroupFormation.StaggeredOffset` (paired consecutive units only half a
  spacing apart in depth, tighter than Line's own full-spacing rank
  neighbors) — proved no splash-radius value could fix it, asked the user
  per protocol rather than silently expanding scope, user approved fixing it
  this same session. Fixed by keeping each unit's lateral position identical
  to Line's own and staggering depth alone by 1.5x spacing (a Pythagorean
  choice so a lateral neighbor's diagonal distance clears the splash
  radius). Re-verified live: Staggered now takes 1/8 hit vs Line's 3/8 (14
  vs 42 total damage) for the same attack — a clear 3x reduction, matching
  the acceptance criterion. See `docs/SESSION_LOG.md`'s 2026-09-02 Phase 2.3
  entry and Roadmap Section 1/Section 6 for full detail. One scoped commit
  covers `HostileFilter.cs` (new), `BuildingAttacker.cs`, `MeleeAttacker.cs`,
  `SiegeFactory.cs`, `GroupFormation.cs`, and `SiegeSplashTests.cs` (new).
- **Prior session (2026-09-01, same day as the items below) worked
  `AOE_PARITY_EXECUTION_PLAN.md`** (a new companion doc handed in mid-session,
  not previously part of this roadmap) instead of the item queued below —
  Phase 1 (Player Color System) was investigated and **deferred** (its own
  premise assumes an arbitrary-N player-slot system this codebase doesn't
  have — `FactionId` is exactly 3 fixed factions, Player/Enemy/Enemy2, not a
  multiplayer lobby; tied to Phase 5's existing "real transport doesn't exist
  yet" blocker rather than deleted from the plan), but a real live bug
  surfaced while checking it was fixed: `CivilizationSetup` could silently
  assign the same civilization to two of the three fixed factions (the
  scene's own `aiCivilization` default collided with an ordinary player pick
  of Vijayanagara) — fixed with a deterministic dedup guard
  (`CivilizationSetup.ResolveDistinctCivilization`). Phase 2 (Combat
  calibration) items 2.1/2.2/2.3 are logged as one batch in Section 1 below:
  2.1 closed (raised `CombatBonus.Multiplier(Archer,Cavalry)` 1.5x→2.0x after
  a numeric audit found the old value was a near coin-flip, not a real hard
  counter — Infantry→Archer/Cavalry→Infantry audited and left unchanged,
  already decisive), 2.2/2.3 audited and logged as new open items (no
  soft-counter mechanic exists; Siege has no splash damage so formations are
  cosmetic against it), not implemented. **Phase 3.1 (dedicated
  resource-specific drop-off buildings) closed the same day**, at the user's
  explicit go-ahead — see Roadmap Section 1's matching item and
  `docs/SESSION_LOG.md`'s Phase 3.1 entry for full detail: new Lumber
  Camp/Mining Camp/Mill buildings, each a valid `Gatherer` drop-off only for
  its own resource type via a new `Gatherer.AcceptsDropOff` rule
  (`TownCenter` stays the universal drop-off), full `BuildingPlacer`/
  `BuildMenu` wiring (3 new hotkeys J/U/P, 3 new scene buttons duplicated
  from `DockButton`), 8 new EditMode tests (99 total, all pass), and a live
  Play-mode verification (via UnityMCP, bypassing the mission-select flow
  that normally gates gameplay-entity spawn) proving a Wood-carrying worker
  routes to a farther-but-valid Lumber Camp over both a nearer wrong-type
  building and a much-farther TownCenter — confirmed via reflection on
  `Gatherer`'s private `_dropOff` field, since the aggregate Wood-stockpile
  number alone wasn't trustworthy evidence (an unrelated passive-income tick
  confounded it). **Asked the user directly whether they wanted Phase 3.2
  (team-bonus/alliance economic stacking) at all before proposing anything,
  per instruction — they confirmed yes**, explicitly wanting the project's
  systemic depth to reach AoE IV's level. Also received a new standing
  instruction this session: always flag when a building/character task needs
  a real art asset/model, rather than only noting a procedural-fallback gap
  in the session log (saved to cross-session memory; retroactively flagged
  Phase 3.1's own Lumber Camp/Mining Camp/Mill procedural silhouettes against
  this rule). **Phase 3.2 closed the same day**, via Plan Mode (approved
  before implementation, per protocol — this touched 5 separate gameplay
  systems across 6 files) — see Roadmap Section 1's matching item and
  `docs/SESSION_LOG.md`'s Phase 3.2 entry for full detail: new
  `TeamBonus.cs` hand-written hook (same bespoke-per-civ convention as
  `UniqueTechDefinition`/`RajputDefianceHook` — the existing scaffolded
  `CivilizationDefinition.teamBonus` `StatModifier` field turned out to be
  dead code, never read at runtime, since Building-targeted bonuses aren't
  representable in the generic `UnitCategory` schema) shares a diluted
  version of each civ's own unique-tech identity with every
  `DiplomacyRegistry`-allied faction, unconditionally: Maurya allies get
  -25% Wood on Houses, Vijayanagara allies get +15% Wall/Gate/Tower HP,
  Rajput allies get +1 flat Cavalry damage, Maratha allies get +10% Cavalry
  move speed, Chola allies get a narrowed +/-5-point Market spread. 5 new
  EditMode tests plus one added to `CivPassiveBonusTests.cs` (105 total, all
  pass), and all 5 bonuses live-verified in Play mode via UnityMCP with
  exact before/after-alliance A/B comparisons (Wall HP 250→287.5, Cavalry
  damage 6→7, Cavalry speed 6.5→7.15, Market sell/buy 0.70/1.30→0.75/1.25),
  including the negative/self-exclusion case (a civ's own building never
  double-counts its own team bonus via the alliance path). **Phase 4.2
  (worker self-defense/cross-awareness, the last open worker-mechanics-audit
  item) closed 2026-09-01** in a follow-on session that continued from code
  already saved on disk (a prior session had written the implementation but
  never run the test suite or live-verified it) — see Roadmap Section 1's
  matching item and `docs/SESSION_LOG.md`'s Phase 4.2 entry for full detail:
  new `Attackable.OnDamaged` event, `CombatResponse` (Fight/Flee) enum, and
  `WorkerCombatResponseDefaults` per-civ lookup (same bespoke-hook convention
  as `TeamBonus`) — every civ's Workers now auto-fight back when attacked
  mid-gather except Maratha's, which flee (guerrilla identity, consistent
  with its other bonuses). This session's own verification pass found and
  fixed 3 real latent bugs unrelated to the feature's own design: an
  ambiguous `DamageType` reference in `WildBoar.cs` (a second, unrelated
  global-namespace `DamageType` enum already existed in `UnitDefinition.cs`,
  and C# resolves an unqualified name against the enclosing global namespace
  *before* `using` directives — this had silently blocked EditMode
  compilation, which is why `run_tests` was returning 0 tests with no
  visible error until the actual Unity `Editor.log` was checked directly),
  and `UnitMover`/`MeleeAttacker` both caching a sibling component in
  `Awake` instead of lazily (the same "Awake doesn't run synchronously right
  after AddComponent" gotcha already documented below for
  `ConstructionSite`/`Repairable`, newly exposed because no prior EditMode
  test had driven `Gatherer.GatherFrom` end-to-end). 12 new EditMode tests,
  117 total, all pass. Live-verified both responses in Play mode via
  UnityMCP through the real production event path (not a test shortcut): a
  Maurya Worker closed a real ~4-unit NavMesh-pathed gap down to 0.21 units
  onto its attacker; a Maratha Worker under the identical setup increased
  its real tracked distance from 19.9 to 25.9 units, never engaging.
- **Consolidation pass done 2026-09-01**: the companion plan doc turned out
  to genuinely exist (`~/Downloads/AOE_PARITY_EXECUTION_PLAN.md`, not "loose
  chat text" as the prior status note here guessed) — copied into
  `docs/AOE_PARITY_EXECUTION_PLAN.md` so it's actually reachable by a future
  session, and Roadmap Section 6 ("AoE-Parity Execution Plan Status") now
  summarizes every phase's real status, cross-checked against the actual repo
  (grepped file/method existence directly, not trusted from session-log
  claims). Confirmed: Phase 1 deferred (documented reason, tied to Phase 5),
  Phase 2 closed (Archer→Cavalry 1.5x→2.0x, with the real before/after HP%
  numbers), Phase 3.1 closed, Phase 3.2 closed (user-confirmed), Phase 4.1 —
  which is the same item as this list's own item 12 (General garrisoning),
  the plan folds it in rather than introducing a new one — closed, Phase 4.2
  closed. Read Roadmap Section 6 for the full writeup, not this summary.
- **Phase 5, doable-now part closed 2026-09-01** (via Plan Mode, approved
  before implementation, per protocol): the investigation's own finding —
  `BuildingPlacer.TryConfirmPlacement` was the one remaining Player-input
  path bypassing `CommandBus`'s lockstep input-delay queue — is now fixed.
  New `BuildCommand.cs` (same delegate shape as `TrainCommand`);
  `TryConfirmPlacement` now only pre-checks + enqueues, with the real
  resource deduction + `Factory.Place` moved into a new `ExecuteBuild`
  that re-validates at execute time (mirrors `Barracks.RequestTrain`'s own
  re-check convention). `CanAfford`/`IsClearForKind`/`CurrentFootprint`
  refactored to take an explicit `BuildingKind` param instead of the
  mutable `_kind` field — a real correctness fix, not style, since the
  command captures a kind that could differ from `_kind` by execute time.
  Also added the requested self-consistency test:
  `CommandBus.ExecuteTick`/new `EnqueueAt` made `internal` for direct
  EditMode testability, 4 new tests in `CommandBusDeterminismTests.cs`
  proving "same inputs → same state" through real `CommandBus`+`StateHash`
  (121 EditMode tests total, up from 117) — hit and fixed a real
  test-authoring bug along the way (`Unit.OnEnable()` doesn't fire
  synchronously after `AddComponent<Unit>()` in EditMode, the same gotcha
  `BuildingAttackerTests` already documents, which silently made the first
  draft's negative case pass for the wrong reason). Live-verified in Play
  mode via UnityMCP through the real production path (real match started,
  real `Physics.Raycast`, real `SimClock` ticking): Wood/House count
  unchanged immediately after the click despite a real `BuildCommand` being
  enqueued, then correctly deducted/spawned ~2 real seconds later. See
  Roadmap Section 6 and `docs/SESSION_LOG.md` for full detail, including two
  environment quirks hit and worked around (Age-gated `BeginPlacementBarracks`,
  stale/off-screen cached `Input.mousePosition`), neither caused by this
  session's changes.
- **Phase 5's doable-now scope fully closed 2026-09-02** (via Plan Mode,
  approved before implementation): resync-on-desync logic, the last item
  with any non-transport-blocked work in it. `StateHash` wired to
  `SimClock.OnTick` (genuinely live for the first time — previously zero
  call sites anywhere), `SaveManager.Capture`/a new
  `ApplySnapshotToRunningMatch` extracted for reuse outside the file-based
  save/load flow, new `DesyncRecovery.Apply` as the transport-facing entry
  point. **Found and fixed a real, pre-existing bug while live-verifying,
  not before it**: `SaveManager.Capture()` crashed with a
  `NullReferenceException` in any standard (non-3rd-faction) match — Enemy2's
  `ResourceStockpile` is scene-authored but never active without
  `enableThirdFaction`, and this would have crashed the existing F5
  quicksave feature too, not just this new code. 2 new EditMode tests (123
  total, up from 121) after working through several genuine EditMode-only
  artifacts (documented in full in `docs/SESSION_LOG.md` — `Unit.OnEnable`/
  `Destroy()` not firing/taking-effect synchronously, `FindObjectsByType`
  not preserving creation order which briefly looked like a real recovery
  bug before direct debugging cleared it, and `LogAssert.ignoreFailingMessages`
  not suppressing a specific Editor-only error in this UTF version). Live-
  verified in Play mode via UnityMCP against a real running match: real
  perturbation, real `StateHash` divergence, real reconvergence after
  `DesyncRecovery.Apply`. Also hit and recovered from a real mid-session
  mistake, unrelated to the feature: an EditMode cleanup script accidentally
  deleted the Main scene's own real `ResourceStockpile` instances — caught
  immediately, fixed by reloading the scene from disk (nothing had been
  saved, fully recoverable). See Roadmap Section 6 and `docs/SESSION_LOG.md`
  for full detail.
- Currently on: **Scenario Editor heavy path, session 6 (per-kind bespoke
  input widgets) closed (2026-09-03)** — see this file's own bullet above
  for full detail. **This closes the entire Scenario Editor heavy-path
  epic** — every item from session 1's original deferred list is now
  done. A real, separate `RectMask2D` rendering bug was found (not fixed)
  during this session's own verification and flagged via `spawn_task` for
  a dedicated follow-up (`task_545a0590`) — see this file's own bullet
  above. Before that: **Scenario Editor heavy path, session 5
  (multiplayer LAN play of a custom scenario) closed (2026-09-03)** — see
  this file's own bullet above for full detail; also fixed a real
  adjacent pre-existing bug found live (Enemy `AiController` running
  during real 2-human LAN matches). Before that: **Scenario Editor heavy
  path, session 4 (richer palette
  icons) closed (2026-09-03)** — see this file's own bullet above for
  full detail. Before that: **Scenario Editor heavy path, session 3
  (saved-scenario browse list) closed (2026-09-03)** — see this
  file's own bullet above for full detail. Before that: **Scenario Editor
  heavy path, session 2 (Objective/Trigger authoring) closed
  (2026-09-03)** — see this file's own bullet above for full detail.
  Before that: **Scenario Editor heavy path, session 1 (Placements)
  closed (2026-09-03)** — see this file's own bullet above for full
  detail. Before that: **item 6 (Scenario Editor) light path
  closed (2026-09-03)** — see this file's own bullet above for full
  detail. That closed the plan doc's entire recommended order (items 1-6
  all done); remaining deferred sub-items (Fish Trap) are tracked in
  `docs/Roadmap.md` — see this file's own "Lower priority" Roadmap notes
  and the "Everything else" items scoped 2026-09-03. Before that:
  **item 5 (Renewable Resource — Farm depletion) Farm half closed
  (2026-09-03)** — see this file's own bullet above for full detail.
  Before that: **item 4 (Diplomacy — Tribute) closed (2026-09-03)** — see
  this file's own bullet above for full detail. Before that: **item 3
  (Area of Effect / Trample) closed (2026-09-03)** — see this file's own
  bullet above for full detail. Before that: **item 2 (Victory
  Conditions) closed (2026-09-03)** — see this file's own bullet above
  for full detail. Before that: **item 1 (Hotkeys) closed
  (2026-09-03)** — see this file's own bullet above for
  full detail. Before that:
  **"Everything else" items scoped (2026-09-03)** — Music,
  Tutorial, Profiling, Store/marketing assets, and README drift each given a
  concrete scope in Roadmap Section 1; see this file's own bullet above for
  full detail. Nothing implemented yet there — a future session should pick
  README drift or Music (both zero-blocker, cheapest) unless the user says
  otherwise. Before that: **Maurya Tower re-sourced and wired (2026-09-03) —
  closes the
  last civ-specific-building gap, 45/45 complete**, see this file's own bullet
  above for full detail. Before that: Rajput TownCenter/Barracks re-sourced and
  Rajput Tower's rotation bug fixed (2026-09-02/03) — both findings flagged by
  the mesh-decimation session, closed. The mesh decimation pass itself (Section 1/5 item 17) closed
  2026-09-02 — see Roadmap Section 1/5 for full detail (44/45 buildings
  decimated to ~500,000 tris each, real target-ratio finding, new regression
  test). Otherwise: Section 5 items 1-9 and
  11-15 are all done — **the worker-mechanics-audit is fully closed** — and
  `AOE_PARITY_EXECUTION_PLAN.md`'s Phases 1-4 are fully resolved (see the
  consolidation note above), **including item 2.3 (Siege splash/area damage
  vs. formations), closed 2026-09-02** — see that session's own bullet above
  for the full writeup. **Phase 5 (item 16) — the LAN transport MVP closed
  2026-09-02** (see this session's own bullet above for full detail): real
  cross-peer desync detection and resync-under-real-network-conditions are
  now genuinely done, live-verified over real TCP sockets. **Only remaining
  Phase 5 item**: true cross-machine NavMeshAgent/physics determinism
  testing, which needs the user's own second physical machine — nothing
  further Claude Code can do on that specific item alone. Online
  play/matchmaking/NAT traversal deferred per explicit user instruction
  ("at the end of the project"). **Both Naval balance follow-up
  findings closed 2026-09-02** (see this session's own bullet above) — no
  longer an open item. **Item 7 (civ-specific building models) is now
  45/45, fully complete** — all 5 civs are 9/9 (Rajput's TownCenter and
  Barracks re-sourced 2026-09-02/03; Maurya's Tower re-sourced 2026-09-03, see
  this file's own bullet above for both).
  `Assets/Editor/MeshyBuildingImporter.cs` remains reusable for any future
  civ-model work, but every lesson below stays load-bearing: never trust bounds
  alone (an angled `game_view` screenshot at identity can *look* upright via
  parallax even when the model is lying flat on its back — this shipped once in
  Maratha's own session, caught only by the user from a live screenshot; a true
  top-down + front-elevation shot, or the vertex base/tip density check for
  wide/sprawling shapes, is what actually catches it), and when two rotation
  candidates tie on Y-tallest bounds (as with any `ImportRotationCorrections`-keyed
  asset like Tower), always visually confirm **both** tying candidates against
  reference art, not just one. A **"Per-civ soldier visual differentiation"**
  initiative started 2026-09-01 (Roadmap Section 1, supersedes the old "wire in
  Crusader Knight body" item) had its session 1 (tint-gap fix + scoping) closed
  the same day: the base-body decision is now made deliberately
  (**keep the current Human Character Dummy, don't swap** — see below), and the
  Maurya/Maratha untinted-white tint bug is fixed. **All 4 worker-mechanics-
  audit items — Repair, General garrisoning, resource-specific drop-off
  buildings, and worker self-defense/cross-awareness — are now closed as of
  2026-09-01** (see below and Roadmap Section 1). Remaining real options for
  a future session: the
  **Crusader Knight body swap itself — now explicitly blocked, not just
  deferred** (rig-compatibility was verified positive in a concurrent
  2026-08-28 session, but this session found its 2 source glTF files
  — `Assets/importedmodels/Item47/TemplarKnight`/`HospitalierKnight` — were
  deleted the same day as "confirmed-unused asset scrap" and are not
  recoverable from git history in usable form; the user chose to stop rather
  than restore/re-source when told. Needs new source models (same files
  restored, or replacements) before the ~247x `humanScale` fix and
  weapon-re-parenting work can resume — see this session's own bullet above),
  **per-civ gear/prop variants** (helmet/shield/weapon style per civ — real new
  asset need, spec written 2026-09-01, needs the user to source; item 7's
  civ-specific building models are now fully done, 45/45), other
  "everything else" items (music, tutorial, performance profiling, store
  assets, multiplayer determinism gaps, README drift), or continued balance
  work — user's call.
- Last completed (this session): **General garrisoning system, AoE IV style**
  (Roadmap Section 1 worker-mechanics-audit item / Section 5 item 12) — pooled
  capacity, any eligible friendly land unit, scaling defensive firepower, and
  ejection to a rally point, replacing the old Maratha-Durg-only single-slot
  mechanic. `Garrison.cs` generalized in place into
  `Assets/Scripts/Buildings/GarrisonPoint.cs` (pooled capacity + a `durgOnly`
  flag that reproduces Wall's exact old single-slot Durg-only behavior
  unchanged), `DurgGarrisonWorker.cs` generalized into
  `Assets/Scripts/Buildings/GarrisonSeeker.cs` (added to
  Worker/Soldier/Archer/Cavalry/Spearman and every already-spawnable land
  unique unit except Siege), and `TowerAttacker.cs` generalized into
  `Assets/Scripts/Combat/BuildingAttacker.cs` so **TownCenter could get a
  baseline `Attacker` for the first time** (it had none before this item) —
  TownCenter is now the Keep/TC-equivalent (capacity 8, up to 5 simultaneous
  shots), Tower the Outpost-equivalent (capacity 4, up to 4 simultaneous
  shots), Wall unchanged (capacity 1, durgOnly), Gate still ungarrisonable.
  Design decisions made explicitly rather than assumed: Siege units excluded
  from garrisoning (AoE IV siege engines don't garrison), and siege-immunity
  stays a Maratha-Durg-specific bonus layered on the same mechanism rather
  than becoming "any full building is siege-immune." **Found and fixed a real
  latent bug during this session's own live-verification pass, not before
  it**: `GarrisonSeeker` moved to and range-checked against the target
  building's raw `transform.position`, which for TownCenter's 6-tile
  footprint sits deep inside its own carved NavMeshObstacle (edge-to-center
  distance up to 3, wider than the 2.5 interactionRange itself) — a unit
  ordered to garrison a TownCenter could physically never get close enough to
  trigger entry, the exact same class of bug already fixed once for
  Gatherer's drop-off approach (`BuildingFootprintTag.GetNearestApproachPoint`)
  — fixed the same way, computed once per order. 16 new EditMode tests
  (`GarrisonPointTests.cs`, `BuildingAttackerTests.cs`, plus 3 pre-existing
  Garrison tests in `UniqueUnitsTests.cs` updated to the new API), all 85
  pass — `BuildingAttackerTests` needed `LogAssert.ignoreFailingMessages`
  around any damage-dealing `Tick()` call, since `Attackable.TakeDamage`'s
  VFX burst logs an Editor-only "Destroy may not be called from edit mode"
  once its particle system's stop-action fires outside Play mode (no
  precedent existed for testing `TakeDamage` in EditMode before this).
  Live-verified in Play mode via UnityMCP: real capacity enforcement (8/8 on
  TownCenter, 9th unit rejected), shot-count scaling measured precisely
  against fresh 5000-HP dummy targets (Tower: 1 target hit ungarrisoned,
  exactly 4 once garrisoned with 3 occupants), `UngarrisonAll` repositioning
  every occupant outside the building, and the Wall `durgOnly` gate rejecting
  a regular Soldier while accepting the real Maratha Durg Garrison unit and
  flipping `Attackable.SiegeImmune` exactly as before this generalization.
  See `docs/SESSION_LOG.md`.
- Previously completed: **Repair system** (Roadmap Section 1
  worker-mechanics-audit item / Section 5 item 11) — right-click a damaged
  building/ship/siege unit with a worker selected to repair it, at a resource
  cost proportional to HP restored, the AoE reference behavior. New
  `Repairable` (`Assets/Scripts/Combat/Repairable.cs`, target-side) and
  `Repairer` (`Assets/Scripts/Buildings/Repairer.cs`, worker-side) mirror
  `Builder`/`ConstructionSite`'s exact shape, reusing
  `ConstructionSite.SpeedMultiplier` directly for multi-repairer diminishing
  returns rather than a second formula. Wired onto all 9 building kinds (incl.
  TownCenter), Siege, and both naval units — 12 factories, one line each.
  `SelectionManager` gained a `hitRepairable` branch (same friendly-only
  chain-of-exclusivity pattern as `hitGarrison`/`hitFarm`), and every other
  order branch now cancels an in-progress repair. **Known, disclosed
  compromise**: this project doesn't retain each building/unit instance's
  original build/train cost at runtime, so `Repairable` charges a flat
  Wood-per-HP rate keyed off `Attackable.Class` (Building 0.4/HP cheapest,
  Naval 0.6/HP, Siege 1.2/HP priciest) rather than an exact per-instance "half
  of original cost" figure — refinable later if exactness is wanted. Hit and
  fixed a real EditMode-test-only gotcha along the way: an initial draft cached
  `Attackable` in `Repairable.Awake()`, which silently failed in EditMode tests
  because Unity doesn't guarantee `Awake` has run synchronously right after
  `AddComponent` there (same class of issue `ConstructionSiteTests`'s own
  comment already flags) — fixed by lazily resolving via a property getter
  instead, same convention Barracks/Dock already use for their own
  Site/FactionMember. 8 new EditMode tests via an `internal Tick(deltaTime)`
  (same pattern as `ConstructionSite.EnsureInitialized`); all 75 EditMode tests
  pass. Live-verified in Play mode via UnityMCP: real HP restoration and Wood
  deduction at the exact documented rate, a stall-then-resume across an
  insufficient-funds gap with zero value lost, auto-stop at full health,
  `UnitStatus` showing "Repairing", and `Repairable` correctly present with the
  right `UnitClass` on a live-spawned Siege unit and War Galley. See
  `docs/SESSION_LOG.md`.
- Previously completed: **Per-civ soldier visual differentiation,
  session 1 — tint-gap fix + scoping** (new Roadmap Section 1 item, supersedes
  the old Crusader Knight item). `HumanModelFactory.PaletteNameFor()`
  (`Assets/Scripts/Units/HumanModelFactory.cs:158`) only wired 3 of 5 civs
  (Chola→Red, Vijayanagara→Yellow, Rajput→Blue) — Maurya and Maratha hit
  `default: return null` and spawned every unit completely untinted (plain
  white), a real previously-unflagged bug. Fixed by pixel-sampling the shared
  trim-sheet texture (`Human Character Dummy/Textures/HumanCharacterDummy_
  ColorPalette.png`, 16 rows) against each civ's canon crest color
  (`docs/UI_ART_BRIEF.md`): Maratha's forest green (#267333) turned out a
  near-exact match to the *existing* `Green` material (color distance 601 of
  16 candidates — no new asset needed, just remapped); Maurya's warm gray/stone
  (#807866) has no close match anywhere in the sheet, so a new
  `HumanDummy_Gray.mat` was wired to an unused neutral-gray row (offset
  y=0.9375) — a known, disclosed compromise (neutral gray, not warm stone),
  swappable later for a proper warm-gray texture with zero code changes if the
  user sources one. Also made explicit, in Plan Mode with the user before any
  code: **keep the current Human Character Dummy body this session, don't swap
  to the verified-compatible Crusader Knight model** (closes the "Base human
  body decision" Roadmap Section 5 item) — the swap (fixing its ~247x
  `Animator.humanScale` anomaly + re-parenting sword/shield/staff props to a
  hand bone via `WeaponAttachment`) stays open as a real future item, not
  bundled in blind. Gear/prop variants (helmet/shield/weapon style per civ)
  scoped into a spec for the user to source (`Assets/Resources/Weapons/`
  currently has exactly one generic weapon per unit type, no civ variants) —
  not implemented, not guessed at with low-confidence substitutes per the
  cursor-pack precedent. Live-verified in Play mode via UnityMCP: spawned all 5
  civs' bodies side-by-side and screenshotted — each reads as a visually
  distinct tint (red/yellow/blue/gray/green). All 67 EditMode tests still pass
  (no new test — pure lookup-table data change, consistent with how the
  original 3-civ mapping was never separately unit-tested either). See
  `docs/SESSION_LOG.md`.
- Previously completed: **Maratha civ-specific building models, 9/9 —
  closes all 5 civs' civ-specific building models (45/45).** (Roadmap Section
  4.3 / Section 5 item 7) — wired via the same `MeshyBuildingImporter.cs`
  pipeline, no code changes. Identification: 7 of 9 resolved confidently by
  Meshy-internal filename; the prior session's flagged 2-way ambiguity
  (`Fortified_Stone_Prison`/`Fortified_Stone_Villa` against Barracks/House)
  was resolved by the user directly (Prison→Barracks, Villa→House) after this
  session's own live-geometry screenshots came back too poorly framed to
  read. No duplicate-asset trap (byte-diffed all 9 folders — all distinct).
  Orientation: 7 of 9 (Gate/Farm/Market/Dock/Barracks/House/TownCenter) needed
  `Quaternion.Euler(-90,0,0)` — TownCenter via the vertex base/tip density
  method (8.42:1 base-heavy on local Z, matching Rajput's/Maurya's own
  wide/sprawling TownCenter pattern, correct on the first attempt). Tower hit
  the exact Y+90-vs-Y-90 tying-bounds trap flagged by the prior session's own
  endnote — both screenshotted and checked against the reference Watchtower
  art before picking `Euler(0,-90,0)` (Y+90 was upside down: crenellated cap
  on the bottom, lion-pedestal-style base on top). **Wall shipped wrong once
  from this session's own verification, caught only by the user from a live
  screenshot, not by this session's process**: an angled `game_view`
  screenshot at identity rotation looked plausible via parallax even though
  the model was actually lying flat on its back (front face pointing at the
  sky); the resulting prefab's anomalously thick footprint (Z depth ~90% of
  height, nearly 2x a wall's expected thinness) should have been the tell but
  wasn't caught before reporting it as done. Re-verified via a true top-down +
  front-elevation shot (not an angled one) against the reference art —
  `Euler(-90,0,0)` is correct (thin footprint, merlons on top). Re-checked all
  other 8 buildings the same rigorous way as a precaution afterward; all 8
  confirmed already correct — only Wall was wrong. **New lesson for future
  sessions: an angled `game_view`/Scene View screenshot is not sufficient to
  confirm "upright" — always take a true top-down shot (camera straight down,
  looking for a thin/plausible footprint) and a true front-elevation shot
  (camera level with the ground) for every building, not just the
  bounds-ambiguous ones.** Scale: worker height measured fresh (1.902692,
  matching Maurya's session's own measurement exactly), same ratio hierarchy
  reused — TownCenter 11.22/Tower 8.00/Market 4.85/Barracks 4.36/Dock
  3.81/Wall 2.66≈Gate 2.65/House 2.58/Farm 2.09. All 67 EditMode tests pass
  (no new tests — pure asset-pipeline work). Raw source folders deleted after
  confirming every prefab re-spawns correctly post-deletion (matching prior
  civs' precedent); concept art kept at civ root except Tower's/TownCenter's,
  which were bundled inside their own now-deleted raw folders (same as every
  prior civ). See `docs/SESSION_LOG.md`.
- Previously completed: **Maurya civ-specific building models, 9/9**
  (Roadmap Section 4.3 / Section 5 item 7) — wired via the same
  `MeshyBuildingImporter.cs` pipeline, no code changes. The raw delivery
  actually had 9 folders, not the 8 the prior session's endnote claimed (that
  note was stale — re-checked per protocol rather than trusted); 8 resolved
  confidently by filename, the 9th (`Domed_Stone_Sanctuary`) resolved to House
  by elimination + live geometry check. Orientation: 8 of 9
  (Market/Farm/Gate/Dock/Wall/House/Barracks/TownCenter) needed
  `Quaternion.Euler(-90,0,0)` — several of these (Market, House, Barracks) had
  *already-Y-tallest* bounds at identity purely by coincidence (X/Y extents
  tied near Meshy's normalization ceiling) despite actually lying flat, caught
  only by looking (a dome bulging out the front face, not the top) not by the
  numbers alone — re-confirmed every one of the 8 visually, not just the
  obviously-wrong-looking ones. TownCenter (wide/sprawling like Rajput's) used
  the vertex base/tip density method directly rather than trusting bounds,
  correctly predicting the same correction. **Tower shipped upside-down once
  from this session's own verification, caught only by the user from a
  delivered screenshot**: `Euler(0,90,0)` and `Euler(0,-90,0)` both give
  Y-tallest bounds through the civ-blind `ImportRotationCorrections["Tower"]`
  runtime stomp, and were wrongly treated as interchangeable on a "pure Y-spin
  can't flip up/down" assumption that doesn't hold once composed with the
  stomp's own Z rotation — `Euler(0,-90,0)` is the correct one (confirmed
  against the Watchtower concept art from two sides). Also hit a genuine Unity
  `ModelImporter` bug (not source corruption): Wall's raw FBX imported as a
  0-vertex mesh under the project's default `useFileScale=true`; root-caused
  by diffing importer settings against a working one-off import of the same
  bytes, fixed by setting `useFileScale=false` on that one asset's importer.
  Scale targets derived from this session's own measured worker height
  (1.902692) against the established ratio hierarchy — TownCenter
  11.22/Tower 8.00/Market 4.85/Barracks 4.36/Dock 3.81/Wall 2.66≈Gate
  2.65/House 2.58/Farm 2.09, all confirmed via live `BuildingModelFactory.Spawn`
  in both Editor and real Play mode. Hit and worked around a genuine `ENOSPC`
  mid-session (flagged as a risk by the prior session's own endnote,
  `df -h /` showed only 1.1Gi free before starting) — stopped and asked the
  user to free space per CLAUDE.md rather than guessing what was safe to
  delete; user emptied Trash, freeing 13Gi, session resumed cleanly. Raw
  source folders deleted after confirming each prefab re-spawns correctly
  (matching Chola's/Rajput's precedent). All 67 EditMode tests pass (no new
  tests — pure asset-pipeline work). See `docs/SESSION_LOG.md`.
- Previously completed: **Rajput civ-specific building models, 9/9**
  (Roadmap Section 4.3 / Section 5 item 7) — wired via the same
  `MeshyBuildingImporter.cs` pipeline, no code changes. The raw delivery had only
  8 folders for 9 building types (no Tower/Wall candidate); flagged to the user
  rather than force-fit, who added a `tower/` folder and identified the two
  remaining generic-named folders as Wall and House. Orientation: 5 of 9 (Tower,
  Farm, Dock, Market, House) were lying on their back at import and needed
  `Quaternion.Euler(-90,0,0)`; Barracks/Wall/Gate were already correct at
  identity — each confirmed via single positioned screenshots against reference
  concept art (this session's `manage_camera` `batch="surround"` mode returned
  stale/identical images regardless of target, a tooling bug worked around by
  switching to non-batch screenshots). Tower separately needed the usual
  civ-blind `ImportRotationCorrections["Tower"]` treatment; this asset's
  counter-rotation was `Euler(0,90,0)`, found via the same
  all-6-cardinal-candidates test Vijayanagara's session established. **A more
  serious mismatch surfaced mid-verification**: the `rajput towncenter/`
  folder's FBX turned out to be a byte-level duplicate (338 bytes differ out of
  80MB) of the Tower folder's FBX — it rendered as a watchtower, not the grand
  palace shown in its own bundled concept art. Flagged to the user rather than
  decided silently; user supplied a genuine second `towncenter/` export
  (confirmed as real distinct geometry, 80M+ bytes different from Tower's,
  before wiring). **That asset then needed two more rotation passes before
  landing correctly** — the "pick the cardinal rotation giving Y-tallest
  bounds" heuristic that worked for every other asset (including both Towers
  in prior civs) produced two different confidently-wrong results here, both
  live-verified with screenshots and described to the user as correct, both
  caught by the user from live in-game views ("still tilted sideways", then
  "the stairs are going into the ground"). Root cause: this building is wide
  and sprawling, not tall-and-narrow, so its correct orientation isn't its
  tallest possible bounding box — a jutting staircase wing pushes X/Z past the
  true height even when correctly assembled. Resolved by reading the raw
  mesh's vertex data directly and comparing base-heavy vs. tip-heavy vertex
  density per local axis (rather than trusting bounds), landing on
  `Quaternion.Euler(-90,0,0)` — confirmed against the reference concept art
  from both the ornate tiered-dome side and the staircase side (correctly
  ascending from the ground). Scale targets derived from this session's own
  measured worker height (1.9027) against the established ratio hierarchy;
  TownCenter's `extraScale` had to be fully recomputed (91.159, not the
  wrong-axis-derived 61.559) once the correct up-axis changed which dimension
  maps to world height. All 9 buildings confirmed to ~0.002 world units of
  target via live `BuildingModelFactory.Spawn` in Editor and real Play mode.
  All 67 EditMode tests pass (no new tests — pure asset-pipeline work). See
  `docs/SESSION_LOG.md`.
- Previously completed (same day): **Vijayanagara building-model rotation fix**
  (ad hoc bug report, not a roadmap item) — user reported 6 of Vijayanagara's 9
  buildings (Dock, Gate, Wall, Farm, House, Market) spawned misoriented (Z-up
  source meshes lying flat in the Y-up scene, some read as upside down/
  sideways). Re-checking the remaining 3 (per the user's own instruction to
  also re-check Chola) found the real scope was **9/9, not 6/9**: TownCenter
  and Barracks had the identical bug, and Tower — initially cleared by a
  same-methodology check — was still upside down (the user caught this from
  an in-game screenshot after the session's first pass and corrected it).
  8 of the 9 got `Quaternion.Euler(-90,0,0)` on the prefab's nested
  `<Name>_model` child (never the prefab root, which `BuildingModelFactory.Spawn`
  always overwrites on spawn); Tower needed a 180° flip on top of its existing
  Y-axis import bake (`Euler(180,90,0)`, was `Euler(0,90,0)`) — verifying it
  correctly requires reproducing the full runtime spawn stack (the factory's
  own `ImportRotationCorrections["Tower"]` stomp plus the child's baked
  correction together), not just inspecting the saved prefab in isolation,
  which is exactly what produced this session's own initial false-negative on
  it. All 9 individually verified against reference concept art via 6-angle
  screenshots, not blanket-applied. Also re-checked all 9 already-wired Chola
  buildings as a precaution (incl. Wall/Gate) — no regression found; Chola's
  Tower briefly looked wrong in a close-up wide-FOV shot but was confirmed
  upright via a proper Scene View screenshot (perspective artifact, not a
  bug). Added a mandatory per-model in-scene visual orientation check — and,
  for Tower specifically, a full-spawn-stack verification requirement — to
  Roadmap Section 4.4's sourcing checklist and this file's gotchas, since a
  bounds-only/prefab-only check is what let this ship (twice, in Tower's
  case) in the first place. All 67 EditMode tests pass (no new tests — pure
  prefab transform data). See `docs/SESSION_LOG.md`.
- Previously completed (same day): **Vijayanagara civ-specific building models**
  (Roadmap Section 4.3 / Section 5 item 7) — all 9 buildings (TownCenter, Barracks,
  Tower, Market, Farm, House, Wall, Gate, Dock) wired from raw Meshy AI exports via
  the same `MeshyBuildingImporter.cs` pipeline Chola's session established, no code
  changes needed. Identified 8/9 confidently from Meshy-internal filenames; the 9th
  (a generic `Ancient_Stone_Temple`-named folder) was genuinely ambiguous and
  resolved to House by live geometry inspection + elimination. Tower needed the
  same civ-blind-`ImportRotationCorrections["Tower"]` counter-rotation workaround
  as Chola's, but this asset's fix was a **Y-axis** bake
  (`Quaternion.Euler(0,90,0)`), not Chola's Z-axis one — found by testing all 6
  cardinal-axis candidates' rendered bounds for the Y-tallest result, then
  confirming visually via screenshot that it read as an upright tower (a Y-axis
  spin can't itself produce an upside-down result, unlike an X/Z flip, so this one
  needed less disambiguation than Chola's). Scale: all 9 raw imports came out
  Meshy-normalized to ~equal height (~1.90, matching the worker's own measured
  height) except Wall/Gate (correctly flatter since their long axis is
  horizontal) — so target heights were derived by applying Chola's established
  *ratio* hierarchy against this session's own measured worker height (1.903), not
  copied absolute values, landing on Tower 7.84/Market 4.85/Barracks
  4.30/Dock 3.73/Wall≈Gate 2.61/House 2.51/Farm 2.06/TownCenter 10.94. Live-verified
  in both Editor mode and real Play mode via `BuildingModelFactory.Spawn`,
  screenshotted (worker dwarfed by every building, Tower upright, Wall vs. Gate
  visually distinct). All 67 EditMode tests still pass (no new tests — pure
  asset-pipeline work, no new logic). Raw source folders left in place (not deleted,
  unlike Chola's session, since not explicitly asked this time). Rajput/Maurya/
  Maratha (27 models) remain unstarted. See `docs/SESSION_LOG.md`.
- Previously completed: **UI skin polish pass** (Roadmap Section 4.3 /
  Section 5 item 9) — pixel-verified the 5 tuned 9-slice elements (`ResourceHUD`,
  `SelectedUnitPanel`/HP bar, `HoverTooltip`, `BuildMenu` command-card buttons,
  `MissionSelectMenu` buttons) live in Play mode via UnityMCP against their actual
  runtime rect sizes (computed each sprite's effective screen-space border from
  `spriteBorder` ÷ `pixelsPerUnitMultiplier`, confirmed against zoomed screenshots).
  Result: no stretching/pinching/seams anywhere - the prior session's tuning already
  holds up; no border/multiplier code changes needed. Hit a new form of the project's
  known "stale compiled state" gotcha along the way: `Image.sprite` read null for
  every Resources-loaded UI element on the first Play session despite the asset
  loading fine standalone - `refresh_unity(mode=force)` cleared it, not a real bug.
  **Found and fixed one real adjacent bug** (flagged to the user first, confirmed
  in-scope): `FormationIndicator.cs`'s own top-left `Canvas` was anchored at the
  exact same `(8, -8)` corner as `ResourceHUD`, rendering both texts on top of each
  other in every session (not just this one, and not just a UnityMCP artifact -
  visible in the very first live screenshot taken this session). Moved its anchor to
  `(8, -206)`, below `ResourceHUD`'s 200x190 footprint; live-verified clean in Play
  mode. **Maurya crest — closed same session.** Re-confirmed live (zoomed
  screenshot) it rendered in Rajput's blue/gold instead of the spec'd warm
  gray/stone; flagged back to the user per CLAUDE.md's "asset sourcing isn't Claude
  Code's job" rule (with a ready-to-use Canva prompt built from
  `docs/UI_ART_BRIEF.md`'s exact template) rather than attempted. User generated a
  new `crest_maurya.png` (warm gray/stone lion, maroon/gold ring) and dropped it in
  at the same path/format - picked up automatically by the existing `.meta`, no code
  changes needed. Verified the new file's actual pixel colors before trusting it,
  then live-confirmed in Play mode: reads clearly distinct from Rajput's crest now.
  All 67 EditMode tests pass (no new tests needed for either fix - no new pure
  logic; both verified live/visually). See `docs/SESSION_LOG.md`.
- Previously completed: **Reconcile uncommitted work: finish wiring
  BuildingFootprint into all building factories** (ad hoc, not a roadmap item) — the
  prior session's rally-flag/deposit-soft-lock fix added `BuildingFootprint.cs` and
  had `Gatherer` query it, but never actually wired `BuildingFootprint.Attach` into
  the factories themselves, so that fix was silently incomplete (TownCenter and every
  other building still fell back to raw `transform.position`). Committed the leftover
  working-tree changes that finish the wiring (10 factory files + `BuildingPlacer.cs`),
  plus an unrelated cleanup of now-unreferenced raw Meshy `.obj`/`.mtl` exports. Left
  ~200 untracked screenshot files and a new `UI_RawOriginals_backup/` folder (119MB)
  untracked per user instruction. See `docs/SESSION_LOG.md`. Commit `d5d78b3`.
- Previously completed: **Bug fix: rally-flag raycast + resource-deposit
  soft-lock** (ad hoc user bug report, not a roadmap item) — user reported the rally
  flag floating mid-air near the TownCenter and Wood/Food/Gold/Stone stuck at 0 for a
  full session, hypothesizing one shared root cause via RallyPoint. Investigation
  found the hypothesis wrong: two unrelated real bugs. (1) `SelectionManager`'s
  rally/move raycasts used an unmasked `Physics.Raycast` that could hit the selected
  building's own collider before the ground - fixed via `Physics.RaycastAll` +
  skip-self (a first-draft "Ground-only LayerMask" fix was caught as itself buggy
  before implementing, since the same raycast also resolves gather/attack/build-
  assist clicks). (2) `Gatherer`'s deposit target was the TownCenter's raw
  `transform.position`, which sits inside the `NavMeshObstacle` the building-footprint
  system carves around that same point (a regression from 2026-08-28's footprint
  work, never re-tuned) - workers could physically never get within
  `interactionRange`, so `Deposit()` (itself correctly wired) never fired; Population/
  Age being stuck were confirmed downstream of this, not separate bugs. Fixed via a
  new reusable `BuildingFootprintTag.GetNearestApproachPoint` building-geometry query
  instead of a flat scalar bump, so it generalizes to future drop-off buildings
  automatically. The console message about RallyPoint blocking TownCenter removal was
  confirmed harmless tooling noise, unrelated to both. 7 new EditMode tests (67 total,
  all pass); live-verified in Play mode via UnityMCP - direct before/after
  reproduction of the raycast bug, and real gather-deposit cycles run from 5 different
  approach angles confirming continuous stockpile growth. No roadmap entry (scope
  stayed within estimate). See `docs/SESSION_LOG.md`.
- Previously completed: **Worker mechanics audit + multi-builder
  construction diminishing-returns fix** (Roadmap Section 1) — audited Gatherer,
  Farm/FarmWorker, LivestockWorker, Builder/ConstructionSite, and worker
  combat/boar-hunting against 6 AoE reference mechanics. Resource walking and
  carry capacity already matched; self-defense partially matched (workers can
  fight/hunt boars but it's always an explicit attack-move command, never an
  auto-interrupt of an in-progress gather task); repair and general garrisoning
  are confirmed missing entirely and logged as new, clearly-scoped roadmap items
  rather than implemented (per instruction) - garrisoning's scoping surfaced a
  real adjacent gap, TownCenter has no `Attacker` component at all, only Tower
  does. User confirmed adding dedicated resource-specific drop-off buildings
  (Lumber Camp/Mining Camp/Mill-equivalent) over keeping unified TownCenter-only
  drop-off - also logged as a new item, not implemented (real new content).
  **Implemented this session**: `ConstructionSite`'s multi-builder speed formula
  changed from flat-linear to AoE II's diminishing-returns curve
  (`ConstructionSite.SpeedMultiplier`: 1x/1.6x/1.9x/2.2x for 1/2/3/4 workers),
  applied uniformly across every building with no per-building exception. Logged
  as a deliberate balance change (not drift) in `playtest_log.csv` per
  instruction, since it affects rush-timing value covered by the earlier balance
  pass (item 5). 5 new EditMode tests, all 61 pass. Live-verified in Play mode
  via UnityMCP: real ticked `Update()` progress ratios across 4 simultaneous
  foundations matched the formula exactly (to float rounding), not just the
  pure-function unit test - also hit and worked around a real tooling gotcha
  along the way (a live Play session kept running a stale compiled assembly
  after the script edit; needed `refresh_unity` with `mode=force` before the new
  formula took effect at runtime - a linear 1x/2x/3x/4x result on the first live
  attempt was the tell). See `docs/SESSION_LOG.md`.
- Previously completed: **Chola building scale hierarchy corrected**
  (Roadmap Section 4.3) — user flagged from a live screenshot that only TownCenter
  looked properly sized; the other 8 buildings' scale had been calibrated
  independently against their old shared siblings, which let Barracks (1.74) and
  House (1.73) end up shorter than the worker unit (1.94) — visibly broken.
  Recalibrated all 8 as multiples of human height, keeping TownCenter unchanged
  (user-confirmed correct). New heights: Tower 7.99, Market 4.95, Barracks 4.38,
  Dock 3.81, Wall/Gate 2.66, House 2.57, Farm 2.10 — clean descending hierarchy, all
  above the worker's 1.94. Hit and caught a real mistake mid-fix (edited a prefab's
  `_model` child's constant import-normalization scale instead of the prefab root's
  actual tuned multiplier); reverted cleanly once caught. Live-verified in Play mode
  via UnityMCP screenshots (sent to the user directly), all 45 EditMode tests still
  pass. Methodology and reference ratios saved to Claude's cross-session memory for
  reuse on the remaining 4 civs' 36 models. See `docs/SESSION_LOG.md`.
- Previously completed: **5 cursor states wired** (Roadmap Section 4.3) —
  closed the "build-placement cursor asset missing" gap using a newly-imported Asset
  Store pack ("Basic RPG Cursors") that turned out not to 1:1-match the UI art
  brief's 5-state spec (generic weapon/tool icons, not purpose-made); reported the
  mismatch with pixel-verified evidence before assuming a match, then proceeded with
  user-confirmed closest-available substitutes per state. Wired all 5 in
  `HoverTooltip.cs` via a new pure/testable `ResolveCursorState` helper (6 new
  EditMode tests, all 45 pass), also fixing a real latent bug found along the way:
  Attack-move previously showed regardless of whether the selection could actually
  attack, and Gather never signaled anything when it couldn't gather — both now
  correctly fall to the (also newly-wired) Invalid state. Corrected the 3
  already-wired cursor textures from 2048×2048 down to the spec'd 32×32 in the same
  pass. Live-verified in Play mode via UnityMCP against real scene objects/components
  (not just EditMode tests) — see `docs/SESSION_LOG.md` for the full methodology and
  a disclosed tooling limitation (no readback for the OS-rendered cursor bitmap
  itself).
- Previously completed: **Chola civ-specific building models** (Roadmap
  Section 4.3) — all 9 buildings (TownCenter, Barracks, Tower, Market, Farm, House,
  Wall, Gate, Dock) wired from raw Meshy AI exports the user supplied, with real PBR
  materials (not the flat-albedo shortcut the unique-unit session used), per-building
  scale correction verified live against the existing shared buildings, and a Tower
  orientation bug found and fixed (the shared rotation-correction dict doesn't know
  about civs). Hit and fixed a genuine Unity Editor crash along the way (in-Editor
  `Texture2D.GetPixels` on 2048x2048 maps took the Editor process down entirely) by
  moving that step to a plain Python/Pillow script outside Unity. All 39 EditMode
  tests pass (2 new ones added). Full methodology, the crash root-cause, and an
  AABB-can't-detect-upside-down lesson for future rotation work are in
  `docs/SESSION_LOG.md`.
- Previously completed: **UI skin display wiring** (Roadmap Section 4.3),
  the follow-up to last session's art delivery/alpha-fix pass. Created
  `Assets/Resources/UI/UIStyleTheme.asset`, set Sprite/Cursor import types + 9-slice
  borders on all 45 files, and wired the art into `BuildMenu` (command-card 4-state
  reskin + icons on ~24 buttons), `ResourceHUD` (background + 4 resource icons),
  `SelectedUnitPanel` (a real HP bar alongside the existing text), `CivPicker` (civ
  crests), and `HoverTooltip` (dedicated tooltip frame + cursor-state switching for
  the 3 states with real art). Live UnityMCP scene inspection ahead of coding caught 3
  things a code-only read had gotten wrong: `ResourceHUD` already has its own
  background `Image`; `BuildMenu` buttons are 204x28 thin text rows, not square icon
  buttons (icons + graceful text word-wrap, verified live, not a clipping bug); and
  `ResourceHUD`/`SelectedUnitPanel`/`HoverTooltip` each need their own dedicated
  background sprite rather than sharing `UIStyleTheme`'s one modal-frame field. Also
  found and fixed `hp_bar_frame.png`/`hp_bar_fill.png` still carrying huge transparent
  margins from last session's leftover noise-speckle artifacts (a largest-component
  filter tightened both). All 37 EditMode tests pass; live-verified in Play mode via
  UnityMCP screenshots (crests, command-card icons, HP bar fill, Settings modal
  reskin), zero new console errors. The 2 known content gaps (missing 5th cursor,
  Maurya crest color) are unchanged - not fixable by code. Full detail, the exact icon
  mapping, and 9-slice border values in `docs/SESSION_LOG.md`.
- Also landed around the same time (concurrent sessions): a **Naval balance pass**
  (Roadmap Section 1 — 4 live forced-fights, no `CombatBonus` changes needed, 2
  adjacent findings flagged not fixed) and a **Crusader Knight rig-compatibility
  verification** (Roadmap Section 1 — both models confirmed rig-compatible via
  `AvatarBuilder.BuildHumanAvatar` + hand-authored `HumanDescription`, live-tested
  through the real `AnimationDriver`/`WeaponAttachment` pipeline; 2 real caveats found
  and not yet fixed: a ~247x `humanScale` anomaly, and sword/shield props parented to
  the scene root instead of a hand bone). Full detail in `docs/SESSION_LOG.md`.

## Engine & architecture
- Unity version: [fill in]
- Render pipeline: URP
- Networking: deterministic lockstep, foundational pass only — see Roadmap Section 1
  "Multiplayer determinism gaps" before touching anything network-related
- Data: civs/techs/units are CSV-driven → generated ScriptableObjects
  (`Assets/Design/Data/*.csv` → `Assets/Editor/CsvToScriptableObject.cs` →
  `Assets/Resources/Data/Generated/`, read via `Core/DataRegistry.cs`). Don't hand-edit
  generated assets — edit the CSV and regenerate.

## STRICT SESSION PROTOCOL
1. On start: read the "Roadmap - Open Items & Priority" and "Implementation Waves 0-6"
   sheets in `docs/KingdomsOfBharat_Master_Reference.xlsx`, plus this file's "Current
   status." State the next item and confirm before starting.
2. One roadmap item per session. Flag adjacent work instead of silently expanding
   scope — ask whether to include it now or log it as a new item.
3. Plan Mode before nontrivial changes.
4. Test before calling it done.
5. Update the relevant Roadmap sheet(s) in `docs/KingdomsOfBharat_Master_Reference.xlsx`
   (status/priority) and this file's status.
6. Log to `/docs/SESSION_LOG.md`.
7. One scoped commit referencing the roadmap item.
8. Stop — don't roll into the next item without being asked.

## Known gotchas (from project history — read before hitting these again)
- **"Editor frame stuck at Time.time=0" flakiness has a real fix**: call
  `Application.runInBackground = true`, `EditorApplication.QueuePlayerLoopUpdate()`,
  and repaint SceneView + GameView together. Try this before falling back to
  reflection-forced ticks.
- **`CombatBonus` and `CounterMatrix` are deliberately separate systems** — don't
  merge them. `CombatBonus` holds playtested balance fixes (incl. the asymmetric
  Cavalry-vs-Archer split); `CounterMatrix` is CSV data used only for Spearman.
- **`AgeProfile`/`UpgradeProgress` are intentionally hardcoded**, not CSV-migrated —
  pure formula tuning, no natural per-row shape. Don't "finish the migration" onto
  these without a real schema design first.
- **Single-session discipline matters here specifically**: this project's own history
  has real bugs from concurrent sessions (a duplicated script folder, a silent
  colliding-enum-name bug, a git-index race) and from trusting peer-relayed claims
  instead of confirming directly. Run one session at a time. Treat any claim about
  "what was already done" as unverified until confirmed against the actual repo.
- **Civ-specific building model imports need a per-model, in-scene visual
  orientation check against reference art — an AABB bounds check alone is not
  enough.** A Z-up-sourced model lying flat on a wide base can have bounds that
  look plausible even though it's on its back; 8 of Vijayanagara's 9 buildings
  shipped this way in the same session Tower's rotation bug was fixed and
  documented, because the check wasn't generalized past Tower. The fix always
  lives on the imported model's nested child transform (e.g. `<Name>_model`),
  never the prefab root — `BuildingModelFactory.Spawn` unconditionally
  overwrites the root clone's `localRotation` on every spawn (identity, or the
  Tower-specific correction), silently reverting any root-level fix.
  **Separately, for Tower (or anything else added to
  `ImportRotationCorrections`), inspecting the saved prefab alone is not
  enough either** — the factory's own rotation stomp is applied at runtime on
  top of the child's baked correction, so a check has to reproduce both
  together (instantiate, apply the dict's correction to the instantiated
  root, then judge) or it can pass a model that's actually still wrong, as
  happened once on Vijayanagara's own Tower mid-fix. See Roadmap Section
  4.4's checklist and `docs/SESSION_LOG.md`'s 2026-08-31 rotation-fix entry
  for the full methodology.
- **Asset sourcing/creation is NOT Claude Code's job right now.** Per Roadmap Section
  4, the user arranges or creates required assets against the spec there. Claude
  Code's role is wiring already-provided assets in (factories, attachment points,
  material tinting), not sourcing packs — the earlier ad hoc sourcing approach was
  explicitly rejected for not meeting the target visual standard.
- **The project now has real `.asmdef` assemblies** (added when the training/trade UI
  batch needed its first tests): `Assets/Scripts/KingdomsOfBharat.Runtime.asmdef` covers
  all of `Assets/Scripts`; `Assets/Editor/KingdomsOfBharat.Editor.asmdef` (references
  Runtime) covers `Assets/Editor`; `Assets/Tests/EditMode/KingdomsOfBharat.Tests.asmdef`
  (references Runtime + the Unity Test Framework) covers `Assets/Tests`. New runtime
  scripts anywhere under `Assets/Scripts` compile into Runtime automatically — no action
  needed. Third-party asset folders (RedCambala, Tree_Packs, PolishedSurfaces, the human
  character-pack demo scripts under `Assets/Resources/human/...`) were deliberately left
  out of this split (still compile into the implicit default assembly) since nothing in
  `Assets/Scripts` depends on them.

## Coding conventions
- Prefer ScriptableObjects / the CSV data pipeline for new unit/civ/tech content over
  hardcoded values, unless it falls into one of the "intentionally hardcoded" gotchas
  above.
- Every new system needs at least a basic PlayMode/EditMode test.

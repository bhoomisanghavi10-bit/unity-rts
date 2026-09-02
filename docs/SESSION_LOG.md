# Session Log — Kingdoms of Bharat

Chronological log of Claude Code sessions against this repo, per CLAUDE.md's session
protocol (step 6). Newest entries at the top.

---

## 2026-09-02/03 — Re-source Rajput TownCenter and Barracks; fix Rajput Tower rotation

**Scope**: two flagged, unresolved gaps from the same-day mesh-decimation session
(Roadmap Section 1/5 item 7) — Rajput TownCenter had no civ-specific model at all
(its original source FBX was confirmed 0 bytes and removed, falling back to the
shared model), and Rajput Barracks' wired model was a small boxy shape that didn't
match its "grand multi-turret courtyard-fort" concept art. The user located and
supplied correct source deliveries in an external folder
(`/Volumes/US/all civ buildings/Rajput/rajput towncenter/`,
`.../rajput barrack/`) and asked for them to be wired in. Went through Plan Mode
first (multi-step Unity asset-pipeline work: texture packing, import, rotation/
scale verification, decimation, live testing).

**Implementation**: `MeshyBuildingImporter.ImportBuilding` requires a pre-packed
`*_metallicSmoothness.png` the raw deliveries didn't have (only separate
`_metallic.png`/`_roughness.png`) — packed via a new scratchpad Python/numpy/Pillow
script (R=metallic, A=1-roughness), copies made into the scratchpad rather than
editing the user's original delivery folder. Both buildings wired via the existing
`MeshyBuildingImporter`/`BuildingMeshDecimator` pipeline, no code changes needed —
same one-shot pipeline every prior civ-model session has used.

**Orientation**: both raw exports were lying on their back at import (Meshy's
usual Z-up-into-Y-up gap). Per this project's own documented gotcha (a bounds-only
check is not enough, especially for wide/sprawling shapes), each was verified via
3/4-view and top-down screenshots against its own concept art before committing to
`Quaternion.Euler(-90,0,0)` for both — confirmed correct for Barracks (reads as
the courtyard fort with 4 corner chhatri turrets, crenellated walls, staircase,
open interior — an exact match to its concept art) and, on the second attempt, for
TownCenter (see below).

**TownCenter needed a second pass mid-session**: the first delivery in the
`rajput towncenter` folder was wired, rotation-verified, scaled, decimated, and
about to be documented as done when the user caught that it was the wrong file —
their own upload mistake, not a pipeline bug. They replaced it with the correct
delivery (`Meshy_AI_Rosestone_Citadel_0902182129_texture.fbx`, same concept art
target); the new raw mesh's bounds differed from the first (a wide/sprawling
tiered-dome-and-staircase palace, not the same shape at all), so rotation and
scale were re-verified from scratch rather than reusing the first pass's numbers.
Confirmed correct via top-down (reads as a real building plan — courtyard, corner
domes, central structure, staircase wing — not a flat facade) and 3/4-view
screenshots against the concept art: a tiered stepped-pyramid palace with domes,
matching "a single isolated grand Rajput fort-and-haveli architectural complex."

**Scale**: measured live worker height (~1.9-2.0, consistent with this project's
established baseline) and targeted the existing Rajput ratio hierarchy (Tower
8.00 > Market 4.85 > Barracks ≈4.36 > Dock 3.81 > Wall/Gate 2.66 > House 2.58 >
Farm 2.09, TownCenter as the largest) — landed on Barracks height 4.34 and
TownCenter height 11.28, both confirmed via live `BuildingModelFactory.Spawn`
bounds and screenshots (worker dwarfed appropriately; TownCenter reads as the
grandest building, wider and taller than Tower; Barracks sits between Tower and
House). Both decimated to ~500,000 tris via `BuildingMeshDecimator` (from
~1.9-2.0M raw Meshy exports), screenshot-confirmed clean at that target (no
visible carved-relief artifacts, matching every other already-decimated civ
building).

**Adjacent bug found and fixed, flagged live by the user mid-session (not part of
this item's own original scope, but directly on-topic and immediately actionable)**:
while comparing TownCenter against the neighboring Tower for scale context, the
user noticed Rajput's Tower — untouched by this session's own changes — was
spawning upside-down through the real `BuildingModelFactory.Spawn` path. Root
cause: `BuildingModelFactory`'s civ-blind `ImportRotationCorrections["Tower"]`
runtime stomp (`Quaternion.Euler(0,0,-90)`, applied to the outer wrapper at spawn
time, unconditionally for any building named "Tower") composes with the
civ-specific model's own baked child correction one level down — Rajput's baked
value (`Euler(0,90,0)`, set in the original 2026-08-31 Rajput building session)
turned out to be the wrong one of the Y+90/Y-90 tying-bounds pair this project's
own history had already flagged as a real trap
(`feedback_tower_rotation_correction.md` in cross-session memory: both give
identical Y-tallest bounds, only one is upright). Root-caused rather than
guessed: tested the raw source FBX standalone (outside the factory's stomp) to
find its correct absolute orientation against the Watchtower concept art
(`Euler(-90,0,0)` — chhatri domes and crenellated parapet on top, buttressed base
at bottom, matching the reference art closely), then solved algebraically for the
child's required local rotation given the fixed parent stomp
(`Quaternion.Inverse(parentStomp) * targetWorld` = `Euler(0,-90,90)`, verified by
composing both in a throwaway test hierarchy before touching the real prefab).
Applied directly to `Assets/Resources/Buildings/Rajput/Tower.prefab`'s nested
child transform via `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`
(the standard scripted-prefab-editing pattern this project's tooling already
uses elsewhere, e.g. `BuildingMeshDecimator`) and re-verified upright through the
real `BuildingModelFactory.Spawn` path afterward, not just the prefab file.

**Tests**: all 146 EditMode tests pass unmodified throughout every stage of this
session (including `BuildingPolycountTests`, which would have caught a
decimation regression) — no test code changes needed, consistent with every
prior civ-model session (pure asset-pipeline and prefab-transform-data work, no
new logic).

**Roadmap**: Section 1/5 item 7 updated — 44/45 civ-specific building models
complete (only Maurya Tower remains on the shared fallback, a separate
pre-existing 0-byte-source gap unrelated to this session), Rajput's own 9/9 is
now genuinely complete end-to-end (model + correct orientation). Section 4.2's
Rajput TownCenter fallback note updated to reflect the fix.

---

## 2026-09-02 — Building mesh decimation pass (Roadmap Section 1/5 item 17)

**Scope**: the 2026-09-02 civ-by-civ visual audit found every civ-specific
building (45 total) shipping at ~1.7-2.0M un-decimated triangles, ~100-250x
the project's own 8,000-20,000 tri spec — scoped as its own dedicated
session per the user's explicit request. Followed the session protocol:
Plan Mode (explored `MeshyBuildingImporter.cs`, `BuildingModelFactory.cs`,
the actual `Assets/Resources/buildings/<Civ>/` prefab structure, and the
nested-`PrefabInstance` gotcha via a research agent before writing the plan),
implemented, tested, live-verified, then this write-up.

**Implementation**: added `UnityMeshSimplifier` (MIT, pure C#) via a git-URL
UPM dependency in `Packages/manifest.json` (needed adding
`Whinarn.UnityMeshSimplifier.Runtime` as an explicit reference in
`Assets/Editor/KingdomsOfBharat.Editor.asmdef` — the package resolved fine
but the Editor assembly couldn't see its types without that). New
`Assets/Editor/BuildingMeshDecimator.cs`, same convention as
`MeshyBuildingImporter.cs`: always re-derives the source mesh fresh from
`_Source/<Name>/<Name>_model.fbx` (never the prefab's current, possibly-
already-decimated mesh) so re-running with a different target is idempotent;
edits each prefab via `PrefabUtility.LoadPrefabContents` → modify
`MeshFilter.sharedMesh` → `SaveAsPrefabAsset` at the same path (Unity
records the mesh swap as a normal nested-instance override — no manual
`UnpackPrefabInstance` needed, simpler than the plan originally assumed).

**Target-ratio judgment call — the real finding of this session**: the
spec's literal 8,000-20,000 tri target, and even the plan's own
30,000-60,000 conservative fallback, both proved unreachable with
`UnityMeshSimplifier`'s default settings on this raw, un-retopologized Meshy
export geometry. Proof-of-concept on Chola TownCenter (1,828,917 source
tris, the most ornate case): requesting the spec's own 15,000-tri target only
reached 280,221 tris even at the simplifier's maximum aggressiveness
(`quality=0.0082`) — the default `MaxIterationCount` (100) ran out before
reaching the requested ratio — and that result showed real visible
degradation on screenshot comparison (crease/faceting artifacts on flat wall
and floor surfaces that weren't there in the original). Tried pushing
further via `SimplificationOptions.VertexLinkDistance` (to weld the many
near-but-not-exactly-coincident vertices this raw export style produces) and
a higher `MaxIterationCount` — this made the algorithm's per-iteration cost
explode: one such attempt pegged the Unity Editor's main thread at 99% CPU
for over 25 minutes with no completion, and `execute_code` has no way to
cancel a running synchronous call. **Resolved by killing and relaunching
the Unity Editor process, at the user's explicit instruction** ("kill it and
just use the default result") rather than waiting further — a real, if
unglamorous, part of this session worth logging honestly.

**The actual chosen target: 500,000 tris/building** — landed on this after
testing it directly (still default settings only, no risky option tuning):
Chola TownCenter reached 499,999 tris in about 2 minutes with **no visible
degradation** on the same screenshot comparison that showed clear artifacts
at the tighter target. This is a genuine ~3.65x reduction on the worst case
(a full 9-building base drops from ~17M+ triangles to roughly 4.5M) — a
real, substantial rendering-cost win, even though it falls well short of the
spec's literal number. Batched all 45 via `DecimateAll(500000)`; 43
succeeded (all landing within 1 triangle of 500,000, quality ratios
0.25-0.29 depending on each building's own source complexity), and the 2
pre-existing gaps (Rajput TownCenter, Maurya Tower — 0-byte source FBX from
before this session, see Roadmap item 7) were correctly skipped with a clean
log message rather than crashing, leaving their existing shared-fallback
model untouched.

**Found and fixed one genuine Unity `AssetDatabase` caching bug along the
way**: re-running `DecimateBuilding` on the same prefab within one Editor
session left `AssetDatabase.LoadAssetAtPath` and `Resources.Load` (and
therefore `BuildingModelFactory.Spawn` itself, which crashed with a
`NullReferenceException`) serving a stale cached prefab graph with a null
`MeshFilter.sharedMesh`, even though the saved `.prefab` file and new mesh
asset were both correct on disk — confirmed via
`PrefabUtility.LoadPrefabContents`, which always re-reads from disk and
showed the right mesh, proving it was a cache issue, not a save issue. Fixed
by calling `AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate)`
right after saving each prefab; the whole 45-building batch ran clean after
that fix.

**New regression coverage**: `Assets/Tests/EditMode/BuildingPolycountTests.cs`
spawns every `CivilizationId` × building-name combination via
`BuildingModelFactory.Spawn` and asserts total triangle count stays under a
750,000 ceiling (a 1.5x buffer over the ~500,000 target, wide enough to
never flake on the ~1-triangle variance the simplifier itself produces, but
tight enough to catch a real regression back toward the multi-million-
triangle baseline). 146 EditMode tests total (up from 145), all pass.

**Live verification**: spawned all 45 civ/building combinations via the real
`BuildingModelFactory.Spawn` path and read `MeshFilter.sharedMesh.triangles
.Length` directly (matching the audit session's own methodology) — 43
confirmed at ~500,000 tris, Maurya/Tower correctly untouched at its existing
9,956 (already low-poly, different import pipeline), Rajput/TownCenter
correctly falling back (see below). Screenshot-verified visual quality on 6
buildings across all 5 civs (Chola TownCenter close-up, Vijayanagara Wall,
Rajput Barracks, Maurya Market, Maratha Gate) — clean in every case, no
visible faceting/artifacts at the 500,000-tri target.

**Flagged mid-session by the user, investigated live, resolved as a false
alarm**: the user flagged the Rajput Barracks screenshot as looking "tilted
sideways." Checked directly rather than assumed: every `Transform.localRotation`
in its hierarchy is identity (matching every correctly-oriented building),
my script never touches any `Transform`, and a genuine ground-level
front-elevation shot (not the original steep-angled one) confirmed it
standing upright — the original screenshot's steep downward camera angle
foreshortened the roofline in a way that read as tilted, the same parallax
illusion this project's history has hit before (see CLAUDE.md's own
gotchas). **A second, separate, real finding did come out of that same
exchange**: the user then shared a reference image (a grand multi-turret
open-courtyard fort) as what Rajput Barracks should look like — clearly not
what's in-game. Checked the original, un-decimated `_Source/Barracks/
Barracks_model.fbx` (untouched by this session's own work) directly, and its
bounds are already small and boxy (1.90 × 0.91 × 1.63 world units) — the
same shape currently in-game. This confirms the mismatch predates this
session and isn't a rotation or mesh-processing bug: whichever prior session
sourced/identified the Rajput Barracks Meshy export appears to have picked
an asset that doesn't match the intended concept art. Flagged here for a
future session rather than fixed — asset sourcing isn't in scope for this
item, and CLAUDE.md's standing rule is that Claude Code doesn't source
replacement assets unprompted.

**Also found while investigating the git working tree before committing**:
`AssetDatabase.SaveAssets()` (called as part of the batch) flushed several
already-pending, already-in-memory-but-never-saved changes from an earlier
session — 6 more `Assets/Resources/buildings/Maurya/_Source/*/*.mat` files
(beyond the 1 `Barracks.mat` already showing as modified before this session
started) and the previously-documented Cow/Palm2 material fix
(`M_Cow_URP.mat`, `Palm_Trunk.mat`). These are legitimate, already-decided
work from a prior session that simply hadn't been written to disk yet — left
unstaged/uncommitted rather than bundled into this session's commit, since
they're unrelated to item 17 and not this session's to claim credit for or
decide about.

One scoped commit covers `Packages/manifest.json`/`packages-lock.json`,
`Assets/Editor/BuildingMeshDecimator.cs` (new),
`Assets/Editor/KingdomsOfBharat.Editor.asmdef`,
`Assets/Tests/EditMode/BuildingPolycountTests.cs` (new), all 43 decimated
prefabs + their new `_Decimated/*.asset` mesh assets across 5 civs, and the
doc updates (`CLAUDE.md`, `docs/Roadmap.md`, this entry).

---

## 2026-09-02 — Scope the Crusader Knight body-swap sourcing spec (not implemented)

**Scope**: user picked the Crusader Knight body-swap item next, then asked to
scope it first rather than implement — it's explicitly blocked on new source
model files (the original TemplarKnight/HospitalierKnight glTFs were deleted
in the same day's "confirmed-unused asset scrap" cleanup and aren't
recoverable from git history, unlike the same-day Cow/Palm2 texture
recovery). Pure docs/planning, no code/asset changes.

**Wrote a sourcing spec into `docs/ROADMAP.md`** (Section 1's "Per-civ soldier
visual differentiation" item, body-swap sub-item), derived directly from the
2026-08-28 rig-compatibility verification session's own findings rather than
guessed at fresh, so a replacement doesn't repeat the same 2 caveats that
session flagged: format (FBX preferred, glTF also provably workable via
`AvatarBuilder.BuildHumanAvatar` + a hand-authored `HumanDescription`, no
Blender step needed), rig (any standard Humanoid biped, exact bone names
don't matter), scale (model in meters near the scene's own ~1.9-unit
worker/soldier height — traced the deleted models' ~247x `humanScale`
anomaly to a literal `(2.54,2.54,2.54)` inches-to-cm bake on the Hips bone,
so cleaner sourcing avoids needing that fix again at all), animation (none
needed, this project drives all clips through its own `AnimationDriver`
Playables pipeline), and weapon/prop meshes (either omit sculpted weapons
and reuse the existing `WeaponAttachment.AttachToBone` system — the
precedent already used for the 3 humanoid unique units — or ensure any
bundled weapon meshes are separable/re-parentable to a hand bone rather than
static scene-root props, which is exactly how both deleted models were
built and part of why they'd have needed rework even if not deleted). Also
tied polycount guidance back to the same day's building-polycount audit
finding, so this doesn't ship a high-poly sculpt export unnoticed the way
the buildings did.

**Noted during this session**: `docs/SESSION_LOG.md`/`docs/ROADMAP.md`
already carried a same-day "Scope building mesh decimation pass" entry
(commit `dc8fcd9`) that hadn't existed at this session's own start —
flagged to the user per CLAUDE.md's single-session-discipline gotcha
(another concurrent session appears to have picked up and closed that
scoping item independently) rather than silently treated as this session's
own prior work.

**Not implemented this session** — per the user's own explicit scope
("scope it out first"), and because there is nothing to implement yet: this
item stays blocked until a replacement source model actually lands.

---

## 2026-09-02 — Scope the building mesh decimation pass (not implemented)

**Scope**: user asked to scope the mesh-decimation item (the audit session's
headline finding — every civ-specific building is ~1.7-2.0M un-decimated
triangles) as its own dedicated future session, rather than implement it now.
Pure research/planning, no code/asset changes.

**Investigated feasibility**:
- **Blender is not available in this environment** — checked directly
  (`which blender`, `find`/`mdfind` for `Blender.app`, all came back empty).
  This matters because an earlier session's own log entry mentions "rigged via
  Blender command-line scripting" for the Maurya/Maratha unique units — that
  must have run under different provisioning than this session has, so it
  can't be assumed available going forward without the user confirming/
  installing it.
- **Unity's own Editor API has no mesh-simplification method** — checked via
  reflection over the entire `UnityEditor`-containing assembly for any type/
  method matching "simplify"/"decimate": only `UnityEditor.MeshUtility` and
  `UnityEditor.InternalMeshUtil` matched by name, and `MeshUtility`'s actual
  methods (`Optimize`, `OptimizeIndexBuffers`, `OptimizeReorderVertexBuffer`)
  are vertex-cache/index-buffer *ordering* optimizations, not polygon
  *reduction* — confirmed by listing every public/non-public static method on
  the type directly, not assumed from the name alone.
- **Internet access confirmed working** (`curl` to github.com/
  raw.githubusercontent.com both succeeded) — meaning a UPM git-URL package
  dependency is actually fetchable in this environment, unlike relying on a
  missing local Blender install.

**Plan written into `docs/ROADMAP.md`** (Section 1's new item + Section 5 item
17): add [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)
(MIT, pure C#, no native binary) via `Packages/manifest.json`, drive it from a
new `Assets/Editor/BuildingMeshDecimator.cs` (same convention as the existing
`MeshyBuildingImporter.cs`) that decimates each civ-specific building's mesh
and repoints its prefab's `MeshFilter` at the result — no gameplay code
changes needed. Recommended process: proof-of-concept on Chola TownCenter
first (the most ornately-detailed case) to pick a real target ratio before
batching the other 44, since the spec's stated 8,000-20,000 tri target implies
a ~99.2% reduction that could visibly collapse fine carved-relief detail at
that extremity — explicitly flagged as a judgment call to make with visual
verification, not a number to hit blindly. Also scoped a new EditMode
regression test (building polycount ceiling) since this exact problem had zero
test coverage before the audit found it live.

**Not implemented this session** — per the user's own explicit instruction,
this is scoped only, to be picked up as its own dedicated session.

---

## 2026-09-02 — Civ-by-civ visual quality audit (Roadmap Section 4.1/4.2)

**Scope**: user-requested audit ("do the civ-by-civ visual quality audit," following
a request to flag anything not up to AoE IV graphic quality) against the AoE IV
visual standard already specced in `docs/ROADMAP.md` Section 4.1 (poly count,
texture resolution, material workflow, style differentiation) - Section 4.2 had
flagged this as "not yet audited" for over a month. Pure investigation/reporting,
no code changes - findings written into Section 4.2 directly, replacing its stale
pre-civ-model-landing text.

**Method**: live, not estimated. Play mode via UnityMCP; `execute_code` spawned
every one of the 45 civ-specific buildings (5 civs x TownCenter/Barracks/Tower/
Market/Farm/House/Wall/Gate/Dock) one at a time via `BuildingModelFactory.Spawn`
directly, measuring real triangle counts (`MeshFilter.sharedMesh.triangles.Length/3`),
texture resolution, and shader name off the actual spawned `Renderer`/`Material`
objects before destroying each and moving to the next. Also measured a shared
Soldier (Human Character Dummy), the Pillar Edict Scholar unique unit, and the War
Galley the same way. Screenshotted TownCenters and a Barracks side by side for the
style-differentiation half of the audit (qualitative, not measurable).

**Findings** (full detail now in Roadmap Section 4.2 - summary here):

1. **Building polycount badly fails spec - the headline finding.** Every
   civ-specific building measured came back ~1.7-2.0 million triangles (Chola
   TownCenter: 1,828,917 tris, one single un-decimated mesh) against a spec of
   8,000-20,000 - roughly 100-250x over budget. Raw undecimated Sketchfab-style
   exports, never caught by any prior per-civ import session since those checked
   rotation/scale, not polycount. Texture resolution (2048) and shader workflow
   (`Universal Render Pipeline/Lit`) both pass cleanly on all 43 real civ-specific
   models - polycount is the one real failure.
2. **Style differentiation mostly real, one soft spot**: Rajput (fort/haveli -
   verified via a live-spawned Barracks: arched facade, corner chhatri domes,
   crenellated parapet) and Maratha (dark Deccan hill-fort) both clearly read as
   distinct traditions. Chola and Vijayanagara's TownCenters read as the same
   architectural family at a glance (both stepped-pyramid gopuram towers, same
   stone tone) - Vijayanagara's doesn't clearly land Hampi's more distinct granite
   Deccan style the spec calls for. Maurya's TownCenter (large gilded dome over
   colonnades) is visually striking and clearly distinct from the other 4, but
   reads closer to Mughal/colonial palace architecture than actual Mauryan-era
   style (no big central dome historically) - an authenticity nuance, not a "looks
   bad" finding.
3. Confirmed live (not just from file state): Rajput TownCenter and Maurya Tower
   render the shared fallback model, exactly as expected from this same day's
   earlier broken-model removal - not a new bug.
4. **Shared Human Character Dummy body is the most visually obvious gap,
   confirmed live**: 5 civ soldiers spawned side by side are the identical
   mannequin body, differentiated only by flat color tint. Body swap (Crusader
   Knight) is blocked on missing assets; gear/prop variants remain unsourced. Its
   128x128 palette texture is technically under the 1024 minimum but is a flat-
   color swatch sheet, not a detail texture - no real fidelity loss, not worth
   flagging for re-sourcing on its own.
5. Pillar Edict Scholar (Maurya unique unit) passes cleanly (7,084 tris, 2048
   texture, clean URP/Lit shader) - a good reference example of the pipeline done
   right.
6. War Galley mixes two shaders on one model (`Universal Render Pipeline/Lit` +
   an unconverted raw glTFast `Shader Graphs/glTF-pbrMetallicRoughness`) -
   inconsistent material workflow, low urgency (doesn't look visibly broken).

**Not done this session**: environment props (trees/mines/quarries/farmland)
weren't poly/texture-audited - flagged as remaining scope for a future pass, not
silently skipped.

**Roadmap**: Section 4.2 rewritten with these live findings, replacing its stale
pre-civ-art text. No code/test changes - pure investigation.

---

## 2026-09-02 — Bug fix: pink cow material, grey palm tree material

**Scope**: ad hoc user bug report from a live Play mode screenshot - a livestock
Cow rendered fully magenta/pink, and one of two spawned Palm2 trees looked grey/
flat instead of textured. Not a roadmap item; investigated and fixed directly,
same convention as prior ad hoc bug-report sessions (rally-flag raycast/deposit
soft-lock, etc).

**Root causes were different for each, despite looking like the same "broken
material" symptom**:

1. **Cow (pink)** - `Assets/Resources/Environment/Livestock/SK_Cow.fbx`'s
   `ModelImporter` remaps its material slot to an external material by GUID
   (`285081fa0fe474425b4223e77f374d68`, named "M_Cow") - and that GUID doesn't
   belong to any material asset anywhere in the project (confirmed via a
   project-wide grep across every `.meta` file). Unity's fallback for a
   dangling material reference is its own default pink/magenta material -
   exactly the reported symptom. The `Shepherd_Valley` pack this cow model
   ships with does have `M_Cow.mat`/`Cow_URP.mat` on disk, but **neither is
   actually that missing GUID**, and both use an HDRP-only shader GUID that
   doesn't resolve in this URP project either - so pointing the remap at
   either existing file would have just swapped one broken material for
   another. Fixed by creating a real new `Universal Render Pipeline/Lit`
   material (`Assets/Shepherd_Valley/HDRP/Materials/M_Cow_URP.mat`) wired to
   the pack's own `T_Cow_B/N/M.png` textures (already correctly imported as
   albedo/normal/mask - no import-setting changes needed), then editing
   `SK_Cow.fbx.meta`'s external-material remap to point at this new
   material's real GUID instead of the dangling one.

2. **Palm2 tree (grey)** - a genuinely different bug: `Palm_Leaf.mat`/
   `Palm_Trunk.mat`'s shader was already correctly `Universal Render Pipeline/
   Lit` (confirmed via `manage_material.get_material_info`, not just reading
   the raw YAML) - the problem was that both materials' `_BaseMap`/`_BumpMap`/
   `_MetallicGlossMap` texture slots were empty (`fileID: 0`), so they
   rendered as flat grey/white with no texture at all, not a shader error.
   Root-caused via `git log --diff-filter=D`: the same "Remove
   confirmed-unused asset scrap" commit that deleted the Crusader Knight
   files (flagged blocked in an earlier session today) also deleted
   `Assets/Tree_Packs/PalmTreePack/Prefabs/Textures/` as "PalmTreePack's
   orphaned Prefabs/Textures cache" - true for the *Palm1* pack this texture
   folder primarily served, but wrong for *Palm2*, whose still-in-use
   materials referenced 5 files inside that same folder by GUID. Unlike the
   Crusader Knight files, these were recoverable: `git log --diff-filter=D`
   found the exact deleting commit, and `git checkout <parent commit> --
   <path>` restored the 5 exact texture files (2 `.psd` leaf textures + 1
   `.psd` metallic mask + 2 `.png` trunk textures) at their original paths
   with their original GUIDs intact - confirmed by grepping each restored
   `.meta`'s `guid:` line against what the materials actually reference.
   Re-wired both materials' texture slots via `manage_material` once the
   files existed again.

**Live-verified** via UnityMCP in Play mode (bypassed the CivPicker flow via
`CivilizationSetup.BeginMatch`, same convention prior sessions used):
screenshotted a real spawned `SK_Cow` (now shows its real black-and-white
coat, not pink) and a real spawned `Palm_2_1` tree (now shows real bark
texture on the trunk and real green frond texture on the leaves, not flat
grey). No new console errors from either fix. No test changes needed - both
fixes are pure asset-pipeline/material data, no logic touched (confirmed:
all 145 EditMode tests still pass unmodified).

**Asset-quality note flagged to the user** (per CLAUDE.md's standing
"flag when something needs real art, don't just note a fallback gap"
instruction): the user separately asked to be told whenever an asset isn't at
AoE IV-level quality. Answered directly in chat rather than fixed here (out
of this bug-fix's own scope) - see the chat response for the specific list.

---

## 2026-09-02 — Phase 5: real LAN transport MVP (2-player)

**Scope**: user explicitly asked not to leave Phase 5 (multiplayer determinism)
deferred any further. Clarified scope first (LAN-only, 2 human players; online
play/matchmaking explicitly deferred to "end of the project" per the user's own
words) before any implementation, since the full AoE-Parity execution plan's
Phase 5 checklist was blocked on "a real transport that doesn't exist yet" for:
real cross-peer desync detection, validating resync under real network
conditions, and cross-machine determinism testing. Plan Mode used given the
size (new greenfield subsystem, ~10 new files, edits across 7 existing files).

**What already existed and had to be reused, not rebuilt**: `CommandBus`
(input-delay queue, `InputDelayTicks=4`), `Command`/`MoveCommand`/
`TrainCommand`/`BuildCommand`/`AttackCommand` (live-object/closure-based, not
serializable), `SimClock` (20Hz tick clock), `StateHash` (FNV-1a hash of
Unit.All's positions/faction/health, recomputed once/sim-second), and
`DesyncRecovery.Apply`/`SaveManager.Capture`/`ApplySnapshotToRunningMatch`
(already `[Serializable]`/JSON-round-tripped for F5/F9 quicksave, already
built as the intended transport-facing seam per its own prior comment). None
of the existing multiplayer-foundation code needed restructuring — this
session's job was building the missing pieces around it.

**New code** (`Assets/Scripts/Multiplayer/`):
- `NetworkId.cs` — a deterministic spawn-order integer identity for units/
  buildings, assigned centrally from `Unit.OnEnable`/`Building.OnEnable`
  (one line added to each) rather than touching any of the ~15 factory
  files. Reset once, at the very top of `CivilizationSetup.BeginMatchCore`
  (before any gated spawner activates a match's initial units/buildings —
  had to be here rather than in `SimClock`'s own match-start block, which
  fires a frame later, after those initial spawns already happened).
- `Wire/NetMessage.cs` — pure-data DTOs (`NetMessageEnvelope` + `NetTrainKind`/
  `NetBuildKind` enums) for every command type plus StateHash/ResyncSnapshot/
  Heartbeat/Hello messages. One shared envelope struct (not polymorphic
  types) since `JsonUtility` can't serialize a C# union - `kind` discriminates
  which fields are meaningful.
- `CommandSerializer.cs` — the one place that knows how to go from a real
  `Command` to its DTO (using data already at hand at each origination call
  site, not reflection into private fields) and back (resolving NetworkIds to
  live objects, re-deriving the same delegate shape each factory's own
  `RequestTrain*`/`ExecuteBuild`/`AttackMove` call already uses).
- `LanTransport.cs` — raw TCP (not UDP: lockstep needs guaranteed in-order
  delivery, and LAN latency is irrelevant against the existing 200ms
  `InputDelayTicks` budget), length-prefixed JSON framing, a background
  accept/connect thread and a background receive thread pushing into a
  `ConcurrentQueue` — every Unity API touch stays on the main thread.
- `NetworkDriver.cs` — self-installing `MonoBehaviour` (same convention as
  `SimClock`/`SaveManager`) that drains that queue once per `Update()` and
  dispatches into `CommandBus.EnqueueAt`/`NetworkDesyncMonitor`/
  `DesyncRecovery.Apply`.
- `NetworkMatch.cs` — the "who am I" seam that was missing entirely before
  this: `LocalFaction` (defaults to `FactionId.Player`, so every existing
  single-player/AI-opponent flow is untouched), `RemoteMaxAckedTick` (the
  actual lockstep gate `SimClock` reads).
- `NetworkDesyncMonitor.cs` — exchanges `StateHash` once per simulated second;
  on a real mismatch, the host (authoritative) captures and sends a real
  `MatchSaveData` snapshot, the client applies it via the existing
  `DesyncRecovery.Apply`.
- `LanMatchMenu.cs` — minimal Host/Join entry point, built entirely at
  runtime via plain uGUI (`Text`/`Button`/`InputField`, no 9-slice theming) —
  a disclosed, deliberate visual-polish compromise (flagged per CLAUDE.md's
  own "flag when something needs real art/UI work" rule), not a functional
  gap. Handshake protocol: once TCP connects, host sends `HostHello` (its own
  civ pick + map + a freshly generated seed) and joiner sends `JoinHello`
  (its own civ pick) independently, neither waiting on the other first; each
  side finalizes (`NetworkMatch.Begin` + a new
  `CivilizationSetup.BeginNetworkMatch`) the moment it receives the other's
  Hello.

**Modified**: `Unit.cs`/`Building.cs` (NetworkId assignment hook),
`CivilizationSetup.cs` (NetworkId.Reset() call + new `BeginNetworkMatch` entry
point sourcing civs from the handshake instead of Inspector defaults, mirroring
`BeginScenarioMatch`'s existing pattern; `NetworkMatch.End()` added to
`OnDestroy`), `SimClock.cs` (the actual lockstep gate — ticks won't advance
past `NetworkMatch.RemoteMaxAckedTick`, an initial handshake Heartbeat breaks
the chicken-and-egg at tick 1, ongoing Heartbeats sent after each tick fires;
`NetworkMatch.PendingSeed` takes priority over the single-player reseed
fallback), `BuildingPlacer.cs`/`BuildMenu.cs`/`SelectionManager.cs` (every
hardcoded `FactionId.Player` "who is clicking" reference replaced with
`NetworkMatch.LocalFaction` — ~40 occurrences across the 3 files, all of them
genuinely meant "the local human," never literally "the enum value Player";
each of the 4 order-origination call sites — Move/Attack in
`SelectionManager`, Train in `BuildMenu`, Build in `BuildingPlacer` — now also
sends the matching wire envelope when `NetworkMatch.IsActive`, no-op otherwise).

**Tests**: 18 new EditMode tests (145 total, up from 127) — `NetworkIdTests.cs`
(assignment determinism, stale-reference resolution, separate unit/building
sequences, Reset semantics), `CommandSerializerTests.cs` (receive-side
reconstruction for all 4 command types incl. stale-NetworkId → null, plus a
`NetMessageEnvelope` JSON round-trip fidelity check), and `LanTransportTests.cs`
— genuinely two real OS TCP sockets talking over 127.0.0.1 within one test
process (connect, bidirectional delivery with field fidelity, disconnect
propagation) - the closest thing to "real cross-peer" achievable inside a
single EditMode run.

**Live verification** (Play mode via UnityMCP) — went well beyond the EditMode
tests specifically to prove the "real cross-peer" claim genuinely, not just
in-memory: started a real match, opened a real `LanTransport.StartHost`, and
a real second `LanTransport.StartJoin("127.0.0.1", ...)` socket standing in
for the remote human (a real second OS TCP connection, not a mock). Called
`NetworkMatch.Begin`/`CivilizationSetup.BeginNetworkMatch` directly (bypassing
`LanMatchMenu`'s UI clicks, same "bypass the menu flow" convention prior
sessions used for gameplay-entity spawn testing) to start a genuine
network-gated match. Proved, in order:
1. **Lockstep gating is real, not a no-op**: `SimClock.CurrentTick` stayed
   pinned at 0 across real elapsed Play-mode time until the "remote" peer's
   handshake Heartbeat was actually received - confirmed by directly reading
   `SimClock.CurrentTick`/`NetworkMatch.RemoteMaxAckedTick` between separate
   `execute_code` calls (each call's own synchronous execution blocks Unity's
   main thread, so real wall-clock elapsed *between* calls, not within one, is
   what let `Update()`/ticks actually progress).
2. Once the remote's Heartbeat arrived, `CurrentTick` advanced to exactly the
   acknowledged bound (4) and correctly stalled again there.
3. A locally-originated `MoveCommand` (spawned via the real `SoldierFactory`)
   was serialized and delivered to the "remote" socket with the exact
   scheduled tick/faction/unitNetId/destination intact.
4. A `Move` order **sent from the "remote" socket** for a separately-spawned
   Enemy-faction unit was received, resolved via `NetworkId`, enqueued into
   the real `CommandBus`, and executed at its scheduled tick — the unit's
   `NavMeshAgent` genuinely moved to the exact remote-specified destination
   (verified via position before/after). Hit and worked around one
   test-artifact false alarm along the way: an enemy unit spawned far outside
   the baked NavMesh silently failed `SetDestination` ("not close enough to
   the NavMesh") - not a networking bug, fixed by respawning within the
   actual playable area.
5. The real `StateHash` (recomputed once/sim-second) was sent to the peer and
   matched.
6. **Forced a genuine desync**: sent a deliberately wrong hash from the
   "remote" socket for a tick the host had already hashed - the host's
   `NetworkDesyncMonitor` logged the real mismatch (`[NetworkDesyncMonitor]
   Desync detected...`) and, being host/authoritative, captured a real
   `SaveManager.Capture()` snapshot (3987 bytes of JSON) and sent it over the
   actual TCP connection as a `ResyncSnapshot` message - confirmed by
   dequeuing it from the "remote" socket's own receive queue.

This directly closes 2 of Phase 5's 3 remaining transport-blocked checklist
items with genuine live evidence (real cross-peer desync detection; validating
resync under real network conditions) - not the earlier single-process
synthetic test. **Not claimed as done**: true cross-machine NavMeshAgent/
physics determinism testing - this verification used two real sockets within
one machine/process, not two separate physical machines/OSes, which needs the
user's own second machine to actually run.

**Console**: only 1 self-caused error (the off-NavMesh spawn noted above), and
1 pre-existing unrelated scaffolding warning (`AiController on
'TestAi_MultiFront'...`) - no new errors from any of this session's own code.

**Roadmap/CLAUDE.md**: Section 1's Phase 5 item and
`docs/AOE_PARITY_EXECUTION_PLAN.md`'s Phase 5 checklist updated - both
transport-blocked items closed for the LAN scope, cross-machine testing
explicitly still open pending the user's own hardware.

**Not built this session, deliberately**: online play/matchmaking/NAT
traversal (explicit user instruction to defer to later), reconnect-after-drop,
>2 players, spectators. `MatchSaveData`'s pre-existing v1 fidelity gaps
(in-progress construction/training countdowns, rally points, current unit
orders - see `SaveManager.cs`'s own documented limitations) are inherited
unchanged by the resync path, not newly introduced or fixed here.

---

## 2026-09-02 — Remove 2 broken civ building models + close Naval balance findings

**Note**: this session started on the Crusader Knight body-swap item (Roadmap Section
1/5.10), but its source glTF files (`Assets/importedmodels/Item47/TemplarKnight`,
`.../HospitalierKnight`) turned out to have been deleted the same day, in the
"Remove confirmed-unused asset scrap" commit — genuinely gone, not present in the
working tree or git history in usable form. Flagged to the user rather than restored
silently; user chose to stop that item and pick two different, unblocked follow-ups
instead: removing 2 other pre-existing broken assets, and closing the two Naval
balance findings flagged-but-not-fixed in the 2026-08-28 Naval balance session.

**Part 1 — Rajput TownCenter / Maurya Tower removed (Roadmap Section 5, item 7
follow-up)**: `Assets/Resources/buildings/Rajput/_Source/TownCenter/TownCenter_model.fbx`
and `.../Maurya/_Source/Tower/Tower_model.fbx` are 0 bytes — confirmed via
`git cat-file -s` back through every commit that ever touched them (`9d8b627`,
`319b06f`, and the current HEAD), including the very commits that originally "wired"
them in. There's no earlier good version to recover; the source delivery was
corrupted/incomplete from the start, matching what CLAUDE.md's status already flagged
("2 pre-existing broken civ building models found... not caused by anything recent").
Since asset sourcing isn't a code task and there's no valid file to fall back to,
deleted the broken civ-specific prefabs and their `_Source/` folders entirely
(`Assets/Resources/buildings/Rajput/TownCenter.prefab` + `.meta`,
`Assets/Resources/buildings/Maurya/Tower.prefab` + `.meta`, and both `_Source`
subfolders including their now-orphaned `.meta` files) rather than leaving them in
place silently rendering nothing. `BuildingModelFactory.Spawn`'s existing fallback
chain (`Buildings/{civId}/{resourceName}` → `Buildings/{resourceName}` → nested
variants) already handles a missing civ-specific asset by design — no code changes
needed.

**Live verification** (Play Mode via UnityMCP, `execute_code`): assigned
`CivilizationRegistry` to Rajput/Maurya and spawned a real TownCenter/Tower via
`TownCenterFactory.Place`/`TowerFactory.Place`. Confirmed via `Resources.Load`
directly that the civ-specific paths now return null and the shared fallback paths
still resolve; confirmed the spawned models' first child is named `TownCenter(Clone)`
and `scene(Clone)` respectively — real clones of the shared imported models, **not**
`Procedural_TownCenter`/`Procedural_Tower` (the primitive-shape fallback one level
further down the chain), proving the intended fallback tier was hit, not the last-resort
one. Confirmed the other 8 Rajput and 8 Maurya buildings were untouched by this change
(only the two specific prefabs/folders were removed). All 127 EditMode tests still
pass unmodified — no test targeted these two specific prefabs.

**Docs**: Roadmap Section 5 item 7 updated from "45/45" to "43/45 civ-specific building
models complete," with the 2 removed assets and the reason noted as a pending
re-sourcing need, not a code task.

**Part 2 — Naval balance findings closed (Roadmap Section 1, follow-up to the
2026-08-28 Naval balance pass)**: that session flagged, but deliberately didn't fix,
two adjacent findings. User decided: fix the first, document-only on the second.

1. **`WarGalleyFactory` now applies per-class upgrade bonuses.** Added
   `UpgradeProgress.ClassArmorBonus(faction, UnitClass.Naval)` to both the melee and
   pierce terms in `attackable.ConfigureArmor(...)`, and
   `UpgradeProgress.ClassDamageBonus(faction, UnitClass.Naval)` to
   `attacker.SetDamageBonus(...)` — the exact pattern every land factory
   (`SoldierFactory`, `ArcherFactory`, etc.) already uses. `FishingBoatFactory` is
   untouched — it has no `Attackable`/attacker component (non-combat by design).
   This is currently inert in live play: nothing in `BuildMenu`/`AiController` ever
   advances `UnitClass.Naval`'s per-class research tiers today (a separate,
   pre-existing gap noted in `UpgradeProgress.cs`'s own "item 40" comment) — but it
   makes Naval consistent with the rest of the combat-factory convention instead of
   silently missing out the moment Naval per-class research is ever wired up.
2. **`BoatAttacker`'s 1.5s vs `MeleeAttacker`'s 1.0s attack interval — left unchanged,
   documented as intentional.** Added a comment explaining the difference is Naval's
   own range (4) and move speed (3.0) advantage over land units acting as the
   offsetting tradeoff for its slower cadence, per the reasoning the 2026-08-28
   session itself used to judge the War Galley vs. Siege matchup as
   working-as-designed. No behavior change.

**Live verification** (Play Mode via UnityMCP): spawned a War Galley via
`WarGalleyFactory.Spawn` before and after calling
`UpgradeProgress.AdvanceClassArmor`/`AdvanceClassAttack(faction, UnitClass.Naval)`,
reading `Attackable`'s private `meleeArmor`/`pierceArmor` fields and `BoatAttacker`'s
private `_damageBonus` field via reflection (same convention as prior sessions'
private-field verification, e.g. `Gatherer._dropOff`). Confirmed exactly the expected
deltas: armor 0→0.5/0.5 (both melee and pierce), damage bonus 0→1, matching
`UpgradeProgress`'s own `ClassArmorPerTier`/`ClassDamagePerTier` constants. All 127
EditMode tests pass unmodified (no test asserted the old formula; the added term is
additive and zero unless a Naval tier is actually advanced).

**Console**: no new errors/warnings from either change; the 3 entries present
(a duplicate-AiController scaffolding warning, 2 "depth surface... memoryless"
render-pipeline notices) are pre-existing and unrelated.

**Roadmap/CLAUDE.md**: Section 1's Naval balance item's two adjacent findings marked
closed; Section 5 item 7 updated to 43/45 (see Part 1 above).

**Code changes**: `Assets/Scripts/Units/WarGalleyFactory.cs` (2 one-line additions +
header comment), `Assets/Scripts/Combat/BoatAttacker.cs` (comment only, no logic
change), plus the 4 deleted asset paths from Part 1. `docs/ROADMAP.md`,
`docs/SESSION_LOG.md`, and `CLAUDE.md`'s status section.

---

## 2026-09-02 — AoE-parity Phase 2.3: Siege splash/area damage

**Scope**: closed the AoE-parity Phase 2.3 open item logged in the prior Phase 2 audit
(`docs/ROADMAP.md` Section 1) — Siege had no splash/area damage, so all 5
`FormationController` shapes were cosmetic against it. Implementation was written
before this session (working tree, uncommitted); this session verified it via Unity
MCP (EditMode tests, then live Play Mode) per the strict session protocol.

**Implementation** (already on disk at session start): new
`Assets/Scripts/Combat/HostileFilter.cs` extracts the shared faction-hostility check
out of `BuildingAttacker.IsHostile` so splash resolution and building-attack targeting
share one implementation, not two copies. `MeleeAttacker` gained
`SetSplashRadius(float)` (Siege-only — every other user keeps the default 0/disabled),
`Update()` refactored into `internal Tick(float deltaTime)` (same convention as
`BuildingAttacker`), and `ResolveHit`/`ResolveSplash` — a hit also damages nearby
hostile `Unit.All`/`Building.All` members within `splashRadius` of the primary
target's position, each getting its own `CombatBonus` multiplier. `SiegeFactory` wires
`SetSplashRadius(2.25f)`.

**EditMode verification**: confirmed Unity MCP connectivity, forced an asset
refresh/recompile (external file changes weren't yet imported), zero compile errors.
`Assets/Tests/EditMode/SiegeSplashTests.cs` (5 new tests) initially failed 4/5 with
"Unhandled log message: Destroy may not be called from edit mode" — a variant of the
already-documented `Attackable.TakeDamage` VFX-burst gotcha (`BuildingAttackerTests`),
but this file also uses `LogAssert.Expect` for the pre-existing SetDestination error;
once a test uses `Expect` at all, `ignoreFailingMessages` alone stops suppressing
*other* unexpected error logs in this Unity Test Framework version. Fixed by adding an
explicit `LogAssert.Expect` for the VFX-destroy log, once per hit the test causes (2 for
tests where the primary target and a splash victim both take damage, 1 otherwise). All
127 EditMode tests pass (122 pre-existing + 5 new), no regressions from the `Tick`
refactor.

**Live Play Mode verification — found and fixed a real bug not in this item's own
diff**: per the roadmap's own acceptance criterion, spawned two real 8-Soldier squads
via the actual `SoldierFactory`/`GroupFormation` pipeline (Line vs. Staggered, default
1.5 spacing) and a real `SiegeFactory` attacker per squad, then drove one real attack
cycle through the production `Tick()` path (reflection-invoked once, deterministically,
rather than waiting on real-time `Update()` ticks — a first attempt using real elapsed
wall-clock time hit the project's known "Editor not ticking while unfocused" gotcha,
worked around with `Application.runInBackground = true` +
`EditorApplication.QueuePlayerLoopUpdate()`, but was still unusable for a *precise*
single-hit comparison because of unpredictable tool-round-trip latency between issuing
the attack order and reading results).

Initial result was the **opposite** of the acceptance criterion: Staggered took
*double* Line's casualties (4/8 hit vs. 2/8, 56 vs. 28 total damage) at the shipped
2.25 splash radius. Root cause, confirmed by hand-computing pairwise distances and
then live-verified: `GroupFormation.StaggeredOffset` (pre-existing, unrelated to this
item's diff) paired consecutive unit indices into the *same lateral slot*, offset only
half a spacing apart in depth — tighter together than Line's own full-spacing rank
neighbors — so any splash radius wide enough to span a Line rank's neighbor also spans
a Staggered pair even more easily. Proved mathematically that no splash-radius value
in `SiegeFactory.cs` could fix this (the flaw is in the formation's geometry ordering,
not a tuning gap), so per CLAUDE.md's scope-flagging protocol, asked the user how to
proceed rather than silently expanding scope or reporting a false success. User chose
to fix it in this same session.

**Fix**: rewrote `StaggeredOffset` (`Assets/Scripts/Units/GroupFormation.cs`) to keep
each unit's lateral position identical to what plain Line would give it (reusing
Line's own `RankOffset(index, total, spacing, moveDirection, ...)` call) and push only
odd-indexed units back in depth by `1.5x spacing` — a deliberate Pythagorean choice: a
lateral neighbor's diagonal distance (`spacing`, `1.5x spacing`) then safely clears a
splash radius tuned to just span Line's own 1x-spacing rank neighbors. No existing
test pinned the old formula. Re-ran all 127 EditMode tests (still pass) and re-verified
live: attacking each squad's middle unit, Line now hits 3/8 (42 total damage) and the
fixed Staggered hits only 1/8 (14 total damage) — a clear, measurable 3x reduction,
matching the acceptance criterion.

**Roadmap**: Section 1's Phase 2 batch item 2.3 and Section 6's Phase 2 status
checked off/updated as closed (previously "audited, not implemented"). CLAUDE.md's
"Current status" updated. No committed change yet to `GroupFormation.cs`,
`HostileFilter.cs`, `MeleeAttacker.cs`, `SiegeFactory.cs`, or the new
`SiegeSplashTests.cs` before this session — one scoped commit follows this entry.

---

## 2026-09-02 — AoE-parity Phase 5: resync-on-desync logic

**Scope**: the last remaining Phase 5 item with any doable-now work in it -
"design and implement resync-on-desync logic using the existing StateHash."
Went through Plan Mode first (touches SaveManager, a sensitive existing
system, plus new Multiplayer files). User confirmed continuing into this
specific item after the prior session closed the `BuildingPlacer`/`CommandBus`
wiring and self-consistency test.

**Design**: `StateHash.Compute()` had zero call sites anywhere before this -
now genuinely live. `StateHash.Subscribe()` hooks `SimClock.OnTick` (called
once per match start, same block that reseeds `DeterministicRandom`),
recomputing `LatestHash`/`LatestHashTick` once per simulated second rather
than every tick (no consumer yet to justify hashing full state 20x/sec).
`SaveManager.Capture()` changed `private` → `internal`; `LoadRoutine()`'s
tail (from its second `WipeCurrentMatch()` through `RestoreUnits`) extracted
into a new `internal static ApplySnapshotToRunningMatch(MatchSaveData)` -
the actual wipe-and-restore-dynamic-state operation, reusable outside the
file-based Load flow and deliberately skipping the civ/map/`BeginMatch`
ceremony (only relevant when starting a match from a save file, not
correcting an already-running one). New `Assets/Scripts/Multiplayer/
DesyncRecovery.cs`: `public static void Apply(MatchSaveData)`, a thin
wrapper over `ApplySnapshotToRunningMatch` - kept separate so the "when
hashes disagree, do this" policy has its own transport-facing name to plug
a real transport into later.

**Real bug found and fixed while live-verifying, not before it**:
`SaveManager.Capture()`'s `CaptureFaction` threw a `NullReferenceException`
reading `ResourceStockpile.For(FactionId.Enemy2)` in any standard match
without the 3rd faction enabled - Enemy2's stockpile is scene-authored but
never activated in that case. This class's own `AllFactions` comment already
documented the intended behavior ("Enemy2's entry is just harmless
defaults... when no 2nd AiController ever spawned"), but the actual
implementation didn't do that - it crashed instead. This isn't new-code-only
scope: the pre-existing F5 quicksave feature would have hit the exact same
crash in any normal 2-faction match, just never got exercised that way
before. Fixed with a null-check defaulting to 0 resources, matching the
already-documented intent.

**Tests**: `Assets/Tests/EditMode/DesyncRecoveryTests.cs`, 2 tests -
replaying a diverge-then-recover scenario and confirming `StateHash`
reconverges (with a negative-case guard that the perturbation actually
changed the hash, so the positive case isn't trivially true), plus a direct
non-hash check (exact restored position/health) guarding against a hash
collision masking a real bug. Hit and worked through several genuine
EditMode-only artifacts, each confirmed via direct debugging rather than
assumed:
- `Unit.OnEnable()` doesn't fire synchronously after `AddComponent<Unit>()`
  in EditMode (same gotcha the prior session's `CommandBusDeterminismTests`
  already hit) - worked around the same way (register into `Unit.All`
  directly), applied to both the test's own dummy units and the real
  Factory-spawned replacement units `RestoreUnits` creates.
- `WipeCurrentMatch`'s `Destroy()` (correct for real Play mode) doesn't take
  effect synchronously in EditMode either, so stale pre-recovery units are
  still physically present (and still in `Unit.All`) at the moment recovery
  "finishes" within a single synchronous test method - had to be scrubbed
  and the newly-restored ones found via `Object.FindObjectsByType` instead
  (unaffected by the `Unit.All`-registration gotcha, since it queries
  Unity's own object graph).
- That fix's first draft still failed on a hash mismatch that looked like a
  real recovery bug - direct debugging showed every restored unit's
  position/faction/health was already byte-for-byte correct, and only
  `Unit.All`'s enumeration order (via `FindObjectsByType`, which doesn't
  preserve creation order) differed from the original capture order -
  `StateHash` folds order-sensitively, so a real Play-mode frame boundary
  (where `OnEnable` registers units in actual spawn order) wouldn't hit this
  at all. Fixed by sorting the found units back into snapshot order
  (matched by position) before re-registering.
- `LogAssert.ignoreFailingMessages = true` does not suppress the resulting
  Editor-only "Destroy may not be called from edit mode" error in this Unity
  Test Framework version (confirmed directly, twice) - neither does swapping
  `Debug.unityLogger.logHandler` to filter it (the test framework's own log
  capture sits ahead of that hook). The only mechanism that actually works
  is an *exact-count* `LogAssert.Expect` queue - and the count isn't simply
  "1 per unit": `SoldierFactory.Spawn`'s own weapon attachment
  (`WeaponAttachment.KeepOnlyFirstMesh`) also calls `Destroy()` once per
  extra renderer sub-mesh the sourced weapon model happens to have, on top
  of `WipeCurrentMatch`'s per-unit calls - measured directly off a real test
  run's captured console output (14 and 8 for the two tests respectively),
  not guessed. Also found, separately, that `Unit.All`/`Building.All` being
  shared static lists across the *whole* EditMode run meant another test
  file's imperfect cleanup could leave stale entries that would have
  inflated this count unpredictably - fixed by clearing both in this test
  class's own `[SetUp]` first.

123 EditMode tests total, all pass (up from 121).

**Live verification**: Play mode via UnityMCP, against a real running match
(`CivilizationSetup.BeginMatch`) - not EditMode dummies. Captured a real
snapshot via reflection (`SaveManager.Capture` is `internal`), perturbed a
real worker's position and a real building's HP via `Attackable.TakeDamage`,
confirmed `StateHash.Compute()` differed from the baseline, called
`DesyncRecovery.Apply` with the real baseline snapshot, and confirmed
`StateHash` reconverged to the exact baseline value - with real unit/building
counts intact (8 units, 2 buildings, matching pre-perturbation). Also hit and
recovered from a real mistake mid-session, unrelated to the feature itself:
an EditMode-scene cleanup script (`FindObjectsByType<ResourceStockpile>` +
`DestroyImmediate`, meant to scrub leftover debug objects from earlier
manual `execute_code` testing) accidentally deleted the Main scene's own
real scene-authored `ResourceStockpile` instances. Caught immediately by
re-checking the scene hierarchy afterward rather than assuming success;
fixed by reloading `Main.unity` from disk (safe - nothing had been saved),
confirmed both real instances were back before continuing.

**Roadmap**: Section 6's Phase 5 writeup and Section 5 item 16 both updated -
the doable-now scope of Phase 5 is now fully closed. What's left (real
cross-peer desync detection, validating resync under real network
conditions, cross-machine NavMeshAgent/physics determinism) is genuinely
blocked on a transport that doesn't exist yet, exactly as flagged in the
prior session's investigation - nothing further to do on Phase 5 until then.

---

## 2026-09-01 — AoE-parity Phase 5: BuildingPlacer→CommandBus wiring + self-consistency hash test

**Scope**: the doable-now half of Phase 5, approved by the user after the prior
same-day investigation (report-only, no code) laid out the doable-vs-blocked split.
Two deliverables: close `BuildingPlacer`'s confirmed `CommandBus` bypass, and add the
requested self-consistency test proving `CommandBus`+`StateHash` actually deliver
"same inputs → same state." Went through Plan Mode first per protocol (touches core
placement logic + adds new public surface to `CommandBus`).

**`BuildCommand`**: new `Assets/Scripts/Multiplayer/BuildCommand.cs`, same
delegate-over-`Action` shape as `TrainCommand`/`AttackCommand` (no shared
"placeable" interface across Barracks/Farm/House/etc., same reasoning those two
already documented). `BuildingPlacer.TryConfirmPlacement` now only does a
client-side pre-check (so an obviously-doomed click doesn't enqueue a pointless
command) and enqueues; the real resource deduction + `XFactory.Place` call moved
into a new `ExecuteBuild(kind, point)`, which **re-validates**
`IsClearForKind`/`CanAfford` again before spending anything - real game state can
change in the delay window, the same reason `Barracks.RequestTrain` re-checks
rather than trusting `TrainCommand`'s enqueue-time state. `CanAfford`/
`IsClearForKind`/`CurrentFootprint` were refactored to take an explicit
`BuildingKind` parameter instead of reading the mutable `_kind` field - a real
correctness concern, not just style: the command captures a kind at click time,
but the player could start placing a *different* kind before the queued command
executes 4 ticks later, so the deferred re-check must use the captured kind. AI's
own placement (`AiController.cs`, calls `Factory.Place` directly) stays untouched,
matching `AttackCommand.cs`'s existing documented precedent that automatic
per-tick AI/simulation decisions don't need queuing, only real player input does.

**Self-consistency test**: `CommandBus.ExecuteTick` changed from `private` to
`internal`, and a new `internal CommandBus.EnqueueAt(tick, command)` added
(bypasses `SimClock.CurrentTick`-relative scheduling) - both purely for direct
EditMode testability, since `SimClock` never ticks in EditMode (`Update()` is
gated on `CivilizationSetup.HasMatchStarted`, always false there). New
`Assets/Tests/EditMode/CommandBusDeterminismTests.cs`, 4 tests: fixed-enqueue-order
execution (the specific guarantee `CommandBus`'s own code comment calls
load-bearing), an unscheduled-tick no-op case, and the actual proof - replaying an
identical sequence of a test-local `Command` against two independently-built,
identical starting worlds (`Unit`+`Attackable`+`FactionMember` GameObjects, the
exact fields `StateHash.Compute()` folds) produces an identical `StateHash`, while
a deliberately different command stream produces a different one (guards against
the test passing trivially by both hashes being wrong the same way). **Hit and
fixed a real test-authoring bug while writing this, not before it**: the negative
case initially failed with both hashes equal regardless of the different delta -
root cause was `Unit.OnEnable()` not firing synchronously right after
`AddComponent<Unit>()` in EditMode (confirmed directly: `Unit.All.Count` stayed 0
immediately after `AddComponent<Unit>()`), meaning `StateHash.Compute()` was
silently folding over zero units the whole time and only ever hashing
`SimClock.CurrentTick` (unchanged between runs) - the exact same gotcha
`BuildingAttackerTests` already documents for `Unit.All`, just not yet hit by any
test that needed `StateHash` specifically. Fixed by registering into `Unit.All`
directly at spawn time, matching `BuildingAttackerTests`' own established
convention, rather than inventing a new workaround.

**Tests**: 121 EditMode tests total (117 + 4 new), all pass - confirmed via a real
`run_tests` call, not assumed. Also re-hit this session's own earlier lesson about
silent compile failures: checked `EditorUtility.scriptCompilationFailed` was false
before trusting the test count, though this time compilation succeeded cleanly on
the first attempt.

**Live verification**: Play mode via UnityMCP, through the real production path -
not a reflection-only shortcut. Started a real match
(`CivilizationSetup.BeginMatch(Chola)`), then drove the actual private
`TryConfirmPlacement()` method via reflection (not a synthetic bypass of it) with a
real `Physics.Raycast` against the real ground collider. Hit and worked around two
real environment quirks along the way, neither caused by this session's changes:
(1) `BeginPlacementBarracks()` is Classical-Age-gated and a fresh match starts in
Ancient Age, so it silently no-op'd on the first attempt - switched to
`BeginPlacementHouse()` (no age gate); (2) the Editor's cached `Input.mousePosition`
was stale/off-screen (`y=-106`, below the window), so `TryGetGroundPoint`'s raycast
missed the ground entirely - worked around by temporarily repositioning the real
Main Camera to guarantee a valid ray/ground intersection (restored immediately
after), rather than fabricating a fake ground point. With a real 500 Wood granted
to the Player stockpile: confirmed Wood and House count were **both unchanged
immediately after the click** (500 Wood, 1 House) while a real `BuildCommand` was
genuinely present in `CommandBus`'s schedule (checked via reflection on its private
`_scheduled` dictionary) - proving the spend/spawn is deferred, not skipped. After
~2 real seconds (10x past the ~200ms `InputDelayTicks` window), confirmed Wood had
actually dropped to 474.5 (30 base House cost × Chola's existing -15% civ discount,
an exact match) and a second House now existed - proving the deferred command
really executes through `SimClock`'s real per-frame tick loop, not just compiles.
`SimClock.CurrentTick` was independently observed to have advanced by hundreds of
ticks across the debugging session, confirming the clock itself was genuinely
live throughout, not stalled.

**Roadmap**: Section 6's Phase 5 writeup and Section 5 item 16 both updated to
"in progress, doable-now part closed" with full detail. Explicitly not done this
session (per the plan's own scope, and the earlier investigation's blocked-on-
transport finding): real cross-peer desync detection, resync/rollback recovery
logic, and cross-machine NavMeshAgent/physics determinism testing.

---

## 2026-09-01 — Roadmap consolidation pass + Phase 5 (multiplayer determinism) investigation

**Scope**: two parts, per instruction. First, a documentation consolidation: fold
`AOE_PARITY_EXECUTION_PLAN.md`'s status into `docs/Roadmap.md` so a brand-new
session that only opens the roadmap (not this log, not the plan doc separately) has
full context. Second, investigate Phase 5 (multiplayer determinism) — report only,
no code — and specifically split what's genuinely doable right now from what's
blocked on a network transport that doesn't exist yet.

**Consolidation**: the companion plan doc turned out to genuinely exist at
`~/Downloads/AOE_PARITY_EXECUTION_PLAN.md` — outside the repo entirely, which is
why an earlier session's status note guessed it was "loose chat text, not a file."
Copied it into `docs/AOE_PARITY_EXECUTION_PLAN.md` so it's actually reachable by a
future session (the roadmap's own new Section 6 references it by that path). Before
writing the summary, cross-checked every "closed" claim against the actual repo
rather than trusting prior session-log text:
- **Phase 1** (Player Color System): confirmed deferred, not implemented — the
  plan doc's own decision note (`## Phase 1`, item 1.1) already recorded the exact
  reasoning and date. Confirmed the adjacent bug fix is still live:
  `CivilizationSetup.ResolveDistinctCivilization` exists at
  `Assets/Scripts/Core/CivilizationSetup.cs:160`.
- **Phase 2** (Combat calibration): confirmed `CombatBonus.Multiplier` (Archer,
  Cavalry) = 2.0f at `Assets/Scripts/Combat/CombatBonus.cs:71`, with the exact
  before/after hit-count and HP% numbers already recorded in that file's own
  comment block (7 hits/7% HP remaining at old 1.5x vs. 5 hits/33% HP remaining at
  the new 2.0x).
- **Phase 3.1** (drop-off buildings): confirmed `LumberCamp.cs`/`MiningCamp.cs`/
  `Mill.cs` exist and `Gatherer.AcceptsDropOff` exists at
  `Assets/Scripts/Resources/Gatherer.cs:365`.
- **Phase 3.2** (team bonus): confirmed `TeamBonus.cs` exists and is actually
  called (not just present) from all 5 claimed hook sites —
  `BuildingPlacer.WoodMultiplierFor`, `WallFactory`, `CavalryFactory` (both Rajput
  and Maratha bonuses), and `Market.EffectiveSellRate`/`EffectiveBuyRate` — via
  direct grep, not a session-log trust.
- **Phase 4.1**: this is the plan doc's label for the already-shipped "General
  garrisoning system" (Roadmap Section 5 item 12), not a separate unimplemented
  item — confirmed `GarrisonPoint.cs`, `GarrisonSeeker.cs`, `BuildingAttacker.cs`
  all exist and `TownCenterFactory.cs:46` adds a `BuildingAttacker` (TownCenter's
  previously-absent baseline `Attacker`).
- **Phase 4.2**: confirmed via this same session's own EditMode run (117/117 pass)
  and the live Play Mode verification already logged in the prior entry.

Wrote all of this into a new `docs/Roadmap.md` Section 6 ("AoE-Parity Execution
Plan Status"), added a pointer to it from the roadmap's own header, and added item
16 to Section 5's priority list marking Phase 5 as the explicit next item.
`CLAUDE.md`'s "Current status" updated to match (and to correct the stale "not a
file in this repo" note about the plan doc, now that its real location is known).

**Phase 5 investigation** (research only — no code written, per instruction to
report the doable-vs-blocked split before proceeding): read
`Assets/Scripts/Multiplayer/CommandBus.cs`, `SimClock.cs`, `StateHash.cs`,
`Command.cs`/`MoveCommand.cs`/`TrainCommand.cs`/`AttackCommand.cs`, and the actual
call sites in `SelectionManager.cs`/`BuildMenu.cs`/`BuildingPlacer.cs`/
`AiController.cs`, plus `SaveManager.cs` for reusable serialization
infrastructure. Findings (see the chat response for the full doable-vs-blocked
split given back to the user):
- `CommandBus`/`SimClock` are real and working: Move and Attack orders
  (`SelectionManager.cs`) and every Train order (`BuildMenu.cs`) are already
  wired through `CommandBus.Enqueue`, executing `InputDelayTicks` (4) ticks later
  in fixed enqueue order — the actual lockstep input-delay-queue pattern, just
  single-process with no peer yet.
- `BuildingPlacer.TryConfirmPlacement` (`Assets/Scripts/Buildings/BuildingPlacer.cs:401`)
  is a confirmed, genuine bypass: it deducts `ResourceStockpile` and calls
  `XFactory.Place(...)` synchronously at click time, not through a `Command` at
  all — unlike Train orders, where `Barracks.RequestTrain`'s own resource
  deduction only happens inside `TrainCommand.Execute()`, i.e. at delayed
  tick-execution time. This is a real, narrow, doable-now gap: build a
  `BuildCommand` mirroring `TrainCommand`'s shape, move the affordability
  check + deduction + `Factory.Place` call into its `Execute()`, keep only the
  ghost-preview/placement-validity check (`IsClearForKind`) at click time. AI's
  own building placement (`AiController.cs`) intentionally stays direct, matching
  the existing precedent that automatic per-tick AI/simulation decisions don't
  need to be queued (`AttackCommand.cs`'s own comment already states this
  design principle for AI attack-move) — this only affects the Player's manual
  placement clicks.
- `StateHash.Compute()` exists and is a real, well-reasoned FNV-1a fold over
  tick/position/faction/health — but it is **never called anywhere** in the
  codebase (confirmed by grep — zero call sites outside its own file). The plan
  doc's framing ("currently only detects desync, doesn't recover from it")
  slightly overstates the current state: nothing currently detects anything live
  either, since nothing calls `Compute()` or compares two results. What exists is
  the hashing primitive, not a live detection loop.
- `SaveManager.cs` (F5/F9 quicksave) is a real, working full-state JSON
  serializer that could plausibly seed a resync/rollback snapshot format —
  but it carries documented v1 limitations (no in-progress construction/
  training countdowns, no unit orders/targets, no `ResourceNode` depletion
  restored) that would resurface as the same gaps in a resync design if reused
  naively.
- Confirmed zero networking/transport code exists anywhere in the repo (grepped
  for socket/transport/netcode-shaped names, found nothing genuine).

**Outcome**: reported the split back to the user in chat rather than proceeding —
per instruction, this phase pauses here for a decision on whether to do the
doable-now `BuildingPlacer`/`CommandBus` wiring (and possibly a
self-consistency-only resync design, testable without a live second machine) now,
or hold the whole phase until real transport work lands.

**Scope**: `AOE_PARITY_EXECUTION_PLAN.md` Phase 4.2 — the last open item from the
2026-08-29 worker mechanics audit. `Gatherer` and `MeleeAttacker` had zero
cross-awareness of each other: a worker being attacked mid-gather never
auto-interrupted into a defensive state, unlike AoE, where villagers either fight
back or flee once hit. Continued into this session from a prior one whose code
changes were already saved on disk (`Gatherer.cs`, `Attackable.cs`,
`MeleeAttacker.cs`, `BoatAttacker.cs`, `BuildingAttacker.cs`, `WildBoar.cs`,
`WorkerFactory.cs`, `CombatResponse.cs`, `WorkerCombatResponseDefaults.cs`, and the
new `GathererCombatResponseTests.cs`) but never run through the EditMode suite or
live-verified — this session's job was exactly that: run the tests, verify live in
Play mode, then log and update the roadmap.

**Design** (as implemented, confirmed by reading the diff): new
`Attackable.OnDamaged` event fires whenever a non-lethal hit lands, carrying the
attacker's own `Attackable`. Every site that calls `Attackable.TakeDamage` now
passes its own `Attackable` as the new optional `attacker` parameter —
`MeleeAttacker`, `BoatAttacker`, `BuildingAttacker`, `WildBoar` — each resolving it
lazily via a `Self` property rather than caching in `Awake`, matching the existing
`GarrisonPoint`/`Repairable` sibling-component-ordering convention. New
`CombatResponse` enum (Fight/Flee) and `WorkerCombatResponseDefaults`, a
hand-written per-civ lookup in the same bespoke-hook convention as `TeamBonus`/
`RajputDefianceHook` (a categorical behavior choice isn't representable as a
`passiveBonuses` `StatModifier`): every civ defaults to Fight — matching the
capability every Worker already had via its own weak `MeleeAttacker`, just now
auto-triggered instead of requiring an explicit attack-move command — except
Maratha, whose guerrilla hit-and-run identity (Ganimi Kava, already reflected in
its Cavalry speed bonus and Phase 3.2 team bonus) extends here: its Workers flee.
`Gatherer.HandleDamaged` subscribes to `OnDamaged` lazily in `Update` (not `Awake`
— `WorkerFactory` adds `Gatherer` before `Attackable`), only interrupts while
actively `MovingToNode`/`Gathering` (a load already in `MovingToDropOff` finishes
its trip rather than losing it, the same carve-out `CancelGather` already uses),
and either turns the worker's own `MeleeAttacker` on the attacker (Fight) or moves
away via a new pure/testable `ComputeFleeDestination` helper (Flee).

**Bugs found and fixed this session, all pre-existing/latent, none in the feature's
own design**:
1. **Ambiguous `DamageType` reference, `WildBoar.cs:118`** — the EditMode suite
   returned 0 tests on the first several runs; root cause was a compile failure
   (`EditorUtility.scriptCompilationFailed == true`) that the recent console log
   didn't surface, only found by grepping the actual Unity `Editor.log` for
   `error CS`. A second, unrelated `DamageType` enum already existed in the global
   namespace (`Assets/Scripts/Data/Scripts/UnitDefinition.cs`, no `namespace`
   block) alongside `KingdomsOfBharat.Combat.DamageType`. C# resolves an
   unqualified name against enclosing namespace scopes *before* consulting `using`
   directives, so `WildBoar.cs`'s newly-added explicit `DamageType.Melee` argument
   (this call previously passed no `DamageType` at all, relying on `TakeDamage`'s
   default parameter, which resolves at its own declaration site and was never
   ambiguous) silently bound to the wrong enum and failed with CS1503. This bug
   had been structurally latent since `UnitDefinition.cs` was written — nothing
   had ever passed an explicit `DamageType` literal from inside
   `KingdomsOfBharat.Wildlife` before this feature. Fixed by fully qualifying:
   `KingdomsOfBharat.Combat.DamageType.Melee`.
2. **`UnitMover._agent` cached in `Awake`**, not lazily. Once compilation was
   fixed, 3 of the 12 new tests failed with `NullReferenceException` at
   `Gatherer.GatherFrom` → `UnitMover.MoveTo`. This is the exact "`Awake` doesn't
   run synchronously right after `AddComponent`" gotcha CLAUDE.md already
   documents for `ConstructionSite`/`Repairable`, but it had never been hit for
   `UnitMover` specifically because no prior EditMode test drove `Gatherer`'s real
   `GatherFrom` (which needs a live mover) end-to-end. Fixed by converting
   `_agent` to a lazy `Agent` property, same pattern as this diff's own
   `MeleeAttacker.Self`/`BoatAttacker.Self`/`BuildingAttacker.Self`.
3. **`MeleeAttacker._mover` cached in `Awake`**, not lazily — same class of bug,
   one level deeper: fixing (2) surfaced a second `NullReferenceException` at
   `MeleeAttacker.AttackMove`, since a Fight-response worker's own `MeleeAttacker`
   hits this same ordering gotcha for its own `UnitMover` reference. Fixed the
   same way (`Mover` lazy property), consistent with the `Self` lazy property this
   diff had already added to the same class for `Attackable`.

Also fixed the test file itself: `GathererCombatResponseTests.CreateWorker` never
added a `UnitMover` component (needed for (2) above to even surface), and after
(2)/(3) were fixed, Unity's newer Test Framework turned out not to suppress the
expected "SetDestination can only be called on an active agent placed on a
NavMesh" Editor error via `LogAssert.ignoreFailingMessages` alone (unlike the
precedent `BuildingAttackerTests` documents for its own Editor-only VFX log) — it
still failed affected tests as an "Unhandled log message" until explicitly
consumed via `LogAssert.Expect`, added once per `MoveTo`-triggering call.

**Tests**: All 117 EditMode tests pass (105 pre-existing + 12 new in
`GathererCombatResponseTests.cs`: Fight/Flee interrupt-and-react behavior, the
idle/null-attacker no-op cases, `ComputeFleeDestination`'s pure direction math,
and the 5 per-civ `WorkerCombatResponseDefaults` cases).

**Live verification**: Play mode via UnityMCP, through the real production event
path (`Attackable.TakeDamage(amount, type, attacker)` → `OnDamaged` →
`Gatherer.HandleDamaged`), not the internal `HandleDamaged` test shortcut. Used
`CivilizationRegistry.Assign(FactionId.Player, civ)` + `WorkerFactory.Spawn` to
spawn real Workers directly (bypassing the mission-select flow, same precedent as
Phase 3.1's own verification) — first attempt spawned off the baked NavMesh
entirely (`NavMeshAgent.isOnNavMesh == false` at world position (100,100));
`NavMesh.CalculateTriangulation()` showed the actual baked bounds are roughly
±49 units, so subsequent spawns used in-bounds coordinates. A Maurya Worker (Fight
default), mid-`MovingToNode` toward a distant `ResourceNode`, hit by a real
`Attackable.TakeDamage` call from a spawned enemy `Attackable`: immediately
`IsWorking` false, `MeleeAttacker.IsAttacking` true, `NavMeshAgent.destination` set
to the attacker's position — then, over the following real ticks, closed the
actual ~4-unit NavMesh-pathed gap down to 0.21 units (visually standing on the
attacker). A Maratha Worker (Flee default) under the identical setup: `IsWorking`
false, `MeleeAttacker.IsAttacking` stayed false throughout, and its real tracked
distance from the attacker increased from 19.9 to 25.9 units over the same window,
never engaging. Screenshot saved to
`Assets/Screenshots/phase4_2_flee_verification.png`. Test GameObjects cleaned up
and Play mode exited afterward.

**Roadmap**: Section 1's "Worker mechanics audit" item's self-defense sub-point
marked closed; new "Worker self-defense/cross-awareness" item added (checked off)
with full implementation detail; Section 5 priority-order list gained item 15.

---

## 2026-09-01 — AoE-parity Phase 3.2: team-bonus / alliance economic stacking layer

**Scope**: `AOE_PARITY_EXECUTION_PLAN.md` Phase 3.2, marked DECISION NEEDED at the end
of Phase 3.1's session (same day). Per instruction, this phase was not started until the
user was asked directly whether they wanted a team-bonus/alliance economic-stacking layer
at all — they confirmed yes, explicitly motivated by wanting the project's systemic depth
to reach AoE IV's level. Also carried over from that same exchange: a new standing
instruction to always flag when a building/character task needs a real art asset rather
than silently using a procedural placeholder (saved to cross-session memory; retroactively
flagged Phase 3.1's own Lumber Camp/Mining Camp/Mill procedural fallbacks against this new
rule before starting this phase's work).

**Research before planning**: dispatched an Explore agent to map the existing diplomacy
and civ-bonus infrastructure before designing anything, per this project's "verify the
plan's assumptions against the repo" discipline. Findings that shaped the plan:
`DiplomacyRegistry.AreAllied` (`Assets/Scripts/Core/DiplomacyRegistry.cs`) already exists
and is N-ary (Enemy and Enemy2 can ally each other, not just Player — `AiController`
already forms alliances dynamically via `TryEvaluateDiplomacy`), toggleable in-game via
`DiplomacyMenu` (F11) — but nothing consumed an alliance for anything beyond vision-
sharing/non-hostility. A `CivilizationDefinition.teamBonus` `StatModifier` field was
already scaffolded (populated from `civ_bonus_template.csv`'s `TeamBonus` column at
CSV-import time, one full row per civ already spelling out a specific team bonus) but
**never read anywhere at runtime** — confirmed by grep. Root cause: `UnitCategory` has no
"Building" entry, and most of the 5 CSV-authored team bonuses target Houses/
fortifications/Markets, so the existing regex-based `ParseNumericEffect` can't reliably
turn their free-text cells into a generic `StatModifier` — `CsvToScriptableObject.cs`'s
own comment block already anticipated exactly this gap, saying the remainder needs
"hand-written hooks... the same way Rajput's dismount-survival and Vijayanagara's
fortification-HP bonus already are." That's the existing bespoke-per-civ-hook pattern
(`UniqueTechDefinition.cs`, `RajputDefianceHook.cs`, `BuildingPlacer.WoodMultiplierFor`/
`StoneMultiplierFor`) this session followed, rather than trying to force all 5 through the
generic (and currently-dead) `teamBonus` field.

**Plan Mode used before implementation** (per CLAUDE.md protocol step 3 — this touches 5
separate gameplay systems across 6 files, clearly nontrivial): wrote and got explicit
approval for a plan naming the exact hook site, exact constant value, and exact stacking
behavior for each of the 5 team bonuses before any code was written.

**Implementation**: new `Assets/Scripts/Core/TeamBonus.cs` — `HasAlly(FactionId, CivilizationId)`
(true if the faction has at least one *other* allied faction of that civ; explicitly
excludes the checking faction itself, since `DiplomacyRegistry.AreAllied(a,a)` is true by
design but a civ's own bonus is a different, already-existing code path from its team
bonus) plus 5 named constants. Wired into the 5 existing per-civ-bonus hook sites the CSV
already specified as each civ's "diluted" team-bonus version, matching the exact numeric
values in `civ_bonus_template.csv`'s last row per civ:
- **Maurya** → `BuildingPlacer.WoodMultiplierFor` (`Assets/Scripts/Buildings/BuildingPlacer.cs`):
  allied factions' Houses cost 25% less Wood. Stayed Player-only, matching this method's
  pre-existing scope (it's an inherently player-only placement tool) — AI's own building-
  cost path doesn't call this method today either, a pre-existing asymmetry not expanded.
- **Vijayanagara** → the identical 3-line `fortificationMultiplier` block already present
  in `WallFactory.cs`/`GateFactory.cs`/`TowerFactory.cs`: allied Wall/Gate/Tower get +15%
  max HP, multiplying alongside the owner's own unique-tech multiplier. Uses the
  factory's `faction` parameter (not hardcoded Player), so this one applies to AI-built
  fortifications too, unlike Maurya's Player-only case above.
- **Rajput** → `CavalryFactory.cs`'s existing `uniqueTechDamageBonus`: allied Cavalry get
  +1 flat damage, added unconditionally (not gated on the ally having researched Warrior
  Clans — matching AoE2/4's "team bonuses are always-on" convention, confirmed as the
  right read of the CSV's "diluted version" wording).
- **Maratha** → `CavalryFactory.cs`'s existing `agent.speed *= FindCategoryMultiplier(...)`
  line: allied Cavalry get +10% move speed, multiplying alongside the owner's own
  civ-wide Cavalry speed bonus.
- **Chola** → `Market.EffectiveSellRate`/`EffectiveBuyRate` (`Assets/Scripts/Buildings/Market.cs`):
  allied Markets get a narrowed +/-5-point spread. This is the one bonus that needed no
  spawn-time baking — `Market`'s rate properties were already live-computed on every read
  (re-evaluating the owner's own unique-tech state each call), so the team-bonus term
  slotted into the same already-dynamic shape and correctly reacts to an alliance forming
  or breaking after the Market was built, unlike the other 4 (which follow this project's
  established non-retroactive, baked-at-spawn convention, same as every other spawn-time
  civ bonus already in the codebase).

**Tests**: 5 new EditMode tests (`TeamBonusTests.cs`) covering `HasAlly` directly — no
alliance set, allied with the matching civ, allied with a different civ, self-exclusion
(explicitly asserting `DiplomacyRegistry.AreAllied(a,a)` is true while `HasAlly(a, ownCiv)`
is still false), and an explicit War relation overriding a state where an alliance might
otherwise be assumed. One new case added to the existing `CivPassiveBonusTests.cs` for
`WoodMultiplierFor`'s new ally path, following that file's own documented convention
(restore Player's civ and reset `DiplomacyRegistry` in a `finally` block, since these
statics persist across tests in the same run). 105 EditMode tests total, all pass (up
from 99 before this session).

**Live-verified in Play mode via UnityMCP**, going beyond the pure-logic unit tests since
this codebase has no existing precedent for EditMode-testing factory-spawn output
directly (confirmed by grep — zero pre-existing Wall/Cavalry/Market factory tests): for
each of the 4 baked-at-spawn bonuses, spawned the same building/unit type once at War and
once after `DiplomacyRegistry.SetAllied`, and compared the exact resulting stat — Wall HP
250→287.5 (exact 1.15x), Cavalry damage 6→7 (exact +1 flat), Cavalry speed 6.5→7.15
(exact 1.10x). For Chola's Market bonus, verified the *same* `Market` instance's rates
changed immediately after calling `SetAllied` with no respawn at all (0.70→0.75 sell,
1.30→1.25 buy) — direct proof of the "already-dynamic, no baking needed" design point.
Also verified the negative/self-exclusion case live, not just in the unit test: a
Vijayanagara-owned Wall spawned while allied with Player stayed at exactly 250 HP (not
287.5) — confirming a civ's own building never double-counts its own team bonus via the
alliance path. No console errors at any point (compile, test run, or Play mode).

Updated `docs/ROADMAP.md` Section 1 (new item, done) and Section 5 (new item 14, done)
and this file. One scoped commit follows, referencing this roadmap item.

---

## 2026-09-01 — AoE-parity Phase 3.1: resource-specific drop-off buildings

**Scope**: `AOE_PARITY_EXECUTION_PLAN.md` Phase 3.1 (resource-specific drop-off
buildings), sequenced before 3.2 (team-bonus/alliance economic stacking, marked
DECISION NEEDED — not started, awaiting the user's direct answer on whether they
want that layer at all before any implementation details are discussed). Same
session as Phase 2's combat-calibration batch, continued at the user's explicit
direction. This closes the last of the 3 worker-mechanics-audit items from
2026-08-29 (Repair and General garrisoning closed earlier the same day).

**Pre-flight verification (per instruction — check the plan's assumptions against
the repo before coding)**: read `Gatherer.cs` and `BuildingFootprint.cs` directly.
Confirmed `Gatherer.FindNearestDropOff` really did hardcode `building is TownCenter`
with zero resource-type awareness — the plan's stated premise held. Also confirmed
`BuildingFootprint.Attach`/`BuildingFootprintTag.GetNearestApproachPoint` (the
NavMeshObstacle-carving + approach-point system fixed for TownCenter's drop-off
case back on 2026-08-29) generalizes to any new building kind with zero code
changes — only a new `BuildingFootprint.DropOffTiles` constant was needed, no
changes to the approach-point math itself. Nothing about drop-off routing or
building placement differed from what the plan assumed.

**Implementation**: 3 new marker buildings — `LumberCamp.cs`, `MiningCamp.cs`,
`Mill.cs` (each mirrors `House.cs`'s lazy-`ConstructionSite`-resolution shape
exactly) — plus matching factories (`LumberCampFactory.cs`/`MiningCampFactory.cs`/
`MillFactory.cs`, mirroring `HouseFactory.cs`: 100 Wood, 200 HP, 2x2 footprint,
5s build time). Routing itself is a new `internal static Gatherer.AcceptsDropOff
(Building, ResourceType)` method: `TownCenter` still accepts every resource type
(stays the universal drop-off, unchanged AoE convention), Lumber Camp accepts only
Wood, Mining Camp accepts both Gold and Stone (matching AoE's own single Mining
Camp building covering both), Mill accepts only Food.
`Gatherer.FindNearestDropOff`'s loop now calls this instead of the old `is
TownCenter` check — everything else about the method (per-faction filtering,
nearest-distance scan over `Building.All`) is unchanged. Made `internal` (not
private) via the project's existing `InternalsVisibleTo("KingdomsOfBharat.Tests")`
grant, same convention as `BuildingPlacer`'s civ-cost-multiplier helpers, so the
routing rule could be tested directly instead of driving a full `Gatherer` state
machine.

Added 3 new procedural fallback silhouettes to `ProceduralBuildingFactory`
(log-pile shed for Lumber Camp, ore-heap shed for Mining Camp, stilted granary for
Mill) rather than letting them silently fall through to `BuildHut`'s generic hut
shape — the exact bug Market's own code comment already flags as previously
having shipped once. Full `BuildingPlacer` wiring: 3 new `BuildingKind` entries,
new hotkeys (J/U/P — checked every existing `KeyCode.*` usage across
`Assets/Scripts` first via grep to avoid colliding with any already-bound key,
including ones bound only in `SettingsMenu.cs`'s rebind list rather than a
placement key, like `T`/`G`/`C`/`R`/`V`), and the usual `TryConfirmPlacement`/
`CanAfford`/`CurrentSize`/`CurrentFootprint` switch-case additions.

**BuildMenu UI wiring** done directly in the live scene via UnityMCP rather than
guessed at blind: read every existing placement button's `RectTransform.
anchoredPosition` first, which revealed the 8 existing placement buttons
(Barracks/Farm/House/Wall/Gate/Tower/Dock/Market) are NOT laid out as one
contiguous column — Barracks/Farm/House sit at y=-4/-40/-76, then Wall/Gate/
Tower/Dock/Market resume at y=-328/-364/-400/-436/-472, with the gap in between
occupied by a *different* button group (Worker/Age/economy-tech buttons for the
TownCenter-selected view) that shares the same screen real estate safely because
`BuildMenu.Update()` never shows both groups at once. The 3 new buttons were
duplicated directly from `DockButton` (preserving its exact RectTransform/Image/
Button/TMP_Text child structure) and placed at y=-508/-544/-580, continuing the
36px-step pattern past Market's slot with no additional collision risk for the
same never-simultaneous reason. `BuildMenu.cs` gained 3 new `[SerializeField]
Button` fields (no dedicated cost-label field needed — matching House/Farm/Wall's
static-text convention, not Dock's dynamic-relabel one, since none of these 3 are
gated on anything at placement time), wired via `manage_components.set_property`
directly onto the live `BuildMenu` component instance and confirmed by reading
the component back before saving the scene.

**Tests**: 8 new EditMode tests (`GathererDropOffTests.cs`) exercising
`AcceptsDropOff` directly — TownCenter against all 4 resource types, each new
camp against its own type and every other type, and a plain `House` (an
unrelated building) rejecting every type. All 99 EditMode tests pass (up from 91
before this session's other work).

**Live-verified in Play mode via UnityMCP**, working around the fact that the
Main scene normally gates all gameplay-entity spawning behind a mission-select
flow: entered Play mode and used `execute_code` to spawn a Player `TownCenter`
and a purpose-built test scenario directly — a Wood `ResourceNode`, a decoy
`House` 3 units away (a real building, but not a valid drop-off for Wood at all),
a `LumberCamp` 6 units away, and a worker ordered to gather. Tracked the worker's
real position across multiple full gather-deposit cycles over ~15 real seconds.
The aggregate Wood-stockpile number turned out to be unreliable evidence on its
own (it jumped by inconsistent amounts between checks — likely some passive
income tick unrelated to gathering — so a rising number alone didn't prove the
routing worked correctly); the actual proof used was reading `Gatherer`'s private
`_dropOff` field via reflection at a moment mid-state, which resolved to the
`LumberCamp` instance specifically, not the nearer `House` (correctly never
selected — not a valid drop-off type) and not the far-away (40 units) `TownCenter`
(also a valid drop-off, but farther) — confirming the "nearest *valid* drop-off"
rule the plan asked for, not just "nearest drop-off of any kind." No console
errors at any point (compile, scene-save, or Play mode).

**Not started this session (per instruction)**: Phase 3.2 (team-bonus/alliance
economic stacking) is DECISION NEEDED — the user has not yet been asked directly
whether they want that layer at all, and no implementation approach should be
proposed until they answer.

Updated `docs/ROADMAP.md` Section 1 (item marked done) and Section 5 (new item 13,
done) and this file. One scoped commit follows, referencing this roadmap item.

---

## 2026-09-01 — AoE-parity Phase 2 (Combat calibration): audited 2.1/2.2/2.3, one approved multiplier change

**Scope**: `AOE_PARITY_EXECUTION_PLAN.md` Phase 2, items 2.1 (audit `CombatBonus` against
AoE4's reference scale, DECISION NEEDED before any change), 2.2 (soft-counter mechanic
audit), 2.3 (formations-vs-Siege splash-damage audit). Same session as Phase 1's
deferral, continued at the user's explicit direction.

**2.1 audit**: confirmed `CombatBonus.Multiplier()` (`Assets/Scripts/Combat/
CombatBonus.cs`) is the actual damage-resolution path (`MeleeAttacker`/`BoatAttacker`/
`BuildingAttacker` all read it) — `CounterMatrix` is genuinely inert, loaded into
`DataRegistry` but never read by any damage-dealing code, confirming CLAUDE.md's
documented separation is real, not stale. Compared current values against AoE4's 2x–3x
hard-counter reference band: Infantry→Archer/Archer→Cavalry/Cavalry→Infantry all sat at
1.5x ("slight advantage," not "hard counter" by that reference); Siege→Building (3x)
and Spearman→Cavalry (2x) already matched. Per instruction, did not assume "more like
AoE4" was automatically correct — instead hand-computed real 1v1 duel outcomes (using
this project's actual flat-armor-subtraction `TakeDamage` formula and real base stats
from `unit_roster_template.csv`) for all three Infantry/Archer/Cavalry pairings at
1.5x/2.0x/2.5x, to show gameplay effect (hits-to-kill, HP retained) rather than just the
raw numbers. Found: Infantry→Archer and Cavalry→Infantry already resolve decisively at
1.5x (loser retains only ~20-30% max HP, 3-4 hits) — raising them further only saves one
hit off an already-fast fight (a "hits to kill" ceiling effect of this project's damage
model), so left unchanged. Archer→Cavalry was the real finding: at 1.5x, Archer "wins"
its supposed hard counter with only 7% of its own HP left when Cavalry finally dies — a
near coin-flip in practice once any pathing/positioning noise is added, not a decisive
counter. **User-approved change**: raised Archer→Cavalry from 1.5x to 2.0x only (2.5x
was modeled and rejected as stronger than needed) — CombatBonus.cs's own comment now
carries the full 7%-vs-33%-HP rationale next to the value.

**2.2 audit**: confirmed no soft-counter mechanic exists anywhere — every relationship in
`CombatBonus`/`CounterMatrix` is pure damage-multiplier; no kiting AI, no
armor-class-based mitigation independent of the multiplier table. Real, undesigned depth
gap per the plan's own framing, not folded into 2.1's implementation.

**2.3 audit**: confirmed Siege has no splash/area damage anywhere (`SiegeFactory` uses a
plain `MeleeAttacker`, single-target, identical to every other unit). `FormationController`'s
5 shapes rearrange units geometrically but nothing in combat resolution reads formation
shape/spacing — so per the plan's own acceptance criterion, the 5 shipped formation types
are cosmetic against Siege specifically, not functional, even though the feature reads as
"closed" on its own terms.

**What changed**: `Assets/Scripts/Combat/CombatBonus.cs` — Archer→Cavalry multiplier
1.5f → 2f, plus an expanded design comment recording the audit numbers and rejection of
2.5x.

**Tests**: `Assets/Tests/EditMode/CombatBonusTests.cs` (new), 2 EditMode tests — the raw
multiplier value, and a full simultaneous round-by-round duel simulation (real
`CombatBonus.Multiplier` + real `Attackable.TakeDamage`, base CSV stats) asserting
Cavalry dies in exactly 5 rounds with the Archer holding ~6/18 HP (33%), pinning the
audit's own hand-computed numbers as a regression test. Hit a genuine multi-layered
tooling snag getting this file to actually compile in: `Assets/Scripts/Data/Scripts/
UnitDefinition.cs` declares a second, global-namespace `enum DamageType` distinct from
`KingdomsOfBharat.Combat.DamageType` — an unqualified `DamageType.Melee` reference
resolved ambiguously and needed full qualification; separately, `refresh_unity(mode=
force, compile=request)` reported successful "idle"/"compiling" transitions several
times in a row without the on-disk `KingdomsOfBharat.Tests.dll` actually changing (still
had a stale AppDomain type list minus the new file) until a `CompilationPipeline.
RequestScriptCompilation(CleanBuildCache)` forced a real rebuild — the underlying real
compile errors (confirmed by reading `~/Library/Logs/Unity/Editor.log` directly, since
`read_console` wasn't surfacing them) were an `Object` ambiguity (`UnityEngine.Object` vs
`System.Object`, from an unnecessary stray `using System;`) and the `DamageType`
ambiguity above. All 91 EditMode tests pass (89 prior + 2 new).

**Manual verification**: Play Mode, via UnityMCP `execute_code`/`manage_camera` — spawned
a real equal-cost squad fight (8 Archers = 600 resources vs. 5 Cavalry = 600 resources,
via the actual `ArcherFactory`/`CavalryFactory` spawners, `AttackMove`-ordered round-robin
at spawn, real ticked `Update()` over ~13 real seconds with `Application.runInBackground`
set per the project's known Editor-ticking gotcha) rather than trusting the 1v1 hand-calc
alone. Result: all 5 Cavalry dead, all 8 Archers alive at 132/144 total HP (92%
retained) — a decisive win, even more lopsided than the 1v1 prediction since the
numbers advantage compounds the counter. Screenshotted
(`Assets/Screenshots/archer_vs_cavalry_2x_staged_fight.png`) and logged to
`Assets/Design/playtest_log.csv` as a deliberate balance change per the project's
existing convention (matching the multi-builder-speed-formula precedent's phrasing).

**Roadmap**: 2.1/2.2/2.3 logged together as one batch in `ROADMAP.md` Section 1 per
instruction (not piecemeal) — see that entry for the full checklist. Session paused here
per instruction, batch entry shown to the user before Phase 3.

---

## 2026-09-01 — AoE-parity Phase 1 (Player Color System): investigated, deferred; live duplicate-civilization bug found and fixed

**Scope**: `AOE_PARITY_EXECUTION_PLAN.md` (a new companion doc handed in this session,
not previously part of `ROADMAP.md`) sequences a "Player Color System" as its first
phase, opening with a DECISION NEEDED item (1.1): resolve the conflict where
`HumanModelFactory.PaletteNameFor()` uses the same tint slot for civ identity that AoE
uses for player identity.

**Real assumption gap found before any code was written**: the plan's own framing —
"assigns one palette entry per player slot at match/scenario setup," acceptance test
"two players on the same civ render as visually distinct player colors" — presumes an
arbitrary-N player-slot system. This repo doesn't have one. `FactionId`
(`Assets/Scripts/Core/FactionMember.cs`) is exactly three fixed factions (Player,
Enemy, Enemy2 — one human, up to two AI), not a general multiplayer lobby. Flagged to
the user rather than silently building infrastructure with no consumer; user agreed
and directed deferring all of 1.1/1.2/1.3/1.4 as scoped, tying it explicitly to the
existing Phase 5 multiplayer-determinism blocker in `AOE_PARITY_EXECUTION_PLAN.md`
rather than deleting it (see that doc's own updated Phase 1 section for the recorded
decision).

**Before deferring, user asked one cheap real-bug check**: can
`CivilizationSetup.Assign` currently put the same civilization on two of the three
fixed factions in the same skirmish today? Confirmed yes, and confirmed it's not
theoretical — `CivilizationSetup`'s `aiCivilization` Inspector default in the actual
scene is Vijayanagara, and `CivPicker` places no restriction on the player's own pick
against it, so a player picking Vijayanagara collides with the AI's default civ on a
totally ordinary first match, no special setup needed. Since civ identity is currently
the *only* body tint that exists (`HumanModelFactory.PaletteNameFor`), a collision
means two factions are visually identical — a small, live version of the exact
"indistinguishable identity" problem Phase 1 was written to solve, but reachable today
with zero new systems.

**Fix**: `CivilizationSetup.BeginMatchCore` (`Assets/Scripts/Core/CivilizationSetup.cs`)
now resolves `Enemy`/`Enemy2` around whatever the player picked (authoritative, never
rerolled) via a new pure `internal static ResolveDistinctCivilization(CivilizationId
desired, ICollection<CivilizationId> alreadyTaken)` helper — falls back deterministically
to the first `CivilizationId` (enum declaration order) not already taken, rather than
randomizing, so results stay reproducible. Enemy2's resolution takes both Player's and
the already-resolved Enemy's civs into account, so all three (when the third faction is
on) end up mutually distinct, not just pairwise-checked against the player.

**Tests**: `Assets/Tests/EditMode/CivilizationSetupTests.cs`, 4 new EditMode tests
(desired-not-taken passthrough, fallback-to-first-untaken-in-enum-order, all-civs-taken
non-throwing fallback, and a direct reproduction of the real Vijayanagara/Vijayanagara
collision). All 89 EditMode tests pass (85 prior + 4 new).

**Manual verification**: Play Mode, via UnityMCP `execute_code` — called the real
`CivilizationSetup.BeginMatch(CivilizationId.Vijayanagara)` against the actual scene's
`CivilizationSetup` component (confirmed its live `aiCivilization` Inspector value is
Vijayanagara before calling, not assumed from the C# default) through the full spawn
pipeline, not the pure function in isolation. Result: `CivilizationRegistry.For(Enemy)`
resolved to Chola (first untaken in enum order), confirmed distinct from Player's
Vijayanagara; the actual spawned Enemy workers are even named "Enemy Chola Worker" by
the pre-existing spawner naming convention. Read each side's live
`Renderer.sharedMaterial.mainTextureOffset` directly (not just a screenshot, since fog
of war hid the Enemy base from a game-view shot in this single-player-perspective
scene) — Player workers at `(0, 0.75)` (Yellow/Vijayanagara's existing row), Enemy
workers at `(0, 0.88)` (Red/Chola's existing row) — genuinely distinct palette rows
through the real `HumanModelFactory.ApplyPaletteMaterial` path, not just distinct
enum values in the registry.

**Roadmap**: no `ROADMAP.md` Section 1 entry added — this was a small ad hoc fix
surfaced while investigating a new companion doc's Phase 1, not itself a scoped
punch-list item. `AOE_PARITY_EXECUTION_PLAN.md`'s own Phase 1 section now carries the
decision/defer record and references this fix. Phase 1 is closed out (deferred, not
implemented) — user explicitly directed moving on to Phase 2 item 2.1 (see the next
log entry, if any, for what came of that) in the same session.

---

## 2026-09-01 — General garrisoning system, AoE IV style (Roadmap Section 1 worker-mechanics-audit item / Section 5 item 12)

**Scope**: the last-but-one of the 3 worker-mechanics-audit items (Repair closed
earlier the same day). The only existing garrison code (`Garrison.cs`) was narrowly
scoped to the Maratha Durg Garrison unique unit — single-slot, Wall/Tower only, sole
effect siege-immunity — not AoE IV's real model: pooled capacity, any eligible unit,
scaling defensive firepower, ejection to a rally point. Explicitly scoped as AoE IV
style (not AoE II's simpler version) per the user's brief. Dedicated resource-specific
drop-off buildings (the audit's last item) queued as the very next session.

**Design decisions made explicitly in Plan Mode, not assumed** (see the approved plan
for full rationale):
1. Only TownCenter and Tower get general garrison capacity + firepower scaling — the
   two buildings with (or gaining) an `Attacker`, matching AoE IV's Keep/TC + Outpost
   model.
2. Wall keeps its exact pre-existing narrow behavior on the *same* class rather than a
   separate one — a `durgOnly` flag on `GarrisonPoint` (capacity 1, only a
   siege-immunity-granting unit may enter) reproduces today's Player-facing behavior
   unchanged. Gate stays ungarrisonable (unchanged - GateFactory never added `Garrison`).
3. Siege-immunity stays a Maratha-Durg-specific bonus layered on top, not "any full
   building is siege-immune" — `GarrisonPoint` counts currently-garrisoned
   immunity-granting occupants rather than assuming a single hardcoded slot.
4. Siege units are excluded from garrisoning entirely (AoE IV siege engines don't
   garrison; `SiegeFactory` untouched).
5. Roster scope: Worker + Soldier/Archer/Cavalry/Spearman + every already-spawnable
   land unique unit (Rajput Royal Guard, Pillar Edict Scholar, Maratha Mavla Raider,
   both War Elephants, the Chola "Naval" Raider — which despite its name has
   `UnitMover` and is land-capable) get the new garrison-entry component. Naval units
   (Fishing Boat, War Galley) untouched — no `UnitMover`, can't reach a land building.
6. Ejection reuses and generalizes the existing `ungarrisonButton` in `BuildMenu`
   rather than a new right-click gesture — right-click on a selected building is
   already claimed by `RallyPoint` target-setting (`SelectionManager.HandleRallyInput`),
   so a second competing meaning on the same click would conflict on TownCenter.
7. Ejected units resume the building's `RallyPoint` if it has one (TownCenter), else
   just step outside and go idle — mirrors `RallyPoint.ApplyTo`, the only existing
   "what should a unit leaving this building do next" mechanism in the codebase (used
   for freshly-trained units). No new "remember what I was doing before I garrisoned"
   state invented, since nothing else in the codebase does that either.

**What changed**:
- `Assets/Scripts/Buildings/Garrison.cs` → renamed to `GarrisonPoint.cs`: pooled
  capacity (`Configure(int capacity, bool durgOnly)`), `TryGarrison`/`UngarrisonAll`
  (replacing the old single-slot `TryGarrison`/`Ungarrison`), `Count` (replacing
  `HasDurgGarrison`), and a `_siegeImmuneOccupantCount` counter driving
  `Attackable.SetSiegeImmune` instead of a single hardcoded slot.
- `Assets/Scripts/Buildings/DurgGarrisonWorker.cs` → renamed/generalized to
  `GarrisonSeeker.cs`: same move-then-act shape as `Builder`/`Repairer`, plus a new
  `grantsSiegeImmunity` flag (set only by `MarathaDurgGarrisonFactory`, via
  `Configure(true)`) that both satisfies a `durgOnly` `GarrisonPoint` and drives
  siege-immunity on whichever building it enters.
- `Assets/Scripts/Combat/TowerAttacker.cs` → renamed/generalized to
  `BuildingAttacker.cs`: `FindNearestHostile()` → `FindNearestHostiles(int count)`
  (same two-loop `Unit.All`/`Building.All` scan, now keeping a small sorted buffer
  instead of one best candidate), and `Update`/`Tick` compute
  `shotCount = 1 + min(garrisonPoint.Count, maxBonusShots)`, firing at up to
  `shotCount` distinct nearest hostiles per interval — the AoE IV "murder holes"
  mechanic (more garrisoned units = more simultaneous arrows at potentially different
  targets, not a flat damage multiplier). Exposed an `internal Tick(deltaTime)`, same
  convention as `Repairable`/`ConstructionSite`, for EditMode test coverage.
- `TowerFactory.cs`: `Garrison`→`GarrisonPoint` (capacity 4, durgOnly:false),
  `TowerAttacker`→`BuildingAttacker` wired to the same `GarrisonPoint`
  (maxBonusShots:3).
- `WallFactory.cs`: `Garrison`→`GarrisonPoint` (capacity 1, durgOnly:true) — the exact
  unchanged Durg-only behavior, just migrated onto the generalized class.
- `TownCenterFactory.cs`: **added an `Attacker` for the first time** — TownCenter had
  none before this item. New `GarrisonPoint` (capacity 8, durgOnly:false) and
  `BuildingAttacker` (damage 8, range 8, interval 1.4s, maxBonusShots:4) — the
  Keep/TC-equivalent, highest capacity/firepower of any building but capped below
  Tower's per-occupant ratio so a fully-garrisoned TC isn't absurd against a real army.
- 11 unit factories gained `GarrisonSeeker` (one `AddComponent` line each):
  `WorkerFactory`, `SoldierFactory`, `ArcherFactory`, `CavalryFactory`,
  `SpearmanFactory`, `RajputRoyalGuardFactory`, `PillarEdictScholarFactory`,
  `MarathaMavlaRaiderFactory`, `MauryaWarElephantFactory`,
  `VijayanagaraWarElephantFactory`, `CholaNavalRaiderFactory`.
  `MarathaDurgGarrisonFactory` swapped its old dedicated `DurgGarrisonWorker` for
  `GarrisonSeeker.Configure(true)`. `SiegeFactory` deliberately untouched.
- `SelectionManager.cs`: `hitGarrison`'s type changed from `Garrison` to
  `GarrisonPoint`; the `DurgGarrisonWorker`-only eligibility check became a general
  `GarrisonSeeker != null` check, so any eligible unit (not just the Durg unit) can be
  ordered to garrison any friendly `GarrisonPoint` it right-clicks — Wall's
  `durgOnly` gate still rejects a non-Durg unit *inside* `TryGarrison`, so this reads
  as a normal "walked up, order silently didn't take" no-op, same shape as any other
  rejected order in this codebase.
- `BuildMenu.cs`: the Ungarrison button's visibility/action generalized from
  `(selected is Wall || selected is Tower) && garrison.HasDurgGarrison` to any
  selected building with `GarrisonPoint.Count > 0`, calling `UngarrisonAll()` —
  TownCenter is covered automatically, no new UI code.
- `UniqueUnitsTests.cs`: 3 pre-existing Garrison tests updated to the new
  `GarrisonPoint`/`GarrisonSeeker` API (the old ones passed a bare `GameObject` with
  no identity marker and still got siege-immunity, since the *old* class's
  "durg-only" gating lived entirely in `SelectionManager`, not inside `Garrison`
  itself — the new class enforces it internally via `GarrisonSeeker.GrantsSiegeImmunity`,
  so the tests now attach one).
- New `Assets/Tests/EditMode/GarrisonPointTests.cs` (6 tests): capacity enforcement,
  `durgOnly` gating both ways, siege-immunity count clearing only once the last
  immunity-granting occupant leaves, `UngarrisonAll` reactivating every occupant.
- New `Assets/Tests/EditMode/BuildingAttackerTests.cs` (4 tests): shot count is
  exactly 1 with no garrison, scales exactly with occupancy, caps exactly at
  `maxBonusShots` regardless of how many more are garrisoned, and does nothing with no
  hostiles in range. Needed `LogAssert.ignoreFailingMessages` around any
  damage-dealing `Tick()` call — `Attackable.TakeDamage` unconditionally spawns a
  `VfxFactory` particle burst (`stopAction: Destroy`), and Unity's Editor logs
  "[Error] Destroy may not be called from edit mode!" once that particle system's own
  stop-action fires outside Play mode. No existing EditMode test had ever exercised
  `TakeDamage` before this, so this gotcha was previously undocumented for it
  specifically — same "expected, not a regression to chase" situation
  `BuildingModelFactoryTests.cs` already documents for its own unrelated Editor-only
  warning, same fix (`LogAssert.ignoreFailingMessages`, precedent already in that
  file). All 85 EditMode tests pass (75 existing + 10 new).

**Real latent bug found and fixed mid-session, via live-verification, not before it**:
`GarrisonSeeker.GarrisonAt` originally called `_mover.MoveTo(target.transform.position)`
and range-checked against that same raw center point (unchanged carry-over from the old
`DurgGarrisonWorker`). For Wall/Tower's small footprints this happened to still work (the
carved-out unreachable zone around the center is smaller than `interactionRange`), which
is presumably why it was never caught before — but TownCenter's 6-tile footprint carves a
NavMeshObstacle with a half-extent up to 3 world units from center, wider than
`interactionRange` (2.5) itself: a `NavMeshAgent` ordered to `target.transform.position`
gets silently re-routed to the nearest valid point on the walkable NavMesh, which can end
up several units away from where the *unit* actually stood relative to the building,
and the range check against the (unreachable) raw center could then never succeed. First
caught live during this session's own UnityMCP Play-mode verification (a worker ordered
to garrison a fresh TownCenter walked up and stopped, `GarrisonPoint.Count` staying 0
indefinitely) — not by the EditMode tests, which drive `GarrisonPoint`/`BuildingAttacker`
directly and never exercised `GarrisonSeeker`'s real pathing at all. Fixed with the exact
same `BuildingFootprintTag.GetNearestApproachPoint`-based approach-point pattern
`Gatherer.ComputeDropOffApproachPoint` already established for the identical class of bug
(computed once per `GarrisonAt` call, not recomputed every frame, matching that
precedent) — re-verified live afterward and confirmed working (see below).

**Live verification (UnityMCP, Play mode)**: spawned a real Player TownCenter, Tower,
and Wall via their factories directly. Garrisoned real Worker/Soldier instances into
TownCenter — confirmed `GarrisonPoint.Count` incrementing as each unit walked in and
deactivated (only after the approach-point fix above; failed silently before it).
Filled TownCenter to its exact capacity (8/8) and confirmed a 9th ordered unit was
rejected and stayed active outside. Measured shot-count scaling precisely against
fresh 5000-HP dummy `Attackable` targets around the Tower (no death/removal noise to
confuse the count): exactly 1 target took damage with 0 garrisoned, exactly 4 distinct
targets took damage once garrisoned with 3 occupants (1 base + 3, matching
`maxBonusShots`) — an exact match, not approximate. Called `UngarrisonAll()` on a
fully-garrisoned TownCenter and confirmed every occupant reactivated and repositioned
outside the building (measured 3.6-5.2 world units from center, none stuck inside).
Re-verified the Durg Garrison case end-to-end through the generalized system: a regular
Soldier ordered to garrison the Wall was rejected by the `durgOnly` gate (stayed active,
`GarrisonPoint.Count` 0), while the real Maratha Durg Garrison unit succeeded and
flipped `Attackable.SiegeImmune` to `true`, then back to `false` after `UngarrisonAll` —
byte-for-byte the same Player-facing behavior as before this generalization.

**Roadmap/CLAUDE.md updates**: Section 1's "General garrisoning system" item checked
off with full outcome detail; Section 5 gained item 12. CLAUDE.md's "Current status"
updated — this item done, dedicated resource-specific drop-off buildings now the
explicit next session.

---

## 2026-09-01 — Repair system (Roadmap Section 1 worker-mechanics-audit item / Section 5 item 11)

**Scope**: the 2026-08-29 worker mechanics audit found Repair completely missing — no
`Repair` anywhere in the codebase. User chose this over the Crusader Knight body swap,
gear/prop variants (needs sourced art first), and General Garrisoning as this
session's item, via `AskUserQuestion`. AoE reference: right-click a damaged
building/ship/siege unit with a worker selected to repair it, at a resource cost
proportional to HP restored.

**What changed**:
- `Assets/Scripts/Combat/Attackable.cs`: new `Heal(float amount)` — incremental/live
  healing, distinct from the existing `RestoreHealth` (absolute, save/load-only).
- New `Assets/Scripts/Combat/Repairable.cs` (target-side): mirrors
  `ConstructionSite`'s shape exactly (`BeginRepair`/`StopRepair` tracking
  `_activeRepairers`, ticks only while `> 0`) and reuses
  `ConstructionSite.SpeedMultiplier` directly for multi-repairer diminishing returns
  instead of a second formula. `IsRepairable` requires not dead, damaged, and (no
  `ConstructionSite` or `IsComplete`) — a foundation mid-build stays Builder's job.
  Every field (`Attackable`, `ConstructionSite`, `FactionMember`) is lazily resolved
  via a property getter, not cached in `Awake()` — same "AddComponent ordering
  hazard" convention Barracks/Dock already use, and the same reason
  `ConstructionSiteTests` avoids relying on `Awake` timing in EditMode (confirmed the
  hard way: an initial `Awake()`-caching draft passed compilation but 4 of 8 new
  tests failed with the target's `Attackable` reading null — Unity doesn't
  guarantee `Awake` has run synchronously right after `AddComponent` inside an
  EditMode test).
- New `Assets/Scripts/Buildings/Repairer.cs` (worker-side): byte-for-byte shape of
  `Builder.cs` (`RepairAt`/`CancelRepair`/`IsRepairing`, in-range `Begin`/out-of-range
  `Stop`).
- **Known, disclosed simplification**: this project doesn't retain each
  building/unit instance's original build/train cost at runtime (those consts are
  spent once at creation in `BuildingPlacer`/`Barracks`/`Dock`, never stored on the
  spawned object) — deriving an exact "half of original cost" per instance would mean
  threading cost data through every factory's `Place`/`Spawn` signature, a much
  bigger change than this item warrants. `Repairable` instead charges a flat
  Wood-per-HP rate keyed off `Attackable.Class`: Building 0.4/HP (cheapest),
  Naval 0.6/HP, Siege 1.2/HP (priciest), 0.5/HP default fallback. Documented as an
  approximation, not an exact per-instance figure — refinable later if the user wants
  exactness, same spirit as Maurya's neutral-gray tint compromise.
- Wired `Repairable` onto all 9 building kinds (Barracks, Farm, House, Wall, Gate,
  Tower, Market, Dock, TownCenter), Siege, and both naval units (Fishing Boat, War
  Galley) — 12 factories, one `AddComponent<Repairable>()` line each, right after the
  existing `ConfigureClass` call. Wired `Repairer` onto `WorkerFactory`.
- `Assets/Scripts/Selection/SelectionManager.cs`: new `hitRepairable` candidate in
  `HandleMoveInput`, following the exact `hitGarrison`/`hitFarm` chain-of-exclusivity
  pattern (friendly-only via `IsFriendlyToPlayer`, per-unit `IsSameFaction` re-check).
  Added `repairer?.CancelRepair()` to every other order branch so starting a
  different task correctly interrupts an in-progress repair.
- `Assets/Scripts/UI/UnitStatus.cs`: added a "Repairing" status line (via
  `Repairer.IsRepairing`), right after "Building" — shown in both
  `SelectedUnitPanel` and `HoverTooltip` since both read this shared helper.

**Tests**: 8 new EditMode tests (`Assets/Tests/EditMode/RepairableTests.cs`), via an
`internal Tick(float deltaTime)` exposed the same way `ConstructionSite.
EnsureInitialized` is (see `Assets/Scripts/AssemblyInfo.cs`'s `InternalsVisibleTo`) —
Update()/Time.deltaTime can't be relied on to tick inside an EditMode test. Covers:
heal + Wood spend at the documented Building rate, per-class rate ordering
(Building < Naval < Siege), the multi-repairer `SpeedMultiplier` reuse, clamping at
max health (and only charging for the actual HP delivered, not the requested amount),
full-health/dead/still-under-construction all correctly reading `IsRepairable ==
false`, and an insufficient-funds tick stalling with zero partial heal/spend. All 75
EditMode tests pass (67 previous + 8 new).

**Live verification** (Play mode via UnityMCP `execute_code`, not just EditMode
tests): spawned a real Barracks via `BarracksFactory.Place` + `CompleteImmediately()`,
damaged it via `Attackable.TakeDamage`, spawned a real Worker via
`WorkerFactory.Spawn`, and called `Repairer.RepairAt` directly (bypassing the mouse
raycast, which UnityMCP can't drive precisely) — the production `SelectionManager`
wiring itself compiled clean with zero console errors, same as the rest of the
project. Confirmed: HP climbed from 102→300 (Barracks MaxHealth 300, melee armor 2)
while Wood dropped by exactly 198 × 0.4 = 79.2 (the documented Building rate);
`Repairer` auto-stopped and `UnitStatus` returned to "Idle" once fully healed;
draining Wood to near-zero mid-repair produced a genuine stall (HP and Wood both
static) that resumed correctly and finished exactly at the documented rate once Wood
was topped back up, with zero value lost across the stall; `UnitStatus` showed
"Repairing" while a worker was actively in range; a live-spawned Siege unit and War
Galley both carry `Repairable` with the correct `UnitClass` (`Siege`/`Naval`). One
observation not chased further given session scope: a second repairer spawned
mid-test to check the diminishing-returns multiplier live never registered as
`IsRepairing` before the target reached full health from real elapsed wall-clock
time between tool calls (Unity keeps ticking between MCP round-trips) — the
diminishing-returns formula itself is already directly covered by an EditMode test
reusing `ConstructionSite.SpeedMultiplier`'s own proven values, so this wasn't
pursued as a live-verification blocker, but is worth a second look if the multi-
repairer path specifically is ever suspected of a bug later.

**Roadmap**: Section 1's Repair system item checked off with a closure summary;
Section 5 gained item 11.

---

## 2026-09-01 — Per-civ soldier visual differentiation, session 1: tint-gap fix + scoping (new Roadmap Section 1 item, supersedes the old Crusader Knight item)

**Scope**: first session of a new initiative — differentiate the 5 civs'
shared soldier body via civ-colored texture + minor gear/prop variants
(not 5 separate bodies), per an earlier planning conversation. Session
protocol required resolving two decisions explicitly in Plan Mode before
writing any code, via `AskUserQuestion`, rather than assuming: (1) whether
an imperfect neutral-gray stand-in for Maurya's tint was acceptable, and
(2) whether this session actually swaps the base body to the Crusader
Knight model or just extends the current dummy's palette. User chose the
neutral-gray stand-in and chose to keep the current body (defer the swap).

**Tint-gap bug found and fixed**: `HumanModelFactory.PaletteNameFor()`
(`Assets/Scripts/Units/HumanModelFactory.cs:158`) only mapped 3 of 5 civs
(Chola→Red, Vijayanagara→Yellow, Rajput→Blue); Maurya and Maratha fell
through `default: return null`, so every Maurya/Maratha unit spawned with
the plain white base material instead of a civ tint — a real bug, not by
design, never previously flagged. Root-caused by reading
`ApplyPaletteMaterial`/`PaletteNameFor` directly rather than guessing.

**Fix methodology**: rather than guessing plausible palette names, wrote a
small Python/Pillow script to pixel-sample all 16 rows of the shared
trim-sheet texture (`Human Character Dummy/Textures/
HumanCharacterDummy_ColorPalette.png`) at the exact UV offset each row
maps to (calibrated against the 6 known materials' own offsets first, to
confirm the sampling method before trusting it on unknowns), then compared
every row's RGB against each civ's canon crest color from
`docs/UI_ART_BRIEF.md` (`CivilizationProfile.cs`'s own accent colors).
Result: Maratha's forest green (#267333) is a near-exact match to the
*existing* `Green` material (squared color distance 601 — closest of all
32 civ×row comparisons) — no new asset needed, just remapped in the
switch. Maurya's warm gray/stone (#807866) has no close match anywhere in
the 16-row sheet; the closest reasonable option is an *unused* neutral-gray
row (offset y=0.9375, RGB≈75,75,75) that had no `.mat` wired to it yet.
Flagged this compromise to the user explicitly (neutral gray, not truly
warm stone) before proceeding — confirmed acceptable, with the explicit
understanding it's trivially swappable later (new texture, same `.mat`
offset field) if a proper warm-gray trim-sheet variant gets sourced.

**Implementation**: created `HumanDummy_Gray.mat` (same shader/shared
texture as the other 6 palette materials, `m_Offset: {x:0, y:0.9375}`) and
its `.meta`, then added `Maurya→"Gray"` and `Maratha→"Green"` cases to
`PaletteNameFor`'s switch, replacing their `default` fallthrough (kept for
any future unwired civ). No other code changes — `ApplyPaletteMaterial`
already handles any named palette generically.

**Base-body decision — made explicitly, not assumed**: per the roadmap's
own standing instruction (Section 5's "Base human body decision" item) to
resolve this deliberately rather than leave it a silent "someday," asked
the user directly via `AskUserQuestion` before writing any code. User chose
to keep the current Human Character Dummy body this session. The
previously-verified Crusader Knight swap (2 known caveats: ~247x
`Animator.humanScale` anomaly, sword/shield/staff props parented to the
scene root instead of a hand bone) remains real, scoped, future work — not
implemented this session, not silently dropped either; documented under the
new roadmap item with its exact remaining caveats preserved.

**Gear/prop variants — scoped, not implemented**: audited
`Assets/Resources/Weapons/` — currently exactly one generic weapon per unit
type (Sword/Bow/Spear/Kanabo), no civ-specific variants exist for any of
them. Wrote a spec (in the roadmap) for the user to source: per-civ
distinct hand-held weapon mesh/texture (highest priority, reusing the
already-proven `WeaponAttachment.AttachToBone` pipeline —
`SoldierFactory.cs`'s sword attachment is the reference implementation),
then a helmet/headgear prop, then a shield-emblem decal reusing existing
crest art. Not guessed at with a low-confidence substitute pack, per the
cursor-pack precedent (Roadmap Section 4.4) — asset sourcing isn't Claude
Code's job per CLAUDE.md.

**Verification**: `refresh_unity(mode=force, compile=request)` came back
clean (no new errors/warnings). All 67 EditMode tests pass (no new
test — this is a pure lookup-table data change with the same shape as the
original 3-civ mapping, which was never separately unit-tested either).
Live-verified in Play mode via UnityMCP: spawned `HumanModelFactory` bodies
for all 5 civs side-by-side via `execute_code` and screenshotted
(`Assets/Screenshots/civ_tint_check.png`) — Chola red, Vijayanagara yellow,
Rajput blue (unchanged, confirming no regression), Maurya now reads as a
clear neutral gray (previously plain white), Maratha now reads as green
(previously plain white). All 5 visually distinct at a glance.

**Docs updated**: `docs/ROADMAP.md` Section 1 (new "Per-civ soldier visual
differentiation" item, annotated the old Crusader Knight item as
superseded), Section 5 (checked off "Base human body decision," added item
10 to the Near-Term Order), and this file, per protocol step 5/6.

**Not done this session (deliberately, per scope)**: the actual Crusader
Knight body swap, and any gear/prop implementation (both need user-sourced
art or a dedicated future session per the body-swap decision above).

---

## 2026-09-01 — Maratha civ-specific building models wired, 9/9 — all 5 civs complete, 45/45 (Roadmap Section 4.3 / Section 5 item 7)

**Scope**: fifth and final content delivery against Section 4.3's civ-specific
building spec, following Chola/Vijayanagara/Rajput/Maurya (same
`MeshyBuildingImporter.cs` pipeline, no code changes needed). Closes Section 5
item 7 entirely: all 5 civs' civ-specific building models now land (45/45).

**Identification**: the prior session's endnote correctly described the raw
delivery under `Assets/Resources/buildings/Maratha/` — 9 folders (7 UUID-named
Meshy exports plus `maratha tower`/`maratha towncenter`), 7 resolved
confidently by Meshy-internal filename (`Stone_Gatehouse`→Gate,
`Stone_Bastion_Wall`→Wall, `Fortified_Harvest_Man[or]`→Farm,
`Fortress_Bazaar`→Market, `Harborstone_Keep`→Dock, `Stone_Citadel_Tower`→
Tower, `Stone_Citadel_of_Ashv...`→TownCenter), leaving the flagged 2-way
ambiguity (`Fortified_Stone_Prison`/`Fortified_Stone_Villa` against
Barracks/House — unlike every prior civ's single 1:1 elimination). This
session's own live geometry inspection (importing both raw FBXs and
screenshotting them in isolation) came back inconclusive — poor camera framing
on the first attempt, not a real ambiguity in the assets themselves. The user
resolved it directly mid-session: Prison→Barracks, Villa→House. No
duplicate-asset trap (byte-diffed all 9 raw FBXs — genuinely distinct, no
Rajput-style Tower/TownCenter mixup).

**Rotation**: 7 of 9 (Gate/Farm/Market/Dock/Barracks/House/TownCenter) needed
`Quaternion.Euler(-90,0,0)` on the nested `<Name>_model` child. TownCenter
(wide/sprawling, like Rajput's and Maurya's own) used the vertex base/tip
density method directly rather than trusting bounds — 8.42:1 base-heavy ratio
on local Z, correctly predicting the correction on the first attempt, same as
Maurya's session. Tower needed the usual civ-blind
`ImportRotationCorrections["Tower"]` counter-rotation treatment and hit the
*exact* Y+90-vs-Y-90 tying-bounds trap the prior session's endnote explicitly
warned about (both candidates give identical Y-tallest bounds through the
runtime stomp) — both screenshotted and checked against the reference
Watchtower concept art before picking `Euler(0,-90,0)` (Y+90 was confirmed
upside down: crenellated parapet cap ended up on the bottom, tapered
lion-pedestal-style base on top). This is the second civ in a row this exact
trap has been correctly caught by following the endnote's instruction to
check both candidates, rather than assuming a "pure Y-spin can't flip
up/down."

**Real mid-session mistake, caught by the user, not by this session's own
process**: Wall shipped with `Quaternion.identity` after this session judged
it correct from a single angled `game_view` screenshot (camera positioned off
to the side, looking down slightly) — the shot looked plausible via parallax
even though the model was actually lying flat on its back with its
crenellated front face pointing straight up at the sky, not standing
vertical. The resulting scaled prefab's bounds (5.92 × 2.66 × Z-depth
initially computed at ~4.82 before the fix, i.e. Z within ~80% of X and
nearly double the Y height) should have been the numerical tell — a wall's
thickness being nearly as large as its own length is not a plausible
proportion — but wasn't caught before reporting the building done. The user
flagged it directly ("the wall is tilted... plus all models have flat side
which will always go on the ground"). Re-verified using a **true top-down
shot** (camera straight down the Y axis) and a **true front-elevation shot**
(camera level with the ground, well back) rather than an angled one: at
identity, the top-down view showed the full detailed crenellated face lying
flat (should show a thin footprint strip instead), and the front-elevation
view showed only a thin edge-on sliver (should show the full wall face
instead) — conclusive proof the model was on its back. `Euler(-90,0,0)` is
correct: true top-down now shows a thin rectangular footprint, front
elevation shows the merlons right-side-up with the corner turret standing,
matching the reference art exactly. Final bounds after the fix: 5.92 (length)
× 2.66 (height) × 1.47 (thickness) — proportions that actually read as a
wall. **Re-checked all other 8 buildings the same rigorous top-down +
front-elevation way as a precaution once this was caught; all 8 (Gate,
Farm, Market, Dock, Barracks, House, Tower, TownCenter) confirmed already
correct — the mistake was isolated to Wall.**

**New process lesson for future civ-model sessions, added to CLAUDE.md's
gotchas**: an angled `game_view`/Scene View screenshot alone is not sufficient
to confirm a building is standing upright — parallax can make a
lying-on-its-back model read as correct from the wrong angle. Always take a
true top-down shot (straight down, checking for a plausible thin/wide
footprint, not a full detailed face) and a true front-elevation shot (camera
level with the ground) for every building, not only the ones that look
bounds-ambiguous.

**Scale**: worker height measured fresh via `WorkerFactory.Spawn` + combined
`Renderer` bounds (1.902692) — matches Maurya's own session's measurement
exactly (same shared Worker asset, as expected). Same ratio hierarchy reused
against the freshly-measured height: TownCenter 11.22 (5.90×H) / Tower 8.00
(4.21×H) / Market 4.85 (2.55×H) / Barracks 4.36 (2.29×H) / Dock 3.81 (2.00×H)
/ Wall 2.66 ≈ Gate 2.65 (1.40×H) / House 2.58 (1.36×H) / Farm 2.09 (1.10×H).
All 9 confirmed to ~0.002 world units of target via live
`BuildingModelFactory.Spawn`, in both Editor mode and real Play mode (entered
Play, spawned all 9 + a worker fresh via `execute_code`, screenshotted,
checked console — only pre-existing unrelated scaffolding/NavMesh warnings
from the test spawn coordinates, no building-model errors). All 67 EditMode
tests pass (no new tests — pure asset-pipeline work, same as every prior civ
in this series).

**Cleanup**: raw source folders (all 7 UUID-named + `maratha tower` +
`maratha towncenter`) deleted only after re-confirming every one of the 9
prefabs still spawns correctly post-deletion (matching Chola's/Rajput's/
Maurya's precedent) — freeing disk space, which sat at 9.8-12Gi free for most
of the session (watched per the prior session's flagged risk; never hit
`ENOSPC` this time). Concept art images stayed at the civ root except
Tower's and TownCenter's, which were bundled inside their own now-deleted raw
folders — same loss pattern as every prior civ (Maurya's Tower/TownCenter
concept art was lost the same way; not a regression specific to this
session).

**Roadmap Section 5 item 7 is now fully closed**: Chola (2026-08-28),
Vijayanagara (2026-08-31), Rajput (2026-08-31), Maurya (2026-09-01), Maratha
(2026-09-01) — all 5 civs, 45/45 civ-specific building models landed.

---

## 2026-09-01 — Maurya civ-specific building models wired, 9/9 (Roadmap Section 4.3 / Section 5 item 7)

**Scope**: fourth content delivery against Section 4.3's civ-specific building
spec, following Chola/Vijayanagara/Rajput (same `MeshyBuildingImporter.cs`
pipeline, no code changes needed).

**Identification**: the prior session's endnote said 8 raw folders existed
under `Assets/Resources/buildings/Maurya/`, one short of 9 — re-checked at the
start of this session per the "confirm it's still accurate" instruction and
found that note stale: there were actually 9 folders (7 resolved confidently
by Meshy-internal filename — `Ivory_Sentinel_Tower`→Tower,
`Domed_Bazaar_Pavilion`→Market, `Lion_Temple_Farmstead`→Farm,
`Lion_Gate_Citadel`→Gate, `Harbor_Lion_Temple`→Dock,
`Ancient_Fortress_Wall`→Wall, plus the human-named `maurya barrack`/
`maurya towncenter` folders), leaving exactly one ambiguous folder
(`Domed_Stone_Sanctuary`) and exactly one unassigned type (House) — resolved
by elimination and confirmed by live geometry inspection (single-story
lion-pillar residence with a dome accent, matching the House concept art).
No duplicate-asset trap like Rajput's Tower/TownCenter mixup this time.

**Real mid-session incident: a corrupted-looking raw import turned out to be a
Unity `ModelImporter` bug, not a bad source file.** Wall's raw FBX imported
with a 0-vertex mesh (`mesh_node`, 0 verts) even after `ForceSynchronousImport`
and a from-scratch byte-identical re-copy (confirmed via `md5`) — genuinely
looked like source corruption. Isolated by importing the *original* untouched
raw FBX via a separate one-off path (UnityMCP's `import_model_file`), which
produced a normal 1,038,109-vertex mesh — proving the bytes were fine and the
difference was purely import settings. Diffed `ModelImporter` fields between
the working one-off import and the broken pipeline copy: identical except
`useFileScale`/`globalScale` (pipeline default: `useFileScale=true,
globalScale=1`, reading the FBX's own embedded file-scale metadata). Setting
`useFileScale=false` on the `_Source/Wall/Wall_model.fbx` importer and
reimporting fixed it immediately (1,038,109 verts). Root cause: this
particular FBX's embedded file-scale metadata is degenerate/unreadable in a
way Unity's importer silently resolves to an empty mesh instead of erroring —
a real, narrow gotcha (only Wall hit it; the other 8 Maurya FBXs all imported
fine with the project's default `useFileScale=true`). Fix has to be reapplied
after every `MeshyBuildingImporter.ImportBuilding` call for Wall specifically,
since re-copying the FBX resets the importer to project defaults; the raw mesh
comes in fully unnormalized without file-scale (~190×36×116 world units before
`extraScale`), which is fine since `extraScale` absorbs it as needed.

**Rotation**: 8 of 9 needed `Quaternion.Euler(-90,0,0)` (Market, Farm, Gate,
Dock, Wall, House, Barracks, TownCenter) — this batch's raw exports lie flat
far more often than Chola's/Vijayanagara's/Rajput's did. Caught a real
false-positive along the way: several of these (Market, House, Barracks) had
*already-Y-tallest* bounds at identity rotation purely by coincidence (their
X and Y extents happened to tie near Meshy's ~1.90 normalization ceiling),
which would have passed a bounds-only check as "already correct." A live
screenshot of Market at identity showed the giveaway: its dome-shaped accent
was bulging out of the *front* face, not the top — caught only by looking,
not by the numbers. Re-confirmed this on every one of the 8, not just the
ones with an obviously-wrong bounds signature.

**Tower — a real mistake that shipped past this session's own verification
once, caught only by the user.** Tower needed the civ-blind
`ImportRotationCorrections["Tower"]` treatment (a runtime `Euler(0,0,-90)`
stomp baked into `BuildingModelFactory`, applied to the prefab root, composed
with whatever correction is baked into the nested `Tower_model` child at
import time). Tested all 6 cardinal single-axis candidates through the real
`BuildingModelFactory.Spawn` path per the established method; `Euler(0,90,0)`
and `Euler(0,-90,0)` were the only two giving Y-tallest bounds — and were
treated as interchangeable, on the (wrong, in this case) assumption carried
over from Vijayanagara's session that "a pure Y-axis spin can't itself
produce an upside-down result." That assumption only holds for a *standalone*
Y rotation; here it's composed with the factory's own Z-axis stomp, and a
Y-then-Z (or Z-then-Y) compound rotation demonstrably *can* invert vertical
orientation — `Euler(0,90,0)` was picked, screenshotted, and reported to the
user as correct, but was actually upside-down (the wide lion-pedestal base
was on top, the crenellated/domed cap was on the bottom — an easy misread in
a single screenshot without the reference art open side-by-side). Caught by
the user from the delivered screenshot, not by this session's own process.
`Euler(0,-90,0)` is the correct one, confirmed against the Watchtower concept
art from two sides after the correction. **Lesson generalized for future
Tower-style corrections**: when a civ-blind runtime stomp is involved, two
candidates tying on Y-tallest bounds are not interchangeable — both still
need independent visual confirmation against the reference art, not just one
of the pair.

**TownCenter (wide/sprawling, not tall/narrow)**: concept art showed a
monumental tiered pyramid complex with a jutting grand staircase, the same
shape category that broke the "Y-tallest bounds" heuristic for Rajput's
TownCenter. Used the vertex base/tip density method directly instead of
testing bounds-based candidates: raw mesh's local Z axis showed a 31.45:1
ratio of vertices in the bottom 15% band vs. the top 15% band (extreme
base-heavy signature), correctly predicting the same `Euler(-90,0,0)`
correction as the other 8 buildings. Confirmed visually from both the
staircase side (descending cleanly to the ground, not into it) and the
opposite side before trusting it.

**Scale**: worker height measured fresh this session via `WorkerFactory.Spawn`
+ live renderer bounds (not reused from memory): 1.902692. Applied the
established ratio hierarchy against that measurement: Tower 8.00 (target
4.2H), Market 4.85 (2.55H), Barracks 4.36 (2.3H), Dock 3.81 (2.0H), Wall 2.66
/ Gate 2.65 (1.4H, matching each other per convention), House 2.58 (1.35H),
Farm 2.09 (1.1H), TownCenter 11.22 (5.9H) — all strictly taller than the
worker, clean descending hierarchy, all landed within ~0.01 of target via
`newScale = target / measuredHeightAtExtraScale1`.

**Disk space**: hit genuine `ENOSPC` mid-session (flagged as a risk by the
prior session's own endnote) — `df -h /` had shown only 1.1Gi free before
starting, and an in-progress Unity FBX reimport failed outright with "Disk
full" once it ran out; even basic shell commands (`df -h`) failed afterward
since there was no space left for the tool's own output file. Per CLAUDE.md's
instruction, stopped and asked the user to free space rather than guessing
what was safe to delete; the user freed the OS Trash, restoring 13Gi. Deleted
all 9 raw source folders under `Assets/Resources/buildings/Maurya/` at the
end (their content is duplicated into `_Source/<Name>/` and every prefab
re-verified spawning correctly after the deletion), matching Chola's/Rajput's
precedent.

**Verification**: every one of the 9 rotation/scale decisions was confirmed
via a live positioned screenshot (not batch/orbit, per the known UnityMCP
`batch="surround"` staleness bug) against its own reference concept art. Sent
key screenshots to the user directly rather than only asserting correctness —
this is what surfaced the Tower mistake above. Re-verified all 9 spawning
correctly (bounds + no console errors) via the real `BuildingModelFactory.Spawn`
path in actual Play mode, alongside a live-spawned worker for scale comparison
(TownCenter dwarfs it, as intended), both before and after the Tower fix. All
67 EditMode tests pass throughout (no new tests — pure asset-pipeline work,
same as every prior civ-model session).

---

## 2026-08-31 — Rajput civ-specific building models wired, 9/9 (Roadmap Section 4.3 / Section 5 item 7)

**Scope**: third content delivery against Section 4.3's civ-specific building
spec, following Chola's 2026-08-28 and Vijayanagara's earlier-today sessions
(same `MeshyBuildingImporter.cs` pipeline, no code changes needed).

**Identification**: the raw delivery under `Assets/Resources/buildings/Rajput/`
only had 8 folders for 9 required building types — no Tower or Wall candidate.
6 of the 8 resolved confidently from explicit Meshy-internal filenames
(`Rose_Palace_Farmstead`→Farm, `Harbor_Palace_of_Rose`→Dock,
`Rose_Palace_Bazaar`→Market, `Rose_Citadel_Gate`→Gate, plus the two
human-named `rajput towncenter`/`rajput barrack` folders); the remaining 2
(`Red_Sandstone_Citadel`, `Rose_Sandstone_Courty`) were genuinely ambiguous —
"Citadel" is also used internally by both the TownCenter and Barracks exports,
so it isn't a reliable signal. Live geometry inspection (import + 6-angle
screenshot vs. reference concept art) showed both looked like the House
reference (compact single-story, corner chhatri domes, front stairs, raised
courtyard) and neither matched Tower's tall-narrow or Wall's long-horizontal
silhouette — flagged to the user as a real content gap rather than force-fit.
User added a new `tower/` folder and identified the two ambiguous ones as Wall
(`Red_Sandstone_Citadel`) and House (`Rose_Sandstone_Courty`).

**A second, more serious mismatch surfaced during verification**: byte-diffing
the new `tower/` folder's FBX against `rajput towncenter/`'s FBX (both
internally named `Rosestone_Citadel`, different job-ID suffixes) showed only
338 bytes differ out of an 80MB file — metadata/timestamps only, same
geometry. Spawning `rajput towncenter/`'s asset through the real pipeline
confirmed it renders as a watchtower, not the grand multi-tier palace shown in
that folder's own bundled concept art. **TownCenter's true raw asset was never
delivered** — what's sitting in `rajput towncenter/` is a duplicate of the
Tower export. Rather than ship a watchtower mislabeled as the civ's Town
Center, the exploratory TownCenter prefab was deleted (`BuildingModelFactory`
falls back to the shared TownCenter model until a real asset arrives) and the
gap flagged to the user instead of decided silently. **User then supplied a
genuine second `towncenter/` export** (a different ~94.6MB FBX, internally
named `Rosestone_Citadel` with yet another job-ID suffix) — confirmed as real
distinct geometry (80M+ bytes differ from the Tower FBX, not ~300) before
spending any time wiring it in.

**TownCenter's orientation needed real rework after two wrong passes shipped
as "confirmed."** The bounds-based heuristic that correctly handled every
other asset this civ (and both Towers in prior civs) — "test the 6 cardinal
90°-rotation candidates through the real spawn path, pick the one giving
Y-tallest bounds, confirm visually" — produced a *confidently wrong* result
here, twice: first a rotation that visually read as a richly-detailed tiered
structure from some angles (shipped, screenshotted, described to the user as
correct) but was actually lying on its side; then `identity` rotation, which
quantitative checks (topmost-vertex-near-center) and better camera framing
*seemed* to support, but was still wrong. The user caught both from live
in-game screenshots, correctly describing the second as "the flat side of the
building should be on the ground... showing the stairs going into the
ground." Root cause: this specific building is a **wide, sprawling fort
complex**, not tall-and-narrow like a Tower — its correct upright orientation
is not its *tallest* possible bounding-box orientation, because a jutting
staircase wing pushes the horizontal (X/Z) extent past the true vertical
height even when correctly assembled. "Pick Y-tallest" silently assumes the
opposite and was never actually validated against this building's true
proportions. What resolved it: reading the raw mesh's vertex data directly
(`MeshFilter.sharedMesh.vertices` transformed by `localToWorldMatrix`) and
computing bottom-15%-band vs. top-15%-band vertex counts per *local* axis
before any rotation — local Z showed a genuine base-heavy signature (203184
vs. 35179, ratio 5.78, direction: low-Z = wide base, high-Z = sparse
dome/finial detail), correctly identifying Z (not the Y-tallest candidate)
as the true up-axis. The fix was `Quaternion.Euler(-90,0,0)` on the nested
model child — visually confirmed against the reference concept art from both
the ornate entrance side (grand cascading tiered domes, matching the
reference almost exactly) and the opposite side (staircase correctly
ascending from ground level to the entrance gate, not descending into the
ground), plus a live Play-mode ground-placement screenshot showing the base
flat against sloped terrain. Because the axis correction changed which
dimension maps to world Y, `extraScale` also had to be recomputed from
scratch against the *new* height axis (final extraScale 91.159, replacing the
wrong-axis-derived 61.559) to hit the same established target height.

**Orientation** (mandatory per-model live visual check against reference art,
not AABB-only, per the gotcha this civ's own predecessor sessions established):
Tower, Farm, Dock, Market, and House were all lying on their back at import
(Y=0.87-1.18 vs. a ~1.9 horizontal axis) and needed `Quaternion.Euler(-90,0,0)`
baked onto the nested model child; Barracks, Wall, and Gate were already
correctly oriented at identity — confirmed via single (non-batch) positioned
screenshots against each building's reference concept art, not assumed from
the bounds numbers alone. Tower separately needed the usual civ-blind
`BuildingModelFactory.ImportRotationCorrections["Tower"]` treatment
(`Euler(0,0,-90)`, applied at runtime on top of whatever's baked at import) —
this asset's correct import-time counter-rotation was `Euler(0,90,0)`, found
by testing all 6 cardinal single-90°-rotation candidates through the real
`BuildingModelFactory.Spawn` path and picking the Y-tallest result, then
confirming visually, matching Vijayanagara's exact methodology rather than
guessing the axis mathematically.

**A tooling issue was hit and worked around**: `manage_camera`'s
`batch="surround"` screenshot mode returned identical cached images regardless
of which GameObject was targeted (confirmed by requesting two visibly
different buildings back-to-back and getting pixel-identical contact sheets)
— not a project bug, a bug in this session's MCP screenshot tooling. Switched
to single (non-batch) positioned screenshots with an explicit
`screenshot_file_name` for every verification shot in this session instead,
which correctly re-rendered each time.

**Scale**: worker height measured fresh this session (1.9027, via
`HumanModelFactory`-spawned live renderer bounds — matches Vijayanagara's
1.903, confirming worker bodies are civ-blind as expected) and applied against
Chola/Vijayanagara's established ratio hierarchy, landing on: TownCenter 10.94
(5.75x), Tower 7.84 (4.12x), Market 4.85 (2.55x), Barracks 4.30 (2.64x), Dock
3.73 (3.17x), Wall≈Gate 2.61 (5.13x/7.81x — these two also needed a much
larger extraScale since their raw imports came in far flatter at
extraScale=1, not because the target ratio changed), House 2.51 (1.89x), Farm
2.06 (2.12x). All 9 confirmed to within ~0.002 world units of target via live
`BuildingModelFactory.Spawn` renderer bounds, in both Editor mode and a real
Play mode session (spawned, measured, screenshotted, no new console errors
beyond pre-existing/unrelated NavMesh warnings from the off-map test
position). All 67 EditMode tests still pass (no new tests — pure
asset-pipeline work, no new logic).

**Not done this session, per user instruction to stop after Rajput**:
Maurya/Maratha (18 models).

---

## 2026-08-31 — Vijayanagara building-model rotation fix (ad hoc, not a roadmap item)

**Scope**: user reported 6 of Vijayanagara's 9 buildings (Dock, Gate, Wall, Farm,
House, Market) spawned misoriented in-scene — some upside down, some sideways —
despite the earlier same-day building-model wiring session (see entry below)
apparently checking each one visually before committing. Root cause: every one
of these raw Meshy FBX exports uses a Z-up axis convention (per the "Watch for
Z-up source packs" gotcha, Roadmap Section 4.4), which the earlier session's
per-building bounds-only check didn't catch — an upright building and one
lying flat on a wide base can have superficially similar AABB dimensions,
unlike a case where the wrong orientation is obviously wider-than-tall.

**Actual scope was all 9/9, not 6/9** — found by re-checking the remaining 3
(TownCenter, Barracks, Tower) per the user's own instruction to re-check Chola
for the same bug, applied here to Vijayanagara's own untested buildings first:
TownCenter and Barracks turned out to have the identical Z-up bug (missed in
the user's report). Tower initially read as correct in a same-methodology
check, but the user flagged it as still upside-down from an in-game screenshot
after the session's first pass — re-verification confirmed the user was right:
the crenellated pillared gallery (top of the reference) was rendering at the
bottom, and the stepped plinth (bottom of the reference) at the top.

**Methodology**: for the 8 non-Tower buildings, instantiated the prefab
directly into `Main.unity` at an isolated Y offset (not via
`BuildingModelFactory.Spawn` — none of these resource names are in the
factory's `ImportRotationCorrections` dict, so a direct instantiate exactly
reproduces the runtime-spawned orientation), captured a 6-angle
`manage_camera(batch="surround")` contact sheet, and compared against the
matching reference concept-art JPEG already sitting alongside each raw source
folder (`Assets/Resources/buildings/Vijayanagara/*.jpg` — the same isometric
renders the earlier session used to identify each building by name, doubling
as orientation reference here). All 8 showed the identical symptom: front/
back/left/right views read as a thin horizontal sliver, with the true façade
only visible from the "top" camera angle — the classic signature of a Z-up
mesh lying on its back in a Y-up scene. Tried `Quaternion.Euler(90,0,0)` first
(produced an upside-down result, confirmed visually), then `Euler(-90,0,0)`,
which read correctly for all 8 — confirmed individually per building, not
assumed from the first result or blanket-applied; they genuinely all needed
the identical correction (same Z-up export pipeline).

Tower needed a different check, since it's the one resource name
`BuildingModelFactory.Spawn` treats specially: the factory unconditionally
stomps the instantiated prefab-root clone's rotation to
`ImportRotationCorrections["Tower"] = Euler(0,0,-90)` on every spawn, on top of
whatever rotation is baked into the nested `Tower_model` child at import time
(`Euler(0,90,0)`, set in the original Vijayanagara wiring session). A plain
direct-instantiate check (as used for the other 8) only reflects the child's
baked rotation, not the combined runtime result, and misjudging that gap is
exactly what produced this session's own initial false-negative on Tower.
Correct verification requires reproducing both rotations together: instantiate
the prefab, apply `Euler(0,0,-90)` to the instantiated root (reproducing the
factory's spawn-time stomp), *then* inspect/adjust the child underneath.
Doing that against the user's screenshot correction found the child's baked
`Euler(0,90,0)` needed to become `Euler(180,90,0)` — a 180° flip on top of the
existing Y-axis bake — confirmed via the same 6-angle screenshot comparison
against the reference (crenellated gallery + cross-windows now correctly at
top, elephant-frieze band + stepped plinth at bottom).

**Fix location**: applied on each prefab's nested `<Name>_model` child
transform (the instantiated FBX, one level below the prefab root) via
`manage_prefabs(open_prefab_stage)` → `manage_gameobject(modify, rotation=...)`
→ `save_prefab_stage` — never on the prefab root itself, since
`BuildingModelFactory.Spawn` unconditionally overwrites
`model.transform.localRotation` (the clone of the prefab root) to either
`Quaternion.identity` or the Tower-specific correction on every spawn, which
would silently revert a root-level fix. This is the same nested-child pattern
`MeshyBuildingImporter.ImportBuilding`'s `modelRotationCorrection` parameter
already establishes.

**Chola re-check**: per the recurrence risk this implies for every future civ
batch, re-checked all 9 already-wired Chola buildings (including its Wall/Gate,
explicitly flagged as the most likely to share this bug) via the same surround-
screenshot method. All 9 read correctly upright — no regression, no fix needed.
Chola's Tower briefly looked lying-down in a close-up wide-FOV surround shot;
re-verified via a proper Scene View screenshot and confirmed upright (a
perspective artifact of a tall thin object viewed close-up, not a real bug) —
this is the same false-negative risk Vijayanagara's own Tower check hit, caught
here only because the shape was unambiguous at a glance; Vijayanagara's Tower
needed the full spawn-stack reproduction to be judged reliably.

**Process gap identified and documented** (see CLAUDE.md's Known Gotchas and
Roadmap Section 4.4): the art-import checklist didn't previously make "verify
orientation against reference art, per model, before committing" an explicit
mandatory step, and didn't call out that the runtime spawn stack (not just the
saved prefab in isolation) is what needs verifying for any resource name with
a factory-level rotation override. Tower's rotation fix was documented as a
one-off in both the Chola and Vijayanagara wiring sessions rather than
generalized into a standing per-model check, which is how 8 more misoriented
models (plus a wrongly-cleared Tower) landed in the very session meant to
audit for exactly this bug. Added as an explicit checklist item now, ahead of
Rajput/Maurya/Maratha's 27 remaining models.

All 67 EditMode tests still pass (no test coverage needed — pure prefab
transform data, no new logic). No `BuildingModelFactory.cs`/
`MeshyBuildingImporter.cs` code changes — fix is prefab-asset-only, matching the
existing `ImportRotationCorrections["Tower"]` precedent for where these
corrections are meant to live.

---

## 2026-08-31 — Vijayanagara civ-specific building models wired (Roadmap Section 4.3 / Section 5 item 7)

**Scope**: second content delivery against Section 4.3's civ-specific building
spec, following Chola's 2026-08-28 session (see that entry below for the pipeline
this one reused unchanged). Raw Meshy AI exports for all 9 Vijayanagara buildings
were already dropped at `Assets/Resources/Buildings/Vijayanagara/` (confirmed:
`Buildings/` and `buildings/` are the same directory on this filesystem —
case-insensitive, same inode; git tracks it under the lowercase spelling).

**Identification**: 8 of 9 folders resolved confidently from Meshy-internal
filenames (`Lotus_Stone_Bazaar`→Market, `Temple_Farmstead`→Farm,
`Elephant_Fortress_Gate`→Gate, `Ancient_Stone_Rampart`→Wall (a rampart is a
fortification wall), `Elephant_Dock_Temple`→Dock, `Elephant_Watchtower`→Tower,
plus the two already human-named `vijayanagara towncenter`/`vijyanagara barrack`
folders). The 9th folder carried a generic `Ancient_Stone_Temple` internal name
(distinct from the barrack folder's more specific `Ancient_Stone_Temple__...`) —
genuinely ambiguous, resolved by importing it and inspecting the actual geometry
(compact bounds, small single-story form with a flat corbelled roofline, matching
the concept art's "modest single-story residence") plus elimination (only House
was left unassigned) rather than guessing from the filename alone.

**Metallic/smoothness packing**: `scratchpad/pack_metallic_smoothness.py` (deleted
after Chola's session, since it lived outside the repo) was recreated — same
approach, plain Pillow file I/O packing metallic(R)+smoothness/inverted-roughness(A)
into one `_metallicSmoothness.png` per building, run before touching Unity at all.
No in-Editor pixel manipulation attempted this time (Chola's session already found
that crashes the Editor process outright).

**Tower rotation**: same underlying issue as Chola's — `BuildingModelFactory`'s
shared `ImportRotationCorrections["Tower"] = Euler(0,0,-90)` is keyed by resource
name only, not civ, and this Tower's raw FBX was already upright at import
(Y-tallest bounds at identity). Rather than guessing the counter-rotation
mathematically (Chola's session found the "obviously correct" cancellation
rotation was wrong axis, because the raw mesh's native tall axis differed between
an ad hoc import and the pipeline's fresh reimport of the same bytes), this session
tested all 6 cardinal-axis single-90°-rotation candidates by spawning through the
real `BuildingModelFactory.Spawn` path and reading rendered bounds for each —
`(0,90,0)` and `(0,-90,0)` were the only two giving Y-tallest bounds. Since a
Y-axis-only correction is a spin around the vertical axis, it cannot itself produce
an upside-down result (unlike an X/Z-axis correction), so — unlike Chola's session,
which needed a screenshot to disambiguate two AABB-equivalent candidates — this one
only needed the screenshot to confirm the general upright read, not to break a tie.
Landed on `Quaternion.Euler(0,90,0)`.

**Scale**: live-instantiated all 9 raw imports (extraScale=1) via the real
`BuildingModelFactory.Spawn` path and found something Chola's session didn't hit —
every building except Wall/Gate rendered at ~1.90-1.904 world-unit height
regardless of building type (TownCenter, Tower, and Farm all landed within 0.001
of each other), essentially identical to the worker's own measured height
(1.903 in this session, vs. Chola's 1.94) — a Meshy export-normalization artifact,
not a meaningful signal about relative size. Wall/Gate came out shorter (0.49/0.36)
only because their long axis is horizontal, not because of any real height
difference in the source data. Rather than deriving fresh ratios from scratch,
applied Chola's already-established *ratio* hierarchy (Tower ~4.1x/Market
~2.55x/Barracks ~2.26x/Dock ~1.96x/Wall≈Gate ~1.37x/House ~1.32x/Farm
~1.08x/TownCenter ~5.75x) against this session's own measured worker height, per
the cross-session memory's explicit instruction not to reuse Chola's *absolute*
values. Final heights: TownCenter 10.94, Tower 7.84, Market 4.85, Barracks 4.30,
Dock 3.73, Wall 2.61, Gate 2.61, House 2.51, Farm 2.06 — a clean descending
hierarchy, all clear of the worker's 1.90.

**Live verification**: spawned all 9 through the real `BuildingModelFactory.Spawn`
path in both Editor mode and actual Play mode (identical results in both),
screenshotted from multiple angles — confirmed the worker is visibly dwarfed by
every building, the Tower reads upright (wide base, tapering shaft, cresting),
Wall and Gate are visually distinct from each other (Wall a long continuous
fluted run with no opening; Gate a more complex structure with a visible
ramp/archway), and TownCenter's silhouette shows real carved tiered-temple detail
(confirmed from a correctly-lit angle after an initial screenshot came out
backlit/silhouetted from one side — not a geometry bug, just directional-light
angle). `read_console` showed zero building-related errors; one pre-existing
"Instantiating material... during edit mode" warning from `TintMaterials` calling
`renderer.materials` while spawning test objects in Editor mode (not Play mode) —
confirmed as existing factory behavior unrelated to this session's changes, and
one "Failed to create agent because it is not close enough to the NavMesh" from
spawning a test worker at an arbitrary off-navmesh coordinate for measurement
purposes, also unrelated to the building models. All 67 EditMode tests still pass
— no new tests added (pure asset-pipeline work, no new testable logic, matching
the task's own scope note).

**Not done this session**: raw source folders left in place (not deleted, unlike
Chola's session, since deletion wasn't explicitly requested this time). Rajput/
Maurya/Maratha (27 models) remain unstarted — same reusable pipeline, same
per-asset live verification requirement, not blind reuse of either prior civ's
numbers.

---

## 2026-08-31 — UI skin polish pass + Maurya crest fixed (Roadmap Section 4.3 / Section 5 item 9)

**Scope**: pixel-verify the 9-slice border/multiplier values landed in the prior UI
display-wiring session (spot-checked visually, not pixel-verified) against actual
runtime rect sizes; flag (not fix) the known Maurya crest color gap per CLAUDE.md's
"asset sourcing/creation is not Claude Code's job" rule.

**9-slice polish**: live-verified all 5 tuned elements in Play mode via UnityMCP —
`ResourceHUD` (`panel_resource_bar`, ppuMultiplier 12), `SelectedUnitPanel`
(`panel_selected_unit` 65, `hp_bar_frame` 13), `HoverTooltip` (`panel_tooltip` 44),
`BuildMenu` command-card buttons (`CommandCardButton` set, 18), `MissionSelectMenu`
buttons (shared `UIStyleTheme` menu-button art). For each, computed the effective
screen-space border (`spriteBorder` texture px ÷ `pixelsPerUnitMultiplier`) against
the element's actual runtime rect size, then confirmed against zoomed crops of real
screenshots. Result: no stretching, pinching, or seams anywhere — the prior session's
tuning already holds up under pixel scrutiny. No border/multiplier code changes made.

Hit the project's known "stale compiled state" gotcha in a new form: on the first Play
session, every `Image.sprite` read back `null` for Resources-loaded UI art (panels,
command-card buttons) despite `Resources.Load` working fine when called directly —
not a real bug, just a stale asset-database state that `refresh_unity(mode=force)`
cleared before re-entering Play mode. Cost real investigation time before being ruled
out; worth remembering alongside the existing script-staleness gotcha in CLAUDE.md.

**Found and fixed one real adjacent bug** (flagged via AskUserQuestion, user said fix
now rather than log-only): `FormationIndicator.cs` — its own always-on top-left
`Canvas`, added in a past "Phase 6 gap-close" session and never touching `Main.unity`
— was anchored at the exact same `(8, -8)` corner as `ResourceHUD`, so the two texts
rendered directly on top of each other in every session, not just this one (visible in
the very first Play-mode screenshot taken this session, before any fix). Moved
`FormationIndicator`'s anchor to `(8, -206)`, just below `ResourceHUD`'s 200x190
footprint. Live-verified fixed via UnityMCP screenshot (clean separation, no overlap).

**Maurya crest**: re-confirmed live (zoomed screenshot crop) that `crest_maurya.png`
renders in the same dark-navy/gold palette as `crest_rajput.png` rather than the
spec'd warm gray/stone (`#807866`) — the two civs' crests are genuinely confusingly
similar on the CivPicker screen. Per CLAUDE.md, this needs a regenerated/recolored
image from the user, not code — flagged back rather than attempted, along with a
ready-to-use Canva prompt built from `docs/UI_ART_BRIEF.md`'s exact template/spec.

**Closed same session**: user generated a new `crest_maurya.png` (Ashokan lion
pillar capital, warm gray/stone lion against a maroon/gold ring) and dropped it in
at the same path/format. No code or import-settings changes needed — the existing
`.meta` (Sprite mode, alignment, etc.) picked it up automatically on the next asset
refresh. Verified the new file's actual pixel colors (not just eyeballing the
thumbnail) before trusting it, then live-confirmed in Play mode via a fresh UnityMCP
screenshot: reads clearly distinct from Rajput's crest now. Roadmap Section 4.3's
Maurya crest item and Section 1's matching pointer both closed 2026-08-31.

All 67 EditMode tests pass (no new tests needed — no new testable pure logic; the
`FormationIndicator` fix is a scene-generated RectTransform constant, already
live-verified visually rather than unit-tested).

---

## 2026-08-29 — Reconcile uncommitted work: finish wiring BuildingFootprint into all factories (ad hoc, not a roadmap item)

**Scope**: user asked to reconcile a working tree with uncommitted changes left over
from in-progress work, not a new roadmap item. Investigated before committing anything.

Found: the previous session's commit (rally-flag raycast + resource-deposit soft-lock
fix) added `BuildingFootprint.cs` and had `Gatherer` query it via
`BuildingFootprintTag.GetNearestApproachPoint`, but never actually wired
`BuildingFootprint.Attach` into the building factories themselves — confirmed via
`git show HEAD:...TownCenterFactory.cs`, which had no `BuildingFootprintTag` call at
all. That meant the previous fix was incomplete: TownCenter (and every other
building) still fell back to raw `transform.position` for the deposit-approach-point
query, the same soft-lock the fix intended to close, just not yet exercised in that
session's test. The intended-but-uncommitted work (10 factory files +
`BuildingPlacer.cs`) was sitting unstaged in the working tree.

**Committed**: wired `BuildingFootprint.Attach` into TownCenter/Barracks/Farm/House/
Market/Tower/Dock factories (square-tile footprint per `BuildingFootprint`'s tile
table, margin-shrunk `NavMeshObstacle`); Wall/Gate keep their own pre-existing
full-footprint obstacle but now also tag themselves (`carveObstacle:false`) so other
buildings' overlap checks see their real shape. `BuildingPlacer`'s ghost-preview
clearance now reads from the same tile table instead of a separate `minClearance`
constant. Also committed: removal of `chola-gopuram`/`farmland` raw Meshy
`.obj`/`.mtl` exports (confirmed unreferenced elsewhere — already baked into
prefabs), a `ProjectSettings.asset` diff (`runInBackground: 1`, matching the
project's own documented Editor-tick gotcha, plus Editor-auto-populated
build/target-OS fields), and a one-line README roadmap-path addition.

**Left alone, by user instruction**: ~200 untracked screenshot files
(`Assets/Screenshots/`, 23MB, accumulated across many past sessions with no clear
commit-all convention — only 12 older ones were already tracked) and a new untracked
`UI_RawOriginals_backup/` folder (119MB of raw source art) — user said leave both
untracked rather than commit or gitignore either.

No new tests (no behavior changed beyond what the previous session's tests already
cover — this was completing that session's own uncommitted wiring, not new logic).
Single commit: `d5d78b3`.

---

## 2026-08-29 — Bug fix: rally-flag raycast + resource-deposit soft-lock (ad hoc, not a roadmap item)

**Scope**: user-reported bug, not a roadmap item — investigate before changing
anything, propose a fix, wait for confirmation. Three symptoms reported: (1) a rally
flag placed near/behind the TownCenter floats mid-air on its roofline instead of
landing on the ground, (2) Wood/Food/Gold/Stone stay at 0 for a full session despite
workers appearing to gather, with Population/Age stuck as a result, (3) a console
message "Can't remove TownCenter (Script) because RallyPoint (Script) depends on it".
User's working hypothesis was that all three shared one root cause (the rally-point
raycast hitting the TownCenter's own collider, and Gatherer's deposit logic being
coupled to that bad rally position) — **investigation found this hypothesis wrong**:
two unrelated real bugs, plus one already-diagnosed-harmless red herring.

**Findings** (via two background investigation agents, code-read only, no changes
until confirmed):
1. **Rally/move raycast bug (real)**: `SelectionManager.HandleRallyInput`/
   `HandleMoveInput` (`Assets/Scripts/Selection/SelectionManager.cs`) used a plain
   unmasked `Physics.Raycast` with no exclusion of the selected building's own
   collider. TownCenter has one large `BoxCollider` spanning its full multi-tier
   model (`BuildingModelFactory.AddBoundsCollider`) — a click aimed near/behind it
   can hit that box before the ground, landing the rally flag on the building's own
   surface.
2. **Deposit soft-lock (real, unrelated to #1)**: `Gatherer.FindNearestDropOff`/
   `TickMovingToDropOff` never referenced `RallyPoint` at all — the original
   hypothesis was wrong here. The actual cause: `Gatherer.interactionRange` (2.5) was
   smaller than the minimum distance a `NavMeshAgent` (radius 0.4) could ever get to
   the TownCenter's raw `transform.position`, which sits inside the `NavMeshObstacle`
   `BuildingFootprint.Attach` carves at half-extent 2.5 around that same point
   (`Assets/Scripts/Buildings/BuildingFootprint.cs`) — a strict regression from the
   AoE building-footprint work landed 2026-08-28, never re-tuned against this. Workers
   walked to the obstacle boundary, stopped just outside `interactionRange`, and
   parked forever without ever calling `Deposit()` (which was itself correctly wired
   to `ResourceStockpile.Add` — not broken). Population-cap-stuck-at-10 and
   Age-never-advancing were confirmed downstream of this one cause (both gated on
   resources that never arrived), not separately broken systems.
3. **Console message (harmless, unrelated to both)**: `RallyPoint`'s
   `[RequireComponent(typeof(Building))]` blocking a reflection-driven test-harness
   component removal — already diagnosed as tooling noise in a prior session's log
   entry, not a gameplay bug.

**Fixes implemented** (both confirmed with user before touching code; a first draft
of fix 1 — a Ground-only `LayerMask` — was caught as itself buggy before
implementation, since the same raycast also resolves gather/attack/build-assist/
staff/garrison clicks, none of which live on a Ground layer, and was corrected before
proceeding):
- **Fix 1**: `HandleRallyInput`/`HandleMoveInput` switched from `Physics.Raycast` to
  `Physics.RaycastAll` + sort-by-distance + skip any hit belonging to the issuing
  entity's own GameObject(s) (`_selectedBuilding` for rally, every currently selected
  unit for move) — new shared `TryRaycastSkipping` helpers, mirroring the
  `RaycastAll` pattern `SelectSingle` already used. Every other hit-type branch
  (node/attackable/site/farm/livestock/garrison) is untouched.
- **Fix 2**: new `BuildingFootprintTag.GetNearestApproachPoint(fromPosition, buffer)`
  (`Assets/Scripts/Buildings/BuildingFootprint.cs`) — a reusable building-geometry
  query, not Gatherer-specific — computes the real point on this building's walkable
  boundary nearest `fromPosition` (matching whatever `NavMeshObstacle` box it actually
  carves, falling back to the raw footprint edge for non-carving buildings), pushed
  outward by `buffer`. `UnitMover` gained a `Radius` accessor (previously private) so
  `Gatherer.ComputeDropOffApproachPoint` could pass `_mover.Radius + 0.1f` as the
  buffer. `Gatherer.TickMovingToDropOff` now walks to and arrival-checks against this
  computed point instead of the building's raw `transform.position`. Generalizes
  automatically to any future dedicated drop-off building (Lumber Camp/Mining Camp
  equivalents, tracked separately in Roadmap Section 1) since it only depends on the
  target having a `BuildingFootprintTag`, which every building gets already.

**Testing**: 7 new EditMode tests (`BuildingFootprintTests.cs`) — `GetNearestApproachPoint`
verified from 5 different approach angles (±X, ±Z, diagonal) against a carving
building, confirming the diagonal case correctly exits through the box's farther
corner distance rather than the same face distance as the axis-aligned cases (an
error caught by the test itself on first run — initial expected value was wrong,
fixed), plus one test confirming a non-carving building uses the raw footprint edge
instead of subtracting `Margin`. All 67 EditMode tests pass. Live-verified in Play
mode via UnityMCP: (a) direct reproduction of the raycast bug — a camera angle
chosen so a ray through screen-center hits the TownCenter's own `BoxCollider` at
y=10.18 under the old single-`Raycast` logic, confirmed the new skip-self logic
correctly falls through to the Ground hit at y=0.53 instead; (b) real gather/deposit
cycles run end-to-end for workers approaching the TownCenter from 5 different
directions (E/W/N/S/diagonal) via `WorkerFactory.Spawn` + `Gatherer.GatherFrom` —
Wood/Stone stockpiles measurably and continuously increased over multiple
observation windows (162 → 202 Wood over one 5s window with 4 concurrent workers;
diagonal-approach worker independently deposited Stone), confirming deposits fire
repeatedly from every tested angle, not just once.

**Not touched**: no roadmap entry (user confirmed not needed, scope stayed within
estimate). The `TestAi_MultiFront` duplicate-AiController console warning flagged in
the bug report was noted but not investigated/fixed per the user's explicit
lower-priority instruction.

---

## 2026-08-29 — Worker mechanics audit + multi-builder construction formula (Roadmap Section 1)

**Scope**: audit Gatherer, Farm/FarmWorker, LivestockWorker, Builder/ConstructionSite,
and worker combat/boar-hunting against 6 AoE reference mechanics (resource walking,
carrying capacity, diminishing multi-builder construction, repair, self-defense,
garrisoning); report findings categorized as matches/partial/missing before touching
any code; then implement the one confirmed small fix (construction speed formula) with
user sign-off, and log the two confirmed-missing systems (repair, garrisoning) plus
the resolved drop-off scope question as new roadmap items rather than implementing
them.

**Audit results**:
- **Resource walking**: matches. `Gatherer.FindNearestDropOff` (`Assets/Scripts/
  Resources/Gatherer.cs`) is real nearest-distance, faction-scoped logic, not a
  simplification — but only ever considers `TownCenter` (no Lumber Camp/Mining
  Camp/Mill equivalent exists). Flagged as a deliberate scope question rather than
  silently expanded; user chose to add dedicated resource-specific drop-off
  buildings (logged as a new roadmap item, not implemented this session — real new
  content).
- **Carrying capacity**: matches. `Gatherer.carryCapacity` (10, tech-multipliable)
  triggers a mandatory drop-off trip.
- **Multi-builder construction**: was flat-linear (`ConstructionSite.Update()`:
  `progress += (dt/buildTime) * activeBuilders`, i.e. `n` workers = exactly `n`x
  speed), confirmed via direct code read before any change. **Fixed this session**
  (see below).
- **Repair**: missing entirely (confirmed via `grep -rl Repair Assets/Scripts` —
  zero hits). Logged as a new roadmap item, not implemented (real new system per
  user's own instruction not to implement this session).
- **Self-defense**: partially matches. `WorkerFactory` gives every worker a weak
  `MeleeAttacker` (2 dmg vs. a Soldier's baseline) plus `Attackable`, so workers can
  fight/hunt boars — but engaging is always an explicit player attack-move command
  through `SelectionManager`/`MeleeAttacker.AttackMove`. `Gatherer` and
  `MeleeAttacker` are fully independent components with zero cross-awareness: a
  worker taking boar damage mid-gather is never auto-interrupted into a defensive
  state, unlike AoE's "fighting interrupts the gather task" pattern. `WildBoar.cs`
  confirmed symmetric on the boar side (damages any `Unit` in range, no
  state-based interruption logic either).
- **Garrisoning**: missing for the general case. The only existing `Garrison`
  component (`Assets/Scripts/Buildings/Garrison.cs`) is narrowly scoped to the
  Maratha Durg Garrison unique unit — single-slot, Wall/Tower only, sole effect is
  toggling siege-immunity — not the AoE reference behavior (any unit garrisons for
  safety + boosts the building's own firepower, ejects to prior task/rally point).
  Real adjacent gap found while scoping this: **TownCenter has no `Attacker`
  component at all** (only Tower does, via `TowerAttacker`), so "boost the
  building's defensive firepower while garrisoned" has no existing TownCenter
  firepower to augment — building this out means adding TownCenter's baseline
  firepower too. Logged as a new roadmap item, not implemented (real new system,
  ties into existing Wall/Tower/TownCenter defense code, needs its own design
  pass).

**Construction formula — implemented this session, with user confirmation first**:
replaced the flat-linear multiplier with AoE II's diminishing-returns curve:
`ConstructionSite.SpeedMultiplier(n) = 1 + 0.6*min(n-1,1) + 0.3*max(n-2,0)` — a new
public static pure function (same "pure/testable helper" convention as
`HoverTooltip.ResolveCursorState`), giving 1x/1.6x/1.9x/2.2x for 1/2/3/4 simultaneous
workers. Applied uniformly across every building type, no per-building exception, per
explicit user confirmation. This is a deliberate balance change to numbers the
existing item-5 balance pass covers (rushing with extra workers is now meaningfully
less efficient — was `n`x, now caps around 2.2x at 4 workers) — logged as a process
note (not a fight row) in `Assets/Design/playtest_log.csv` per instruction, rather
than silent drift.

**Testing**: 5 new EditMode `TestCase`s (`ConstructionSiteTests.
SpeedMultiplier_MatchesAoeIIDiminishingReturnsFormula`) assert the exact predicted
multiplier for n=0..4. All 61 EditMode tests pass (was 56 before this session's
addition). Live-verified in Play mode via UnityMCP, not just the pure-function test:
spawned 4 real `ConstructionSite` components (60s solo build time) with 1/2/3/4
`BeginBuilding()` calls each, let real Play-mode frames tick for several real
seconds, then read `Progress` back. Ratios matched the formula to within float
rounding (1 : 1.59999... : 1.89999... : 2.19999...).

**Real tooling gotcha hit and worked around**: the first live attempt read back
exactly linear 1x/2x/3x/4x ratios despite the source already containing the new
formula and EditMode tests already passing — a live Play Mode session had kept
running a stale pre-edit compiled assembly (`execute_code` could resolve
`ConstructionSite.SpeedMultiplier` for *compiling* new code but threw
`MissingMethodException` calling it at runtime), even after a normal `AssetDatabase`
refresh reported nothing dirty. Only an explicit `refresh_unity` with `mode=force`,
`compile=request` forced the actual domain reload that picked up the change; the
Play Mode session had to be stopped, refreshed, and restarted for the new formula to
actually be live in a running scene, not just compiled. Worth remembering for future
Play-mode-based live verification sessions — a suspiciously-exact old-behavior result
right after a fresh code change is a signal to force-refresh before trusting it.

**Roadmap updates**: Section 1 gained the resolved "worker mechanics audit" entry
plus 3 new unimplemented items (dedicated drop-off buildings, repair system, general
garrisoning system). CLAUDE.md's Current status section updated to match.

---

## 2026-08-28 — Chola building scale hierarchy corrected (Roadmap Section 4.3)

**Scope**: the user flagged, from a live Play-mode screenshot, that Chola building
scale wasn't proportional to human units or to each other (TownCenter alone looked
right). This was a real bug, not a perception issue.

**Root cause**: the original Chola session (this same day) calibrated each of the 9
buildings' scale *independently* against its old shared (non-civ) sibling — a
per-building check that passed in isolation but never cross-checked against a human
unit or the rest of the family together. Measured live via `BuildingModelFactory.
Spawn(...)` + combined `Renderer` bounds in Play mode: Barracks came out at world
height 1.74 and House at 1.73, both **shorter than the Chola Worker unit** (measured
1.94, matching its `CapsuleCollider.height` of 2) — a building shorter than the
humans who use it reads as broken, exactly what the screenshot showed.

**Fix**: recomputed all 8 non-TownCenter buildings' scale as a multiple of the
worker's height, keeping TownCenter unchanged (user-confirmed correct: height
11.16, footprint ~13.9×15.7). New heights: Tower 7.99 (was 6.01), Market 4.95 (was
4.89, negligible change), Barracks 4.38 (was 1.74), Dock 3.81 (was 2.74), Wall 2.66
(was 2.13), Gate 2.66 (was 2.12), House 2.57 (was 1.73), Farm 2.10 (was 1.88) — all
now clear the worker's 1.94 height, in a clean descending hierarchy
TownCenter > Tower > Market ≈ Barracks > Dock > Wall ≈ Gate ≈ House > Farm. Full
reference ratios and methodology saved to Claude's cross-session memory
(`feedback_building_scale_hierarchy`) per the user's explicit request, for reuse on
the 4 remaining civs' 36 models.

**A real mistake made and caught mid-fix**: the first attempt edited the wrong
transform — `PrefabUtility.LoadPrefabContents(path).transform.GetChild(0)` is the
`<Building>_model` child, which carries a constant ~100 import-normalization scale
from the raw Meshy export (identical across every building, not the tuned number);
the actual tuned per-building multiplier lives on the **prefab root's own
`localScale`**. Caught by diffing the edited prefab against git and noticing the
edit landed inside an unrelated nested-PrefabInstance override block instead of
changing the value grep had originally found. `git checkout --` to revert was
blocked by the permission classifier (a destructive command); reverted instead by
setting the wrongly-touched child back to its original value (100) via the same
`PrefabUtility` path, confirmed via `git diff` that the resulting change was
byte-for-byte equivalent to no-op before proceeding with the correct root-transform
edit.

**Live verification**: entered Play mode, spawned all 9 Chola buildings plus a real
Worker unit side-by-side via `BuildingModelFactory.Spawn`/`WorkerFactory.Spawn`
(temporarily disabling `FogOfWarManager` so the out-of-the-way test area rendered
lit), screenshotted via UnityMCP — confirmed visually: Worker now correctly dwarfed
by Barracks (previously inverted), and the full lineup reads as a coherent
descending hierarchy. All 45 EditMode tests still pass (scale-only change, no code
touched). Screenshots sent to the user directly rather than only described.

---

## 2026-08-28 — 5 cursor states wired (Roadmap Section 4.3)

**Scope**: closed the "build-placement cursor asset missing" item — the user
imported a new cursor icon pack (`Assets/Cursors/Cursors 64/` + `Cursors 256/`,
2 resolutions of the same 20 icons) hoping it filled the roadmap's known gap.

**Pack identification, not assumed**: `.meta` `AssetOrigin` metadata identified it as
the Unity Asset Store package **"Basic RPG Cursors"** — a generic fantasy-RPG icon
set (sword, axe+hammer, shield, potion, grasping hand, loot hand, crossbow, book,
gear, ship, plain arrows in bronze/gold recolors, some also in green/red), imported
as Sprite (not Cursor) type. Actually opened and visually inspected the candidate
images (not just filenames) before proposing a mapping: none of the 20 concepts are
purpose-labeled for the brief's 5 states, and every "action" icon is a plain arrow
pointer with a small icon badge attached (arrow-tip hotspot baked into the
composition), structurally different from the brief's spec'd standalone centered
icon for 4 of the 5 states. Reported this mismatch to the user with a table of
best-available substitutes before touching anything; user chose to proceed with the
approximations (see below) over holding off or partial use.

**Mapping used** (all cropped to content bbox from the 256px source, resized to the
spec'd 32×32, saved as RGBA PNG via Python/Pillow — not in-Editor
`Texture2D.GetPixels`, per the known Editor-crash gotcha from the Chola building
session):
- Default → `Arrow.png` (clean match)
- Attack-move → `Cursor_Attack.png` (single sword, not literal crossed-swords)
- Invalid → `Arrow_R.png` (red arrow — the pack has **no** circle-slash/prohibition
  icon at all, closest available)
- Gather → `Cursor_loot.png` (hand + coins, user's pick over the closed-fist
  `Cursor_Hand.png`)
- Build-placement → `Cursor_Production.png` (axe+hammer, not literal
  hammer-and-nail)

Confirmed via pixel inspection (`PIL` alpha-channel check) that all 5 sources
already have real alpha transparency — no border-flood-fill fix needed this time,
unlike the prior UI-art-delivery session.

**Also found and fixed while auditing existing cursor code** (not part of the pack
work, but directly adjacent): the 3 already-wired cursors (`default`/`gather`/
`attack_move`) were imported at **2048×2048**, wildly off the brief's 32×32 spec —
corrected in the same pass. `invalid.png` existed as a delivered asset from a prior
session but was **never referenced by any code** — there was no invalid-hover
detection logic anywhere in the codebase at all. Added real detection logic instead
of just wiring the texture: `HoverTooltip.cs` now checks whether the actual
selection can attack (`MeleeAttacker`/`BoatAttacker`, mirroring
`SelectionManager`'s own attack-dispatch check) before showing Attack-move, and
whether it can gather (`Gatherer`) before showing Gather — falling to Invalid
otherwise. This fixes a real latent bug: previously, hovering a hostile target
*always* showed the Attack-move cursor regardless of whether the selection could
actually attack (e.g. a pure economy selection), and hovering a resource node with a
non-gathering selection silently showed the default cursor with no signal at all.

**Testable design**: extracted the state decision into `HoverTooltip.
ResolveCursorState` — a pure `internal static` method (mirrors the
`CivilizationProfile.FindCategoryMultiplier`-style testable-helper pattern already
used elsewhere), covered by 6 new EditMode tests in the new
`HoverCursorStateTests.cs`. All 45 EditMode tests pass (39 previous + 6 new).

**Live verification (Play mode, via UnityMCP, not just tests)**: entered Play mode
on the `Main` scene, confirmed all 5 textures load at runtime (via reflection on the
live `HoverTooltip` instance — none null, all 32×32). The scene was sitting at
`MissionSelectMenu` with spawners inactive; activated `UnitSpawner`/
`ResourceNodeSpawner`/`TownCenterSpawner`/`AiController` directly to get real
Player/Enemy workers, resource nodes, and town centers into the scene (Play-mode-only
changes, discarded automatically on Stop — no persistent scene edit). Verified
against real objects and the real live methods (via reflection, not a
reimplementation): empty selection hovering a hostile worker or a resource node both
correctly resolve to Invalid (confirming the bug fix above); a real Player worker
selected (which carries both `Gatherer` and `MeleeAttacker`) correctly resolves to
AttackMove/Gather on the respective hovers; `BuildingPlacer.IsPlacing = true`
correctly resolves to BuildPlacement regardless of what's under the cursor.
**Caveat, disclosed rather than glossed over**: Unity's `Cursor.SetCursor` is a
write-only OS API with no readback, and available tooling can't capture the
OS-rendered hardware cursor bitmap in a screenshot — so "live-verified" here means
confirming the real decision logic and real texture assignment against actual scene
state and components, not a visual screenshot of the pointer itself. (The prior
session's cursor-wiring claims had the same limitation and didn't claim otherwise
either, on inspection.)

**Docs**: `docs/UI_ART_BRIEF.md`'s Tier 2 cursor checklist item and wiring note
updated with the approximation caveats; `docs/ROADMAP.md` Section 4.3's
"Build-placement cursor asset missing" item closed.

---

## 2026-08-28 — Chola civ-specific building models wired (Roadmap Section 4.3)

**Scope**: first content delivered against Section 4.3's civ-specific building spec.
User supplied 9 raw Meshy AI exports (FBX + separate albedo/metallic/normal/roughness
PNGs, 2048×2048) for all 9 spawnable building types, dropped at
`Assets/Buildings/Chola/Chola/` — not the path `BuildingModelFactory.Spawn` probes
(`Assets/Resources/Buildings/<CivId>/<resourceName>`). This session identified,
scale-corrected, PBR-material-wired, and live-verified all 9.

**Identification**: only 2 of 9 source folders were named (`chola barrack/`,
`chola towncenter/`); the other 7 sat in opaque UUID folders. 5 resolved confidently
from the Meshy-internal filename (`Ancient_Temple_Tower`→Tower,`Temple_Bazaar`→Market,
`Harbor_Temple_Miniature`→Dock, `Harvest_Temple`→Farm, `Temple_Gatehouse`→Gate). The
remaining 2 (`Golden_Temple_Courtyard`, `Golden_Temple_Gate`) were genuinely ambiguous
between House/Wall — resolved by importing each into the Editor and inspecting the
actual geometry (Gatehouse: symmetric two-wing structure with a central archway →
Gate; the other "Gate"-named one: a long continuous run with no opening → Wall;
Courtyard: a single modest tower form → House), not by guessing from filenames.

**Scale correction**: raw imports needed per-building multipliers (1.18×–3.16×,
except TownCenter at 93× — that one file was normalized at a very different internal
scale than its 8 siblings). Derived by live-instantiating both the new Chola model and
its existing shared-building sibling in the Editor and matching heights via
`Renderer` bounds (not asset-only `AssetDatabase.LoadAssetAtPath` bounds queries,
which proved unreliable off-scene). Post-fix, all 9 spawned within ~0.01 units of
their reference building's height.

**Real PBR wiring, not the prior shortcut**: confirmed the earlier Meshy-sourced
unique-unit session (2026-08-27) never actually wired metallic/normal/roughness maps
despite the roadmap's own PBR standard — it shipped flat-albedo-only materials. User
asked for real PBR this time. New `Assets/Editor/MeshyBuildingImporter.cs` builds a
proper URP Lit material per building (`_BaseMap`, `_BumpMap`, `_MetallicGlossMap`),
confirmed `_Color` genuinely exists on URP Lit (so `BuildingModelFactory.TintMaterials`'s
civ-tint fallback chain works, not a silent no-op).

**A real Unity Editor crash, root-caused and worked around**: the first attempt
packed Meshy's separate metallic+roughness PNGs into one URP-layout texture via
`Texture2D.GetPixels`/`SetPixels` inside a UnityMCP `execute_code` call — this crashed
the Unity Editor process outright (confirmed via `ps` showing no Unity process left,
and an Editor.log stack trace with a single frame repeated 250+ times). Root-caused
and fixed by moving that packing out of Unity entirely — `pack_metallic_smoothness.py`
(Pillow) does it as plain file I/O on disk; `MeshyBuildingImporter` now only ever
copies files, sets import settings, and assembles the prefab, with no heavy pixel
manipulation inside the Editor. No data was lost (only the first building's raw
source files had been copied before the crash); re-ran one building at a time after
the fix, all 9 succeeded cleanly this time.

**A second real bug, live-caught by the user**: `BuildingModelFactory`'s existing
`ImportRotationCorrections["Tower"] = Euler(0,0,-90)` (authored for the *other*,
shared Tower asset) is keyed by resource name only, not civ — applying it to Chola's
Tower (already differently-oriented) knocked it over. Worked around by baking a
counter-rotation into the wrapper prefab's inner model node (one level below where
the factory overwrites rotation), so the net result is correct regardless of the
shared correction. First fix attempt (a Z-axis counter-rotation) canceled the
shared correction mathematically but the model was still lying on its side —
turned out the raw mesh's native tall axis differs between the original ad hoc
import and the pipeline's fresh re-import of the same bytes (Unity's
Convert-Units/axis-conversion default apparently isn't guaranteed identical across
imports), so the needed correction was a Y-axis rotation, not Z. That got the
bounding box right (Y tallest) but the user caught that it was still upside-down —
AABB checks can't distinguish a correct orientation from its 180°-flipped twin.
Fixed by visually screenshotting each of the two AABB-equivalent candidates and
picking the one that actually reads as a tower (wide fortified base, tapering
tiers, ornamental cresting on top), landing on `Quaternion.Euler(0, -90, 0)` for
the inner model node. **Lesson for future civ-specific Tower/rotation-sensitive
imports**: bounds-only automated checks are not sufficient to verify orientation;
a screenshot check is required.

**Verification**: live-spawned all 9 through the real `BuildingModelFactory.Spawn`
path (not just loading the prefab) — confirmed collider auto-add, civ-color tinting,
and ground alignment all work end-to-end via UnityMCP screenshots. Added 2 EditMode
tests to `BuildingModelFactoryTests.cs` (`CholaBuildingModels_ExistAtTheExactResourcePath...`,
`Spawn_UsesCholaSpecificModel_AndStandsUpright` — the latter asserts a non-trivial Y
bound specifically to catch a future lying-on-its-side regression). All 39 EditMode
tests pass. Deleted the raw upload duplicates at `Assets/Buildings/Chola/Chola/` at
the user's request now that everything needed is copied into
`Assets/Resources/Buildings/Chola/_Source/`.

**Not done this session**: the other 4 civs (Vijayanagara, Rajput, Maurya, Maratha)
still have zero civ-specific building models — `MeshyBuildingImporter` is reusable
for them once sourced, but scale/rotation corrections are per-asset and will need
the same live-verification process, not blind reuse of Chola's numbers.

---

## 2026-08-28 — UI skin display wiring: theme asset, import settings, icons/HP-bar/crests/cursors (Roadmap Section 4.3)

**Note**: other Claude Code sessions touched this repo around the same time (Naval
balance, Crusader Knight rig verification - see entries below) — flagged per
CLAUDE.md's single-session-discipline gotcha. This session's changes are scoped to
`Assets/Scripts/UI/*.cs`, `Assets/Resources/UI/**` (import settings + 2 art fixes),
and did not touch combat/balance or model-rigging code.

**Scope**: continuation of the UI skin item — the two prior sessions got an audit +
color-only `UIStyleTheme` scaffold, then delivered and alpha-fixed Tiers 1-3 art
(commit `d8ad6ec`). Nothing displayed any of it yet. Asked to "start the next
unstarted step, stop after committing" — this session does the actual display wiring:
theme asset, Sprite/Cursor import settings + 9-slice borders, and code changes to
put the art on screen.

**Live-scene investigation before planning** (per CLAUDE.md: verify claims against
the actual repo, not just prior notes) — inspected the real `Main.unity` layout via
UnityMCP `find_gameobjects`/resource reads, not just the C#, and found 3 things the
earlier code-only audit had gotten wrong or hadn't caught:
- `ResourceHUD`'s root GameObject **already has its own background `Image`**
  (`(0,0,0,0.55)`) — missed because the earlier audit only read the script, which has
  no `Image` field, not the scene.
- `BuildMenu` buttons are **204×28px thin text rows**, not square icon buttons — the
  longest label (`"Build Barracks (100 Wood, 50 Stone)"`, TMP-measured preferred
  width 215.6px) already nearly overflows a 204px button, which shaped how icons
  could be added (see below).
- `ResourceHUD`/`SelectedUnitPanel`/`HoverTooltip` each have their **own dedicated
  background art** (`panel_resource_bar`/`panel_selected_unit`/`panel_tooltip`) from
  the art delivery — distinct images, not interchangeable with the one shared
  `UIStyleTheme.PanelFrameSprite` (`modal_frame`, meant for the 4 true modal popups).
  Routing all of them through the single shared field would've put the wrong art on
  3 of the 4 panels.

**Import settings + a real bug found in the art**: batch-set Texture Type (Sprite for
41 files, Cursor for the 4 cursor pngs — `maxTextureSize=64`, since the native art is
2048px and an unscaled `Cursor.SetCursor` call would render a screen-covering
cursor), plus 9-slice `spriteBorder` on the 12 frame/button assets (auto-detected via
a gradient-flatness scan per file, then hand-verified since painterly textures fooled
the naive version once — see script). Along the way, found `hp_bar_frame.png`/
`hp_bar_fill.png` still carried **huge transparent margins** from the previous
session's leftover noise-speckle artifacts (scattered opaque specks reaching the
canvas edges had prevented that session's bbox-crop from tightening) — both were
functionally unusable for 9-slicing until fixed. Wrote a largest-connected-component
filter (`tighten_bbox.py`, same family as the earlier `alpha_key.py`) to drop the
speckles and re-crop: `hp_bar_frame.png` 2752x1536 → 2620x276, `hp_bar_fill.png`
3165x1344 → 2994x249. Applied the same filter to `resource_wood.png`'s minor stray
speckle while at it (no size change, just cleanup).

**What changed (code)**:
- New `Assets/Resources/UI/UIStyleTheme.asset` — `PanelFrameSprite=modal_frame`,
  `ButtonBackgroundSprite=menu_button_normal`. `UIStyleTheme.cs` gained
  `ButtonHoverSprite`/`ButtonPressedSprite` fields and `ApplyButton` now wires real
  `Button.spriteState` (`SpriteSwap` transition) when a `Button` + both sprites are
  present, instead of one static sprite — `SettingsMenu`/`DiplomacyMenu`/
  `MissionSelectMenu`'s generic buttons now actually respond to hover/press.
- `BuildMenu.cs`: `ApplyTheme()` rewritten to wire the dedicated `CommandCardButton`
  4-state set (`SpriteSwap`, not the shared theme) onto all ~31 command-card buttons,
  plus a new `AddCommandIcon` helper adding a left-edge 20x20 icon + label inset to
  the ~24 buttons with a matching asset (mapping in the plan file/code comments; no
  icon for `ungarrisonButton`/`fishingBoatButton`/`warGalleyButton`/`uniqueTechButton`/
  the 3 economy-tech buttons — no asset exists for them).
- `ResourceHUD.cs`: new `Awake()` applies `panel_resource_bar` to the existing
  background `Image`, and a new `AddResourceIcon` helper adds icons to
  wood/food/gold/stone labels (civ/population/age have no matching asset).
- `SelectedUnitPanel.cs`: `panel_selected_unit` applied to `panelRoot`'s `Image`
  (replacing the generic `UIStyleTheme.ApplyPanel` call). New `SetUpHealthBar`/
  `SetHealthBar` add a real HP bar (`hp_bar_frame`+`hp_bar_fill`, `Image.Type.Filled`,
  fill driven by `Health/MaxHealth`) as siblings of `hpLabel` (not children - a
  Graphic's children always draw after its own graphic in uGUI depth order, which
  would've put the bar on top of the text instead of behind it) alongside the
  existing HP text.
- `HoverTooltip.cs`: same dedicated-sprite treatment for `panel_tooltip`. New cursor
  wiring in `Update()` — `gather` when hovering a `ResourceNode` with a
  `Gatherer`-capable unit selected, `attack_move` when hovering a hostile
  `FactionMember`, `default` otherwise. `BuildingPlacer.IsPlacing` is checked and
  deliberately left on the default cursor (no build-placement asset exists).
- `CivPicker.cs`: new `AddCrest` shifts each card's `Name`/`Blurb` down 64px and adds
  a 64x64 crest `Image` in the freed top strip, sprite resolved from
  `civId.ToString().ToLowerInvariant()`.

**Tests**: no new tests (this is UI display wiring, not a new testable system — the
existing `UIStyleThemeTests` already cover `UIStyleTheme.Current`). All 37 EditMode
tests still pass after every change.

**Manual verification**: Play Mode via UnityMCP, with real screenshots (not just
state assertions) at each step: CivPicker showing all 5 crests cleanly above
Name/Blurb with no overlap; BuildMenu command-card showing icons on Barracks/Farm/
House/Wall/Gate/Tower/Dock/Market placement buttons and Train Worker/Advance Age,
with the longest labels (`"Build Barracks (Requires Classical Age)"`,
`"Build Market (100 Wood, 50 Gold)"`) gracefully word-wrapping to 2 lines instead of
clipping, confirming the risk flagged in planning was real but non-breaking;
SelectedUnitPanel showing a green HP bar at full health for both a selected
TownCenter (500/500) and a selected worker (20/20); SettingsMenu showing the
`modal_frame` background and reskinned buttons. Zero new console errors across
compile, the test run, and the Play Mode session (a few "PlayerLoop called
recursively"/"Can't remove TownCenter... RallyPoint depends on it" messages appeared
from the MCP tooling's own reflection-based test harness calls — verified these were
harmless: `TownCenter` component count stayed at 2 throughout, i.e. Unity's
dependency guard blocked the attempted removal rather than it succeeding; not related
to any code shipped this session).

**Known, accepted gaps (unchanged from the prior audit, not fixable by wiring code)**:
the missing 5th cursor (build-placement) and the Maurya crest's off-palette blue.
9-slice border/multiplier values are a first pass (visually spot-checked, not
pixel-perfect) — refining them is cosmetic polish for a future pass, not a
correctness gap.

**Roadmap**: Section 4.3's UI skin bullet updated with the wiring completion.

---

## 2026-08-28 — Crusader Knight rig-compatibility verification (Roadmap Section 1)

**Note**: two other Claude Code sessions touched this repo around the same time (UI
art delivery, Naval balance pass — both logged separately). This session's own
changes are docs-only (`docs/ROADMAP.md`, this file, `CLAUDE.md`'s status) — no
gameplay code or asset files were touched; the verification itself was done entirely
in transient Editor-runtime objects, cleaned up before this session ended.

**Scope**: Roadmap Section 1's "2 Crusader Knight body models sourced but not wired
in — rig-compatibility with `WeaponAttachment`/`AnimationDriver` was never verified."
Explicitly a verification-only task per the roadmap's own framing (a prerequisite
decision point before any actual swap-in), not the swap itself.

**What the 2 models actually are**: `Assets/importedmodels/Item47/TemplarKnight/
scene.gltf` and `.../HospitalierKnight/scene.gltf` — both already imported (real
`.meta` files exist) but unwired into any factory. Parsed directly from each file's
glTF JSON before touching Unity: both are already-skinned meshes with an embedded
Mixamo skeleton (66 joints for Templar including full 5-finger hand chains, 34 for
Hospitalier with only index fingers) — **not the same rig as each other**, and
neither matches the shared dummy's Blender/Rigify skeleton. Zero embedded animation
clips in either file. Both import via `com.unity.cloud.gltfast`'s `ScriptedImporter`,
not Unity's native FBX `ModelImporter` — meaning neither has a Humanoid Avatar or
Animator component as imported (confirmed live: `GetComponentInChildren<Animator>()`
returned null on both fresh instances).

**Method — live inspection**: instantiated each glTF prefab via
`AssetDatabase.LoadAssetAtPath<GameObject>` + `Instantiate` and walked the full
transform hierarchy live to get the *actual* bone names per model (not assumed from
generic Mixamo convention — bone names carry a per-file numeric suffix, e.g.
`mixamorig:Hips_01` for Templar vs `mixamorig:Hips_00` for Hospitalier). Confirmed no
pre-existing `Animator`, and that each model's sword/shield/staff meshes
(`MeshFilter`s, not `SkinnedMeshRenderer`s) sit as static children directly under the
scene root rather than under any hand bone.

**Method — building a Humanoid Avatar without Blender**: since glTFast's import
path has no equivalent to `ModelImporter.humanDescription`/`CreateFromThisModel` (the
mechanism the prior unique-units rigging session relied on), used
`UnityEngine.AvatarBuilder.BuildHumanAvatar(GameObject, HumanDescription)` instead —
a Unity API that builds a valid Humanoid Avatar directly from an existing skeleton
hierarchy plus a hand-specified bone mapping, with no FBX-import step required. Hand-
authored a `HumanDescription` per model: a `human` array mapping each of the ~28-52
real bone names found live (matched by name-prefix search, e.g. `mixamorig:LeftArm*`
→ `LeftUpperArm`, using `HumanTrait.BoneName`'s exact canonical strings) to
`HumanBodyBones`, and a `skeleton` array covering every transform in the hierarchy
with its actual local position/rotation/scale. This is standard Editor scripting —
the normal "wire an already-provided asset in" work, not the from-scratch mesh-
binding/Blender pipeline the unique-units session needed an explicit override for.

**Result — both models produced a valid Humanoid Avatar on the first attempt**:
`avatar.isValid && avatar.isHuman == true` for both (52/52 mapped bones for Templar
including full fingers, 28/28 for Hospitalier's reduced finger set — zero missing).

**Live animation-retargeting test (the actual "AnimationDriver-compatible" question)**:
attached an `Animator` with the built Avatar, then drove the shared dummy's own
`HumanM@Walk01_Forward` clip through the *exact* Playables pipeline `AnimationDriver`
uses (`PlayableGraph` + `AnimationPlayableOutput.Create` + `AnimationClipPlayable.Create`
+ `PlayableOutputExtensions.SetSourcePlayable`). First check (a single `Evaluate` call,
screenshot-compared before/after) was inconclusive — the Scene View didn't visibly
update until `SceneView.RepaintAll()` was called (same class of Editor-repaint gotcha
already documented for Play mode ticking). Real confirmation came from directly
sampling a leg bone's local rotation across 4-8 points through the clip's time range:
both models showed a smooth, continuous, monotonic ~40° swing on `LeftUpperArm`'s
opposite number `LeftUpperLeg` (Templar: -46.5° → -7.3° across t=0.05-0.40s;
Hospitalier: -37.7° → +2.4° across t=0.05-0.35s) — a real walking stride, not a
frozen/T-pose/exploded result. `WeaponAttachment.AttachToBone(instance,
HumanBodyBones.RightHand, "Weapons/Sword/scene", ...)` — the exact call convention
every land-unit factory already uses — was also run end-to-end against the built
Avatar and returned a real attached prop, not null, on the Templar model.

**2 real caveats found, deliberately not fixed this session (verification only)**:
1. Both models report `Animator.humanScale` ≈ 247-248 (Templar) / 247 (Hospitalier)
   — Unity's own measure of how large this skeleton is relative to a normal reference
   human. Traced to the `Hips` bone itself carrying a baked-in `localScale` of
   `(2.54, 2.54, 2.54)` plus a large positional offset in its bind pose — 2.54 is
   exactly the cm-per-inch conversion factor, strongly suggesting a unit-convention
   mismatch baked into the source rig (the same general class of "packs come in
   whatever real-world scale the artist modeled at" issue `WeaponAttachment`'s own doc
   comment already anticipates, just a much larger factor than the 4-27x precedent
   already on record for Wall/Gate). Retargeting itself is scale-invariant (confirmed
   above — rotation angles swing correctly regardless), but a real swap-in would need
   this normalized (measure-and-scale a wrapper, the same pattern `WeaponAttachment`/
   `HumanModelFactory.AlignFeetToGround` already use) before the model is usable at
   the shared dummy's actual in-scene size.
2. Confirmed via the earlier hierarchy walk: each model's sword/shield/staff/scabbard
   meshes are parented directly under the scene root, not under any hand bone. They
   will not follow the animated hand once wired in — a real swap-in needs to either
   re-parent them to the correct hand joint (in Blender) or strip them and rely on
   the existing `WeaponAttachment` prop system instead, mirroring the precedent
   already set for the 3 humanoid unique units (their sculpted weapons were kept,
   cosmetic `WeaponAttachment` calls removed).

**Also noted, not investigated further**: the raw imported scene includes some
static prop geometry whose original purpose/placement wasn't audited (visible in a
scene-view screenshot as an oddly large diagonal shape and a separate ground-level
mesh alongside the standing figure) — likely inert display/scabbard geometry from
the original Sketchfab scene rather than anything wrong with the character itself,
but worth a first-look pass by whoever does the actual swap-in.

**Console**: only expected `Destroy may not be called from edit mode!` warnings from
cleanup calls that used `Destroy` instead of `DestroyImmediate` in Editor context —
harmless (Unity treats it as an immediate destroy either way in-editor, just warns);
confirmed the scene had zero leftover test objects after cleanup.

**No code changes landed** — pure Editor-runtime verification (Avatar/Animator/
PlayableGraph objects that were never persisted), consistent with the plan's explicit
scope (verification only, not the swap-in). This session's changes are
`docs/ROADMAP.md`, this file, and `CLAUDE.md`'s status section.

---

## 2026-08-28 — UI art delivered: alpha-fix + rename pass (Roadmap Section 4.3 "UI skin")

**Note**: another Claude Code session logged a Naval balance pass to this same repo
around this session (see entry immediately below) — flagged per CLAUDE.md's
single-session-discipline gotcha. This session's changes are scoped entirely to
`Assets/Resources/UI/` (art files only) and didn't touch combat/balance code.

**Scope**: The user delivered Tier 1-3 UI art (per the asset spec proposed in the
2026-08-27 UI-skin-audit entry below) into `Assets/Resources/UI/`, generated via
Canva/Gemini in jpg/png. Asked to locate it, confirm what was actually delivered
against the spec, and report what's needed to wire it in. Scoped down to just the
alpha-fix + rename pass this session; theme-asset creation and the actual icon/HP-bar/
crest/cursor wiring code were deferred to a planned session (user's explicit choice).

**Audit against the spec**: read/viewed every delivered file. Essentially complete:
4 of 5 cursors (missing build-placement), all 19 action icons, all 4 resource icons,
both button-background sets (command-card + menu), both HP-bar pieces, all 3 panel
frames, all 3 menu-button states, all 5 civ crests. Two content gaps flagged (not
fixable by this pass, still open): the missing 5th cursor, and the Maurya crest
rendering in Rajput's blue instead of the spec'd warm gray/stone.

**Critical technical finding**: checked pixel data, not just preview thumbnails —
every single delivered file had a **fully opaque baked-in background** instead of
real alpha, confirmed via direct alpha-channel sampling (`frac_alpha==255` at 100%
on every file checked). Two failure modes by source: the Gemini-generated PNGs had a
*painted-on fake checkerboard* imitating a transparency preview (not real alpha —
verified by sampling corner pixels: alpha=255, RGB matching the checker's light-gray
tone); the Canva-generated JPGs had a flat cream/parchment background baked in (JPEG
can't carry alpha at all). Left as-is, every icon/cursor/crest would have rendered as
a solid opaque square in-game instead of blending onto the themed UI.

**Fix**: wrote a border-flood-fill alpha-key script (`alpha_key.py` — corner-block
color sampling, per-pixel color-distance matching, binary dilation before connected-
component labeling to bridge anti-aliasing/JPEG-noise gaps, then intersected back
against the true match mask so dilation doesn't eat real edges). Iterated twice on
methodology after live failures: v1 (sampling the full border ring for reference
colors) corrupted 2 files where real artwork touched the canvas edge, poisoning the
reference palette; v2 (corner-only sampling) fixed that. Verified every output by
compositing onto solid magenta (not trusting the tool's own transparency-preview
rendering, which turned out to render some fully-transparent PNGs against white
regardless) — caught that `panel_selected_unit.png` (`Panels/Gemini..39v2pk..png`)
has **no real background at all** (fully painted edge-to-edge); running the flood-
fill on it destroyed ~93% of the real frame art before this was caught, so it's
copied through unmodified instead of alpha-fixed.

**What changed**: ~40 files alpha-fixed and renamed from auto-generated prompt-text
filenames (e.g. `Generate _Flat 2D hand-painted game UI icon for a historical...
representing BUILD DOCK_ one .jpg`) to stable short names (`build_dock.png`, etc.)
across `Cursors/`, `Icons/` (+ `Icons/CommandCardButton/`), `Panels/`, and a renamed
`Menu/` folder (was `tier 3/`). One file, `resource_stone.png`, is only ~85% cleaned
after several tuning attempts (isolated background patches resisted the flood-fill
even at high tolerance/dilation) — used the best available version and flagged it as
needing a manual touch-up or regeneration rather than continuing to tune. One other
file (`resource_wood.png`) has a few cosmetically-negligible stray unremoved pixels.
Raw pre-fix originals preserved at `UI_RawOriginals_backup/` (repo root, deliberately
outside `Assets/` so Unity's AssetDatabase never imports the backup as real project
content).

**Verification**: spot-checked ~15 of the ~40 processed files individually (composited
onto magenta to reveal true transparency, not just preview) plus a full contact-sheet
pass over the remaining ones — all clean except the two noted above. Refreshed Unity
via UnityMCP after the restructure: zero console errors/warnings, every file has a
valid `.meta`.

**Not done this session (explicitly deferred)**: creating the actual
`UIStyleTheme.asset` and assigning the new sprites to it, Sprite import-type/9-slice
Border setup, and the new code (`BuildMenu` icon slots, `ResourceHUD`/`SelectedUnitPanel`
HP-bar, `CivPicker` crest layer, `Cursor.SetCursor` wiring) needed to actually display
any of this in a running match. That's Section 4.3's "UI skin" item's remaining scope
for a future planned session.

**Roadmap**: Section 4.3's UI skin bullet updated with this delivery's status.

---

## 2026-08-28 — Naval balance pass (Roadmap Section 1)

**Scope**: Close the roadmap's "Naval balance is one evidenced fix, not a full pass"
item. `CombatBonus.cs` has exactly one `UnitClass.Naval` entry (Naval→Archer = 0.5x);
every other Naval pairing, either direction, falls through to the default flat 1x.
War Galley is the only combat-capable Naval unit (Fishing Boat has no `BoatAttacker`,
non-combat by design).

**Pre-test audit**: `Assets/Design/playtest_log.csv` already had 2 live Naval fights
from the prior balance session (War Galley beat Soldier at 44% HP and Spearman at 33%
HP, both judged "real wins, not free ones," no fix needed). That left 4 genuinely
unaudited or unlogged pairings: War Galley vs Archer (the *tuned* pairing itself had
never actually been live-tested — the 0.5x fix predates the CSV, referenced only in
code comments), vs Cavalry, vs Siege, and a War Galley mirror match. Comparing
`BoatAttacker.cs` to the land equivalent `MeleeAttacker.cs` found the damage pipeline
is structurally identical (`damage * multiplier + bonus`, then `CombatBonus.Multiplier`,
then `TakeDamage`) with one real difference: `BoatAttacker`'s `attackInterval` defaults
to 1.5s vs `MeleeAttacker`'s 1.0s (no setter exists for either — never overridden
per-unit anywhere), making Naval attack 33% less often than any land unit in every
matchup, independent of `CombatBonus`. Also found `WarGalleyFactory.cs`/
`FishingBoatFactory.cs` never call `UpgradeProgress.ClassArmorBonus`/
`ClassDamageBonus` the way every land factory does — Naval units can't benefit from
per-class upgrade tiers at all. Both are real, adjacent findings, deliberately **not**
fixed this session (flagged for a future one, per CLAUDE.md's scope-flagging rule)
since this session's scope is auditing matchups and adding `CombatBonus` entries only
where live evidence shows a genuine problem, not restructuring the naval combat
pipeline itself.

**Method**: Identical to the prior balance-pass session — entered Play mode via
UnityMCP, confirmed `Time.time`/`Time.frameCount` genuinely advancing
(`Application.runInBackground` + `QueuePlayerLoopUpdate()`), then for each fight
`execute_code`-spawned both units directly via their factories at full HP, placed
within immediate attack range (no pathing/chase), called `AttackMove` on both
attacker components, and let real ticks run until one side's `IsDead` flipped —
using `Time.time` as the authoritative clock, not wall-clock estimates.

**Results** (all 4 logged to `Assets/Design/playtest_log.csv`):
1. **War Galley vs Archer**: Galley won at 21/45 HP (47%), Archer died. First-ever
   live confirmation of the existing 0.5x fix — it softens the kill speed without
   flipping the outcome, exactly its original intent.
2. **War Galley vs Cavalry**: Galley won at 9/45 HP (20%) — the closest naval win
   logged so far, but still a real win with real damage taken both ways, not a
   near-instant kill either direction. No fix warranted, same bar as the existing
   Soldier/Spearman rows.
3. **War Galley vs Siege**: decisive Siege win — Galley destroyed after landing only
   2 hits (16/50 HP dealt, 32%), Siege took no further damage. Hand-calc matched
   exactly (Siege's 15dmg/1.0s interval kills Galley's unarmored 45 HP in 3 hits/~2s;
   Galley's 8dmg/1.5s interval only lands 2 hits on Siege in that window). **Judged
   working-as-designed, not a bug**: Siege has no unit-vs-unit penalty anywhere in
   `CombatBonus` (its whole identity is high melee burst against any non-Building
   target), and Siege's real range (3) and move speed (1.8) mean a Galley should
   never let itself get meleed in the first place — the actual counterplay is Naval's
   range (4) and speed (3.0) edge over a unit that can't enter water at all, which a
   static forced-melee test structurally can't capture. Flagged for reconsideration
   only if real (non-synthetic) gameplay surfaces this as an actual problem.
4. **War Galley vs War Galley (mirror)**: symmetric stats as expected — both sides
   traded identical hits down to 13/45 HP simultaneously, then the winner was decided
   purely by which attacker's `Update()` ran first in frame order (5/45 HP, 11%
   remaining). No asymmetry bug found, same pattern as the existing Spearman mirror
   test.

**No `CombatBonus` changes made** — every result either confirmed the existing fix or
met the "real win/loss, not a bug" bar already established by the prior session's
Soldier/Spearman/stacking-audit conclusions.

**Small doc fix**: `UnitClass.cs`'s `Naval` enum comment and `WarGalleyFactory.cs`'s
header comment both claimed "Deliberately no CombatBonus entries yet" — stale since
Naval→Archer was added; updated both to reflect current reality.

**Console**: Same `NavMeshAgent`/`SetDestination` warnings as the prior balance
session (land units spawned via `execute_code` outside a baked match have no NavMesh)
— a documented test-harness artifact, not a game bug; none of the fights' units ever
needed to move (already in range at spawn).

**Roadmap/CLAUDE.md**: Section 1's Naval balance item marked done for this session's
scope (the two adjacent findings explicitly left open, not silently closed).
`playtest_log.csv` now has 20 total rows (16 + 4 new).

**Code changes**: only the 2 stale doc comments — no multiplier/logic changes, so no
new/updated tests were needed. This session's changes are `Assets/Design/
playtest_log.csv`, `Assets/Scripts/Combat/UnitClass.cs`,
`Assets/Scripts/Units/WarGalleyFactory.cs`, `docs/ROADMAP.md`, `docs/SESSION_LOG.md`,
and `CLAUDE.md`'s status section.

---

## 2026-08-28 — WaterMover obstacle avoidance (Roadmap Section 1)

**Scope**: Close the roadmap's "WaterMover has no obstacle avoidance" item. The
roadmap framed this as a future-proofing concern ("will break the moment any map gets
a non-trivial coastline"), but investigation before implementing found the actual bug
was narrower and already reachable today, not hypothetical.

**Root cause**: `WaterMover.MoveTo(Vector3)` (`Assets/Scripts/Units/WaterMover.cs`)
stored whatever destination it was given with zero bounds checking, and nothing else
in the call chain clamped it either — confirmed by reading every call site:
`SelectionManager.cs` (player right-click move order + formation offset),
`RallyPoint.cs` (rally point), `BoatAttacker.AttackMove`, `BoatGatherer`'s
gather/return-to-dock state machine. Since the water plane has no collider
(`ProceduralGround.BuildWaterPlane`) and there's no water NavMesh, nothing stopped a
boat sailing straight onto land — reproducible right now on the existing Coastal map
by right-clicking past the shoreline, not just a hypothetical multi-shape-coastline
problem for some future map.

**Fix**: The current water region (`MapDefinitionData.WaterCenter`/`WaterHalfExtents`)
is a single axis-aligned rectangle — convex. That means clamping any destination point
into it before storing is sufficient to guarantee a boat's straight-line path never
crosses onto land, for every water shape the data model actually supports today.
Added `WaterProximity.ClampToWater(Vector3)` (`Assets/Scripts/Core/WaterProximity.cs`),
reusing the exact clamp math that was already written inline inside
`DirectionToNearestWater` (refactored to call the new method instead of duplicating
it). `WaterMover.MoveTo` now clamps through it before storing `_destination`, and a
new `Destination` read-only getter exposes the stored value for testing (mirrors the
existing `HasArrived` pattern). Deliberately did **not** build real polygon/NavMesh
pathfinding for a non-convex coastline — no map defines one in `MapDefinitionData`
today, and CLAUDE.md is explicit about not designing for hypothetical future
requirements; the class doc comment on `WaterMover` was updated to say so honestly
(still no avoidance between boats, still would need real pathfinding if a non-convex
coastline is ever added).

**Tests**: New `Assets/Tests/EditMode/WaterMovementTests.cs` (5 tests), following
`CivPassiveBonusTests.cs`'s established convention (`_spawned` GameObject list +
`[TearDown]`, explicit restore of `MapRegistry.Select(MapId.RiverValley)` since it's
shared static state across the EditMode run): `ClampToWater` on a point already inside
water, past each of the 4 edges individually, past both axes at once (a corner), and
on a no-water map (passthrough unchanged); `WaterMover.MoveTo` with a destination far
inland ends up inside `WaterProximity.IsInsideWater`. All 37 EditMode tests pass (32
pre-existing + 5 new).

**Manual verification**: Play Mode via UnityMCP. Entered Play mode, selected the
Coastal map (`MapRegistry.Select`), spawned a `WaterMover`-driven test GameObject just
inside the western shoreline, and issued `MoveTo` toward a point 80 units past the
shore on dry land. Confirmed via `execute_code`: the stored `Destination` was clamped
to the shoreline edge and reported inside water immediately; after several real ticked
frames (`Time.time` advanced ~4.5s), the boat's actual `transform.position` had moved
to and stopped exactly at the clamped shoreline point — never toward the on-land
target, never left `WaterProximity.IsInsideWater`. Zero console errors/warnings
through compile, the test run, and the Play mode session.

**Roadmap**: Section 1's WaterMover item checked off with the fix summary. CLAUDE.md's
Current status updated.

---

## 2026-08-27 — UI skin audit + style-theme scaffold (Roadmap Section 4.3 "UI skin")

**Note on the request's item numbering**: the session was framed as "item 6 (per-civ
architectural differentiation)" and "moving to item 7: UI skin," but that doesn't
match Section 5's actual numbering — item 7 there is per-civ architectural groundwork
and item 8 is unique-unit visual closure, both already done (verified live this
session, see below). UI skin isn't its own numbered Section 5 item; it's one of five
things bundled into item 6's "everything else" and its own standalone bullet in
Section 4.3. Flagged to the user directly before proceeding rather than guessing;
confirmed the actual next unstarted work is UI skin either way.

**Scope**: Audit of the current UI implementation (Section 4.3's "UI skin" item),
plus — per the user's explicit follow-up choice — a technical scaffold ahead of any
art arriving.

**Verification of prior claims (per CLAUDE.md's single-session-discipline gotcha)**:
confirmed live in the repo, not just trusted from CLAUDE.md's status text, that
`BuildingModelFactory.Spawn` (`Assets/Scripts/Buildings/BuildingModelFactory.cs:84`)
really does probe `Buildings/{civId}/{resourceName}` before falling back — item 7's
groundwork is genuinely done.

**Audit findings**: read every UI script (`ResourceHUD`, `SelectedUnitPanel`,
`BuildMenu`, `HoverTooltip`, `MinimapController`, `CivPicker`, `ObjectivePanel`,
`MissionSelectMenu`, `SettingsMenu`, `DiplomacyMenu`). Confirmed: zero UI art
anywhere in the project (no icon/sprite files outside `Assets/Screenshots`), no
cursor-state code at all (`Cursor.SetCursor` never called — default OS arrow
throughout), and no separate tech/age tree viewer (upgrades are inline BuildMenu
text buttons). Concrete bug found: the 4 runtime-code-generated menus
(`SettingsMenu`/`DiplomacyMenu`/`MissionSelectMenu`/`ObjectivePanel`) had already
drifted into 2 different ad hoc dark palettes with no shared source — panel bg
`(0.05,0.05,0.08)` vs `(0.12,0.12,0.14)`, button `(0.22,0.2,0.16)` vs
`(0.25,0.25,0.3)` — before any real art had even landed.

**Proposed to the user, not implemented (per Section 4's asset-sourcing rule)**: a
prioritized, spec'd asset list (~35-40 assets: command-card button background +
~18 action icons + 4 resource icons as Tier 1, selected-unit/tooltip panels + 5
cursor states as Tier 2, shared modal panel/button + civ-select cards as Tier 3,
minimap frame/portraits as Tier 4), plus a 9-slice + shared-style-token technical
approach — the user will arrange/commission the art separately, same as items 3/6.

**What changed (code, user-confirmed scope expansion to scaffold ahead of art)**:
- New `Assets/Scripts/UI/UIStyleTheme.cs`: a `ScriptableObject` style-token source
  (`PanelBackground`/`PanelBackdrop`/`ButtonNormal`/`TextPrimary`/`TextSecondary`/
  `TextSuccess`, plus nullable `PanelFrameSprite`/`ButtonBackgroundSprite` for later).
  `UIStyleTheme.Current` lazy-loads `Resources.Load<UIStyleTheme>("UI/UIStyleTheme")`
  and falls back to hardcoded defaults if no asset exists yet — same caching pattern
  as `DataRegistry`, same "hardcoded until a real reason to change" precedent as
  `AgeProfile`/`UpgradeProgress`. `ApplyPanel`/`ApplyButton` set color today and will
  pick up a real 9-slice sprite automatically the moment one is assigned on a theme
  asset, with zero further code changes.
- Wired to the theme: `SettingsMenu.cs`, `DiplomacyMenu.cs`, `MissionSelectMenu.cs`,
  `ObjectivePanel.cs` (their ad hoc panel/button/text colors replaced), `BuildMenu.cs`
  (new `ApplyTheme()` in `Awake()` sets all ~31 command-card buttons' `Image.color`,
  previously left on Unity's default gray), `SelectedUnitPanel.cs`/`HoverTooltip.cs`
  (theme applied to `panelRoot`'s `Image` if one exists, via defensive
  `TryGetComponent` — no scene edit needed).
- **Deliberately left out of scope**: `ResourceHUD.cs` has no panel background in
  code or scene at all (bare labels only) — nothing to theme without a scene edit,
  which this pass avoided entirely (pure code scaffold, zero scene changes).

**Tests**: `Assets/Tests/EditMode/UIStyleThemeTests.cs` — `Current` non-null and
stable/cached, default colors sane (opaque except the deliberately-translucent
`PanelBackdrop`/near-opaque `PanelBackground`), `ApplyPanel`/`ApplyButton` null-safe.
All 32 EditMode tests (28 pre-existing + 4 new) pass.

**Manual verification**: Play Mode via UnityMCP `execute_code` — confirmed
`SettingsMenu`/`DiplomacyMenu` boxes now read the identical `(0.05, 0.05, 0.08, 0.97)`
(previously 2 different colors) and a `BuildMenu` button (`barracksButton`) now reads
the shared `(0.25, 0.25, 0.3, 1)` instead of Unity's default gray. Zero new
console errors/warnings from compile or Play mode entry.

**Roadmap**: Section 4.3's UI skin bullet updated with the audit + scaffold status
(art still not started, as intended). CLAUDE.md's Current status updated.

---

## 2026-08-27 — Visual closure for the 4 unique units (Roadmap Section 5, item 8 / Section 1's matching item)

**Note**: a concurrent Claude Code session worked this same repo during this
session (item 7, per-civ architectural differentiation groundwork - logged
separately, immediately below). Flagged per CLAUDE.md's single-session-discipline
gotcha; this session's own changes are scoped entirely to the 4 unique-unit models
and didn't touch `BuildingModelFactory`/civ-building work.

**Scope**: Section 5 item 3 (the 4 Maurya/Maratha unique units) was backend-complete
from an earlier session but explicitly not fully closed - no models existed, all 4
spawned on the shared Human Dummy body. The user had 4 raw Meshy AI FBX exports ready
(Pillar Edict Scholar, Mavla Raider, Durg Garrison, War Elephant) and asked to drive
the entire rigging pipeline via Blender's `--background --python` scripting, not
manual Editor steps - an explicit, deliberate override of this project's standing
"asset sourcing/creation is not Claude Code's job" rule for this specific task, given
directly in chat.

**Humanoid pipeline (Scholar/Raider/Garrison)**: each raw Meshy mesh (~1M vertices)
was decimated to ~7,000 tris (Roadmap 4.1's unit budget), scaled to match the shared
Human Character Dummy rig's height, and bound via Blender's `ARMATURE_AUTO`
(automatic weights). All 3 got all 51 vertex groups populated and deformed cleanly
under the rig's real Idle/Walk/Attack clips in a Blender-side render test (with the
caveat that Blender's raw bone-fcurve playback is an approximation of Unity's actual
Mecanim humanoid retargeting, not a guarantee - confirmed identical in Unity below).
User confirmed go on renders before export.

**War Elephant pipeline - two attempts**: first attempt extended a copy of the wild
boar's 19-bone generic quadruped armature with 3 new trunk bones, per the original
plan discussed with the user. Bind and a hand-posed walk-cycle sanity test both
worked, but literally reusing the boar's actual Unity `.anim` clip data failed with
mangled/exploded poses - a real Unity-to-Blender bone-local-rotation-axis convention
mismatch (Blender's FBX import discards the joint orientation Unity/Maya preserve),
not a rig or gait defect. Rather than solve that axis conversion, the user redirected
mid-session: use real elephant animation from two Sketchfab CC-BY models (Asian
elephant, African bush elephant) instead of the boar-based approach entirely. After
inspecting both (different, incompatible skeletons - 51 vs 106 bones, no shared rig
despite the same author), and starting a cross-rig retarget of the African model's
Death animation onto the Asian model's skeleton via bone-constraint baking, the user
simplified further: use only the Asian elephant, which already had all 4 needed clips
(`Idle1`, `walk`, `Attack1`, `Die`) natively - dropping the African model and the
retarget work entirely. Final approach: discard the boar-skeleton work, bind the
Meshy elephant mesh directly onto the Asian elephant's own 51-bone rig.

**Real bugs found and fixed in the final elephant pipeline** (all confirmed via
direct Blender-side investigation, not assumed):
1. Blender's FBX importer auto-assigns whatever action happened to be first in a
   multi-take file (`Attack1`) to the armature's `animation_data.action` on import -
   binding against this un-cleared, already-posed skeleton produced a garbage
   automatic-weight solve. Fixed by explicitly clearing the pose to rest before
   binding.
2. Every action in this file (an old 3ds Max Biped export) carries two Biped-export
   artifacts, neither of them real animation: an **object-level** `scale` channel
   pinned to a constant `0.01` (shrinks the whole mesh to a dot the instant any
   action is assigned - this was the "tiny dot" render bug), and a large spurious
   `location` value baked onto every bone (Biped bones deform via rotation only).
   Fixed by stripping everything except bone rotation curves - consistent with how
   this project already drives unit movement via `NavMeshAgent`, not baked root
   motion (`AnimationDriver`'s own `applyRootMotion=false` convention).
3. With those fixed, Idle/Walk/Attack deformed cleanly; the Die clip looked broken
   only in its more extreme late-clip poses (confirmed clean earlier in the same
   clip) - a genuine automatic-weight-under-large-rotation limitation, not another
   export bug. Flagged to the user as a known first-pass limitation, not silently
   accepted as fine.

**Wiring into Unity**: copied all 4 final FBX + albedo textures to
`Assets/Resources/UniqueUnits/<Name>/`. Configuring the 3 humanoid FBX imports as
Humanoid rig type hit a real, precisely-diagnosed chain of Unity avatar-validator
errors, each fixed in turn by reading the exact `Rig Error:` console message rather
than guessing: (a) "Copy From Other Avatar" failed on a transform-hierarchy mismatch
(Blender's export collapsed the original rig's `Rig -> B-root -> B-hips` wrapper
chain down to a bare `B-hips`) - added both empty wrapper objects back in Blender and
re-exported; (b) a name collision meant the new `B-root` empty silently exported as
`B-root.001` - fixed by removing any stale same-named datablock before creating it;
(c) with the hierarchy now matching, switched from "Copy From Other Avatar" (still
failed - likely the FBX root object's own name differing per file) to explicitly
copying the source rig's full `HumanDescription` (bone-name mapping + skeleton) onto
each new importer with `CreateFromThisModel`, which succeeded cleanly
(`avatar.isHuman == true`) for all 3. The War Elephant's Generic-rig import needed
`avatarSetup` explicitly set away from its `NoAvatar` default before an
Animator/Avatar would generate at all - all 4 baked animation takes (Idle/Walk/
Attack/Die) split correctly into named clips automatically once that was set.

`HumanModelFactory.Spawn` gained two backward-compatible optional parameters
(`prefabPathOverride`, `applyPaletteMaterial`, both defaulting to today's dummy-body
behavior - every existing caller unaffected) plus a new public
`ApplyCustomTexture` helper, so the 3 humanoid factories could point at their own
model/texture instead of the generic dummy/civ palette. New
`ElephantAnimationSet`/`ElephantAnimationDriver` (mirroring `BoarAnimationDriver`'s
Generic-rig Playables pattern, not `AnimationDriver`'s Humanoid one) drive the
elephant's 4 clips from `MeleeAttacker`/`Attackable` state. `MauryaWarElephantFactory`
was rewritten to spawn the new model directly (ground alignment, collider,
`GroundFollower`, all mirroring `HumanModelFactory`'s own pattern) instead of routing
through the human dummy at all. Old cosmetic `WeaponAttachment` calls (sword/Kanabo)
were removed from all 4 factories since the new meshes already sculpt their own held
weapons/armor; Mavla Raider keeps its cosmetic horse-mount attachment since the
sourced mesh is a standing foot-soldier pose with no horse geometry, and it's still a
Cavalry-class unit.

**Testing**: clean compile, then live Play mode via UnityMCP (documented flakiness
fix applied). All 4 units spawned via their real factories with no console errors;
verified live (not assumed) that each has a valid Animator+Avatar, the correct
model/texture (not the old dummy), and - for the War Elephant - genuine NavMeshAgent
movement across the scene. Two Enemy-faction spawns briefly vanished during testing;
confirmed via a Player-faction respawn that this was pre-existing scene/AI test
scaffolding interference (likely from the concurrent session's own test setup active
in the same running Editor), not a defect in the new factories.

**Licensing**: Meshy-generated meshes/textures are project-owned (no attribution
needed). The War Elephant's skeleton+animation set is a real third-party CC-BY
Sketchfab model (Asian Elephant, planeta-elefante) - added
`Assets/Resources/UniqueUnits/CREDITS.md` per Roadmap 4.4's standing rule, including
a note that the African Bush Elephant model (also downloaded, also CC-BY) was
evaluated but not used in the final asset.

**Roadmap/CLAUDE.md**: Section 1's unique-units item and Section 4.3's matching
checklist item both marked fully closed (were "backend-complete, visual pending").
Section 5 gained item 8 (done). CLAUDE.md status updated, noting the concurrent
session explicitly.

**Commit**: one scoped commit covering the 4 new/changed factory scripts, the 2 new
`Elephant*` scripts, the `HumanModelFactory` extension, the 4 new
`Assets/Resources/UniqueUnits/` model+texture folders, `CREDITS.md`, and the 3 doc
files above.

---

## 2026-08-27 — Per-civ architectural differentiation, code groundwork (Roadmap Section 5, item 7)

**Scope**: Confirmed item 5 (balance pass resumption) was already fully closed per
CLAUDE.md/roadmap before starting — no open work there. Then addressed the "all 5
civs share one visual building set" gap flagged in Roadmap Section 4.1/4.2: audit
`BuildingModelFactory` for what civ-specific model support would take, propose the
architecture, produce a prioritized asset list, and implement only the code
groundwork (no models, per Section 4.3/4.4 - asset sourcing is the user's task).

**Audit**: `BuildingModelFactory.Spawn(string resourceName, Vector3 rootPosition,
Vector3 fallbackSize, Color civColor)` is the single chokepoint all 9 building
factories (`TownCenterFactory`, `BarracksFactory`, `TowerFactory`, `WallFactory`,
`GateFactory`, `MarketFactory`, `DockFactory`, `FarmFactory`, `HouseFactory`) call
through. Every factory already resolves `CivilizationId` via
`CivilizationRegistry.For(faction)` immediately before calling `Spawn` - civ ID was
in scope at every call site, just not passed through. Buildings aren't CSV-driven
(no building rows in `Assets/Design/Data/`), so there's no generated-asset layer to
touch. Tinting (`TintMaterials`) is a separate post-instantiation step regardless of
model source, so it composes cleanly with a civ-specific model too. Minimap
(`MinimapController`, real top-down camera over scene geometry) and `BuildMenu` (no
icon fields) are both already civ-agnostic and needed no changes.

**Architecture implemented**: `Spawn` now takes a `CivilizationId civId` parameter.
The existing 4-deep Resources.Load fallback chain gained one new candidate, tried
first: `Buildings/{civId}/{resourceName}` (e.g. `Buildings/Chola/Barracks`). Because
a missing `Resources.Load` result already falls through to the next candidate (this
was the existing pattern for the shared-vs-procedural fallback), a civ with no model
yet costs nothing and falls through to today's shared model automatically - no
manifest/registry needed, and models can be sourced one civ/building at a time. All
9 factories updated to pass their already-resolved `civId` through.

**Testing**: New `Assets/Tests/EditMode/BuildingModelFactoryTests.cs` - spawns a
Barracks for all 5 `CivilizationId` values (none of which have a civ-specific model
yet) and asserts the fallback still produces a valid model + collider for every one,
confirming the new lookup doesn't break the existing path. (Suppressed an edit-mode-
only "material leak" log Unity emits from `TintMaterials`' `renderer.materials`
call when run synchronously in a test - not a real issue, doesn't occur at actual
runtime spawn.) Full EditMode suite: 28/28 passing, no regressions, verified via
UnityMCP `run_tests`/`get_test_job`.

**Asset list proposed to user** (not sourced this session, per Section 4.3/4.4):
9 building types × 5 civs = 45 models for full coverage. Priority: Tier 1 (TownCenter
+ Barracks, 10 models) first - highest camera-time, smallest set that makes all 5
civs read as distinct from match start; Tier 2 (Tower/Wall/Gate) next - tall forms
read architecturally distinct fastest; Tier 3 (Market/Dock); Tier 4 (Farm/House,
lowest individual visual weight) last. Full spec (poly/texture targets, style
references per civ) logged in Roadmap Section 4.3.

**Note on peer sessions**: 4 other Claude Code sessions were observed active on this
same repo mid-session (`ListAgents`). Flagged to the user per CLAUDE.md's
single-session-discipline gotcha; this session made no assumptions about concurrent
edits and only touched files directly relevant to its own scoped change.

---

## 2026-08-27 — Resume the balance pass (Roadmap Section 5, item 5)

**Scope**: Item 5 - start actually using `playtest_log.csv` (documented as "process
exists, still empty" - one row existed from item 43's original work), and audit
civ/age/upgrade multiplier stacking. Proposed a specific 15-matchup test list to the
user before running anything (per their explicit ask); user confirmed to proceed as-is.

**Pre-test code audit**: Before running anything, read how HP/armor/damage combine at
spawn time across all 6 melee-unit factories (Soldier/Archer/Cavalry/Spearman/Siege/
Worker) plus Barracks/TownCenter/Dock train-time calculations. Pattern is consistent
everywhere: HP and train time multiply (base × civ × age, by design per `AgeProfile`'s
own doc comment); armor and damage bonuses add (flat Attack/Armor tier +
per-class tier + unique-tech bonus); the `CombatBonus` counter-matrix multiplier is
applied exactly once, at hit time, on top of that. No double-application found in the
code itself.

**Method**: Entered Play mode via UnityMCP, applied the documented flakiness fix
(`Application.runInBackground` + `QueuePlayerLoopUpdate()` + `SceneView.RepaintAll()`),
confirmed `Time.time`/`Time.frameCount` genuinely advancing across real wall-clock
sleeps before testing anything. All fights ran as real 1v1 forced-melee matches
(`execute_code`-spawned units, `AttackMove` called directly, full HP, placed within
immediate attack range - same convention as the one existing playtest_log.csv row) -
not reflection-forced ticks. Used `UnityEngine.Time.time` (not wall-clock/tool-latency
estimates, which turned out to run much longer than expected between tool
round-trips) as the authoritative clock for the stacking-audit's damage-rate
measurements, since MCP tool-call overhead made short real-time sleeps unreliable for
isolating a single hit.

**Results** (all 15 rows logged to `Assets/Design/playtest_log.csv` with full detail):

1. **Roster coverage gaps** (7 fights): Siege vs Infantry/Archer/Cavalry all confirmed
   flat 1x as documented. Spearman vs Infantry: Soldier actually won (2/30 HP) -
   verified against real CSV stats (not fallback constants), Infantry's 1.25x bonus
   vs Spearman narrowly outweighs Spearman's raw stat edge once armor is factored in,
   exactly matching the CombatBonus.cs comment's claim that "a Spearman blob without
   support dies fast to plain Infantry." Spearman mirror match: symmetric, no
   asymmetry bug. War Galley vs Infantry/Spearman: Galley won both but took real
   damage (44%/33% HP lost) - a raw-stat win, not a free one, consistent with the
   roadmap's existing note that only Naval-vs-Archer has real balance evidence.
2. **Unique-unit factory live checks** (3 fights, first-ever live combat confirmation
   for these): Maurya War Elephant beat Cavalry on raw stats as designed; Pillar
   Edict Scholar lost to a dedicated Soldier as intended (support unit, "not a
   fighter"); Maratha Mavla Raider narrowly beat generic Cavalry as the
   higher-damage/lower-HP raider archetype it's designed to be.
3. **Durg Garrison siege-immunity mechanic** (1 measurement, not a win/loss fight):
   built a real Wall via `WallFactory.Place` + `ConstructionSite.CompleteImmediately()`,
   measured Siege's actual live damage-per-hit against it before/after
   `Garrison.TryGarrison()`. Ungarrisoned: exactly 39 dmg/hit (15 base × 3x Siege-vs-
   Building − 6 wall armor). Garrisoned: exactly 9 dmg/hit (15 × 1x − 6 armor). Confirms
   the coded 3x-to-1x immunity strip is exact, live, not just present in the code -
   this mechanic had never been given a live before/after damage measurement before.
4. **Stacking audit** (4 fights): re-confirmed baseline Cavalry-vs-Archer (Archer won
   narrowly, 1.2/18 HP - first playtest_log entry for this specific pairing, matches
   hand-calculated math exactly). Then stacked every non-retroactive bonus at once on
   one side (Rajput civ, Imperial age, max flat + max per-class Attack/Armor tiers,
   unique tech) and ran that Cavalry against both a baseline Archer and a baseline
   Spearman (the hard 2x anti-cavalry counter). In both cases the fully-stacked
   Cavalry won overwhelmingly (52.2/55.2 HP and 50.7/55.2 HP respectively). Checked
   the actual numbers against the coded formula by hand each time - every result
   matched exactly, confirming no double-counting across civ/age/upgrade/unique-tech
   layers. **Conclusion: this is not a stacking bug** - a large enough tech-level gap
   overwhelming a hard counter is the intended, designed consequence of an AoE-style
   tech tree, not silent double-application. The counter multiplier itself (0.4x/2x)
   still applied correctly in both fights; it was just outweighed by the flat
   damage/armor gap. No code changes were needed anywhere in this session.

**One non-bug side note found and documented (not fixed)**: `CombatBonus.cs`'s
Spearman doc comment describes Infantry's bonus against Spearman as Spearman
receiving "weak 0.8x" - the actual coded multiplier (1.25x extra damage dealt by
Infantry) is functionally identical but described backwards in the comment text.
Comment-only, zero behavior impact - noted in the playtest_log.csv row and here
rather than touched this session (out of scope: not a numeric balance issue).

**Console**: A few `NavMeshAgent`/`SetDestination` warnings from later test-position
spawns landing near the edge of/outside the baked NavMesh area - a test-harness
artifact (spawn coordinates chosen for convenience, not on the actual walkable mesh),
not a game bug; `MeleeAttacker`'s damage pipeline doesn't depend on `NavMeshAgent`
placement when the target is already in range, and every fight's result matched hand-
calculated expected values exactly regardless, confirming this had no effect on any
result.

**Roadmap/CLAUDE.md**: Section 5 item 5 marked done for this session's scope (training
cost-vs-power ratios and continued sustained playtesting remain explicitly open for a
future balance session, not silently closed). `playtest_log.csv` now has 16 total
rows (1 pre-existing + 15 new).

**No code changes landed this session** - every system audited checked out correct.
This session's changes are `Assets/Design/playtest_log.csv`, `docs/ROADMAP.md`,
`docs/SESSION_LOG.md`, and `CLAUDE.md`'s status section.

---

## 2026-08-27 — Re-verify reflection/config-only items live (Roadmap Section 5, item 4)

**Scope**: Item 3 (4 unique-unit factories) marked backend-complete-not-fully-closed
in the roadmap/CLAUDE.md first (models still pending from the user's asset pipeline,
per Roadmap Section 4.3 — not this session's job, and not touched further). Then item
4: re-verify every item in the dev history that was previously confirmed only via
reflection/config-inspection or reflection-forced ticks, not a real live-ticking Play
mode check, now that the "Editor frame stuck" flakiness has a real fix.

**Search method**: grepped the full detailed dev log
(`1787767106136_plan-it-out-and-dynamic-wolf.md`, referenced as source-of-truth by
Roadmap.md) for every variant of "reflection only", "not live", "config inspection",
"blocked by flakiness", "isolated reflection tests", "ambiguous/stale", etc. Found 3
genuine candidates (not just any mention of "verified via reflection", which is this
project's normal verification method and often paired with a real Play mode smoke
test too — only cases where a live check was explicitly blocked, skipped, or worked
around via reflection-forced ticks before the real fix existed):

1. **Wall's NavMeshObstacle carving** (item 35) — the one already flagged in
   Roadmap.md; config-inspection only, live check blocked by the flakiness.
2. **Control-groups dead-unit pruning** (item 34) — the `RemoveAll(unit => unit ==
   null)` path in `SelectionManager.SelectControlGroup` hit the flakiness mid-test and
   was accepted on the strength of being copied from already-proven code elsewhere in
   the file, not its own live confirmation.
3. **Highlands/Coastal ground+NavMesh rebuild** (item 44) —
   `ProceduralGround.Rebuild()`/`NavMeshBaker.RebuildNavMesh()` (called from
   `CivilizationSetup.BeginMatchCore`) were verified twice, but both times via
   reflection-forced `Start()`/`Update()` invocation while working around the stuck
   frame, before the real fix existed.

Presented this list to the user before running anything (per their explicit ask); all
3 confirmed in scope.

**Method**: entered Play mode, applied the fix (`Application.runInBackground = true`
+ `EditorApplication.QueuePlayerLoopUpdate()` + `SceneView.RepaintAll()` + GameView
repaint), confirmed `Time.frameCount`/`Time.time` actually advancing across real wall-
clock sleeps (not reflection-forced single ticks) before testing anything. All 3
tests ran against the real, naturally-ticking frame loop via UnityMCP `execute_code`,
polling live state across real `sleep` waits between checks — not single reflection
snapshots.

**1. Wall carving — confirmed correct, one real (non-bug) nuance found.** Built a
6-segment wall row (RiverValley) and used `NavMesh.CalculatePath`/`NavMesh.SamplePosition`
plus an actual `SoldierFactory`-spawned unit's live `MoveTo` order. First pass (walls
left at default just-placed state) showed the path cutting straight through the wall
row with zero detour — looked like a real bug, until found the cause: `WallFactory.Place`
was called with `buildTime=0`/no builder assigned, and `ConstructionSite.Awake()`
deliberately squashes a not-yet-built wall to `localScale.y = 0.01` (the "foundation"
visual state) until a Builder is actively working it. `NavMeshObstacle.size` scales
with the object's own transform, so at `scale.y=0.01` the obstacle's box no longer
vertically overlaps the walkable NavMesh surface at all — an unbuilt foundation
doesn't block pathing. This is a real, previously-undocumented mechanical
consequence of the construction-visual system, not something anyone verified before
(reflection-only or otherwise) — but it's arguably correct behavior (a foundation
shouldn't act as a full wall) and nothing in the game currently depends on it either
way, so documenting rather than changing it. Called `ConstructionSite.CompleteImmediately()`
on all 6 segments (full height, `scale=(1,1,1)`) and re-ran both tests: `NavMesh.SamplePosition`
at the wall's exact center now correctly finds nothing within 0.05 units (real hole),
`NavMesh.CalculatePath` from one side to the other correctly detours around the
wall's end (via `x=7.7`, past the row's `x=±7.2` extent) instead of cutting through,
and a real live unit's `MoveTo` order physically walked that same detour and arrived
exactly at its destination. **Confirmed live: a completed Wall's carving works
correctly.** Roadmap Section 1's Wall bullet closed.

**2. Control-groups dead-unit pruning — confirmed correct.** Spawned 2 real Soldiers,
populated `SelectionManager`'s private `_selected` list via reflection (matching this
project's established verification convention), invoked the private
`AssignControlGroup(0)` to put both in group 1. Destroyed one unit
(`GameObject.Destroy`) and let real frames pass so it actually left the scene (not
just queued) before checking — confirmed the group still held 2 entries with a Unity
fake-null in it. Invoked the private `SelectControlGroup(0)` (the exact reselect path
containing the `RemoveAll(unit => unit == null)` line) — no exception, group correctly
pruned to 1 entry, and `_selected` ended up holding only the surviving unit. No
regression.

**3. Highlands/Coastal ground+NavMesh rebuild — confirmed correct.** Invoked the
private `CivilizationSetup.BeginMatchCore(Chola, Vijayanagara, MapId.Highlands)` via
reflection (public `BeginMatch` has no map parameter). Measured the real `Ground`
GameObject's `Renderer.bounds` (130×130, exact match to `MapDefinitionData.GroundSize`)
and `NavMesh.CalculateTriangulation()`'s real walkable extent (~128.7×128.7, consistent
with the 145×145 bake-bounds parameter minus normal voxel/edge margin). Spawned a
real Soldier at one edge (`x=-60`) and sent a live `MoveTo` to the opposite edge
(`x=60`) — watched it progress across multiple real-time polls and arrive exactly at
`(60, 0.94, 0)`, confirming the full 120-unit span is genuinely walkable, not just
mesh-sized. Repeated for Coastal: `Renderer.bounds` 125×125 (exact match), NavMesh
extent asymmetric (98.7×123.7 — expected, since Coastal's water rectangle cuts into
one edge's walkable X range per item 49), and a live unit crossed the full 110-unit
north-south land span (`z=-55` to `z=55`, away from water) end to end. Both maps
confirmed genuinely rebuilt at their real scaled size, not stale reflection-forced
snapshots.

**Console**: a handful of pre-existing, already-documented noise (RallyPoint/
TownCenter component-strip ordering during `BuildingModelFactory`'s prefab load, and
transient NavMeshAgent-not-yet-placed warnings during map-switch spawn timing) — none
newly introduced by this session's testing, none affecting the actual verified
behavior.

**No code changes were needed** — all 3 items check out correct in live Play mode.
This session's only changes are documentation (Roadmap.md, this log, CLAUDE.md
status).

**Roadmap**: Section 1's Wall bullet and Section 5 item 4 marked done. Section 5
item 3's changelog line updated to reflect its backend-complete/visual-pending state
explicitly, per the user's ask at the top of this session.

---

## 2026-08-27 — 4 missing unique-unit factories (Roadmap Section 5, item 3)

**Scope**: Built the 4 unique-unit factories CSV data already had
(`maurya_war_elephant`, `pillar_edict_scholar`, `maratha_mavla_raider`,
`maratha_durg_garrison`). Verified every unit's stats/cost/train-time against its
`unit_roster_template.csv` row before writing any code, per the user's explicit ask.

**Conflicts flagged and confirmed with the user before implementing** (both resolved
toward the fuller option, not stats-only stubs):
1. `UniqueUnitDefinition`/`Barracks`/`BuildMenu` only supported ONE unique unit per
   civ (single dict entry, single `TrainingUnit.UniqueUnit` slot, single button) —
   Maurya and Maratha each need 2. Extended `UniqueUnitDefinition.Sources` to a
   per-civ `List` (`For(civId, slot)`/`CountFor(civId)`), added
   `TrainingUnit.UniqueUnit2`/`Barracks.RequestTrainUniqueUnit(int)`, and a 2nd
   button+label in `BuildMenu`/`Main.unity` (via UnityMCP, duplicating the existing
   `UniqueUnitButton`), gated on `UniqueUnitCount > 1` so Chola/Vijayanagara/Rajput's
   UI is unchanged.
2. `pillar_edict_scholar` and `maratha_durg_garrison` both turned out to be real
   combat-shaped Infantry units (CSV Category, not Support) so they follow the same
   factory pattern as the existing 3 unique units — but each also carries a genuine
   new mechanic:
   - **Pillar Edict Scholar**: +50% gather-rate aura to nearby Workers. New
     `PillarEdictAura` (static registry, same convention as `Building.All`) +
     `Gatherer._auraMultiplier`, polled every 0.5s (self-correcting as a worker
     walks in/out of range, no enter/exit events needed).
   - **Maratha Durg Garrison**: immune to Siege's 3x anti-building bonus while
     garrisoned inside a Wall/Tower. New `Garrison` component (added by
     `WallFactory`/`TowerFactory`, single-slot) + `DurgGarrisonWorker` (mirrors
     `FarmWorker`'s walk-then-join shape) + `Attackable.SiegeImmune`, checked as a
     target-side override in `MeleeAttacker` (deliberately NOT folded into
     `CombatBonus` itself, keeping that system's existing "don't merge with
     CounterMatrix" precedent intact). `SelectionManager` gained a `hitGarrison`
     branch (right-click an owned Wall/Tower with a Durg Garrison unit selected),
     `BuildMenu` gained an "Ungarrison" button (Wall/Tower had no panel before this).

**A real EditMode-test-only bug found and fixed along the way**: `Garrison`
originally resolved its `Attackable` sibling in `Awake()` and `PillarEdictAura`
registered itself in `OnEnable()` — both failed silently in EditMode tests because
Unity doesn't invoke `Awake`/`OnEnable` outside Play Mode for AddComponent-created
objects (only destruction callbacks like `OnDisable` are unreliable there too, it
turns out — confirmed via a `MissingReferenceException` from a stale destroyed
`PillarEdictAura` still sitting in the static registry across two tests). Fixed by
switching `Garrison` to lazy `GetComponent` resolution (same convention `Barracks`
already documents for Site/FactionMember) and `PillarEdictAura` to register on
`Configure()` instead of `OnEnable()`; `Gatherer`'s aura scan also now skips a
destroyed (fake-null) aura defensively rather than assuming `OnDisable` cleaned it
up in time. None of this affects real Play Mode behavior (`Awake`/`OnEnable`/
`OnDisable` all fire normally there) — it only made the EditMode tests trustworthy.

**Verification**: 27/27 EditMode tests pass (10 new, in
`Assets/Tests/EditMode/UniqueUnitsTests.cs`). Play Mode, via UnityMCP `execute_code`:
spawned all 4 factories directly and confirmed class/HP/components; spawned a Wall,
garrisoned a Durg Garrison unit, and confirmed a Siege `MeleeAttacker`'s actual hit
dropped from 39 to 9 damage (garrisoned vs. not) on the same wall type; confirmed a
Gatherer's `AuraMultiplier` reads 1.5x near a Pillar Edict Scholar and 1x far away.
Did not fully step through Barracks' real multi-second training timer in Play Mode
(EditMode tests cover `RequestTrainUniqueUnit`'s cost/slot logic; the Play Mode pass
covers the actual spawn path `TickTraining` calls into, which is the part that
couldn't be exercised without either playing).

**Assets**: no models exist for any of the 4 — all spawn on the shared Human
Character Dummy body (Male), matching the "stats distinguish it before art catches
up" convention. **All 4 still need real models from the user** before they visually
stand out from each other or from the existing roster; not sourced this session per
CLAUDE.md/Roadmap Section 4 (that's explicitly not this session's job).

---

## 2026-08-27 — Per-civ passive bonuses (Roadmap Section 5, item 2)

**Scope**: Wired every civ's passive bonus live, not just the 4 data-backed entries
the roadmap line literally names — confirmed with the user upfront to include the 6
structural/mechanical bonuses too (Free Houses, Classical-Age start, dismount-survival,
permanent scout memory, fortification cost/range), since none of those 10 actually did
anything in gameplay before this session despite existing in the CSV design doc.

**Audit first** (per the user's explicit ask, before writing any code): read every
civ's actual generated `Assets/Resources/Data/Generated/Civilizations/*.asset` and
compared against what `CivilizationProfile`'s 5 named fields read. Chola and
Vijayanagara had **zero** live gap — their generated `passiveBonuses` were already
fully covered by the existing 5 fields. The real data-backed gap was exactly 4 entries:
Rajput's Cavalry-only Gold-cost discount, Maurya's Worker-only move-speed bonus, and
Maratha's Cavalry-only move-speed + Naval-only train-time bonuses. Confirmed no overlap
with `CombatBonus`/`CounterMatrix` (different enum — `StatModifier.targetCategory` uses
the Data-side `UnitCategory`, `CombatBonus` uses the Combat-namespace `UnitClass`) or
with `AgeProfile`/`UpgradeProgress`'s hardcoded formulas (untouched — Maurya's
Classical-Age-start bonus only changes what age `AgeProgress.Initialize` *seeds*, not
the formulas themselves).

**Part A — 4 data-backed bonuses**: added
`CivilizationProfile.FindCategoryMultiplier(CivilizationId, StatType, UnitCategory,
fallback)` — the general-purpose counterpart the class's own existing comment already
called for, deliberately excluding `applyToAllCategories` entries so it can never
double-count what the 5 named fields already surface. 4 call sites:
`CavalryFactory.cs`/`WorkerFactory.cs` (move speed), `Barracks.RequestTrainCavalry`
(Gold cost — documented as a hand-picked reading, since `StatModifier` has no
resource-type field to distinguish Gold from Wood/Stone), `Dock.ScaledTrainTime`
(Naval train time, as an independent factor alongside the existing civ-wide one).

**Part B — 6 structural mechanics**, none representable as `passiveBonuses` data
(`CsvToScriptableObject.cs` already documents why: no Building category in
`UnitCategory`, no probabilistic-mechanic support) — hand-written civ checks, same
shape as the project's existing `UniqueTechDefinition` per-civ dictionary pattern:
- Maurya: Houses cost no Wood (`BuildingPlacer.WoodMultiplierFor`), starts in Classical
  Age (`AgeProgress.Initialize` gained an `AgeId startingAge` overload, defaulted so
  every other call site is unchanged).
- Vijayanagara: Wall/Gate/Tower cost 20% less Stone (`BuildingPlacer.StoneMultiplierFor`,
  6 case-block edits since there's no shared cost-deduction method), Towers get +1
  attack range (`TowerAttacker` gained a `Configure(float)`, previously the only combat
  component with no factory-time configuration at all).
- Rajput: defeated Cavalry has a 25% chance to leave a weakened Infantry survivor —
  new `Assets/Scripts/Combat/RajputDefianceHook.cs`, kept separate from
  `Attackable.TakeDamage` so `Attackable` doesn't need a `Core`/`Multiplayer`
  dependency baked into its damage-resolution method. Uses `DeterministicRandom.Match`
  (not `UnityEngine.Random`), and only draws from it for eligible deaths so every other
  death in the game doesn't silently consume a Match roll.
- Maratha: permanent scouted-position memory — the one bonus needing real new state
  (confirmed with the user this was worth the extra scope: buildings are easy since
  they don't move, but units needed a genuinely new "last-known-position" concept that
  didn't exist anywhere in the fog system). `FogOfWarManager.cs`: buildings that have
  ever been seen stay permanently revealed; units get a frozen primitive-shape ghost
  marker (no new art — reuses `GameplayMaterial.CreateTransparent`, collider stripped
  immediately per this file's own existing lesson about stray colliders breaking every
  raycast-based click) at their last-visible position when they leave vision, replaced
  only if the real unit is seen again later. Gated behind
  `CivilizationRegistry.For(FactionId.Player) == Maratha` (fog was already
  confirmed Player-only, so no new per-faction plumbing needed).

**Tests**: `Assets/Tests/EditMode/CivPassiveBonusTests.cs`, 8 new EditMode tests (17
total in the suite now) covering `FindCategoryMultiplier` against the audited values,
the Rajput Gold-cost path through a real `Barracks`, `BuildingPlacer`'s cost-multiplier
helpers (made `internal` + a new `Assets/Scripts/AssemblyInfo.cs` granting
`InternalsVisibleTo("KingdomsOfBharat.Tests")`, rather than driving the full
mouse-driven placement UI), `AgeProgress.Initialize`'s new overload, and
`RajputDefianceHook`'s gating logic (split into pure `IsEligible`/`RollSucceeds` so the
roll itself doesn't need to run through a full unit spawn in a test). All pass.

**Manual verification**: Play Mode, via UnityMCP `execute_code`, one real match started
per civ (`CivilizationSetup.BeginMatch`) — confirmed all 10 bonuses fire with the exact
audited numbers (Maurya: Classical Age at start, House costs 0 Wood, Worker speed
4.025 = 3.5×1.15; Vijayanagara: Wall/Tower Stone ×0.8, Tower range 10; Maratha: Cavalry
speed 7.8 = 6.5×1.2, Dock train time 5.1 = 6×0.85; Rajput: ~22% survival rate over 200
trials (expected 25%), 0% for a non-Rajput civ over 100 trials; Maratha fog: enemy
building/unit both confirmed staying revealed/getting a ghost marker after leaving
vision, and confirmed NOT happening for Chola — no regression to normal fog behavior).

**Roadmap**: Section 1 item 2 and Section 5 item 2 marked done.

---

## 2026-08-27 — Training/trade UI batch (Roadmap Section 5, item 1)

**Scope**: Closed the training/trade UI gap for Cavalry, Siege, Dock units (Fishing
Boat/War Galley), Spearman, and Market Buy/Sell — all previously backend-only (only
`AiController` or nothing at all could reach `Barracks.RequestTrainCavalry/Siege/
Spearman`, `Dock.RequestTrainFishingBoat/WarGalley`, `Market.Sell/Buy`).

**What changed**:
- `Assets/Scripts/UI/BuildMenu.cs`: added Cavalry/Siege/Spearman buttons to the
  existing Barracks panel (same `CommandBus`/`TrainCommand` pattern as Soldier/Archer),
  a new Dock panel (Fishing Boat/War Galley training), and a new Market panel (Sell/Buy
  buttons for Wood/Food/Stone at a fixed 50-unit increment, calling `Market.Sell`/`Buy`
  directly — not through `CommandBus`, matching the existing precedent set by
  `ResearchAttackAtSelected`/etc. for non-train building actions).
- `Assets/Scripts/Buildings/Market.cs`: `EffectiveSellRate`/`EffectiveBuyRate` made
  public so `BuildMenu` can read the live rate for trade-button labels/gating.
- `Assets/Scripts/AI/AiController.cs`: fixed a stale comment that inaccurately claimed
  players already had Spearman access via `Barracks.RequestTrainSpearman` before this
  session's UI actually existed.
- New scene Canvas buttons in `Assets/Scenes/Main.unity` (11 new buttons + labels),
  added via UnityMCP tooling (duplicate + reposition + rewire), not hand-edited YAML.

**Scope expansion (user-confirmed mid-session)**: CLAUDE.md requires a test for every
new system, but the project had **zero `.asmdef` files** anywhere — all code compiled
into the implicit default `Assembly-CSharp`, which a new Tests assembly cannot
reference (Unity compiles predefined assemblies last, specifically so they can
reference custom asmdefs, not the reverse). Asked the user how to handle this; they
chose to do the restructuring now rather than defer it. Added:
- `Assets/Scripts/KingdomsOfBharat.Runtime.asmdef` (all of `Assets/Scripts`)
- `Assets/Editor/KingdomsOfBharat.Editor.asmdef` (references Runtime)
- `Assets/Tests/EditMode/KingdomsOfBharat.Tests.asmdef` (references Runtime + Unity
  Test Framework)

Investigated first for risk (reflection-across-boundaries, stray `UnityEditor` refs in
runtime code, third-party asset folders) — confirmed low-risk, zero new compile errors
after the split.

**Tests**: First automated tests in the project —
`Assets/Tests/EditMode/TrainingAndTradeTests.cs`, 9 EditMode tests covering
`RequestTrainCavalry/Siege/Spearman`, `Dock.RequestTrainFishingBoat/WarGalley`,
`Market.Sell/Buy` (including an insufficient-resource no-op case). All 9 pass.

**Manual verification**: Play Mode, via UnityMCP `execute_code` — spawned real
Barracks/Dock/Market with `FactionMember`, selected them through `SelectionManager`,
invoked the actual scene `Button.onClick` (not a direct method call) to exercise the
full click → `CommandBus` → `RequestTrainX`/`Sell`/`Buy` path. Confirmed: Cavalry
trains and spawns with correct Food/Gold deduction (70/50); War Galley trains and
spawns with correct deduction (60 Food/60 Gold); Market Sell/Buy apply the correct
0.7x/1.3x rate immediately (not through `CommandBus`, by design).

**Follow-on note**: `MatchManager`/`CivilizationSetup.BeginMatch` gate `SimClock`
ticking (and therefore all `CommandBus`-queued commands, including the pre-existing
Worker/Soldier/Archer training) behind `HasMatchStarted` — nothing trains until the
match has actually started via the CivPicker flow. Not a bug introduced this session
(pre-existing behavior, confirmed by testing the already-working Soldier button through
the same mechanism), just worth knowing when testing training UI directly in the Editor
without going through the normal match-start flow.

**Roadmap**: Section 1 items 1–2 checked off; Section 5 item 1 marked done; Section 2
gained a note about the new asmdef structure being a deliberate keeper, not a target
for "cleanup."

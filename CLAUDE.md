# Project: Kingdoms of Bharat

Historical RTS across Indian kingdoms/empires, targeting AoE II/IV-level systemic
depth. Full roadmap: `/docs/ROADMAP.md` (Master Roadmap v3 — sections: 1. Open items
punch list, 2. Architectural notes to preserve, 3. Process note, 4. Art direction &
asset requirements, 5. Priority order).

## Current status (keep current — update every session)
- Working from Roadmap Section 5's priority order.
- Currently on: nothing started yet for the next session. Section 5 items 1-5, 7-9
  are all done. Remaining real options: **civ-specific building models for the
  remaining 3 civs** (Chola's 9/9 and Vijayanagara's 9/9 are landed; Rajput/Maurya/
  Maratha, 27 models, are unstarted; `Assets/Editor/MeshyBuildingImporter.cs` is
  reusable but each asset needs its own live scale/rotation verification, not blind
  reuse of Chola's or Vijayanagara's numbers), **wiring in a Crusader Knight body** (rig-compatibility verified positive in a concurrent session
  — scale normalization + weapon re-parenting still unstarted), the **3 new
  worker-mechanics items from this session's audit** (Repair system, general
  garrisoning system, and dedicated resource-specific drop-off buildings — all real
  new systems, none started, see Roadmap Section 1's "worker mechanics audit" entry
  for full scoping notes), other "everything else" items (music, tutorial,
  performance profiling, store assets, multiplayer determinism gaps, README drift),
  two adjacent findings from a concurrent Naval balance session (Naval factories
  missing `ClassArmorBonus`/`ClassDamageBonus`; `BoatAttacker`'s 1.5s vs
  `MeleeAttacker`'s 1.0s attack interval), or continued balance work — user's call.
- Last completed (this session): **Vijayanagara building-model rotation fix**
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
1. On start: read `/docs/ROADMAP.md` Section 5 and this file's "Current status." State
   the next item and confirm before starting.
2. One roadmap item per session. Flag adjacent work instead of silently expanding
   scope — ask whether to include it now or log it as a new item.
3. Plan Mode before nontrivial changes.
4. Test before calling it done.
5. Update Section 1/5 of the roadmap (check off/reorder) and this file's status.
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

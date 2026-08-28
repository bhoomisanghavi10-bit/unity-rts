# Session Log — Kingdoms of Bharat

Chronological log of Claude Code sessions against this repo, per CLAUDE.md's session
protocol (step 6). Newest entries at the top.

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

# Session Log — Kingdoms of Bharat

Chronological log of Claude Code sessions against this repo, per CLAUDE.md's session
protocol (step 6). Newest entries at the top.

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

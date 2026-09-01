# Master Roadmap — Kingdoms of Bharat (v3, reconciled against actual dev history)

Source of truth for prior status: `1787767106136_plan-it-out-and-dynamic-wolf.md`
(items 1–51+, Phases 1–6). This document does not restate that history — it
synthesizes it into what's genuinely open, and folds in anything from the earlier
generic AAA-planning docs that still applies. Treat the source doc as the detailed
commit-level log; treat this as the current punch list.

A second, more recent companion plan — `docs/AOE_PARITY_EXECUTION_PLAN.md` — adds a
separate phased execution order layered on top of this roadmap's own priorities
(Section 5). **Section 6 below summarizes that plan's status** (what's closed, what's
deferred and why, what's next) so this file alone is enough context to pick up
either the roadmap's own priority order or the AoE-Parity plan's phases — the
companion doc itself carries the full step-by-step detail for each phase.

---

## 0. Reality Check

The project is far past "prototype approaching AoE-level" — it now has systems most
solo RTS projects never reach: a full counter-triangle roster (Infantry/Archer/
Cavalry/Siege/Spearman/Naval), 5 civilizations (Chola/Vijayanagara/Rajput/Maurya/
Maratha) on a real CSV→ScriptableObject data pipeline, naval warfare with docks and
two ship types, 3-faction diplomacy with AI-initiated alliances, a campaign/scenario
system with real missions, save/load, a settings menu with colorblind mode and full
key rebinding, SFX audio, formations (5 types), rally points, control groups, and a
foundational deterministic-lockstep multiplayer layer. Every one of these was still
"not started" in the earlier generic roadmap — that roadmap is now superseded almost
entirely. What follows is the real remaining list.

---

## 1. Genuinely Open Items (the actual punch list)

### High priority — gaps that affect what's already shipped

- [x] **Training UI gaps**: Cavalry, Siege, Dock/naval units, and Spearman all train
  through backend-only paths (M-key or code-only, no BuildMenu button). This has been
  deliberately deferred across multiple items — it's now the single most repeated
  "not yet picked up" item in the whole log. Worth closing as one batch rather than
  continuing to defer it per-unit.
  **Closed** — `BuildMenu.cs` now has Cavalry/Siege/Spearman buttons on the Barracks
  panel and a new Dock panel (Fishing Boat/War Galley), following the existing
  Soldier/Archer `CommandBus`/`TrainCommand` pattern. Verified live in Play Mode
  (see `docs/SESSION_LOG.md`).
- [x] **Market trade UI**: Buy/Sell backend (item 39) works, but the BuildMenu-level
  trade interface was deliberately deferred and doesn't appear closed later — same
  category as the training-UI gap above.
  **Closed** — new Market panel in `BuildMenu.cs` with Sell/Buy buttons for
  Wood/Food/Stone at a fixed 50-unit increment, calling `Market.Sell`/`Buy` directly
  (not through `CommandBus` — matches the existing precedent set by
  `ResearchAttackAtSelected`/etc. for non-train building actions on `BuildMenu`).
- [ ] **Multiplayer determinism gaps**: `BuildingPlacer` orders still bypass
  `CommandBus` (only Move/Train/Attack are wired); no rollback/resync-on-desync logic
  despite `StateHash` existing to detect a desync; cross-machine NavMeshAgent/physics
  determinism is flagged as unverified and a known risk of the lockstep choice — this
  needs real testing once (if) an actual network transport is added, not just
  single-process proof.
- [x] **WaterMover has no obstacle avoidance** — straight-line movement only. Fine for
  the current single-rectangle water body; will break the moment any map gets a
  non-trivial coastline. Worth fixing before adding more naval-heavy maps.
  **Closed** — the real, reachable-today bug wasn't a future non-convex-coastline
  problem: `WaterMover.MoveTo` stored any destination with zero bounds checking, so a
  player right-click, rally point, or attack-move past the shoreline sailed a boat
  straight onto land right now, on the existing Coastal map. Since the water region is
  a single convex rectangle, clamping the destination into it before storing
  (`WaterProximity.ClampToWater`, called from `WaterMover.MoveTo`) is sufficient to
  guarantee the whole straight-line path stays in water — real pathfinding for a
  non-convex coastline stays out of scope since no map defines one. 5 new EditMode
  tests (`WaterMovementTests.cs`), all 37 pass. Live-verified in Play mode via
  UnityMCP: a boat ordered onto dry land stopped exactly at the shoreline instead of
  sailing onto it, confirmed across several real ticked frames. See
  `docs/SESSION_LOG.md`.
- [x] **Wall's NavMeshObstacle carving was never confirmed live** — only verified by
  config inspection, blocked repeatedly by the Editor "frame stuck" flakiness.
  **Closed** — live-confirmed in a genuinely, naturally-ticking Play mode session
  (via the `Application.runInBackground` + `QueuePlayerLoopUpdate()` + forced-repaint
  fix): a fully-constructed Wall correctly carves a hole in the NavMesh and a live
  unit's move order actually detours around it end-to-end, not just a config check.
  **Real nuance found along the way, not a bug**: a Wall's `NavMeshObstacle.size` is
  read at its current `transform.localScale`, and `ConstructionSite` deliberately
  keeps a not-yet-built wall squashed to `scale.y = 0.01` (the "foundation" visual) —
  at that height the obstacle's box no longer vertically overlaps the walkable
  NavMesh surface, so an unbuilt wall foundation doesn't block pathing at all. Only
  matters until a Builder is actually assigned to it (`ConstructionSite.BeginBuilding`);
  a completed Wall (the only state that matters for real gameplay) carves correctly.
  See `docs/SESSION_LOG.md` for the full test methodology. **This exact nuance is
  fixed as a side effect of the AoE building-footprint item below** (2026-08-28) -
  `ConstructionSite`'s squash now targets the visual child only, so a foundation's
  root (and any NavMeshObstacle on it) keeps a stable scale from placement onward.
- [x] **AoE-style building footprint rules** (ad hoc request, 2026-08-28) — audit
  found placement/collision had no grid concept at all: `BuildingPlacer` used a
  circle-distance clearance check (not a square/tile overlap), and only Wall/Gate/
  Tower carved a `NavMeshObstacle` - Barracks/House/Farm/Market/Dock/TownCenter
  blocked no movement whatsoever, units walked straight through them. New
  `BuildingFootprint.cs`: a 1-world-unit-per-tile table (House/Farm/Tower 2x2,
  Market 3x3, Barracks 4x4, TownCenter 6x6), a `BuildingFootprintTag` component so
  the new AABB placement-overlap check (`BuildingFootprint.IsClear`, replacing the
  old circle check) sees every building's real shape, and a `NavMeshObstacle` shrunk
  by a fixed 0.5-unit `Margin` on every side (the "thin walkable edge" every
  building leaves per the AoE spec this was modeled on). Wall/Gate are the one
  exemption (full footprint, no margin, unchanged modular chain-placement system) -
  Dock is a second, narrower exemption from the *square* requirement only (stays its
  existing 2.2x4 rectangle, a functional need to reach water) but still gets the
  margin treatment and, new this session, an actual `NavMeshObstacle` (previously
  had none at all). `ConstructionSite`'s squash-to-grow animation was moved from the
  root transform to the visual child specifically so this holds from the instant a
  foundation is placed, not only once a Builder starts working it (see the Wall item
  above - this incidentally fixes that exact documented gap for Wall/Gate/Tower too).
  11 new EditMode tests (`BuildingFootprintTests.cs`, `ConstructionSiteTests.cs`),
  all pass. See `docs/SESSION_LOG.md` for the full tile-size table, world-unit
  conversion, and live-verification methodology.

### Medium priority — real content/design work, not bug fixes

- [x] **Worker mechanics audit vs. AoE reference behavior** (2026-08-29) — audited
  Gatherer, Farm/FarmWorker, LivestockWorker, Builder/ConstructionSite, and
  worker combat/boar-hunting against 6 AoE reference mechanics. Results: resource
  walking (nearest-drop-off) and carry capacity already match; multi-builder
  construction was flat-linear, not diminishing (fixed this session, see below);
  repair and general garrisoning are missing entirely (see the two new items
  below); self-defense partially matched at audit time (workers had a weak
  `MeleeAttacker` and could fight/hunt boars, but only via an explicit player
  attack-move command — `Gatherer` and `MeleeAttacker` had zero cross-awareness,
  so a worker being attacked mid-gather was never auto-interrupted into a
  defensive state, unlike AoE's pattern; **closed 2026-09-01, see the
  "Worker self-defense/cross-awareness" item below**). **Multi-builder
  construction speed fixed same session (2026-08-29):**
  `ConstructionSite` now uses AoE II's diminishing-returns formula
  (`ConstructionSite.SpeedMultiplier`: 1x/1.6x/1.9x/2.2x for 1/2/3/4 simultaneous
  workers, +0.3x per worker beyond the 2nd) instead of flat-linear (`n` workers =
  `n`x speed), applied uniformly across every building type. A deliberate balance
  change to numbers item 5's pass covers, not a bug fix — logged as a process
  note (not a fight row) in `Assets/Design/playtest_log.csv`. 5 new EditMode
  tests (`ConstructionSiteTests.SpeedMultiplier_MatchesAoeIIDiminishingReturnsFormula`),
  all 61 pass. Live-verified in Play mode via UnityMCP: real ticked `Update()`
  progress across 4 simultaneous foundations matched the formula's predicted
  ratios to within float rounding, not just the pure-function unit test — and
  caught a real tooling gotcha along the way (a live Play session can keep
  running a stale compiled assembly after a script edit; `refresh_unity` with
  `mode=force` was needed before the new formula actually took effect at
  runtime, confirmed by `SpeedMultiplier` being briefly unresolvable via
  reflection at runtime despite compiling clean). **Resource drop-off scope
  question resolved**: user confirmed dedicated resource-specific drop-off
  buildings (Lumber Camp/Mining Camp/Mill-equivalent) over keeping unified
  TownCenter-only drop-off — logged as its own item below, not implemented this
  session (real new content, not a small tweak). See `docs/SESSION_LOG.md`.
- [x] **Dedicated resource-specific drop-off buildings** (from the worker mechanics
  audit, 2026-08-29; implemented as AoE-Parity Execution Plan Phase 3.1,
  2026-09-01) — `Gatherer.FindNearestDropOff` used to only ever consider
  `TownCenter`; now routes by resource type. **Closed 2026-09-01.** Verified
  the plan's premise against the actual repo first (per instruction): confirmed
  `Gatherer.FindNearestDropOff` really did hardcode `is TownCenter` with no
  resource-type awareness, and `BuildingFootprint`/`BuildingFootprintTag` (incl.
  `GetNearestApproachPoint`) needed no changes — the existing carve-obstacle/
  approach-point system generalizes to new building kinds with zero code
  changes, just a new `BuildingFootprint.DropOffTiles` constant. Added 3 new
  marker buildings (`LumberCamp.cs`/`MiningCamp.cs`/`Mill.cs`, mirroring
  `House.cs`'s shape exactly) + matching factories (100 Wood each, 200 HP,
  2x2 footprint, matching House/Farm's cost tier), each accepting one resource
  type per AoE convention (Lumber Camp: Wood; Mining Camp: Gold **and** Stone;
  Mill: Food) via a new `Gatherer.AcceptsDropOff(Building, ResourceType)`
  routing rule (`internal` for direct testing) — `TownCenter` stays the
  universal drop-off, unchanged. 3 new procedural fallback silhouettes in
  `ProceduralBuildingFactory` (log-pile shed, ore-heap shed, stilted granary)
  so an unsourced civ model doesn't silently fall through to `BuildHut` (the
  exact bug Market's own comment already flags). Full `BuildingPlacer` wiring
  (3 new `BuildingKind` entries, hotkeys J/U/P — checked every existing
  `KeyCode` usage across the codebase first to avoid collisions) and `BuildMenu`
  wiring (3 new buttons duplicated from `DockButton` directly in the live scene
  via UnityMCP, positioned in the placement-button column's already-established
  36px-step layout, continuing past Market's slot — safe because the
  placement-button group and the TownCenter-training-button group never
  render simultaneously, confirmed by reading every existing button's
  `RectTransform.anchoredPosition` in the scene rather than assuming a
  contiguous 8-button layout, since Wall/Gate/Tower/Dock/Market's own Y values
  turned out non-contiguous with Barracks/Farm/House's). 8 new EditMode tests
  (`GathererDropOffTests.cs`, exercising `AcceptsDropOff` directly rather than
  driving a full `Gatherer` state machine), all 99 pass. Live-verified in Play
  mode via UnityMCP (bypassing the mission-select flow that normally gates
  gameplay-entity spawn, by spawning a Player `TownCenter` + test scenario
  directly): a Wood `ResourceNode` with a decoy `House` 3 units away (wrong
  resource type) and a `LumberCamp` 6 units away, worker's real position/state
  tracked across multiple gather-deposit cycles, and `Gatherer`'s private
  `_dropOff` field read via reflection at the moment of a completed trip
  confirmed it resolved to the `LumberCamp`, not the nearer `House` or the
  much farther (40 units) `TownCenter` — the aggregate Wood-stockpile number
  alone wasn't trustworthy evidence (jumped by inconsistent amounts between
  checks, likely a passive income tick unrelated to gathering), so the
  reflection check was the actual proof used. See `docs/SESSION_LOG.md`.
- [x] **Team-bonus / alliance economic stacking layer** (AoE-Parity Execution Plan
  Phase 3.2 — marked DECISION NEEDED after Phase 3.1, then user-confirmed "yes" when
  asked directly whether they wanted it at all). **Closed 2026-09-01.** Researched
  the existing diplomacy/civ-bonus infrastructure before writing any code: the
  alliance plumbing already existed and worked (`DiplomacyRegistry.AreAllied`,
  N-ary — Enemy/Enemy2 can ally each other, not just Player, and `AiController`
  already forms alliances dynamically), but nothing consumed an alliance for any
  economic/combat benefit. A `CivilizationDefinition.teamBonus` `StatModifier` field
  was already scaffolded (parsed from `civ_bonus_template.csv`'s `TeamBonus` column
  at CSV-import time) but never read anywhere at runtime — investigated why: most of
  the 5 team bonuses target Houses/fortifications/Markets, none of which map to a
  `UnitCategory` (no "Building" entry), so the generic regex-based
  `ParseNumericEffect` can't reliably represent them; `CsvToScriptableObject.cs`'s
  own comment already anticipated this needing "hand-written hooks... the same way
  Rajput's dismount-survival and Vijayanagara's fortification-HP bonus already are."
  Followed that existing bespoke-hook convention instead of forcing everything
  through the generic field: new `TeamBonus.cs`
  (`HasAlly(FactionId, CivilizationId)` + 5 named constants) wired into the 5
  existing per-civ-bonus hook sites the CSV already specified as each civ's
  "diluted" team version — Maurya (`BuildingPlacer.WoodMultiplierFor`, Player-only,
  matching that method's existing scope): allied Houses -25% Wood; Vijayanagara
  (`WallFactory`/`GateFactory`/`TowerFactory`'s existing `fortificationMultiplier`,
  applies to AI-built fortifications too since it lives in the shared factory, not
  the Player-only placer): allied Wall/Gate/Tower +15% HP; Rajput (`CavalryFactory`'s
  existing `uniqueTechDamageBonus`): allied Cavalry +1 flat damage; Maratha
  (`CavalryFactory`'s existing `agent.speed` multiplier line): allied Cavalry +10%
  move speed; Chola (`Market.EffectiveSellRate`/`EffectiveBuyRate`, already a live
  computed property, so this one needed no spawn-time baking unlike the other 4):
  allied Markets get a narrowed +/-5-point spread. Every bonus is unconditional
  (granted just by having an ally of that civ, not gated on the ally researching
  anything) and `HasAlly` deliberately excludes the checking faction itself even
  though `DiplomacyRegistry.AreAllied(a,a)` is true by design, so a civ's own bonus
  is never double-counted as its own team bonus. 5 new EditMode tests
  (`TeamBonusTests.cs`, pure `HasAlly` logic: no alliance, allied+matching civ,
  allied+wrong civ, self-exclusion despite `AreAllied` self-true, war overrides a
  prior alliance) plus one new case in `CivPassiveBonusTests.cs` for
  `WoodMultiplierFor`'s ally path — 105 EditMode tests total, all pass (up from 99).
  Live-verified all 5 bonuses in Play mode via UnityMCP with before/after-alliance
  A/B spawns of the same building/unit type (not just the pure-logic unit tests):
  Wall HP 250→287.5 (exact 1.15x), Cavalry damage 6→7 (exact +1 flat), Cavalry speed
  6.5→7.15 (exact 1.10x), Market sell rate 0.70→0.75 / buy rate 1.30→1.25 (exact,
  and confirmed live-dynamic — same `Market` instance re-read after
  `DiplomacyRegistry.SetAllied` with no respawn needed), and `WoodMultiplierFor`
  already covered directly by its own EditMode test. Also verified the negative/
  self-exclusion case live: a Vijayanagara-owned Wall spawned while allied with
  Player stayed at exactly 250 HP (not 287.5) — the ally-exclusion logic correctly
  never lets a civ's own building pick up its own team bonus. Plan approved via
  Plan Mode before implementation, per protocol. See `docs/SESSION_LOG.md`.
- [x] **Worker self-defense/cross-awareness** (from the worker mechanics audit,
  2026-08-29 — the last open item from that audit; implemented as AoE-Parity
  Execution Plan Phase 4.2, 2026-09-01). **Closed 2026-09-01.** `Gatherer` and
  `MeleeAttacker` had zero cross-awareness before this: a worker being attacked
  mid-gather never auto-interrupted into a defensive state, unlike AoE, where
  villagers either fight back or flee once hit. New `Attackable.OnDamaged`
  event (fires whenever a non-lethal hit lands, carrying the attacker's own
  `Attackable`) plumbed through every damage call site that previously called
  `TakeDamage` without an attacker reference — `MeleeAttacker`, `BoatAttacker`,
  `BuildingAttacker`, `WildBoar` — each now passes its own `Attackable` (lazily
  resolved via a `Self` property, not cached in `Awake`, matching the existing
  `GarrisonPoint`/`Repairable` sibling-component-ordering convention). New
  `CombatResponse` enum (Fight/Flee) and `WorkerCombatResponseDefaults` (a
  hand-written per-civ lookup, same bespoke convention as `TeamBonus`/
  `RajputDefianceHook` — not representable as a `passiveBonuses`
  `StatModifier`, a categorical behavior choice rather than a numeric stat):
  every civ defaults to Fight (matching the pre-4.2 capability every Worker
  already had via its own `MeleeAttacker`, just now auto-triggered instead of
  requiring an explicit command) except Maratha, whose guerrilla hit-and-run
  identity (Ganimi Kava — already reflected in its Cavalry speed bonus and
  team bonus) extends here: its Workers flee instead. `Gatherer.HandleDamaged`
  subscribes to `OnDamaged` lazily in `Update` (not `Awake` — `WorkerFactory`
  adds `Gatherer` before `Attackable`), only interrupts while actively
  seeking/working a node (`MovingToNode`/`Gathering` — a load already being
  carried home in `MovingToDropOff` finishes its trip rather than losing it,
  the same carve-out `CancelGather` already uses), and either turns the
  worker's own `MeleeAttacker` on the attacker (Fight) or issues a move order
  to a point away from the attacker via a new pure/testable
  `ComputeFleeDestination` helper (Flee). Wired at spawn time in
  `WorkerFactory` from `WorkerCombatResponseDefaults.For(civilization)`.
  **Found and fixed 3 real latent bugs during this session's own
  verification pass, none related to the feature's own logic**: (1) an
  ambiguous `DamageType` reference at `WildBoar.cs:118` — a second,
  unrelated global-namespace `DamageType` enum already existed in
  `UnitDefinition.cs`, and C# resolves an unqualified name against the
  enclosing global namespace *before* consulting `using` directives, so the
  newly-added explicit `DamageType.Melee` argument there silently bound to
  the wrong enum and failed to compile — fixed by fully qualifying it as
  `KingdomsOfBharat.Combat.DamageType.Melee` (this had been latent since
  `UnitDefinition.cs` was written; nothing had ever passed an explicit
  `DamageType` from inside `KingdomsOfBharat.Wildlife` before). (2)/(3)
  `UnitMover` and `MeleeAttacker` both cached their own sibling components
  (`NavMeshAgent`, `UnitMover` respectively) in `Awake` rather than lazily —
  the exact same "Awake doesn't run synchronously right after AddComponent"
  gotcha CLAUDE.md already documents for `ConstructionSite`/`Repairable`, but
  never previously hit because no EditMode test had exercised `Gatherer`'s
  `GatherFrom` (which needs a live `UnitMover`) end-to-end before this
  feature's own test needed to. Both converted to the same lazy-property
  pattern already used elsewhere in this diff (`MeleeAttacker.Self`,
  `Gatherer.Mover`). 12 new EditMode tests (`GathererCombatResponseTests.cs`:
  Fight/Flee interrupt-and-react behavior, the idle/null-attacker no-op
  cases, `ComputeFleeDestination`'s pure direction math, and the 5 per-civ
  defaults), 117 EditMode tests total, all pass (up from 105 — the +12 gap
  vs. Phase 3.2's own tests is fully accounted for). Live-verified both
  responses in Play mode via UnityMCP through the real production event path
  (`Attackable.TakeDamage(..., attacker)` → `OnDamaged` → `Gatherer.HandleDamaged`,
  not the internal test shortcut): a live Maurya Worker mid-`MovingToNode`,
  hit by a real `Attackable.TakeDamage` call, immediately stopped gathering
  and turned its `MeleeAttacker` on the attacker, then closed a real ~4-unit
  NavMesh-pathed distance down to 0.21 units over the following ticks; a live
  Maratha Worker under the identical setup instead moved away, real tracked
  distance from the attacker increasing from 19.9 to 25.9 units with its
  `MeleeAttacker` never engaging. See `docs/SESSION_LOG.md`.
- [x] **Repair system** (from the worker mechanics audit, 2026-08-29) — completely
  missing; no `Repair` anywhere in the codebase. AoE reference: right-click a
  damaged building/ship/siege unit with a worker selected to repair it, at a
  resource cost proportional to HP restored.
  **Closed 2026-09-01.** New `Repairable` (`Assets/Scripts/Combat/Repairable.cs`,
  target-side) and `Repairer` (`Assets/Scripts/Buildings/Repairer.cs`, worker-side)
  mirror `Builder`/`ConstructionSite`'s exact shape, and reuse
  `ConstructionSite.SpeedMultiplier` directly for multi-repairer diminishing
  returns instead of a second formula. Wired onto all 9 building kinds (incl.
  TownCenter), Siege, and both naval units (Fishing Boat/War Galley) - 12
  factories, one `AddComponent<Repairable>()` line each. `SelectionManager`
  gained a `hitRepairable` branch (same friendly-only chain-of-exclusivity
  pattern as `hitGarrison`/`hitFarm`), and every other order branch now cancels
  an in-progress repair. **Known, disclosed simplification**: this project
  doesn't retain each building/unit instance's original build/train cost at
  runtime (those consts are spent once in `BuildingPlacer`/`Barracks`/`Dock`,
  never stored on the spawned object) - deriving an exact "half of original
  cost" per instance would mean threading cost data through every factory's
  `Place`/`Spawn` signature. `Repairable` instead charges a flat Wood-per-HP
  rate keyed off `Attackable.Class` (Building cheapest at 0.4/HP, Naval 0.6/HP,
  Siege priciest at 1.2/HP) - an approximation, not an exact per-instance
  figure, refinable later if exactness is wanted. 8 new EditMode tests
  (`RepairableTests.cs`, via an `internal Tick(deltaTime)` exposed the same way
  `ConstructionSite.EnsureInitialized` is, since EditMode tests can't rely on
  `Update()`/`Time.deltaTime` ticking); all 75 tests pass. Live-verified in
  Play mode via UnityMCP: real HP restoration and Wood deduction at the exact
  documented rate, a stall-then-resume across an insufficient-funds gap with no
  partial/lost spend, auto-stop on reaching full health, `UnitStatus` showing
  "Repairing", and `Repairable` correctly present with the right `UnitClass` on
  a live-spawned Siege unit and War Galley. See `docs/SESSION_LOG.md`.
- [x] **General garrisoning system, AoE IV style** (from the worker mechanics
  audit, 2026-08-29) — closed 2026-09-01. The only existing `Garrison`
  component was narrowly scoped to the Maratha Durg Garrison unique unit
  (single-slot, Wall/Tower only, siege-immunity toggle only) — generalized in
  place into `GarrisonPoint` (`Assets/Scripts/Buildings/GarrisonPoint.cs`,
  renamed from `Garrison.cs`): pooled capacity, any eligible friendly land
  unit (via the new `GarrisonSeeker`, renamed/generalized from
  `DurgGarrisonWorker.cs`, added to Worker/Soldier/Archer/Cavalry/Spearman and
  every already-spawnable land unique unit except Siege), garrisoned units
  deactivated (untargetable/off the field), and ejection via `UngarrisonAll`
  (the existing BuildMenu Ungarrison button, generalized to any building with
  a `GarrisonPoint`) which repositions every occupant to the building's
  nearest walkable edge (`BuildingFootprintTag.GetNearestApproachPoint`) and
  resumes whatever the building's `RallyPoint` currently points at, if it has
  one. Scaling defensive firepower (the AoE IV "murder holes" mechanic - more
  garrisoned units = more simultaneous shots at potentially different
  targets, not a flat damage multiplier) needed generalizing the Tower-only
  `TowerAttacker` into `BuildingAttacker`
  (`Assets/Scripts/Combat/BuildingAttacker.cs`) so **TownCenter could get a
  baseline `Attacker` for the first time** (it had none before this item) -
  TownCenter is now the Keep/TC-equivalent (capacity 8, up to 5 simultaneous
  shots) and Tower the Outpost-equivalent (capacity 4, up to 4 simultaneous
  shots). Explicit design decisions made rather than assumed: Wall keeps its
  exact pre-existing narrow behavior via a `durgOnly` flag on the same
  `GarrisonPoint` class (capacity 1, only the Maratha Durg Garrison unique
  unit's `GrantsSiegeImmunity` flag satisfies it - a thin 0.4-unit Wall
  segment has no real interior for a general population), Gate stays
  ungarrisonable (unchanged), Siege units are excluded from garrisoning
  entirely (AoE IV siege engines don't garrison), and siege-immunity stays a
  Maratha-Durg-specific bonus layered on the same mechanism rather than
  becoming "any full building is siege-immune." A real latent bug was found
  and fixed mid-implementation, not just live-verified around: `GarrisonSeeker`
  originally moved to and range-checked against the target building's raw
  `transform.position`, which for TownCenter's 6-tile footprint sits deep
  inside its own carved NavMeshObstacle (edge-to-center distance up to 3,
  wider than the 2.5 interactionRange itself) - a unit ordered to garrison a
  TownCenter could physically never get close enough to trigger entry. Fixed
  with the same `GetNearestApproachPoint`-based approach-point pattern
  Gatherer's drop-off already established for the identical class of bug
  (computed once per order, not every frame, matching that precedent) - this
  shipped broken in this session's own first live-verification pass, caught
  by the session's own Play-mode check, not by the EditMode tests (which
  drive `GarrisonPoint`/`BuildingAttacker` directly and never exercised
  `GarrisonSeeker`'s real pathing). 16 new EditMode tests
  (`GarrisonPointTests.cs`, `BuildingAttackerTests.cs`, plus 3 existing
  Garrison tests in `UniqueUnitsTests.cs` updated to the new API), all 85
  pass - `BuildingAttackerTests` needed `LogAssert.ignoreFailingMessages`
  around any `Tick()` call that deals damage, since `Attackable.TakeDamage`'s
  VFX burst logs an Editor-only "Destroy may not be called from edit mode"
  once its particle system's stop-action fires outside Play mode (same
  "expected, not a regression" situation `BuildingModelFactoryTests` already
  documents for its own case) - no precedent existed for testing
  `TakeDamage` in EditMode before this. Live-verified in Play mode via
  UnityMCP: spawned a real TownCenter/Tower/Wall, garrisoned real
  Worker/Soldier/Durg-unit instances, confirmed capacity enforcement (Count
  hit exactly 8/8 on TownCenter, a 9th unit rejected and left active
  outside), confirmed shot-count scaling precisely against fresh 5000-HP
  dummy targets (Tower: exactly 1 target hit ungarrisoned, exactly 4 distinct
  targets hit once garrisoned with 3 occupants - 1 base + 3, matching
  maxBonusShots), confirmed `UngarrisonAll` repositions every occupant
  outside the building (all measured 3.6-5.2 units from TownCenter's center,
  none stuck inside), and confirmed the Wall `durgOnly` gate rejects a
  regular Soldier (stays active, count 0) while accepting the real Maratha
  Durg Garrison unit and flipping `Attackable.SiegeImmune` true/false exactly
  as before this generalization. See `docs/SESSION_LOG.md`.

- [x] **4 unique units for Maurya/Maratha have no live factory** — CSV data exists
  (`maurya_war_elephant`, `pillar_edict_scholar`, `maratha_mavla_raider`,
  `maratha_durg_garrison`), but they're not spawnable yet. This is the main reason
  Maurya/Maratha, while selectable, aren't yet on par with the original 3 civs.
  **Fully closed** — backend was already complete (all 4 spawnable via
  `Barracks.RequestTrainUniqueUnit(int slot)`, 2-slot-per-civ system, Pillar Edict
  Scholar/Durg Garrison have real mechanics beyond stats). **Visual closure landed
  2026-08-27**: real rigged models for all 4, replacing the shared Human Dummy
  fallback. The 3 humanoids (Scholar/Raider/Garrison) got Meshy-sourced meshes bound
  onto the existing shared rig (reuses its Idle/Walk/Attack clips as-is); the War
  Elephant got its Meshy-sourced mesh bound onto a real third-party elephant
  skeleton+animation set (CC-BY, see `Assets/Resources/UniqueUnits/CREDITS.md`). The
  user drove the actual rigging via Blender command-line Python scripting
  (`blender --background --python`), not manual Editor work. All 4 live-tested
  spawning correctly in Play mode. See `docs/SESSION_LOG.md` for the full
  methodology, several real Unity/Blender interop bugs hit and fixed along the way,
  and one known first-pass limitation (the elephant Die clip strains visibly in its
  more extreme back-half poses under automatic-weight skinning).
- [x] **Narrower per-civ passive bonuses aren't live** — Rajput's cavalry-only gold
  discount, Maurya/Maratha's specific move-speed bonuses, etc. exist in
  `CivilizationDefinition.passiveBonuses` but nothing reads them; only the legacy
  5-field `CivilizationProfile` subset is wired. Closing this is what makes the 2 new
  civs (and Chola/Vijayanagara/Rajput's fuller bonus sets) actually function as
  designed rather than just as flavor text.
  **Closed** — audited every civ's generated `passiveBonuses` against what
  `CivilizationProfile` actually read; the real data-backed gap was 4 entries across
  Rajput/Maurya/Maratha (Chola/Vijayanagara had none). Added
  `CivilizationProfile.FindCategoryMultiplier` and wired all 4
  (`CavalryFactory`/`WorkerFactory` move speed, `Barracks` Cavalry gold cost, `Dock`
  Naval train time). Also wired the 6 bonuses that aren't `passiveBonuses` data at all
  (non-representable per `CsvToScriptableObject.cs`'s own comment) as hand-written
  hooks: Maurya's free Houses + Classical-Age start, Vijayanagara's fortification
  Stone discount + Tower range, Rajput's Cavalry dismount-survival, Maratha's
  permanent scouted-position memory (building permanence + unit ghost markers in
  `FogOfWarManager`). See `docs/SESSION_LOG.md` for the full audit and verification.
- [x] **Balance pass (item 43) resumed** — `playtest_log.csv` now has 15 real
  logged 1v1 forced-melee fights (up from the 1 existing row): every roster gap the
  roadmap flagged as unaudited (Siege vs Infantry/Archer/Cavalry, Spearman vs
  Infantry/Spearman, War Galley vs Infantry/Spearman), all 4 new unique units'
  factories live-confirmed fighting correctly for the first time, the Durg Garrison
  siege-immunity mechanic live-measured exactly (39/hit at 3x → 9/hit at 1x, not
  approximate), and a dedicated civ/age/upgrade/unique-tech stacking audit. Stacking
  audit result: HP/armor/damage combine exactly as coded (multiplicative HP/train-time,
  additive armor/damage bonuses, counter-matrix multiplier applied once) with no
  double-counting found anywhere across 6 factories checked - a large enough tech gap
  (Imperial+maxed-upgrades+unique-tech vs Ancient+unupgraded) can overwhelm even a 2x
  hard counter, which is intended AoE-style tech-tree behavior, not a bug. No code
  changes were needed. Training cost-vs-power ratios and further sustained live
  playtesting remain open for a future balance session - this pass closed the
  specific "start logging + audit stacking" scope of item 5, not the whole ongoing
  balance-pass item. See `docs/SESSION_LOG.md` for full methodology and results.
- [x] **Naval balance is one evidenced fix, not a full pass** — only Naval→Archer was
  tuned; every other naval matchup is unaudited flat 1x.
  **Audited for this session's scope** — 4 new live 1v1 forced-fights logged to
  `Assets/Design/playtest_log.csv` (War Galley vs Archer/Cavalry/Siege/War Galley
  mirror), closing the gap the roadmap flagged (Naval→Archer had never actually been
  live-tested/logged despite being "evidenced"; Naval vs Cavalry/Siege/Naval were
  fully unaudited). Results: vs Archer confirmed the existing 0.5x fix still leaves a
  clear win (21/45 HP), vs Cavalry a real win with real damage taken (9/45 HP, no fix
  needed, same bar as the existing Soldier/Spearman rows), mirror match symmetric with
  no asymmetry bug. vs Siege was decisive in Siege's favor (Galley destroyed, Siege at
  34/50) but judged working-as-designed rather than a bug: Siege has no unit-vs-unit
  penalty anywhere in `CombatBonus`, and Naval's actual counterplay is its range/speed
  edge over Siege (which can't enter water) — not something a static forced-melee test
  can capture. No `CombatBonus` changes made. Two adjacent findings flagged but
  deliberately not fixed this session (candidates for a future session): Naval
  factories never call `ClassArmorBonus`/`ClassDamageBonus` (every land factory does),
  and `BoatAttacker`'s attack interval (1.5s) vs `MeleeAttacker`'s (1.0s) is a real
  structural asymmetry independent of `CombatBonus`. See `docs/SESSION_LOG.md`.
- [x] **UI skin (item 47's second half)** — art delivered, alpha-fixed, and wired in
  as of 2026-08-28 (see the full entry under Section 4.3). One content gap remains,
  tracked as its own Section 4.3 item: no build-placement cursor asset. (The Maurya
  crest off-palette gap closed 2026-08-31 — see the matching Section 4.3 item.)
- [x] **2 Crusader Knight body models sourced but not wired in** — rig-compatibility
  with `WeaponAttachment`/`AnimationDriver` was never verified; swapping the shared
  human rig risks breaking every unit at once if done blind. Needs a dedicated
  verification step before it's safe to use.
  **Verified — both models are rig-compatible, confirmed live, not assumed.** The 2
  models are TemplarKnight/HospitalierKnight (`Assets/importedmodels/Item47/`),
  already-skinned Mixamo-rigged glTF imports with zero embedded animation and no
  native Humanoid Avatar (glTFast produces a plain hierarchy, unlike FBX
  `ModelImporter`). Built a valid Humanoid Avatar for each via
  `AvatarBuilder.BuildHumanAvatar` + a hand-authored `HumanDescription` (pure Editor
  scripting, no Blender needed) — both `isValid && isHuman`. Live-tested by driving
  the shared dummy's Walk clip through `AnimationDriver`'s exact Playables pipeline:
  confirmed a smooth ~40° leg-bone rotation swing across the cycle on both models (not
  frozen/exploded), and confirmed `WeaponAttachment.AttachToBone` resolves bones and
  attaches a real prop successfully on both. **2 real caveats for whoever does the
  actual swap-in later** (not fixed here — verification only): both models have a
  ~247-248x `Animator.humanScale` anomaly (a scale-normalization fix needed, same
  class of issue `WeaponAttachment` already anticipates for arbitrary import scale),
  and each model's sword/shield/staff meshes are static props parented to the scene
  root rather than a hand bone (confirmed via hierarchy inspection — won't follow the
  animated hand, need re-parenting or replacing with `WeaponAttachment`, mirroring the
  precedent set for the 3 humanoid unique units). No code changes landed — pure
  Editor-runtime verification, cleaned up after. See `docs/SESSION_LOG.md`.
  **Superseded 2026-09-01 by the "Per-civ soldier visual differentiation" item
  below** — the actual swap-in decision, deliberately deferred rather than done
  blind, lives there now (kept as its own explicitly-scoped future item, not
  folded into this closed verification entry).
- [ ] **Per-civ soldier visual differentiation** (new initiative, 2026-09-01,
  supersedes the Crusader Knight item above and closes the Section 5 "Base human
  body decision" item below) — user decision: upgrade the shared body once via
  civ-colored texture + minor gear/prop variants, not 5 separate bodies.
  **Tint-gap sub-item: done 2026-09-01.** `HumanModelFactory.PaletteNameFor()`
  only wired 3 of 5 civs (Chola→Red, Vijayanagara→Yellow, Rajput→Blue); Maurya
  and Maratha hit `default: return null` and spawned completely untinted (plain
  white) — a real, previously-unflagged bug, not by design. Fixed by
  pixel-sampling the shared trim-sheet texture
  (`Human Character Dummy/Textures/HumanCharacterDummy_ColorPalette.png`, 16
  rows) against each civ's canon crest color (`docs/UI_ART_BRIEF.md`): Maratha's
  forest green (#267333) is a near-exact match to the *existing* `Green` row (no
  new asset needed, just remapped); Maurya's warm gray/stone (#807866) has no
  close match anywhere in the sheet, so a new `HumanDummy_Gray.mat` was wired to
  an unused neutral-gray row (offset y=0.9375, RGB≈75,75,75) — a known
  compromise (neutral, not warm-stone), swappable later for a proper warm-gray
  texture with zero code changes if the user sources one. Live-verified in Play
  mode (all 5 civs screenshotted side-by-side, each reads as a distinct tint);
  all 67 EditMode tests still pass (no new test — pure lookup-table data change,
  same as the original 3-civ mapping was never separately unit-tested). See
  `docs/SESSION_LOG.md`.
  **Base-body sub-decision: made 2026-09-01, deliberately, not left silent** —
  user chose to keep the current Human Character Dummy body this session rather
  than swap to the verified-compatible Crusader Knight model. The swap itself
  (fixing the ~247x `Animator.humanScale` anomaly + re-parenting
  sword/shield/staff props to a hand bone via `WeaponAttachment`, both already
  scoped in the entry above) remains a real, doable future item — just not
  bundled into this session.
  **Gear/prop variant sub-item: scoped, not implemented** — real new asset need,
  5 civs × helmet/shield/weapon style. `Assets/Resources/Weapons/` currently has
  exactly one generic weapon per unit type (Sword/Bow/Spear/Kanabo), no civ
  variants. Spec for whoever sources this: reuse the proven
  `WeaponAttachment.AttachToBone` pipeline (`SoldierFactory.cs`'s sword
  attachment is the reference call site) — per civ, a distinct hand-held weapon
  mesh/texture (civ-appropriate style, not a recolor of the same mesh) is the
  highest-value single variant; a helmet/headgear prop attached similarly is the
  second priority; a shield emblem (a flat civ-crest decal reusing the crest art
  already in `docs/UI_ART_BRIEF.md`) is the lowest-effort/lowest-impact third.
  Not guessed at with low-confidence packs per the cursor-pack precedent (see
  Section 4.4) — needs the user to source or commission per-civ.

- [x] **AoE-parity Phase 2 — Combat calibration audit** (`AOE_PARITY_EXECUTION_PLAN.md`
  Phase 2, 2026-09-01) — three-part audit of `CombatBonus`/`CounterMatrix` against
  AoE4 reference design; logged as one batch per user instruction rather than three
  disconnected entries.
  - [x] 2.1 — Audit `CombatBonus` multiplier scale against AoE4's reference values.
    **Closed.** Confirmed `CombatBonus.Multiplier()` is the real damage-resolution
    path (`CounterMatrix` is inert, loaded but never read by damage code — the
    documented separation is real, not stale). Infantry→Archer/Archer→Cavalry/
    Cavalry→Infantry all sat at 1.5x against AoE4's 2x-3x hard-counter band; a
    hand-computed 1v1 duel audit (real armor-subtraction formula, real base stats)
    found Infantry→Archer and Cavalry→Infantry already decisive at 1.5x (loser
    retains ~20-30% HP) — left unchanged, raising them only shaves a hit off an
    already-fast fight. Archer→Cavalry was the real gap: 1.5x won with only 7% Archer
    HP left (a coin-flip in practice) — **raised to 2.0x** (33% HP left, clear win;
    2.5x modeled and rejected as unneeded), user-approved after seeing the specific
    numbers, not assumed from "AoE4 is 2-3x." See `CombatBonus.cs`'s own comment and
    `docs/SESSION_LOG.md` for the full rationale, EditMode test
    (`CombatBonusTests.cs`), and a live equal-cost squad fight (8 Archers vs. 5
    Cavalry, both 600 resources) logged to `Assets/Design/playtest_log.csv`.
  - [ ] 2.2 — Evaluate adding a soft-counter mechanic. **Audited, not implemented.**
    Confirmed no soft-counter mechanic exists anywhere in this codebase — every
    relationship in `CombatBonus`/`CounterMatrix` is pure damage-multiplier, no
    kiting AI, no armor-class mitigation independent of the multiplier table. Real,
    undesigned depth gap; scoped as its own future item per the execution plan's own
    instruction not to fold an audit into an ad hoc new-mechanic implementation.
  - [ ] 2.3 — Verify formations actually do something against Siege. **Audited, not
    implemented.** Confirmed Siege has no splash/area damage anywhere (`SiegeFactory`
    uses a plain single-target `MeleeAttacker`, same as every other unit) and
    `FormationController`'s 5 shapes rearrange units geometrically with nothing in
    combat resolution reading formation shape/spacing — so the 5 shipped formation
    types are cosmetic against Siege specifically, a "shipped but not delivering the
    AoE experience" gap even though the feature itself reads as closed. Flagged as a
    new item (add splash/area damage to the Siege class) per the plan's own
    instruction, not folded into this audit.

### Lower priority — real gaps, but not urgent

- [ ] **Music is entirely absent** — item 42's audio pass was explicitly SFX-only.
- [ ] **No dedicated new-player tutorial** — the 3 campaign missions are real content
  but aren't a "learn to play" onboarding flow; a genuinely new player still has only
  the README to go on.
- [ ] **No profiling/optimization pass at real scale** — maps were scaled up 2.5x
  (item, Phase 5) and the map-unaware-systems bugs were fixed, but nobody has profiled
  actual frame cost with a realistic large-match unit count on the new map sizes.
- [ ] **Store/marketing assets** — not started, only relevant if a public release is
  a real goal; worth deciding intent explicitly rather than leaving implicit.
- [ ] **README drift** — the roadmap doc's own maintenance rule says to keep README's
  "Build milestones" section in sync; given the sheer volume of work past milestone
  26, confirm that section actually reflects current reality before it's read by
  anyone (including a future Claude Code session bootstrapping from it).

---

## 2. Architectural Notes Worth Preserving (not bugs — deliberate decisions)

These aren't open items, but they're easy for a future session to "fix" by mistake if
they're not written down clearly:

- **`CombatBonus` and `CounterMatrix` are deliberately NOT merged.** `CombatBonus`
  holds the playtested, balance-verified `UnitClass` pairings (including the
  asymmetric Cavalry-vs-Archer fix); `CounterMatrix` is real CSV-generated data used
  only for Spearman, the one genuinely new unit category it introduced. Don't
  "clean this up" into one system without re-verifying every existing balance fix.
- **`AgeProfile`/`UpgradeProgress` are intentionally still hardcoded**, not migrated
  to the CSV pipeline — they're pure formula tuning (tiers/multipliers) with no
  natural per-row CSV shape, not an oversight.
- **`UniqueTechDefinition`'s bespoke per-civ effect values stay hardcoded** — the CSV
  only carries flavor-text descriptions for unique techs, not structured effect data.
- **The project has real `.asmdef` assemblies now** (`KingdomsOfBharat.Runtime`/
  `.Editor`/`.Tests`, added when the training/trade UI batch needed its first tests —
  see CLAUDE.md's gotchas section for the full layout). Don't "flatten" this back to
  the implicit default assembly — it's what makes `Assets/Tests` possible at all.

---

## 3. Process Note (worth reading before the next session)

The source log shows several real bugs caused specifically by **multi-session
concurrency and trusting peer-relayed claims instead of direct user confirmation**:
a shared-git-index race during a simultaneous commit, a duplicated script folder that
broke compilation project-wide, two independently-declared `FormationType` enums
colliding by name, and at least one stale "still missing" gap note written by a
session that hadn't cross-checked the actual file tree. The log also shows the
project already self-correcting toward better discipline — later entries consistently
say "confirmed directly with the user, not acting on a peer relay alone" before major
decisions.

Given this project now has a strict single-session, single-item CLAUDE.md protocol in
place, the strongest recommendation here is procedural, not technical: **keep running
one Claude Code session at a time against this repo going forward**, and treat any
claim about "what another session did" as unverified until you or the current session
confirms it against the actual repo state — exactly the pattern the log's later
entries already converged on independently.

---

## 4. Art Direction & Asset Requirements

The ad hoc "grab whatever free CC-BY pack fits" approach got the project real 3D
content fast, but it has a ceiling — it's why Wall/Gate needed three sourcing attempts,
why several units still share one generic body, and why nothing so far has been built
*to* a standard rather than *found near* one. Going forward, assets are specified here
to a real target, and sourced or created (by you, on other platforms/commissions,
outside the coding session) against that spec — not scavenged and hoped to fit.

### 4.1 Target Visual Standard
**Mid-poly realistic PBR**, in line with AoE II:DE / AoE IV's visual tier — this
matches what's already landed best (the AoE2-faithful TownCenter/Barracks
recreations, the PBR castle pack) and the original ask to match AoE's graphical
level, not a stylized low-poly look. Concretely:

| Parameter | Standard |
|---|---|
| Unit poly count | 4,000–8,000 tris (readable detail at RTS zoom, not hero-model density) |
| Building poly count | 8,000–20,000 tris depending on tier (House low end, Wonder/TownCenter high end) |
| Unit texture resolution | 1024×1024 minimum, PBR (albedo/normal/metallic-roughness) |
| Building texture resolution | 2048×2048–4096×4096, PBR |
| Material workflow | Metallic/roughness PBR only — reject any pack still on a Built-in-RP Standard shader without a clean URP conversion path |
| Rig | One shared humanoid rig for all human units (already established — keep it), but the rig itself and its base body should be re-evaluated against this standard (see 4.2) |
| Style reference | Each civ's buildings should read as a distinct real-world architectural tradition — Chola (Dravidian temple architecture, gopuram-style towers), Vijayanagara (Hampi's granite/Deccan style), Rajput (fort/haveli architecture, chhatris), Maurya (Mauryan pillar/stupa motifs), Maratha (Deccan hill-fort style) — not just a palette swap on one shared building kit |

### 4.2 Existing Assets — Audit Against the Standard
Not everything already wired in necessarily clears this bar. Worth a deliberate
pass, not silent replacement:
- **TownCenter, Barracks, Tower, Market, Tavern, Farm, Wall, Gate, ships, Dock**:
  sourced as real detailed models, plausibly close to standard already — verify each
  against the poly/texture targets above before assuming they pass.
  **Building style differentiation is not close to standard yet**, though — the
  current 5 civs at least share one visual building set; per-civ architectural
  identity (per the 4.1 reference list) hasn't been built.
- **Shared "Human Character Dummy" base body**: this is the one most likely to fall
  short of "AAA representation" — it was picked for rig availability, not fidelity.
  Worth a direct decision: keep it and only upgrade weapon/mount attachments, or
  replace the base body wholesale (this is exactly what the 2 unused Crusader Knight
  models were sourced for — if a body swap happens, do it once, deliberately, with a
  full rig-compatibility check, not as another ad hoc addition).
- **Environment props (trees, mines, quarries, farmland)**: functional multi-variant
  coverage exists; not yet audited against the 4.1 poly/texture targets.

### 4.3 Asset Requirements — What's Actually Needed Next
Specced to the 4.1 standard, so whatever you arrange or create has a concrete target
rather than "something reasonable":

**Civ-specific building models — priority order and count**: 9 spawnable building
types (TownCenter, Barracks, Tower, Market, Farm, House, Wall, Gate, Dock) × 5 civs =
45 models for full coverage.

- [x] **Chola (9/9) — done 2026-08-28.** All 9 buildings wired via new
  `Assets/Editor/MeshyBuildingImporter.cs`: real PBR materials (URP Lit,
  metallic+roughness packed into one texture via an out-of-Unity Pillow script after
  an in-Editor `Texture2D.GetPixels` attempt crashed the Unity process), a Tower
  orientation fix (`BuildingModelFactory`'s shared `ImportRotationCorrections["Tower"]`
  doesn't account for civ, so Chola's Tower needed a counter-rotation baked into the
  wrapper prefab), and — **corrected same day** — a scale hierarchy that's actually
  proportional to human units and to each other (initial per-building scale
  correction, 1.18×-3.16×/TownCenter 93×, checked each building against its old
  shared sibling in isolation only, which let Barracks/House come out shorter than a
  worker unit; recalibrated against a human-height anchor and the whole family
  together — see `docs/SESSION_LOG.md`'s "Chola building scale hierarchy corrected"
  entry, methodology saved to memory for reuse on the other 4 civs). Live-verified
  spawning through the real factory path, 2 new EditMode tests added.
  Vijayanagara/Rajput/Maurya/Maratha (36 models) remain unstarted — see
  `docs/SESSION_LOG.md` for full methodology, the crash root-cause, and the
  AABB-can't-detect-upside-down lesson for future rotation fixes.

- [x] **Vijayanagara (9/9) — done 2026-08-31.** All 9 buildings wired via the same
  `MeshyBuildingImporter.cs` pipeline Chola established — no code changes needed,
  reused as-is. Identification: 8/9 resolved confidently from Meshy-internal
  filenames (`Lotus_Stone_Bazaar`→Market, `Temple_Farmstead`→Farm,
  `Elephant_Fortress_Gate`→Gate, `Ancient_Stone_Rampart`→Wall,
  `Elephant_Dock_Temple`→Dock, `Elephant_Watchtower`→Tower, plus the two
  human-named TownCenter/Barracks folders); the 9th (a generic
  `Ancient_Stone_Temple`-named folder) was genuinely ambiguous and resolved to
  House by live geometry inspection + elimination (small single-story form,
  matching the concept art). Tower needed the same counter-rotation workaround as
  Chola's (`BuildingModelFactory`'s shared `ImportRotationCorrections["Tower"]` is
  civ-blind) — but this asset's correct fix was a **Y-axis** bake
  (`Quaternion.Euler(0,90,0)`), not Chola's Z-axis one; found by testing all 6
  cardinal-axis candidates' bounds, picking the Y-tallest result, then confirming
  visually it read as an upright tower (Y-axis spin can't itself cause an
  upside-down result, unlike X/Z). Scale: all 9 raw imports came out
  Meshy-normalized to ~equal height (~1.90, essentially matching the worker's own
  measured height) except Wall/Gate (correctly flatter, ~0.36-0.49, since their
  long axis is horizontal) — so target heights were derived by applying Chola's
  established *ratio* hierarchy (Tower ~4.1x/Market ~2.55x/Barracks
  ~2.26x/Dock ~1.96x/Wall≈Gate ~1.37x/House ~1.32x/Farm ~1.08x, TownCenter ~5.75x)
  against this session's own measured worker height (1.903), not copied absolute
  values. Live-verified in both Editor mode and real Play mode via
  `BuildingModelFactory.Spawn`, screenshotted (worker dwarfed by every building,
  Tower upright, Wall vs. Gate visually distinct — Wall a long continuous run,
  Gate a complex structure with a ramp/archway). All 67 EditMode tests still pass
  (no new tests — pure asset-pipeline work, no new logic). Rajput/Maurya/Maratha
  (27 models) remain unstarted. See `docs/SESSION_LOG.md`.
  **Rotation fix, same day**: user reported 6 buildings misoriented
  (Dock/Gate/Wall/Farm/House/Market); re-checking the remaining 3 found the
  real scope was 9/9 — TownCenter and Barracks had the identical Z-up-lying-flat
  bug, and Tower (initially cleared by a same-methodology check) was still
  upside down, confirmed by the user from an in-game screenshot after the
  first pass. 8 of the 9 got `Quaternion.Euler(-90,0,0)` on the prefab's nested
  `<Name>_model` child; Tower's fix was a 180° flip layered onto its existing
  Y-axis import bake (`Euler(180,90,0)`, replacing `Euler(0,90,0)`), since
  verifying Tower correctly requires reproducing the full runtime spawn stack
  (the factory's own rotation stomp plus the child's baked correction), not
  just the saved prefab in isolation. All 9 individually verified against
  reference concept art via 6-angle screenshots, not blanket-applied. All 9
  Chola buildings re-checked as a precaution — no regression found. See
  `docs/SESSION_LOG.md`'s "Vijayanagara building-model rotation fix" entry.

- [x] **Rajput (9/9) — done 2026-08-31.** Wired via the same
  `MeshyBuildingImporter.cs` pipeline, no code changes. Identification: 6/9
  resolved confidently from explicit Meshy-internal filenames
  (`Rose_Palace_Farmstead`→Farm, `Harbor_Palace_of_Rose`→Dock,
  `Rose_Palace_Bazaar`→Market, `Rose_Citadel_Gate`→Gate, plus the two
  human-named `rajput towncenter`/`rajput barrack` folders); the raw delivery
  only had 8 folders for 9 building types (no Tower/Wall candidate), flagged to
  the user rather than force-fit — user then added a `tower/` folder and
  identified the two remaining generic-named folders as Wall
  (`Red_Sandstone_Citadel`) and House (`Rose_Sandstone_Courty`). Orientation:
  6 of 9 (Tower, Farm, Dock, Market, House, and TownCenter — see below) needed
  `Quaternion.Euler(-90,0,0)` on the nested model child; Barracks, Wall, and
  Gate were already correctly oriented at identity. Tower needed the usual
  civ-blind `ImportRotationCorrections["Tower"]` counter-rotation treatment —
  this asset's fix was `Quaternion.Euler(0,90,0)`, found via the same
  all-6-cardinal-candidates-through-the-full-spawn-stack test as
  Vijayanagara's. Scale: targets derived from this session's own measured
  worker height (1.9027) against Chola/Vijayanagara's established ratio
  hierarchy, landing on (in world units) TownCenter 10.94/Tower
  7.84/Market 4.85/Barracks 4.30/Dock 3.73/Wall≈Gate 2.61/House 2.51/Farm
  2.06 — all confirmed to 1-2 thousandths of the target via live
  `BuildingModelFactory.Spawn` bounds in both Editor and real Play mode.
  **TownCenter's own raw folder turned out to be a duplicate of the Tower
  mesh** (338 bytes differ out of 80MB — metadata only), a real content gap
  flagged to the user, who supplied a genuine second TownCenter export
  (`towncenter/`, a different ~94.6MB FBX, 80M+ bytes different from Tower's —
  confirmed as real distinct geometry before wiring). **That asset's
  orientation fix needed real rework after an initial wrong pass**: the
  "pick whichever cardinal rotation gives Y-tallest bounds" heuristic that
  worked for every other asset in this civ (and Chola/Vijayanagara's Towers)
  produced a confidently-wrong result here, because this building is a wide,
  sprawling fort complex whose *correct* upright orientation is not its
  tallest possible bounding-box orientation (unlike a narrow Tower, where
  Y-tallest is a reliable signal) — two visually-plausible-looking wrong
  rotations were live-verified, screenshotted, and shipped as "confirmed"
  before the user caught the tilt from an in-game view both times. What
  actually resolved it: reading the raw mesh's vertex data directly
  (`MeshFilter.sharedMesh.vertices` × `localToWorldMatrix`) and computing
  bottom-band vs. top-band vertex-count ratios per local axis — the true
  up-axis has far more geometry near its base (walls, foundations) than its
  tip (domes, finials), and this ratio picked out the correct axis (though the
  *first* pass over-trusted which axis had the strongest ratio rather than
  checking direction/sign carefully, still requiring one more visual iteration
  against the reference concept art before the staircase was confirmed
  ascending from the ground, not descending into it). See Roadmap Section
  4.4's checklist (updated with this lesson) and `docs/SESSION_LOG.md` for
  the full corrected methodology. All 67 EditMode tests still pass (no new
  tests — pure asset-pipeline work). Maurya/Maratha (18 models) remain
  unstarted. See `docs/SESSION_LOG.md`.
- [x] **Maurya (9/9) — done 2026-09-01.** All 9 buildings wired via the same
  pipeline. Identification: 8 of 9 folders resolved confidently by
  Meshy-internal filename or human-naming; `Domed_Stone_Sanctuary` resolved to
  House by elimination + live geometry check (no missing-folder or
  duplicate-asset gap this time — the prior session's "8 folders, one short"
  endnote was stale, actually 9). Rotation: 8/9 needed `Euler(-90,0,0)`
  (Market/Farm/Gate/Dock/Wall/House/Barracks/TownCenter) — several of these
  (Market, House, Barracks) coincidentally had Y-tallest bounds at identity
  despite lying flat, caught only by looking (a dome bulging out the front
  face, not the top), not by the bounds numbers. TownCenter used the vertex
  base/tip density method directly (wide/sprawling like Rajput's), correctly
  predicting the same correction (31.45:1 base-heavy ratio on local Z).
  **Tower shipped upside-down once from this session's own verification** —
  `Euler(0,90,0)` and `Euler(0,-90,0)` both give Y-tallest bounds through the
  civ-blind `ImportRotationCorrections["Tower"]` stomp, and the two were
  wrongly treated as interchangeable (a "pure Y-spin can't flip up/down"
  assumption that doesn't hold once composed with the stomp's own Z rotation);
  caught by the user from a delivered screenshot, not by this session's
  process — `Euler(0,-90,0)` is correct. Also hit a genuine Unity
  `ModelImporter` bug: Wall's raw FBX imported as a 0-vertex mesh under the
  project's default `useFileScale=true`, root-caused (not source corruption —
  byte-identical re-copies reproduced it, but a separate one-off import of the
  same bytes worked fine) to that FBX's embedded file-scale metadata being
  degenerate; fixed by setting `useFileScale=false` on that one asset's
  importer. Scale: worker height measured fresh (1.902692), ratio hierarchy
  applied — TownCenter 11.22/Tower 8.00/Market 4.85/Barracks 4.36/Dock
  3.81/Wall 2.66≈Gate 2.65/House 2.58/Farm 2.09. Also hit and worked around a
  genuine `ENOSPC` mid-session (flagged as a risk by the prior session's own
  endnote) — stopped and asked the user to free space rather than guessing
  what was safe to delete, per CLAUDE.md. All 67 EditMode tests still pass (no
  new tests — pure asset-pipeline work). Maratha (9 models) remains unstarted.
  See `docs/SESSION_LOG.md`.
- [x] **Maratha (9/9) — done 2026-09-01. Closes all 5 civs' civ-specific
  building models (45/45).** All 9 buildings wired via the same pipeline.
  Identification: 7 of 9 resolved confidently by Meshy-internal filename
  (`Stone_Gatehouse`→Gate, `Stone_Bastion_Wall`→Wall, `Fortified_Harvest_Man`→
  Farm, `Fortress_Bazaar`→Market, `Harborstone_Keep`→Dock,
  `Stone_Citadel_Tower`→Tower, `Stone_Citadel_of_Ashv...`→TownCenter); the
  2-way ambiguity flagged by the prior endnote (`Fortified_Stone_Prison` vs
  `Fortified_Stone_Villa` against Barracks/House) was resolved by the user
  directly (Prison→Barracks, Villa→House) after this session's own live
  geometry inspection came back inconclusive on framing. No duplicate-asset
  trap (byte-diffed all folders). Rotation: 8/9 needed `Euler(-90,0,0)`
  (Gate/Farm/Market/Dock/Barracks/House/TownCenter — TownCenter via the
  vertex base/tip density method, 8.42:1 base-heavy on local Z, matching the
  wide/sprawling-fort pattern from Rajput/Maurya's own TownCenters); Tower
  needed the usual civ-blind `ImportRotationCorrections["Tower"]` treatment
  and hit the exact Y+90-vs-Y-90 tying-bounds trap flagged by the prior
  session's endnote — both screenshotted and checked against the reference
  Watchtower art before picking `Euler(0,-90,0)` (Y+90 was upside down,
  crenellated cap on the bottom). **Wall shipped wrong once from this
  session's own verification, caught only by the user from a live
  screenshot**: an angled `game_view` shot at identity rotation looked
  plausible (parallax made a wall lying flat on its back read as upright),
  and the resulting scaled prefab had an anomalously thick footprint (Z depth
  ~90% of height) that should have been the tell but wasn't caught before
  shipping. Re-verified via true top-down + front-elevation shots (not
  angled) against the reference art — identity was actually lying flat face-up;
  `Euler(-90,0,0)` is correct (thin footprint, merlons on top, matches
  reference). Re-checked all other 8 buildings the same rigorous way as a
  precaution; all 8 confirmed already correct, only Wall was wrong. Scale:
  worker height measured fresh (1.902692, matching Maurya's session exactly),
  same ratio hierarchy reused — TownCenter 11.22/Tower 8.00/Market 4.85/
  Barracks 4.36/Dock 3.81/Wall 2.66≈Gate 2.65/House 2.58/Farm 2.09. All 67
  EditMode tests pass (no new tests — pure asset-pipeline work). See
  `docs/SESSION_LOG.md`.

Recommended sequencing, highest visual impact first:
1. **TownCenter, Barracks** — every match has exactly one TC (the civ's visual
   anchor) and Barracks is the first production building; smallest set (10 models)
   that makes all 5 civs read as visually distinct from the start of a match.
2. **Tower, Wall, Gate** — tall/vertical fortification forms read architecturally
   distinct fastest (gopuram towers vs. chhatris vs. hill-fort silhouettes).
3. **Market, Dock** — mid-game economy buildings, moderate visibility.
4. **Farm, House** — small, numerous, lowest individual visual weight; can
   reasonably stay on the shared model longest.

Each model: 8,000–20,000 tris (per-tier per 4.1), 2048×2048–4096×4096 PBR
(metallic/roughness), civ style per the 4.1 reference list. Lands at
`Assets/Resources/Buildings/<CivId>/<BuildingResourceName>` (e.g.
`Buildings/Chola/Barracks`) — `BuildingModelFactory.Spawn` already probes this path
first as of Section 5 item 7; a civ/building with no model there just falls through
to the existing shared model, so these can be added one at a time.

- [ ] **UI skin** — full HUD/menu visual pass; currently functional-only, genuinely
  unstarted. Needs its own style sheet (panel art, icon set, cursor set) consistent
  with the 3D art direction above, not sourced piecemeal per element.
  **Audit + technical scaffold done 2026-08-27** (art itself not started — arranged
  separately by the user, same as items 3/6). Audited every UI script
  (`ResourceHUD`/`SelectedUnitPanel`/`BuildMenu`/`HoverTooltip`/`MinimapController`/
  `CivPicker`/`ObjectivePanel`/`MissionSelectMenu`/`SettingsMenu`/`DiplomacyMenu`):
  zero UI art exists anywhere in the project, no cursor-state code at all, and — a
  concrete bug found along the way — the 4 runtime-code-generated menus had already
  drifted into 2 different ad hoc dark palettes before any real art landed. Added
  `Assets/Scripts/UI/UIStyleTheme.cs`, a `ScriptableObject` style-token source
  (`UIStyleTheme.Current`, `Resources.Load`-with-hardcoded-default-fallback, same
  pattern as `DataRegistry`) and wired every panel/button across all 7 affected
  scripts to it — fixes the palette drift today with zero art, and once a real
  `UIStyleTheme.asset` with 9-slice `PanelFrameSprite`/`ButtonBackgroundSprite` exists,
  every panel picks it up automatically (`ApplyPanel`/`ApplyButton` already check for
  a non-null sprite). `ResourceHUD` deliberately left out of scope — it has no
  background element in code or scene to theme without a scene edit. Live-verified in
  Play mode via UnityMCP: `SettingsMenu`/`DiplomacyMenu` boxes now read the identical
  `(0.05, 0.05, 0.08, 0.97)` instead of 2 different colors; `BuildMenu` buttons now
  read the shared `(0.25, 0.25, 0.3, 1)` instead of Unity's default gray. A prioritized
  asset spec (icons/9-slice frames/cursors, ~35-40 assets, sequenced by visibility) was
  proposed separately for the user to arrange/commission. See `docs/SESSION_LOG.md`.
  **Tiers 1-3 art delivered 2026-08-28** to `Assets/Resources/UI/` (Cursors/Icons/
  Panels/Menu) — audited against the spec (essentially complete: 4/5 cursors, all 19
  action icons, all 4 resource icons, both button-background sets, both HP-bar pieces,
  all 3 panel frames, all 3 menu-button states, all 5 civ crests). Found every asset
  had an opaque baked-in background instead of real alpha (confirmed via pixel
  sampling, not just preview) — fixed with a purpose-built border-flood-fill script
  (samples corner reference colors, flood-fills matching background inward from the
  border, dilates first to bridge anti-aliasing gaps) rather than trusting Canva/
  Gemini's "transparent" output at face value. All ~40 files renamed from
  auto-generated prompt-text filenames to stable short names in the process; raw
  pre-fix originals preserved at `UI_RawOriginals_backup/` (repo root, outside
  `Assets/` so Unity never imports it). Two known gaps carried over from the audit
  (still open, not fixable by this pass): the 5th cursor (build-placement) was never
  generated, and the Maurya crest renders in Rajput's blue instead of the spec'd warm
  gray/stone. One image (`resource_stone.png`) is only ~85% cleaned (a few residual
  background patches resisted the flood-fill) — needs a manual touch-up or
  regeneration. Theme-asset creation, Sprite import settings/9-slice borders, and the
  actual icon/HP-bar/crest/cursor wiring code remain for a planned session (Section
  4.3's item, not yet started).
  **Display wiring landed 2026-08-28.** Created `Assets/Resources/UI/UIStyleTheme.asset`
  (assigned `modal_frame`/`menu_button_*`), set Sprite/Cursor import types and 9-slice
  borders (auto-detected per file, hand-verified) for all 45 files. Live-scene
  inspection via UnityMCP (not just the C#) surfaced 3 corrections to the earlier
  audit: `ResourceHUD` actually already has its own background `Image` (missed by a
  code-only read); `BuildMenu` buttons are 204x28 thin text rows, not square icon
  buttons, constraining icon placement; `ResourceHUD`/`SelectedUnitPanel`/
  `HoverTooltip` each got their own dedicated background sprite
  (`panel_resource_bar`/`panel_selected_unit`/`panel_tooltip`) rather than the one
  shared `UIStyleTheme.PanelFrameSprite` (reserved for the 4 true modals), since
  routing them all through one field would put the wrong art on 3 of the 4 panels.
  `BuildMenu` command-card buttons got the dedicated `CommandCardButton` 4-state
  sprite set (via `Button.spriteState`, not the shared theme) plus a left-edge icon
  on the ~24 buttons with a matching asset (verified live: text gracefully word-wraps
  on the few longest labels instead of clipping - accepted, not a bug).
  `SelectedUnitPanel` gained a real HP bar (`hp_bar_frame`/`hp_bar_fill`, fill-amount
  driven by `Health/MaxHealth`) alongside the existing HP text. `CivPicker` cards got
  civ crests (Name/Blurb shifted down 64px to make room). Cursor wiring added to
  `HoverTooltip.Update()` for the 3 states with real art (default/gather/attack-move);
  build-placement is deliberately left on the default cursor (asset doesn't exist).
  Also found and fixed `hp_bar_frame.png`/`hp_bar_fill.png` still carrying huge
  transparent margins from leftover noise-speckle artifacts that blocked the earlier
  bbox-crop — tightened via a largest-connected-component filter before use. All 37
  EditMode tests pass; live-verified in Play mode via UnityMCP screenshots (CivPicker
  crests, BuildMenu icons + text wrap, HP bar fill, SettingsMenu modal reskin) with
  zero new console errors. 9-slice border/multiplier values are a reasonable first
  pass (visually spot-checked, not pixel-perfect) - refining them further is cosmetic
  polish, not a correctness gap. Two content gaps carried out of the art delivery,
  tracked as their own items below since they need new/redone art, not more wiring:
  **9-slice polish pass done 2026-08-31.** Pixel-verified all 5 tuned elements
  (`ResourceHUD`, `SelectedUnitPanel`/HP bar, `HoverTooltip`, `BuildMenu` command-card
  buttons, `MissionSelectMenu` buttons) live in Play mode via UnityMCP at their actual
  runtime rect sizes: computed each sprite's effective screen-space border from
  `spriteBorder` ÷ `pixelsPerUnitMultiplier`, then confirmed against zoomed screenshots.
  No stretching, pinching, or seams found on any of the 5 - the prior session's tuning
  holds up under pixel scrutiny; no code changes were needed. Hit and fixed one real
  tooling gotcha along the way (the known "stale compiled state" issue, this time for
  Resources-loaded assets, not scripts - `Image.sprite` read null on every themed
  element until `refresh_unity` with `mode=force`; see Known Gotchas in CLAUDE.md).
  **Found and fixed one real adjacent bug** (not 9-slice-related, user confirmed
  in-scope): `FormationIndicator.cs` (its own always-on top-left `Canvas`, added in a
  past "Phase 6 gap-close" session, never touches `Main.unity`) was anchored at the
  exact same `(8, -8)` corner as `ResourceHUD`, rendering both texts on top of each
  other in every session, not just this one. Fixed by moving `FormationIndicator`'s
  anchor to `(8, -206)`, just below `ResourceHUD`'s 200x190 footprint. Live-verified
  fixed in Play mode; all 67 EditMode tests pass.
- [x] **Build-placement cursor asset missing** — **closed 2026-08-28.** The user
  imported an Asset Store pack ("Basic RPG Cursors", `Assets/Cursors/`) hoping it
  1:1-replaced the spec'd 5 states; it didn't (generic weapon/tool icons at 64/256px,
  none a literal crossed-swords/circle-slash/sickle/hammer-and-nail, arrow-badge
  composition rather than the brief's standalone centered icons) — confirmed via
  actual pixel inspection before assuming a match, then the user explicitly chose to
  proceed with the closest-available icon per state anyway (see
  `docs/UI_ART_BRIEF.md`'s Tier 2 cursor entry for the exact mapping and caveats).
  All 5 states now wired in `HoverTooltip.cs` via a new pure/testable
  `ResolveCursorState` helper (6 new EditMode tests, `HoverCursorStateTests.cs`), also
  fixing a real latent bug found along the way: Attack-move previously showed
  regardless of whether the selection could actually attack (e.g. a pure economy
  selection hovering an enemy), and Gather never signaled anything when the selection
  couldn't gather — both now correctly resolve to the (also newly-wired) Invalid
  state instead. All existing textures corrected from 2048×2048 to the spec'd 32×32
  in the process (cropped/resized via Python/Pillow, not in-Editor
  `Texture2D.GetPixels`, per the known Editor-crash gotcha). Live-verified in Play
  mode via UnityMCP against real scene objects (real selection component checks, real
  `BuildingPlacer.IsPlacing`), not just EditMode tests — see `docs/SESSION_LOG.md`.
- [x] **Maurya crest is off-palette** — **closed 2026-08-31.** User regenerated
  `crest_maurya.png` (Ashokan lion pillar capital, warm gray/stone lion against a
  maroon/gold ring) and dropped it in at the same path/format — no code or import-
  settings changes needed, the existing `.meta` picked it up automatically.
  Live-verified in Play mode via UnityMCP screenshot: reads clearly distinct from
  Rajput's crest now, no longer confusingly similar on the CivPicker screen.
- [x] **4 Maurya/Maratha unique units** (`maurya_war_elephant`, `pillar_edict_scholar`,
  `maratha_mavla_raider`, `maratha_durg_garrison`) — **done 2026-08-27**, real Meshy-
  sourced models rigged and wired in for all 4 (see Section 1's matching item and
  `docs/SESSION_LOG.md`).
- [x] **Base human body decision** (see 4.2) — **resolved deliberately 2026-09-01**:
  keep the current Human Character Dummy body, don't swap to the Crusader Knight
  model this round. See Section 1's "Per-civ soldier visual differentiation" item
  for the full decision and what remains open (the swap itself, and gear/prop
  variants, both scoped there as separate future work — not silently dropped).
- [ ] **Per-civ architectural differentiation** — the single biggest visual-fidelity
  gap relative to real AoE civs, which distinguish themselves architecturally, not
  just by color. This is a real content project of its own (5 civs × building set),
  worth scoping as its own milestone rather than folding into general "art pass."
- [ ] **Music** — zero coverage currently.

### 4.4 Sourcing/Creation Checklist (apply to anything new, regardless of source)
Standing rules from real bugs already hit — apply these whether you're commissioning,
buying, or making an asset yourself:
- Verify scale **in-Editor, in-scene, next to what it'll actually stand beside** —
  isolated preview has already missed a 4–27x scale error once; don't trust preview
  alone again.
- Confirm PBR metallic/roughness workflow before import — reject Built-in-RP-only
  material sets outright rather than fixing them post-hoc.
- Check the color/tint property name if civ-color tinting matters
  (`_Color`/`baseColorFactor`/`diffuseFactor` have all appeared from different
  sources) — `TintMaterials` already falls back through all three, but verify a new
  source actually works before assuming it does.
- Watch for Z-up sources needing a corrected child transform (not a root-transform
  fix — the spawn path resets root rotation).
- **Mandatory, per model, before committing**: instantiate the model in-scene
  (isolated, e.g. at a high Y so it doesn't overlap other geometry) and compare
  it against its reference concept art from at least a front-on angle, not just
  an AABB bounds check — a Z-up model lying flat on a wide base can have
  superficially plausible bounds while still being on its back. A bounds-only
  check is what let Tower's rotation bug (Chola, Vijayanagara) get caught while
  6 more Vijayanagara buildings with the identical Z-up symptom shipped
  undetected in the same session that discussed Tower's fix (see
  `docs/SESSION_LOG.md`'s 2026-08-31 "Vijayanagara building-model rotation fix"
  entry). Applies to every model in every future civ batch (Rajput/Maurya/
  Maratha next), not just Tower-shaped assets — this bug has now recurred
  across both civs shipped so far. **For Tower specifically** (or any resource
  name ever added to `BuildingModelFactory.ImportRotationCorrections`), a
  bare instantiate-and-inspect of the saved prefab is not sufficient — that
  dict's correction is applied at runtime on top of whatever's baked into the
  model, so the check must reproduce the full spawn stack (instantiate, apply
  the dict's rotation to the instantiated root, then judge the result) or it
  can produce a false negative, as happened once already on Vijayanagara's own
  Tower mid-fix.
- **"Pick the rotation candidate that gives the tallest Y bounds" is not a
  universal heuristic** — it only holds for genuinely tall/narrow assets
  (Towers). A wide, sprawling building (a TownCenter fort complex, e.g.) can
  have its correct, upright orientation be *shorter* than some wrong
  orientation's bounds, because a jutting wing or staircase can make X or Z
  exceed the true height even when correctly assembled. Two visually
  plausible-looking wrong rotations were live-verified and shipped as
  "confirmed" for Rajput's TownCenter this way before the user caught the
  tilt from an in-game view (see `docs/SESSION_LOG.md`'s 2026-08-31 "Rajput
  TownCenter orientation" entry). What actually resolved it: read the raw
  mesh's vertex data directly (`MeshFilter.sharedMesh.vertices` ×
  `localToWorldMatrix`) and compare bottom-band vs. top-band vertex-count
  ratios per local axis — a real building has far more geometry near its
  base (walls, foundation) than its tip (domes, finials), so the axis with
  the strongest base-heavy ratio is the true up-axis, independent of bounds
  extent. Still confirm the *sign* (which end is actually "up") and the
  final result visually against the reference before committing — the ratio
  alone doesn't rule out an upside-down or off-yaw result.
- For any body-swap or rig-affecting asset, verify rig compatibility explicitly and
  in isolation before wiring it into the shared path every unit depends on.
- Keep unused/source-only import content out of `Assets/Resources/` (use
  `Assets/importedmodels/`) so it doesn't bloat builds.
- Every CC-BY (or similar attribution-required) asset gets a `CREDITS.md` entry at
  the time it's added — the current Sword/Bow/Kanabo/Mounts gap (sourced with no
  recorded attribution) shows how easily this slips if it's not immediate.

---

## 5. Recommended Near-Term Order

1. ~~**Batch-close the training/trade UI gaps** (Cavalry, Siege, Dock, Spearman,
   Market trade) — same category of fix, same BuildMenu pattern already established
   repeatedly, highest value-for-effort item on this whole list.~~ **Done.**
2. ~~**Wire the remaining per-civ passive bonuses live** — this is what makes Maurya/
   Maratha (and the fuller bonus sets on the original 3) actually functional, not just
   selectable.~~ **Done.**
3. ~~**Build the 4 missing unique-unit factories** — closes the last real content gap
   in the 5-civ roster.~~ **Backend-complete; visual closure (4 models) pending,
   tracked separately per Section 4.3 — not blocking, not part of this item's scope.**
4. ~~**Re-verify every "confirmed by reflection/config only, not live" item** now that
   the Editor flakiness fix exists (Wall carving is the flagged one, but check for
   others across the log).~~ **Done.** Found 3 candidates across the dev history (Wall
   carving item 35, control-groups dead-unit pruning item 34, Highlands/Coastal
   ground+NavMesh rebuild item 44) and live-confirmed all 3 in a genuinely,
   naturally-ticking Play mode session — no regressions found, one real (non-bug)
   nuance documented on Wall. See `docs/SESSION_LOG.md`.
5. ~~**Resume the balance pass properly** — start actually logging to
   `playtest_log.csv`, then tackle civ/age/upgrade stacking.~~ **Done for this
   session's scope** — 15 real fights logged, roster coverage gaps filled, stacking
   audited and confirmed correct (no code fix needed). Training cost-vs-power ratios
   and continued sustained playtesting remain open for a future balance session.
6. Everything else (music, tutorial, performance profiling, store assets) is
   real but lower-urgency — sequence after the above based on what you want to
   prioritize next, not by default order. UI skin's code-side work (9-slice polish
   pass, item 9 below) is done; what remains is art-only (Maurya crest recolor, 5th
   cursor), not a code task.
7. ~~**Per-civ architectural differentiation, architecture groundwork** — audited
   `BuildingModelFactory` and wired it (plus all 9 building factories) to probe a
   civ-keyed model path before falling back to today's shared model, so civ-specific
   building models can land incrementally, one civ/building at a time, with no code
   changes needed per asset.~~ **Code groundwork done.** Actual civ-specific models
   are a separate content project (see Section 4.3) — Chola (9/9, 2026-08-28),
   Vijayanagara (9/9, 2026-08-31), Rajput (9/9, 2026-08-31), Maurya (9/9,
   2026-09-01), and Maratha (9/9, 2026-09-01) done. **All 5 civs' civ-specific
   building models complete (45/45).**
8. ~~**Visual closure for the 4 Maurya/Maratha unique units** — real Meshy-sourced
   models, rigged via Blender command-line scripting (3 onto the existing shared
   human rig, the War Elephant onto a real third-party elephant skeleton+animation
   set) and wired into their factories.~~ **Done** (2026-08-27). See Section 1's
   matching item and `docs/SESSION_LOG.md` for full methodology and the one known
   first-pass limitation (elephant Die clip).
9. ~~**UI skin polish pass** — pixel-verify the 9-slice border/multiplier values
   against actual runtime sizes instead of the earlier visual spot-check.~~ **Done**
   (2026-08-31). No border/multiplier changes needed - all 5 tuned elements hold up
   under pixel scrutiny. Found and fixed one real adjacent bug instead (FormationIndicator/
   ResourceHUD anchor collision). Maurya crest recolor still needs new art from the
   user - not a code task. See Section 1's matching item and `docs/SESSION_LOG.md`.
10. **Per-civ soldier visual differentiation — session 1 (2026-09-01): tint-gap fix
    + scoping.** Fixed the real Maurya/Maratha untinted-white bug (all 5 civs now
    tint correctly). Made the base-body decision explicitly (keep current dummy,
    defer the Crusader Knight swap). Scoped gear/prop variants as a spec for the
    user to source. Body swap and gear/prop implementation remain open, tracked in
    Section 1's matching item — user's call on when to pick them up.
11. ~~**Repair system** (worker mechanics audit item) — right-click a damaged
    building/ship/siege unit with a worker to heal it at a Wood cost proportional
    to HP restored.~~ **Done** (2026-09-01). New `Repairable`/`Repairer` mirror
    `ConstructionSite`/`Builder`'s shape and reuse its diminishing-returns
    multi-worker formula; wired onto all 9 building kinds, Siege, and both naval
    units. See Section 1's matching item and `docs/SESSION_LOG.md`.
12. ~~**General garrisoning system, AoE IV style** (worker mechanics audit
    item) — pooled capacity, any eligible unit, scaling defensive firepower,
    ejection to a rally point.~~ **Done** (2026-09-01). Generalized `Garrison`
    into `GarrisonPoint` and `DurgGarrisonWorker` into `GarrisonSeeker`;
    generalized `TowerAttacker` into `BuildingAttacker` so TownCenter could
    get a baseline `Attacker` for the first time. Wall keeps its exact
    pre-existing narrow (durgOnly) behavior on the same class; Siege units
    excluded from garrisoning. Found and fixed a real latent NavMeshObstacle
    approach-point bug for TownCenter mid-session. See Section 1's matching
    item and `docs/SESSION_LOG.md`.
13. ~~**Dedicated resource-specific drop-off buildings** (the last
    worker-mechanics-audit item; implemented as AoE-Parity Execution Plan
    Phase 3.1)~~ **Done** (2026-09-01). New Lumber Camp (Wood)/Mining Camp
    (Gold+Stone)/Mill (Food) buildings, each a valid `Gatherer` drop-off only
    for its own resource type via a new `Gatherer.AcceptsDropOff` routing
    rule; `TownCenter` stays the universal drop-off. Full placement/BuildMenu
    wiring alongside. See Section 1's matching item and `docs/SESSION_LOG.md`.
14. ~~**Team-bonus / alliance economic stacking layer** (AoE-Parity Execution
    Plan Phase 3.2, user-confirmed "yes" after being asked directly)~~ **Done**
    (2026-09-01). New `TeamBonus.cs` hand-written hook (same bespoke-per-civ
    convention as `UniqueTechDefinition`/`RajputDefianceHook`) shares a diluted
    version of each civ's own unique-tech identity with every ally, gated on
    the existing `DiplomacyRegistry.AreAllied` alliance state and unconditional
    (not gated on the ally researching anything): Maurya allies get -25% Wood
    on Houses, Vijayanagara allies get +15% Wall/Gate/Tower HP, Rajput allies
    get +1 flat Cavalry damage, Maratha allies get +10% Cavalry move speed,
    Chola allies get a narrowed +/-5-point Market spread. See Section 1's
    matching item and `docs/SESSION_LOG.md`.
15. ~~**Worker self-defense/cross-awareness** (the last worker-mechanics-audit
    item; implemented as AoE-Parity Execution Plan Phase 4.2)~~ **Done**
    (2026-09-01). New `Attackable.OnDamaged` event, `CombatResponse`
    (Fight/Flee) enum, and `WorkerCombatResponseDefaults` per-civ lookup —
    every civ's Workers fight back when attacked mid-gather except Maratha's,
    which flee (guerrilla identity). Found and fixed 3 real latent bugs along
    the way (an ambiguous global-vs-namespaced `DamageType` in `WildBoar.cs`,
    and `UnitMover`/`MeleeAttacker` both caching sibling components in `Awake`
    instead of lazily). See Section 1's matching item and
    `docs/SESSION_LOG.md`.
16. **Multiplayer determinism** (AoE-Parity Execution Plan Phase 5) — in
    progress. Investigated first (no code), reported the doable-vs-blocked
    split, user approved the doable-now part. ~~`BuildingPlacer` wired
    through `CommandBus`~~ **Done** (2026-09-01): the one remaining
    Player-input path that bypassed the lockstep input-delay queue now goes
    through a new `BuildCommand`, live-verified in Play mode (resource spend
    + building spawn genuinely deferred ~4 ticks, not immediate). ~~Add a
    `CommandBus`/`StateHash` self-consistency test~~ **Done** (2026-09-01):
    4 new EditMode tests proving "same inputs → same state" directly (121
    total, up from 117). **Remaining, explicitly not done**: real desync
    detection (needs two peers), resync/rollback recovery logic (design-only
    doable now, real validation blocked on a transport), and cross-machine
    NavMeshAgent/physics determinism (needs two real machines). See Section 6
    below for full detail and `docs/AOE_PARITY_EXECUTION_PLAN.md` for the
    plan's own step-by-step scope of what's left.

---

## 6. AoE-Parity Execution Plan Status

`docs/AOE_PARITY_EXECUTION_PLAN.md` is the detailed, phase-by-phase reference
(goals, decision points, step-by-step scope, named failure modes) — this section
is the status summary so a session that only opens *this* file still has full
context without also opening that one. Every "closed" claim below was
cross-checked against the actual repo state (not just chat-log/session-log
claims) on 2026-09-01 as part of this consolidation pass — file/method
existence and wiring were grepped directly, not assumed.

**Phase 1 — Player Color System: deferred, not dropped.**
Item 1.1 asked "how should player color and civ color coexist," but before
answering, the session found the item's own premise doesn't hold: it assumes
an arbitrary-N player-slot system (its acceptance test is "two players on the
same civ render as distinct colors"), and this codebase has no such thing —
`FactionId` is exactly 3 fixed factions (Player, one human; Enemy/Enemy2, up to
two AI — `Assets/Scripts/Core/FactionMember.cs`), not a multiplayer lobby.
Building a player-color palette system now would be infrastructure with no
current consumer. User agreed: all of 1.1–1.4 are deferred until a real
arbitrary-player-count multiplayer path exists — **explicitly tied to this
section's own Phase 5 below** (see Phase 5's "blocked on transport" split),
not deleted from the plan. Civ tint stays exactly as-is
(`HumanModelFactory.PaletteNameFor` — Chola→Red, Vijayanagara→Yellow,
Rajput→Blue, Maurya→Gray, Maratha→Green).
*Adjacent real bug found and fixed while checking*: `CivilizationSetup` could
silently assign the same civilization to two of the three fixed factions in
one match (the scene's own `aiCivilization` Inspector default is
Vijayanagara, so a player picking Vijayanagara collided with it on an
ordinary first match) — a live instance of the exact "can't tell factions
apart" problem this phase exists to solve. Fixed with a deterministic dedup
guard, confirmed still present:
`CivilizationSetup.ResolveDistinctCivilization` (`Assets/Scripts/Core/CivilizationSetup.cs:160`).

**Phase 2 — Combat calibration: closed.** Audited `CombatBonus`'s Archer↔Cavalry
pairing against a numeric standard rather than eyeballing it — at the old 1.5x
Archer→Cavalry multiplier, a stationary duel against base-stat Cavalry (40 HP)
killed it in 7 hits, but Cavalry's own return hits left the Archer at just
1.2/18 HP (7%) remaining when it died — tactically a coin-flip once any
pathing/positioning noise is added, not a real hard counter. Raised to **2.0x**
(`CombatBonus.Multiplier`, `Assets/Scripts/Combat/CombatBonus.cs:71`): the same
duel now kills Cavalry in 5 hits with the Archer still at 6/18 HP (33%)
remaining — a clear, not-a-coin-flip win. 2.5x was also modeled (4 hits, 47%
HP remaining) and rejected as stronger than needed. Infantry→Archer and
Cavalry→Infantry were audited in the same pass and left unchanged — both
already resolve decisively (loser retains only ~20–30% max HP), so raising them
further only compresses already-fast fights without changing the outcome.
Items 2.2 (soft-counter mechanics — none exist; every relationship in
`CombatBonus`/`CounterMatrix` is purely damage-multiplier based) and 2.3
(formations vs. Siege — Siege has no splash damage, so the 5 shipped formation
types are cosmetic against it, not functional) were audited and logged as new,
separate open items in Section 1, not implemented — a numbers audit isn't
license to build a new mechanic mid-item.

**Phase 3.1 — Resource-specific drop-off buildings: closed.** `LumberCamp.cs`,
`MiningCamp.cs`, `Mill.cs` (`Assets/Scripts/Buildings/`) and
`Gatherer.AcceptsDropOff` (`Assets/Scripts/Resources/Gatherer.cs:365`) all
confirmed present and wired — Lumber Camp/Mining Camp (Gold+Stone)/Mill (Food),
`TownCenter` still the universal drop-off. Full `BuildingPlacer`/`BuildMenu`
wiring alongside (hotkeys J/U/P).

**Phase 3.2 — Team bonus / alliance economic stacking: closed, user
explicitly confirmed wanting it.** Asked directly per the plan's own
DECISION NEEDED gate ("does the user want a team-bonus layer at all") before
scoping anything — user said yes, explicitly motivated by wanting the
project's systemic depth to reach AoE IV's level. `TeamBonus.cs`
(`Assets/Scripts/Core/TeamBonus.cs`) confirmed present and wired into all 5
hook sites: `BuildingPlacer.WoodMultiplierFor` (Maurya, allied Houses -25%
Wood), `WallFactory` (Vijayanagara, allied Wall/Gate/Tower +15% HP),
`CavalryFactory` (Rajput +1 flat Cavalry damage; Maratha +10% Cavalry move
speed), and `Market.EffectiveSellRate`/`EffectiveBuyRate` (Chola, narrowed
+/-5-point spread) — all confirmed by direct grep, not just SESSION_LOG claim.

**Phase 4.1 — General garrisoning system: closed** (this is the same item as
Roadmap Section 5's item 12 — the plan's Phase 4.1 folds in an
already-scoped roadmap item rather than introducing a new one). `GarrisonPoint.cs`,
`GarrisonSeeker.cs`, and `BuildingAttacker.cs` (generalized from the old
Maratha-Durg-only `Garrison`/`TowerAttacker`) confirmed present; TownCenter
confirmed to now have a baseline `Attacker` component
(`TownCenterFactory.cs:46`, `AddComponent<BuildingAttacker>()`) where it
previously had none.

**Phase 4.2 — Worker self-defense/cross-awareness: closed.** `Attackable.OnDamaged`,
`CombatResponse`, `WorkerCombatResponseDefaults` all confirmed present and wired
(`Assets/Scripts/Combat/Attackable.cs:54`,
`Assets/Scripts/Resources/CombatResponse.cs`,
`Assets/Scripts/Core/WorkerCombatResponseDefaults.cs`). Design outcome: every
civ's Workers auto-fight back when attacked mid-gather (matching the capability
every Worker already had via its own weak `MeleeAttacker`, just now
auto-triggered instead of requiring an explicit attack-move command) except
Maratha's, which flee instead (guerrilla hit-and-run identity, consistent with
its Phase 3.2 team bonus and existing Cavalry-speed bonus). 117 EditMode tests
pass (confirmed by re-running the suite this pass, not just trusting the
session-log count). Live-verified in Play mode through the real production
event path: a Maurya Worker closed a real ~4-unit NavMesh-pathed gap down to
0.21 units onto its attacker; a Maratha Worker under the identical setup
increased its tracked distance from the attacker from 19.9 to 25.9 units,
never engaging.

**Phase 5 — Multiplayer determinism: in progress, doable-now part closed.**
Investigated first (no code) and reported a "doable now" vs. "blocked on
transport" split back to the user before writing anything, per instruction.
Findings: `CommandBus`/`SimClock` already wire Move/Attack/Train orders
through the lockstep input-delay queue; `BuildingPlacer.TryConfirmPlacement`
was the one remaining Player-input path that bypassed it (deducted resources
and called `XFactory.Place` synchronously at click time instead of through a
`Command`); `StateHash.Compute()` existed but had **zero call sites anywhere**
(confirmed by grep — the plan doc's "currently only detects desync" framing
overstated it; nothing detects anything today, the hashing primitive just
exists). User approved doing the doable-now part now. **Closed same session**:
new `BuildCommand.cs` (`Assets/Scripts/Multiplayer/BuildCommand.cs`), same
delegate shape as `TrainCommand`. `BuildingPlacer.TryConfirmPlacement` now
only does a client-side pre-check and enqueues; the actual resource spend +
`Factory.Place` call moved into a new `ExecuteBuild(kind, point)`, which
**re-validates** `IsClearForKind`/`CanAfford` again at execute time (state may
have changed in the delay window) — mirrors `Barracks.RequestTrain`'s own
re-check-and-no-op convention exactly. `CanAfford`/`IsClearForKind`/
`CurrentFootprint` all refactored to take an explicit `BuildingKind` parameter
instead of reading the mutable `_kind` field, since the command captures a
kind at click time that could differ from `_kind` by the time it executes.
AI's own building placement (`AiController.cs`) stays direct on purpose,
matching `AttackCommand.cs`'s existing documented precedent that automatic
per-tick AI/simulation decisions don't need queuing. Also added the
**self-consistency hash test** requested alongside this: `CommandBus.ExecuteTick`
made `internal` (was `private`) and a new `internal CommandBus.EnqueueAt`
added, both purely for direct EditMode testability (`SimClock` never ticks in
EditMode). 4 new EditMode tests in `CommandBusDeterminismTests.cs` — fixed
enqueue-order execution, and the actual "same inputs → same state" proof:
replaying an identical command stream against two independently-built,
identical starting worlds produces an identical `StateHash`, while a
genuinely different stream produces a different one (guards against the test
passing trivially). Hit and fixed a real test-authoring gotcha along the way:
`Unit.OnEnable()` doesn't fire synchronously right after `AddComponent<Unit>()`
in EditMode — the exact same gotcha `BuildingAttackerTests` already documents
for `Unit.All` — which meant the first draft of the negative-case test passed
for the wrong reason (`StateHash.Compute()` was silently folding over zero
units); fixed by registering into `Unit.All` directly, matching
`BuildingAttackerTests`' own established convention. 121 EditMode tests total,
all pass (up from 117). **Live-verified in Play mode via UnityMCP through the
real production path** (not a reflection-only shortcut): started a real match
(`CivilizationSetup.BeginMatch`), drove the actual private `TryConfirmPlacement`
method with a real `Physics.Raycast` against the real ground collider (camera
temporarily repositioned to guarantee a valid hit, since the Editor's stale/
off-screen cached mouse position doesn't raycast onto the ground on its own -
this is pre-existing `Input.mousePosition` behavior, unrelated to this
session's changes) — confirmed Wood stockpile and House count were **both
unchanged immediately after the click** (500 Wood, 1 House) while a real
`BuildCommand` was genuinely enqueued in `CommandBus`, then, after ~2 real
seconds (far past the 200ms `InputDelayTicks` window), confirmed Wood actually
dropped to 474.5 (30 base cost × Chola's -15% civ discount, exact match) and a
second House existed. **Remaining, explicitly not done this pass**: real
desync detection (needs two peers to compare hashes against), resync/rollback
recovery logic (the plan's own scope, genuinely blocked on having a transport
to validate against — `SaveManager`'s existing F5/F9 JSON serializer is a
plausible snapshot-format starting point but carries its own documented v1
gaps), and cross-machine NavMeshAgent/physics determinism testing (needs two
real machines). See Roadmap Section 5 item 16 and `docs/AOE_PARITY_EXECUTION_PLAN.md`
for the full item-by-item scope of what's left.

**Phase 6 — Home City-style meta-progression: deferred, do not start without
explicit user request** (per the plan's own instruction — real new-system
scope, not required for "AoE-level" parity on its own).

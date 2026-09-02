# Project: Kingdoms of Bharat

Historical RTS across Indian kingdoms/empires, targeting AoE II/IV-level systemic
depth. Full roadmap: `/docs/ROADMAP.md` (Master Roadmap v3 — sections: 1. Open items
punch list, 2. Architectural notes to preserve, 3. Process note, 4. Art direction &
asset requirements, 5. Priority order).

## Current status (keep current — update every session)
- Working from Roadmap Section 5's priority order.
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
- Currently on: nothing started for the next session. Section 5 items 1-9 and
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
  longer an open item. Item 7 (civ-specific building models) is at
  **43/45** — Chola/Vijayanagara/Maratha are 9/9 each; Rajput and Maurya are
  8/9 each, missing TownCenter and Tower respectively (their source FBX
  files were confirmed 0 bytes from the original delivery, unrecoverable
  from git history — removed 2026-09-02 rather than left silently broken,
  both civ/building combos now render the shared fallback model; re-sourcing
  real art for these 2 is a pending asset need, not a code task).
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
  asset need, spec written 2026-09-01, needs the user to source),
  **re-sourcing the Rajput TownCenter / Maurya Tower models** (see item 7
  above — same "needs new art, not code" shape as the Crusader Knight
  blocker), other "everything else" items (music, tutorial, performance
  profiling, store assets, multiplayer determinism gaps, README drift), or
  continued balance work — user's call.
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

# Master Roadmap — Kingdoms of Bharat (v3, reconciled against actual dev history)

Source of truth for prior status: `1787767106136_plan-it-out-and-dynamic-wolf.md`
(items 1–51+, Phases 1–6). This document does not restate that history — it
synthesizes it into what's genuinely open, and folds in anything from the earlier
generic AAA-planning docs that still applies. Treat the source doc as the detailed
commit-level log; treat this as the current punch list.

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
  below); self-defense partially matches (workers have a weak `MeleeAttacker` and
  can fight/hunt boars, but it's always an explicit player attack-move command —
  `Gatherer` and `MeleeAttacker` have zero cross-awareness, so a worker being
  attacked mid-gather is never auto-interrupted into a defensive state, unlike
  AoE's pattern). **Multi-builder construction speed fixed same session**:
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
- [ ] **Dedicated resource-specific drop-off buildings** (from the worker mechanics
  audit, 2026-08-29) — `Gatherer.FindNearestDropOff` currently only ever
  considers `TownCenter`; user confirmed (over keeping unified TC-only drop-off)
  that this should become real Lumber Camp/Mining Camp/Mill-equivalent buildings,
  each valid only for its own resource type, with "nearest valid drop-off"
  becoming meaningful the way it is in AoE. Real new content: new
  building type(s), factory/placement/footprint wiring, and updating
  `Gatherer.FindNearestDropOff` to filter by resource type per drop-off kind
  instead of a hardcoded `is TownCenter` check. Not started.
- [ ] **Repair system** (from the worker mechanics audit, 2026-08-29) — completely
  missing; no `Repair` anywhere in the codebase. AoE reference: right-click a
  damaged building/ship/siege unit with a worker selected to repair it, at a
  resource cost proportional to HP restored. Needs a new right-click command path
  through `SelectionManager` (mirroring the existing `Builder`/`ConstructionSite`
  and `FarmWorker`/`Farm` "worker-side component + target-side component" shape),
  a repair-rate/resource-cost formula, and interaction with `Attackable`'s
  existing HP/`ConstructionSite`'s completion state. Not started — real new
  system, not a small tweak.
- [ ] **General garrisoning system** (from the worker mechanics audit, 2026-08-29) —
  the only existing `Garrison` component (`Assets/Scripts/Buildings/Garrison.cs`)
  is narrowly scoped to the Maratha Durg Garrison unique unit, single-slot,
  Wall/Tower only, and its sole effect is toggling siege-immunity — not the AoE
  reference behavior of any worker/soldier entering a TownCenter/Tower/Outpost-
  equivalent for safety AND boosting that building's own defensive firepower,
  with ejection back to a prior task or rally point. Real gap found while scoping
  this: **TownCenter has no `Attacker` component at all** (only `Tower` does, via
  `TowerAttacker`) — "increase the building's defensive firepower while
  garrisoned" has no existing TownCenter firepower to augment yet, so building
  this out means adding TownCenter's baseline firepower too, not just hooking
  into something pre-existing. Should be scoped against (likely generalizing,
  not replacing) the existing `Garrison` component and `TowerAttacker`. Not
  started — real new system, ties into existing defensive-firepower code, needs
  its own deliberate design pass before implementation.

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
- [ ] **Base human body decision** (see 4.2) — resolve deliberately, don't leave it
  as a deferred "someday" item indefinitely.
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

# AoE-Parity Execution Plan — Kingdoms of Bharat

Companion doc to `ROADMAP.md`. Everything here comes from a gap analysis against
`aoe_research_pack.md` (AoE2 economy/civ-bonus design, AoE4 counter/combat design,
AoE formations, AoE3 Home City, and AoE's player-color system), cross-referenced
against the current roadmap's actual open items and closed-item notes.

**How to use this doc**: hand this whole file to the Claude Code session working on
this repo, one phase at a time, in order. Each item is written the way this
project's own `docs/SESSION_LOG.md` entries are — goal, decision point, steps,
required verification, and named failure modes to avoid — because this project's
own history shows exactly where "looks done" and "is done" diverge.

---

## 0. Ground rules before any phase starts (read this every session)

These are lifted directly from Section 2 and Section 3 of `ROADMAP.md` — they are
not new rules, they're a reminder that this plan does not override them:

- **One Claude Code session, one item, at a time.** Do not start Phase 2 while
  Phase 1 is still open. Do not let a session touch two unrelated items "while
  it's in there."
- **"Confirmed by config/reflection only" is not confirmed.** Every item below
  that touches gameplay-visible behavior needs a live Play Mode verification step,
  not just a passing EditMode test. This project has been burned by this exact
  gap at least three times already (Wall carving, control-group pruning, Highlands
  ground rebuild) — don't add a fourth.
- **Don't trust a peer-session's claim about what's done.** If a prior session's
  note says something is closed, re-check the actual repo state before building
  on top of it.
- **`CombatBonus` and `CounterMatrix` stay separate.** Nothing in Phase 2 is
  permission to merge them.
- **Write the test before declaring the item closed**, not after, and re-view any
  file immediately before editing it — this project's own tooling notes flag that
  stale `view` output has already caused at least one bad edit.
- **When a phase item requires a design decision** (marked **DECISION NEEDED**
  below), stop and get it from the user directly in chat before writing code.
  Guessing and shipping a guess is exactly the pattern that caused the Maurya
  gray-palette compromise and the deferred base-body swap — better to ask once
  than to build the wrong thing confidently.

---

## Phase 1 — Player Color System (foundational; do this first)

**Why first**: every other visual/UI phase below (garrison indicators, formation
readability, future multiplayer lobby UI) assumes "color tells you who owns this."
Right now it doesn't — color tells you which civ was picked. Building anything
else on top of the current scheme means re-touching it later anyway.

### 1.1 — DECISION NEEDED: resolve the civ-identity vs. player-identity conflict

The current system (`HumanModelFactory.PaletteNameFor()`) uses the *same* tint
slot for civ identity that AoE uses for *player* identity. Before writing code,
get an explicit answer from the user on one of:

- **(a)** Civ identity moves entirely to gear/props/architecture (the already-scoped
  Section 1 "Per-civ soldier visual differentiation" gear/prop item), freeing the
  base-body tint slot for player color. This is the AoE-faithful answer but means
  the crest-color tint work already done (Chola→Red, Vijayanagara→Yellow, etc.)
  gets *repurposed*, not thrown away — those same colors become candidate
  player-slot colors instead of civ-locked ones.
- **(b)** Civ keeps its body tint, and player color becomes a *second*, additive
  overlay (banner/flag prop, selection ring, HP-bar color, minimap dot only —
  never touching the body material). Cheaper, less visually clean, but zero risk
  to the just-finished tint-gap fix.
- **(c)** Something else the user prefers.

Do not proceed to 1.2 until this is answered and written into this doc's own
changelog (add a line under this item recording the decision and date, the same
way `ROADMAP.md` records decisions like the base-body one).

> **Decision (2026-09-01): deferred, not (a)/(b)/(c).** Before picking, Claude Code
> flagged that this item's own framing — "assigns one palette entry per player slot,"
> acceptance test "two players on the same civ" — assumes an arbitrary-N player-slot
> system that doesn't exist in this codebase. `FactionId` is exactly three fixed
> factions (Player, Enemy, Enemy2 — one human, up to two AI;
> `Assets/Scripts/Core/FactionMember.cs`), not a multiplayer lobby. Building (a) or (b)
> now would be infrastructure with no current consumer. User agreed: **all of
> 1.1/1.2/1.3/1.4 are deferred**, not implemented, until a real arbitrary-player-count
> multiplayer path exists — tied explicitly to Phase 5's existing "real transport
> doesn't exist yet" blocker below, not deleted from this plan. Civ tint stays exactly
> as-is (`HumanModelFactory.PaletteNameFor`, Chola→Red/Vijayanagara→Yellow/Rajput→Blue/
> Maurya→Gray/Maratha→Green).
>
> **Adjacent real bug found and fixed while checking, same session**: before deferring,
> the user asked whether `CivilizationSetup.Assign` can currently put the same civ on
> two of the three fixed factions in a single skirmish — it could, and it wasn't
> theoretical (the scene's own `aiCivilization` Inspector default is Vijayanagara, so a
> player picking Vijayanagara collided with it on an ordinary first match). Since civ
> identity is the only body tint that exists today, that collision made two factions
> render identically — a small, live instance of the exact problem this phase was
> written to solve. Fixed with a deterministic dedup guard in
> `CivilizationSetup.BeginMatchCore` (the player's pick is authoritative, AI factions
> resolve around it and each other). See `docs/SESSION_LOG.md`'s 2026-09-01 entry for
> the full fix, tests, and live verification.

### 1.2 — Build the per-match player-color palette

- [ ] Define a fixed slot palette (reuse or extend the existing 5 civ crest
  colors as the starting candidate set; AoE2:DE's reference set for parity is
  Blue/Red/Green/Yellow/Cyan/Purple/Gray/Orange if more than 5 concurrent
  players are ever in scope).
- [ ] Add a `PlayerColorService` (or equivalent) that assigns one palette entry
  per player slot at match/scenario setup — **not** per civ selection.
- [ ] **Collision guard, mandatory**: write the assignment logic so two active
  player slots can never receive the same color in the same match. This is a
  documented real bug in AoE2 itself (two players both assigned Blue in an
  unranked match) — don't ship the same class of bug on day one. Add an EditMode
  test that asserts uniqueness across N players for N ≤ palette size, and asserts
  a defined, non-crashing fallback behavior if N exceeds palette size.
- [ ] Wire the assignment into whatever overlay path 1.1 decided (material tint,
  banner prop, selection outline, HP-bar color, minimap dot — apply consistently
  across all of these, not just one).

### 1.3 — Reconcile with colorblind mode

- [ ] Colorblind mode currently remaps civ colors. After 1.2, it must remap
  *player* colors instead (or both, if 1.1 chose option (b)). Audit
  `docs/UI_ART_BRIEF.md`'s colorblind palette definitions against whatever new
  palette 1.2 introduced, and update the deuteranopia/protanopia/tritanopia
  mappings to match — a colorblind mode that still keys off civ instead of
  player identity doesn't actually solve the readability problem it was built for.

### 1.4 — Verification (do not skip)

- [ ] EditMode: color-assignment uniqueness test (1.2), fallback-on-overflow test.
- [ ] Live Play Mode: spawn a match with two players on the *same* civ; confirm
  they render as visually distinct player colors while sharing identical
  architecture/unit meshes. This is the actual acceptance criterion — a same-civ
  mirror match is the one scenario the current system cannot pass, so it's the
  right test to prove the fix.
- [ ] Live Play Mode: toggle colorblind mode on and confirm it now remaps the new
  player-color layer correctly, screenshotted side by side per the existing
  civ-tint verification precedent.

**Definition of done**: two players on the same civilization are visually
distinguishable by color alone, in every UI surface that currently shows civ
tint, with no regression to the existing per-civ mesh/architecture work.

---

## Phase 2 — Combat calibration (AoE4 axis)

**Why second**: this doesn't require new systems, just auditing and adjusting
values already in `CombatBonus`/`CounterMatrix` — cheap relative to its impact on
"does combat feel like AoE." Sequenced after Phase 1 only because Phase 1 touches
shared visual infrastructure other phases will build UI against; this phase is
otherwise independent and could be reordered ahead of Phase 1 if the user prefers.

### 2.1 — Audit `CombatBonus` multiplier scale against AoE4's reference values

- [ ] Pull the current numeric values out of `CombatBonus` for every counter pair
  (Infantry/Archer/Cavalry/Siege/Spearman/Naval).
- [ ] Compare against AoE4's actual scale: hard counters there run at **2x–3x
  damage multipliers** (e.g. Spearman vs. Cavalry, War Elephant vs. Cavalry),
  not small percentage bonuses. If BHARAT RTS's values are in the +15–25% range,
  they will read as "slight advantage," not "hard counter" — flag this explicitly
  to the user with the actual numbers side by side before changing anything,
  since rebalancing existing playtested values is a deliberate design change, not
  a bug fix (same category distinction the roadmap already draws for the
  multi-builder speed formula).
- [ ] **DECISION NEEDED**: get explicit user sign-off on any multiplier change —
  this is exactly the kind of playtested-balance value the roadmap's own
  architectural notes warn against touching casually.

### 2.2 — Evaluate adding a soft-counter mechanic

- [ ] AoE4 has counters that work through unit *behavior* rather than bonus
  damage (e.g. Archers effectively counter Men-at-Arms by kiting, not by dealing
  extra damage). Audit whether BHARAT RTS has any equivalent (kiting AI, armor
  classes that reduce incoming damage without a symmetric bonus-damage entry) or
  whether every relationship in `CounterMatrix`/`CombatBonus` is purely
  damage-multiplier based.
- [ ] If purely multiplier-based: this is a real, undesigned depth gap, not a bug.
  Scope it as its own future item rather than bundling into 2.1 — don't let an
  audit turn into an ad hoc new-mechanic implementation mid-item.

### 2.3 — Verify formations actually do something against Siege

- [ ] Check whether Siege-class attacks in BHARAT RTS have area/splash damage.
  AoE's Staggered and Flank formations only matter tactically because siege
  splash damage exists to dodge — if BHARAT RTS's Siege units are single-target,
  the 5 shipped formation types are cosmetic, not functional, and that's a
  "shipped but not delivering the AoE experience" gap even though the checkbox
  reads closed.
  - [ ] If Siege already has splash: write a live Play Mode test — two identical
    squads, one in Line, one in Staggered, both hit by the same Siege attack;
    confirm Staggered takes measurably fewer casualties. This is the actual
    acceptance criterion, not "the formation button exists and units visually
    rearrange."
  - [ ] If Siege has no splash: flag as a new item (add splash damage to Siege
    class), don't silently fold it into this audit.

### 2.4 — Verification

- [ ] EditMode tests for any changed `CombatBonus` values, following the existing
  pattern (`ConstructionSiteTests`-style named assertions).
- [ ] Live Play Mode: staged fights per pairing showing the new multiplier
  actually resolves combat decisively (a "hard counter" should win convincingly,
  not narrowly), logged to `playtest_log.csv` per the project's existing
  balance-pass convention.

---

## Phase 3 — Economy depth (AoE2 axis)

### 3.1 — Dedicated resource-specific drop-off buildings

This is already a scoped, open item in `ROADMAP.md` Section 1 (Lumber Camp/Mining
Camp/Mill-equivalent). No new decisions needed — sequencing note only: do this
**before** 3.2, since a team-bonus economy layer is more meaningful once
resource-gathering has real building-placement decisions attached to it.

- [ ] Implement the new building type(s): factory, placement, footprint wiring
  (reuse `BuildingFootprint.cs`'s existing table pattern).
- [ ] Update `Gatherer.FindNearestDropOff` to filter by resource type per
  drop-off kind instead of the current hardcoded `is TownCenter` check.
- [ ] EditMode tests mirroring the existing `WaterMovementTests`/
  `BuildingFootprintTests` structure.
- [ ] Live Play Mode: confirm a villager gathering Wood actually routes to the
  nearest Lumber Camp over a nearer-but-wrong-resource-type building, and over a
  farther TownCenter.

### 3.2 — Team bonus layer for civ asymmetry

New item, surfaced by this gap analysis — not previously in `ROADMAP.md`.

- [ ] **DECISION NEEDED**: does the user want a team-bonus layer at all? It only
  matters if diplomacy/alliances are meant to carry an economic incentive beyond
  military cooperation — confirm this is a real goal, not just theoretically
  AoE-faithful, before scoping it.
- [ ] If yes: add one team-bonus entry per civ (mirroring AoE2's pattern — a
  single, narrow, stackable bonus, e.g. "allied Docks -X% cost") to
  `CivilizationDefinition`, and wire it so an active alliance applies the ally's
  team bonus alongside the player's own civ bonus — confirm stacking behaves
  additively/multiplicatively exactly as designed, since AoE2's own stacking
  behavior (civ bonus + ally team bonus on the same building) is a documented,
  specific interaction, not "whichever is bigger wins."
- [ ] EditMode test asserting stacked-bonus math against a known example.
- [ ] Live Play Mode: verify the stacked discount/bonus applies in a real
  allied-diplomacy game, not just in isolation.

---

## Phase 4 — Systems already scoped in ROADMAP.md (no new decisions, just sequencing)

These are already open items in the existing roadmap; listed here only to fold
them into one coherent execution order relative to the new phases above.

### 4.1 — General garrisoning system
Per `ROADMAP.md` Section 1: needs a design pass first (TownCenter has no
`Attacker` component to augment yet — that's new baseline firepower to add, not
just a hook-in). Sequence this **after** Phase 1 (player color), since garrison
UI will likely want to show garrisoned-unit-count per building, which benefits
from Phase 1's clean player/civ visual separation already existing.

- [ ] Design pass: generalize (not replace) the existing `Garrison` component and
  `TowerAttacker` pattern to cover TownCenter/Tower/Outpost-equivalent, any
  unit type, ejection-to-prior-task-or-rally-point behavior.
- [ ] Add TownCenter baseline `Attacker` component (new, currently absent).
- [ ] EditMode + live Play Mode verification, same standard as Phase 1–3.

### 4.2 — Worker self-defense / cross-awareness

- [ ] `Gatherer` and `MeleeAttacker` currently have zero cross-awareness. Add an
  interrupt: a worker taking damage while gathering should transition to a
  defensive/flee-or-fight state, matching the AoE reference pattern flagged in
  the worker-mechanics audit.
- [ ] EditMode test: simulate damage to a gathering worker, assert state
  transition.
- [ ] Live Play Mode: attack a gathering villager, confirm it responds without an
  explicit player command.

---

## Phase 5 — Multiplayer determinism

Sequence last among the "real gap" phases — highest effort, and per the roadmap's
own note, cross-machine determinism can't be meaningfully tested until an actual
network transport exists, so this phase may partially block on external work
outside Claude Code's control.

- [ ] Wire `BuildingPlacer` orders through `CommandBus` (currently bypassed —
  only Move/Train/Attack are wired).
- [ ] Design and implement resync-on-desync logic using the existing `StateHash`
  (currently only detects desync, doesn't recover from it).
- [ ] Once a real transport exists: run actual cross-machine determinism testing
  for NavMeshAgent/physics — flagged as unverified risk, not yet a known bug.

---

## Phase 6 — Deferred / optional (do not start without explicit user request)

- **AoE3-style Home City meta-progression layer**: real new-system scope (a 4th
  resource-like currency, ~25-card deckbuilding UI, shipment logic, per-civ card
  sets). Per the research pack's own framing, this is only worth it if BHARAT
  RTS wants a between-match progression/customization layer — it is not required
  for "AoE-level" parity on its own. Do not scope this further until the user
  explicitly asks for it.
- Everything already listed as lower-priority in `ROADMAP.md` Section 1 (music,
  tutorial, profiling pass, store assets) stays exactly where it is — this plan
  doesn't reorder those.

---

## Suggested order summary (for pasting into ROADMAP.md Section 5)

1. Phase 1 — Player Color System (decision needed first: 1.1)
2. Phase 2 — Combat calibration audit (decision needed: 2.1 multiplier changes)
3. Phase 3.1 — Resource-specific drop-off buildings (already scoped, no new decision)
4. Phase 3.2 — Team bonus layer (decision needed: whether wanted at all)
5. Phase 4.1 — General garrisoning system (design pass required)
6. Phase 4.2 — Worker self-defense/cross-awareness
7. Phase 5 — Multiplayer determinism (partially blocked on transport work)
8. Phase 6 — Home City-style meta-progression (only if explicitly requested)

Each phase's checkbox items should be copied into `ROADMAP.md` Section 1 under
their own headers as they're started, using the project's existing `[ ]`/`[x]`
+ **Closed**/**Not started** convention, with a `docs/SESSION_LOG.md` entry
written the same way every other closed item in the current roadmap is
documented — this plan doesn't introduce a new documentation format, just a
sequence.

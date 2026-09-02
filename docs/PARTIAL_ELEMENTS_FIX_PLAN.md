# Partial-Status Elements — Fix & Implementation Plan

Companion to `docs/AoE_vs_KingdomsOfBharat_Comparison.xlsx`. Covers the 6 elements marked
**Partial** in the Comparison tab, in the recommended implementation order. Each section is
scoped the way a Roadmap Section 1 item would be (per this project's own session protocol:
one item per session, Plan Mode before nontrivial changes, test before done, log to
`SESSION_LOG.md`). None of this has been implemented — this is the plan only.

---

## Recommended order

| # | Element | Why this position |
|---|---|---|
| 1 | Hotkey | **Done (2026-09-03)** — see the item's own section and `docs/SESSION_LOG.md` |
| 2 | Victory conditions | Closes a real core-loop gap (Conquest + Time Limit only, no new assets) |
| 3 | Area of Effect / Trample damage | Reuses existing splash-damage infrastructure, needs one design decision |
| 4 | Diplomacy (tribute) | Reuses existing DiplomacyRegistry, UI-only + small resource-transfer logic |
| 5 | Renewable resource (Farm reseed) | Needs a verification pass first before any code is written |
| 6 | Scenario Editor | Largest scope question — recommend the lightweight path, flag the heavy path |

Fish Trap (part of item 5) and a full in-game visual level editor (part of item 6) are called
out as **blocked on asset sourcing / a scope decision**, not part of this implementation pass,
consistent with "asset sourcing is not Claude Code's job" and the one-item-per-session rule.

---

## 1. Hotkey — audit and complete coverage — **Done (2026-09-03)**

**Result**: the real gap was bigger than "missing keys" — see
`docs/SESSION_LOG.md`'s 2026-09-03 entry for full detail. Only 3 of ~18
`BuildMenu` actions had a hotkey at all, and those 3 had a genuine
selection-scoping bug (a hotkey fired at every idle building of that type,
not just the selected one) — fixed by centralizing dispatch into
`BuildMenu.Update()`. Added 15 new hotkeys (all of Barracks/TownCenter/Dock
training and research, plus Ungarrison), registered every binding
(including 4 already-functional-but-unlisted placement keys and Save/Load/
Diplomacy) in `SettingsMenu.Actions`, and built the optional F1 hotkey-
reference overlay panel (`Assets/Scripts/UI/HotkeyOverlay.cs`). Market
buy/sell stayed click-only per user decision (consistent with AoE
convention). All 146 EditMode tests pass; live-verified via UnityMCP
against the real production path (see session log for the exact repro of
the bug being fixed).

<details>
<summary>Original plan (for reference)</summary>

**Gap:** Only the 3 newest drop-off buildings (Lumber Camp/Mining Camp/Mill = J/U/P) have
confirmed hotkeys. Coverage across the rest of `BuildMenu` and unit-training buttons is
undocumented.

**Plan:**
- Audit every `BuildMenu` command-card button and every unit-train button for an existing
  `KeyCode` binding (grep for the existing hotkey-handling code path used by J/U/P as the
  reference implementation).
- Assign a hotkey to every remaining building and trainable unit, following AoE II's
  QWERTY-row convention where practical (first letter or first available letter).
- Add a simple `Hotkeys.cs` (or extend the existing lookup) as a single source of truth, so a
  future new building/unit doesn't silently ship without one.
- Optional: a hotkey-reference overlay panel (toggle with a key like F1), matching
  `UIStyleTheme`'s existing 9-slice style. Not required to close the gap, just a nice-to-have.

**Assets needed:** None (code only) unless the reference-overlay panel is included, in which
case it reuses `UIStyleTheme` — no new art.

**Testing:** Hotkey dispatch normally lives in `Update()`, which EditMode tests can't easily
drive — plan for live verification via UnityMCP (press each key, screenshot/confirm the
correct action fires) rather than a pure-function unit test, same precedent as other
Input-driven fixes in this project's history.

**Estimated size:** Small — good single-session "quick win."

</details>

---

## 2. Victory conditions — Conquest + Time Limit first

**Gap:** Mission-based objectives exist (`MissionObjective`/`MissionTrigger`) for scripted
campaign missions, but there's no standard skirmish victory condition (Conquest, Time Limit,
Score, Regicide, King of the Hill).

**Plan — scope to 2 of the 5 AoE-standard conditions this pass, defer the rest:**
- **Conquest** (implement): a faction loses when it has zero remaining combat-capable units
  *and* zero remaining buildings capable of producing them (mirrors AoE's real conquest rule,
  not just "0 units this instant"). Hook a check into the existing tick loop (`SimClock`) —
  once per second is enough, no need for per-frame.
- **Time Limit** (implement): a configurable match-length setting; at expiry, declare the
  winner by a simple tiebreaker (e.g. total remaining population, or "no winner / draw" if
  tied) — deliberately simple since a full Score system doesn't exist yet.
- **Score-based, Regicide, King of the Hill** (defer): each depends on a system this
  report already flags as Missing — Score-based needs the Score element itself; Regicide
  needs a King unit + special-death rule; King of the Hill needs a capturable map objective.
  Flag these as separate future roadmap items rather than scoping them blind here.

**New code:** `Assets/Scripts/Core/VictoryCondition.cs` (Conquest + TimeLimit check), wired
into the match's tick loop; a `MatchOutcome` event/state that the UI subscribes to.

**Assets needed:** A Victory/Defeat splash-screen panel (1-2 full-screen UI panels, matching
`UIStyleTheme`). This is the one concrete new-art item — flag to the user as a small art
ask (a single win/lose banner design, reusable across both conditions), not a blocker to
writing the code.

**Testing:** New EditMode tests should synthesize game states (zero-unit faction, expired
tick count) and assert the correct `MatchOutcome` fires — this is the kind of pure logic this
project already tests well (see `CommandBusDeterminismTests.cs` for the pattern). Live-verify
in Play mode via UnityMCP by forcing both conditions in a real match.

**Estimated size:** Medium — a real new system, but a narrow, well-precedented one.

---

## 3. Area of Effect / Trample damage — cavalry charge damage

**Gap:** Siege splash/area damage is implemented and live-verified (`HostileFilter`,
`MeleeAttacker.SetSplashRadius`). Cavalry trample — damage to infantry a charging cavalry
unit passes through — does not exist.

**Plan:**
- **Design decision needed before coding** (per this project's own protocol of asking rather
  than assuming): should trample be (a) passive splash on every Cavalry melee hit, reusing
  `MeleeAttacker.SetSplashRadius` at a small radius/low secondary damage, exactly like Siege's
  implementation but tuned down — the cheap option — or (b) a true "charge through" mechanic
  that damages units along the movement path during a charge, which needs new
  path-intersection logic, not just a reuse of the splash radius? Recommend starting with (a):
  it's a one-line `SiegeFactory`-style wiring change (`CavalryFactory.SetSplashRadius(small
  value)`) with no new subsystem, and is what most AoE-likes actually mean by "trample" in
  practice.
- Tune the radius/damage low enough that it reads as a minor area effect, not Siege-tier
  splash — needs a numeric audit pass similar to the Archer-vs-Cavalry combat-bonus tuning
  already done in this project.

**Assets needed:** None required; optionally a subtle dust/impact VFX on trample hits if the
existing splash VFX doesn't already read well for a fast-moving unit — verify against a live
screenshot before deciding this is needed.

**Testing:** Extend the existing `SiegeSplashTests.cs` pattern with `CavalryTrampleTests.cs`;
live-verify via UnityMCP the same way the original Siege splash fix was verified (spawn a
formation, run one real attack cycle, compare casualties with/without).

**Estimated size:** Small-medium — mostly a tuning/verification pass once the design
decision is made.

---

## 4. Diplomacy — tribute and a player-facing stance UI

**Gap:** `DiplomacyRegistry` supports alliance state (already consumed by the Team Bonus
system) but there's no tribute (resource transfer between players) and no confirmed
player-facing UI for viewing/changing stance.

**Plan:**
- **Tribute:** a `Tribute(FactionId from, FactionId to, ResourceType type, float amount)`
  method that deducts from the sender's `ResourceStockpile` and credits the receiver's, minus
  a small tax percentage (matches AoE II's convention of taxing tribute to discourage farming
  it). Gate it behind an existing-alliance check via `DiplomacyRegistry` (or allow tribute to
  anyone, per AoE II's actual rule — confirm which the user wants before implementing).
- **Diplomacy UI panel:** expose current stance per faction + tribute buttons, using the
  civ crest icons that already exist (`CivPicker`'s art). This is the one piece that was
  "unconfirmed" rather than "missing" — first step is actually checking the live UI for
  whether a diplomacy panel already exists before writing a new one from scratch.
- **Shared vision for allies:** explicitly **out of scope for this item** — it depends on the
  Fog of War / Line of Sight system, which this report lists as fully Missing. Flag as a
  follow-on once Fog of War exists, don't build it blind against no vision system.

**Assets needed:** None beyond the already-existing civ crest icons for the panel.

**Testing:** New EditMode tests for the tribute math (tax applied correctly, insufficient-
funds rejected) mirroring `Repairable`'s cost-deduction test pattern; live-verify the UI via
UnityMCP.

**Estimated size:** Small-medium.

---

## 5. Renewable resource (Farms) — verify first, then fix; Fish Trap deferred

**Gap:** `Farm`/`FarmWorker` exists but it's unconfirmed whether farms auto-replenish or
require a manual reseed action; no Fish Trap building exists at all.

**Plan:**
- **Step 1 — verify, don't assume:** read `FarmWorker.cs`/`Farm.cs` directly and drive a farm
  to zero Food live via UnityMCP to observe actual behavior before deciding anything needs
  fixing. This report flagged it "Partial" specifically because the behavior wasn't confirmed
  either way in the session log — the first real task here is confirmation, per this
  project's own "treat unverified claims as unverified" rule.
- **Step 2a — if farms already auto-replenish:** this element is actually closer to
  Implemented than Partial; update the Comparison sheet and move on, no code needed.
- **Step 2b — if farms deplete to 0 with no recourse:** add a reseed action mirroring
  `Repairer`'s worker-side pattern — right-click a depleted Farm with a worker selected to
  replant it at a Wood cost, restoring its Food capacity. Reuses the existing
  worker-order-dispatch conventions (`SelectionManager`'s chain-of-exclusivity branches).
- **Fish Trap:** genuinely new content, not a fix — needs a placeable food building near
  water, which per this project's own rule ("asset sourcing is not Claude Code's job right
  now") is **blocked on the user sourcing a model** (1 model, or 1 per civ if it should match
  the rest of the building roster's civ-specific standard). Flag to the user rather than
  substituting a low-confidence placeholder, consistent with how the cursor-pack and Maurya
  crest gaps were handled previously in this project.

**Assets needed:** None for the reseed fix. Fish Trap needs 1-5 building models (see the
Comparison sheet's Assets column) — sourcing task, not a coding task.

**Testing:** New EditMode tests for the reseed cost/restore math if Step 2b applies; live
Play-mode verification via UnityMCP either way.

**Estimated size:** Small for the verification + possible reseed fix; Fish Trap is a separate,
larger, asset-blocked item — don't bundle it into the same session.

---

## 6. Scenario Editor — recommend the lightweight (CSV-authoring) path

**Gap:** `MissionObjective`/`MissionTrigger` exist as a real trigger/objective system, but
there's no player-facing in-game editor tool to build missions — mission authoring today is
code/data, not a UI a non-programmer could use.

**Plan — two paths, recommend starting with the light one:**
- **Light path (recommended):** extend this project's existing CSV -> ScriptableObject
  pipeline (`CsvToScriptableObject.cs`) to cover mission definitions — objectives, triggers,
  starting conditions — the same way civ/unit/tech data already works. This lets new missions
  be authored by editing a CSV and regenerating, with zero new UI, fully consistent with this
  project's established data convention. Closes most of the *practical* gap (non-programmer
  content authoring) without building editor tooling at all.
- **Heavy path (defer, flag to user):** a true in-Editor or in-game visual tool — drag-place
  units/triggers on a map, save as a custom scenario file, closer to AoE II's actual Scenario
  Editor. This is a substantial standalone feature (new UI, serialization format, likely a
  separate Unity Editor window) and should only be scoped once the user confirms it's
  actually wanted (e.g. for user-generated content / modding support) — not assumed.

**Assets needed:** Light path: none. Heavy path: editor UI art (panels, trigger-type icons,
drag handles) — sizeable UI-only asset task, scoped only if the heavy path is chosen.

**Testing:** Light path: an EditMode test asserting a sample mission CSV round-trips into a
correct `ScriptableObject` graph, mirroring existing CSV-pipeline tests. Heavy path: out of
scope until chosen.

**Estimated size:** Light path — small-medium, fits the existing data-pipeline pattern.
Heavy path — large, separate scoping conversation.

---

## What this plan deliberately does not do

Per this project's session protocol, nothing above has been implemented — this is scoping
only. Each numbered item above is sized to be its own single session's work (matching "one
roadmap item per session"), and items 2-6 each contain at least one explicit
decision/confirmation point that should be settled with the user in Plan Mode before code is
written, not assumed.

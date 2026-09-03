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
| 2 | Victory conditions | **Done (2026-09-03)** — Conquest already existed; Time Limit + Draw added |
| 3 | Area of Effect / Trample damage | **Done (2026-09-03)** — Cavalry trample via a generalized splash-damage multiplier |
| 4 | Diplomacy (tribute) | **Done (2026-09-03)** — stance UI already existed; added Tribute + buttons |
| 5 | Renewable resource (Farm reseed) | **Farm half done (2026-09-03)**; Fish Trap still deferred/asset-blocked |
| 6 | Scenario Editor | **Light path done; heavy path sessions 1 (Placements), 2 (Objective/Trigger authoring), 3 (saved-scenario browse list), and 4 (palette icons) done (2026-09-03)** |

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

## 2. Victory conditions — Conquest + Time Limit first — **Done (2026-09-03)**

**Result**: this item's own premise turned out half wrong once checked against the
actual repo (not just trusted) — see `docs/SESSION_LOG.md`'s 2026-09-03 entry for full
detail. **Conquest was already fully implemented** (`MatchManager.Evaluate`, present
before this session) and the "new art" Victory/Defeat splash already existed too
(`GameOverScreen.cs`) — neither needed building. **Time Limit was the real, only gap**,
now closed: `GameSettings.TimeLimitMinutes` (Off/15/30/45/60, cycled via a new Settings
row), `MatchManager.EvaluateSkirmishOutcome`/`ResolveTimeLimitOutcome` (population
tiebreaker, ally-aware, `Draw` on a tie — a new 4th `MatchOutcome` value),
`GameOverScreen` extended to render Draw. 9 new EditMode tests
(`MatchManagerTests.cs`), live-verified via UnityMCP through the real
`Update()`/`Time.unscaledTime` path, not just the isolated pure functions. **Also fixed
a real regression found while touching the same Settings screen**: `SettingsMenu`'s
Key Bindings list had silently overflowed its panel background since the prior
session's hotkey-coverage pass grew it from 12 to 34 rows with no layout resize —
fixed with a proper scrollable list (`ScrollRect`/`Viewport`/`Content`), screenshot-
verified scrolled to both ends.

**Original plan (for reference):**

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

## 3. Area of Effect / Trample damage — cavalry charge damage — **Done (2026-09-03)**

**Result**: implemented option (a) with the user-confirmed refinement of reduced
secondary damage — see `docs/SESSION_LOG.md`'s 2026-09-03 entry for full detail.
`MeleeAttacker.SetSplashRadius` gained an optional `damageMultiplier` parameter
(default 1f, so Siege's existing single-arg call is byte-for-byte unchanged);
`CavalryFactory` wires `SetSplashRadius(1.25f, 0.35f)` — a radius below formation
spacing (so only units clumped tight around the impact point are caught, not a full
adjacent rank) at 35% secondary damage (a minor effect even stacked on Cavalry's
existing 1.5x hard-counter bonus vs. Infantry). No new VFX needed — the existing
per-hit particle burst already fires for trample hits. 4 new EditMode tests
(`CavalryTrampleTests.cs`), live-verified via UnityMCP through the real
`CavalryFactory`/`SoldierFactory`/`MeleeAttacker.Tick` production path.

<details>
<summary>Original plan (for reference)</summary>

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

</details>

---

## 4. Diplomacy — tribute and a player-facing stance UI — **Done (2026-09-03)**

**Result**: this item's own first instruction — check the live UI before writing a
new panel — found the stance UI was **already fully built**
(`Assets/Scripts/UI/DiplomacyMenu.cs`, F11, real War/Allied toggle per faction), not
just "unconfirmed." Only Tribute itself was genuinely missing. New
`Assets/Scripts/Core/Tribute.cs` (`Tribute.Send(from, to, type, amount)`, 20% tax,
deliberately **not** gated behind `DiplomacyRegistry` — user-confirmed to match real
AoE II's rule that tribute works regardless of War/Allied stance). `DiplomacyMenu`
gained 4 flat-amount (50) tribute icon buttons per faction row (reusing the existing
`resource_{wood,food,stone,gold}` icons), affordability-gated the same way
`BuildMenu`'s Market Buy/Sell buttons already are. 5 new EditMode tests
(`TributeTests.cs`), live-verified via UnityMCP through the real button `onClick` (not
just the isolated static method) — see `docs/SESSION_LOG.md` for the exact numbers.

<details>
<summary>Original plan (for reference)</summary>

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

</details>

---

## 5. Renewable resource (Farms) — Farm half **Done (2026-09-03)**; Fish Trap still deferred

**Result**: Step 1 (verify, don't assume) found a third case neither of the plan's own
Step 2a/2b anticipated — a staffed Farm produced Food **forever, with no cap at all**,
not "auto-replenishes" (implies exhaustion+regen) and not "depletes with no recourse"
(implies exhaustion+no-regen). Put this finding to the user directly rather than
picking a fix silently; **confirmed: retrofit real AoE-style depletion + reseed** (the
larger option, bigger than this item's own "Small" estimate). `Farm.cs` now has a real
175-Food capacity (matching AoE II's own Dark-Age value) that depletes as it's worked
and a reseed mechanic (`BeginReseed`/`StopReseed`, Wood-costed at the Farm's own build
cost of 60 Wood for a full reseed, using `ConstructionSite.SpeedMultiplier` for the
same multi-worker diminishing-returns curve `Repairable` already uses). No new player
order or `SelectionManager` change was needed: `FarmWorker` autonomously switches
between harvesting and reseeding based on the Farm's own live depleted state, so the
existing right-click order just does the right thing. 8 new EditMode tests
(`FarmTests.cs`), live-verified via UnityMCP through the real production `Tick`/
`Update` path — see `docs/SESSION_LOG.md` for the exact numbers, including an
unplanned but convincing proof: the real system was observed cycling through a full
harvest→deplete→reseed→harvest loop entirely on its own between verification calls,
with no forcing. **Fish Trap stays deferred/asset-blocked**, untouched this session,
per this item's own original scoping below.

<details>
<summary>Original plan (for reference)</summary>

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

</details>

---

## 6. Scenario Editor — light path (CSV-authoring) **Done**; heavy path sessions 1
(Placements), 2 (Objective/Trigger authoring), 3 (saved-scenario browse list), and
4 (palette icons) — **Done (2026-09-03)**

**Heavy path, session 4 result**: picked up per the user's explicit request ("start
item on richer palette art for scenario editor"), the last cosmetic item session 1
flagged as deferred ("richer palette icons"). This project already has real
command-card icon assets for almost every placeable type - `BuildMenu.cs` already
wires them via its own `AddCommandIcon` helper reading `Resources/UI/Icons/
<name>.png` - so this was a wiring task (reusing already-provided assets), not new
asset sourcing, consistent with this project's standing rule that asset creation
isn't Claude Code's job. Cross-referencing `EntitySpawner.BuildingTypes`/
`UnitTypes` (the exact vocabulary `ScenarioEditorMenu`'s palette already iterates)
against the real `Resources/UI/Icons/` folder found **12 of 13 entries have a ready
icon; only `TownCenter` has none** (no `build_towncenter.png` exists anywhere in
the project - TownCenter is normally auto-spawned, never player-built through any
existing menu, so no other UI surface ever needed one either). New
`ScenarioEditorMenu.AddPaletteIcon` (adapted from `BuildMenu.AddCommandIcon`'s own
"icon + inset label" shape, retuned for this file's smaller 260×24 palette rows vs.
BuildMenu's 204×28 command cards - not shared directly, since the geometries differ
enough that reuse would need extra parameters) wires all 12; `TownCenter` stays
text-only, the same disclosed fallback `BuildMenu.cs` itself already uses for
Dock/LumberCamp/MiningCamp/Mill - a precedented pattern in this exact codebase, not
invented for this session. Pure UI-wiring with no new branching logic, so no new
EditMode test was added (matching `BuildMenu`'s own equivalent, which has none for
the same reason); ran the full suite to confirm 200/200 unchanged. Live-verified via
UnityMCP: opened the real editor and screenshotted the Buildings/Units palette -
all 12 icons render correctly next to their labels with no text overlap/clipping,
and TownCenter's row renders cleanly text-only with no broken/missing-icon
placeholder. See `docs/SESSION_LOG.md` for full detail.

<details>
<summary>Heavy path, session 3 result (for reference)</summary>

**Heavy path, session 3 result**: picked up per the user's explicit request ("start
a saved-scenario browse list on MissionSelectMenu"), the last item session 1/2 both
flagged as deferred - until now, the only way to play a saved custom scenario was
from inside the editor itself (open editor → Load → Play). New
`Assets/Scripts/Core/SavedScenarioLibrary.cs` extracts the scenario-file I/O
(`ScenarioFolder`/`ListSavedScenarioNames`/`Load`) that `ScenarioEditorMenu` had
inline in its own `ScenarioFolder`/`RefreshFileList`/`LoadFile` - both the editor and
`MissionSelectMenu` now read the exact same files through one source of truth,
verified behavior-preserving (the editor's own already-tested Save/Load flow was
re-run live after the refactor with no change in behavior). `MissionSelectMenu`
gained a "Custom Scenarios" section below Skirmish/Create Scenario: a `ScrollRect`-
based row list (same pattern `SettingsMenu`'s Key Bindings list and session 2's own
`ScenarioEditorMenu` Objectives tab already establish, reused rather than
reinvented, since the list is unbounded) listing every saved scenario name, with an
explicit "No saved scenarios yet" empty state. A new `ChooseCustomScenario(string)`
mirrors `ChooseScenario(ScenarioDefinition)`'s existing shutdown sequence exactly,
calling the same `CivilizationSetup.BeginCustomScenarioMatch` the editor's own Play
button already uses - no new match-start logic. 3 new EditMode tests
(`SavedScenarioLibraryTests.cs`), 200 total, all pass. Live-verified via UnityMCP
through the real production path: saved 2 scenarios through the real editor (one
with a `SurviveSeconds` objective, one placements-only) via reflection against the
real `Save()`, screenshotted the real Mission Select panel confirming both appear
in the new list with the correct empty/populated states elsewhere unaffected, then
invoked the real `ChooseCustomScenario` (the same call a click makes) and confirmed
`ScenarioManager.ActiveScenario`'s title and objective description matched the
saved scenario exactly - not the hand-coded/CSV mission list's own content. Test
scenario files deleted after verification. Explicitly still out of scope: deleting/
renaming a saved scenario from this browse list (editor-only for now), row metadata
(civ/map/placement count), thumbnail art. See `docs/SESSION_LOG.md` for full detail.

</details>

<details>
<summary>Heavy path, session 2 result (for reference)</summary>

**Heavy path, session 2 result**: picked up per the user's explicit request ("start
item on objective/trigger authoring for custom scenarios"), one of the deferred
follow-ons session 1 itself flagged. `MissionCsvLoader.BuildObjectives`/
`BuildTriggers` (the light path's own interpreter) were split into a typed,
JsonUtility-serializable row layer — new `ObjectiveRow`/`TriggerRow` classes plus
`internal static BuildObjectivesFromRows`/`BuildTriggersFromRows` — so the CSV path
(which now just maps `Dictionary` rows into these types first) and the in-game editor
share one interpreter for the same small fixed vocabulary (5 objective kinds, 2
trigger kinds), with zero behavior change for existing CSV missions (the full
pre-existing test suite passes unmodified). `CustomScenarioData` now carries
`objectives`/`triggers`/`victoryText`/`defeatText`. `ScenarioEditorMenu` gained a
second **Objectives** tab (alongside session 1's **Placements** tab, toggled by 2
buttons at the panel top) — a `ScrollRect`-based row list (same pattern
`SettingsMenu`'s own Key Bindings scroll fix already establishes) where an author adds
objective/trigger rows, cycles each row's Kind, and fills generic Param1-5/
Description text fields, each with a live one-line hint describing that kind's
param meaning (the same "small fixed vocabulary, not a general expression language"
disclosure the light path already makes, just surfaced in the UI instead of left to a
CSV author's memory). `CivilizationSetup.BeginCustomScenarioMatch` now calls
`ScenarioManager.Begin` with a real `ScenarioDefinition` built from the authored rows
— **but only when `data.objectives.Count > 0`**: `ScenarioManager.EvaluateOutcome()`
treats an empty objective list as "already complete" (an empty `foreach` never hits
its `Ongoing` branch), so calling `Begin` unconditionally would have made every
session-1 placements-only scenario resolve to an instant Victory — this gate is what
keeps that existing behavior intact. 7 new EditMode tests (`MissionRowsTests.cs` x6,
`CustomScenarioDataTests.cs` x1 more), 197 total, all pass. Live-verified via
UnityMCP through the real production path, not just the tests: authored a
`PopulationThreshold` objective + a `GrantResourceAtTime` trigger through the actual
`ScenarioEditorMenu` UI state (Kind/Param fields), Played it, and confirmed
`ScenarioManager.ActiveScenario` was correctly wired (non-null, right title/
objective/description, `IsComplete()` reading real live `Population.Current`) and
`MatchManager.Outcome` resolved to `Victory` via the scripted-mission branch, not
elimination; separately confirmed the regression case this session's own gate is
designed to prevent — a placements-only scenario with zero objectives still leaves
`ScenarioManager.ActiveScenario` null and `MatchManager.Outcome` at `Ongoing`
immediately after Play, exactly matching session 1's pre-existing behavior; and
separately proved Save→Close→re-Open→Load round-trips objective/trigger rows and
victory text correctly through the real file-based UI methods (not just the
JsonUtility unit test). Explicitly still deferred, not silently dropped: per-kind
bespoke input widgets (dropdowns instead of generic Param text fields), a
saved-scenario browse list on `MissionSelectMenu` itself, richer palette art,
multiplayer/LAN play of a custom scenario. See `docs/SESSION_LOG.md` for full detail.

</details>

<details>
<summary>Heavy path, session 1 result (for reference)</summary>

**Heavy path, session 1 result**: at the user's explicit request ("start the heavy
scenario path"), confirmed via AskUserQuestion this means a real in-game runtime
editor (UGC/modding), not a Unity EditorWindow, and that this first session should
ship placements (starting units/buildings per faction) — the part that genuinely
doesn't exist anywhere, deferring objective/trigger authoring to a follow-on. New
`Assets/Scripts/UI/ScenarioEditorMenu.cs` (opened via a new "Create Scenario" button
on `MissionSelectMenu`): a faction-scoped palette, click-to-place/drag-to-move/
right-click-to-delete lightweight markers on the real map, named Save/Load to
`persistentDataPath/Scenarios/*.json`, and Play (starts a real match with exactly
those placements via a new `CivilizationSetup.BeginCustomScenarioMatch`).
`SaveManager`'s own type-string→factory dispatch was extracted into a shared
`EntitySpawner.cs` (pure refactor) so both save/load and the new placement system
use one source of truth. v1 has no custom objectives/triggers — `MatchManager`'s
existing elimination-based Conquest evaluation just runs, same as a normal skirmish.
**A real gap was caught live, not assumed away**: this item's own plan only accounted
for 2 of the 3 gated default-spawn components (`TownCenterSpawner`/`AiController`) —
a live population check after Playing a saved scenario read 6 instead of the expected
2, revealing a 3rd gated spawner (`UnitSpawner`, unconditionally dropping 4 default
Workers) that needed the same opt-out. Fixed and re-verified live: exact population
match. 12 new EditMode tests, live-verified via UnityMCP through the real UI end to
end (open editor → place 5 markers across 2 factions → Save → Load → Play → confirm
exact positions/population/AI-adoption/standard-Conquest in the real running match).
Explicitly deferred, not silently dropped: objective/trigger authoring for custom
scenarios, a saved-scenario browse list on `MissionSelectMenu` itself, richer palette
art, multiplayer/LAN play of a custom scenario. See `docs/SESSION_LOG.md` for full
detail.

</details>

<details>
<summary>Light-path result (for reference)</summary>

**Result**: implemented the recommended light path, with a real architecture
correction found by reading the actual mission system before assuming the existing
CSV pipeline pattern transfers directly — see `docs/SESSION_LOG.md`'s matching entry
for full detail. `ScenarioDefinition.BuildObjectives`/`BuildTriggers` are
`System.Func<>` delegates, which Unity can't serialize into a ScriptableObject asset,
so unlike Tech/Unit/Civ data there's no Editor-time "bake CSV → asset" step possible
here — missions are parsed and turned into real closures **at runtime** instead, via
a new `Assets/Scripts/Match/MissionCsvLoader.cs` reading 3 CSVs
(`mission_definitions`/`mission_objectives`/`mission_triggers`) as `TextAsset`s under
`Assets/Resources/Data/Missions/`. A small fixed vocabulary of 5 objective kinds and 2
trigger kinds (sized directly off the 3 real hand-coded missions in
`ScenarioRegistry.cs`, plus 2 shapes the Roadmap's own Tutorial item already wants)
covers real mission content without a general expression language — a disclosed
limitation, not literally "any mission logic." `ScenarioRegistry.All` now merges the 3
hand-coded missions with `MissionCsvLoader.LoadAll()`; `MissionSelectMenu.cs` needed
**zero changes** since it already iterates that list directly. 8 new EditMode tests
(`MissionCsvLoaderTests.cs`) plus one new real sample mission ("The Muster"), live-
verified via UnityMCP through the actual Mission Select UI and the real
`Resources.Load<TextAsset>` path (not just the EditMode tests' in-memory CSV
strings). The heavy path (a true visual in-game editor) stays deferred, pending
explicit user intent, per this item's own original scoping below.

<details>
<summary>Original plan (for reference)</summary>

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

</details>

</details>

---

## What this plan deliberately does not do

Per this project's session protocol, nothing above has been implemented — this is scoping
only. Each numbered item above is sized to be its own single session's work (matching "one
roadmap item per session"), and items 2-6 each contain at least one explicit
decision/confirmation point that should be settled with the user in Plan Mode before code is
written, not assumed.

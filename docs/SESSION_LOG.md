# Session Log — Kingdoms of Bharat

Chronological log of Claude Code sessions against this repo, per CLAUDE.md's session
protocol (step 6). Newest entries at the top.

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

# Session Log — Kingdoms of Bharat

Chronological log of Claude Code sessions against this repo, per CLAUDE.md's session
protocol (step 6). Newest entries at the top.

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

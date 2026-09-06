# Implementation Roadmap — KingdomsOfBharat_Model_Spec.xlsx

Companion to `docs/KingdomsOfBharat_Model_Spec.xlsx` and `docs/PARTIAL_ELEMENTS_FIX_PLAN.md`.
This is the execution plan: every "To build" / "To build (asset)" / "Design decision" /
"Enum exists, unused" row from the spec workbook, sequenced into dependency-ordered waves,
each item sized to be one Claude Code session — matching this project's own established
protocol (one roadmap item per session, Plan Mode before nontrivial changes, log to
`docs/SESSION_LOG.md`, test before done).

Nothing here has been implemented. This is scoping and sequencing only.

## How this roadmap is organized, and why

A spreadsheet lists items; it does not order them. Four principles decided the order below,
in priority order when they conflict:

1. **Foundation before content.** A bug in the retroactive-upgrade rule or the two-taxonomy
   split gets multiplied by every unit built on top of it. Fix the plumbing first, even though
   it produces nothing visible, because a visible unit built on broken plumbing has to be
   redone twice.
2. **Structural unlocks before what they unlock.** The Durg age has to exist before anything
   can be Durg-tier. The Durg building has to exist before "garrison in Durg" means anything.
   Sequencing violations here are the single most expensive mistake to make.
3. **Cross-cutting systems before the content that will need to have been built with them in
   mind.** Player/team colour touches every unit and building material. Doing it once, early,
   costs one pass. Doing it after 20 new units and 15 new buildings exist costs a retrofit pass
   across all of them. Same logic for the UI layout re-anchor — cheap now, because the panel
   content already exists; still cheap later, but why carry the visual mismatch for six more
   waves of feature work.
4. **Independent tracks run in parallel, not in sequence.** Where an item has no dependency on
   anything else in the list, it is marked "parallel-safe" — a second Claude Code session (or a
   later session, out of order) can pick it up without waiting.

Every item below carries: **[size]** (rough session-count), **[depends on]** (nothing = safe to
start immediately), and the exact class(es)/enum(s) it touches, taken directly from the spec
workbook so nothing here requires re-deriving what the workbook already established.

Items marked **(asset-blocked)** need art/audio sourced by you before they can be closed —
Claude Code should build the CODE PLUMBING for these now (a switch statement, a material slot,
a factory hook) with an obvious placeholder fallback, rather than waiting idle for art that
may arrive later. That plumbing is not wasted work: it is what makes dropping the real asset in
later a five-minute change instead of a new feature.

---

## Wave 0 — Foundation (do this first, no exceptions)

These are cheap, invisible, and everything else compounds on top of them. Skipping this wave
to get to "real" content faster is the single most common way this kind of plan goes over
budget.

1. **[S] Reconcile UnitClass vs UnitCategory.** `Combat/UnitClass.cs` (7 values) and
   `Data/Scripts/UnitDefinition.cs`'s `UnitCategory` (9 values) diverge — `Support` and `Hero`
   exist in one and not the other. Decide: extend `UnitClass` to match, or collapse to one
   enum used everywhere. *Depends on: nothing. Blocks: any Support or Hero unit (Vaidya,
   Purohita, Maharaja hero), because they currently have no runtime `UnitClass` to attack/be
   attacked as.*
2. ~~**[S] Verify the retroactive upgrade rule.**~~ **Closed (2026-09-04).** Confirmed
   `Progression/UpgradeProgress.cs` did NOT promote already-spawned units — bonuses were read
   once at spawn and baked in (the class's own comment said so: "not retroactive"). Fixed:
   `Attackable`/`MeleeAttacker`/`BoatAttacker` now live-read the bonus at damage-resolution
   time via an opt-in `EnableUpgrade*Scaling()` call, made only by the 13 factories that
   already baked it in (buildings/Workers never called it and still don't — no scope creep).
   See `docs/SESSION_LOG.md`'s matching entry and this session's `CLAUDE.md` status note for
   full detail.
3. ~~**[S] Confirm the minimum-damage clamp.**~~ **Closed (2026-09-04).** Confirmed
   `Attackable.TakeDamage` (`Combat/Attackable.cs:160`,
   `Mathf.Max(1f, amount - armor)`) already clamps correctly: every real call site
   (`MeleeAttacker.ResolveHit`, `BoatAttacker.Tick`, `BuildingAttacker.Tick`) computes
   `baseDamage * CombatBonus.Multiplier(...)` — already including any sub-1.0 counter
   multiplier — before passing the final `amount` into `TakeDamage`, and the floor is
   applied as the *last* step, after armor subtraction, not before it — so a stacked
   armor value plus a <1.0 `CombatBonus` multiplier can't silently floor a hit at 0. No
   code change needed — this was a verify-only item, not a bug. 4 new EditMode tests
   (`DamageClampTests.cs`, 213 total, up from 209) covering: armor exceeding a
   post-multiplier hit, armor an order of magnitude over the hit, the floor NOT
   distorting a hit that legitimately beats armor, and repeated clamped hits still
   accumulating toward 0 (never stalling — the actual AoE guarantee this item exists
   to protect: no unit is mathematically unkillable). Hit this project's own documented
   "two `DamageType` enums" ambiguous-reference gotcha while writing the tests (an
   unqualified `DamageType.Melee` silently resolved to the wrong global enum — fixed by
   fully qualifying `KingdomsOfBharat.Combat.DamageType.Melee`/`.Pierce`, same fix
   `WildBoar.cs`/Wave 0 item 2 hit before) — the MCP console bridge reported zero
   compile errors while this was broken; the real errors were only visible in
   `~/Library/Logs/Unity/Editor.log` directly, matching this project's own documented
   lesson. Live-verified via UnityMCP through the real production path: a real match
   (`CivilizationSetup.BeginMatch`), a real `CavalryFactory`-spawned Cavalry unit
   attacking a real `ArcherFactory`-spawned Archer with 500 melee armor configured
   (Cavalry→Archer is CombatBonus's real 0.4x hard-countered matchup) — the real
   `MeleeAttacker.ResolveHit`/`Attackable.TakeDamage` path dealt exactly 1 damage, not
   0. *Depends on: nothing. Blocks: safe tuning of any new counter unit.*
4. ~~**[S] Wire `DamageType.Trample` and `DamageType.Fire`.**~~ **Trample half closed
   (2026-09-04)**; Fire deliberately deferred to Wave 4 (Fire Ship), per this item's own
   note. First resolved the enum duplication item 1 flagged as this item's prerequisite:
   `Combat/Attackable.cs`'s `DamageType` (previously Melee/Pierce only) is now the single
   shared enum (Melee/Pierce/Siege/Fire/Trample), and the second, incompatible global
   `DamageType` declared on `UnitDefinition.cs` purely as unread CSV metadata is deleted —
   `UnitDefinition.attackType` now references the same shared type. `Attackable.TakeDamage`
   gained an explicit `UsesPierceArmor(DamageType)` helper (Pierce/Fire → pierceArmor;
   Melee/Trample/Siege → meleeArmor) so the armor lookup and the upgrade-scaling-applies
   check can't drift out of sync. Wired `MauryaWarElephantFactory.cs`/
   `VijayanagaraWarElephantFactory.cs`: both now call `SetDamageType(DamageType.Trample)`
   plus `SetSplashRadius(1.4f, 0.4f)`, reusing the exact splash mechanism
   `CavalryFactory`'s own trample already established (PARTIAL_ELEMENTS_FIX_PLAN item 3)
   rather than inventing a second one — a radius below `GroupFormation`'s 1.5 default unit
   spacing, at a reduced secondary-damage multiplier, tuned a shade larger/heavier than
   Cavalry's own 1.25/0.35. `unit_roster_template.csv`'s `AttackType` column updated
   Melee→Trample for both war elephants to match (regenerated via `BharatRTS/Generate
   Data Assets From CSV`, zero parse warnings). 6 new EditMode tests (`TrampleDamageTests.cs`,
   215 total, up from 213): Trample resolves against meleeArmor not pierceArmor, and a
   Trample-tagged splash attacker damages a nearby hostile but not a far one. Live-verified
   via UnityMCP through the real production path: a real match
   (`CivilizationSetup.BeginMatch(Maurya)`), real `MauryaWarElephantFactory`/
   `VijayanagaraWarElephantFactory`-spawned units both confirmed (via reflection) to carry
   `damageType=Trample`/`splashRadius=1.4` at spawn, and a real Maurya War Elephant
   attacking a real `SoldierFactory`-spawned Soldier dealt full damage to the primary
   target, reduced splash damage to a second Soldier placed within the trample radius, and
   zero damage to a third placed outside it. Fire (`UnitDefinition.attackType`/
   `Combat.DamageType.Fire` both now exist on the shared enum) stays genuinely unset on
   every attacker — no factory calls `SetDamageType(DamageType.Fire)` yet, exactly as
   before this item, since the Fire Ship doesn't exist until Wave 4. *Depends on: nothing.
   Blocked (Fire half only): Wave 4's Fire Ship.*

**Wave 0 exit criteria:** one shared unit taxonomy, a verified retroactive upgrade rule, a
verified damage floor, one previously-dead enum value (`Trample`) now live and the other
(`Fire`) correctly deferred to its real Wave 4 consumer rather than wired without one.
Nothing shipped is visible to a player yet — that's expected and correct for this wave.
**Wave 0 is now fully closed (2026-09-04).**

---

## Wave 1 — The 4th Age (Durg)

Everything in the spec workbook that says "Durg" as an age is blocked on this wave. Do it as
one deliberate unit, not spread across other work.

5. ~~**[M] Add `AgeId.Durg`.**~~ **Closed (2026-09-04).** `Progression/AgeProfile.cs`'s
   `AgeId` enum now has `Durg` between `Classical` and `Imperial` (int-backed ordering shifts
   `Imperial` from 2→3 — `AgeProgress.NextAge`'s `CurrentAge + 1` arithmetic and
   `SaveManager`'s `(int)`/`(AgeId)` round-trip both still work correctly off the enum's
   *current* ordering, since neither hardcodes a numeric value; no back-compat guarantee was
   made or needed for old save files, consistent with this project's other enum-reordering
   sessions). Added a `Durg` row to `age_profile_template.csv` (250 Wood + 150 Stone, 40s
   research, gather x1.18, HP x1.15, train x0.85 — interpolated between Classical/Imperial),
   regenerated via `BharatRTS/Generate Data Assets From CSV`, and added the matching
   `Fallback`/`AgeIds` dictionary entries to `AgeProfile.cs` so the no-generated-asset
   fallback path stays correct too. **Deliberately dropped the roadmap's own proposed "50
   gold" component**: `AgeProfileDefinition`/the CSV schema have never had a Gold-cost column
   (only Wood/Stone) for any age, and adding one for Durg alone would be a schema change
   this item didn't need — flagged rather than silently added. 6 new EditMode tests
   (`AgeProgressionDurgTests.cs`, 221 total, up from 215, all pass): `NextAge` from
   Classical/Durg resolves correctly, `Advance`/`CurrentAge` round-trip through Durg, and
   `AgeProfile.For(AgeId.Durg)`'s every field sits strictly between Classical's and
   Imperial's. Live-verified via UnityMCP through the real production path: a real match
   (`CivilizationSetup.BeginMatch(Maurya)`, which itself starts Player at Classical per
   Maurya's existing bonus), a real Player `TownCenter.RequestAgeUp()` deducted exactly 250
   Wood/150 Stone, ran its real `Update()`-ticked 40s research countdown to completion, and
   left `AgeProgress.CurrentAge(Player)` at `Durg` — then a second real `RequestAgeUp()` from
   Durg correctly targeted `Imperial` (deducted 300 Wood/200 Stone, began progressing). User
   explicitly deferred item 6 (see below) rather than bundling it into this session.
   *Depends on: nothing. Blocks: every "Durg" row in Waves 2-4.*
6. ~~**[S] Age-up building-count requirement.**~~ **Closed (2026-09-04).** Resolved the
   design question flagged when item 5 closed via AskUserQuestion: "2 buildings" means 2
   completed, non-TownCenter buildings currently owned by the faction (not a true per-age
   building taxonomy — that remains materially out of scope for an [S] item), and the gate
   applies from Classical onward only — Ancient→Classical stays cost-only, since a player's
   very first age-up may genuinely only have the starting TownCenter. New
   `Buildings/AgeUpRequirement.cs` (`AppliesTo(AgeId)`, `IsMet(FactionId)`) recomputes fresh
   from `Building.All` every call, same "recompute, don't incrementally track" convention as
   `Population.Cap` — no counter that can drift when a building is destroyed. A building
   still under construction (`ConstructionSite.IsComplete == false`) doesn't count, checked
   generically via `TryGetComponent<ConstructionSite>` rather than a per-type switch (mirrors
   `Market.IsComplete`'s own pattern). Wired into `TownCenter.RequestAgeUp()` as an
   additional gate alongside the existing Wood/Stone cost check, and into `BuildMenu`'s
   Age-up button label/interactability so a blocked player sees "(needs 2 buildings)"
   instead of a silently inert button. 11 new EditMode tests
   (`AgeUpRequirementTests.cs`, 232 total, up from 221, all pass) — hit and worked around
   this project's own documented `Building.OnEnable` timing gotcha (component registration
   into `Building.All` isn't guaranteed synchronous within a single EditMode test method,
   same as `BuildingFootprintTests`/`Unit.All`-using tests already document; fixed by
   registering directly into `Building.All` in the test helpers, matching that existing
   convention, after first hitting real test failures from trusting `AddComponent` alone).
   Live-verified via UnityMCP through the real production path: a real match
   (`CivilizationSetup.BeginMatch(Rajput)`, which starts at Ancient), a real
   `TownCenter.RequestAgeUp()` correctly advanced Ancient→Classical with zero non-TownCenter
   buildings (exempt, as designed); the same real TownCenter then correctly *refused*
   Classical→Durg with zero non-TownCenter buildings owned (no resources spent, `IsAgingUp`
   stayed false); real `HouseFactory.Place`/`BarracksFactory.Place` + `ConstructionSite.
   CompleteImmediately()` then brought the Player to 2 real completed non-TownCenter
   buildings, and the identical `RequestAgeUp()` call immediately succeeded (250 Wood
   deducted, `IsAgingUp` true) — proving the gate reads real, live building state, not just
   a test fixture. *Depends on: item 5 (closed).*

**Wave 1 exit criteria:** a player can reach 4 ages in a match, `AgeProfile.For(AgeId.Durg)`
returns real data, and reaching Durg requires the same kind of building prerequisite as
Classical and Imperial now do. **All three confirmed live — Wave 1 is fully closed
(2026-09-04).**

---

## Wave 2 — The Durg building and Karmashala (structural buildings other systems need)

7. ~~**[M] Durg building.**~~ **Closed (2026-09-04).** New `Buildings/Durg.cs`/
   `DurgFactory.cs` (`TownCenterFactory.cs`/`BarracksFactory.cs` as templates). User-confirmed
   design decisions via AskUserQuestion: it trains unique units (relocated off `Barracks`,
   matching AoE's Castle-trains-uniques convention — `Barracks.RequestTrainUniqueUnit`/
   `UniqueUnitCount`/`UniqueUnitAt`/`UniqueUnit` deleted, `Durg` has its own trimmed-down
   copy), and it's the strongest defensive building in the game: `GarrisonCapacity` 12 (vs.
   TownCenter's 8), 6 max bonus shots (vs. 4), 700 HP/4-6 armor (vs. 500/3-5), 12 dmg/range
   9/1.2s interval (vs. 8/8/1.4) — strictly stronger on every axis, live-confirmed via
   reflection against a real spawned TownCenter. Age-gated to Durg or later
   (`BuildingPlacer.CanPlaceDurg`), 200 Wood/150 Stone, 25s build, hotkey D. Wired through
   the full stack: `BuildingPlacer`'s per-kind switches, `NetBuildKind.Durg`/
   `CommandSerializer`'s `building is Durg` train-dispatch branch (LAN parity with every
   other trainable building), `BuildMenu`'s existing unique-unit buttons re-gated from
   `Barracks` to `Durg` (no new button GameObjects needed there — only the placement button
   itself is new, added via UnityMCP scene editing, not code), `HotkeyOverlay`/
   `SettingsMenu` updated to match. **A real regression was caught and fixed before it
   shipped**: the AI opponent trained its unique unit via `_barracks.RequestTrainUniqueUnit()`
   in `TryTrainSoldiers()` — moving that off Barracks with no AI-side Durg would have
   silently stopped the AI from ever training its unique unit again. Fixed with a
   `TryBuildDurg()`/`AssignDurgBuilderIfNeeded()` pair mirroring `TryBuildBarracks()`'s own
   shape (own `durgOffset`, Durg-age gated), and `TryTrainSoldiers()`'s case 6 now falls back
   to a plain Soldier when the AI's Durg isn't built/complete yet rather than stalling that
   rotation slot. 4 new EditMode tests (`DurgTests.cs`) plus 2 existing unique-unit tests in
   `UniqueUnitsTests.cs` updated to build a `Durg` instead of a `Barracks` (236 total, up
   from 232, all pass) — building factories deliberately not exercised in EditMode tests
   (this project's own documented NRE-outside-Play-mode limitation for
   `BarracksFactory.Place`/`TownCenterFactory.Place`, so `DurgFactory.Place` isn't tested
   there either). **Live-verified via UnityMCP through the real production path, including a
   real environment gotcha worked through, not around**: `BuildMenu`'s new `durgButton`/
   `durgLabel` `[SerializeField]` fields were null in the scene (added to the C# class but
   never wired to a GameObject) — this made `BuildMenu.Update()` NRE on every frame silently
   (zero console errors reported by the MCP console bridge; only reflection-invoking
   `Update()` directly and catching the exception surfaced the real stack trace pointing at
   `SetPlacementButtonsActive`, another instance of this project's own "the console bridge
   can miss real compile/runtime errors" gotcha — check directly when something inexplicably
   doesn't update). Fixed by duplicating `MillButton` into a real `DurgButton` scene object
   via UnityMCP (`manage_gameobject`/`manage_components`) and wiring it to the component's
   fields, not a code workaround. After that fix: a real match
   (`CivilizationSetup.BeginMatch(Maurya)`), `BuildingPlacer.CanPlaceDurg` false pre-Durg-age
   and true after `AgeProgress.Advance(..., AgeId.Durg)`; a real `DurgFactory.Place` +
   `ConstructionSite.CompleteImmediately()` Durg spawned with the exact stats above (via
   reflection); selecting a real `Barracks` showed Soldier/Archer/etc. buttons with the
   unique-unit buttons hidden, selecting the real `Durg` showed the reverse (`Train Maurya
   War Elephant (130 Food, 100 Gold)`/`Train Pillar Edict Scholar (40 Food, 10 Gold)` labels
   resolved correctly, civ-specific); clicking the real 2nd unique-unit button's `onClick`
   correctly enqueued through `CommandBus` (deferred, matching this project's lockstep
   input-delay convention — zero immediate spend, confirmed the delayed spend/train a couple
   of ticks later: Food 1000→960, Gold 1000→990, matching Pillar Edict Scholar's cost
   exactly); clicking the real `durgButton`'s `onClick` correctly entered
   `BuildingPlacer.IsPlacing`. **Flagged, not fixed (asset gap, not a bug)**: `Durg` has no
   bespoke 3D model yet, so `BuildingModelFactory.Spawn` falls back to its generic
   procedural shape, same disclosed placeholder convention as Lumber Camp/Mining Camp/Mill
   before their models existed — needs real art sourced later, same as every other
   "(asset-blocked)" item in this roadmap. See `docs/SESSION_LOG.md`'s matching entry for
   full detail.
8. ~~**[M] Karmashala (Blacksmith-equivalent building).**~~ **Closed (2026-09-04) — this
   closes Wave 2.** New `Buildings/Karmashala.cs`/`KarmashalaFactory.cs`
   (`MillFactory.cs` as the factory template, `Barracks.cs`'s own flat Attack/Armor
   research code as the component template). User-confirmed design decisions via
   AskUserQuestion: gated to Classical Age (same as Barracks, not a late-game unlock like
   Durg), and the flat Attack/Armor tracks (`RequestResearchAttack`/`RequestResearchArmor`
   and their supporting state) move OFF Barracks onto Karmashala entirely — mirrors how
   Durg took unique-unit training off Barracks last session. Barracks' per-class
   Attack/Armor tracks (item 40, never wired to any UI button) and civ UniqueTech stay on
   Barracks/Durg untouched, out of scope. 150 Wood only (no Stone/Gold, matching AoE2's
   real Blacksmith cost), 10s build, 220 HP/1-2 armor (deliberately "raidable," between
   Mill's 200 HP and Barracks' 300), 3-tile footprint (Market-sized). Wired through the
   full stack: `BuildingPlacer`'s per-kind switches (`CanPlaceKarmashala`), `NetBuildKind.Karmashala`
   (build-only — research itself was already un-networked before this session, a
   pre-existing gap not fixed here), `BuildMenu`'s `attackUpgradeButton`/`armorUpgradeButton`
   re-gated from a selected Barracks to a selected Karmashala (new `UpdateKarmashalaButtons`,
   split out of `UpdateBarracksButtons` the same way `UpdateDurgButtons` split out
   unique-unit training last session) plus one genuinely new placement button
   (`karmashalaButton`, wired via UnityMCP scene editing — same gotcha as Durg's, see
   below), `SettingsMenu`/`HotkeyOverlay` updated to match (new `KarmashalaGroup`,
   mirroring `DurgGroup`'s own split from `BarracksGroup`). **A real regression was caught
   and fixed before it shipped, same class of bug Durg's session hit**: the AI opponent
   researched Attack/Armor via `_barracks.RequestResearchAttack()/RequestResearchArmor()`
   in `TryResearchUpgrades()` — moving those off Barracks with no AI-side Karmashala would
   have silently ended the AI's flat Attack/Armor research forever. Fixed with a
   `TryBuildKarmashala()`/`AssignKarmashalaBuilderIfNeeded()` pair mirroring
   `TryBuildDurg()`'s own shape, and `TryResearchUpgrades()` now checks `_karmashala` first
   (independently of the `_barracks` guard below it) — if the AI has no Karmashala yet, it
   simply skips flat Attack/Armor research for now rather than erroring, same
   "silently no-ops if unavailable" convention `TryTrainSoldiers`' Durg fallback already
   established. 9 new EditMode tests (`KarmashalaTests.cs`, 245 total, up from 236, all
   pass) — building factories deliberately not exercised there, same documented
   NRE-outside-Play-mode limitation as `BarracksFactory.Place`/`DurgFactory.Place`. Hit the
   exact same environment gotcha Durg's session already documented: the new
   `karmashalaButton`/`karmashalaLabel` `[SerializeField]` fields were null in the scene
   (added to the C# class but never wired to a GameObject) — `BuildMenu.Update()` NRE'd
   every frame with zero errors surfaced by the MCP console bridge; only
   reflection-invoking `Update()` directly inside a try/catch surfaced the real stack
   trace pointing at `SetPlacementButtonsActive`. Fixed the same way Durg's session did:
   duplicated `DurgButton` into a real `KarmashalaButton` scene GameObject via UnityMCP and
   wired the component fields to it. Live-verified via UnityMCP through the real
   production path after that fix: a real match (`CivilizationSetup.BeginMatch(Chola)`,
   which starts at Ancient — Maurya's own Classical-start convention would have made the
   Ancient-age gate check meaningless), `CanPlaceKarmashala` false in Ancient/true in
   Classical, a real spawned Karmashala + a real spawned Barracks selected in turn showed
   exactly the right button sets (`attackUpgradeButton`/`armorUpgradeButton` active only on
   Karmashala, `soldierButton` active only on Barracks), a real button click on
   `attackUpgradeButton` deducted Gold and started research through the real
   `RequestResearchAttack` path, a real `karmashalaButton` click correctly entered
   `BuildingPlacer.IsPlacing`, and the AI's own `TryBuildKarmashala`/
   `AssignKarmashalaBuilderIfNeeded`/`TryResearchUpgrades` chain built a real Karmashala,
   deducted Wood, then deducted Gold and started research once complete — all through the
   real production path, not test shortcuts. **Flagged, not fixed**: `Karmashala` has no
   bespoke 3D model yet (falls back to the generic procedural shape, same as Durg/Lumber
   Camp/Mining Camp/Mill before their models existed) — needs real art sourced later.

**Wave 2 exit criteria met (2026-09-04):** the two buildings every later unique-unit and
upgrade-line item implicitly assumes exist, actually exist. **This closes Wave 2.**

---

## Wave 3 — Upgrade ladders (one line per session, strict)

Do NOT batch multiple unit lines into one session — the spec workbook's Upgrade Lines sheet
exists specifically because each line is its own scoped decision (tier count, age placement,
naming). Order below goes cheapest/most-valuable first; re-order freely, but keep it
one-line-per-session.

9. ~~**[S] Infantry line (5 tiers).**~~ **Closed (2026-09-04).** Padati (tier 0, the
   existing flat Soldier, no research needed) → Senani → Khandayata → Maha Khandayata → Vir
   Yodha, gated Ancient/Classical/Durg/Imperial/Imperial. Resolved 3 design decisions via
   AskUserQuestion before coding: research happens on Barracks (not Karmashala - it upgrades
   what Barracks itself trains, matching AoE II's own convention), each tier renames the
   spawned unit and improves stats but reuses the existing Human Character Dummy model (no
   new art), and progress is NOT retroactive - matches every other progression system in this
   project (UpgradeProgress/AgeProfile/CivilizationProfile all "baked in at spawn"). New
   `Progression/InfantryLineProgress.cs` (`InfantryTierData` struct table: name/required
   age/HP bonus/damage bonus/gold+wood cost/research time per tier - hardcoded, same
   "intentionally hardcoded" reasoning as `AgeProfile`/`UpgradeProgress`), a new independent
   research track on `Barracks.cs` (`RequestResearchInfantryTier`/
   `IsResearchingInfantryTier`/`InfantryTierResearchProgress`, same non-blocking shape as
   every other Barracks research track), and `SoldierFactory.cs` now reads
   `InfantryLineProgress.Current(faction)` at spawn to bake in the current tier's name/HP/
   damage bonus - genuinely "tier 1 of a ladder," not a flat unit. **Deliberately did NOT
   need any CommandBus/NetBuildKind/CommandSerializer wiring**: unlike Durg/Karmashala, this
   item adds no new placeable building and no new trainable unit type - Barracks' existing
   `RequestTrain()`/training queue slot is unchanged, so the same "Train Soldier" button and
   network path already used continues to work untouched; only what gets baked in at spawn
   changes. New `infantryTierButton`/`infantryTierLabel` in `BuildMenu.cs` (gated on a
   selected Barracks, same "Researching.../(Max Tier)/Upgrade to X" shape as
   `UpdateUpgradeButton`, plus an age-gate branch showing "(needs Durg Age)" when the next
   tier's age requirement isn't met yet), hotkey I (`ResearchInfantryTier`), wired into
   `SettingsMenu`/`HotkeyOverlay`'s existing `BarracksGroup`. **Explicitly out of scope, not
   a regression**: no AI-side research hook was added (unlike Durg/Karmashala's sessions,
   nothing existing broke by adding this - the AI simply won't research Infantry tiers yet,
   same "never wired" status this project's own per-class Attack/Armor tracks already have -
   a real future balance-work item, not a bug). 10 new EditMode tests
   (`InfantryLineTests.cs`, 255 total, up from 245, all pass): `InfantryLineProgress` gating/
   sequencing, `Barracks.RequestResearchInfantryTier`'s cost/age-gate/already-researching/
   max-tier guards, and `SoldierFactory` baking the current tier in at spawn without
   retroactively changing an already-spawned unit. **Hit the same environment gotcha Durg/
   Karmashala's sessions already documented**: the new `infantryTierButton`/
   `infantryTierLabel` `[SerializeField]` fields were null in the scene - fixed the same way,
   duplicating `UniqueTechButton` into a real `InfantryTierButton` scene object via UnityMCP
   and wiring the component fields to it (confirmed via reflection before live-testing).
   Live-verified via UnityMCP through the real production path: a real match
   (`CivilizationSetup.BeginMatch(Maurya)`, which starts at Classical per Maurya's own
   bonus), the age gate correctly false for the Enemy faction (still Ancient) and true for
   Player; a real `Barracks.RequestResearchInfantryTier()` deducted exactly 100 Gold/50 Wood
   and started research; forcing the real `Update()` tick to complete it (reflection-set
   `_infantryTierResearchRemaining` near zero, then invoked the real private `Update()`
   method - not a test shortcut) advanced `InfantryLineProgress.Tier` to 1 and left the
   research flag correctly cleared; a Soldier spawned via `SoldierFactory.Spawn` *before* that
   completion stayed "Maurya Padati" at 33 HP throughout (not retroactive, confirmed by
   re-reading the same GameObject's HP after the tier advanced), while one spawned *after*
   came out "Maurya Senani" at 41.8 HP; the real `infantryTierButton`'s label correctly read
   "Upgrade to Khandayata (needs Durg Age)" while Player was Classical, and after advancing
   Player to Durg via `AgeProgress.Advance` the same button's real `onClick.Invoke()`
   correctly called through to `RequestResearchInfantryTier` and deducted the real Khandayata
   cost (150 Gold/75 Wood). **Found, not fixed (real pre-existing bug, unrelated to this
   item's own diff)**: while live-testing, selecting a real Maurya Barracks and driving
   `BuildMenu.Update()` threw `KeyNotFoundException` inside `UniqueTechDefinition.For` -
   `UniqueTechDefinition.cs`'s `Bonuses` dictionary only has entries for Chola/Vijayanagara/
   Rajput, not Maurya/Maratha, so `BuildMenu.UpdateUniqueTechButton` (called every frame for
   any selected Barracks) throws for those 2 civs specifically. Not this item's bug to fix
   (pre-existing, untouched by this session's diff) - flagged via `spawn_task`
   (`task_55dbb0cc`) for a dedicated follow-up rather than left silently unnoticed; isolated
   my own new `UpdateInfantryTierButton` logic by invoking it directly via reflection instead
   of through the crashing `Update()` to keep this item's own verification clean. *Depends
   on: Wave 0 items 1-2, Wave 1.*
10. ~~**[S] Spearman line (3 tiers).**~~ **Closed (2026-09-04).** Bhaladhari (tier 0, the
    existing flat Spearman, no research needed) → Trishuladhari → Maha Trishuladhari, gated
    Classical/Durg/Imperial. Mirrored item 9's (Infantry line) exact shape, since this item's
    own roadmap text left no open design questions the way item 9's did: new
    `Progression/SpearmanLineProgress.cs` (`SpearmanTierData` struct table, same
    "intentionally hardcoded" reasoning), a new independent research track on `Barracks.cs`
    (`RequestResearchSpearmanTier`/`IsResearchingSpearmanTier`/`SpearmanTierResearchProgress`,
    same non-blocking shape alongside the existing Infantry tier track - a Barracks can
    research both tracks at once, same as every other pair of independent tracks there), and
    `SpearmanFactory.cs` now reads `SpearmanLineProgress.Current(faction)` at spawn to bake in
    the current tier's name/HP/damage bonus, not retroactive. Tier costs/bonuses scaled off
    Infantry's own established curve at matching age gates (Trishuladhari mirrors Khandayata's
    Durg-gate growth: +18 HP/+4 damage, 120 Gold/60 Wood, 25s; Maha Trishuladhari mirrors Maha
    Khandayata's Imperial-gate growth: +30 HP/+6 damage, 200 Gold/100 Wood, 40s) - a deliberate
    consistency choice, not independently balanced. New `spearmanTierButton`/
    `spearmanTierLabel` in `BuildMenu.cs` (identical "Researching.../(Max Tier)/Upgrade to X
    (needs Y Age)/Upgrade to X (Gold, Wood)" shape as `UpdateInfantryTierButton`, gated on a
    selected Barracks), hotkey L (`ResearchSpearmanTier` - avoided every hotkey already used
    within the Barracks-selected context), wired into `SettingsMenu`/`HotkeyOverlay`'s existing
    `BarracksGroup`. Same explicitly-out-of-scope call as item 9: no AI-side research hook
    added (the AI simply won't research Spearman tiers yet - a future balance-work item, not a
    regression). 10 new EditMode tests (`SpearmanLineTests.cs`, mirroring `InfantryLineTests.cs`
    exactly - 269 total including these, all pass; the full suite's prior count had already
    drifted past CLAUDE.md's stale 255 figure before this session started, from work this
    session didn't do - not re-verified here beyond confirming the current 269 all pass). Hit the same environment gotcha every Wave 2/3
    session before this one has documented: the new `spearmanTierButton`/`spearmanTierLabel`
    `[SerializeField]` fields were null in the scene - fixed the same way, duplicating
    `InfantryTierButton` into a real `SpearmanTierButton` scene object via UnityMCP and wiring
    the component fields to it (confirmed via reflection before live-testing). Live-verified via
    UnityMCP through the real production path: a real match
    (`CivilizationSetup.BeginMatch(Rajput)`, deliberately not Maurya/Maratha - see item 9's own
    flagged `UniqueTechDefinition` bug, `task_55dbb0cc`, still open and unrelated to this item -
    to keep this item's own `BuildMenu.Update()` calls crash-free), a real spawned+completed
    Barracks; `RequestResearchSpearmanTier()` correctly refused at Ancient age even with 1000
    Gold/Wood on hand, then correctly deducted exactly 120 Gold/60 Wood and started research
    once advanced to Durg; forcing the real `Update()` tick to complete it (reflection-set
    `_spearmanTierResearchRemaining` near zero, then invoked the real private `Update()` method)
    advanced `SpearmanLineProgress.Tier` to 1; a Spearman spawned via `SpearmanFactory.Spawn`
    after that came out "Rajput Trishuladhari" at 70.0925 HP (base 35 + 18 bonus, scaled by
    Rajput's own profile/age multipliers); the real `spearmanTierButton`'s label correctly read
    "Upgrade to Maha Trishuladhari (needs Imperial Age)" while Player was Durg (button
    non-interactable), and after advancing Player to Imperial the same button's real
    `onClick.Invoke()` correctly called through to `RequestResearchSpearmanTier` and deducted
    the real Maha Trishuladhari cost (200 Gold/100 Wood), matching the button's own displayed
    label exactly. *Depends on: Wave 0, Wave 1.*
11. ~~**[S] Archer line (3 tiers).**~~ **Closed (2026-09-04).** Dhanurdhara (base) →
    Yantra Dhanurdhara (Durg) → Maha Dhanurdhara (Imperial). New
    `Progression/ArcherLineProgress.cs` mirrors SpearmanLineProgress.cs exactly (same
    Classical/Durg/Imperial gate shape, same "baked in at spawn, not retroactive"
    convention); tier bonuses/costs reuse SpearmanLineProgress's own values at
    matching age gates (Yantra Dhanurdhara = Trishuladhari's Durg-gate growth: +18
    HP/+4 dmg/120 Gold/60 Wood/25s; Maha Dhanurdhara = Maha Trishuladhari's
    Imperial-gate growth: +30 HP/+6 dmg/200 Gold/100 Wood/40s). New independent
    research track on `Barracks.cs` (`RequestResearchArcherTier`, non-blocking
    alongside Infantry/Spearman tracks), `ArcherFactory.cs` now reads
    `ArcherLineProgress.Current(faction)` at spawn. New `archerTierButton`/
    `archerTierLabel` in `BuildMenu.cs` (identical shape), hotkey H, wired into
    `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope
    call as items 9/10: no AI-side research hook. 10 new EditMode tests
    (`ArcherLineTests.cs`, mirroring `SpearmanLineTests.cs` - 279 total, all pass).
    Hit the same environment gotcha every Wave 2/3 session has documented (new
    `[SerializeField]` fields null in the scene) - fixed by duplicating
    `SpearmanTierButton` into a real `ArcherTierButton` scene object via UnityMCP.
    Live-verified via UnityMCP through the real production path: a real match
    (`CivilizationSetup.BeginMatch(Rajput)`), a real `RequestResearchArcherTier()`
    correctly refused at Ancient age even with 1000 Gold/Wood on hand, then deducted
    exactly 120 Gold/60 Wood at Durg; forcing the real `Update()` tick advanced the
    tier and an Archer spawned afterward came out "Rajput Yantra Dhanurdhara" at
    47.61 HP; the real button's label and `onClick.Invoke()` correctly showed and
    paid the Maha Dhanurdhara cost (200 Gold/100 Wood) once Player reached Imperial.
    *Depends on: Wave 0, Wave 1.*
12. ~~**[S] Knight/Cavalry line (3 tiers).**~~ **Closed (2026-09-04).** Ashvarohi (tier 0,
    the existing flat Cavalry, no research needed) → Maha Ashvarohi → Vir Ashvarohi, gated
    Durg/Imperial/Imperial. Mirrored item 11's (Archer line) exact shape - no new design
    decisions needed, since this item's own roadmap text already fixes tier count/names/
    ages and the mechanism (research on Barracks, baked in at spawn, not retroactive) was
    already established by items 9-11 in the same wave. Confirmed by re-reading
    `SpearmanLineProgress`/`InfantryLineProgress` that tier 0's `RequiredAge` field is
    descriptive only (never enforced - `NextTierAgeRequirementMet` only checks the *next*
    tier), so this item does not change when Cavalry itself becomes trainable
    (`RequestTrainCavalry` still has no age gate, as before). New
    `Progression/CavalryLineProgress.cs` mirrors `SpearmanLineProgress.cs`'s shape exactly
    (same Durg/Imperial gate pattern, with the last two tiers sharing the Imperial gate per
    this item's own spec); tier bonuses/costs reuse `InfantryLineProgress`'s own
    back-to-back Imperial pair (Maha Khandayata → Vir Yodha) at matching gates, the closest
    existing precedent for two successive Imperial-gated tiers, rather than independently
    balanced (Maha Ashvarohi = Maha Khandayata's growth: +30 HP/+6 dmg/200 Gold/100
    Wood/40s; Vir Ashvarohi = Vir Yodha's growth: +45 HP/+9 dmg/250 Gold/125 Wood/50s). New
    independent research track on `Barracks.cs` (`RequestResearchCavalryTier`, runs
    alongside the existing Infantry/Spearman/Archer tier tracks without blocking them),
    `CavalryFactory.cs` now reads `CavalryLineProgress.Current(faction)` at spawn. New
    `cavalryTierButton`/`cavalryTierLabel` in `BuildMenu.cs` (identical shape to
    `archerTierButton`/`spearmanTierButton`), hotkey M, wired into
    `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope call as
    items 9-11: no AI-side research hook (future balance work, not a regression); Rajput
    Royal Guard (the civ-unique cavalry alternative) untouched, as the roadmap text
    specifies. 10 new EditMode tests (`CavalryLineTests.cs`, mirroring
    `SpearmanLineTests.cs`). **Live UnityMCP verification follow-up closed (2026-09-04)**,
    a separate session once UnityMCP reconnected: the predicted `cavalryTierButton`/
    `cavalryTierLabel` `[SerializeField]` null-in-scene gotcha was confirmed and fixed
    (duplicated `ArcherTierButton` into a real `CavalryTierButton` scene object, wired
    the component fields). Full EditMode suite (289/289) passes. Live-verified via
    UnityMCP through a real match (`CivilizationSetup.BeginMatch(Rajput)`): the age gate
    correctly refused research at both Ancient and Durg (this line's tier 1 gates on
    Imperial, not Durg), a real `RequestResearchCavalryTier()` deducted exactly 200
    Gold/100 Wood at Imperial, a trained Cavalry came out "Rajput Maha Ashvarohi" at the
    boosted HP, and the real scene button's label/`onClick.Invoke()` correctly paid tier
    2's cost (Vir Ashvarohi, 250 Gold/125 Wood). *Depends on: Wave 0, Wave 1.*
13. ~~**[S] Elephant line (2 tiers, our own invention — no AoE II line to import).**~~
    **Closed (2026-09-05).** Gajaroha (tier 0, the existing flat War Elephant, no
    research needed) → Maha Gajaroha, gated Durg/Imperial. Two design decisions
    resolved via AskUserQuestion before coding: (1) ONE shared ladder for both War
    Elephant civs (Maurya, Vijayanagara) rather than two independently-tuned ones —
    matches every other tier line's own precedent (a single table read by whichever
    faction trains that unit); each civ's own CivilizationProfile/AgeProfile
    multipliers plus each factory's own already-distinct base stats/model still
    differentiate the two outcomes, same as today; (2) this line doubles as item 16's
    planned "Elite tier" for these same two factories rather than stacking a second
    Durg→Imperial upgrade on top — item 16's own 7-unit list is now 5 (see below). New
    `Progression/ElephantLineProgress.cs` mirrors `CavalryLineProgress.cs`'s shape
    (single Imperial-gated step); tier bonus/cost reuses the same +30 HP/+6 dmg/200
    Gold/100 Wood/40s growth every other line's own first Imperial-gate step already
    uses, not independently balanced. Research lives on **Durg**, not Barracks — the
    one line that deviates from every prior tier line's convention, because War
    Elephants train from Durg (`UniqueUnitDefinition.Spawn` via
    `Durg.RequestTrainUniqueUnit`), not Barracks: new `Durg.RequestResearchElephantTier`/
    `IsResearchingElephantTier`/`ElephantTierResearchProgress`, and a new
    `Durg.TrainsElephant` property (checks `UniqueUnitDefinition.UnitId` against
    `ElephantLineProgress.IsElephantUnitId` — a new `UnitId` field added to
    `UniqueUnitDefinition` for this — so Chola/Rajput/Maratha's Durg never shows a
    button that would do nothing) gates the new `elephantTierButton`/
    `elephantTierLabel` in `BuildMenu.cs` (hotkey R, wired into
    `SettingsMenu`/`HotkeyOverlay`'s `DurgGroup`). `MauryaWarElephantFactory.cs`/
    `VijayanagaraWarElephantFactory.cs` both read `ElephantLineProgress.Current(faction)`
    at spawn, same "baked in at spawn, not retroactive" convention as every other line.
    13 new EditMode tests (`ElephantLineTests.cs`, 302 total, all pass). Live-verified
    via UnityMCP through the real production path: a real match
    (`CivilizationSetup.BeginMatch(Maurya)`), a real `DurgFactory.Place` +
    `ConstructionSite.CompleteImmediately()` Durg confirmed `TrainsElephant=true`, the
    age gate correctly refused research at both Classical and Durg (this line's tier 1
    gates on Imperial, not Durg) with zero deduction, then at Imperial deducted exactly
    200 Gold/100 Wood and completed via a forced real tick; a War Elephant trained
    through the real `Durg.RequestTrainUniqueUnit()` → `TickTraining()` path (slot 0)
    spawned as "Maurya Maha Gajaroha" at 156 HP; the real scene button (duplicated from
    `CavalryTierButton`, the same recurring null-in-scene gotcha, fixed the same way)
    correctly showed "Elephant (Max Tier)" once selected on the real Durg via
    `SelectionManager`; a second real Durg built for a Rajput-assigned faction
    confirmed the button correctly stays hidden (`TrainsElephant=false`) for a civ with
    no elephant. *Depends on: Wave 0, Wave 1.*
14. ~~**[S] Mangonel/Siege line (3 tiers).**~~ **Closed (2026-09-05).** Shilakshepaka
    (tier 0, the existing flat Siege, no research needed) → Maha Shilakshepaka → Vajra
    Shilakshepaka, gated Durg/Imperial/Imperial. No new design decisions needed - this
    item's own roadmap text already fixes tier count/names/ages, and the mechanism
    (research on Barracks, baked in at spawn, not retroactive, back-to-back Imperial-gated
    last two tiers) exactly mirrors `CavalryLineProgress.cs`'s own shape (item 12). New
    `Progression/SiegeLineProgress.cs` mirrors `CavalryLineProgress.cs` exactly; tier
    bonuses/costs reuse the same growth every other line's own back-to-back Imperial pair
    already uses (Cavalry's Maha Ashvarohi → Vir Ashvarohi values: +30 HP/+6 dmg/200
    Gold/100 Wood/40s, then +45 HP/+9 dmg/250 Gold/125 Wood/50s), not independently
    balanced. New independent research track on `Barracks.cs`
    (`RequestResearchSiegeTier`, runs alongside the existing Infantry/Spearman/Archer/
    Cavalry tier tracks without blocking them), `SiegeFactory.cs` now reads
    `SiegeLineProgress.Current(faction)` at spawn. New `siegeTierButton`/`siegeTierLabel`
    in `BuildMenu.cs` (identical shape to `cavalryTierButton`), hotkey O, wired into
    `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope call as
    items 9-13: no AI-side research hook (future balance work, not a regression). 10 new
    EditMode tests (`SiegeLineTests.cs`, mirroring `CavalryLineTests.cs` - 312 total, all
    pass). Hit the same recurring "new `[SerializeField]` null in the scene" gotcha every
    Wave 2/3 session has hit - fixed the same way, duplicating `CavalryTierButton` into a
    real `SiegeTierButton` scene object via UnityMCP. Live-verified via UnityMCP through
    the real production path: a real match (`CivilizationSetup.BeginMatch(Maurya)`), a
    real Barracks (`BarracksFactory.Place` + `ConstructionSite.CompleteImmediately()`),
    the age gate correctly refused research at Durg (this line's tier 1 gates on
    Imperial, not Durg) with zero deduction, then at Imperial deducted exactly 200
    Gold/100 Wood via the real scene button's `onClick.Invoke()`; forcing the real tick
    advanced the tier and a Siege unit trained afterward came out "Maurya Maha
    Shilakshepaka" at 96 HP; the real scene button's label correctly showed "Siege (Max
    Tier)" once both tiers were researched. *Depends on: Wave 0, Wave 1.*
15. ~~**[S] Galley/Naval line (3 tiers).**~~ **Closed (2026-09-05).** Rana Nauka
    (tier 0, the existing flat War Galley, no research needed) → Maha Rana Nauka →
    Samrat Nauka, gated Classical/Durg/Imperial. No new design decisions needed - this
    item's own roadmap text already fixes tier count/names/ages, and the mechanism
    exactly mirrors `ArcherLineProgress.cs`'s own shape (item 11, same 3-tier
    Classical/Durg/Imperial gate pattern). New `Progression/NavalLineProgress.cs`
    mirrors `ArcherLineProgress.cs` exactly; tier bonuses/costs reuse
    `ArcherLineProgress`'s own values at matching age gates (+18 HP/+4 dmg/120
    Gold/60 Wood/25s at Durg, +30 HP/+6 dmg/200 Gold/100 Wood/40s at Imperial), not
    independently balanced. Unlike every land-line item, research lives on **Dock**,
    not Barracks - the same "research lives where the unit trains" deviation item 13's
    Elephant line already established for Durg, since War Galley trains from Dock
    (`Dock.RequestTrainWarGalley`), not Barracks. New `Dock.RequestResearchNavalTier`/
    `IsResearchingNavalTier`/`NavalTierResearchProgress`/`TickNavalTierResearch` mirror
    Barracks' own tier-research shape exactly. `WarGalleyFactory.cs` now reads
    `NavalLineProgress.Current(faction)` at spawn (previously always named the unit
    literal "War Galley" - now `"{civ} {tier.Name}"`, matching every other tier-ladder
    factory) and adds the tier's HP/damage bonus on top of the existing
    `UpgradeProgress`/civ-profile scaling. New `navalTierButton`/`navalTierLabel` in
    `BuildMenu.cs` (identical shape to `siegeTierButton`, gated on a selected Dock
    instead of Barracks), hotkey X (every other letter already claimed across the
    project's contextual hotkey map - checked directly via `GameSettings.GetKey`
    call sites before picking it), wired into `SettingsMenu`/`HotkeyOverlay`'s existing
    `DockGroup`. Same explicitly-out-of-scope call as items 9-14: no AI-side research
    hook (future balance work, not a regression); Chola's separate War Galley-adjacent
    unique unit (Naval Raider) is untouched, out of scope. 10 new EditMode tests
    (`NavalLineTests.cs`, mirroring `SiegeLineTests.cs` - 322 total, all pass). Hit and
    fixed the same recurring "new `[SerializeField]` null in the scene" gotcha every
    Wave 2/3 session has hit (duplicated `WarGalleyButton` into a real
    `NavalTierButton` scene object via UnityMCP). Live-verified via UnityMCP through
    the real production path: a real match (`CivilizationSetup.BeginMatch(Chola)`), a
    real `DockFactory.Place`-spawned Dock selected via `SelectionManager`, the age gate
    correctly refused research at Ancient with 1000 Gold/Wood on hand (zero deduction),
    then at Durg deducted exactly 120 Gold/60 Wood and completed via a forced real
    tick, a War Galley trained afterward through the real `WarGalleyFactory.Spawn` path
    came out "Chola Maha Rana Nauka" at 72.45 HP, the real scene button's own
    `onClick.Invoke()` deducted exactly 200 Gold/100 Wood at Imperial for the second
    tier, and after that tier completed a War Galley spawned "Chola Samrat Nauka" at
    90 HP with the real button's label settling on "Naval (Max Tier)" and
    `interactable=false`. *Depends on: Wave 0, Wave 1.*
16. ~~**[M] Unique-unit Elite tier (2 tiers × 5 units — was 7, see item 13).**~~
    **Closed (2026-09-05).** Every existing unique unit EXCEPT
    `MauryaWarElephantFactory`/`VijayanagaraWarElephantFactory`
    (`CholaNavalRaiderFactory`, `RajputRoyalGuardFactory`, `PillarEdictScholarFactory`,
    `MarathaMavlaRaiderFactory`, `MarathaDurgGarrisonFactory`) gets a Durg → Imperial
    elite step, matching AoE's rule that *every* unique unit gets exactly one elite
    upgrade. The two War Elephant factories are excluded — item 13's own
    Gajaroha → Maha Gajaroha ladder (closed 2026-09-05) already IS their one
    Durg→Imperial elite step; user-confirmed resolution, not stacking a second one.
    New `Progression/UniqueUnitEliteProgress.cs` keyed by unitId (not CivilizationId,
    since these 5 units are genuinely civ-exclusive, unlike the shared ladders every
    other Wave 3 item reads) - single Imperial-gated step per unitId reusing the same
    +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other line's own first
    Imperial-gate step already uses. Research lives on **Durg**, not Barracks - same
    "research where the unit trains" deviation item 13 already established, since
    every one of these 5 units trains from Durg. New
    `Durg.RequestResearchEliteTier(slot)`/`TrainsEliteEligible(slot)`/
    `IsResearchingEliteTier(slot)`/`EliteTierResearchProgress(slot)` - two independent
    per-slot tracks (Maurya and Maratha each have 2 unique-unit slots) gated by
    `UniqueUnitEliteProgress.IsEligible(unitId)` at that slot, not the slot index
    itself - Maurya's slot 0 (War Elephant) is NOT eligible, only slot 1 (Pillar
    Edict Scholar) is; Maratha's both slots are. New `eliteTierButton`/
    `eliteTierButton2` in `BuildMenu.cs` (hotkeys F/G, reused from other mutually-
    exclusive contexts per this file's own established convention), wired into
    `SettingsMenu`/`HotkeyOverlay`'s `DurgGroup`. Same explicitly-out-of-scope call
    as every other tier line: no AI-side research hook. 16 new EditMode tests
    (`UniqueUnitEliteTests.cs`, 351 total, all pass). **Hit a real concurrent-session
    file collision mid-session, not silently worked around**: `BuildMenu.cs` was
    being actively edited by a separate session building Wave 4 item 18 (Scout) at
    the same time - per this project's own "single-session discipline" gotcha,
    stopped and asked the user before touching that file further, reverted the one
    edit already made, and waited until the other session's changes stabilized
    before resuming (`Barracks.cs`/`SettingsMenu.cs`/`HotkeyOverlay.cs` were also
    concurrently dirty). Live-verified via UnityMCP through the real production
    path: a real match (`CivilizationSetup.BeginMatch(Maratha)`), a real
    `DurgFactory.Place`-spawned Durg confirmed both slots elite-eligible, the age
    gate correctly deducted exactly 200 Gold/100 Wood per slot at Imperial for both
    tracks running concurrently, forced real ticks completed both and a Mavla
    Raider/Durg Garrison trained afterward through the real factory paths came out
    "Maratha Maha Mavla Raider"/"Maratha Maha Durg Garrison" at correctly boosted
    HP, the real scene buttons correctly showed "Elite (Max Tier)"/non-interactable
    once maxed; a second real Durg (Maurya) confirmed slot 0 (War Elephant)
    ineligible/slot 1 (Pillar Edict Scholar) eligible exactly as designed; a third
    real Durg (Rajput) confirmed the real scene button's own `onClick.Invoke()`
    deducted the exact cost and started research for its single eligible slot,
    with slot 2 correctly hidden/ineligible. *Depends on:
    Wave 0, Wave 1, Wave 2 item 7 (thematically the Durg building is where "you can now train
    the base-tier unique unit" makes sense, even if the elite upgrade itself researches
    elsewhere).*
17. ~~**[S] Blacksmith-style stat upgrade steps.**~~ **Closed (2026-09-05).** Extend
    `UpgradeProgress.cs` from 2 research steps to 3 (Classical/Durg/Imperial), and
    wire it to the new Karmashala building. Investigation first: `UpgradeProgress.MaxTier`
    was already 3 (a prior session's own value), but NONE of the 3 tiers were age-gated
    at all — a Karmashala existing (Classical Age minimum, per its own build gate) was
    enough to research all 3 tiers back-to-back in the same Age, purely gold-limited.
    New `UpgradeProgress.TierRequiredAges` (`{Classical, Durg, Imperial}`, indexed by
    current tier) plus `NextAttackTierRequiredAge`/`NextArmorTierRequiredAge`/
    `NextAttackTierAgeRequirementMet`/`NextArmorTierAgeRequirementMet` — same
    "array indexed by current tier" + ordinal `AgeId` comparison shape every other
    tier line's own `NextTierAgeRequirementMet` already uses. `Karmashala.
    RequestResearchAttack`/`RequestResearchArmor` both gained the age check alongside
    their existing cost/already-researching guards. `BuildMenu.cs`'s shared
    `UpdateUpgradeButton` (used by both Attack/Armor, unlike every other tier line's
    own dedicated `Update*TierButton` method) gained the same "Upgrade to X (needs Y
    Age)" branch every other tier button already has, via 2 new parameters
    (`ageRequirementMet`/`requiredAge`) rather than a rewrite. **Found and fixed one
    real edge-case bug before it shipped**: `NextAttackTierRequiredAge`/
    `NextArmorTierRequiredAge` are evaluated unconditionally by `BuildMenu`'s call
    site (C# evaluates all arguments before a method call, and `UpdateUpgradeButton`'s
    own early-return on `!hasNextTier` happens too late to matter) — at max tier this
    would index `TierRequiredAges[3]` on a 3-element array and throw
    `IndexOutOfRangeException` every frame once any track was maxed; fixed by clamping
    the index to the array's last valid tier, with a dedicated regression test
    (`NextAttackTierRequiredAge_DoesNotThrowOnceMaxed`). 7 new/updated EditMode tests
    in `KarmashalaTests.cs` (the 6 pre-existing tests needed `AgeProgress.Initialize`
    added, since they previously relied on the ungated behavior implicitly — 358 total,
    all pass). Live-verified via UnityMCP through the real production path: a real
    match (`CivilizationSetup.BeginMatch(Maurya)`), a real `KarmashalaFactory.Place`
    Karmashala correctly refused tier 1 research at Ancient with zero deduction even
    with 1000 Gold on hand, started tier 1 at Classical (80 Gold), correctly refused
    tier 2 at Classical with zero deduction, started tier 2 at Durg (160 Gold), the
    real scene button's own label correctly showed "Upgrade Attack (needs Imperial
    Age)" with `interactable=false` at Durg for tier 3 and the real
    `onClick.Invoke()` correctly no-op'd (zero deduction), then at Imperial the same
    real button's `onClick.Invoke()` deducted exactly 240 Gold and started tier 3,
    and after tier 3 completed the real button settled on "Attack (Max)" /
    `interactable=false`. *Depends on: Wave 1, Wave 2 item 8.*

**Not in Wave 3 — deferred to Wave 4 because they are NEW units, not upgrades to existing
ones:** Skirmisher, Cavalry Archer, Scout, Camel Rider, Battering Ram, Scorpion, Trebuchet,
Fire Ship, Trader. Keep the "upgrade an existing unit" work separate from the "build a unit
that doesn't exist yet" work — they carry different risk (the first can't break what already
plays fine; the second is new surface area).

**Wave 3 exit criteria:** every currently-flat unit type has a real tier ladder, and the
retroactive-promotion rule (verified in Wave 0) has been exercised 7 times without incident.

---

## Wave 4 — New units surfaced by the AoE tier-chain import

Ordered by a mix of value and dependency, not alphabetically. Each is fully independent of
the others — genuinely parallel-safe once Waves 0-1 are done.

18. ✅ **[M] Scout (Chara), 3-tier line — closed 2026-09-05.** Chara → Vega Ashvarohi → Maha
    Vega Ashvarohi, Ancient/Classical/Durg. Flagged repeatedly across this workbook's history
    as the single most conspicuous missing unit — now that Fog of War is confirmed to exist
    (`FogOfWar/FogOfWarManager.cs`), a scout has real function. First Wave 4 item, done first
    per this item's own recommendation. See `CLAUDE.md`'s "Current status" for full detail:
    new `Progression/ScoutLineProgress.cs` (deliberately grows vision radius/move speed per
    tier instead of HP/damage - "Vega" translates to "speed"), `Combat/ScoutFactory.cs` (first
    live user of `UnitClass.Support` for real combat classification, not just Worker's
    move-speed-multiplier-only use), `Barracks.RequestTrainChara`/
    `RequestResearchCharaTier`, full `BuildMenu`/hotkey (F/G)/`NetTrainKind` wiring, 12 new
    EditMode tests. *Depends on: Wave 0, Wave 1.*
19. ✅ **[S] Skirmisher (anti-archer counter-archer), 2 tiers — closed 2026-09-05.** Pratirodhi
    Dhanurdhara → Maha Pratirodhi Dhanurdhara, Classical/Durg. Completes the counter web's last
    gap - the dedicated anti-archer specialist (see `CombatBonus`: 2x vs Archer, 1.25x received
    from Infantry, both reusing Spearman's own pairing exactly - the closest existing precedent
    for "a unit built to counter one other class," not independently balanced). See
    `CLAUDE.md`'s "Current status" for full detail: new `Combat/UnitClass.Skirmisher`, new
    `Progression/SkirmisherLineProgress.cs` (only 2 tiers total, not 3 like every Wave 3 line -
    tier 1's bonus/cost reuses `ArcherLineProgress`'s own Durg-gate growth exactly), new
    `Combat/SkirmisherFactory.cs` (mirrors `ArcherFactory.cs`'s shape - reuses the shared bow
    model/animation, no dedicated Skirmisher model exists yet, flagged directly),
    `Barracks.RequestTrainSkirmisher`/`RequestResearchSkirmisherTier`, full `BuildMenu`/hotkey
    (C/V)/`NetTrainKind` wiring, 13 new EditMode tests. *Depends on: Wave 0, Wave 1.*
20. ✅ **[S] Battering Ram, 3 tiers — closed 2026-09-05.** Dwarabhanjaka → Maha Dwarabhanjaka →
    Vajra Dwarabhanjaka, Classical/Durg/Imperial. Anti-building only, no splash, garrisonable —
    distinct role from the existing Mangonel-like Siege unit. See `CLAUDE.md`'s "Current status"
    for full detail: new `MeleeAttacker.SetBuildingOnly` (a real hard block, not just a weak
    multiplier - an AttackMove order against a non-Building target is flatly refused),
    `Progression/BatteringRamLineProgress.cs` (mirrors `ArcherLineProgress`'s 3-tier
    Classical/Durg/Imperial shape exactly), `Combat/BatteringRamFactory.cs` (never calls
    `SetSplashRadius` - no splash; adds a `GarrisonPoint` to itself, capacity 4 - the
    "garrisonable" trait, since a Ram hosts friendly units for protection rather than
    garrisoning into a building itself), new `CombatBonus.Multiplier(BatteringRam, Building)`
    at 4x (steeper than Siege's own 3x), `Barracks.RequestTrainBatteringRam`/
    `RequestResearchBatteringRamTier`, full `BuildMenu`/hotkey (D/R)/`NetTrainKind` wiring, 16
    new EditMode tests. *Depends on: Wave 0, Wave 1.*
21. ~~**[S] Cavalry Archer, 2 tiers.** Ashva Dhanurdhara → Maha Ashva Dhanurdhara,
    Durg/Imperial. Mobile ranged raider, fits Rajput/Maratha horse-archer tradition.
    *Depends on: Wave 0, Wave 1.*~~ **Closed (2026-09-05).** See `CLAUDE.md`'s
    "Current status" for full detail.
22. ~~**[S] Camel Rider, 2 tiers (design decision first).** Ushtrarohi → Maha Ushtrarohi,
    Durg/Imperial. Resolve in Plan Mode whether this ships at all before coding — it's listed
    as a design decision, not a confirmed build, in the spec workbook. *Depends on: Wave 0,
    Wave 1, and an explicit yes/no from you.*~~ **Closed (2026-09-05, code+tests only — Unity-side
    steps blocked this session, see below).** See `CLAUDE.md`'s "Current status" for full detail.
23. ~~**[S] Scorpion, 2 tiers.** Bana Yantra → Maha Bana Yantra, Durg/Imperial. `DamageType.Pierce`
    already exists — this needs a raycast-through code path for pass-through damage, not a new
    mechanic type. *Depends on: Wave 0, Wave 1.*~~ **Closed (2026-09-05, code+tests only — Unity-side
    steps blocked this session, see below).** See `CLAUDE.md`'s "Current status" for full detail.
24. **[S] Trebuchet, 1 tier.** Maha Yantra, Imperial only. Long-range anti-building with a
    minimum range; needs a pack/unpack state (two mesh states or a fold animation) — flag the
    animation need to yourself as a possible asset dependency even though the unit itself is
    code-buildable now with a placeholder model. *Depends on: Wave 0, Wave 1.*
25. ~~**[S] Fire Ship, 3 tiers.** Agni Nauka → Maha Agni Nauka → Vega Agni Nauka,
    Classical/Durg/Imperial. First real consumer of `DamageType.Fire` (wired in Wave 0 item 4).
    *Depends on: Wave 0 item 4, Wave 1.*~~ **Closed (2026-09-05).** See `CLAUDE.md`'s
    "Current status" for full detail.
26. **[M] Trader (land + naval), design decision first.** Vanik (land) / Trade Ship (naval).
    Needs a Market-to-Market (and Dock-to-Dock) route system — this is genuinely new economic
    machinery, not just a new unit, so size it as Medium and expect it to touch
    `Buildings/Market.cs` and `Buildings/Dock.cs` as well as a new unit class. *Depends on:
    Wave 0, Wave 1, and a yes/no on whether trade routes are wanted at all — it's listed as a
    "Design decision" on the Economy sheet, not a confirmed build.*
27. **[M] Support units — Vaidya (healer) and Purohita (converter), splitting AoE's Monk.**
    Deliberately two units instead of one so healing can ship without committing to conversion
    mechanics. *Depends on: Wave 0 item 1 (the Support category needs a resolved UnitClass
    first), Wave 2 item 7 or a Monastery-equivalent building — resolve which building houses
    these in Plan Mode.*
28. **[M] Hero unit — Maharaja, per civ.** `UnitCategory.Hero` is declared and unused. Only
    build this if a Regicide-style victory condition is wanted (see Wave 6) — a hero with no
    win-condition consumer is pure cost. *Depends on: Wave 0 item 1, and confirm the victory
    condition it serves BEFORE building 5 hero units.*

**Wave 4 exit criteria:** the roster gap the AoE tier-chain import surfaced (8 units) is
closed, in whatever subset you actually confirmed you want — several items in this wave are
explicitly gated on a design decision, not just an implementation.

---

## Wave 5 — Cross-cutting systems (do once, not per-unit)

These touch *everything* built in Waves 3-4, which is exactly why they belong in their own
wave rather than being retrofitted piecemeal afterward.

29. **[M] Player/team colour system.** The gap you identified directly. AoE colours by PLAYER
    SLOT as a layer on top of civ identity (accent regions only — tunic, shield, roof trim —
    not a full recolour), where we currently colour by CIVILIZATION only
    (`CivilizationProfile.PrimaryColor`). Needs: a `FactionId`-keyed colour assignment, a
    designated team-colour material slot/mask region on unit and building models
    (`GameplayMaterial.cs`, `BuildingModelFactory.cs`), and minimap blips confirmed to read
    per-player rather than per-civ. **Do this in Wave 5, not Wave 6** — every unit built in
    Wave 4 should get its team-colour material slot from the start rather than retrofitted.
    *Depends on: nothing structurally, but sequenced here deliberately so it lands before any
    more new unit art is finalized.*
30. **[L] UI layout re-anchor — bottom bar. Closed (2026-09-05).** From the UI Layout sheet:
    re-anchor `BuildMenu.cs`, `SelectedUnitPanel.cs`, and a slice of `ResourceHUD.cs` into one
    shared bottom-docked root (command panel / info panel / minimap, left to right), matching
    AoE's convention, instead of the prior build-menu-right / resource-panel-top-left split.
    User-confirmed design decisions before implementing (both via AskUserQuestion, resolving
    the item's own two open questions): the "slice" of `ResourceHUD` that moves down is
    Civilization + Population + Age (Wood/Food/Gold/Stone stay as the top-left ticker,
    unchanged); `BuildMenu`'s own internal ~56-button vertical stack stays exactly as-is this
    session — only where it docks changed, per the item's own scope note ("mainly anchor
    code, not because the logic is complex") — see item 32 below for the follow-up that was
    explicitly deferred out of this session. None of the 3 scripts set their own root anchor
    in code (all Inspector/scene data, matching this project's established convention), so
    this was primarily a scene edit via UnityMCP: new `InfoPanel` root (anchor/pivot
    (0.5,0)/(0.5,0), bottom-center) holding `SelectedUnitPanel` (reparented, untouched
    internal layout) with a new `MatchStatus` child stacked above it (holds
    `civLabel`/`populationLabel`/`ageLabel`, reparented out of `ResourceHUD`, repacked to
    sequential rows); `ResourceHUD` itself shrunk to just its 4 remaining rows/repacked;
    `BuildMenu` re-anchored from floating mid-right to bottom-left. `MinimapController` needed
    no change — already bottom-right, already satisfied the target. One small code addition:
    `ResourceHUD.cs` gained a `matchStatusBackground` `[SerializeField] Image` field, wired in
    `Awake()` the same way its existing `background` field already is (reuses
    `panel_resource_bar` art, no new asset needed) — `MatchStatus` lives under a different
    root (`InfoPanel`) so it needs its own background wiring rather than inheriting
    `ResourceHUD`'s. `SelectedUnitPanel.cs`/`BuildMenu.cs` got doc-comment updates only (no
    functional change) describing their new position in the shared bar. No new EditMode tests
    (pure layout change, matching this project's own precedent for prior UI-wiring-only
    sessions — e.g. items 7-9's cursor/skin-wiring sessions); full suite (445/445) confirmed
    unchanged. Live-verified via UnityMCP through the real production path: a real match
    (`CivilizationSetup.BeginMatch(Maurya)`), screenshotted the live HUD confirming the exact
    left-to-right order (BuildMenu / InfoPanel / Minimap) with no overlap, a real selected
    TownCenter showed `SelectedUnitPanel`'s name/status/HP bar correctly stacked directly
    below `MatchStatus`'s Civilization/Population/Age with no clipping, `ResourceHUD.Update()`
    correctly live-updated Food (0→150) at its new tighter top-left ticker and Population
    (4→5) inside `MatchStatus`, and the real `WorkerButton`'s own `onClick.Invoke()` at its
    new bottom-left position correctly routed through `CommandBus`'s lockstep queue (Food
    stockpile unchanged immediately, deducted exactly 50 ~2s later) — hit-testing/raycasting
    unaffected by the re-anchor. Next: item 31 (Age/research readout, explicitly designed to
    bundle with this one) or item 32 (newly flagged below), user's call.
31. **[M] BuildMenu command-panel grid redesign. Closed (2026-09-06).** Item 30's own
    re-anchor kept `BuildMenu`'s internal content exactly as it was — a single tall vertical
    column of 60 stacked text-row buttons, each at a fixed absolute Y position, leaving large
    empty gaps when only a subset of a context's buttons was active. Research (an Explore
    agent survey of `BuildMenu.cs`) found the real shape of the problem before any design
    choice was made: 60 buttons across 7 contexts, Barracks the worst case at 23 simultaneous
    buttons; only 25 of 60 have a real icon asset (the other 35 — every tier-upgrade/research
    button, most Wave 4 units — are text-only, and that text carries live cost/percent/
    age-gate state); no tooltip system exists for UI widgets (`HoverTooltip.cs` is a 3D-raycast
    world-object tooltip, not reusable here); no `GridLayoutGroup`/pagination pattern exists
    anywhere in the project. Two design decisions confirmed via AskUserQuestion before
    planning further: **true icon-only grid + a new hover tooltip** (not a smaller "keep
    icon+text rows, pack them tighter" option) for the full 60-button/7-context migration in
    one session (not split across sessions), and **paging (Prev/Next buttons)** for overflow,
    not a `ScrollRect`. The key architectural decision that kept the diff bounded: **none of
    the ~40 existing `Update*` label-generation methods needed to change** — each button's
    existing `TMP_Text` label is disabled (`enabled = false`) once in `Awake()` so it stops
    rendering but keeps receiving `.text =` writes every frame exactly as before, and a new
    `TooltipTrigger` component reads that same live text on hover; a new `LayoutCommandGrid()`
    called once at the end of `Update()` (after every context branch has already decided each
    button's `activeSelf` for the frame) repositions/resizes only the current page's active
    buttons into a 4-column grid and hides the rest — so no button's scene RectTransform
    needed hand-editing either, all 60 are positioned in code every frame. New
    `Assets/Scripts/UI/TooltipTrigger.cs` (`IPointerEnterHandler`/`IPointerExitHandler`,
    relays its `TMP_Text` source's live text to `ButtonTooltip`) and
    `Assets/Scripts/UI/ButtonTooltip.cs` (single Canvas-level instance, follows the mouse the
    same way `HoverTooltip.cs`'s own panel does, but pointer-event-triggered instead of
    raycast-triggered — kept as a separate class, same "don't merge topically-related but
    mechanically-different systems" precedent as `CombatBonus`/`CounterMatrix`). New
    `BuildMenu.SetupGridCell`/`PlaceholderIcon` (a flat generated `Texture2D`/`Sprite`, cached
    after first build — not sourced art, the same "generic procedural shape" fallback
    convention this project already uses for buildings with no 3D model yet, applied to the 35
    icon-less buttons — **flagged directly per the flag-asset-needs convention: real
    per-unit/per-tech icon art is still needed eventually**) replace the old `AddCommandIcon`.
    New `internal static BuildMenu.ComputeGridPage` is pure pagination math (given which
    buttons are active, in a fixed order, plus a page capacity, returns the current page's
    visible slice, page count, and clamped page index) — fully unit-testable with no scene/
    MonoBehaviour dependency, 6 new tests in `CommandGridLayoutTests.cs` (451 total, all pass)
    covering single-page/multi-page/clamping/empty-set cases. New scene objects (`ButtonTooltip`
    panel, `GridPrevButton`/`GridNextButton`/`GridPageLabel`) wired via UnityMCP; the 60
    existing buttons needed zero scene edits since their layout is now fully code-driven.
    Live-verified via UnityMCP through the real production path: a real match
    (`CivilizationSetup.BeginMatch(Maurya)`), a real Builder-capable worker selected showed the
    Placement context's real 13 buttons as a clean 4-column icon grid (8 real icons, 5
    placeholders, visually distinct), a real `BarracksFactory.Place`-spawned Barracks selected
    showed the real worst-case 23-button context in a single page with Prev/Next/page-label
    correctly hidden (23 ≤ the ~28-capacity page), a real `TooltipTrigger.OnPointerEnter` call
    on `DurgButton` showed the tooltip with the exact live text `Update()` had already computed
    ("Build Durg (Requires Durg Age)"), confirmed hidden again on `OnPointerExit`. **Not
    live-verified, by design**: pagination's actual page-2 behavior, since no context today has
    enough active buttons to force a second page (worst case 23 < capacity 28) — covered
    instead by `CommandGridLayoutTests.cs`'s own synthetic multi-page cases, not glossed over.
    Next: item 32 (Age/research readout) or any other item, user's call.
32. **[S] Age/research always-visible readout.** Top-center per AoE's convention: current
    age name + research-in-progress meter, always on screen. Cheap, and answers the most
    common new-player question. Natural to bundle with item 30 since both touch the top bar
    region. *Depends on: item 30 (do them in the same pass — same screen region, same
    screenshot-verification cycle).*

**Wave 5 exit criteria:** every unit and building has a working team-colour slot, and the HUD
reads as one coherent AoE-style bottom-bar layout rather than two disconnected corners.

---

## Wave 6 — Economy, meta and remaining systems (parallel-safe, pick any order)

None of these block each other. Treat this as a backlog to draw from once Waves 0-5 are
stable, not a strict sequence.

33. **[S] Town Bell.** One-click garrison-all-workers. Cheap, high perceived value during a
    raid. *Depends on: nothing.*
34. **[S] Idle-worker indicator.** Small UI addition near the minimap. *Depends on: item 30
    if you want it inside the new bottom bar; otherwise independent.*
35. **[M] Relics + Monastery-equivalent, if wanted.** AoE II relics pay 0.5 gold/sec and can
    trigger a victory countdown — needs a carrier unit (Vaidya/Purohita from Wave 4 item 27,
    or a dedicated Relic-carrier) and a building to store them in. *Depends on: a yes/no
    design decision, and Wave 4 item 27 if the carrier is Vaidya/Purohita.*
36. **[M] Score system.** Four weighted categories (Military/Economy/Technology/Society),
    modeled on AoE II's own weighting from the Meta & UI sheet. *Depends on: nothing, but
    higher value once Wave 3/4 give it more to actually score.*
37. **[M] Victory conditions beyond Conquest.** `Match/MatchManager.cs` and
    `UI/GameOverScreen.cs` already exist — verify which conditions are wired before adding
    more. Wonder, Relic, Regicide (needs Wave 4 item 28's hero), Time Limit. *Depends on: a
    design decision on which conditions you actually want, since building all of AoE's is not
    automatically the right scope for this project.*
38. **[M] Game modes.** Selectable Skirmish/Deathmatch/Regicide-style/Empire-Wars-style modes
    on top of the scenario system that already exists. *Depends on: item 37 if modes are
    tied to specific victory conditions.*
39. **[S] Cheat codes.** Low priority, genuinely useful for testing your own scenarios.
    *Depends on: nothing.*
40. **[S] Tutorial content.** Rides the existing `MissionObjective`/`MissionTrigger` system —
    pure content authoring, no new system needed. *Depends on: nothing, but higher value once
    more of the roster exists to teach.*
40. **(asset-blocked) Soundtrack.** Already scoped in a prior session: self-serve CC0 tracks
    from Kenney.nl, same sourcing approach as the existing `Audio/SfxPlayer.cs` pass. Pure
    asset-sourcing task on your side — Claude Code's role here is limited to wiring whatever
    tracks you provide into the existing audio system, not composing anything.

---

## Asset-blocked track — runs alongside all of the above, on your schedule

These are listed separately because they are YOUR work (or a commissioned artist's), not a
coding session's. Claude Code's job for each is limited to the code plumbing that makes
dropping the asset in trivial once it exists — build that plumbing whenever the corresponding
code wave lands, don't wait for the art first.

- **Villager (Praja) art.** You flagged this directly: currently an unclothed generic
  humanoid. Given the Worker is the unit a player looks at most, this may be worth resourcing
  ahead of combat-unit art, not after it.
- **Town Center age-variants.** 4 ages × 5 civs = up to 15 additional models beyond the
  current single model, once Wave 1's Durg age exists to have a look of its own.
- **Tower and Wall upgrade tiers.** ~10 additional models each, once Wave 3-style tier
  ladders are decided for buildings (not yet scoped above — buildings don't currently have an
  upgrade-tier system the way units now will; flag this as a Wave 3.5 candidate if you want
  building tiers as well as unit tiers).
- **Drop-off building art** (Lumber Camp / Mining Camp / Mill) — currently procedural
  silhouettes, visibly below the bar the other 45 buildings set.
- **Per-civ unit gear** (helmet/shield/weapon variants) — `WeaponAttachment.cs`'s hand-bone
  system already exists; only the art is missing.
- **New-unit models** for every Wave 4 item you confirm — Scout, Skirmisher, Battering Ram,
  Cavalry Archer, Camel Rider, Scorpion, Trebuchet, Fire Ship, Trader, Vaidya/Purohita,
  Maharaja.

---

## Summary table

| Wave | Theme | Items | Parallel-safe? |
|---|---|---|---|
| 0 | Foundation fixes | 4 | No — do first, sequentially |
| 1 | 4th Age (Durg) | 2 | No — blocks everything Durg-tagged |
| 2 | Structural buildings | 2 | Mostly sequential with each other |
| 3 | Upgrade ladders | 9 | One-line-per-session, order flexible |
| 4 | New units | 11 | Fully parallel once Waves 0-1 done |
| 5 | Cross-cutting systems | 3 | Parallel with Waves 0-4 |
| 6 | Economy/meta backlog | 9 | Fully parallel, pick any order |

40 scoped items total. Nothing from the spec workbook's "To build" / "To build (asset)" /
"Design decision" / "Enum exists, unused" rows was dropped — every one is either an item
above or explicitly named as a dependency of one.

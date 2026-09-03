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

7. **[M] Durg building.** (Proposed name already used for the Maratha unit
   `MarathaDurgGarrisonFactory.cs` — this gives the *building* the same name, deliberately.)
   No class exists yet. This is the single most-referenced missing piece in the spec workbook:
   it anchors the late game (unique units, unique techs, best defence) the way AoE's Castle
   does. Follow the existing building-factory pattern (`TownCenterFactory.cs` /
   `BarracksFactory.cs` as the closest references). *Depends on: Wave 1 (it unlocks in the
   Durg age). Design decision still open: garrison capacity, what it trains — resolve in Plan
   Mode before writing code, don't guess.*
8. **[M] Karmashala (Blacksmith-equivalent building).** `Progression/UpgradeProgress.cs`
   already exists and works — what's missing is a physical, raidable building to research
   stat upgrades at, matching AoE's convention of every upgrade having a building "home."
   *Depends on: nothing structurally, but do it in this wave since it's the same shape of
   work as item 7 (a new production/research building) and shares review context.*

**Wave 2 exit criteria:** the two buildings every later unique-unit and upgrade-line item
implicitly assumes exist, actually exist.

---

## Wave 3 — Upgrade ladders (one line per session, strict)

Do NOT batch multiple unit lines into one session — the spec workbook's Upgrade Lines sheet
exists specifically because each line is its own scoped decision (tier count, age placement,
naming). Order below goes cheapest/most-valuable first; re-order freely, but keep it
one-line-per-session.

9. **[S] Infantry line (5 tiers).** Padati → Senani → Khandayata → Maha Khandayata → Vir
   Yodha, across Ancient/Classical/Durg/Imperial/Imperial. `SoldierFactory.cs` becomes tier 1
   of a ladder rather than a flat unit. *Depends on: Wave 0 items 1-2, Wave 1.*
10. **[S] Spearman line (3 tiers).** Bhaladhari → Trishuladhari → Maha Trishuladhari,
    Classical/Durg/Imperial. `SpearmanFactory.cs` becomes tier 1. *Depends on: Wave 0, Wave 1.*
11. **[S] Archer line (3 tiers).** Dhanurdhara → Yantra Dhanurdhara → Maha Dhanurdhara,
    Classical/Durg/Imperial. `ArcherFactory.cs` becomes tier 1. *Depends on: Wave 0, Wave 1.*
12. **[S] Knight/Cavalry line (3 tiers).** Ashvarohi → Maha Ashvarohi → Vir Ashvarohi,
    Durg/Imperial/Imperial. `CavalryFactory.cs` becomes tier 1; Rajput Royal Guard stays the
    civ-unique alternative alongside it. *Depends on: Wave 0, Wave 1.*
13. **[S] Elephant line (2 tiers, our own invention — no AoE II line to import).** Gajaroha →
    Maha Gajaroha, Durg/Imperial. Design decision first: one shared ladder for Maurya and
    Vijayanagara, or two divergent ones — resolve before coding. *Depends on: Wave 0, Wave 1.*
14. **[S] Mangonel/Siege line (3 tiers).** Shilakshepaka → Maha Shilakshepaka → Vajra
    Shilakshepaka, Durg/Imperial/Imperial. `SiegeFactory.cs` becomes tier 1. *Depends on:
    Wave 0, Wave 1.*
15. **[S] Galley/Naval line (3 tiers).** Rana Nauka → Maha Rana Nauka → Samrat Nauka,
    Classical/Durg/Imperial. `WarGalleyFactory.cs` becomes tier 1. *Depends on: Wave 0, Wave 1.*
16. **[M] Unique-unit Elite tier (2 tiers × 7 units).** Every existing unique unit
    (`CholaNavalRaiderFactory`, `VijayanagaraWarElephantFactory`, `RajputRoyalGuardFactory`,
    `MauryaWarElephantFactory`, `PillarEdictScholarFactory`, `MarathaMavlaRaiderFactory`,
    `MarathaDurgGarrisonFactory`) gets a Durg → Imperial elite step, matching AoE's rule that
    *every* unique unit gets exactly one elite upgrade. Size this as one session covering all
    7 — they share one mechanical pattern even though there are 7 of them. *Depends on:
    Wave 0, Wave 1, Wave 2 item 7 (thematically the Durg building is where "you can now train
    the base-tier unique unit" makes sense, even if the elite upgrade itself researches
    elsewhere).*
17. **[S] Blacksmith-style stat upgrade steps.** Extend `UpgradeProgress.cs` from 2 research
    steps to 3 (Classical/Durg/Imperial), and wire it to the new Karmashala building.
    *Depends on: Wave 1, Wave 2 item 8.*

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

18. **[M] Scout (Chara), 3-tier line.** Chara → Vega Ashvarohi → Maha Vega Ashvarohi,
    Ancient/Classical/Durg. Flagged repeatedly across this workbook's history as the single
    most conspicuous missing unit — now that Fog of War is confirmed to exist
    (`FogOfWar/FogOfWarManager.cs`), a scout has real function. **Recommend doing this one
    first in Wave 4** — it's the highest player-facing value per session of anything in this
    wave. *Depends on: Wave 0, Wave 1.*
19. **[S] Skirmisher (anti-archer counter-archer), 2 tiers.** Pratirodhi Dhanurdhara → Maha
    Pratirodhi Dhanurdhara, Classical/Durg. Completes the rock-paper-scissors triangle
    (Archer counters Infantry, Skirmisher counters Archer, currently missing). *Depends on:
    Wave 0, Wave 1.*
20. **[S] Battering Ram, 3 tiers.** Dwarabhanjaka → Maha Dwarabhanjaka → Vajra Dwarabhanjaka,
    Classical/Durg/Imperial. Anti-building only, no splash, garrisonable — distinct role from
    the existing Mangonel-like Siege unit. *Depends on: Wave 0, Wave 1.*
21. **[S] Cavalry Archer, 2 tiers.** Ashva Dhanurdhara → Maha Ashva Dhanurdhara,
    Durg/Imperial. Mobile ranged raider, fits Rajput/Maratha horse-archer tradition.
    *Depends on: Wave 0, Wave 1.*
22. **[S] Camel Rider, 2 tiers (design decision first).** Ushtrarohi → Maha Ushtrarohi,
    Durg/Imperial. Resolve in Plan Mode whether this ships at all before coding — it's listed
    as a design decision, not a confirmed build, in the spec workbook. *Depends on: Wave 0,
    Wave 1, and an explicit yes/no from you.*
23. **[S] Scorpion, 2 tiers.** Bana Yantra → Maha Bana Yantra, Durg/Imperial. `DamageType.Pierce`
    already exists — this needs a raycast-through code path for pass-through damage, not a new
    mechanic type. *Depends on: Wave 0, Wave 1.*
24. **[S] Trebuchet, 1 tier.** Maha Yantra, Imperial only. Long-range anti-building with a
    minimum range; needs a pack/unpack state (two mesh states or a fold animation) — flag the
    animation need to yourself as a possible asset dependency even though the unit itself is
    code-buildable now with a placeholder model. *Depends on: Wave 0, Wave 1.*
25. **[S] Fire Ship, 3 tiers.** Agni Nauka → Maha Agni Nauka → Vega Agni Nauka,
    Classical/Durg/Imperial. First real consumer of `DamageType.Fire` (wired in Wave 0 item 4).
    *Depends on: Wave 0 item 4, Wave 1.*
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
30. **[L] UI layout re-anchor — bottom bar.** From the UI Layout sheet: re-anchor
    `BuildMenu.cs`, `SelectedUnitPanel.cs`, and a slice of `ResourceHUD.cs` into one shared
    bottom-docked root (command panel / info panel / minimap, left to right), matching AoE's
    convention, instead of the current build-menu-right / resource-panel-top-left split. The
    content and icon language mostly already exist — this is a genuine layout change, sized
    Large mainly because it touches 3+ UI classes' anchor code and needs full visual
    re-verification via screenshot, not because the logic is complex. *Depends on: nothing —
    fully parallel-safe with Waves 0-4 if you want a second track running.*
31. **[S] Age/research always-visible readout.** Top-center per AoE's convention: current
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

32. **[S] Town Bell.** One-click garrison-all-workers. Cheap, high perceived value during a
    raid. *Depends on: nothing.*
33. **[S] Idle-worker indicator.** Small UI addition near the minimap. *Depends on: item 30
    if you want it inside the new bottom bar; otherwise independent.*
34. **[M] Relics + Monastery-equivalent, if wanted.** AoE II relics pay 0.5 gold/sec and can
    trigger a victory countdown — needs a carrier unit (Vaidya/Purohita from Wave 4 item 27,
    or a dedicated Relic-carrier) and a building to store them in. *Depends on: a yes/no
    design decision, and Wave 4 item 27 if the carrier is Vaidya/Purohita.*
35. **[M] Score system.** Four weighted categories (Military/Economy/Technology/Society),
    modeled on AoE II's own weighting from the Meta & UI sheet. *Depends on: nothing, but
    higher value once Wave 3/4 give it more to actually score.*
36. **[M] Victory conditions beyond Conquest.** `Match/MatchManager.cs` and
    `UI/GameOverScreen.cs` already exist — verify which conditions are wired before adding
    more. Wonder, Relic, Regicide (needs Wave 4 item 28's hero), Time Limit. *Depends on: a
    design decision on which conditions you actually want, since building all of AoE's is not
    automatically the right scope for this project.*
37. **[M] Game modes.** Selectable Skirmish/Deathmatch/Regicide-style/Empire-Wars-style modes
    on top of the scenario system that already exists. *Depends on: item 36 if modes are
    tied to specific victory conditions.*
38. **[S] Cheat codes.** Low priority, genuinely useful for testing your own scenarios.
    *Depends on: nothing.*
39. **[S] Tutorial content.** Rides the existing `MissionObjective`/`MissionTrigger` system —
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

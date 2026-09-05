# Project: Kingdoms of Bharat

Historical RTS across Indian kingdoms/empires, targeting AoE II/IV-level systemic
depth. Full roadmap: `/docs/ROADMAP.md` (Master Roadmap v3 — sections: 1. Open items
punch list, 2. Architectural notes to preserve, 3. Process note, 4. Art direction &
asset requirements, 5. Priority order).

## Current status (keep current — update every session)
- **Wave 4 item 21 (Cavalry Archer, 2-tier mobile ranged raider) closed
  (2026-09-05).** Picked up after being offered a choice between this and item
  22 (Camel Rider, which needs a ship-or-not design decision first) — user
  picked Cavalry Archer as the no-open-questions option, right after item 20
  (Battering Ram) closed. Design call made explicitly (the roadmap fixes tier
  names/ages/count, not what class this counts as for combat purposes):
  `CavalryArcherFactory` classifies as `UnitClass.Archer`, not a new class — a
  mounted Archer, not a new counter archetype, keeping it inside the existing
  counter web for free (`CombatBonus`): it still hard-counters Cavalry
  (Archer→Cavalry 2x) and is still hard-countered by Skirmisher
  (Skirmisher→Archer 2x), taking Infantry→Archer's 1.5x penalty too, exactly
  like a foot Archer — only its move speed (borrowed from Cavalry's own
  value, 6.0) and cost differ. Deliberately does NOT read
  `CivilizationProfile.FindCategoryMultiplier` with `UnitClass.Cavalry` (the
  Maratha cavalry-speed/Rajput cavalry-damage civ bonuses) — those are scoped
  to units whose actual combat class is Cavalry, and this one's is Archer, so
  it correctly falls outside them; it just happens to ride a horse. New
  `Progression/CavalryArcherLineProgress.cs` mirrors
  `SkirmisherLineProgress.cs`'s 2-tier shape exactly, but gated at
  Durg/Imperial (not Classical/Durg) per the roadmap's own item text — tier
  0's Durg `RequiredAge` is descriptive only, never enforced
  (`RequestTrainCavalryArcher` has no age gate of its own, same convention
  `CavalryLineProgress`/`ArcherLineProgress` already established). Tier 1
  (Maha Ashva Dhanurdhara) reuses every other line's own established
  Imperial-gate growth exactly (+30 HP/+6 dmg/200 Gold/100 Wood/40s), not
  independently balanced. New `Combat/CavalryArcherFactory.cs` combines
  `ArcherFactory`'s ranged-attack setup with `CavalryFactory`'s mount/speed
  setup — reuses the same Male Human Character Dummy body plus both the Bow
  (`LeftHand`) and Horse (`AttachBeside`) props, no dedicated mounted-archer
  model exists yet — **flagging directly per the flag-asset-needs convention:
  a Cavalry Archer currently looks identical to a mounted Archer/Cavalry
  hybrid using existing props, no distinct silhouette.** New
  `Barracks.RequestTrainCavalryArcher`/`RequestResearchCavalryArcherTier`
  (independent research track alongside every other Barracks tier line), new
  `cavalryArcherButton`/`cavalryArcherTierButton`/`cavalryArcherTierLabel` in
  `BuildMenu.cs`, hotkeys K/P (unused within the Barracks context
  specifically — both already reused across mutually-exclusive
  TownCenter/Karmashala contexts, this file's own established convention),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.CavalryArcher`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "cavalry_archer" row (60 Food/40 Gold/30 HP/5
  dmg/Pierce/6 range/6.0 speed) and regenerated data assets via
  `BharatRTS/Generate Data Assets From CSV`. 12 new EditMode tests
  (`CavalryArcherLineTests.cs`, 399 total, all pass). Hit the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3/4 session
  has hit (duplicated `BatteringRamButton`/`BatteringRamTierButton` into real
  `CavalryArcherButton`/`CavalryArcherTierButton` scene objects via
  UnityMCP). Live-verified via UnityMCP through the real production path: a
  real match (`CivilizationSetup.BeginMatch(Rajput)`), a real
  `BarracksFactory.Place` Barracks — `RequestTrainCavalryArcher()` correctly
  trained even at Ancient Age (confirming tier 0's Durg `RequiredAge` is
  descriptive only, same as Cavalry's own base) and deducted exactly 60
  Food/40 Gold; a forced tick spawned a real "Rajput Ashva Dhanurdhara"
  (`Attackable.Class == Archer`, HP 34.5, move speed 6, a real
  `GarrisonSeeker` and `VisionSource` both present);
  `RequestResearchCavalryArcherTier()` correctly refused at Durg with zero
  deduction, then deducted exactly 200 Gold/100 Wood at Imperial; after a
  forced tick completed it, the two already-spawned Ashva Dhanurdhara units
  stayed at 34.5 HP while a new one trained afterward came out "Rajput Maha
  Ashva Dhanurdhara" at 82.8 HP — not retroactive, confirmed live. Then
  through the real scene UI path specifically: the real `CavalryArcherButton`'s
  own `onClick.Invoke()` left the stockpile unchanged immediately (confirming
  it goes through `CommandBus`'s lockstep queue, not a synchronous
  deduction) and deducted the exact cost ~2 real seconds later; the real
  `CavalryArcherTierButton`'s label correctly read "Cavalry Archer (Max
  Tier)" once that faction's tier was already maxed from the earlier test.
  No AI-side training hook, same explicitly-out-of-scope call as every other
  Wave 3/4 item. Next: Wave 4 item 22 (Camel Rider — needs a design-decision
  Plan Mode session first per its own roadmap text) or any other Wave 4
  item, user's call — all are parallel-safe once Wave 0/1 are done.
- **Wave 4 item 20 (Battering Ram, 3-tier anti-building specialist) closed
  (2026-09-05).** Picked up per the user's "start wave 4 item 20" request,
  right after item 19 (Skirmisher) closed. This project already had one
  anti-building specialist (Siege/Mangonel, `CombatBonus.Multiplier(Siege,
  Building) = 3x`), but it can still fight units (unremarkably, flat 1x)
  and has splash — item 20 specs a second, genuinely distinct siege unit:
  anti-building ONLY (structurally unable to target units, not just weak
  against them), no splash, and garrisonable. New
  `MeleeAttacker.SetBuildingOnly(bool)` reads "anti-building only"
  literally — when set, `AttackMove` flatly refuses any target whose
  `Attackable.Class` isn't `Building` (every other `MeleeAttacker` user
  unaffected, defaults false), matching AoE II's own ram (can't even be
  given an attack-move onto a unit). New `Progression/
  BatteringRamLineProgress.cs` mirrors `ArcherLineProgress.cs`'s 3-tier
  Classical/Durg/Imperial shape exactly (Dwarabhanjaka → Maha
  Dwarabhanjaka → Vajra Dwarabhanjaka), reusing Archer's own growth curve
  at matching gates, not independently balanced. New `CombatBonus.
  Multiplier(BatteringRam, Building) = 4x` — steeper than Siege's own 3x
  since a Ram's entire kit is "hit buildings." New `Combat/
  BatteringRamFactory.cs`: `SetBuildingOnly(true)`, never calls
  `SetSplashRadius` (no splash), and — re-reading `GarrisonPoint`/
  `GarrisonSeeker` (the General Garrisoning system from Wave 1) before
  coding — resolves "garrisonable" as "hosts friendly units for
  protection," not "enters a building": adds a `GarrisonPoint` to itself
  (capacity 4, matching Tower's), which generalizes to a non-Building host
  with **zero changes needed** to either existing class
  (`GarrisonSeeker.ComputeApproachPoint` already falls back to the
  target's raw `transform.position` with no `BuildingFootprintTag`, and
  `GarrisonPoint.OnDestroy` already ungarrisons everyone on death).
  Deliberately no `StanceController` (an Aggressive/Defensive auto-engage
  scan would "target" nearby units it can structurally never hit — worse
  than requiring an explicit order, matching real AoE II). New
  `Barracks.RequestTrainBatteringRam`/`RequestResearchBatteringRamTier`
  (independent research track alongside every other Barracks tier line),
  new `batteringRamButton`/`batteringRamTierButton`/`batteringRamTierLabel`
  in `BuildMenu.cs`, hotkeys D/R (unused within the Barracks context
  specifically — both already reused across the mutually-exclusive
  TownCenter/Durg contexts, this file's own established convention), wired
  into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.BatteringRam`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "battering_ram" row (60 Food/120 Wood/80
  HP/18 dmg/Melee/3 melee armor/1.3 speed/1.5 range) and regenerated data
  assets via `BharatRTS/Generate Data Assets From CSV`. 16 new EditMode
  tests (`BatteringRamLineTests.cs`, 387 total, all pass). No dedicated
  Battering Ram model exists yet — reuses the same Human Character Dummy
  body + Kanabo weapon as `SiegeFactory` (whose own comment already
  flagged this exact prop as a better long-term fit for a real battering
  ram) — **flagging directly per the flag-asset-needs convention: a
  Battering Ram currently looks identical to a Siege unit in the field.**
  Hit the same recurring "console bridge shows zero errors while new code
  fails to compile" gotcha every session eventually hits (the new test
  file was missing `using KingdomsOfBharat.Units;`, silently leaving the
  suite at the old 371-test count with `read_console` showing nothing —
  only `~/Library/Logs/Unity/Editor.log` directly showed the real
  `CS0246`/`CS0103` errors); fixed, then 387/387 passed. Hit the same
  recurring "new `[SerializeField]` null in the scene" gotcha every Wave
  2/3/4 session has hit (duplicated `SkirmisherButton`/
  `SkirmisherTierButton` into real `BatteringRamButton`/
  `BatteringRamTierButton` scene objects via UnityMCP). Live-verified via
  UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real `BarracksFactory.Place`
  Barracks — directly via the component API, `RequestTrainBatteringRam()`
  deducted exactly 60 Food/120 Wood and spawned a real "Maurya
  Dwarabhanjaka" (`Attackable.Class == BatteringRam`, HP 88, a real
  `GarrisonPoint` with `Capacity == 4`); a real `MeleeAttacker.AttackMove`
  call against a real hostile Soldier was refused (`IsAttacking` stayed
  false) while the identical call against a real hostile Tower was
  accepted (`IsAttacking` became true) — the anti-building-only trait
  confirmed live, not just in the isolated unit test; a real Worker
  garrisoned into and back out of the Ram correctly. Then through the real
  scene UI path specifically: the real `BatteringRamButton`'s own
  `onClick.Invoke()` deducted the exact cost and spawned a real Battering
  Ram through `CommandBus`'s lockstep queue (confirmed the delayed, not
  synchronous, execution directly); the real `BatteringRamTierButton`'s
  label correctly read "Upgrade to Maha Dwarabhanjaka (120 Gold, 60 Wood)"
  at Durg and its own `onClick.Invoke()` deducted exactly that. No
  AI-side training hook, same explicitly-out-of-scope call as every other
  Wave 3/4 item. Next: any other Wave 4 item (Cavalry Archer, Scorpion,
  Trebuchet, Fire Ship, or the design-decision items — Camel Rider,
  Trader), user's call — all are parallel-safe once Wave 0/1 are done.
- **Wave 4 item 19 (Skirmisher, anti-archer counter-archer, 2 tiers) closed
  (2026-09-05).** Picked up per the user's "start wave 4 item 19" request,
  right after item 18 (Scout) closed as the first Wave 4 item. Completes
  the counter web's last gap: this project's `CombatBonus` already had
  Infantry→Archer (1.5x), Archer→Cavalry (2x), Cavalry→Infantry (1.5x),
  and Spearman's own Cavalry/Infantry pair, but nothing hard-countered
  Archer's own counter-pick — Skirmisher is the dedicated anti-archer
  specialist that closes it. New `UnitClass.Skirmisher` plus two new
  `CombatBonus` pairings (Skirmisher→Archer 2x, Infantry→Skirmisher
  1.25x) — both values reuse Spearman's own pairing exactly (the closest
  existing precedent for "a unit built to counter one other class"), not
  independently balanced. New `Progression/SkirmisherLineProgress.cs` —
  only 2 tiers total (Pratirodhi Dhanurdhara at Classical, Maha
  Pratirodhi Dhanurdhara at Durg), not 3 like every Wave 3 line, matching
  this item's own roadmap spec; tier 1's bonus/cost reuses
  `ArcherLineProgress`'s own Durg-gate growth exactly (+18 HP/+4 dmg/120
  Gold/60 Wood/25s). New `Combat/SkirmisherFactory.cs` mirrors
  `ArcherFactory.cs`'s shape almost exactly (ranged/Pierce attack,
  `UnitClass.Skirmisher` the actual differentiator) — reuses the shared
  bow model/animation since no dedicated Skirmisher model exists yet
  (`docs/YOUR_ACTION_ITEMS.md` item 19 specs a quilted-armor archer with
  a forearm buckler, not delivered) — **flagging directly per the
  flag-asset-needs convention: a Skirmisher currently looks identical to
  an Archer in the field.** New `Barracks.RequestTrainSkirmisher`/
  `RequestResearchSkirmisherTier` (independent research track alongside
  every other Barracks tier line), new `skirmisherButton`/
  `skirmisherTierButton`/`skirmisherTierLabel` in `BuildMenu.cs`, hotkeys
  C/G (Train Skirmisher = C, Upgrade Skirmisher Tier = V — the last two
  unused letters in `BuildMenu`'s Barracks-context hotkey map), wired
  into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Full
  `NetTrainKind.Skirmisher`/`CommandSerializer` wiring. Added a
  `unit_roster_template.csv` "skirmisher" row (35 Food/25 Gold/20 HP/3
  dmg/Pierce/5 range/3.8 speed) and regenerated data assets via
  `BharatRTS/Generate Data Assets From CSV`. 13 new EditMode tests
  (`SkirmisherLineTests.cs`, 371 total, all pass). Hit the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3/4
  session has hit (duplicated `CharaButton`/`CharaTierButton` into real
  `SkirmisherButton`/`SkirmisherTierButton` scene objects via UnityMCP).
  Live-verified via UnityMCP through the real production path: a real
  match (`CivilizationSetup.BeginMatch(Maurya)`), a real
  `BarracksFactory.Place`-spawned Barracks selected via
  `SelectionManager`, the real scene button's own `onClick.Invoke()`
  deducted exactly 35 Food/25 Gold and trained a real "Maurya Pratirodhi
  Dhanurdhara" (23 HP, `Attackable.Class == Skirmisher`); the real tier
  button correctly showed "Upgrade to Maha Pratirodhi Dhanurdhara (120
  Gold, 60 Wood)" and its own `onClick.Invoke()` deducted exactly that; a
  forced tick advanced the tier and the real button settled on
  "Skirmisher (Max Tier)"; a second Skirmisher trained afterward came out
  "Maurya Maha Pratirodhi Dhanurdhara" at 43.7 HP while the first,
  already-spawned Skirmisher stayed at 23 HP — not retroactive, confirmed
  live; `CombatBonus.Multiplier(Skirmisher, Archer)` and
  `CombatBonus.Multiplier(Infantry, Skirmisher)` both confirmed live at
  2x/1.25x. No AI-side training hook, same explicitly-out-of-scope call
  as every other Wave 3/4 item. Next: Wave 4 item 20 (Battering Ram, 3
  tiers) or any other Wave 4 item, user's call — all are parallel-safe
  once Wave 0/1 are done.
- **Wave 3 item 17 (Blacksmith-style stat upgrade steps, Karmashala) closed
  (2026-09-05).** Picked up per the user's explicit "start now" request right
  after item 16 closed Wave 3's own numbered list (item 17 doesn't block
  Wave 3's exit criteria — it's the last remaining item in that wave's own
  numbering, now also done). Investigated first, per protocol, rather than
  assuming the roadmap text was still accurate: `UpgradeProgress.MaxTier`
  was already 3 (set by a prior session), but **none of the 3 tiers were
  age-gated at all** — a Karmashala only needs Classical Age to exist, and
  once it did, all 3 tiers were researchable back-to-back in the same Age,
  purely gold-limited. New `UpgradeProgress.TierRequiredAges`
  (`{Classical, Durg, Imperial}`) plus `NextAttackTierRequiredAge`/
  `NextArmorTierRequiredAge`/`NextAttackTierAgeRequirementMet`/
  `NextArmorTierAgeRequirementMet` — same array-indexed-by-current-tier +
  ordinal `AgeId` comparison shape every other tier line's own
  `NextTierAgeRequirementMet` already uses. `Karmashala.RequestResearchAttack`/
  `RequestResearchArmor` both gained the age check; `BuildMenu.cs`'s shared
  `UpdateUpgradeButton` (used by both Attack/Armor tracks) gained the same
  "Upgrade to X (needs Y Age)" branch every other tier button already has.
  **Found and fixed one real edge-case bug before it shipped**: the new
  `NextAttackTierRequiredAge`/`NextArmorTierRequiredAge` calls are evaluated
  unconditionally at `BuildMenu`'s call site (C# evaluates all arguments
  before a method call — `UpdateUpgradeButton`'s own `!hasNextTier`
  early-return happens too late to matter) — at max tier this would index
  a 3-element array at position 3 and throw `IndexOutOfRangeException` every
  single frame once any track maxed out; fixed by clamping the index, with
  a dedicated regression test. 7 new/updated EditMode tests in
  `KarmashalaTests.cs` — the 6 pre-existing tests needed `AgeProgress.Initialize`
  added, since they'd implicitly relied on the previously-ungated behavior
  (358 total, all pass). Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maurya)`), a
  real `KarmashalaFactory.Place` Karmashala correctly refused tier 1 at
  Ancient with zero deduction (1000 Gold on hand), started tier 1 at
  Classical (80 Gold), refused tier 2 at Classical with zero deduction,
  started tier 2 at Durg (160 Gold), the real scene button's own label
  correctly read "Upgrade Attack (needs Imperial Age)"/`interactable=false`
  at Durg for tier 3 and its real `onClick.Invoke()` correctly no-op'd, then
  at Imperial the same real button's `onClick.Invoke()` deducted exactly 240
  Gold and started tier 3, settling on "Attack (Max)"/`interactable=false`
  once complete. No scene-wiring gotcha this session (attackUpgradeButton/
  armorUpgradeButton were already wired from Wave 2 item 8 — only their
  underlying gating logic and label text changed). **This closes item 17,
  the last item in Wave 3's own numbered list** — everything Wave 3 and its
  own numbering set out to do is now done. Next: Wave 4 (new units — item 18
  already closed by a concurrent session, see below; Skirmisher/Battering
  Ram/Cavalry Archer/etc. remain, user's call), or any other Section 5
  priority.
- **Wave 3 item 16 (Unique-unit Elite tier, 2 tiers × 5 units) closed
  (2026-09-05) — this closes Wave 3.** Picked up right after item 15 per the
  user's "your call" hand-off. No new design decisions needed — item 16's own
  roadmap text already fixes scope (every unique unit except the two War
  Elephant factories, which item 13's own Gajaroha ladder already covers as
  their one elite step) and the mechanism (research on Durg, baked in at
  spawn, not retroactive) mirrors item 13's own "research where the unit
  trains" deviation exactly. New `Progression/UniqueUnitEliteProgress.cs`
  keyed by unitId (not CivilizationId, since these 5 units — Chola Naval
  Raider, Rajput Royal Guard, Maurya's Pillar Edict Scholar, Maratha's Mavla
  Raider and Durg Garrison — are genuinely civ-exclusive, unlike every other
  Wave 3 line's shared table); single Imperial-gated step reusing the same
  +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other line's own first
  Imperial-gate step already uses, not independently balanced. New
  `Durg.RequestResearchEliteTier(slot)`/`TrainsEliteEligible(slot)`/
  `IsResearchingEliteTier(slot)`/`EliteTierResearchProgress(slot)` — two
  independent per-slot tracks (Maurya/Maratha each have 2 unique-unit slots)
  gated by unitId eligibility, not slot index — Maurya's slot 0 (War
  Elephant) is NOT eligible, only slot 1 (Pillar Edict Scholar) is; Maratha's
  both slots are. All 5 factories now read `UniqueUnitEliteProgress` at spawn
  for name + HP/damage bonus (baked in, not retroactive, matching every
  other tier line). New `eliteTierButton`/`eliteTierButton2` in
  `BuildMenu.cs` (hotkeys F/G, reused from other mutually-exclusive contexts
  per this file's own established convention — safe since Durg is never
  selected at the same time as the Barracks/TownCenter contexts those
  letters are also bound in, including item 18's own new Chara F/G below),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `DurgGroup`. 16 new EditMode
  tests (`UniqueUnitEliteTests.cs`, 351 total, all pass). **Hit a real
  concurrent-session file collision mid-session**: `BuildMenu.cs` was being
  actively edited by a separate session building Wave 4 item 18 (Scout,
  below) — per this project's own documented "single-session discipline"
  gotcha, stopped and asked the user before touching that file further,
  reverted the one edit already made, and waited until the other session's
  changes stabilized (`Barracks.cs`/`SettingsMenu.cs`/`HotkeyOverlay.cs`
  were also concurrently dirty) before resuming — no work was lost or
  clobbered on either side. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maratha)`), a
  real `DurgFactory.Place`-spawned Durg confirmed both slots elite-eligible,
  the age gate correctly deducted exactly 200 Gold/100 Wood per slot at
  Imperial for both tracks running concurrently, forced real ticks completed
  both and a Mavla Raider/Durg Garrison trained afterward through the real
  factory paths came out "Maratha Maha Mavla Raider"/"Maratha Maha Durg
  Garrison" at correctly boosted HP, the real scene buttons correctly showed
  "Elite (Max Tier)"/`interactable=false` once maxed; a second real Durg
  (Maurya) confirmed slot 0 (War Elephant) ineligible/slot 1 (Pillar Edict
  Scholar) eligible exactly as designed; a third real Durg (Rajput) confirmed
  the real scene button's own `onClick.Invoke()` deducted the exact cost and
  started research for its single eligible slot, with the 2nd-slot button
  correctly hidden. **This closes Wave 3** — every currently-flat unit type
  now has a real tier ladder. Next: Wave 4 (item 18 already closed by a
  concurrent session, below; otherwise Skirmisher, Battering Ram, Cavalry
  Archer, etc., user's call), or item 17 (Blacksmith-style stat upgrade
  steps on Karmashala, still open within Wave 3's own numbering but not
  blocking Wave 3's exit criteria).
- **Wave 4 item 18 (Scout/Chara, 3-tier line) closed (2026-09-05) — first Wave 4
  item.** Picked up per the user's "start wave 4 item 18" request, since it depends
  only on Wave 0/1 (both closed), not Wave 2/3 — the roadmap's own item text also
  recommends doing this one first in Wave 4 (highest player-facing value). Unlike
  every Wave 3 item, this is a wholly new unit, not a tier upgrade to an existing
  one — Spearman's addition (Phase 2) was the closest precedent, mirrored directly.
  Two design calls made explicitly (the roadmap fixes tier names/ages, not what
  each tier improves): tier names literally translate "Vega" = speed, so
  `Progression/ScoutLineProgress.cs` grows vision radius and move speed per tier
  (`VisionBonus`/`SpeedBonus` fields replacing every other line's `DamageBonus`)
  rather than combat stats — Chara/Vega Ashvarohi/Maha Vega Ashvarohi at
  Ancient/Classical/Durg, tier 1/2 costs reusing InfantryLineProgress's own
  Padati→Senani/Senani→Khandayata values at the matching Classical/Durg gates; and
  `Combat/ScoutFactory.cs` deliberately does NOT opt into
  `EnableUpgradeArmorScaling`/`EnableUpgradeDamageScaling` or add a
  `StanceController`, mirroring `WorkerFactory`'s own "utility unit, not a combat
  unit" choice rather than the 13 combat factories that do opt in. Uses
  `UnitClass.Support` for `Attackable`/`MeleeAttacker` classification — the first
  live unit to do so for real combat resolution (Worker uses `Infantry` there,
  `Support` only for its own move-speed multiplier lookup), safe since
  `CombatBonus`/`CounterMatrix` have zero Support entries. `VisionSource` base
  radius 12 (vs. every other unit's 8) plus the tier's own bonus (14/15 at tiers
  1/2) — the actual point of the unit, needing zero `FogOfWarManager` changes
  since vision radius has always been per-instance. New independent research
  track on `Barracks.cs` (`RequestResearchCharaTier`, alongside the existing
  Infantry/Spearman/Archer/Cavalry/Siege tracks) plus `RequestTrainChara` (the
  second Barracks unit read from `DataRegistry` rather than fixed Inspector
  fields, same as Spearman). New `charaButton`/`charaTierButton`/`charaTierLabel`
  in `BuildMenu.cs`, hotkeys F/G, wired into `SettingsMenu`/`HotkeyOverlay`'s
  `BarracksGroup`. Full `NetTrainKind.Chara`/`CommandSerializer` wiring (every
  other trainable unit has this; skipping it would silently no-op the train
  command for a LAN peer). No dedicated Scout-horse model exists yet
  (`docs/YOUR_ACTION_ITEMS.md` item 18 specs one, not delivered) — reuses
  `CavalryFactory`'s own "Mounts/Horse/scene" placeholder, same
  primitive-until-a-real-pack-lands convention as everywhere else; **flagging
  directly per the flag-asset-needs convention: Scout currently looks identical
  to Cavalry in the field and needs a real, visually distinct mount eventually.**
  No AI-side training hook, same explicitly-out-of-scope call as every Wave 3
  item. 12 new EditMode tests (`ScoutLineTests.cs` + 1 in
  `TrainingAndTradeTests.cs`, 335 total, all pass). Hit the same recurring "new
  `[SerializeField]` null in the scene" gotcha every Wave 2/3/4 session has hit
  (duplicated `SpearmanButton`/`SiegeTierButton` into real `CharaButton`/
  `CharaTierButton` scene objects via UnityMCP). Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real Barracks, the real scene
  button's `onClick.Invoke()` deducted exactly 50 Food and trained a real
  "Maurya Chara" with `VisionSource` radius 12 and move speed 8.625 (7.5 base ×
  Maurya's 1.15 civ multiplier); the real tier button correctly showed "Upgrade
  to Vega Ashvarohi (100 Gold, 50 Wood)" and its own `onClick.Invoke()` deducted
  exactly that; a forced tick advanced the tier and a second Chara trained
  afterward came out "Maurya Vega Ashvarohi" with vision 14, speed 9.775, HP
  30.8, while the first, already-spawned Chara stayed at vision 12/speed 8.625 —
  not retroactive, confirmed live. Next: Wave 4 item 19 (Skirmisher, 2 tiers) or
  any other Wave 4 item, user's call — all are parallel-safe once Wave 0/1 are
  done.
- **Wave 3 item 15 (Galley/Naval line, 3 tiers) closed (2026-09-05).** Picked up
  right after item 14, per the user's "start wave 3 item 15" request. No new design
  decisions needed — item 15's own roadmap text already fixes tier count/names/ages
  (Rana Nauka → Maha Rana Nauka → Samrat Nauka, Classical/Durg/Imperial), and the
  mechanism exactly mirrors `ArcherLineProgress.cs`'s own shape from item 11 (same
  3-tier Classical/Durg/Imperial gate pattern). New `Progression/NavalLineProgress.cs`
  mirrors `ArcherLineProgress.cs` exactly; tier bonuses/costs reuse Archer's own values
  at matching gates (+18 HP/+4 dmg/120 Gold/60 Wood/25s at Durg, +30 HP/+6 dmg/200
  Gold/100 Wood/40s at Imperial), not independently balanced. Unlike every land-line
  item, research lives on **Dock**, not Barracks — the same "research lives where the
  unit trains" deviation item 13's Elephant line already established for Durg, since
  War Galley trains from Dock (`Dock.RequestTrainWarGalley`), not Barracks. New
  `Dock.RequestResearchNavalTier`/`IsResearchingNavalTier`/`NavalTierResearchProgress`/
  `TickNavalTierResearch` mirror Barracks' own tier-research shape exactly.
  `WarGalleyFactory.cs` now reads `NavalLineProgress.Current(faction)` at spawn
  (previously always named the unit literal "War Galley" — now `"{civ} {tier.Name}"`,
  matching every other tier-ladder factory) and adds the tier's HP/damage bonus. New
  `navalTierButton`/`navalTierLabel` in `BuildMenu.cs` (identical shape to
  `siegeTierButton`, gated on a selected Dock), hotkey X (checked every other letter
  was already claimed across the project's contextual hotkey map via
  `GameSettings.GetKey` call sites before picking it), wired into
  `SettingsMenu`/`HotkeyOverlay`'s existing `DockGroup`. Same explicitly-out-of-scope
  call as items 9-14: no AI-side research hook (future balance work, not a
  regression); Chola's Naval Raider unique unit is untouched, out of scope. 10 new
  EditMode tests (`NavalLineTests.cs`, 322 total, all pass). Hit and fixed the same
  recurring "new `[SerializeField]` null in the scene" gotcha every Wave 2/3 session
  has hit (duplicated `WarGalleyButton` into a real `NavalTierButton` scene object via
  UnityMCP). Live-verified via UnityMCP through the real production path: a real
  match (`CivilizationSetup.BeginMatch(Chola)`), a real `DockFactory.Place`-spawned
  Dock selected via `SelectionManager`, the age gate correctly refused research at
  Ancient with 1000 Gold/Wood on hand (zero deduction), then at Durg deducted exactly
  120 Gold/60 Wood and completed via a forced tick, a War Galley trained afterward
  through the real `WarGalleyFactory.Spawn` path came out "Chola Maha Rana Nauka" at
  72.45 HP, the real scene button's own `onClick.Invoke()` deducted exactly 200
  Gold/100 Wood at Imperial for the second tier, and after that tier completed a War
  Galley spawned "Chola Samrat Nauka" at 90 HP with the real button's label settling
  on "Naval (Max Tier)" and `interactable=false`. Next: Wave 3 item 16 (Unique-unit
  Elite tier, 2 tiers × 5 units), user's call.
- **Wave 3 item 14 (Mangonel/Siege line, 3 tiers) closed (2026-09-05).** Picked up
  right after item 13, per the user's "start wave 3 item 14" request. No new design
  decisions needed — item 14's own roadmap text already fixes tier count/names/ages
  (Shilakshepaka → Maha Shilakshepaka → Vajra Shilakshepaka, Durg/Imperial/Imperial),
  and the mechanism exactly mirrors `CavalryLineProgress.cs`'s own shape from item 12
  (research on Barracks, baked in at spawn, not retroactive, back-to-back
  Imperial-gated last two tiers). New `Progression/SiegeLineProgress.cs` mirrors
  `CavalryLineProgress.cs` exactly; tier bonuses/costs reuse the same growth every
  other line's own back-to-back Imperial pair already uses (Cavalry's Maha Ashvarohi
  → Vir Ashvarohi values: +30 HP/+6 dmg/200 Gold/100 Wood/40s, then +45 HP/+9
  dmg/250 Gold/125 Wood/50s), not independently balanced. New independent research
  track on `Barracks.cs` (`RequestResearchSiegeTier`, alongside the existing
  Infantry/Spearman/Archer/Cavalry tier tracks), `SiegeFactory.cs` now reads
  `SiegeLineProgress.Current(faction)` at spawn (previously always named the unit
  literal "Siege" — now `"{civ} {tier.Name}"`, matching every other tier-ladder
  factory). New `siegeTierButton`/`siegeTierLabel` in `BuildMenu.cs` (identical
  shape to `cavalryTierButton`), hotkey O, wired into `SettingsMenu`/`HotkeyOverlay`'s
  `BarracksGroup`. Same explicitly-out-of-scope call as items 9-13: no AI-side
  research hook (future balance work, not a regression). 10 new EditMode tests
  (`SiegeLineTests.cs`, 312 total, all pass). Hit and fixed the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3 session has hit
  (duplicated `CavalryTierButton` into a real `SiegeTierButton` scene object via
  UnityMCP). Live-verified via UnityMCP through the real production path in Play
  mode (needed for `BuildMenu.Awake()`'s `_selectionManager` resolution to actually
  run): a real match (`CivilizationSetup.BeginMatch(Maurya)`), a real
  `BarracksFactory.Place`-spawned Barracks selected via `SelectionManager`, the age
  gate correctly showed the "(needs Imperial Age)" label at Durg with zero
  deduction, the real scene button's own `onClick.Invoke()` deducted exactly 200
  Gold/100 Wood at Imperial and started research, a forced tick advanced the tier
  and a Siege unit trained afterward through the real `SiegeFactory.Spawn` path came
  out "Maurya Maha Shilakshepaka" at 96 HP, and after researching the second tier
  the real button's label correctly settled on "Siege (Max Tier)". Next: Wave 3
  item 15 (Galley/Naval line, 3 tiers), user's call.
- **Wave 3 item 13 (Elephant line, 2 tiers) closed (2026-09-05).** Picked up right
  after item 12's live-verification follow-up, per the user's "start wave 3 item 13"
  request. Two design decisions resolved via AskUserQuestion before coding — item 13's
  own text already flagged one (one shared ladder vs. two divergent), and re-reading
  Wave 3 item 16 (Unique-unit Elite tier, not yet started) surfaced a second, real
  overlap not previously caught: item 16 separately planned a Durg→Imperial "elite"
  upgrade for the same two War Elephant factories item 13 also upgrades. Resolved:
  (1) one shared `ElephantLineProgress.cs` table (Gajaroha → Maha Gajaroha,
  Durg/Imperial) read by both `MauryaWarElephantFactory`/
  `VijayanagaraWarElephantFactory` — matches every other tier line's own precedent;
  (2) item 13 IS the elephant elite tier, so item 16's own roadmap text now excludes
  both War Elephant factories from its 7-unit list (5 remain). Research lives on
  **Durg**, not Barracks — the one tier line that deviates from every prior line's
  convention, since War Elephants train from Durg
  (`UniqueUnitDefinition.Spawn`/`Durg.RequestTrainUniqueUnit`), not Barracks: new
  `Durg.RequestResearchElephantTier`/`IsResearchingElephantTier`/
  `ElephantTierResearchProgress`/`TrainsElephant` (the last gates the new
  `elephantTierButton` so Chola/Rajput/Maratha's Durg never shows a button that does
  nothing — added a `UnitId` field to `UniqueUnitDefinition` for this, a data-driven
  check rather than a hardcoded civ list). Single Imperial-gated tier reuses the same
  +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other line's own first Imperial
  step already uses, not independently balanced. 13 new EditMode tests
  (`ElephantLineTests.cs`, 302 total, all pass). Hit and fixed the same recurring
  "new `[SerializeField]` null in the scene" gotcha every Wave 2/3 session has hit
  (duplicated `CavalryTierButton` into a real `ElephantTierButton` scene object).
  Live-verified via UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), a real Durg confirmed
  `TrainsElephant=true`, the age gate correctly refused research at both Classical and
  Durg (this line's tier 1 gates on Imperial, not Durg) with zero deduction, then at
  Imperial deducted exactly 200 Gold/100 Wood; a War Elephant trained through the real
  slot-0 unique-unit path spawned as "Maurya Maha Gajaroha" at 156 HP; the real scene
  button correctly showed "Elephant (Max Tier)" on the real selected Durg; a second
  real Durg built for a Rajput-assigned faction confirmed the button correctly stays
  hidden for a civ with no elephant. No AI-side research hook (future balance work,
  same as every other tier line). Next: Wave 3 item 14 (Mangonel/Siege line, 3 tiers),
  user's call.
- **Age-aware building visuals + new asset import closed (2026-09-04)** —
  `docs/YOUR_ACTION_ITEMS.md` items 1-4. User supplied a fresh art delivery
  (`/Volumes/US/3D MODELS/`): TownCenter Ancient/Classical (2 shared) + 5 civ-specific
  Durg models, Tower/Wall Ancient/Classical/Durg (3 shared each), Lumber Camp/Mining
  Camp/Mill. Villager art was already fully wired from a prior session (staged files
  confirmed byte-identical via `md5`) — nothing to do there. **User-confirmed design
  decision**: Age-up re-skins ARE retroactive (every standing TownCenter/Tower/Wall a
  faction owns rebuilds its visual mesh in place the instant Age-up completes,
  AoE-style) — a deliberate one-off exception to this project's usual
  "baked in at spawn, not retroactive" convention. `BuildingModelFactory.Spawn`
  gained an optional `AgeId?` param (all 11 other, non-age-tiered factories
  unaffected) and a new `Refresh` method (destroys/rebuilds only the visual mesh +
  collider on an already-live building, leaving every gameplay component untouched —
  a pure re-skin, not a re-spawn); new `AgeTieredBuildingVisual.cs` marker + static
  `RefreshAllForFaction`, called from `TownCenter.TickAgeUp()` (the sole call site of
  `AgeProgress.Advance`) right after a faction's Age actually advances — covers
  Tower/Wall too, since they react to the same faction-wide event.
  **Resource-path convention** (load-bearing for future sessions — see
  `docs/SESSION_LOG.md`'s matching entry for the full spec): shared non-civ age tiers
  at `Buildings/{resourceName}_{ageId}`; civ-specific age tiers (TownCenter Durg only)
  at `Buildings/{civId}/{resourceName}_{ageId}`; Imperial keeps its original unsuffixed
  path, untouched. Extended `MeshyBuildingImporter.cs` with a new
  `ImportSharedBuilding` entry point for the non-civ assets. **Found and fixed a real
  import bug**: source folders on the external volume carry macOS AppleDouble shadow
  files (`._<name>`, same extension, ~4KB) that `Directory.GetFiles(...).
  FirstOrDefault()` could silently pick over the real 76MB+ asset with zero
  compile/console errors (a 4096-byte, 0-mesh prefab was the tell) — fixed by
  excluding `._`-prefixed filenames from every glob in the importer, permanently.
  Asset identification required real visual verification twice over, not name-trust:
  the 4 unlabeled Durg-Age TownCenter folders were matched to civs by their raw UV-atlas
  texture's dominant color/motifs (confirmed live against each civ's existing Imperial
  art); Tower's "Stonewatch_Tower"/"Stonewatch_Bastion" folder names turned out
  **backwards** from their actual tier content once screenshotted against the real
  Ancient/Classical/Durg reference art. Also re-hit (and re-fixed, same algebraic
  method as prior sessions) the documented Tower-specific
  `ImportRotationCorrections` runtime-stomp gotcha — baking a raw visually-verified
  rotation into a new Tower prefab without accounting for the civ-blind stomp
  produces a double-rotated result that only shows up through the real
  `BuildingModelFactory.Spawn` path, not a raw `Resources.Load` probe. All 289
  EditMode tests pass unmodified (pure asset-pipeline + visual-only code, no new
  test, matching every prior building-import session's convention). Live-verified via
  UnityMCP through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Rajput)`), a real Tower + Wall spawned for Player,
  then a real `TownCenter.RequestAgeUp()` forced through Classical→Durg→Imperial —
  `GetInstanceID()` on both confirmed unchanged at every step (re-skinned in place,
  not respawned), `Attackable`/`GarrisonPoint`/`FactionMember` intact, exactly one
  `BoxCollider` each (no duplicate leftover), and the correct model rendered at every
  tier via screenshot. Also confirmed a real `WorkerFactory.Spawn` villager still
  plays its Idle clip correctly through the existing `PlayableGraph`-based
  `AnimationDriver` (untouched this session). See `docs/SESSION_LOG.md`'s matching
  entry for full detail, including the exact scale/rotation numbers per building.
  Next: item 5 (per-civ unit gear, 12 sets × 3 pieces) or item 6 (Wave 4 new-unit
  models) whenever the user sources that art, or back to
  `docs/IMPLEMENTATION_ROADMAP.md`'s wave order for code-only work, user's call.
- **Wave 3 item 12 live-verification follow-up closed (2026-09-04)** — picked up
  exactly where the prior item 12 session left off (both `unity`/`UnityMCP` MCP
  servers were unreachable that whole session, so the code+tests shipped without any
  live verification). UnityMCP reachable again this session. Scene-wired
  `CavalryTierButton`/`CavalryTierLabel` (duplicated from `ArcherTierButton`, the
  exact predicted "new `[SerializeField]` null in the scene" gotcha every Wave 2/3
  session has hit), wired both fields on `BuildMenu`'s component, scene saved. Full
  EditMode suite: 289/289 pass. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Rajput)`), the age
  gate correctly refused research at both Ancient and Durg (this line's tier 1 gates
  on Imperial, not Durg, confirmed against `CavalryLineProgress.cs`'s own table), a
  real `RequestResearchCavalryTier()` deducted exactly 200 Gold/100 Wood at Imperial
  and completed via a forced real tick, a Cavalry trained afterward came out "Rajput
  Maha Ashvarohi" at 96.6 HP, and the real scene button's label ("Upgrade to Vir
  Ashvarohi (250 Gold, 125 Wood)") and `onClick.Invoke()` correctly paid tier 2's
  cost through `SelectionManager`-selected Barracks — the scene-wired button itself
  works end to end, not just the underlying method. No script changes this session
  (scene-only + docs). Next: Wave 3 item 13 (Elephant line, 2 tiers — needs a design
  decision first per its own roadmap text: one shared ladder for
  Maurya/Vijayanagara or two divergent ones), user's call.
- **Female + male Worker body swap closed (2026-09-04)** — not a queued roadmap
  item, picked up at the user's explicit direction (2 real rigged Meshy AI
  "Harvest Guardian" villager glTF models supplied directly, one female one
  male). `HumanModelFactory.Gender.Female` was used by exactly one factory
  (`WorkerFactory.cs`), so this is a Worker-body swap across all 5 civs, not a
  general asset drop — every combat unit still uses the original shared Human
  Character Dummy body via `Gender.Male`, untouched. New reusable
  `Assets/Editor/HumanoidGltfRigImporter.cs` builds a real Humanoid `Avatar`
  for a Mixamo-style-named glTF rig (glTFast's own import doesn't auto-build
  one, unlike native FBX) via `AvatarBuilder.BuildHumanAvatar` + a
  hand-authored `HumanDescription`, with the skeleton bone array read directly
  off the model's own instantiated transform hierarchy (never hand-
  transcribed). Both models imported to `Assets/Resources/human/
  FemaleVillager|MaleVillager/`, scaled to the same measured 1.902692 worker-
  height convention every prior session has used. `WorkerFactory.cs` now
  randomly picks the female or male body per spawn (`Random.value < 0.5f`, no
  gameplay difference — pure crowd variety), `applyPaletteMaterial: false`
  (each model keeps its own painted identity texture matching its own concept
  art, same convention as the 3 existing Meshy-sourced unique units, not a
  civ-palette trim sheet) with `HumanAnimationSet.LoadFor(villagerGender)`
  picking the matching clip set. Confirmed live via UnityMCP that the
  project's existing shared 7-clip human animation library (Idle/Walk/Gather/
  Mine/Farm/Build/Attack) retargets cleanly onto both new rigs with zero new
  animation authoring (Mecanim retargeting is Avatar-based, not
  skeleton-name-based) — so the models' own bundled walk/run clips turned out
  unnecessary and were left unwired. All 289 pre-existing EditMode tests pass
  unmodified (no new tests — pure asset-pipeline + a small factory change,
  matching every prior `HumanModelFactory` session's own convention).
  Live-verified through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), 4 real spawned Workers (2 female
  body, 2 male body, confirmed via reflection), correct scale/ground
  alignment/texture next to a real TownCenter and livestock, a real Walk-clip
  pose showing genuine leg articulation (not T-posed), and a real
  reflection-forced Gather-clip pose showing a correctly bent reaching stance
  — both proving retargeting actually works, not just that the Avatar
  reports valid. This session's own scratch investigation import deleted once
  the real Resources-path prefabs were confirmed working. The
  `HumanoidGltfRigImporter.cs` utility is directly reusable for the still-
  blocked Crusader Knight combat-unit body swap, or any other future
  glTF-rigged body, once new source files exist. Next: back to
  `docs/IMPLEMENTATION_ROADMAP.md`'s wave order, or whatever the user directs.
- **Now working from `docs/IMPLEMENTATION_ROADMAP.md`'s wave order (the new
  AoE-parity execution plan), strictly in wave order, one item per session —
  see that file's own ground rules. `docs/ROADMAP.md` Section 5's priority
  order is the prior plan; items already closed under it stay closed, but new
  work picks up from `IMPLEMENTATION_ROADMAP.md` instead.**
- **Wave 3 item 12 (Knight/Cavalry line, 3 tiers) closed (2026-09-04) - code and
  tests only, live UnityMCP verification still outstanding.** Picked up right
  after item 11 per the user's "wave 3 item 12 start" request. No new design
  decisions needed - item 12's own roadmap text already fixes tier count/names/
  ages (Ashvarohi → Maha Ashvarohi/Durg → Vir Ashvarohi/Imperial - note both
  upgrade tiers gate on Imperial, per the roadmap's own "Durg/Imperial/Imperial"
  spec), and the mechanism (research on Barracks, baked in at spawn, not
  retroactive) was already established by items 9-11. Confirmed tier 0's
  `RequiredAge` is descriptive only (never enforced), so `RequestTrainCavalry`
  keeps its existing no-age-gate behavior - only the tier ladder above it is
  new. New `Progression/CavalryLineProgress.cs` mirrors `SpearmanLineProgress.cs`'s
  shape exactly; tier bonuses/costs reuse `InfantryLineProgress`'s own
  back-to-back Imperial pair (Maha Khandayata → Vir Yodha) at matching gates -
  the closest existing precedent for two successive Imperial-gated tiers - rather
  than independently balanced (Maha Ashvarohi = +30 HP/+6 dmg/200 Gold/100
  Wood/40s; Vir Ashvarohi = +45 HP/+9 dmg/250 Gold/125 Wood/50s). New independent
  research track on `Barracks.cs` (`RequestResearchCavalryTier`, alongside the
  existing Infantry/Spearman/Archer tracks), `CavalryFactory.cs` now reads
  `CavalryLineProgress.Current(faction)` at spawn. New `cavalryTierButton`/
  `cavalryTierLabel` in `BuildMenu.cs`, hotkey M, wired into
  `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope
  call as items 9-11: no AI-side research hook; Rajput Royal Guard untouched. 10
  new EditMode tests (`CavalryLineTests.cs`, mirroring `SpearmanLineTests.cs`).
  **Could not live-verify via UnityMCP this session**: both the `unity` and
  `UnityMCP` MCP servers failed to connect (`ConnectionRefused`) for this entire
  session despite a real Unity Editor instance actively running and compiling
  the new files - checked `~/Library/Logs/Unity/Editor-prev.log` and the
  project's own `Logs/` directory directly (this project's own "console bridge
  can miss real errors" convention) and found no compile errors, but could not
  run the Unity Test Runner or drive a real match/button click the way every one
  of items 9-11 did. **Flagging directly, not glossing over it**: the new
  `cavalryTierButton`/`cavalryTierLabel` `[SerializeField]` fields are almost
  certainly null in the scene right now (the exact recurring gotcha every Wave
  2/3 session has hit) and still need the scene-wiring fix (duplicate
  `ArcherTierButton` into a real `CavalryTierButton` scene object) plus a real
  live-verification pass once UnityMCP reconnects. Next: retry live
  verification for this item once Unity/UnityMCP is reachable, then Wave 3 item
  13 (Elephant line, 2 tiers - needs a design decision first per its own
  roadmap text: one shared ladder for Maurya/Vijayanagara or two divergent
  ones), user's call.
- **Wave 3 item 11 (Archer line, 3 tiers) closed (2026-09-04).** Picked up right
  after item 10 per the user's "start wave 3 item 11" request. No new design
  decisions needed — item 11's own roadmap text already fixes tier count/names/ages
  (Dhanurdhara → Yantra Dhanurdhara/Durg → Maha Dhanurdhara/Imperial), and the
  mechanism (research on Barracks, baked in at spawn, not retroactive) was already
  established by items 9/10 in the same wave. New `Progression/ArcherLineProgress.cs`
  mirrors `SpearmanLineProgress.cs`'s shape exactly (same Classical/Durg/Imperial
  gate pattern); tier bonuses/costs reuse SpearmanLineProgress's own values at
  matching age gates rather than independently balanced (Yantra Dhanurdhara =
  Trishuladhari's Durg-gate growth: +18 HP/+4 dmg/120 Gold/60 Wood/25s; Maha
  Dhanurdhara = Maha Trishuladhari's Imperial-gate growth: +30 HP/+6 dmg/200
  Gold/100 Wood/40s). New independent research track on `Barracks.cs`
  (`RequestResearchArcherTier`, runs alongside the existing Infantry/Spearman tier
  tracks without blocking them), `ArcherFactory.cs` now reads
  `ArcherLineProgress.Current(faction)` at spawn. New `archerTierButton`/
  `archerTierLabel` in `BuildMenu.cs` (identical shape to
  `infantryTierButton`/`spearmanTierButton`), hotkey H, wired into
  `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same explicitly-out-of-scope
  call as items 9/10: no AI-side research hook (future balance work, not a
  regression). 10 new EditMode tests (`ArcherLineTests.cs`, mirroring
  `SpearmanLineTests.cs` — 279 total, all pass). Hit the same environment gotcha
  every Wave 2/3 session has documented (new `[SerializeField]` fields null in the
  scene) — fixed the same way, duplicating `SpearmanTierButton` into a real
  `ArcherTierButton` scene object via UnityMCP. Live-verified via UnityMCP through
  the real production path: a real match (`CivilizationSetup.BeginMatch(Rajput)`,
  deliberately not Maurya/Maratha — see item 9's own flagged `UniqueTechDefinition`
  bug, `task_55dbb0cc`, still open and unrelated to this item), a real
  `RequestResearchArcherTier()` correctly refused at Ancient age even with 1000
  Gold/Wood on hand, then deducted exactly 120 Gold/60 Wood at Durg; forcing the real
  `Update()` tick advanced the tier and an Archer spawned afterward came out
  "Rajput Yantra Dhanurdhara" at 47.61 HP (base 18 + 18 bonus, scaled by Rajput's own
  profile/age multipliers; not retroactive, matching the established convention); the
  real button's label and `onClick.Invoke()` correctly showed and paid the Maha
  Dhanurdhara cost (200 Gold/100 Wood) once Player reached Imperial. Next: Wave 3
  item 12 (Knight/Cavalry line, 3 tiers), user's call.
- **Wave 3 item 10 (Spearman line, 3 tiers) closed (2026-09-04).** Picked up right
  after item 9 per the user's "start wave 3 remaining item" request. No new design
  decisions needed — item 10's own roadmap text already fixes tier count/names/ages
  (Bhaladhari → Trishuladhari/Durg → Maha Trishuladhari/Imperial), and the mechanism
  (research on Barracks, baked in at spawn, not retroactive) was already established
  by item 9 in the same wave. New `Progression/SpearmanLineProgress.cs` mirrors
  `InfantryLineProgress.cs`'s shape exactly; tier bonuses/costs scaled off Infantry's
  own curve at matching age gates rather than independently balanced
  (Trishuladhari = Khandayata's Durg-gate growth: +18 HP/+4 dmg/120 Gold/60 Wood/25s;
  Maha Trishuladhari = Maha Khandayata's Imperial-gate growth: +30 HP/+6 dmg/200
  Gold/100 Wood/40s). New independent research track on `Barracks.cs`
  (`RequestResearchSpearmanTier`, runs alongside the existing Infantry tier track
  without blocking it), `SpearmanFactory.cs` now reads
  `SpearmanLineProgress.Current(faction)` at spawn. New `spearmanTierButton`/
  `spearmanTierLabel` in `BuildMenu.cs` (identical shape to `infantryTierButton`),
  hotkey L, wired into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. Same
  explicitly-out-of-scope call as item 9: no AI-side research hook (future balance
  work, not a regression). 10 new EditMode tests (`SpearmanLineTests.cs`, mirroring
  `InfantryLineTests.cs` — 269 total, all pass). Hit the same environment gotcha
  every Wave 2/3 session has documented (new `[SerializeField]` fields null in the
  scene) — fixed the same way, duplicating `InfantryTierButton` into a real
  `SpearmanTierButton` scene object via UnityMCP. Live-verified via UnityMCP through
  the real production path: a real match (`CivilizationSetup.BeginMatch(Rajput)`,
  deliberately not Maurya/Maratha — see item 9's own flagged `UniqueTechDefinition`
  bug, `task_55dbb0cc`, still open and unrelated to this item), a real
  `RequestResearchSpearmanTier()` correctly refused at Ancient age even with funds
  on hand, then deducted exactly 120 Gold/60 Wood at Durg; forcing the real
  `Update()` tick advanced the tier and a Spearman spawned afterward came out
  "Rajput Trishuladhari" at 70.0925 HP (not retroactive to already-spawned units,
  matching the established convention); the real button's label and
  `onClick.Invoke()` correctly showed and paid the Maha Trishuladhari cost (200
  Gold/100 Wood) once Player reached Imperial. Next: Wave 3 item 11 (Archer line, 3
  tiers), user's call.
- **Wave 3 item 9 (Infantry line, 5 tiers) closed (2026-09-04) — first Wave 3
  item.** Picked up at the user's "start wave 3" request. Resolved 3 design
  decisions via AskUserQuestion before coding: research lives on Barracks
  (not Karmashala), each tier renames the unit and improves stats but reuses
  the existing Human Character Dummy model (no new art needed), and progress
  is NOT retroactive (matches every other progression system here). New
  `Progression/InfantryLineProgress.cs` (hardcoded tier table: name/required
  age/HP+damage bonus/gold+wood cost/research time), a new independent
  research track on `Barracks.cs` (`RequestResearchInfantryTier`), and
  `SoldierFactory.cs` now reads the current tier at spawn — genuinely "tier 1
  of a ladder." No CommandBus/network wiring needed (Barracks' existing
  `RequestTrain()` is untouched — only what's baked in at spawn changes). New
  `infantryTierButton`/`infantryTierLabel` in `BuildMenu.cs` (hotkey I),
  wired into `SettingsMenu`/`HotkeyOverlay`'s `BarracksGroup`. No AI-side
  research hook added — not a regression (nothing existing broke), matches
  the "never wired" status this project's own per-class Attack/Armor tracks
  already have; flagged as future balance work, not fixed. 10 new EditMode
  tests (`InfantryLineTests.cs`, 255 total, up from 245, all pass). Hit the
  same "new SerializeField null in scene" gotcha Durg/Karmashala's sessions
  already documented — fixed by duplicating `UniqueTechButton` into a real
  `InfantryTierButton` scene object via UnityMCP. Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), the age gate correctly blocked
  research at Classical ("needs Durg Age") and unblocked after
  `AgeProgress.Advance` to Durg, a real `RequestResearchInfantryTier()`
  deducted the exact Gold/Wood cost and completed via the real `Update()`
  tick (forced via reflection, not a test shortcut), a Soldier spawned
  *before* completion stayed unchanged ("Maurya Padati", 33 HP) while one
  spawned *after* came out "Maurya Senani" at 41.8 HP — not retroactive,
  confirmed live — and the real button's `onClick.Invoke()` correctly routed
  through to the same method. **Found, not fixed (real pre-existing bug,
  unrelated to this item)**: selecting a real Maurya Barracks and driving
  `BuildMenu.Update()` throws `KeyNotFoundException` inside
  `UniqueTechDefinition.For` — its `Bonuses` dictionary only covers
  Chola/Vijayanagara/Rajput, not Maurya/Maratha. Flagged via `spawn_task`
  (`task_55dbb0cc`) for a dedicated follow-up rather than silently left
  unnoticed; worked around it in this session's own verification by invoking
  the new `UpdateInfantryTierButton` method directly via reflection instead
  of through the crashing `Update()`. See `docs/SESSION_LOG.md`'s matching
  entry for full detail. Next: Wave 3 item 10 (Spearman line, 3 tiers),
  user's call — or the newly-flagged Maurya/Maratha UniqueTechDefinition bug.
- **Wave 2 item 8 (Karmashala, the Blacksmith-equivalent building) closed
  (2026-09-04) — this closes Wave 2.** Picked up right after item 7 per the
  user's "start wave 2 item 8" request. Resolved the item's two design
  decisions via AskUserQuestion before coding: gated to Classical Age (same
  as Barracks, not a late-game unlock like Durg), and the flat Attack/Armor
  research tracks (`RequestResearchAttack`/`RequestResearchArmor`) move OFF
  `Barracks` onto Karmashala entirely — mirrors how Durg took unique-unit
  training off Barracks last session. Barracks' per-class Attack/Armor
  tracks (item 40, never wired to any UI button) and civ UniqueTech stay on
  Barracks/Durg untouched. New `Buildings/Karmashala.cs`/`KarmashalaFactory.cs`
  (`MillFactory.cs` as the factory template, Barracks' own research code as
  the component template): 150 Wood only, 10s build, 220 HP/1-2 armor
  (deliberately "raidable," between Mill's 200 HP and Barracks' 300),
  3-tile/Market-sized footprint. Wired through the full stack:
  `BuildingPlacer`'s `CanPlaceKarmashala`, `NetBuildKind.Karmashala`
  (build-only — research itself was already un-networked before this
  session, a pre-existing gap not fixed here), `BuildMenu`'s
  `attackUpgradeButton`/`armorUpgradeButton` re-gated from a selected
  Barracks to a selected Karmashala (new `UpdateKarmashalaButtons`, split
  out of `UpdateBarracksButtons` the same way `UpdateDurgButtons` split out
  unique-unit training last session) plus one new placement button
  (`karmashalaButton`, added via UnityMCP scene editing — see gotcha
  below), `SettingsMenu`/`HotkeyOverlay` updated to match (new
  `KarmashalaGroup`, mirroring `DurgGroup`'s own split from
  `BarracksGroup`). **A real regression was caught and fixed before it
  shipped, same class of bug Durg's session hit**: the AI opponent
  researched Attack/Armor via `_barracks.RequestResearchAttack()/
  RequestResearchArmor()` in `TryResearchUpgrades()` — moving those off
  Barracks with no AI-side Karmashala would have silently ended the AI's
  flat Attack/Armor research forever. Fixed with a `TryBuildKarmashala()`/
  `AssignKarmashalaBuilderIfNeeded()` pair mirroring `TryBuildDurg()`'s own
  shape, and `TryResearchUpgrades()` now checks `_karmashala` first,
  independently of the `_barracks` guard below it — no Karmashala yet means
  the AI simply skips flat Attack/Armor research for now, same
  "silently no-ops if unavailable" convention `TryTrainSoldiers`' Durg
  fallback already established. 9 new EditMode tests (`KarmashalaTests.cs`,
  245 total, up from 236, all pass) — building factories deliberately not
  exercised there, same documented NRE-outside-Play-mode limitation as
  `BarracksFactory.Place`/`DurgFactory.Place`. **Hit the exact same
  environment gotcha Durg's session already documented**: the new
  `karmashalaButton`/`karmashalaLabel` `[SerializeField]` fields were null
  in the scene (added to the C# class but never wired to a GameObject) —
  `BuildMenu.Update()` NRE'd every frame with zero errors surfaced by the
  MCP console bridge; only reflection-invoking `Update()` directly inside a
  try/catch surfaced the real stack trace pointing at
  `SetPlacementButtonsActive`. Fixed the same way: duplicated `DurgButton`
  into a real `KarmashalaButton` scene GameObject via UnityMCP and wired
  the component fields to it. Live-verified via UnityMCP through the real
  production path after that fix: a real match
  (`CivilizationSetup.BeginMatch(Chola)`, which starts at Ancient — Maurya's
  own Classical-start convention would have made the Ancient-age gate check
  meaningless), `CanPlaceKarmashala` false in Ancient/true in Classical, a
  real spawned Karmashala + a real spawned Barracks selected in turn showed
  exactly the right button sets (`attackUpgradeButton`/`armorUpgradeButton`
  active only on Karmashala, `soldierButton` active only on Barracks), a
  real button click on `attackUpgradeButton` deducted Gold and started
  research through the real `RequestResearchAttack` path, a real
  `karmashalaButton` click correctly entered `BuildingPlacer.IsPlacing`, and
  the AI's own `TryBuildKarmashala`/`AssignKarmashalaBuilderIfNeeded`/
  `TryResearchUpgrades` chain built a real Karmashala, deducted Wood, then
  deducted Gold and started research once complete — all through the real
  production path, not test shortcuts. **Flagged, not fixed**: `Karmashala`
  has no bespoke 3D model yet (falls back to the generic procedural shape,
  same as Durg/Lumber Camp/Mining Camp/Mill before their models existed) —
  needs real art sourced later. **This closes Wave 2** — both structural
  buildings every later unique-unit and upgrade-line item implicitly
  assumes exist, now actually exist. See `docs/SESSION_LOG.md`'s matching
  entry for full detail. Next: Wave 3 (upgrade ladders, one line per
  session), user's call.
- **Wave 2 item 7 (the Durg building) closed (2026-09-04).** Picked up at the
  user's explicit "START WAVE 2" request. Resolved the item's two flagged
  open design decisions via AskUserQuestion before coding: it trains unique
  units (relocated off `Barracks` — matches AoE's Castle-trains-uniques
  convention), and it's the strongest defensive building in the game,
  strictly above `TownCenter` on every axis (`GarrisonCapacity` 12 vs. 8, 6
  max bonus shots vs. 4, 700 HP/4-6 armor vs. 500/3-5, 12 dmg/range 9/1.2s
  interval vs. 8/8/1.4). New `Buildings/Durg.cs`/`DurgFactory.cs`
  (`Barracks.cs`/`TownCenterFactory.cs` templates); `Barracks.cs`'s
  `RequestTrainUniqueUnit`/`UniqueUnitCount`/`UniqueUnitAt`/`UniqueUnit`
  deleted (everything else on Barracks untouched); `BuildingPlacer.cs` gained
  a `Durg` kind (Age-gated to Durg+, 200 Wood/150 Stone, 25s, hotkey D);
  `NetMessage.cs`/`CommandSerializer.cs` gained matching wire support;
  `BuildMenu.cs` re-gated its existing unique-unit buttons from `Barracks` to
  `Durg` and gained one genuinely new placement button (`durgButton`, wired
  via UnityMCP scene editing — see gotcha below). **A real regression was
  caught and fixed before it shipped**: the AI opponent trained its unique
  unit via `_barracks.RequestTrainUniqueUnit()`; moving that off Barracks
  with no AI-side Durg would have silently ended the AI's unique-unit
  training forever — fixed with a `TryBuildDurg()`/
  `AssignDurgBuilderIfNeeded()` pair mirroring `TryBuildBarracks()`'s own
  shape, plus a Soldier fallback in the AI's training rotation until its own
  Durg is complete. 4 new EditMode tests (`DurgTests.cs`) plus 2 existing
  unique-unit tests updated to build a `Durg` instead of a `Barracks` (236
  total, up from 232, all pass). **Hit a real environment gotcha mid-session,
  worked through not around**: the new `durgButton`/`durgLabel`
  `[SerializeField]` fields were null in the scene (added to the C# class but
  never wired to a GameObject), which made `BuildMenu.Update()` NRE every
  frame with **zero errors surfaced by the MCP console bridge** — same
  "console bridge can miss real errors" gotcha this project has hit before
  for compile errors, now also seen for a runtime exception; only
  reflection-invoking `Update()` directly inside a try/catch surfaced the
  real stack trace. Fixed by actually duplicating `MillButton` into a new
  `DurgButton` scene GameObject via UnityMCP and wiring the component fields
  to it — a real scene edit, not a code-only fix; the lesson: a new
  `[SerializeField]` UI field on an existing hand-wired class needs a
  matching scene edit in the same session. Live-verified via UnityMCP through
  the real production path after that fix: a real match
  (`CivilizationSetup.BeginMatch(Maurya)`), `CanPlaceDurg` false pre-Durg-age
  and true after `AgeProgress.Advance`, a real spawned Durg's live stats
  matched every constant exactly via reflection, selecting a real Barracks
  vs. a real Durg showed exactly the right button sets with correct
  civ-specific unique-unit labels, a real button click enqueued training
  through `CommandBus` and spent/spawned correctly a couple of ticks later,
  and a real placement-button click correctly entered
  `BuildingPlacer.IsPlacing`. **Flagged, not fixed**: `Durg` has no bespoke
  3D model yet (falls back to the generic procedural shape, same as Lumber
  Camp/Mining Camp/Mill before their models existed) — needs real art
  sourced later. Wave 2 item 8 (Karmashala) remains open; Wave 2 isn't fully
  closed yet. See `docs/SESSION_LOG.md`'s matching entry for full detail.
  Next: Wave 2 item 8 (Karmashala), user's call.
- **Wave 1 item 6 (age-up building-count requirement) closed (2026-09-04) —
  this closes Wave 1.** Resolved the design question flagged when item 5
  closed via AskUserQuestion: "2 buildings" means 2 completed, non-TownCenter
  buildings currently owned (no per-age building taxonomy — that stays
  materially out of scope), and the gate applies from Classical onward only
  (Ancient→Classical stays cost-only). New `Buildings/AgeUpRequirement.cs`
  wired into `TownCenter.RequestAgeUp()` alongside the existing Wood/Stone
  check, and into `BuildMenu`'s Age-up button label. 11 new EditMode tests
  (232 total, up from 221, all pass) — hit this project's own documented
  `Building.OnEnable`-isn't-synchronous-in-EditMode-tests gotcha, fixed the
  same way `BuildingFootprintTests` already does (register directly into
  `Building.All`). Live-verified via UnityMCP through the real production
  path: a real match (`CivilizationSetup.BeginMatch(Rajput)`), a real
  `TownCenter.RequestAgeUp()` correctly exempted Ancient→Classical with zero
  extra buildings, then correctly refused Classical→Durg with zero extra
  buildings (no resources spent) — then real `HouseFactory.Place`/
  `BarracksFactory.Place` + `ConstructionSite.CompleteImmediately()` brought
  the Player to 2 real buildings and the identical request immediately
  succeeded. Wave 1's exit criteria are all met. Next: Wave 2 (the Durg
  building / Karmashala), user's call.
- **Wave 1 item 5 (add `AgeId.Durg`) closed (2026-09-04).** `AgeId` now has 4
  values (Ancient/Classical/Durg/Imperial), `age_profile_template.csv` has a
  new interpolated Durg row (250 Wood/150 Stone, 40s research, gather x1.18,
  HP x1.15, train x0.85 — deliberately no Gold cost, since the schema has
  never had one for any age), `AgeProfile.cs`'s `Fallback`/`AgeIds`
  dictionaries updated to match. 6 new EditMode tests (221 total, up from
  215, all pass). Live-verified via UnityMCP through the real production
  path: a real match, a real Player `TownCenter.RequestAgeUp()` correctly
  spent Durg's exact cost and, after its real 40s `Update()`-ticked
  countdown, landed `AgeProgress.CurrentAge(Player)` on `Durg`; a second real
  age-up from Durg correctly targeted Imperial (cost/progress both
  confirmed). **Item 6 (age-up building-count requirement) explicitly
  deferred, not started** — the user chose to skip its design decision
  (what "2 buildings" should mean, given this codebase has no per-age
  building taxonomy) rather than force a definition this session; see
  `docs/IMPLEMENTATION_ROADMAP.md`'s matching item for the open question.
  Wave 1's exit criteria are otherwise met (a player can reach Durg/Imperial
  in a real match). Next: item 6 if you want the design question resolved,
  or Wave 2 (the Durg building / Karmashala), user's call.
- **Wave 0 item 4 (wire `DamageType.Trample`/`Fire`) closed (2026-09-04) — this
  closes Wave 0 (items 1-4 all done).** Trample wired, Fire correctly deferred
  to its real Wave 4 consumer (the Fire Ship). First resolved the enum
  duplication item 1 flagged as this item's own prerequisite:
  `Combat/Attackable.cs`'s `DamageType` (previously Melee/Pierce only) is now
  the single shared enum (Melee/Pierce/Siege/Fire/Trample) — the second,
  incompatible global `DamageType` on `UnitDefinition.cs` (unread CSV
  metadata) is deleted, `UnitDefinition.attackType` now references the shared
  type directly. `Attackable.TakeDamage` gained an explicit
  `UsesPierceArmor(DamageType)` helper (Pierce/Fire → pierceArmor;
  Melee/Trample/Siege → meleeArmor) so the armor lookup and the
  upgrade-scaling-applies check can't drift apart. `MauryaWarElephantFactory.cs`/
  `VijayanagaraWarElephantFactory.cs` now call `SetDamageType(DamageType.Trample)`
  + `SetSplashRadius(1.4f, 0.4f)`, reusing `CavalryFactory`'s own already-proven
  trample-splash mechanism (a shade larger/heavier than Cavalry's 1.25/0.35,
  still below `GroupFormation`'s 1.5 default spacing). `unit_roster_template.csv`'s
  `AttackType` column updated Melee→Trample for both war elephants, regenerated
  via `BharatRTS/Generate Data Assets From CSV`. 2 new EditMode tests
  (`TrampleDamageTests.cs`, 215 total, up from 213, all pass): Trample resolves
  against meleeArmor not pierceArmor; a Trample-tagged splash attacker damages a
  nearby hostile but not a far one. Live-verified via UnityMCP through the real
  production path: a real match (`CivilizationSetup.BeginMatch(Maurya)`), real
  `MauryaWarElephantFactory`/`VijayanagaraWarElephantFactory`-spawned units both
  confirmed (via reflection) to carry `damageType=Trample`/`splashRadius=1.4` at
  spawn, and a real Maurya War Elephant attacking 3 real `SoldierFactory`-spawned
  Soldiers dealt full damage to the primary target (30→20 HP), reduced splash
  damage to one placed inside the trample radius (30→26.6 HP), and zero damage
  to one placed outside it (30→30 HP). See `docs/SESSION_LOG.md`'s matching
  entry for full detail. Wave 0 is fully closed — next is Wave 1, user's call.
- **Wave 0 item 3 (confirm the minimum-damage clamp) closed (2026-09-04)** —
  verify-only, no bug found: `Attackable.TakeDamage` (`Combat/Attackable.cs:160`,
  `Mathf.Max(1f, amount - armor)`) already applies its 1-damage floor as the
  *last* step, after armor subtraction, and every real call site
  (`MeleeAttacker.ResolveHit`/`BoatAttacker.Tick`/`BuildingAttacker.Tick`)
  passes the already counter-multiplied `baseDamage * CombatBonus.Multiplier(...)`
  into `TakeDamage` — so a stacked armor value plus a sub-1.0 `CombatBonus`
  multiplier can't silently floor a hit at 0, structurally, not just for
  today's stat ranges. 4 new EditMode tests (`DamageClampTests.cs`, 213
  total, up from 209, all pass) — hit this project's own documented "two
  `DamageType` enums" ambiguous-reference gotcha along the way (fixed by
  fully qualifying `KingdomsOfBharat.Combat.DamageType.Melee`/`.Pierce`,
  same fix as Wave 0 item 2/`WildBoar.cs`), and confirmed again that the MCP
  console bridge can report zero errors while a new file is fully failing to
  compile — the real `CS1503` errors were only visible in
  `~/Library/Logs/Unity/Editor.log` directly. Live-verified via UnityMCP
  through the real production path: a real match
  (`CivilizationSetup.BeginMatch`), a real `CavalryFactory`-spawned Cavalry
  unit (0.4x hard-countered vs. Archer, `CombatBonus`'s real matchup)
  attacking a real `ArcherFactory`-spawned Archer configured with 500 melee
  armor — the real `MeleeAttacker.ResolveHit`/`Attackable.TakeDamage` path
  dealt exactly 1 damage, not 0. See `docs/SESSION_LOG.md`'s matching entry
  for full detail, including a first verification attempt that (correctly)
  failed to clamp because it stacked the wrong armor field against a
  Melee-type attacker — confirming the test setup itself was sensitive
  enough to catch a real miss. Next: Wave 0 item 4 (wire
  `DamageType.Trample`/`Fire` — also needs the separate `DamageType` enum
  duplication resolved first, per item 1's own note).
- **Wave 0 item 2 (verify the retroactive upgrade rule) closed (2026-09-04)**
  — confirmed `Progression/UpgradeProgress.cs` did NOT promote already-
  spawned units; its own comment said so explicitly ("baked in at spawn,
  not retroactive"), and the bonus was in fact read once per unit at spawn
  time by 13 combat-unit factories. Fixed: `Attackable`/`MeleeAttacker`/
  `BoatAttacker` now live-read `UpgradeProgress`'s bonus at damage-
  resolution time (using each unit's already-stored `FactionMember` +
  `unitClass`) instead of a value baked in once. Deliberately **opt-in**,
  not automatic — new `EnableUpgradeArmorScaling(melee, pierce)`/
  `EnableUpgradeDamageScaling()` calls, made only by the same 13 factories
  that already baked this bonus in before the fix (Soldier/Archer/Cavalry/
  Spearman/Siege/CholaNavalRaider/MauryaWarElephant/VijayanagaraWarElephant/
  RajputRoyalGuard/MarathaMavlaRaider/MarathaDurgGarrison/PillarEdictScholar/
  WarGalley) — buildings and Workers never baked this in and still don't
  opt in, so they correctly stay untouched by Blacksmith-style research,
  same as before. Preserved (not "fixed") an existing asymmetry:
  Archer/CholaNavalRaider apply the armor bonus to `pierceArmor` only,
  matching AoE's own pierce-specific "Archer Armor" line — the new
  `EnableUpgradeArmorScaling` takes independent melee/pierce flags for
  this. `CavalryFactory`'s Rajput unique-tech damage bonus
  (`UniqueTechProgress`, a separate mechanic, already documented as its
  own "not retroactive" convention) was left untouched, out of scope. 5
  new EditMode tests (209 total, up from 204, all pass) — hit and fixed
  this project's own documented "two `DamageType` enums" ambiguous-
  reference gotcha along the way (same class of bug `WildBoar.cs` hit
  before), same fix (fully-qualify
  `KingdomsOfBharat.Combat.DamageType.Melee`/`.Pierce`). Live-verified via
  UnityMCP through the real production path: a real match, a real
  `SoldierFactory`-spawned Soldier took 9 damage from a fixed 10-damage
  melee hit pre-research, then 8 damage from the identical hit on the SAME
  GameObject post-`UpgradeProgress.AdvanceArmor` (no respawn) — exactly
  `ArmorPerTier`'s 1-point improvement; a real Worker spawned after that
  same tier was already researched still took the full 10 damage (the
  opt-in scope guard holds live, not just in test fixtures); a real
  Archer's melee hit ignored the researched tier while its pierce hit
  reflected it, confirming the preserved asymmetry live. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Next: Wave 0
  item 3 (confirm the minimum-damage clamp) or item 4 (wire
  `DamageType.Trample`/`Fire`).
- **Wave 0 item 1 (reconcile UnitClass vs UnitCategory) closed (2026-09-04)**
  — collapsed the two overlapping "what kind of combatant is this" enums into
  one: `KingdomsOfBharat.Combat.UnitClass` now carries all 9 values (added
  `Support`/`Hero`), the former global `UnitCategory` enum is deleted, and
  every call site (`CounterMatrix`/`TechNode`/`FormationDefinition`/
  `CivilizationProfile`/`Barracks`/`Dock`/`WorkerFactory`/`CavalryFactory`/
  `CsvToScriptableObject`) reads the shared type. `FormationController`'s
  lossy translation shim (`MapUnitClass`) is gone — `CategoryOf` now reads
  `Attackable.Class` directly. `CombatBonus.Multiplier`'s own hand-tuned
  pairings are untouched (Support/Hero simply fall through to the existing 1x
  default, same as any other unlisted pairing) — this is not a merge of the
  CombatBonus/CounterMatrix *systems* (still deliberately separate, see this
  file's own gotcha below), just their shared vocabulary type. All 204
  EditMode tests pass unmodified; live-verified via UnityMCP: regenerated
  every CSV-driven asset (`BharatRTS/Generate Data Assets From CSV`, zero
  parse warnings, `worker` unit's `category` correctly reads `Support`),
  confirmed `CombatBonus.Multiplier(Archer, Cavalry)` still resolves to the
  audited 2.0x, and confirmed a real `FormationController.ComputeOffsets`
  call still places an Infantry unit at the front rank and an Archer unit at
  the back rank under the simplified direct read. **Flagged, not fixed**: a
  separate, adjacent enum duplication noticed while reading `Attackable.cs`
  — `KingdomsOfBharat.Combat.DamageType` (2 values: Melee/Pierce, what
  `Attackable.TakeDamage` actually uses) vs. the global `DamageType` in
  `UnitDefinition.cs` (5 values, including `Trample`/`Fire`) — is a different
  divergence, directly relevant to Wave 0 item 4 (wiring `Trample`/`Fire`),
  not this item. See `docs/SESSION_LOG.md`'s matching entry for full detail.
  Next: Wave 0 item 2 (verify the retroactive upgrade rule), item 3 (confirm
  the minimum-damage clamp), or item 4 (wire `DamageType.Trample`/`Fire` —
  now also needs the `DamageType` duplication resolved first).
- Prior plan's status (kept for history — see below for the full detail):
  working from Roadmap Section 5's priority order.
- **Fixed (2026-09-03): the Objectives tab `RectMask2D` over-culling bug
  flagged (not fixed) at the end of session 6 below, via `task_545a0590`'s
  follow-up.** Root cause: not an engine bug — every row/label/field under
  the growing `_objectivesScrollContent` was anchored to that rect's
  shifting CENTER `(0.5,0.5)` instead of its fixed TOP edge, so as content
  grew past ~7 rows the anchor drift pushed rows genuinely outside the
  scroll viewport's clip rect (RectMask2D was correctly culling them, just
  not where anyone intended). Fixed via a new `anchorTop` parameter on
  `CreateLabel`/`CreateButton`/`CreateInputField` (default `false`,
  every other call site unaffected), threaded through every creator call
  parented to `_objectivesScrollContent`. 204 EditMode tests pass
  unmodified (pure anchor-data change). Live-verified via UnityMCP: real
  10-row Objectives tab, confirmed early rows land at their exact intended
  `anchoredPosition` and render, confirmed the rows still reading
  `culled=true` at default scroll are legitimately below the viewport
  (scrolling to the bottom correctly un-culls exactly those) — i.e. what's
  left is real scroll clipping, not the bug. Screenshotted the real Game
  View. See `docs/SESSION_LOG.md`'s matching entry for full detail. Next:
  another Roadmap Section 5 item, user's call.
- **Scenario Editor heavy path, session 6 (per-kind bespoke input
  widgets) closed (2026-09-03) — this closes the entire Scenario Editor
  heavy-path epic.** Picked up at the user's explicit request ("start
  item on per-kind bespoke input widgets"), the last named item from the
  epic's original deferred list. Replaced session 2's generic "Param1"/
  "Param2" text fields with a per-Kind `ParamFieldSpec[]` table
  (`ObjectiveFieldSpecs`/`TriggerFieldSpecs`) driving a real widget per
  param: `FactionId`/`ResourceType`/building-type slots render as a new
  `BindEnumCycleField` cycle-on-click button (mirroring the Kind-cycle
  button's own idiom); genuinely free values (seconds, counts, position)
  stay plain text fields. `DestroyScriptedTarget`'s target-building
  widget is deliberately narrower than `BuildingCountThreshold`'s
  (`{"Barracks","TownCenter"}` only) — matches
  `MissionCsvLoader.SpawnScriptedTarget`'s real supported switch, so the
  widget can never offer a value that silently no-ops at play time. No
  changes needed to `ObjectiveRow`/`TriggerRow`/`MissionCsvLoader.cs`/the
  save format — widgets write the same canonical strings a correctly
  hand-typed value already would. 204 tests pass unchanged (pure UI
  change). **Found, but explicitly not fixed this session**: a real
  `RectMask2D` over-culling bug — the Objectives tab renders blank once
  it has ~7+ rows, a pre-existing latent bug from session 2 (not caused
  by this session's own widget change), root cause not isolated despite
  several ruled-out attempts (deferred-Destroy staleness, forced canvas
  updates, mask toggling, this project's own documented stuck-frame fix).
  Flagged via `spawn_task` (`task_545a0590`) for a dedicated follow-up
  session rather than left silently unnoticed. Live-verified the actual
  widget deliverable at row counts proven to render correctly: a real
  button click cycled a Faction field Player→Enemy, Save→Load
  round-tripped it intact, and Play correctly resolved a
  `PopulationThreshold` objective against real
  `Population.Current(FactionId.Enemy)` — not silently defaulting to
  Player — proving the widget-selected value flows through the real
  production path end to end. See `docs/SESSION_LOG.md`'s matching entry
  for full detail. Next: another Roadmap Section 5 item, or the newly-
  flagged RectMask2D follow-up.
- **Scenario Editor heavy path, session 5 (multiplayer LAN play of a
  custom scenario) closed (2026-09-03)** — picked up at the user's
  explicit request ("start item on multiplayer play of a custom
  scenario"), the last item deferred across sessions 1-4. Investigated
  first: `CivilizationSetup.BeginCustomScenarioMatch` was already
  network-safe as-is (every match-start entry point funnels through the
  same `BeginMatchCore`, already rewired project-wide to
  `NetworkMatch.LocalFaction`), so no changes were needed to
  `CivilizationSetup.cs`/`EntitySpawner.cs`/`ScenarioManager.cs`. Two
  real findings surfaced: **(1)** a genuine pre-existing bug —
  `AiController.cs` had zero `NetworkMatch` awareness, so the Enemy
  faction's AI kept fighting for control of units a real 2nd human LAN
  player already commanded. Fixed as a necessary prerequisite:
  `AiController.Start()` now disables itself when
  `myFaction == FactionId.Enemy && NetworkMatch.IsActive` (scoped to
  Enemy only — `Enemy2` untouched, matches the 2-human-only LAN scope;
  zero effect on local/offline matches). **(2)** a disclosed, not
  blocking, determinism caveat — `ScenarioManager`'s trigger closures use
  wall-clock `Time.time`, not `SimClock` ticks, so triggers could fire on
  a different tick per peer; this project's existing live-verified
  `NetworkDesyncMonitor`/`DesyncRecovery` resync safety net already
  covers this class of divergence. New `NetMessageEnvelope.scenarioJson`
  mirrors the existing `snapshotJson` convention, carried on `HostHello`;
  `LanMatchMenu` gained a scenario cycle row (`<`/`>`, "(None -
  Skirmish)" default) and routes to `BeginCustomScenarioMatch` instead of
  `BeginNetworkMatch` when a scenario was exchanged. 4 new EditMode tests
  (204 total, all pass). Live-verified via UnityMCP through the real
  production path (two real sockets within one process, the same
  disclosed single-machine limitation the original Phase 5 session
  flagged): a real `HostHello` carrying an in-memory scenario transmitted
  correctly over the wire, `CompleteHandshake` correctly invoked
  `BeginCustomScenarioMatch` with `ScenarioManager.ActiveScenario`
  matching exactly, and the real Enemy `AiController` GameObject showed
  `enabled=false` while `Enemy2`'s stayed untouched. Explicitly deferred:
  trigger-timing precision beyond the resync safety net, >2-human LAN,
  joiner-side scenario preview, per-scenario civ/map picker (session-1
  gap). See `docs/SESSION_LOG.md`'s matching entry for full detail. This
  closes the Scenario Editor heavy-path epic's last deferred item — next
  is the user's call on another Roadmap Section 5 item.
- **Scenario Editor heavy path, session 4 (richer palette icons) closed
  (2026-09-03)** — picked up at the user's explicit request ("start item
  on richer palette art for scenario editor"), the last cosmetic item
  session 1 flagged as deferred. This project already has real command-
  card icon assets for almost every placeable type (`BuildMenu.cs`'s own
  `build_*`/`train_*` icons under `Resources/UI/Icons/`), so this was a
  wiring task, not new asset sourcing. New
  `ScenarioEditorMenu.AddPaletteIcon` (adapted from `BuildMenu.
  AddCommandIcon`'s own "icon + inset label" shape, retuned for this
  file's smaller 260×24 palette rows) wires icons onto 12 of 13
  `EntitySpawner` types. **`TownCenter` is the one confirmed gap** — no
  `build_towncenter.png` exists anywhere in the project (TownCenter is
  normally auto-spawned, never player-built through any other menu) —
  stays text-only, the same disclosed fallback `BuildMenu.cs` itself
  already uses for Dock/LumberCamp/MiningCamp/Mill. Pure UI-wiring, no new
  branching logic, so no new test (matching `BuildMenu`'s own equivalent);
  full suite confirmed 200/200 unchanged. Live-verified via UnityMCP:
  screenshotted the real palette, all 12 icons render correctly with no
  text overlap, TownCenter renders cleanly text-only. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Next: the user's
  call among the remaining deferred items (per-kind input widgets,
  multiplayer play of a custom scenario), or another Roadmap Section 5
  item.
- **Scenario Editor heavy path, session 3 (saved-scenario browse list)
  closed (2026-09-03)** — picked up at the user's explicit request ("start
  a saved-scenario browse list on MissionSelectMenu"), the last item
  session 1/2 both flagged as deferred. New
  `Assets/Scripts/Core/SavedScenarioLibrary.cs` extracts the scenario-file
  I/O `ScenarioEditorMenu` had inline (`ScenarioFolder`/
  `ListSavedScenarioNames`/`Load`) into one shared source of truth, so both
  the editor and `MissionSelectMenu` read the exact same saved files —
  verified behavior-preserving by re-running the editor's own Save/Load
  flow live after the refactor. `MissionSelectMenu` gained a "Custom
  Scenarios" section (a `ScrollRect`-based row list, same pattern
  `SettingsMenu`'s Key Bindings list and session 2's own Objectives tab
  already establish, since the list is unbounded) with an explicit
  "No saved scenarios yet" empty state. New `ChooseCustomScenario(string)`
  mirrors `ChooseScenario`'s existing shutdown sequence, calling the same
  `CivilizationSetup.BeginCustomScenarioMatch` the editor's own Play
  button already uses. 3 new EditMode tests (200 total, all pass).
  Live-verified via UnityMCP through the real production path: saved 2
  scenarios through the real editor (one with a `SurviveSeconds`
  objective, one placements-only), screenshotted the real Mission Select
  panel confirming both appear correctly, then invoked the real
  `ChooseCustomScenario` and confirmed `ScenarioManager.ActiveScenario`'s
  title/objective matched the saved scenario exactly. Explicitly still out
  of scope: deleting/renaming a saved scenario from this list, row
  metadata (civ/map/placement count), thumbnail art. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Next: the user's
  call among the remaining deferred items (per-kind input widgets, richer
  palette art, multiplayer play of a custom scenario), or another Roadmap
  Section 5 item.
- **Scenario Editor heavy path, session 2 (Objective/Trigger authoring)
  closed (2026-09-03)** — picked up directly from the offer at the end of
  session 1 ("objective/trigger authoring for custom scenarios," the
  user's explicit next request). `MissionCsvLoader`'s existing CSV
  interpreter (`BuildObjectives`/`BuildTriggers`) was split into a typed,
  JsonUtility-serializable row layer (new `ObjectiveRow`/`TriggerRow` +
  `internal BuildObjectivesFromRows`/`BuildTriggersFromRows`), so the CSV
  (light) path and the in-game editor now share one interpreter for the
  same 5-objective/2-trigger-kind vocabulary — zero behavior change for
  existing CSV missions (full pre-existing suite passed unmodified).
  `CustomScenarioData` gained `objectives`/`triggers`/`victoryText`/
  `defeatText`; `ScenarioEditorMenu` gained a second **Objectives** tab
  (a `ScrollRect`-based row list, reusing `SettingsMenu`'s own Key Bindings
  scroll pattern) where an author cycles each row's Kind and fills generic
  Param1-5/Description fields, each with a live hint describing that
  kind's param meaning. `CivilizationSetup.BeginCustomScenarioMatch` now
  calls `ScenarioManager.Begin` with a real `ScenarioDefinition` built
  from the authored rows, but **only when `data.objectives.Count > 0`** —
  calling it unconditionally would have made every session-1 placements-
  only scenario resolve to an instant Victory, since
  `ScenarioManager.EvaluateOutcome()` treats an empty objective list as
  "already complete." 7 new EditMode tests (197 total, all pass). Live-
  verified via UnityMCP through the real production path: authored a
  `PopulationThreshold` objective + `GrantResourceAtTime` trigger through
  the actual editor UI state, Played it, confirmed
  `ScenarioManager.ActiveScenario` correctly wired and `MatchManager.
  Outcome` resolved to Victory via the scripted-mission branch (not
  elimination); separately confirmed the regression case the new gate is
  designed to prevent (a zero-objective scenario stays `Ongoing`,
  `ActiveScenario` stays null, exactly matching session 1's original
  behavior); separately proved Save→Close→re-Open→Load round-trips
  objective/trigger rows and victory text through the real file-based UI
  methods. Explicitly still deferred: per-kind bespoke input widgets, a
  saved-scenario browse list on `MissionSelectMenu`, richer palette art,
  multiplayer/LAN play of a custom scenario. See `docs/SESSION_LOG.md`'s
  matching entry for full detail. Next: the user's call among the
  remaining deferred items, or another Roadmap Section 5 item.
- **Scenario Editor heavy path, session 1 (Placements) closed (2026-09-03)**
  — picked up at the user's explicit request ("start the heavy scenario
  path") right after the light-path session closed. Confirmed 2 real
  forks via AskUserQuestion before coding: in-game runtime editor (real
  UGC, not a Unity EditorWindow), and placements first (starting units/
  buildings per faction — the part that genuinely didn't exist anywhere).
  New `EntitySpawner.cs` (pure extraction of `SaveManager`'s own type-
  string→factory dispatch, shared by both), `CustomScenarioData.cs`/
  `CustomScenarioContext.cs`, `CivilizationSetup.BeginCustomScenarioMatch`
  (deliberately skips `ScenarioManager.Begin` — v1 has no custom
  objectives, so standard Conquest just runs), and a new in-game
  `ScenarioEditorMenu.cs` (palette, click-place/drag-move/right-click-
  delete markers, Save/Load/Play) opened via a new "Create Scenario"
  button on `MissionSelectMenu` — needed zero other UI changes. **A real
  gap caught live, not left unnoticed**: the plan only accounted for 2 of
  3 gated default-spawn components (`TownCenterSpawner`/`AiController`) —
  a live population mismatch (6 instead of 2) after Playing a saved
  scenario traced to a 3rd, `UnitSpawner.cs`, unconditionally dropping 4
  default Workers; fixed with the same opt-out pattern. 12 new EditMode
  tests (190 total, all pass) plus two disclosed EditMode-only limitations
  (building factories and `SpawnUnit("Soldier")` both NRE/hard-error
  outside Play mode for pre-existing reasons unrelated to this session —
  covered by live UnityMCP verification instead). Live-verified the full
  editor→Save→Load→Play loop through the real UI end to end. See
  `docs/SESSION_LOG.md`'s matching entry for full detail. Explicitly
  deferred: objective/trigger authoring for custom scenarios, a saved-
  scenario browse list on `MissionSelectMenu`, richer palette art,
  multiplayer/LAN play of a custom scenario — any is a reasonable next
  session on this same epic.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 6 (Scenario Editor) light path
  closed (2026-09-03)** — picked up right after item 5 per the user's
  "start item 6". Reading `ScenarioDefinition.cs` directly (not trusting
  the plan's premise) found a real architecture correction:
  `BuildObjectives`/`BuildTriggers` are `Func<>` delegates, which Unity
  can't serialize into a ScriptableObject — so unlike Tech/Unit/Civ data,
  missions can't be Editor-time-baked; they're parsed into real closures
  **at runtime** instead. New `Assets/Scripts/Match/MissionCsvLoader.cs`:
  a small fixed vocabulary (5 objective kinds, 2 trigger kinds — sized off
  the 3 real hand-coded missions plus 2 shapes the Tutorial roadmap item
  already wants), reading 3 new CSVs as `Resources`-loaded `TextAsset`s.
  `ScenarioRegistry.All` merges CSV-loaded missions with the 3 existing
  hand-coded ones; `MissionSelectMenu.cs` needed **zero changes** (already
  iterates that list). One new real sample mission ("The Muster") proves
  the pipeline through the actual UI. 8 new EditMode tests (180 total, all
  pass) plus live UnityMCP verification of every kind, including the one
  (`DestroyScriptedTarget`) that can't run in EditMode at all
  (`BarracksFactory.Place` NREs outside Play mode — a pre-existing
  factory limitation, not new). **Hit a real environment problem
  mid-session**: new code silently stopped compiling into the assembly
  with zero errors from `read_console`; the user restarted the Unity
  Editor, which surfaced (via `~/Library/Logs/Unity/Editor.log` directly,
  not the MCP console bridge) 2 real compile errors in the new code
  (a missing `using`, then the same "two `DamageType` enums" class of bug
  `WildBoar.cs` hit once before) — both fixed. **New lesson for future
  sessions: if `read_console` shows zero errors but new code isn't taking
  effect, check the real Editor.log file directly before assuming a
  tooling problem** — the console bridge can miss real compile errors.
  See `docs/SESSION_LOG.md`'s matching entry for full detail. This closes
  the last item in the Partial-Elements Fix Plan's own recommended order
  — items 1-6 are all done; Fish Trap (item 5) and the heavy Scenario
  Editor path (item 6) remain explicitly deferred, tracked in
  `docs/Roadmap.md`.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 5 (Renewable Resource — Farm
  depletion) Farm half closed (2026-09-03)** — picked up right after item
  4 per the user's "start item 5". Step 1 (verify, don't assume) found a
  third case the plan doc's own Step 2a/2b binary didn't anticipate: a
  staffed Farm produced Food forever, no cap, no depletion at all —
  categorically different from AoE's real finite/depleting/reseedable
  Farm. Put this finding to the user directly (AskUserQuestion) rather
  than picking a fix silently; confirmed: retrofit real depletion + reseed
  (the larger option, bigger than the item's own "Small" estimate).
  `Farm.cs` now has a real 175-Food capacity (AoE II's own Dark-Age
  value) that depletes as it's worked, plus a Wood-costed reseed (60 Wood
  full cost, matching the Farm's own build cost) using the same
  `ConstructionSite.SpeedMultiplier` diminishing-returns curve
  `Repairable` already reuses. **No `SelectionManager` change needed**:
  `FarmWorker` autonomously switches between harvesting and reseeding
  based on the Farm's own live depleted state, so the existing right-click
  order just does the right thing on its own. 8 new EditMode tests (172
  total, all pass), live-verified via UnityMCP through the real production
  `Tick`/`Update` path — including an unplanned but convincing proof that
  the real system cycled through a full harvest→deplete→reseed→harvest
  loop entirely on its own between verification calls, with nothing
  forcing it. See `docs/SESSION_LOG.md`'s matching entry for the exact
  numbers. Fish Trap stays deferred/asset-blocked, untouched this session.
  Next per the plan doc's own recommended order: item 6 (Scenario Editor —
  recommend the lightweight CSV-authoring path), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 4 (Diplomacy — Tribute) closed
  (2026-09-03)** — picked up right after item 3 per the user's "start item
  4". That item's own first instruction (check the live UI before writing
  a new panel) found the stance UI already fully existed
  (`DiplomacyMenu.cs`, F11, real War/Allied toggle per faction) — only
  Tribute itself was missing. Asked the user directly (AskUserQuestion)
  whether Tribute should be alliance-gated or open to any faction (real
  AoE II's actual rule); confirmed: any faction. New
  `Assets/Scripts/Core/Tribute.cs` (`Tribute.Send`, 20% tax, deliberately
  not gated by `DiplomacyRegistry`), 4 new tribute icon buttons per row in
  `DiplomacyMenu.cs` (flat 50 per click, affordability-gated like
  `BuildMenu`'s Market buttons). 5 new EditMode tests (164 total, all
  pass), live-verified via UnityMCP through the real button `onClick` (not
  just the isolated method) — Player Wood 200→150, Enemy Wood +40 (50 ×
  0.8 tax) — see `docs/SESSION_LOG.md`'s matching entry. Next per the plan
  doc's own recommended order: item 5 (Renewable resource / Farms — verify
  first, then fix if needed), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 3 (Area of Effect / Trample)
  closed (2026-09-03)** — picked up right after item 2 per the user's
  "start item 3". The plan doc flagged a real design decision (reuse
  Siege's full-damage splash as-is, or reduce trample's secondary damage)
  — asked the user directly rather than picking silently; confirmed:
  reduced secondary damage. `MeleeAttacker.SetSplashRadius` gained an
  optional `damageMultiplier` parameter (default 1f — Siege's existing
  single-arg call stays byte-for-byte unchanged, verified by its own test
  suite passing unmodified). `CavalryFactory` wires
  `SetSplashRadius(1.25f, 0.35f)` — a radius below formation spacing (only
  catches units clumped tight around the impact point, not a full
  adjacent rank) at 35% secondary damage (stays minor even stacked on
  Cavalry's existing 1.5x hard-counter bonus vs. Infantry). No new VFX
  needed — the existing per-hit particle burst already fires for trample
  hits, confirmed live. 4 new EditMode tests (159 total, all pass),
  live-verified via UnityMCP through the real `CavalryFactory`/
  `SoldierFactory`/`MeleeAttacker.Tick` production path (real 30-HP
  Soldiers, real armor/CSV stats) — see `docs/SESSION_LOG.md`'s matching
  entry for the exact numbers. Next per the plan doc's own recommended
  order: item 4 (Diplomacy — tribute), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 2 (Victory Conditions) closed
  (2026-09-03)** — picked up right after item 1 per the user's "start item 2".
  That item's own premise was half wrong once checked against the actual
  repo rather than trusted: **Conquest was already fully implemented**
  (`MatchManager.Evaluate`, pre-existing) and the plan's "new art" Victory/
  Defeat splash already existed too (`GameOverScreen.cs`) — neither needed
  building. **Time Limit was the only real gap**, now closed:
  `GameSettings.TimeLimitMinutes` (Off/15/30/45/60, cycled via a new
  Settings row), `MatchManager.EvaluateSkirmishOutcome`/
  `ResolveTimeLimitOutcome` (population tiebreaker, ally-aware, `Draw` on a
  tie — a new 4th `MatchOutcome` value), `GameOverScreen` extended for
  Draw. 9 new EditMode tests (155 total, all pass), live-verified via
  UnityMCP through the real `Update()`/`Time.unscaledTime` path (not just
  the isolated functions) — see `docs/SESSION_LOG.md`'s matching entry for
  the exact repro. **Also fixed a real regression found while touching the
  same file**: `SettingsMenu`'s Key Bindings list had silently overflowed
  its panel background since item 1's own session grew it from 12 to 34
  rows with no layout resize — fixed with a proper `ScrollRect`-based
  scrollable list, screenshot-verified at both scroll extremes. Next per
  the plan doc's own recommended order: item 3 (Area of Effect / Trample
  damage), not started.
- **`docs/PARTIAL_ELEMENTS_FIX_PLAN.md` item 1 (Hotkeys) closed (2026-09-03)**
  — picked up per the user's instruction to read that plan and start item 1.
  The real gap was bigger than "missing keys": only 3 of ~18 `BuildMenu`
  actions had a hotkey at all, and those 3 (`Barracks`/`Dock`/`TownCenter`'s
  own `Update()`-based `trainKey` checks) had a genuine, previously
  unflagged bug — gated only on faction ownership, not on whether that
  specific instance was the *selected* building, so pressing e.g. G trained
  a Worker at every idle Player TownCenter at once. Fixed by centralizing
  hotkey dispatch into `BuildMenu.Update()` (which already tracks
  `SelectionManager.SelectedBuilding`), added 15 new hotkeys covering the
  rest of Barracks/TownCenter/Dock training and research plus Ungarrison,
  registered every binding (including 4 already-functional-but-unlisted
  placement keys and Save/Load/Diplomacy, the latter of which turned out to
  not even be routed through `GameSettings` at all) in `SettingsMenu.Actions`,
  and built the optional F1 hotkey-reference overlay panel (new
  `Assets/Scripts/UI/HotkeyOverlay.cs`) — its own live-verification pass
  caught and fixed a real column-overlap layout bug before calling it done.
  User-confirmed scope via AskUserQuestion/Plan Mode: fix the bug now (not
  deferred), Ungarrison gets a hotkey but the 6 Market buy/sell buttons stay
  click-only (no real AoE-like hotkeys specific trade amounts), include the
  F1 overlay. All 146 EditMode tests pass unmodified; live-verified via
  UnityMCP against the real production path (`CivilizationSetup.BeginMatch`,
  same technique prior sessions established) — the concrete regression repro
  for the bug (2 real Player TownCenters, select one, invoke the hotkey's
  exact handler method, confirm only the selected one trains) is in
  `docs/SESSION_LOG.md`'s matching entry, along with the full per-context
  hotkey table. Next per the plan doc's own recommended order: item 2
  (Victory conditions — Conquest + Time Limit), not started.
- **"Everything else" items scoped (2026-09-03), not implemented** — at the
  user's explicit request ("plan out the items in everything else"), the
  5 lower-priority Roadmap Section 1 stub bullets (Music, Tutorial,
  Profiling, Store/marketing assets, README drift) were each expanded into a
  concrete scope grounded in a fresh read of the actual repo (`SfxPlayer.cs`,
  `MissionObjective.cs`/`MissionTrigger.cs`/`MissionSelectMenu.cs`,
  `README.md` itself), not assumed from prior session-log claims. Key
  findings: 4 of the 5 are genuinely doable now with **no user blocker** —
  Music can self-serve CC0 tracks from Kenney.nl the same way the existing
  SFX pass did (internet access already confirmed working); Tutorial rides
  the already-proven mission-objective system, pure content authoring, no
  new system needed; Profiling has a ready tool
  (`mcp__UnityMCP__manage_profiler`) and just needs someone to actually run
  it against a realistic large-match scenario; README drift is pure
  doc-writing (confirmed the README is badly stale — still describes the
  original single-map/3-civ/no-naval prototype scope and stops its milestone
  list at 26, missing everything since including 5-civ art, naval, LAN
  multiplayer, diplomacy, and the full worker-mechanics-audit feature set).
  **Only Store/marketing assets is a genuine blocker**, and not on
  sourcing/code — it needs an explicit user decision on whether public
  release is even a goal before any scoping can proceed further.
  Recommended order: README drift + Music first (cheapest, zero blocker),
  Tutorial next, Profiling after that, Store assets last pending the
  release-intent conversation. Full per-item detail in Roadmap Section 1's
  "Lower priority" subsection and Section 5 item 6. Docs-only session, no
  code/asset changes, no tests affected.
- **Maurya Tower re-sourced and wired (2026-09-03, Section 1/5 item 7)** —
  closes the last civ-specific-building gap: **45/45 civ-specific buildings now
  complete** across all 5 civs. User supplied a fresh delivery at
  `/Volumes/US/all civ buildings/Maurya/` (folder `Meshy_AI_Ivory_Sentinel_Tower_...`,
  identified unambiguously by its internal Meshy filename). Packed the raw
  separate metallic/roughness textures into a `_metallicSmoothness.png` via a
  scratchpad Python/Pillow script (same approach as the 2026-09-02 Rajput
  session), then wired via the existing `MeshyBuildingImporter`/
  `BuildingMeshDecimator` pipeline unchanged, no code changes needed.
  **Rotation determined empirically, not guessed**: tested all 6 cardinal-axis
  candidates against the raw FBX's own world bounds first (per
  `feedback_tower_rotation_correction.md`); this asset ties Y-tallest on
  `X+90`/`X-90` (its raw up-axis is local Z, unlike prior Towers' local Y) —
  screenshotted both tying candidates against the folder's own Watchtower
  concept art before committing: `X+90` was upside-down (flared cap at the
  visual bottom), `X-90` matched the concept art exactly (stepped lion-guarded
  base, pillared shaft, crenellated parapet, domed cap), also confirmed via a
  true top-down shot (compact square footprint, not the elongated one the wrong
  candidate gives). Solved algebraically for the prefab's baked
  `modelRotationCorrection` given the fixed civ-blind
  `ImportRotationCorrections["Tower"]` parent stomp
  (`Quaternion.Inverse(parentStomp) * Euler(-90,0,0)` = `Euler(0,-90,90)`), then
  verified via the real `BuildingModelFactory.Spawn` path post-import (world
  height matched the computed target to the last decimal). Scale: worker height
  re-measured fresh (1.960884, vs. the previously-documented 1.902692 baseline)
  and the established Maurya ratio hierarchy's Tower value (8.00) scaled
  proportionally to 8.245 rather than reused blind. Decimated 1,979,816 →
  500,000 tris via `BuildingMeshDecimator`, matching every other building's
  already-validated target. All 146 EditMode tests pass unmodified — pure
  asset-pipeline work, no test changes needed. See `docs/SESSION_LOG.md`'s
  matching entry for full detail.
- **Rajput TownCenter/Barracks re-sourced, Rajput Tower rotation fixed
  (2026-09-02/03, Section 1/5 item 7)** — closes the 2 findings the mesh-
  decimation session flagged but didn't fix (see this file's own note below).
  User supplied correct source deliveries for both buildings in an external
  folder; wired via the existing `MeshyBuildingImporter`/`BuildingMeshDecimator`
  pipeline unchanged, no code changes needed. Both were lying on their back at
  import (Meshy's usual export convention) — orientation confirmed via 3/4-view
  and top-down screenshots against each building's own concept art before
  committing to `Quaternion.Euler(-90,0,0)` for both, not assumed. **TownCenter
  needed a second pass**: the folder's first delivery turned out to be the
  wrong file (the user's own upload mistake), caught and corrected mid-session
  before this was logged as done — re-imported/re-verified from scratch once
  the correct delivery was in place (final: Barracks height 4.34, TownCenter
  height 11.28, matching the established Rajput ratio hierarchy). Both
  decimated to ~500,000 tris (from ~1.9-2.0M raw), screenshot-confirmed clean.
  **Also found and fixed a real, pre-existing adjacent bug, flagged live by the
  user from the running scene**: Rajput's Tower (untouched by this session's
  own changes) was spawning upside-down — its 2026-08-31-session-baked child
  rotation correction (`Euler(0,90,0)`) turned out to be the wrong one of the
  Y+90/Y-90 tying-bounds pair `feedback_tower_rotation_correction.md` already
  flags as a real trap. Root-caused (tested the raw FBX standalone against the
  Watchtower concept art, then solved algebraically for the required child
  rotation given `BuildingModelFactory`'s fixed civ-blind parent stomp) and
  fixed directly on `Tower.prefab`, re-verified via the real
  `BuildingModelFactory.Spawn` path. All 146 EditMode tests pass unmodified
  throughout. Rajput is now genuinely 9/9 (model + correct orientation);
  44/45 civ-specific buildings complete overall (Maurya Tower is the one
  remaining gap, a separate pre-existing 0-byte-source issue). See
  `docs/SESSION_LOG.md`'s matching entry for full detail.
- **Building mesh decimation pass closed (2026-09-02, Section 1/5 item 17)**
  — `UnityMeshSimplifier` package + new `Assets/Editor/BuildingMeshDecimator.cs`
  decimated 43/45 civ-specific buildings (Rajput TownCenter/Maurya Tower's
  pre-existing 0-byte-source gaps correctly skipped) from ~1.7-2.0M
  un-decimated triangles down to ~500,000 each. **The real finding**: the
  spec's literal 8,000-20,000 tri target, and even the 30,000-60,000
  fallback, both proved unreachable on this raw un-retopologized Meshy
  geometry without either visible carved-relief artifacts (proven via
  screenshot on Chola TownCenter at the tighter target) or computationally
  impractical simplifier settings — one tuning attempt (raising
  `VertexLinkDistance`) pegged the Editor at 99% CPU for 25+ minutes with no
  completion and no way to cancel a synchronous call, resolved by killing
  and relaunching Unity at the user's explicit instruction. 500,000 tris/
  building is the real, evidence-backed, screenshot-verified-clean target
  (~3.65x reduction, a full base drops from ~17M+ to ~4.5M triangles). Also
  fixed a genuine Unity `AssetDatabase` caching bug hit along the way
  (`Resources.Load`/`AssetDatabase.LoadAssetAtPath` served a stale prefab
  graph with a null mesh after a re-run, even though the saved prefab was
  correct on disk — fixed via an explicit `AssetDatabase.ImportAsset(...,
  ForceUpdate)` after each save). New `BuildingPolycountTests.cs` regression
  test (146 EditMode tests total, up from 145), all pass. **Two findings
  flagged, not fixed (out of scope for this item)**: spawning Rajput
  TownCenter's shared-fallback model produces a `MeshFilter` with a null
  `sharedMesh` (pre-existing, unrelated to mesh decimation); and — raised
  mid-session by the user with a reference image — Rajput Barracks' actual
  sourced model (a small boxy shape, confirmed already present in the
  original un-decimated FBX, so not caused by this session) doesn't match
  the grand multi-turret courtyard-fort concept art the user expects —
  looks like a wrong/mismatched asset was sourced/identified in an earlier
  session, a real asset-sourcing gap for a future session, not a rotation
  or mesh-processing bug (a separate "tilted sideways" concern raised in the
  same exchange was checked directly and ruled out — every transform in the
  hierarchy is identity, confirmed upright via a true ground-level
  front-elevation shot; the original angled screenshot's steep camera angle
  was just foreshortening the roofline, the same parallax illusion this
  project's history has hit before). Also found 6 additional
  `Maurya/_Source/*.mat` files plus the already-documented Cow/Palm2 fix
  were pending-but-unsaved from an earlier session and got flushed to disk
  by this session's own `AssetDatabase.SaveAssets()` calls — left unstaged,
  not bundled into this session's commit (not this session's work to claim
  or decide about). See `docs/SESSION_LOG.md`'s matching entry for full
  detail.
- **Crusader Knight body-swap sourcing spec written (2026-09-02), not started**
  — user picked up this item after the mesh-decimation one was scoped by a
  concurrent session; asked to scope only, not implement (nothing to
  implement yet — it's still blocked on new source model files). Spec
  written into Roadmap Section 1's matching item, derived directly from the
  2026-08-28 rig-compatibility verification's own findings so a replacement
  doesn't repeat the same problems blind: FBX preferred (glTF works too, per
  that session's proof `AvatarBuilder.BuildHumanAvatar` builds a valid Avatar
  from a hand-authored `HumanDescription` with no Blender step), any standard
  Humanoid biped rig, modeled in meters near the scene's ~1.9-unit worker
  height (the deleted models' `(2.54,2.54,2.54)` baked Hips scale was the
  actual root cause of the ~247x `humanScale` anomaly — cleaner sourcing
  avoids that fix entirely), no embedded animation needed, and either no
  sculpted hand-held weapons (reuse `WeaponAttachment.AttachToBone`, the
  precedent already used for the 3 unique units) or weapon meshes that are
  separable/re-parentable to a hand bone rather than static scene-root props.
  Also flagged a normal real-time polycount range, tying back to the same
  day's building-polycount audit finding. Docs-only session, no code/asset
  changes. Still needs the user to actually source a replacement file before
  any wiring work can start.
- **Building mesh decimation pass scoped (2026-09-02), not started** — the
  visual-audit session's headline finding (every civ-specific building is
  ~1.7-2.0M un-decimated triangles) needs its own dedicated session per the
  user's explicit request rather than being folded into the audit itself.
  Full plan in Roadmap Section 1/5 item 17: no Blender available in this
  environment (checked directly), no built-in Unity mesh-simplification API
  either (checked via reflection) — plan is to add the
  `UnityMeshSimplifier` package (MIT, pure C#, git-fetchable — internet
  access confirmed working) and a new `Assets/Editor/BuildingMeshDecimator.cs`,
  proof-of-concept on Chola TownCenter first to pick a real target ratio
  (the spec's 8,000-20,000 tri target may be too aggressive for the ornate
  carved-relief buildings — a judgment call, not a fixed number), then batch
  the remaining 44 with a screenshot-verified sample per civ, plus a new
  EditMode regression test asserting building polycount stays under a
  ceiling going forward (zero such coverage existed before this finding).
- **Civ-by-civ visual quality audit (2026-09-02)**, user-requested, against
  Roadmap Section 4.1's AoE IV visual standard. Live-measured (not estimated)
  via UnityMCP: every civ-specific building's real triangle count/texture
  resolution/shader, plus side-by-side style comparisons. **Headline finding:
  every civ-specific building is ~1.7-2.0 million un-decimated triangles**
  (100-250x over the 8,000-20,000 tri spec) — texture resolution and shader
  workflow both pass cleanly, polycount is the one real failure, and it was
  never caught by any prior per-civ import session (those checked rotation/
  scale, not polycount). Style differentiation mostly works (Rajput/Maratha
  read as clearly distinct traditions) but Chola/Vijayanagara's TownCenters
  read as the same architectural family, and Maurya's gilded-dome TownCenter
  is visually striking but reads more Mughal/colonial than authentically
  Mauryan. Confirmed live: the shared Human Character Dummy soldier body is
  the single most visually obvious gap (5 civs = identical mannequin, tint
  only). Full findings in Roadmap Section 4.2 (rewritten, replacing stale
  pre-civ-art text) and `docs/SESSION_LOG.md`. Pure investigation, no code
  changes — a mesh-decimation pass for the building polycount problem is a
  real candidate for a future session (flagged, not started).
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
- Currently on: **Scenario Editor heavy path, session 6 (per-kind bespoke
  input widgets) closed (2026-09-03)** — see this file's own bullet above
  for full detail. **This closes the entire Scenario Editor heavy-path
  epic** — every item from session 1's original deferred list is now
  done. A real, separate `RectMask2D` rendering bug was found (not fixed)
  during this session's own verification and flagged via `spawn_task` for
  a dedicated follow-up (`task_545a0590`) — see this file's own bullet
  above. Before that: **Scenario Editor heavy path, session 5
  (multiplayer LAN play of a custom scenario) closed (2026-09-03)** — see
  this file's own bullet above for full detail; also fixed a real
  adjacent pre-existing bug found live (Enemy `AiController` running
  during real 2-human LAN matches). Before that: **Scenario Editor heavy
  path, session 4 (richer palette
  icons) closed (2026-09-03)** — see this file's own bullet above for
  full detail. Before that: **Scenario Editor heavy path, session 3
  (saved-scenario browse list) closed (2026-09-03)** — see this
  file's own bullet above for full detail. Before that: **Scenario Editor
  heavy path, session 2 (Objective/Trigger authoring) closed
  (2026-09-03)** — see this file's own bullet above for full detail.
  Before that: **Scenario Editor heavy path, session 1 (Placements)
  closed (2026-09-03)** — see this file's own bullet above for full
  detail. Before that: **item 6 (Scenario Editor) light path
  closed (2026-09-03)** — see this file's own bullet above for full
  detail. That closed the plan doc's entire recommended order (items 1-6
  all done); remaining deferred sub-items (Fish Trap) are tracked in
  `docs/Roadmap.md` — see this file's own "Lower priority" Roadmap notes
  and the "Everything else" items scoped 2026-09-03. Before that:
  **item 5 (Renewable Resource — Farm depletion) Farm half closed
  (2026-09-03)** — see this file's own bullet above for full detail.
  Before that: **item 4 (Diplomacy — Tribute) closed (2026-09-03)** — see
  this file's own bullet above for full detail. Before that: **item 3
  (Area of Effect / Trample) closed (2026-09-03)** — see this file's own
  bullet above for full detail. Before that: **item 2 (Victory
  Conditions) closed (2026-09-03)** — see this file's own bullet above
  for full detail. Before that: **item 1 (Hotkeys) closed
  (2026-09-03)** — see this file's own bullet above for
  full detail. Before that:
  **"Everything else" items scoped (2026-09-03)** — Music,
  Tutorial, Profiling, Store/marketing assets, and README drift each given a
  concrete scope in Roadmap Section 1; see this file's own bullet above for
  full detail. Nothing implemented yet there — a future session should pick
  README drift or Music (both zero-blocker, cheapest) unless the user says
  otherwise. Before that: **Maurya Tower re-sourced and wired (2026-09-03) —
  closes the
  last civ-specific-building gap, 45/45 complete**, see this file's own bullet
  above for full detail. Before that: Rajput TownCenter/Barracks re-sourced and
  Rajput Tower's rotation bug fixed (2026-09-02/03) — both findings flagged by
  the mesh-decimation session, closed. The mesh decimation pass itself (Section 1/5 item 17) closed
  2026-09-02 — see Roadmap Section 1/5 for full detail (44/45 buildings
  decimated to ~500,000 tris each, real target-ratio finding, new regression
  test). Otherwise: Section 5 items 1-9 and
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
  longer an open item. **Item 7 (civ-specific building models) is now
  45/45, fully complete** — all 5 civs are 9/9 (Rajput's TownCenter and
  Barracks re-sourced 2026-09-02/03; Maurya's Tower re-sourced 2026-09-03, see
  this file's own bullet above for both).
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
  asset need, spec written 2026-09-01, needs the user to source; item 7's
  civ-specific building models are now fully done, 45/45), other
  "everything else" items (music, tutorial, performance profiling, store
  assets, multiplayer determinism gaps, README drift), or continued balance
  work — user's call.
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

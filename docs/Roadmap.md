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
  See `docs/SESSION_LOG.md` for the full test methodology.

### Medium priority — real content/design work, not bug fixes

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
  as of 2026-08-28 (see the full entry under Section 4.3). Two content gaps remain,
  tracked as their own Section 4.3 items: no build-placement cursor asset, and the
  Maurya crest is off-palette.
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
  an in-Editor `Texture2D.GetPixels` attempt crashed the Unity process), per-building
  scale correction (1.18×-3.16×, TownCenter 93×), and a Tower orientation fix
  (`BuildingModelFactory`'s shared `ImportRotationCorrections["Tower"]` doesn't
  account for civ, so Chola's Tower needed a counter-rotation baked into the wrapper
  prefab). Live-verified spawning through the real factory path, 2 new EditMode tests
  added. Vijayanagara/Rajput/Maurya/Maratha (36 models) remain unstarted — see
  `docs/SESSION_LOG.md` for full methodology, the crash root-cause, and the
  AABB-can't-detect-upside-down lesson for future rotation fixes.

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
- [ ] **Build-placement cursor asset missing** — the UI skin cursor set specced 5
  states (default/attack-move/invalid/gather/build-placement); only 4 were ever
  generated. `HoverTooltip.Update()` (`Assets/Scripts/UI/HoverTooltip.cs`) already
  checks `BuildingPlacer.IsPlacing` and is wired to switch cursors for the other 3
  states — it deliberately falls through to the default cursor while placing a
  building, on purpose, not a bug. Once a `build_placement` cursor image exists
  (spec: `docs/UI_ART_BRIEF.md`), drop it at `Assets/Resources/UI/Cursors/
  build_placement.png`, set its import Texture Type to Cursor (max size 64, matching
  the other 4), and add one more branch to `HoverTooltip.Update()`.
- [ ] **Maurya crest is off-palette** — `Assets/Resources/UI/Menu/crest_maurya.png`
  renders in Rajput's blue/gold instead of Maurya's spec'd warm gray/stone
  (`#807866`, see `docs/UI_ART_BRIEF.md`). Subject (Ashokan lion pillar capital) is
  correct, only the color family is wrong — the two civs' crests currently read as
  confusingly similar on the CivPicker screen. Needs a regenerated/recolored image at
  the same path; no code change required once the art is fixed.
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
6. Everything else (music, tutorial, performance profiling, UI skin, store assets) is
   real but lower-urgency — sequence after the above based on what you want to
   prioritize next, not by default order.
7. ~~**Per-civ architectural differentiation, architecture groundwork** — audited
   `BuildingModelFactory` and wired it (plus all 9 building factories) to probe a
   civ-keyed model path before falling back to today's shared model, so civ-specific
   building models can land incrementally, one civ/building at a time, with no code
   changes needed per asset.~~ **Code groundwork done.** Actual civ-specific models
   are a separate content project (see Section 4.3) — not started, and not this
   session's scope.
8. ~~**Visual closure for the 4 Maurya/Maratha unique units** — real Meshy-sourced
   models, rigged via Blender command-line scripting (3 onto the existing shared
   human rig, the War Elephant onto a real third-party elephant skeleton+animation
   set) and wired into their factories.~~ **Done** (2026-08-27). See Section 1's
   matching item and `docs/SESSION_LOG.md` for full methodology and the one known
   first-pass limitation (elephant Die clip).

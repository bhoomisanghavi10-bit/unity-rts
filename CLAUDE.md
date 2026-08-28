# Project: Kingdoms of Bharat

Historical RTS across Indian kingdoms/empires, targeting AoE II/IV-level systemic
depth. Full roadmap: `/docs/ROADMAP.md` (Master Roadmap v3 — sections: 1. Open items
punch list, 2. Architectural notes to preserve, 3. Process note, 4. Art direction &
asset requirements, 5. Priority order).

## Current status (keep current — update every session)
- Working from Roadmap Section 5's priority order.
- **Note**: this update landed alongside a concurrent session doing a Naval balance
  pass (see immediately below) — flagged per the single-session-discipline gotcha;
  this session's own changes are scoped entirely to `Assets/Resources/UI/` art files
  and didn't touch combat/balance code.
- Currently on: nothing started yet for the next session. Section 5 items 1-5, 7-8
  are all done. Remaining real options: civ-specific building models (Section 4.3 has
  the priority order/spec, arranged by the user separately), **UI skin's actual code
  wiring** (Tiers 1-3 art now delivered and alpha-fixed/renamed at
  `Assets/Resources/UI/` — see immediately below — but the `UIStyleTheme.asset`
  creation, Sprite import settings/9-slice borders, and the `BuildMenu`/
  `ResourceHUD`/`SelectedUnitPanel`/`CivPicker`/cursor code to actually display any of
  it are still unstarted), other "everything else" items (music, tutorial, performance
  profiling, store assets, Crusader Knight rig verification, multiplayer determinism
  gaps, README drift), two adjacent findings flagged in the concurrent Naval session
  (Naval factories missing `ClassArmorBonus`/`ClassDamageBonus`; `BoatAttacker`'s 1.5s
  vs `MeleeAttacker`'s 1.0s attack interval), or continued balance work (training
  cost-vs-power ratios, further sustained playtesting) — user's call.
- Last completed (this session): **UI art delivered — alpha-fix + rename pass**
  (Roadmap Section 4.3 "UI skin"). The user dropped Tier 1-3 art (per the asset spec
  from the earlier audit session) into `Assets/Resources/UI/` as jpg/png from Canva/
  Gemini. Audited it against the spec (essentially complete — 4/5 cursors, all action/
  resource icons, both button-background sets, HP bar, all panel frames, all civ
  crests; missing the 5th cursor and the Maurya crest is off-palette blue instead of
  gray/stone). Found every file had a fully **opaque baked-in background instead of
  real alpha** (checkerboard-fake or flat-cream, confirmed by direct pixel sampling,
  not just preview) — wrote `Tools/ui_art_alpha_key.py` (border-flood-fill alpha key
  with corner-color sampling + dilation-bridging) to fix it, verified every output by
  compositing onto magenta (caught that `panel_selected_unit.png` has no real
  background at all and would have been destroyed by the same treatment — copied
  through unmodified instead). Renamed all ~40 files from auto-generated prompt-text
  filenames to stable short names; raw pre-fix originals preserved at
  `UI_RawOriginals_backup/` (repo root, outside `Assets/`). One file
  (`resource_stone.png`) is only ~85% cleaned after tuning attempts — flagged for a
  manual touch-up. Deliberately did **not** create the theme asset or write any of the
  display-wiring code this session (scoped down per explicit user choice) — that's
  the next UI-skin session's job. See `docs/SESSION_LOG.md`.
- Also landed around the same time (concurrent session, Roadmap Section 1): a
  **Naval balance pass** — audited the 4 previously-unaudited Naval matchups via 4
  live forced-fights (War Galley vs Archer/Cavalry/Siege, plus a mirror match), no
  `CombatBonus` changes needed, all results either confirmed the existing 0.5x
  Naval→Archer fix or were judged working-as-designed. Two adjacent (unfixed, flagged)
  findings: Naval factories never call `ClassArmorBonus`/`ClassDamageBonus`, and
  `BoatAttacker`'s 1.5s attack interval vs `MeleeAttacker`'s 1.0s is a real structural
  asymmetry. Full detail in `docs/SESSION_LOG.md`.

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

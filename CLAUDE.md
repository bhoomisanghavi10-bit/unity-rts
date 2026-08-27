# Project: Kingdoms of Bharat

Historical RTS across Indian kingdoms/empires, targeting AoE II/IV-level systemic
depth. Full roadmap: `/docs/ROADMAP.md` (Master Roadmap v3 — sections: 1. Open items
punch list, 2. Architectural notes to preserve, 3. Process note, 4. Art direction &
asset requirements, 5. Priority order).

## Current status (keep current — update every session)
- Working from Roadmap Section 5's priority order.
- **Note**: this update landed alongside a concurrent Claude Code session working
  the same repo (item 7, per-civ architectural differentiation groundwork) — flagged
  here per the single-session-discipline gotcha below; this session's own changes are
  scoped to the 4 unique-unit models only (see immediately below) and didn't touch
  `BuildingModelFactory`/civ-building work.
- Currently on: nothing started yet for the next session — Section 5 item 3's visual
  closure (this session) and item 7 (per-civ architectural differentiation, code
  groundwork, concurrent session) are both done. Remaining: civ-specific building
  models (Section 4.3 has the priority order/spec, arranged by the user separately),
  "everything else" (music, tutorial, performance profiling, UI skin, store assets),
  or continued balance work (training cost-vs-power ratios, further sustained
  playtesting) — user's call.
- Last completed (this session): **Section 5 item 3's visual closure** — the 4
  Maurya/Maratha unique units (backend-complete since an earlier session) now have
  real rigged models instead of the shared Human Dummy fallback. The user drove this
  via Blender command-line Python scripting (`blender --background --python`), not
  manual Editor steps: Pillar Edict Scholar/Mavla Raider/Durg Garrison got their
  Meshy-sourced meshes bound onto the existing shared Human Character Dummy rig
  (reuses its Idle/Walk/Attack clips directly, no new animation needed); the War
  Elephant got its Meshy-sourced mesh bound onto a real third-party elephant
  skeleton+animations (CC-BY, see `Assets/Resources/UniqueUnits/CREDITS.md`) instead
  of the originally-planned "extend the wild boar's skeleton" approach (dropped once
  a real elephant rig became available). `HumanModelFactory.Spawn` gained two
  backward-compatible optional parameters (`prefabPathOverride`,
  `applyPaletteMaterial`) to let the 3 humanoid factories point at their own model
  instead of the generic dummy; a new `ElephantAnimationDriver`/`ElephantAnimationSet`
  (mirroring `BoarAnimationDriver`'s Generic-rig Playables pattern) drives the
  elephant. All 4 live-tested spawning correctly in Play mode (valid Animator/Avatar,
  correct textures, no console errors, real NavMeshAgent movement confirmed). One
  known first-pass limitation, not fixed this session: the elephant's Die clip's
  automatic-weight bind strains visibly in the more extreme back half of that
  animation. See `docs/SESSION_LOG.md` for the full rigging methodology, including
  several real Unity/Blender interop bugs found and fixed along the way (a spurious
  object-scale=0.01 animation-export artifact, a missing intermediate bone-hierarchy
  node the Humanoid Avatar validator required, etc).

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

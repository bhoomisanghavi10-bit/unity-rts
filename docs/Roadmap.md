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
- [ ] **WaterMover has no obstacle avoidance** — straight-line movement only. Fine for
  the current single-rectangle water body; will break the moment any map gets a
  non-trivial coastline. Worth fixing before adding more naval-heavy maps.
- [ ] **Wall's NavMeshObstacle carving was never confirmed live** — only verified by
  config inspection, blocked repeatedly by the Editor "frame stuck" flakiness. That
  flakiness now has a real fix (`Application.runInBackground` +
  `EditorApplication.QueuePlayerLoopUpdate()` + forced repaint, found late in the log)
  — worth going back and live-confirming this and any other "verified by reflection
  only, not live" item now that the blocker is actually solved.

### Medium priority — real content/design work, not bug fixes

- [ ] **4 unique units for Maurya/Maratha have no live factory** — CSV data exists
  (`maurya_war_elephant`, `pillar_edict_scholar`, `maratha_mavla_raider`,
  `maratha_durg_garrison`), but they're not spawnable yet. This is the main reason
  Maurya/Maratha, while selectable, aren't yet on par with the original 3 civs.
- [ ] **Narrower per-civ passive bonuses aren't live** — Rajput's cavalry-only gold
  discount, Maurya/Maratha's specific move-speed bonuses, etc. exist in
  `CivilizationDefinition.passiveBonuses` but nothing reads them; only the legacy
  5-field `CivilizationProfile` subset is wired. Closing this is what makes the 2 new
  civs (and Chola/Vijayanagara/Rajput's fuller bonus sets) actually function as
  designed rather than just as flavor text.
- [ ] **Balance pass (item 43) is still explicitly ongoing** — the Cavalry-vs-Archer
  fix is real and verified, but civ/age/upgrade multiplier stacking, training
  cost-vs-power ratios, and actual sustained live playtesting haven't happened.
  `playtest_log.csv` process exists but is still empty per the log — start actually
  using it.
- [ ] **Naval balance is one evidenced fix, not a full pass** — only Naval→Archer was
  tuned; every other naval matchup is unaudited flat 1x.
- [ ] **UI skin (item 47's second half) hasn't started at all** — building/unit
  models have real sourced art now, but the HUD/menus are still functional-only, no
  visual skin pass.
- [ ] **2 Crusader Knight body models sourced but not wired in** — rig-compatibility
  with `WeaponAttachment`/`AnimationDriver` was never verified; swapping the shared
  human rig risks breaking every unit at once if done blind. Needs a dedicated
  verification step before it's safe to use.

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

- [ ] **UI skin** — full HUD/menu visual pass; currently functional-only, genuinely
  unstarted. Needs its own style sheet (panel art, icon set, cursor set) consistent
  with the 3D art direction above, not sourced piecemeal per element.
- [ ] **4 Maurya/Maratha unique units** (`maurya_war_elephant`, `pillar_edict_scholar`,
  `maratha_mavla_raider`, `maratha_durg_garrison`) — need real models at the 4.1
  unit spec; currently would fall back to the shared generic body with no
  distinguishing silhouette, which undercuts exactly the "civs feel distinct" goal
  these units exist for.
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
2. **Wire the remaining per-civ passive bonuses live** — this is what makes Maurya/
   Maratha (and the fuller bonus sets on the original 3) actually functional, not just
   selectable.
3. **Build the 4 missing unique-unit factories** — closes the last real content gap
   in the 5-civ roster.
4. **Re-verify every "confirmed by reflection/config only, not live" item** now that
   the Editor flakiness fix exists (Wall carving is the flagged one, but check for
   others across the log).
5. **Resume the balance pass properly** — start actually logging to
   `playtest_log.csv`, then tackle civ/age/upgrade stacking.
6. Everything else (music, tutorial, performance profiling, UI skin, store assets) is
   real but lower-urgency — sequence after the above based on what you want to
   prioritize next, not by default order.

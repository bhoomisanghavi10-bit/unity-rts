# Turning the AoE Research Pack into BHARAT RTS Features (Unity/C#)

## Why not just paste the links into Claude Code

Claude Code works best from structured, local context — files it can read, grep, and reference repeatedly — not from a list of URLs it has to fetch (and may not even have network access to fetch, depending on how that session is configured). A link like "AoE2 civ bonus wiki" doesn't tell Claude Code what *your* civ's bonus should number out to, or what C# type it should populate. So the pipeline is:

**Links (done) → Design decisions (done, in `aoe_research_pack.md`) → Structured data specs (this kit's CSVs) → Code scaffolding (this kit's ScriptableObjects) → Sequenced Claude Code prompts (below)**

Everything after the research pack is now in this kit. Drop the whole kit into your repo — e.g. `Assets/Design/` — so Claude Code can read and edit it like any other source file, then run the prompts in order.

## Setup (one-time)

1. Copy this kit into your Unity project: `Scripts/*.cs` → `Assets/Scripts/Data/`, `Data/*.csv` → `Assets/Design/`.
2. Copy `aoe_research_pack.md` (already saved in your BHARAT RTS project on claude.ai — export it or re-paste it) into `Assets/Design/aoe_research_pack.md` too, so it's in the repo Claude Code actually works in.
3. Commit that as a baseline commit ("add AoE research + data schema scaffolding") so later changes diff cleanly.

## Phase 1 — Tech tree + civilization bonuses

The CSVs (`civ_bonus_template.csv`, `tech_tree_template.csv`) currently have 2-3 example rows each, patterned after specific AoE2 bonuses (see the `DesignInspiration` column). That's the format — not the content. The actual civ roster, numbers, and flavor for BHARAT RTS still need deciding.

**Prompt to give Claude Code:**

> Read `Assets/Design/aoe_research_pack.md` (sections 0 and 2) and `Assets/Design/civ_bonus_template.csv` / `tech_tree_template.csv`. Using `TechNode.cs` and `CivilizationDefinition.cs` in `Assets/Scripts/Data/` as the target schema, design a full tech tree and civilization roster for BHARAT RTS: [N] civilizations based on [name the historical dynasties/regions you want — e.g. Maurya, Chola, Maratha, Vijayanagara]. For each civ, follow the AoE2 pattern described in the research pack: 2 unique units, 1-2 unique techs, 2-3 passive bonuses that are structural (change *how* the civ plays, not just flat stat buffs), and one team bonus. Fill out `civ_bonus_template.csv` and `tech_tree_template.csv` completely, then write a Unity Editor script (`Assets/Editor/CsvToScriptableObject.cs`) that reads those CSVs and generates the corresponding `.asset` files automatically.

Why this works: it points Claude Code at the *design reasoning* (research pack §0/§2 — AoE2 civ-bonus philosophy), the *data contract* (the CSV columns), and the *code contract* (the ScriptableObject fields) all at once, so it isn't guessing at any of the three independently.

## Phase 2 — Unit roster + counter system

**Prompt to give Claude Code:**

> Read `Assets/Design/aoe_research_pack.md` section 2 (unit rosters and AoE4 counter design links) and `Assets/Design/unit_roster_template.csv` / `counter_matrix_template.csv`. Using `UnitDefinition.cs` and `CounterMatrix.cs` as the schema, design the full unit roster for BHARAT RTS — generic units available to all civs, plus each civ's 2 unique units from Phase 1. Follow the AoE4 rock-paper-scissors philosophy (explicit, legible counters: spearman-type hard-counters cavalry, cavalry punishes archers/siege, archers punish infantry, siege punishes buildings but is weak vs infantry blobs). Fill both CSVs completely, then extend the Editor CSV-import script from Phase 1 to also generate `UnitDefinition` assets and populate a single `CounterMatrix` asset from `counter_matrix_template.csv`.

Sanity-check numbers against live data rather than eyeballing them: point Claude Code at [aoe4world.com Explorer — Units](https://aoe4world.com/explorer/units) and [unitstatistics.com](https://unitstatistics.com/age-of-empires2/) and ask it to flag any BHARAT RTS unit whose cost-to-stats ratio is wildly out of line with the AoE reference units in the same role.

## Phase 3 — Formations

**Prompt to give Claude Code:**

> Read `Assets/Design/aoe_research_pack.md` section 2 (formations) and `FormationDefinition.cs`. Implement a `FormationController` MonoBehaviour that, given a selected group of units and a `FormationDefinition` asset, arranges units into front/back rows by `UnitCategory` (melee/high-armor in `preferredFrontRow`, ranged/siege in `preferredBackRow`), respecting `unitSpacing` and `unitsPerRow`. Support at minimum the Line and Box formation types from `FormationType`. Model the behavior on the AoE2 formation system described in the research pack (front line absorbs hits, back line free-fires) rather than free-for-all clumping.

## Phase 4 — Balance iteration loop

Once all three systems exist, treat balancing as its own recurring task rather than a one-off:

> After each playtest, log [civ, unit, matchup] outcomes to `Assets/Design/playtest_log.csv`. Periodically ask Claude Code: "Read `playtest_log.csv` and `counter_matrix_template.csv` / `unit_roster_template.csv` — are there any matchups with a >70% win rate in 10+ games? Propose the smallest numeric change (not a redesign) that would bring it toward 50-55%, citing which AoE unit's historical balance patches solved a similar problem if relevant."

This mirrors how AoE2/AoE4 are actually balanced in practice (small numeric patches against measured win rates, not full redesigns) — see the aoe4world.com and aoestats-style sites in the research pack, which exist specifically for this kind of win-rate tracking.

## What's in this kit

- `Scripts/TechNode.cs`, `CivilizationDefinition.cs`, `UnitDefinition.cs`, `CounterMatrix.cs`, `FormationDefinition.cs` — Unity ScriptableObject schema, ready to drop into `Assets/Scripts/Data/`.
- `Data/civ_bonus_template.csv`, `tech_tree_template.csv`, `unit_roster_template.csv`, `counter_matrix_template.csv` — structured spec sheets with 2-3 worked examples each, in the format Claude Code should fill in and then convert to `.asset` files.

None of the example rows are final game balance — they're there so Claude Code (and you) can see the expected shape of a complete row before generating the rest.

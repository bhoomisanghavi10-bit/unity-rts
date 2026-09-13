# Kingdoms of Bharat

A historical real-time strategy game across Indian kingdoms and empires,
targeting AoE II/IV-level systemic depth. This README describes the actual
current state of the Unity project. For the full roadmap, open
`docs/KingdomsOfBharat_Master_Reference.xlsx` — start with its "Dev Status
Overview" sheet.

This is well past the original single-map prototype the earlier version of
this README described. The core RTS loop (gather, build, train, fight) is
long since proven; the project has grown into a 5-civilization, 4-age,
~25-unit-type systemic RTS with naval combat, diplomacy, a scenario editor,
and a working LAN multiplayer MVP.

## Current scope

- **5 civilizations**, each with a real historical identity, passive bonuses,
  a unique unit, and a unique tech: **Chola** (trade/economy), **Vijayanagara**
  (fortifications/tempo), **Rajput** (cavalry power), **Maurya** (administrative/
  gilded, Free Houses + Age-up discount), **Maratha** (guerrilla/cavalry speed).
- **4 Ages**: Ancient → Classical → Durg → Imperial, each with its own cost/
  gather-rate/HP/train-time multipliers and (from Classical onward) a
  building-count prerequisite, not just a resource cost.
- **~25 trainable unit types** across most with 2-5 tier upgrade ladders
  (Infantry, Spearman, Archer, Skirmisher, Cavalry, Cavalry Archer, Camel
  Rider, Scout, Battering Ram, Scorpion, Trebuchet, Siege, naval War
  Galley/Fire Ship, a Trader (Vanik + Trade Ship), support units (Vaidya the
  healer, Purohita the converter), a Maharaja hero unit with an opt-in
  Regicide victory condition, plus 10 civ-exclusive unique units), a full
  counter web (14 `UnitClass` values, melee/pierce armor split, splash/
  trample/pass-through/minimum-range mechanics), and stances/formations.
- **15 building types**, all civ-reskinned (TownCenter, Barracks, Durg,
  Karmashala, Monastery, Market, Dock, Farm, House, Wall, Gate, Tower,
  Lumber Camp, Mining Camp, Mill), with age-tiered visuals on TownCenter/
  Tower/Wall that re-skin retroactively on Age-up.
- **Naval warfare** — Docks, War Galleys, Fire Ships, water-aware pathing.
- **Economy depth** — 4 resources, resource-specific drop-off buildings,
  farm depletion + reseed, tribute, distance-paid trade routes (land and
  sea), repair with diminishing multi-worker returns, a Town Bell.
- **Meta systems** — fog of war with per-unit vision, a minimap, diplomacy
  (war/ally toggle + tribute), save/load, a full in-game scenario editor
  (place starting units/buildings, author objectives/triggers, save/load/
  browse, and play a custom scenario solo or over LAN), hotkeys with an F1
  reference overlay, and a scripted AI opponent.
- **Multiplayer** — a deterministic lockstep LAN MVP (2 human players, real
  TCP transport, cross-peer desync detection with snapshot resync). Online
  play/matchmaking is explicitly deferred; only true cross-machine
  determinism testing remains unverified (needs a second physical machine).
- **UI** — an ornate HUD art pass (resource bar, command grid with icons and
  tooltips, selected-unit panel with portraits, minimap frame, menus), laid
  out as one AoE-style bottom bar.

Everything above is implemented and covered by the EditMode test suite
(~520+ tests) unless the roadmap sheet flags it as partial/blocked. The
long remaining tail is mostly **art sourcing** (per-civ unit gear, unit
tier-step visuals, building portraits, bespoke models for Durg/Karmashala/
Monastery/Fish Trap) and **optional meta content** (Wave 6: idle-worker
indicator, Relics, Score, Wonder/King-of-the-Hill victory conditions, game
modes, cheat codes, tutorial, music) — see the Master Reference workbook's
"Dev Status Overview" and "Implementation Waves 0-6" sheets for the
authoritative, per-item breakdown.

## Tech stack

- Unity (URP), C#
- Built-in NavMesh for land pathfinding; a water-clamped mover for boats
- Deterministic lockstep simulation (`SimClock`/`CommandBus`) with a real
  LAN TCP transport for 2-player multiplayer
- Data-driven civ/tech/unit content: CSVs under `Assets/Design/Data/*.csv`
  → generated ScriptableObjects (`Assets/Editor/CsvToScriptableObject.cs` →
  `Assets/Resources/Data/Generated/`), read via `Core/DataRegistry.cs`.
  Don't hand-edit generated assets — edit the CSV and regenerate via the
  `BharatRTS/Generate Data Assets From CSV` menu item.

## Project structure

```
Assets/
  Scenes/            The main scene (Main.unity)
  Scripts/
    Units/            Unit identity, movement, per-unit-type factories
    Buildings/        TownCenter, Barracks, Durg, Karmashala, Monastery,
                       Market, Dock, Farm, House, Wall, Gate, Tower,
                       Lumber Camp, Mining Camp, Mill, construction/repair
    Combat/           Attack resolution, melee/boat/building attackers,
                       counters, damage types, splash/trample/garrisoning
    Resources/        Gathering, trade routes, healing/conversion, drop-offs
    Progression/      Age progression, per-line tier upgrades, unique tech
    Multiplayer/       LAN transport, command serialization, desync recovery
    Match/             Scenario/mission system, match outcome evaluation
    Core/              Civilization/faction data, team color, team bonuses
    Camera/            RTS camera controller (pan, edge-scroll, zoom)
    Selection/         Click / box-select / order-dispatch input handling
    UI/                 HUD, build menu, selected-unit panel, scenario
                        editor, settings, diplomacy, hotkey overlay
    FogOfWar/           Vision grid, fog rendering, minimap
    AI/                 Scripted opponent (economy, building, combat)
    Wildlife/           Wild boars, livestock
    Audio/              SFX playback
    Data/               Runtime data registry
  Editor/             CSV→ScriptableObject pipeline, asset importers
  Tests/EditMode/     ~520+ NUnit EditMode tests (KingdomsOfBharat.Tests.asmdef)
  Design/Data/        Source CSVs for civs/units/techs/ages
  Resources/          Generated data assets, UI art, per-civ models
```

Each folder is single-responsibility: a unit's movement, selection,
gathering, and combat behaviors are separate components composed onto one
GameObject, not one monolithic script. Runtime code compiles into
`KingdomsOfBharat.Runtime.asmdef`; editor tooling into
`KingdomsOfBharat.Editor.asmdef`; tests into their own asmdef referencing
both plus the Unity Test Framework.

## Running the game

Open `Assets/Scenes/Main.unity` and press Play. Pick a civilization on the
pre-match Civilization Picker, then play a skirmish against the AI or start/
join a LAN match.

- **Camera:** WASD/arrow keys or edge-scroll to pan, mouse wheel to zoom
- **Select:** left-click a unit/building, or left-click-drag a box
- **Orders:** right-click open ground to move, a resource node to gather, a
  hostile unit/building to attack, a friendly building to garrison/repair/
  train, an ally's Market/Dock to set a trade route
- **Build:** the bottom-left command grid shows placement/training/research
  buttons for whatever's selected, each with a hotkey — press **F1** for the
  full hotkey reference overlay
- **HUD:** top-left resource/population/age bar (with a live Age-up research
  meter), bottom-center selected-unit panel (portrait, HP, group icon row),
  bottom-right minimap

Settings (**Esc**, or the in-scene Settings button) exposes key rebinding,
colorblind mode, and the Regicide toggle. Diplomacy (**F11**) lets you flip
War/Allied per faction and send tribute. The Scenario Editor is reachable
from Mission Select ("Create Scenario") for placing custom starts and
authoring objectives/triggers, playable solo or over LAN.

## Testing

Run the EditMode suite via Unity's Test Runner (`Window > General > Test
Runner`), or `mcp__UnityMCP__run_tests` if driving via MCP. The suite should
be green apart from 2 known pre-existing `BuildingModelFactoryTests`
failures (a Chola TownCenter model gap, tracked as a known baseline, not a
regression to chase).

## Full roadmap & design docs

`docs/KingdomsOfBharat_Master_Reference.xlsx` is the single source of truth
for scope, status, and design detail — see its "Dev Status Overview" sheet
first, then "Roadmap - Open Items & Priority", "Roadmap - Architecture
Notes", "Roadmap - Process Note", "Roadmap - Art Direction", and
"Implementation Waves 0-6". `docs/SESSION_LOG.md` has the full session-by-
session history. `CLAUDE.md` carries the current-session status and the
project's coding/session conventions.

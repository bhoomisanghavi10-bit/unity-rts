# DefinitionCatalog migration (F01 vertical slice)

## Implemented contract

| Stable ID | Runtime creation binding | Existing data key |
| --- | --- | --- |
| `unit.common.worker` | WorkerFactory.SpawnCore | `worker` |
| `unit.chola.padati.t1` | SoldierFactory.SpawnCore, infantry tier 0 | `soldier` |
| `unit.chola.dhanurdhara.t1` | ArcherFactory.SpawnCore, archer tier 0 | `archer` |
| `building.common.town_center` | TownCenterFactory.PlaceCore | Factory-owned stats |
| `building.common.barracks` | BarracksFactory.PlaceCore | Factory-owned stats |

IDs are case-sensitive persistence keys, independent of C# type names, display names, object names and CSV asset filenames. Never rename or reuse a released ID. Add an explicit migration/alias if a future content change replaces an ID. `t1` identifies the base roster tier (the existing progression arrays call this index 0); it is not a file-format version. Changes to balance values do not require new IDs.

`DefinitionCatalog.Default` is the single runtime registry. Entries expose kind, display name, existing data key, unit icon, optional civilization restriction and placement offset. Duplicate registration and unknown IDs throw errors containing the offending ID. Spawn delegates live only in ordinary C# objects; no new delegate-bearing Unity assets are created. The existing CSV source and generated assets remain unchanged.

Every catalog spawn receives one `DefinitionId` on its root. Worker, Town Center and Barracks public factories always delegate to the catalog. Soldier/Archer public factories delegate only for Chola's base tier; other civilizations and researched tiers retain the original creation path and are deliberately not mislabeled as the reference definitions. Explicit `t1` spawns use tier 0 even when the faction has researched a later tier. Civilization-specific entries require a matching faction civilization; the catalog does not change the faction registry to satisfy a request.

Existing training callers (TownCenter and Barracks), initial spawners, AI and building placement reach the catalog through those public factory adapters. Their parameters, costs, timers, rally behavior, models, faction bonuses and age scaling remain unchanged. The base UnitDefinition rows contain training metadata but the existing reference training cost/timer fields are still authoritative; migrating those is a separate change.

`EntitySpawner` accepts stable IDs as well as its original legacy names. This connects both custom scenario placement and save restoration without adding a SaveManager type switch. The scenario editor's existing palette continues to emit legacy names; programmatic scenario placement may use stable IDs. Save capture prefers DefinitionId and stores it in the existing `unitType` / `buildingType` fields. Legacy files remain readable. Older game builds do not understand new catalog IDs; a complete versioned save schema is still future F01 work.

Catalog building placement metadata reverses the factory's root height offset during capture (Barracks: ground input; Town Center: existing center/terrain-snapped convention). Unknown catalog save IDs and wrong entity kinds are validated before match teardown. Unmigrated legacy handling retains its existing limitations.

## Inspection findings

- Unit factories build component graphs and read generic CSV UnitDefinition rows; faction/age bonuses and most line tiers are applied at spawn. A generic CSV key alone cannot represent individual civilization/tier identity.
- Building factories own their stats and placement offsets. Wall additionally accepts rotation. Model factories (Human, Boat, Ox, BuildingModel, ProceduralBuilding) are presentation helpers, not separate gameplay definitions.
- TownCenter/Barracks use local training fields for the reference units; later Barracks lines and UniqueUnitDefinition also read generated UnitDefinition data. Dock, Monastery, Market and Durg each have their own training dispatch.
- Scenario placement and save restoration share EntitySpawner. ScenarioEditorMenu has a separate legacy palette/preview mapping; CustomScenarioData reuses the save placement DTOs.
- DataRegistry loads Resources/Data/Generated; CsvToScriptableObject generates those assets from Assets/Design/Data CSVs. UnitDefinition, CivilizationDefinition, AgeProfileDefinition, TechNode, CounterMatrix and FormationDefinition are data assets, distinct from the new runtime creation bindings.

## Remaining entity checklist

- [ ] Soldier/Archer: other civilizations and all higher tiers. Define explicit tier creation arguments, then update adapters; do not assign base-tier IDs to upgraded entities.
- [ ] Common land combat: Spearman, Scout (CSV `chara`), Skirmisher, Cavalry, CavalryArcher, CamelRider, Siege, BatteringRam, Scorpion, Trebuchet, including their tiers.
- [ ] Naval: FishingBoat, WarGalley, FireShip, TradeShip, including tiers and water placement rules.
- [ ] Support/trade: Vaidya, Purohita, Vanik; preserve healing, conversion, relic and trade components.
- [ ] Civilization-specific/hero: CholaNavalRaider, MauryaWarElephant, PillarEdictScholar, RajputRoyalGuard, MarathaMavlaRaider, MarathaDurgGarrison, VijayanagaraWarElephant, Maharaja. Include elite/elephant/hero state and training limits.
- [ ] Buildings already in legacy EntitySpawner: Farm, House, Wall, Gate, Tower, Market. Preserve rotation, footprints, construction durations, garrisons and placement conventions.
- [ ] Buildings missing from legacy EntitySpawner: Dock, Durg, Karmashala, Monastery, LumberCamp, MiningCamp, Mill.

For each batch:

- [ ] Reserve permanent IDs; add metadata/CSV references and explicit runtime creation bindings to the one catalog.
- [ ] Move creation behind public factory adapters; migrate the associated training and scenario palette entries. Validate generated data IDs against duplicates rather than silently overwriting dictionary entries.
- [ ] Test all faction/tier variants, adapters, placement, identity, unknown IDs and save round trips; retain existing gameplay tests.
- [ ] Define conversion behavior: civilization of origin versus current owner. This slice rejects a civilization-specific ID for a mismatching owner civilization.
- [ ] Add aliases/migrations for existing persisted names before removing legacy EntitySpawner and SaveManager inference paths.

## Remaining F01 save work (not completed by this slice)

- [ ] Version and validate the full match schema before teardown, including unsupported legacy entities and malformed data.
- [ ] Serialize individual age/tier/baked modifiers separately from logical definition identity; current faction/age modifiers still apply during spawn.
- [ ] Restore construction progress, orders, training/research queues, resource depletion/carrying, relics, garrisons, rotation, fog, mission state, RNG and simulation tick.
- [ ] Stage restoration of registries, entities, cross-entity references and AI ownership. The existing broader load/resync behavior is not repaired here.
- [ ] Exercise the full F01 acceptance fixture and compare continued simulation after restore.

## Validation

Full suites run in Unity 6000.3.21f1: **715/715 EditMode passed; 12/12 PlayMode passed; zero failures or skips**. The MCP PlayMode job lost its status connection across domain reload and reported an initialization timeout; Unity completed the run and its persisted NUnit TestResults.xml confirms all 12 tests passed. New tests cover fixed IDs, duplicate registration, metadata binding, unknown/wrong-kind rejection, real spawns, root identity, public adapters, researched-tier compatibility and save-ID/placement round trips.

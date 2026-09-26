using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Core
{
    // AoE-style quicksave/quickload: F5 captures the entire running match
    // into a JSON file, F9 tears down whatever's currently running and
    // rebuilds it from that file. Self-installs via
    // RuntimeInitializeOnLoadMethod rather than a scene-placed GameObject,
    // so this feature needs zero Main.unity changes - every system it
    // touches is reached through existing public statics/Factories only.
    //
    // Known v1 limitations (documented, not silently dropped):
    //  - A restored Enemy base isn't "adopted" by AiController - it has no
    //    path to recognize a TownCenter/Barracks/Farm/House it didn't
    //    build/spawn itself (those private fields are only ever assigned
    //    in its own Start()/TryBuild*() methods), so after a load the AI
    //    will attempt to build a fresh economy alongside its restored one
    //    rather than resuming management of it.
    //  - In-progress construction/training/research countdowns aren't
    //    restored - a loaded building resumes as either complete or
    //    freshly-placed-and-idle, never "50% built."
    //  - ResourceNode depletion, rally points, and a unit's current
    //    order/target aren't restored - a loaded unit stands idle at its
    //    saved position.
    //  - UpgradeProgress/CivilizationRegistry/AgeProgress are static
    //    registries that nothing currently resets between matches within
    //    the same running session (a pre-existing gap, not introduced
    //    here) - repeatedly restarting/loading without fully exiting Play
    //    mode could leave a stale, higher tier from an earlier match
    //    "stuck" above what a load's AdvanceToTier (increment-only, see
    //    below) would otherwise bring it down to.
    public class SaveManager : MonoBehaviour
    {
        // Extended catalog-reference save schema (stable definition IDs +
        // stable runtime IDs + construction/production/research-queue
        // restoration + phased restore, see this file's Capture/
        // ApplySnapshotToRunningMatch). A save from before this field
        // existed deserializes with MatchSaveData.version left at its C#
        // default (0) - fully supported, see ValidateSaveVersion's own
        // comment and every new field's own "-1/null = not present"
        // sentinel default in SaveData.cs.
        internal const int CurrentSaveVersion = 1;

        private const string SaveFileName = "quicksave.json";
        [SerializeField] private KeyCode saveKey = KeyCode.F5;
        [SerializeField] private KeyCode loadKey = KeyCode.F9;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // Guards against a duplicate: this method re-fires on every
            // scene load (including GameOverScreen's "Play Again" restart),
            // but the DontDestroyOnLoad instance from the FIRST load is
            // still alive at that point.
            if (FindFirstObjectByType<SaveManager>() != null)
            {
                return;
            }

            GameObject go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
            DontDestroyOnLoad(go);
        }

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // Item 1 (Hotkeys): these were plain fixed fields, never routed
        // through GameSettings like every other hotkey in the project -
        // meaning F5/F9 looked rebindable (they're listed as such in the
        // hotkey reference overlay) but silently weren't. Brought in line
        // with the established per-field GameSettings.GetKey override
        // pattern (see BuildingPlacer.ApplyKeySettings).
        private void Awake()
        {
            saveKey = GameSettings.GetKey("SaveGame", saveKey);
            loadKey = GameSettings.GetKey("LoadGame", loadKey);
        }

        private void Update()
        {
            if (!CivilizationSetup.HasMatchStarted)
            {
                return;
            }

            if (Input.GetKeyDown(saveKey))
            {
                Save();
            }
            else if (Input.GetKeyDown(loadKey))
            {
                StartCoroutine(LoadRoutine());
            }
        }

        private void Save()
        {
            MatchSaveData data = Capture();
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[SaveManager] Saved to {SavePath}");
        }

        // Item 48: Player/Enemy/Enemy2 are always all three captured
        // (Enemy2's entry is just harmless defaults - Chola civ, Ancient
        // age, empty resources - when no 2nd AiController ever spawned)
        // rather than conditionally including it, so RestoreFactionState
        // on load never has to guess whether a save predates 3-faction
        // support - it always finds an entry, even if that entry
        // represents "this faction was never in play."
        private static readonly FactionId[] AllFactions = { FactionId.Player, FactionId.Enemy, FactionId.Enemy2 };

        // AoE-Parity Phase 5 (resync-on-desync): internal rather than
        // private so DesyncRecovery can capture an authoritative snapshot
        // outside the Save()/file-I/O path - see DesyncRecovery.cs.
        internal static MatchSaveData Capture()
        {
            var data = new MatchSaveData { version = CurrentSaveVersion, mapId = (int)MapRegistry.CurrentId };

            foreach (FactionId faction in AllFactions)
            {
                data.factions.Add(CaptureFaction(faction));
            }

            CaptureAlliances(data);

            foreach (Unit unit in Unit.All)
            {
                string unitType = IdentifyUnitType(unit);
                if (unitType == null || !unit.TryGetComponent(out FactionMember factionMember))
                {
                    continue;
                }

                unit.TryGetComponent(out Attackable attackable);
                unit.TryGetComponent(out StanceController stance);
                int unitNetworkId = Multiplayer.NetworkId.TryGetId(unit, out int uid) ? uid : -1;

                data.units.Add(new UnitSaveData
                {
                    unitType = unitType,
                    faction = (int)factionMember.Faction,
                    position = unit.transform.position,
                    health = attackable != null ? attackable.Health : 0f,
                    stance = stance != null ? (int)stance.Stance : -1,
                    networkId = unitNetworkId,
                });
            }

            foreach (Building building in Building.All)
            {
                string buildingType = IdentifyBuildingType(building);
                if (buildingType == null || !building.TryGetComponent(out FactionMember factionMember))
                {
                    continue;
                }

                building.TryGetComponent(out Attackable attackable);
                building.TryGetComponent(out ConstructionSite site);
                int buildingNetworkId = Multiplayer.NetworkId.TryGetId(building, out int bid) ? bid : -1;

                var buildingData = new BuildingSaveData
                {
                    buildingType = buildingType,
                    faction = (int)factionMember.Faction,
                    position = GroundPointFor(buildingType, building),
                    health = attackable != null ? attackable.Health : 0f,
                    isComplete = site == null || site.IsComplete,
                    networkId = buildingNetworkId,
                    constructionProgress = site != null ? site.Progress : 1f,
                };

                // Reference-catalog production/research queue example -
                // see TownCenter.TrainingRemaining/Barracks.TrainingRemaining/
                // TrainingDefinitionId/InfantryTierResearchProgress's own
                // comments for why this stays limited to Worker training,
                // Padati/Dhanurdhara training, and Padati's own tier
                // research rather than every queue this project has.
                ProductionQueue queueToSave = building is TownCenter tcq ? tcq.Queue : (building is Barracks bq ? bq.Queue : null);
                if (queueToSave != null)
                {
                    foreach (ProductionItem item in queueToSave.Items)
                    {
                        var itemSave = new ProductionItemSaveData
                        {
                            kind = item.Kind,
                            label = item.Label,
                            costTypes = new int[item.CostTypes.Length],
                            costAmounts = (float[])item.CostAmounts.Clone(),
                            total = item.Total,
                            remaining = item.Remaining,
                        };
                        for (int i = 0; i < item.CostTypes.Length; i++)
                        {
                            itemSave.costTypes[i] = (int)item.CostTypes[i];
                        }

                        buildingData.productionQueue.Add(itemSave);
                    }
                }

                if (building is TownCenter townCenter && townCenter.IsTraining)
                {
                    buildingData.trainingDefinitionId = DefinitionCatalog.Worker;
                    buildingData.trainingRemaining = townCenter.TrainingRemaining;
                }
                else if (building is Barracks barracks)
                {
                    if (barracks.IsTraining && barracks.TrainingDefinitionId != null)
                    {
                        buildingData.trainingDefinitionId = barracks.TrainingDefinitionId;
                        buildingData.trainingRemaining = barracks.TrainingRemaining;
                    }

                    if (barracks.IsResearchingInfantryTier)
                    {
                        buildingData.infantryTierResearchRemaining = barracks.InfantryTierResearchRemaining;
                    }
                }

                data.buildings.Add(buildingData);
            }

            return data;
        }

        private static readonly UnitClass[] TieredClasses =
        {
            UnitClass.Infantry, UnitClass.Archer, UnitClass.Cavalry, UnitClass.Siege,
        };

        private static FactionSaveData CaptureFaction(FactionId faction)
        {
            var factionData = new FactionSaveData
            {
                faction = (int)faction,
                civilization = (int)CivilizationRegistry.For(faction),
                currentAge = (int)AgeProgress.CurrentAge(faction),
                attackTier = UpgradeProgress.AttackTier(faction),
                armorTier = UpgradeProgress.ArmorTier(faction),
            };

            foreach (UnitClass unitClass in TieredClasses)
            {
                factionData.classAttackTiers.Add(new ClassTierEntry { unitClass = (int)unitClass, tier = UpgradeProgress.ClassAttackTier(faction, unitClass) });
                factionData.classArmorTiers.Add(new ClassTierEntry { unitClass = (int)unitClass, tier = UpgradeProgress.ClassArmorTier(faction, unitClass) });
            }

            // DefinitionCatalog reference-entity tier ladders (Padati's/
            // Dhanurdhara's own progression) - see FactionSaveData's own
            // comment for why this stops at these two lines.
            factionData.infantryTier = InfantryLineProgress.Tier(faction);
            factionData.archerTier = ArcherLineProgress.Tier(faction);

            // AoE-Parity Phase 5 fix: ResourceStockpile.For(Enemy2) returns
            // null whenever the 3rd faction isn't enabled (the normal case -
            // its stockpile is scene-authored but never spawned/activated
            // for a standard match) - this class's own AllFactions comment
            // already documents the intent ("Enemy2's entry is just harmless
            // defaults... when no 2nd AiController ever spawned"), but the
            // implementation crashed instead of actually doing that. Pre-
            // existing bug (would have crashed the F5 quicksave feature too,
            // not just DesyncRecovery), found while live-verifying Apply()
            // against a real 2-faction match.
            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            foreach (ResourceType type in (ResourceType[])Enum.GetValues(typeof(ResourceType)))
            {
                factionData.resources.Add(new ResourceEntry { resourceType = (int)type, amount = stockpile != null ? stockpile.GetTotal(type) : 0f });
            }

            return factionData;
        }

        private static void CaptureAlliances(MatchSaveData data)
        {
            for (int i = 0; i < AllFactions.Length; i++)
            {
                for (int j = i + 1; j < AllFactions.Length; j++)
                {
                    if (DiplomacyRegistry.AreAllied(AllFactions[i], AllFactions[j]))
                    {
                        data.alliances.Add(new AllianceEntry { factionA = (int)AllFactions[i], factionB = (int)AllFactions[j] });
                    }
                }
            }
        }

        // Worker carries the same UnitClass.Infantry tag Soldier does (see
        // WorkerFactory), so Builder-presence is checked first to
        // disambiguate before falling back to Attackable.Class.
        private static string IdentifyUnitType(Unit unit)
        {
            if (unit.TryGetComponent(out DefinitionId identity)) return identity.Value;
            if (unit.TryGetComponent(out Builder _))
            {
                return "Worker";
            }

            if (!unit.TryGetComponent(out Attackable attackable))
            {
                return null;
            }

            switch (attackable.Class)
            {
                case UnitClass.Archer: return "Archer";
                case UnitClass.Cavalry: return "Cavalry";
                case UnitClass.Siege: return "Siege";
                case UnitClass.Infantry: return "Soldier";
                default: return null;
            }
        }

        // TownCenterFactory.Place(position, faction) takes an already-
        // vertically-centered position and uses it as-is; every other
        // building Factory's Place(point, faction, buildTime) takes a
        // GROUND-level point and adds half its own height internally (see
        // BarracksFactory.Place, for one). Building.transform.position is
        // always the elevated/centered one regardless of type - so saving
        // it as-is and feeding it straight back into a non-TownCenter
        // Factory.Place() on load would double that offset. Reading the
        // height back off the root's own BoxCollider (added by
        // BuildingModelFactory.AddBoundsCollider, sized from the model's
        // real rendered bounds) avoids needing each Factory's private Size
        // constant duplicated here.
        private static Vector3 GroundPointFor(string buildingType, Building building)
        {
            Vector3 position = building.transform.position;
            if (DefinitionCatalog.Default.TryGet(buildingType, out EntityDefinition definition))
                return position - Vector3.up * definition.PlacementHeightOffset;
            if (buildingType == "TownCenter" || !building.TryGetComponent(out BoxCollider box))
            {
                return position;
            }

            position.y -= box.size.y * 0.5f;
            return position;
        }

        private static string IdentifyBuildingType(Building building)
        {
            if (building.TryGetComponent(out DefinitionId identity)) return identity.Value;
            switch (building)
            {
                case TownCenter _: return "TownCenter";
                case Barracks _: return "Barracks";
                case Farm _: return "Farm";
                case House _: return "House";
                case Wall _: return "Wall";
                case Gate _: return "Gate";
                case Tower _: return "Tower";
                case Market _: return "Market";
                default: return null;
            }
        }

        private IEnumerator LoadRoutine()
        {
            string path = SavePath;
            if (!File.Exists(path))
            {
                Debug.LogWarning("[SaveManager] No save file found at " + path);
                yield break;
            }

            MatchSaveData data = JsonUtility.FromJson<MatchSaveData>(File.ReadAllText(path));
            ValidateSaveVersion(data);
            ValidateCatalogEntries(data);
            ValidateRuntimeIds(data);

            CivilizationSetup civSetup = FindFirstObjectByType<CivilizationSetup>();
            if (civSetup == null)
            {
                Debug.LogWarning("[SaveManager] No CivilizationSetup in scene - can't load.");
                yield break;
            }

            FactionSaveData playerData = data.factions.Find(f => f.faction == (int)FactionId.Player);
            FactionSaveData enemyData = data.factions.Find(f => f.faction == (int)FactionId.Enemy);
            FactionSaveData enemy2Data = data.factions.Find(f => f.faction == (int)FactionId.Enemy2);

            // Wipe whatever's running right now (this session's live match)
            // before the normal match-start flow spawns its own defaults on
            // top of it.
            WipeCurrentMatch();

            civSetup.BeginMatch(playerData != null ? (CivilizationId)playerData.civilization : CivilizationId.Chola);
            // BeginMatch's own map selection used its Inspector default -
            // override with the save's map right after, before any gated
            // content's Start() (deferred to "before the next Update," not
            // necessarily this exact frame) reads MapRegistry.Current.
            MapRegistry.Select((MapId)data.mapId);
            if (enemyData != null)
            {
                CivilizationRegistry.Assign(FactionId.Enemy, (CivilizationId)enemyData.civilization);
            }

            // Item 48: only assigns Enemy2 a civilization if the save
            // actually has forces/units for it - an empty default entry
            // (see Capture's AllFactions comment) shouldn't make a 2nd AI
            // faction appear real when the save never had one.
            bool enemy2InPlay = data.units.Exists(u => u.faction == (int)FactionId.Enemy2)
                || data.buildings.Exists(b => b.faction == (int)FactionId.Enemy2);
            if (enemy2Data != null && enemy2InPlay)
            {
                CivilizationRegistry.Assign(FactionId.Enemy2, (CivilizationId)enemy2Data.civilization);
            }

            foreach (AllianceEntry entry in data.alliances)
            {
                DiplomacyRegistry.SetAllied((FactionId)entry.factionA, (FactionId)entry.factionB, true);
            }

            // Let every gated Start() (TownCenterSpawner, UnitSpawner,
            // AiController, ResourceNodeSpawner...) finish its own default
            // spawn first - two frames of margin for that deferral.
            yield return null;
            yield return null;

            ApplySnapshotToRunningMatch(data);

            Debug.Log("[SaveManager] Loaded from " + path);
        }

        // AoE-Parity Phase 5 (resync-on-desync): the actual "apply a
        // captured snapshot's dynamic state to whatever's currently
        // running" operation, extracted from LoadRoutine's own tail so
        // DesyncRecovery can reuse it directly - see DesyncRecovery.cs.
        // Deliberately does NOT touch civ/map selection or call
        // CivilizationSetup.BeginMatch: those only make sense when starting
        // a fresh match from a file (LoadRoutine's job above this method),
        // not when correcting an already-running match's diverged state
        // (DesyncRecovery's job) - the civ/map are already correct in that
        // case, only units/buildings/factions have drifted.
        internal static void ApplySnapshotToRunningMatch(MatchSaveData data)
        {
            ValidateSaveVersion(data);
            ValidateCatalogEntries(data);
            ValidateRuntimeIds(data);
            FactionSaveData playerData = data.factions.Find(f => f.faction == (int)FactionId.Player);
            FactionSaveData enemyData = data.factions.Find(f => f.faction == (int)FactionId.Enemy);
            FactionSaveData enemy2Data = data.factions.Find(f => f.faction == (int)FactionId.Enemy2);
            bool enemy2InPlay = data.units.Exists(u => u.faction == (int)FactionId.Enemy2)
                || data.buildings.Exists(b => b.faction == (int)FactionId.Enemy2);

            WipeCurrentMatch();

            // Ordered restore phases (this ticket's own requirement):
            //
            // 1. Match configuration - civ/age/resources/tier progression,
            //    per faction. Deliberately BEFORE any entity creation
            //    below: this project's established convention is that a
            //    tiered unit's stats bake in at spawn time from whatever
            //    tier its faction has RIGHT NOW (see InfantryLineProgress's
            //    own header comment, "not retroactive") - so a saved
            //    Padati-tier-1 ("Senani") unit only spawns with the
            //    correct, already-upgraded stats if its faction's tier is
            //    already restored before EntitySpawner ever runs.
            RestoreFactionState(FactionId.Player, playerData);
            RestoreFactionState(FactionId.Enemy, enemyData);
            if (enemy2InPlay)
            {
                RestoreFactionState(FactionId.Enemy2, enemy2Data);
            }

            // 2. Entity creation - spawn every building, then every unit,
            //    reassigning each one's stable runtime NetworkId (separate
            //    from and in addition to its stable string DefinitionId)
            //    back to its original saved value.
            List<(BuildingSaveData saved, GameObject go)> restoredBuildings = RestoreBuildingEntities(data.buildings);
            List<(UnitSaveData saved, GameObject go)> restoredUnits = RestoreUnitEntities(data.units);

            // 3. Health/construction state - per-entity, so it has to run
            //    after step 2 actually created something to apply it to.
            RestoreBuildingHealthAndConstruction(restoredBuildings);
            RestoreUnitHealthAndStance(restoredUnits);

            // 4. Entity references and orders - out of scope for this
            //    ticket's catalog reference entities: none of Worker/
            //    Padati/Dhanurdhara/TownCenter/Barracks carry a saved
            //    gather target, attack order, or rally override today (see
            //    this class's own header comment's "Known v1 limitations"
            //    - still true, not touched by this pass). Left as an
            //    explicit, named phase with nothing to do yet, rather than
            //    silently absent, so a future session extending this to a
            //    unit/building that DOES have one knows exactly where it
            //    belongs in the ordering.

            // 5. Production/research queues - per-entity, so it also has
            //    to run after step 2.
            RestoreProductionQueues(restoredBuildings);

            // 6. Resume simulation - nothing to do explicitly. Every
            //    restored MonoBehaviour's own Update() already ticks
            //    normally from the next frame on; this class never touches
            //    Time.timeScale.
        }

        // Requirement: unsupported save versions must produce a clear
        // error, never be silently accepted or silently downgraded. A save
        // with no version field at all (any save from before this field
        // existed) deserializes as version 0 and is fully supported - see
        // every new field's own "not present" sentinel default in
        // SaveData.cs, which the restore phases above skip cleanly.
        internal static void ValidateSaveVersion(MatchSaveData data)
        {
            if (data.version < 0 || data.version > CurrentSaveVersion)
            {
                throw new InvalidOperationException(
                    $"Save version {data.version} is unsupported (supported range: 0 through {CurrentSaveVersion}).");
            }
        }

        // Reject unsupported catalog entries before destroying the current match.
        // Legacy saves continue through the existing roster adapters.
        internal static void ValidateCatalogEntries(MatchSaveData data)
        {
            foreach (UnitSaveData unit in data.units)
                ValidateCatalogEntry(unit.unitType, DefinitionKind.Unit, unit.faction, data);
            foreach (BuildingSaveData building in data.buildings)
            {
                ValidateCatalogEntry(building.buildingType, DefinitionKind.Building, building.faction, data);
                ValidateTrainingDefinition(building);
            }
        }

        private static void ValidateTrainingDefinition(BuildingSaveData building)
        {
            if (string.IsNullOrEmpty(building.trainingDefinitionId))
            {
                if (building.trainingRemaining >= 0f)
                {
                    throw new ArgumentException($"Building definition '{building.buildingType}' has a training countdown but no training definition ID.", nameof(building));
                }
                return;
            }

            EntityDefinition trainingDefinition = DefinitionCatalog.Default.Get(building.trainingDefinitionId);
            if (trainingDefinition.Kind != DefinitionKind.Unit)
            {
                throw new ArgumentException($"Training definition '{building.trainingDefinitionId}' is not a unit.", nameof(building));
            }

            if (DefinitionCatalog.Default.TryGet(building.buildingType, out EntityDefinition buildingDefinition)
                && !buildingDefinition.CanTrain(building.trainingDefinitionId))
            {
                throw new ArgumentException($"Building definition '{building.buildingType}' cannot train '{building.trainingDefinitionId}'.", nameof(building));
            }
        }

        internal static void ValidateRuntimeIds(MatchSaveData data)
        {
            var unitIds = new HashSet<int>();
            var buildingIds = new HashSet<int>();
            foreach (UnitSaveData unit in data.units)
            {
                if (unit.networkId >= 0 && !unitIds.Add(unit.networkId))
                    throw new ArgumentException($"Duplicate saved unit network ID '{unit.networkId}'.", nameof(data));
            }

            foreach (BuildingSaveData building in data.buildings)
            {
                if (building.networkId >= 0 && !buildingIds.Add(building.networkId))
                    throw new ArgumentException($"Duplicate saved building network ID '{building.networkId}'.", nameof(data));
            }
        }

        private static void ValidateCatalogEntry(string id, DefinitionKind kind, int faction, MatchSaveData data)
        {
            if (!EntitySpawner.IsDefinitionId(id)) return;
            EntitySpawner.ValidateDefinition(id, kind);
            EntityDefinition definition = DefinitionCatalog.Default.Get(id);
            FactionSaveData savedFaction = data.factions.Find(f => f.faction == faction);
            CivilizationId civilization = savedFaction == null
                ? CivilizationRegistry.For((FactionId)faction) : (CivilizationId)savedFaction.civilization;
            if (definition.Civilization.HasValue && definition.Civilization.Value != civilization)
                throw new ArgumentException($"Definition '{id}' requires civilization {definition.Civilization.Value}.");
        }

        private static void WipeCurrentMatch()
        {
            foreach (Unit unit in new List<Unit>(Unit.All))
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }

            foreach (Building building in new List<Building>(Building.All))
            {
                if (building != null)
                {
                    // Not a destruction refund: the snapshot restores its own
                    // resources, so a refund here would double-pay.
                    if (building.TryGetComponent(out TownCenter wipedTc)) wipedTc.DiscardQueue();
                    else if (building.TryGetComponent(out Barracks wipedBarracks)) wipedBarracks.DiscardQueue();
                    Destroy(building.gameObject);
                }
            }
        }

        private static void RestoreFactionState(FactionId faction, FactionSaveData factionData)
        {
            if (factionData == null)
            {
                return;
            }

            AgeProgress.Advance(faction, (AgeId)factionData.currentAge);

            AdvanceToTier(() => UpgradeProgress.AttackTier(faction), () => UpgradeProgress.AdvanceAttack(faction), factionData.attackTier);
            AdvanceToTier(() => UpgradeProgress.ArmorTier(faction), () => UpgradeProgress.AdvanceArmor(faction), factionData.armorTier);

            foreach (ClassTierEntry entry in factionData.classAttackTiers)
            {
                UnitClass unitClass = (UnitClass)entry.unitClass;
                AdvanceToTier(() => UpgradeProgress.ClassAttackTier(faction, unitClass), () => UpgradeProgress.AdvanceClassAttack(faction, unitClass), entry.tier);
            }

            foreach (ClassTierEntry entry in factionData.classArmorTiers)
            {
                UnitClass unitClass = (UnitClass)entry.unitClass;
                AdvanceToTier(() => UpgradeProgress.ClassArmorTier(faction, unitClass), () => UpgradeProgress.AdvanceClassArmor(faction, unitClass), entry.tier);
            }

            // DefinitionCatalog reference-entity tier ladders. -1 (any
            // save from before these fields existed) correctly advances
            // zero tiers via AdvanceToTier's own "while current < target"
            // loop condition - no separate legacy branch needed.
            AdvanceToTier(() => InfantryLineProgress.Tier(faction), () => InfantryLineProgress.AdvanceTier(faction), factionData.infantryTier);
            AdvanceToTier(() => ArcherLineProgress.Tier(faction), () => ArcherLineProgress.AdvanceTier(faction), factionData.archerTier);

            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            foreach (ResourceEntry entry in factionData.resources)
            {
                stockpile.SetTotal((ResourceType)entry.resourceType, entry.amount);
            }
        }

        // UpgradeProgress only exposes Advance*() (+1 per call), no direct
        // setter - looping to the saved tier keeps that API surface
        // untouched instead of adding parallel Set*() methods to a file
        // several other sessions are actively extending today.
        private static void AdvanceToTier(Func<int> current, Action advance, int targetTier)
        {
            while (current() < targetTier)
            {
                advance();
            }
        }

        // Phase 2 (entity creation), buildings half. Returns each saved
        // entry paired with whatever it actually spawned (null if the
        // spawn itself failed) so the later per-entity phases below have
        // something to apply state to, without re-scanning Building.All
        // and re-guessing which live object corresponds to which saved
        // entry.
        private static List<(BuildingSaveData saved, GameObject go)> RestoreBuildingEntities(List<BuildingSaveData> buildings)
        {
            var result = new List<(BuildingSaveData, GameObject)>(buildings.Count);
            foreach (BuildingSaveData saved in buildings)
            {
                FactionId faction = (FactionId)saved.faction;
                GameObject go = EntitySpawner.SpawnBuilding(saved.buildingType, faction, saved.position);

                if (go != null && saved.networkId >= 0 && go.TryGetComponent(out Building building))
                {
                    Multiplayer.NetworkId.Reassign(building, saved.networkId);
                }

                result.Add((saved, go));
            }

            return result;
        }

        private static List<(UnitSaveData saved, GameObject go)> RestoreUnitEntities(List<UnitSaveData> units)
        {
            var result = new List<(UnitSaveData, GameObject)>(units.Count);
            foreach (UnitSaveData saved in units)
            {
                FactionId faction = (FactionId)saved.faction;
                GameObject go = EntitySpawner.SpawnUnit(saved.unitType, faction, saved.position);

                if (go != null && saved.networkId >= 0 && go.TryGetComponent(out Unit unit))
                {
                    Multiplayer.NetworkId.Reassign(unit, saved.networkId);
                }

                result.Add((saved, go));
            }

            return result;
        }

        // Phase 3 (health/construction state), buildings half. Prefers the
        // exact constructionProgress fraction (this ticket's own addition)
        // when present; falls back to isComplete's own coarser complete-
        // or-freshly-placed behavior for any save from before that field
        // existed, unchanged from what this method always did.
        private static void RestoreBuildingHealthAndConstruction(List<(BuildingSaveData saved, GameObject go)> restored)
        {
            foreach ((BuildingSaveData saved, GameObject go) in restored)
            {
                if (go == null)
                {
                    continue;
                }

                if (go.TryGetComponent(out ConstructionSite site))
                {
                    float progress = saved.constructionProgress >= 0f
                        ? saved.constructionProgress
                        : (saved.isComplete ? 1f : 0f);
                    site.RestoreProgress(progress);
                }

                if (go.TryGetComponent(out Attackable attackable))
                {
                    attackable.RestoreHealth(saved.health);
                }
            }
        }

        private static void RestoreUnitHealthAndStance(List<(UnitSaveData saved, GameObject go)> restored)
        {
            foreach ((UnitSaveData saved, GameObject go) in restored)
            {
                if (go == null)
                {
                    continue;
                }

                if (go.TryGetComponent(out Attackable attackable))
                {
                    attackable.RestoreHealth(saved.health);
                }

                if (saved.stance >= 0 && go.TryGetComponent(out StanceController stance))
                {
                    stance.SetStance((UnitStance)saved.stance);
                }
            }
        }

        // Phase 5 (production/research queues) - reference-catalog scope
        // only, see BuildingSaveData's own field comments. Null/-1 sentinels
        // (nothing queued, or a save from before these fields existed)
        // correctly restore nothing, matching this class's own previously-
        // documented "in-progress construction/training/research countdowns
        // aren't restored" limitation for exactly that case.
        private static void RestoreProductionQueues(List<(BuildingSaveData saved, GameObject go)> restored)
        {
            foreach ((BuildingSaveData saved, GameObject go) in restored)
            {
                if (go == null)
                {
                    continue;
                }

                if (saved.productionQueue != null && saved.productionQueue.Count > 0)
                {
                    var items = new List<ProductionItem>();
                    foreach (ProductionItemSaveData entry in saved.productionQueue)
                    {
                        var types = new ResourceType[entry.costTypes.Length];
                        for (int i = 0; i < types.Length; i++)
                        {
                            types[i] = (ResourceType)entry.costTypes[i];
                        }

                        items.Add(new ProductionItem
                        {
                            Kind = entry.kind,
                            Label = entry.label,
                            CostTypes = types,
                            CostAmounts = entry.costAmounts,
                            Total = entry.total,
                            Remaining = entry.remaining,
                        });
                    }

                    if (go.TryGetComponent(out TownCenter queuedTownCenter))
                    {
                        queuedTownCenter.RestoreQueue(items);
                    }
                    else if (go.TryGetComponent(out Barracks queuedBarracks))
                    {
                        queuedBarracks.RestoreQueue(items);
                    }
                }
                else if (saved.trainingRemaining >= 0f && saved.trainingDefinitionId != null)
                {
                    if (saved.trainingDefinitionId == DefinitionCatalog.Worker && go.TryGetComponent(out TownCenter townCenter))
                    {
                        townCenter.RestoreTraining(saved.trainingRemaining);
                    }
                    else if (go.TryGetComponent(out Barracks barracksForTraining))
                    {
                        barracksForTraining.RestoreTraining(saved.trainingDefinitionId, saved.trainingRemaining);
                    }
                }

                if (saved.infantryTierResearchRemaining >= 0f && go.TryGetComponent(out Barracks barracksForResearch))
                {
                    barracksForResearch.RestoreInfantryTierResearch(saved.infantryTierResearchRemaining);
                }
            }
        }
    }
}

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
            var data = new MatchSaveData { mapId = (int)MapRegistry.CurrentId };

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

                data.units.Add(new UnitSaveData
                {
                    unitType = unitType,
                    faction = (int)factionMember.Faction,
                    position = unit.transform.position,
                    health = attackable != null ? attackable.Health : 0f,
                    stance = stance != null ? (int)stance.Stance : -1,
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

                data.buildings.Add(new BuildingSaveData
                {
                    buildingType = buildingType,
                    faction = (int)factionMember.Faction,
                    position = GroundPointFor(buildingType, building),
                    health = attackable != null ? attackable.Health : 0f,
                    isComplete = site == null || site.IsComplete,
                });
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
            if (buildingType == "TownCenter" || !building.TryGetComponent(out BoxCollider box))
            {
                return position;
            }

            position.y -= box.size.y * 0.5f;
            return position;
        }

        private static string IdentifyBuildingType(Building building)
        {
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
            FactionSaveData playerData = data.factions.Find(f => f.faction == (int)FactionId.Player);
            FactionSaveData enemyData = data.factions.Find(f => f.faction == (int)FactionId.Enemy);
            FactionSaveData enemy2Data = data.factions.Find(f => f.faction == (int)FactionId.Enemy2);
            bool enemy2InPlay = data.units.Exists(u => u.faction == (int)FactionId.Enemy2)
                || data.buildings.Exists(b => b.faction == (int)FactionId.Enemy2);

            WipeCurrentMatch();
            RestoreFactionState(FactionId.Player, playerData);
            RestoreFactionState(FactionId.Enemy, enemyData);
            if (enemy2InPlay)
            {
                RestoreFactionState(FactionId.Enemy2, enemy2Data);
            }
            RestoreBuildings(data.buildings);
            RestoreUnits(data.units);
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

        private static void RestoreBuildings(List<BuildingSaveData> buildings)
        {
            foreach (BuildingSaveData saved in buildings)
            {
                FactionId faction = (FactionId)saved.faction;
                GameObject go = EntitySpawner.SpawnBuilding(saved.buildingType, faction, saved.position);

                if (go == null)
                {
                    continue;
                }

                if (saved.isComplete && go.TryGetComponent(out ConstructionSite site))
                {
                    site.CompleteImmediately();
                }

                if (go.TryGetComponent(out Attackable attackable))
                {
                    attackable.RestoreHealth(saved.health);
                }
            }
        }

        private static void RestoreUnits(List<UnitSaveData> units)
        {
            foreach (UnitSaveData saved in units)
            {
                FactionId faction = (FactionId)saved.faction;
                GameObject go = EntitySpawner.SpawnUnit(saved.unitType, faction, saved.position);

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
    }
}

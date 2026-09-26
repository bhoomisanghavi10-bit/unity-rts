using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.AI;

namespace KingdomsOfBharat.Core
{
    // Assigns which civilization each faction plays as, seeds each
    // faction's starting Age, then releases the actual match content -
    // called once by CivPicker after the player confirms a choice, not at
    // scene load. Every spawner (WorkerFactory, SoldierFactory,
    // TownCenterFactory, BarracksFactory, FarmFactory, AiController, ...)
    // reads CivilizationRegistry.For()/AgeProgress.CurrentAge() at spawn
    // time and bakes the result in permanently, so the player's choice has
    // to be known before any of them run - not just before the player can
    // see the result. The gated GameObjects (wired in the Inspector) start
    // inactive in the scene for exactly this reason: their Awake()/Start()
    // must not fire until BeginMatch activates them here, in this order,
    // after the civ/age state above is already in place.
    public class CivilizationSetup : MonoBehaviour
    {
        [SerializeField] private CivilizationId aiCivilization = CivilizationId.Vijayanagara;
        [SerializeField] private MapId map = MapId.RiverValley;
        // Item 48: off by default so an existing match/scene behaves
        // exactly as before - Inspector-only for now, same "no UI yet"
        // state civ/difficulty/map choices were all in before their own
        // pickers existed. The actual second AiController GameObject
        // still has to exist in the scene (gated, like the first) for
        // this to do anything - this flag alone doesn't spawn one.
        [SerializeField] private bool enableThirdFaction;
        [SerializeField] private CivilizationId enemy2Civilization = CivilizationId.Rajput;
        // Separate from gatedMatchContent (which always activates) since
        // this GameObject (the 2nd AiController) should only exist when
        // enableThirdFaction is actually on.
        [SerializeField] private GameObject[] enemy2GatedContent;
        [SerializeField] private GameObject[] gatedMatchContent;

        // MatchManager reads this so it never evaluates victory/defeat
        // (both factions read as "eliminated" with zero units/buildings)
        // during the CivPicker overlay, before any match content exists.
        public static bool HasMatchStarted { get; private set; }

        private void OnDestroy()
        {
            HasMatchStarted = false;
            MatchConfiguration.End();
            ScenarioManager.EndScenario();
            CustomScenarioContext.End();
            Multiplayer.NetworkMatch.End();
        }

        public void BeginMatch(CivilizationId playerCivilization)
        {
            BeginMatchCore(MatchConfiguration.Create(map, playerCivilization, aiCivilization, enableThirdFaction, enemy2Civilization, _seedOverride));
        }

        // The authoritative entry point: every other Begin* below builds a
        // MatchConfiguration (the "adapter") and lands here.
        public void BeginMatch(MatchConfiguration configuration)
        {
            BeginMatchCore(configuration);
        }

        // Optional fixed seed for the next BeginMatch(CivilizationId) (tests,
        // replays, "same map again"). -1 = pick one fresh per match. Consumed
        // by that next match only - it does not persist across rematches.
        private int _seedOverride = -1;

        public void SetSeed(int seed)
        {
            _seedOverride = seed;
        }

        // CivPicker's map row (or any other pre-match UI) calls this before
        // BeginMatch/BeginMatchAsHost/etc. to override the Inspector's
        // default map choice. A no-op if nothing ever calls it, so every
        // existing scene/test that never touches this keeps picking
        // whatever `map` was set to in the Inspector, exactly as before.
        public void SetMap(MapId id)
        {
            map = id;
        }

        // Phase 5 LAN transport MVP: a 2-human match needs the "Enemy" slot
        // to be the other real player's chosen civilization, not this
        // component's Inspector-configured aiCivilization default (which
        // means "the AI's civ" in every other flow) - same
        // sourced-elsewhere-than-the-Inspector pattern BeginScenarioMatch
        // already established for scripted missions. LanMatchMenu calls
        // this once both sides' civ picks and the host's chosen seed have
        // been exchanged over the wire, instead of calling BeginMatch.
        public void BeginNetworkMatch(CivilizationId hostCivilization, CivilizationId remoteCivilization, MapId networkMap)
        {
            BeginMatchCore(MatchConfiguration.Create(networkMap, hostCivilization, remoteCivilization,
                secondSlotIsHuman: true));
        }

        // Item 50: same match-start pipeline as BeginMatch, but sourcing
        // civ/map from a scripted mission instead of this component's own
        // Inspector defaults, and registering the mission's objectives/
        // triggers first so they're already in place before any gated
        // content's Awake()/Start() runs.
        public void BeginScenarioMatch(ScenarioDefinition scenario)
        {
            ScenarioManager.Begin(scenario);
            BeginMatchCore(MatchConfiguration.Create(scenario.Map, scenario.PlayerCivilization, scenario.AiCivilization, enableThirdFaction, enemy2Civilization, _seedOverride));
        }

        // Item 6 (Scenario Editor, heavy path session 1): a player-authored
        // custom scenario. CustomScenarioContext.Begin() runs BEFORE
        // BeginMatchCore so TownCenterSpawner/AiController (gated content
        // BeginMatchCore activates) can see it in their own Awake()/Start()
        // and skip their own hardcoded default spawn when this scenario
        // already supplies their faction's placements. The actual
        // placements are spawned AFTER BeginMatchCore returns (ground/
        // NavMesh already rebuilt by then, civ/age registries already
        // populated).
        //
        // Heavy path session 2: if the scenario carries authored
        // objectives (data.objectives.Count > 0), also call
        // ScenarioManager.Begin, same ordering BeginScenarioMatch already
        // uses (before BeginMatchCore, so a DestroyScriptedTarget
        // objective's spawned target exists before gated content
        // activates). Deliberately gated on a non-empty objective list, not
        // called unconditionally: ScenarioManager.EvaluateOutcome() treats
        // zero objectives as "every objective complete" (an empty foreach
        // never reaches its Ongoing branch), so calling Begin with an empty
        // list would make every placements-only scenario (session 1's
        // entire feature set) resolve to an instant Victory on the very
        // first MatchManager.Evaluate() tick instead of falling through to
        // its existing elimination-based Conquest evaluation.
        public void BeginCustomScenarioMatch(CustomScenarioData data)
        {
            if (data.objectives.Count > 0)
            {
                var scenario = new ScenarioDefinition
                {
                    Id = data.id,
                    Title = data.title,
                    PlayerCivilization = (CivilizationId)data.playerCivilization,
                    AiCivilization = (CivilizationId)data.aiCivilization,
                    Map = (MapId)data.mapId,
                    VictoryText = string.IsNullOrEmpty(data.victoryText) ? null : data.victoryText,
                    DefeatText = string.IsNullOrEmpty(data.defeatText) ? null : data.defeatText,
                    BuildObjectives = () => MissionCsvLoader.BuildObjectivesFromRows(data.objectives),
                    BuildTriggers = () => MissionCsvLoader.BuildTriggersFromRows(data.triggers),
                };
                ScenarioManager.Begin(scenario);
            }

            CustomScenarioContext.Begin(data);
            BeginMatchCore(MatchConfiguration.Create((MapId)data.mapId, (CivilizationId)data.playerCivilization, (CivilizationId)data.aiCivilization, enableThirdFaction, enemy2Civilization, _seedOverride));
            SpawnPlacements(data);
        }

        // Separated from BeginCustomScenarioMatch for clarity - spawns every
        // placement via EntitySpawner (the same dispatch SaveManager's own
        // restore path uses) and immediately completes any building's
        // ConstructionSite, matching how a scenario's starting base is
        // already-built, not a fresh foundation.
        private static void SpawnPlacements(CustomScenarioData data)
        {
            foreach (BuildingSaveData building in data.buildings)
            {
                FactionId faction = (FactionId)building.faction;
                GameObject go = EntitySpawner.SpawnBuilding(building.buildingType, faction, building.position);
                if (go != null && go.TryGetComponent(out ConstructionSite site))
                {
                    site.CompleteImmediately();
                }
            }

            foreach (UnitSaveData unit in data.units)
            {
                FactionId faction = (FactionId)unit.faction;
                EntitySpawner.SpawnUnit(unit.unitType, faction, unit.position);
            }

            // Every AiController whose faction this scenario placed a
            // TownCenter for needs to adopt it - it skipped its own spawn
            // in Start() (see AiController.AdoptTownCenter), so without this
            // it would have no _townCenter reference at all.
            foreach (AiController ai in FindObjectsByType<AiController>(FindObjectsSortMode.None))
            {
                ai.AdoptTownCenter();
            }
        }

        private void BeginMatchCore(MatchConfiguration config)
        {
            _seedOverride = -1; // a fixed seed applies to one match only

            // Replaces (never merges with) whatever the previous match left,
            // before any system that reads it starts.
            MatchConfiguration.Begin(config);

            // Phase 5 LAN transport MVP: must run before any gated spawner
            // below activates - a match's initial units/buildings spawn
            // synchronously during that activation, before SimClock even
            // exists as a running tick stream, so NetworkId has to start
            // counting from zero here, not from SimClock's own match-start
            // point (which fires a frame later - see NetworkId.Reset's own
            // comment).
            Multiplayer.NetworkId.Reset();

            MapRegistry.Select(config.Map);

            // Phase 5 gap-close: ProceduralGround/NavMeshBaker are always-
            // active from scene load, same as RTSCameraController/
            // FogOfWarManager/MinimapController were before their own
            // Phase 5 fix - their Awake()/Start() already ran against
            // whatever MapRegistry.Current was at scene load (the
            // RiverValley default), before this method's MapRegistry.
            // Select() above ever ran. Rebuilding both here, synchronously
            // and in this order (ground geometry before the NavMesh bake
            // that reads its collider), is what actually makes a picked
            // map's real size/shape show up - gated content below
            // (TownCenterSpawner, ResourceNodeSpawner, UnitSpawner,
            // AiController) all assume both are already correct by the
            // time they activate.
            // ProceduralTerrain replaced ProceduralGround (terrain migration,
            // docs/MAP_VISUAL_UPGRADE_PLAN.md) - same Rebuild() contract.
            ProceduralTerrain ground = FindFirstObjectByType<ProceduralTerrain>();
            if (ground != null)
            {
                ground.Rebuild();
            }

            NavMeshBaker navMeshBaker = FindFirstObjectByType<NavMeshBaker>();
            if (navMeshBaker != null)
            {
                navMeshBaker.RebuildNavMesh();
            }

            DiplomacyRegistry.Reset();
            TeamColorBuildingTint.Reset();
            TeamColorUnitTint.Reset();
            // Repository-audit finding F08: every per-faction progression
            // registry (Score/Upgrade/UniqueTech/EconomyTech/Hero/
            // UniqueUnitElite/the 13 unit-tier ladders) is a static
            // dictionary that otherwise survives into the next match
            // started in this same process. See ProgressionRegistry's own
            // header comment.
            ProgressionRegistry.ResetAllForNewMatch();

            // Civilization/age per playable slot. Civ resolution (the player's
            // pick is authoritative, others resolve around it) already
            // happened in MatchConfiguration.Create.
            foreach (MatchSlot slot in config.Slots)
            {
                if (slot.Type == SlotType.Closed)
                {
                    continue;
                }

                CivilizationRegistry.Assign(slot.Faction, slot.Civilization);
                AgeProgress.Initialize(slot.Faction, StartingAgeFor(slot.Civilization));
            }

            // Slots on the same team start allied (every default slot has its
            // own team, i.e. the previous all-vs-all behaviour).
            for (int i = 0; i < config.Slots.Count; i++)
            {
                for (int j = i + 1; j < config.Slots.Count; j++)
                {
                    MatchSlot a1 = config.Slots[i];
                    MatchSlot b1 = config.Slots[j];
                    if (a1.Type != SlotType.Closed && b1.Type != SlotType.Closed && a1.Team == b1.Team)
                    {
                        DiplomacyRegistry.SetAllied(a1.Faction, b1.Faction, true);
                    }
                }
            }

            if (config.StartingResources.Override)
            {
                foreach (MatchSlot slot in config.Slots)
                {
                    if (slot.Type == SlotType.Closed)
                    {
                        continue;
                    }

                    ResourceGathering.ResourceStockpile stockpile = ResourceGathering.ResourceStockpile.For(slot.Faction);
                    if (stockpile != null)
                    {
                        stockpile.SetTotal(ResourceGathering.ResourceType.Food, config.StartingResources.Food);
                        stockpile.SetTotal(ResourceGathering.ResourceType.Wood, config.StartingResources.Wood);
                        stockpile.SetTotal(ResourceGathering.ResourceType.Gold, config.StartingResources.Gold);
                        stockpile.SetTotal(ResourceGathering.ResourceType.Stone, config.StartingResources.Stone);
                    }
                }
            }

            // Starting forces are spawned here for every playable slot, human
            // or AI alike, at the map's start for that slot - not by the
            // AiController/TownCenterSpawner/UnitSpawner that used to each do
            // it differently.
            StartingForces.SpawnAll(config);

            if (config.IsAi(FactionId.Enemy2) && enemy2GatedContent != null)
            {
                foreach (GameObject content in enemy2GatedContent)
                {
                    content.SetActive(true);
                }
            }

            foreach (GameObject content in gatedMatchContent)
            {
                content.SetActive(true);
            }

            HasMatchStarted = true;

            // Repository-audit reproduction: nothing previously moved the
            // camera at match start, so it stayed wherever the scene/
            // CivPicker screen last left it - which can be nowhere near the
            // actual starting base, reading as an almost entirely black
            // Game view once fog is enabled. Called last, once the map is
            // selected (MapRegistry.Current above) and every faction is
            // resolved - Multiplayer.NetworkMatch.LocalFaction is Player
            // for every current single-player/AI-opponent flow (its own
            // default) and only differs for a real 2-human LAN match, so
            // this focuses whichever faction's start this process is
            // actually meant to be looking at, not always literally
            // FactionId.Player.
            KingdomsOfBharat.Camera.RTSCameraController mainCamera = FindFirstObjectByType<KingdomsOfBharat.Camera.RTSCameraController>();
            if (mainCamera != null)
            {
                mainCamera.FocusOnMatchStart(ResolveLocalPlayerStart(config.LocalFaction, MapRegistry.Current));
            }
        }

        // Extracted from BeginMatchCore, same reason ResolveDistinctCivilization
        // is: a MonoBehaviour method can't be unit-tested directly, but this
        // decision - which map-defined start belongs to whichever faction
        // this process is actually meant to be looking at - can be, in
        // isolation from FindFirstObjectByType/the live scene. Only Player/
        // Enemy are distinguished (matching NetworkMatch.LocalFaction's own
        // documented 2-human-LAN-only scope - it never reports Enemy2).
        internal static Vector3 ResolveLocalPlayerStart(FactionId localFaction, MapDefinitionData map)
        {
            return localFaction == FactionId.Enemy ? map.EnemyTownCenter : map.PlayerTownCenter;
        }

        // Phase 6 gap-close: Maurya's "starts the match already in the
        // Classical Age" bonus - not representable as a passiveBonuses
        // StatModifier (it's not a stat multiplier at all), so this is a
        // hand-picked civ check, same shape as BuildingPlacer's
        // WoodMultiplierFor/StoneMultiplierFor.
        private static AgeId StartingAgeFor(CivilizationId civ)
        {
            return civ == CivilizationId.Maurya ? AgeId.Classical : AgeId.Ancient;
        }

        // Falls back deterministically to the first CivilizationId (enum
        // declaration order) not already in alreadyTaken, so results are
        // reproducible/testable rather than randomized. Returns desired
        // unchanged if every civ is somehow already taken (impossible
        // today with 5 civs and 3 fixed factions).
        internal static CivilizationId ResolveDistinctCivilization(CivilizationId desired, ICollection<CivilizationId> alreadyTaken)
        {
            if (!alreadyTaken.Contains(desired))
            {
                return desired;
            }

            foreach (CivilizationId candidate in (CivilizationId[])Enum.GetValues(typeof(CivilizationId)))
            {
                if (!alreadyTaken.Contains(candidate))
                {
                    return candidate;
                }
            }

            return desired;
        }
    }
}

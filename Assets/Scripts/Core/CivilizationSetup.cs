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
            ScenarioManager.EndScenario();
            CustomScenarioContext.End();
            Multiplayer.NetworkMatch.End();
        }

        public void BeginMatch(CivilizationId playerCivilization)
        {
            BeginMatchCore(playerCivilization, aiCivilization, map);
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
            BeginMatchCore(hostCivilization, remoteCivilization, networkMap);
        }

        // Item 50: same match-start pipeline as BeginMatch, but sourcing
        // civ/map from a scripted mission instead of this component's own
        // Inspector defaults, and registering the mission's objectives/
        // triggers first so they're already in place before any gated
        // content's Awake()/Start() runs.
        public void BeginScenarioMatch(ScenarioDefinition scenario)
        {
            ScenarioManager.Begin(scenario);
            BeginMatchCore(scenario.PlayerCivilization, scenario.AiCivilization, scenario.Map);
        }

        // Item 6 (Scenario Editor, heavy path session 1): a player-authored
        // custom scenario. Deliberately does NOT call ScenarioManager.Begin
        // - v1 has no custom objectives/triggers, so ScenarioManager.
        // ActiveScenario stays null and MatchManager's existing elimination-
        // based Conquest evaluation just runs, same as a normal skirmish.
        // CustomScenarioContext.Begin() runs BEFORE BeginMatchCore so
        // TownCenterSpawner/AiController (gated content BeginMatchCore
        // activates) can see it in their own Awake()/Start() and skip their
        // own hardcoded default spawn when this scenario already supplies
        // their faction's placements. The actual placements are spawned
        // AFTER BeginMatchCore returns (ground/NavMesh already rebuilt by
        // then, civ/age registries already populated).
        public void BeginCustomScenarioMatch(CustomScenarioData data)
        {
            CustomScenarioContext.Begin(data);
            BeginMatchCore((CivilizationId)data.playerCivilization, (CivilizationId)data.aiCivilization, (MapId)data.mapId);
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

        private void BeginMatchCore(CivilizationId playerCivilization, CivilizationId aiCiv, MapId mapId)
        {
            // Phase 5 LAN transport MVP: must run before any gated spawner
            // below activates - a match's initial units/buildings spawn
            // synchronously during that activation, before SimClock even
            // exists as a running tick stream, so NetworkId has to start
            // counting from zero here, not from SimClock's own match-start
            // point (which fires a frame later - see NetworkId.Reset's own
            // comment).
            Multiplayer.NetworkId.Reset();

            MapRegistry.Select(mapId);

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
            ProceduralGround ground = FindFirstObjectByType<ProceduralGround>();
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

            // AoE-parity gap-close: aiCiv/enemy2Civilization are fixed
            // Inspector defaults, not player-aware - nothing previously
            // stopped the player's own CivPicker choice from colliding
            // with one of them (e.g. picking Vijayanagara against the
            // default aiCivilization=Vijayanagara), which today makes two
            // factions render as literally the same civ, since civ
            // identity is the only body tint that exists
            // (HumanModelFactory.PaletteNameFor). The player's pick is
            // authoritative and never rerolled; AI factions resolve
            // deterministically around it and each other.
            var takenCivilizations = new HashSet<CivilizationId> { playerCivilization };
            CivilizationId resolvedAiCiv = ResolveDistinctCivilization(aiCiv, takenCivilizations);
            takenCivilizations.Add(resolvedAiCiv);

            CivilizationRegistry.Assign(FactionId.Player, playerCivilization);
            CivilizationRegistry.Assign(FactionId.Enemy, resolvedAiCiv);

            AgeProgress.Initialize(FactionId.Player, StartingAgeFor(playerCivilization));
            AgeProgress.Initialize(FactionId.Enemy, StartingAgeFor(resolvedAiCiv));

            // Item 48: only touches Enemy2's registries when the 3rd
            // faction is actually on - an untouched CivilizationRegistry/
            // AgeProgress entry for Enemy2 is harmless (nothing reads it
            // unless a 2nd AiController actually spawns and asks), but
            // initializing it unconditionally would be pointless work for
            // the common 2-faction case.
            if (enableThirdFaction)
            {
                CivilizationId resolvedEnemy2Civ = ResolveDistinctCivilization(enemy2Civilization, takenCivilizations);
                takenCivilizations.Add(resolvedEnemy2Civ);

                CivilizationRegistry.Assign(FactionId.Enemy2, resolvedEnemy2Civ);
                AgeProgress.Initialize(FactionId.Enemy2, StartingAgeFor(resolvedEnemy2Civ));

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

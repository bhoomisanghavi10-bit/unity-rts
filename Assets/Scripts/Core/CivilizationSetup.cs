using UnityEngine;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Match;

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
        }

        public void BeginMatch(CivilizationId playerCivilization)
        {
            BeginMatchCore(playerCivilization, aiCivilization, map);
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

        private void BeginMatchCore(CivilizationId playerCivilization, CivilizationId aiCiv, MapId mapId)
        {
            MapRegistry.Select(mapId);
            DiplomacyRegistry.Reset();

            CivilizationRegistry.Assign(FactionId.Player, playerCivilization);
            CivilizationRegistry.Assign(FactionId.Enemy, aiCiv);

            AgeProgress.Initialize(FactionId.Player);
            AgeProgress.Initialize(FactionId.Enemy);

            // Item 48: only touches Enemy2's registries when the 3rd
            // faction is actually on - an untouched CivilizationRegistry/
            // AgeProgress entry for Enemy2 is harmless (nothing reads it
            // unless a 2nd AiController actually spawns and asks), but
            // initializing it unconditionally would be pointless work for
            // the common 2-faction case.
            if (enableThirdFaction)
            {
                CivilizationRegistry.Assign(FactionId.Enemy2, enemy2Civilization);
                AgeProgress.Initialize(FactionId.Enemy2);

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
    }
}

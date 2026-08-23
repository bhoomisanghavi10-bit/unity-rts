using UnityEngine;
using KingdomsOfBharat.Progression;

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
        [SerializeField] private GameObject[] gatedMatchContent;

        // MatchManager reads this so it never evaluates victory/defeat
        // (both factions read as "eliminated" with zero units/buildings)
        // during the CivPicker overlay, before any match content exists.
        public static bool HasMatchStarted { get; private set; }

        private void OnDestroy()
        {
            HasMatchStarted = false;
        }

        public void BeginMatch(CivilizationId playerCivilization)
        {
            CivilizationRegistry.Assign(FactionId.Player, playerCivilization);
            CivilizationRegistry.Assign(FactionId.Enemy, aiCivilization);

            AgeProgress.Initialize(FactionId.Player);
            AgeProgress.Initialize(FactionId.Enemy);

            foreach (GameObject content in gatedMatchContent)
            {
                content.SetActive(true);
            }

            HasMatchStarted = true;
        }
    }
}

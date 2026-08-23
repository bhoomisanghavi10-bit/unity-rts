using UnityEngine;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Core
{
    // Assigns which civilization each faction plays as, and seeds each
    // faction's starting Age. No civ-picker UI yet (future work, needs a
    // real menu) - set via Inspector for now. Runs before anything else in
    // the scene (see execution order): every spawner (WorkerFactory,
    // SoldierFactory, TownCenterFactory, BarracksFactory, FarmFactory)
    // reads CivilizationRegistry.For()/AgeProgress.CurrentAge() at spawn
    // time, so both must exist before the very first Awake()/Start() that
    // spawns something runs.
    [DefaultExecutionOrder(-100)]
    public class CivilizationSetup : MonoBehaviour
    {
        [SerializeField] private CivilizationId playerCivilization = CivilizationId.Chola;
        [SerializeField] private CivilizationId aiCivilization = CivilizationId.Vijayanagara;

        private void Awake()
        {
            CivilizationRegistry.Assign(FactionId.Player, playerCivilization);
            CivilizationRegistry.Assign(FactionId.Enemy, aiCivilization);

            AgeProgress.Initialize(FactionId.Player);
            AgeProgress.Initialize(FactionId.Enemy);
        }
    }
}

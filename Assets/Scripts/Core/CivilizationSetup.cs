using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Assigns which civilization each faction plays as. No civ-picker UI
    // yet (future work, needs a real menu) - set via Inspector for now.
    // Runs before anything else in the scene (see execution order): every
    // spawner (WorkerFactory, SoldierFactory, TownCenterFactory,
    // BarracksFactory, FarmFactory) reads CivilizationRegistry.For() at
    // spawn time, so the assignment must exist before the very first
    // Awake()/Start() that spawns something runs.
    [DefaultExecutionOrder(-100)]
    public class CivilizationSetup : MonoBehaviour
    {
        [SerializeField] private CivilizationId playerCivilization = CivilizationId.Chola;
        [SerializeField] private CivilizationId aiCivilization = CivilizationId.Vijayanagara;

        private void Awake()
        {
            CivilizationRegistry.Assign(FactionId.Player, playerCivilization);
            CivilizationRegistry.Assign(FactionId.Enemy, aiCivilization);
        }
    }
}

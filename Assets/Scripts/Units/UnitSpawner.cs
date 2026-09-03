using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // Drops the Player's starting Worker units on the map. Actual
    // GameObject creation is WorkerFactory's job (shared with
    // AiController's own workers).
    public class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private int unitCount = 4;
        [SerializeField] private float spacing = 2f;

        private void Start()
        {
            // Item 6 (Scenario Editor, heavy path session 1): a custom
            // scenario that already placed Player units supplies its own
            // starting workers - skip this default spawn entirely. Found
            // live (not assumed): TownCenterSpawner/AiController were the
            // only 2 gated spawners this item's own plan accounted for,
            // but CivilizationSetup's own comment lists a 3rd
            // (UnitSpawner) that was still unconditionally adding 4
            // default Workers on top of a scenario's own placements until
            // this fix - caught via a live Population.Current mismatch
            // during verification, not left unnoticed.
            if (CustomScenarioContext.HasPlacementsFor(FactionId.Player))
            {
                return;
            }

            for (int i = 0; i < unitCount; i++)
            {
                float x = i * spacing - (unitCount - 1) * spacing * 0.5f;
                WorkerFactory.Spawn(new Vector3(x, 1f, 0f), FactionId.Player);
            }
        }
    }
}

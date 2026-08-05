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
            for (int i = 0; i < unitCount; i++)
            {
                float x = i * spacing - (unitCount - 1) * spacing * 0.5f;
                WorkerFactory.Spawn(new Vector3(x, 1f, 0f), FactionId.Player);
            }
        }
    }
}

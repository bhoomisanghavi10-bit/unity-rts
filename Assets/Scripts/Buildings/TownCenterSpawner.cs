using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Places the Player's fixed starting Town Center. Actual GameObject
    // creation is TownCenterFactory's job (shared with AiController's own
    // base).
    public class TownCenterSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 position = new Vector3(0f, 1f, 8f);

        private void Awake()
        {
            TownCenterFactory.Place(position, FactionId.Player);
        }
    }
}

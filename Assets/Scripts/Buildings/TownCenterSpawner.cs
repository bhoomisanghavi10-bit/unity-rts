using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Places the Player's starting Town Center. Actual GameObject creation
    // is TownCenterFactory's job (shared with AiController's own base).
    public class TownCenterSpawner : MonoBehaviour
    {
        private void Awake()
        {
            // Item 44: the selected map picks the starting position;
            // RiverValley's matches this field's own default exactly.
            TownCenterFactory.Place(MapRegistry.Current.PlayerTownCenter, FactionId.Player);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    // Single-player resource bank. Gatherer writes to it on every drop-off;
    // the UI (milestone 7) reads GetTotal to render counters.
    public class ResourceStockpile : MonoBehaviour
    {
        public static ResourceStockpile Instance { get; private set; }

        private readonly Dictionary<ResourceType, float> _totals = new Dictionary<ResourceType, float>
        {
            { ResourceType.Food, 0f },
            { ResourceType.Wood, 0f },
        };

        private void Awake()
        {
            Instance = this;
        }

        public float GetTotal(ResourceType type)
        {
            return _totals[type];
        }

        public void Add(ResourceType type, float amount)
        {
            _totals[type] += amount;
        }
    }
}

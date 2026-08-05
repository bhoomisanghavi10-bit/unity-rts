using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    // Single-player resource bank. Gatherer writes to it on every drop-off;
    // the UI (milestone 7) reads GetTotal to render counters.
    public class ResourceStockpile : MonoBehaviour
    {
        // Self-healing: if Enter Play Mode's "Reload Domain" is off, this
        // static field can go stale across Play/Stop cycles instead of
        // being reset. Falling back to a scene lookup means a stale
        // reference can't crash Deposit() with a NullReferenceException.
        private static ResourceStockpile _instance;

        public static ResourceStockpile Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ResourceStockpile>();
                }
                return _instance;
            }
        }

        private readonly Dictionary<ResourceType, float> _totals = new Dictionary<ResourceType, float>
        {
            { ResourceType.Food, 0f },
            { ResourceType.Wood, 0f },
        };

        private void Awake()
        {
            _instance = this;
        }

        public float GetTotal(ResourceType type)
        {
            return _totals[type];
        }

        public void Add(ResourceType type, float amount)
        {
            _totals[type] += amount;
        }

        // Temporary on-screen readout until milestone 7 adds a real HUD.
        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 200, 20), $"Wood: {(int)GetTotal(ResourceType.Wood)}");
            GUI.Label(new Rect(10, 30, 200, 20), $"Food: {(int)GetTotal(ResourceType.Food)}");
        }
    }
}

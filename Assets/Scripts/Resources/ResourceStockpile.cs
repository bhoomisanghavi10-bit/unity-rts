using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.ResourceGathering
{
    // Per-faction resource bank - one instance per FactionId in the scene.
    // Gatherer/Barracks write to whichever faction they belong to via
    // For(faction); ResourceHUD (UI) always reads the Player's.
    public class ResourceStockpile : MonoBehaviour
    {
        [SerializeField] private FactionId faction = FactionId.Player;

        // Every real instance is scene-authored via the Inspector - this
        // only exists so tests can create one programmatically (e.g.
        // DesyncRecoveryTests, which needs a real per-faction stockpile for
        // SaveManager.Capture() to find via For()).
        public void Configure(FactionId newFaction)
        {
            faction = newFaction;
        }

        // Self-healing per faction: if Enter Play Mode's "Reload Domain" is
        // off, a cached entry can go stale across Play/Stop cycles instead
        // of resetting. A scene lookup per faction (not one shared cache
        // that could itself go stale) means a stale reference can't crash
        // Deposit()/RequestTrain() with a NullReferenceException.
        private static readonly Dictionary<FactionId, ResourceStockpile> _instances =
            new Dictionary<FactionId, ResourceStockpile>();

        public static ResourceStockpile For(FactionId faction)
        {
            if (_instances.TryGetValue(faction, out ResourceStockpile existing) && existing != null)
            {
                return existing;
            }

            foreach (ResourceStockpile stockpile in FindObjectsByType<ResourceStockpile>(FindObjectsSortMode.None))
            {
                if (stockpile.faction == faction)
                {
                    _instances[faction] = stockpile;
                    return stockpile;
                }
            }

            return null;
        }

        private readonly Dictionary<ResourceType, float> _totals = new Dictionary<ResourceType, float>
        {
            { ResourceType.Food, 0f },
            { ResourceType.Wood, 0f },
            { ResourceType.Gold, 0f },
            { ResourceType.Stone, 0f },
        };

        private void Awake()
        {
            _instances[faction] = this;
        }

        public float GetTotal(ResourceType type)
        {
            return _totals[type];
        }

        public void Add(ResourceType type, float amount)
        {
            _totals[type] += amount;
        }

        // Save/load only - Add() is relative and every other call site
        // wants that (deposits/costs), but restoring a save needs to land
        // on an exact saved total regardless of whatever this stockpile
        // already holds (freshly reset to 0 by the normal match-start flow
        // that runs before a load rebuilds everything on top of it).
        public void SetTotal(ResourceType type, float value)
        {
            _totals[type] = value;
        }
    }
}

using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    // Wave 6 item 35 (Relics + Monastery collection, economic only - design
    // decision resolved 2026-09-15 via AskUserQuestion). A neutral world
    // prop scattered on the map (see ResourceNodeSpawner.SpawnRelic) that
    // any land unit with a RelicCarrier can pick up and walk to an owned
    // Monastery for a passive Gold trickle - AoE II's own base-case Relic
    // mechanic, deliberately without a Relic-count victory condition (the
    // user explicitly declined that option in the same design-decision
    // round).
    public class Relic : MonoBehaviour
    {
        public bool IsHeld { get; private set; }

        public void SetHeld(bool held)
        {
            IsHeld = held;
        }
    }
}

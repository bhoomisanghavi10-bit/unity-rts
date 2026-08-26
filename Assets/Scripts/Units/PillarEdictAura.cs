using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // Roadmap Section 5 item 3: Pillar Edict Scholar's support ability
    // (CSV CounterNotes: "+50% gather-rate aura to nearby Workers while
    // standing near them"). No proximity-aura system existed before this -
    // GatherRateMultiplier is a static per-civ value baked in at spawn
    // (see WorkerFactory), not spatial. Rather than push per-frame nearby-
    // unit scanning onto every scholar (there are at most a handful per
    // faction), Gatherer itself polls this static registry (same
    // convention as Building.All) - self-correcting as a worker walks in
    // and out of range, with no enter/exit event wiring needed.
    public class PillarEdictAura : MonoBehaviour
    {
        public const float Radius = 6f;
        public const float Multiplier = 1.5f;

        public static readonly List<PillarEdictAura> All = new List<PillarEdictAura>();

        public FactionId Faction { get; private set; }

        // Registers on Configure rather than OnEnable - Unity only invokes
        // OnEnable once the player loop is actually running (Play Mode),
        // not synchronously on AddComponent the way GetComponent-based
        // lookups work, so an OnEnable-based registration would silently
        // never happen in an EditMode test. Every factory calls Configure
        // immediately after AddComponent anyway (same as FactionMember), so
        // this is no less reliable in real play. OnDisable/destruction
        // callbacks DO fire immediately regardless of Play Mode, so removal
        // stays there.
        public void Configure(FactionId faction)
        {
            Faction = faction;
            if (!All.Contains(this))
            {
                All.Add(this);
            }
        }

        private void OnDisable()
        {
            All.Remove(this);
        }
    }
}

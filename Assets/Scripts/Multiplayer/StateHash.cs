using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;
using UnityEngine;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51 (lockstep foundation): folds live simulation state into one
    // number. In lockstep, every peer hashes its own state each tick (or
    // every N ticks) and compares - a mismatch means two peers executed the
    // "same" commands into different results (a desync), caught the moment
    // it happens instead of surfacing as unexplained state divergence
    // minutes later. No peer to compare against yet (single-process), but
    // the function itself - and picking a floating-point-safe hash input -
    // is the part that needs to exist before any networking can use it.
    public static class StateHash
    {
        // FNV-1a: simple, well-known, allocation-free. Positions/health are
        // rounded before hashing (see Fold) - float ops can legitimately
        // produce bit-different-but-gameplay-identical results across
        // machines/compilers, and hashing raw bits would flag that as a
        // false-positive desync.
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        // AoE-Parity Phase 5 (resync-on-desync): the one piece that turns
        // this from a pure function nobody calls into something a future
        // transport layer could actually read and compare against a peer's
        // own value. Recomputed once per simulated second (TickRate ticks),
        // not every tick - hashing every live Unit's full state 20x/sec has
        // no consumer yet to justify the cost. SimClock calls Subscribe()
        // once per match start (see SimClock.cs's own DeterministicRandom.
        // ReseedMatch call site) rather than this class self-installing via
        // RuntimeInitializeOnLoadMethod, since it only ever needs to react
        // to ticks SimClock already owns - a second independent
        // registration pattern would just be redundant plumbing.
        public static uint LatestHash { get; private set; }
        public static int LatestHashTick { get; private set; }
        private static bool _subscribed;

        internal static void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            SimClock.OnTick += OnTick;
        }

        private static void OnTick(int tick)
        {
            if (tick % (int)SimClock.TickRate != 0)
            {
                return;
            }

            LatestHash = Compute();
            LatestHashTick = tick;
        }

        public static uint Compute()
        {
            uint hash = FnvOffsetBasis;

            hash = Fold(hash, (uint)SimClock.CurrentTick);

            // Unit.All's order is insertion order (spawn order), which is
            // itself deterministic given a deterministic command stream -
            // not sorted by anything else, so this doesn't hide an
            // ordering-dependent bug, it relies on order already being
            // reproducible.
            foreach (Unit unit in Unit.All)
            {
                if (unit == null)
                {
                    continue;
                }

                hash = FoldVector(hash, unit.transform.position);

                if (unit.TryGetComponent(out FactionMember member))
                {
                    hash = Fold(hash, (uint)member.Faction);
                }

                if (unit.TryGetComponent(out Attackable attackable))
                {
                    hash = Fold(hash, (uint)Mathf.RoundToInt(attackable.Health * 100f));
                }
            }

            return hash;
        }

        private static uint Fold(uint hash, uint value)
        {
            hash ^= value;
            hash *= FnvPrime;
            return hash;
        }

        private static uint FoldVector(uint hash, Vector3 v)
        {
            hash = Fold(hash, (uint)Mathf.RoundToInt(v.x * 100f));
            hash = Fold(hash, (uint)Mathf.RoundToInt(v.y * 100f));
            hash = Fold(hash, (uint)Mathf.RoundToInt(v.z * 100f));
            return hash;
        }
    }
}

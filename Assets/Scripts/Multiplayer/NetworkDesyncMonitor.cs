using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: the actual "real cross-peer desync
    // detection" the roadmap flagged as blocked-on-transport. StateHash
    // already recomputes a real value once per simulated second (see
    // StateHash.cs) - this is the piece that sends it to the peer and
    // compares. SimClock.cs calls Subscribe() once, right after
    // StateHash.Subscribe(), at match start (same "SimClock owns the
    // subscription order" convention StateHash itself already uses) -
    // registration order matters here: this class's OnTick must run AFTER
    // StateHash's own OnTick for the same tick, so LatestHash/
    // LatestHashTick are already up to date when it reads them.
    public static class NetworkDesyncMonitor
    {
        private static bool _subscribed;
        private static readonly Dictionary<int, uint> _remoteHashes = new Dictionary<int, uint>();

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
            if (!NetworkMatch.IsActive || tick != StateHash.LatestHashTick)
            {
                return;
            }

            NetworkMatch.Transport.SendStateHash(tick, StateHash.LatestHash);
            CompareIfBothPresent(tick);
        }

        public static void OnRemoteHashReceived(int tick, uint hash)
        {
            _remoteHashes[tick] = hash;
            CompareIfBothPresent(tick);
        }

        // MVP-scope simplification (disclosed, not silently dropped): only
        // compares a remote hash against the local hash for the exact same
        // tick, checked at the moment either side arrives. A remote hash
        // for a tick local has already moved past (e.g. a very late/
        // reordered-at-the-application-layer message - shouldn't happen
        // over TCP's own in-order delivery, but a slow Dispatch drain
        // could still see it well after the fact) is silently dropped
        // rather than compared retroactively - acceptable for a LAN MVP
        // where StateHash ticks land roughly once per second and TCP
        // preserves order, but a real production version would keep a
        // longer rolling window.
        private static void CompareIfBothPresent(int tick)
        {
            if (StateHash.LatestHashTick != tick || !_remoteHashes.TryGetValue(tick, out uint remoteHash))
            {
                return;
            }

            _remoteHashes.Remove(tick);

            if (remoteHash != StateHash.LatestHash)
            {
                HandleDesync(tick, remoteHash);
            }
        }

        private static void HandleDesync(int tick, uint remoteHash)
        {
            Debug.LogWarning($"[NetworkDesyncMonitor] Desync detected at tick {tick}: local hash {StateHash.LatestHash}, remote hash {remoteHash}.");

            // The host is authoritative for resync (a symmetric "whoever
            // notices first wins" design would let both sides simultaneously
            // declare themselves correct and overwrite each other) - the
            // client passively waits for the host's ResyncSnapshot message
            // (see NetworkDriver.cs's own Dispatch) rather than also
            // capturing and sending its own.
            if (!NetworkMatch.IsHost)
            {
                return;
            }

            MatchSaveData snapshot = SaveManager.Capture();
            string json = JsonUtility.ToJson(snapshot);
            NetworkMatch.Transport.SendResyncSnapshot(tick, json);
        }
    }
}

using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51 (lockstep foundation): a fixed-tick simulation clock,
    // independent of Time.deltaTime, that every future lockstep-critical
    // system (unit orders, command execution, state hashing) should drive
    // off instead of Update(). Deterministic lockstep needs every peer to
    // simulate the exact same discrete steps in the exact same order - a
    // variable-length frame-based Update() can't give that guarantee even
    // if every system inside it is otherwise deterministic. Self-installs
    // the same way SaveManager/ScenarioManager do, so this needs zero
    // Main.unity changes.
    //
    // v1 scope (proving the pattern, not a full netcode stack): still
    // single-process - there is no peer, no transport, no rollback yet.
    // This only establishes the tick boundary that CommandBus schedules
    // against and that a future network layer would synchronize peers on.
    public class SimClock : MonoBehaviour
    {
        public const float TickRate = 20f;
        public const float TickDuration = 1f / TickRate;

        public static int CurrentTick { get; private set; }

        public delegate void TickHandler(int tick);
        public static event TickHandler OnTick;

        private float _accumulator;
        private bool _wasMatchStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<SimClock>() != null)
            {
                return;
            }

            GameObject go = new GameObject("SimClock");
            go.AddComponent<SimClock>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            CurrentTick = 0;
            _accumulator = 0f;
        }

        // CivilizationSetup.OnDestroy resets HasMatchStarted to false
        // between matches, but this object is DontDestroyOnLoad and never
        // re-enabled - so the false->true transition (a fresh match
        // beginning) is the actual reset point, detected here rather than
        // relying on OnEnable firing again.
        private void Update()
        {
            if (!CivilizationSetup.HasMatchStarted)
            {
                _wasMatchStarted = false;
                return;
            }

            if (!_wasMatchStarted)
            {
                _wasMatchStarted = true;
                CurrentTick = 0;
                _accumulator = 0f;
                // Phase 5 LAN transport MVP: a real 2-peer match needs both
                // sides to reseed DeterministicRandom.Match with the exact
                // same value - NetworkMatch.PendingSeed carries whatever
                // the host generated and both sides agreed on during the
                // LanMatchMenu handshake (see NetworkMatch.cs), taking
                // priority over the single-player MapRegistry/TickCount
                // fallback below (which stays exactly as before for every
                // non-networked match).
                DeterministicRandom.ReseedMatch(NetworkMatch.PendingSeed ?? (MapRegistry.Current.ResourceSeed == -1
                    ? System.Environment.TickCount
                    : MapRegistry.Current.ResourceSeed));
                // AoE-Parity Phase 5 (resync-on-desync): StateHash reacts to
                // OnTick same as CommandBus does - see StateHash.Subscribe.
                StateHash.Subscribe();
                // Phase 5 LAN transport MVP: must subscribe after StateHash
                // above, not before - multicast delegates invoke in
                // registration order, and NetworkDesyncMonitor.OnTick reads
                // StateHash.LatestHash/LatestHashTick, which only StateHash's
                // own (already-registered) handler updates for this same
                // tick. No-op in single-player - its own OnTick immediately
                // returns when NetworkMatch.IsActive is false.
                NetworkDesyncMonitor.Subscribe();
            }

            _accumulator += Time.deltaTime;
            while (_accumulator >= TickDuration)
            {
                // Phase 5 LAN transport MVP: the actual lockstep gate - a
                // real peer must not simulate a tick further ahead than
                // what it's heard from the remote side (see
                // NetworkMatch.RemoteMaxAckedTick's own comment for why
                // this can never deadlock). Breaks out of the loop rather
                // than consuming the buffered time, so a network stall
                // pauses simulation (the buffered real time stays queued in
                // _accumulator) instead of silently dropping ticks - once
                // the remote catches up, several ticks fire in one frame to
                // catch back up, same as a slow frame already does today in
                // single-player.
                if (NetworkMatch.IsActive && NetworkMatch.RemoteMaxAckedTick < CurrentTick + 1)
                {
                    break;
                }

                _accumulator -= TickDuration;
                CurrentTick++;
                OnTick?.Invoke(CurrentTick);

                if (NetworkMatch.IsActive)
                {
                    NetworkMatch.Transport.SendHeartbeat(CurrentTick + CommandBus.InputDelayTicks);
                }
            }
        }
    }
}

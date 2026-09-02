using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: the "who am I" seam that was missing
    // entirely before this - SelectionManager/BuildingPlacer/BuildMenu all
    // hardcoded FactionId.Player as "whoever is at this keyboard," which is
    // only true in single-player. For a 2-human LAN match, each machine
    // runs the exact same scene/scripts, so the one thing that has to
    // differ per-machine is which FactionId this process's own input
    // controls - LocalFaction is that switch. Defaults to FactionId.Player
    // so every existing single-player/AI-opponent flow is completely
    // unaffected (IsActive stays false, LocalFaction stays Player) unless a
    // real LAN match is actually started via LanMatchMenu.
    public static class NetworkMatch
    {
        public static FactionId LocalFaction { get; private set; } = FactionId.Player;
        public static FactionId RemoteFaction { get; private set; } = FactionId.Enemy;
        public static bool IsActive { get; private set; }
        public static bool IsHost { get; private set; }

        // Consumed once by SimClock's own match-start reseed logic (see
        // SimClock.cs) instead of MapRegistry.Current.ResourceSeed/
        // Environment.TickCount - both peers must reseed
        // DeterministicRandom.Match with the exact same value, and only a
        // real transport (this class's caller) can guarantee that.
        public static int? PendingSeed { get; private set; }

        public static LanTransport Transport { get; private set; }

        // Phase 5 LAN transport MVP's actual lockstep gate (see SimClock.cs):
        // the highest tick value seen across every message received from
        // the remote peer (commands and Heartbeats alike - see
        // NetworkDriver.cs). SimClock will not advance its own CurrentTick
        // past this value, so a stalled/slow remote peer visibly stalls
        // local simulation too, rather than the two sides silently running
        // ahead of each other. Starts at CommandBus.InputDelayTicks rather
        // than 0/-1 because Begin() below sends (and expects to receive) an
        // initial Heartbeat for that exact tick as part of the handshake -
        // see LanMatchMenu.cs.
        public static int RemoteMaxAckedTick { get; private set; }

        public static void Begin(FactionId localFaction, bool isHost, int seed, LanTransport transport)
        {
            LocalFaction = localFaction;
            RemoteFaction = localFaction == FactionId.Player ? FactionId.Enemy : FactionId.Player;
            IsHost = isHost;
            PendingSeed = seed;
            Transport = transport;
            RemoteMaxAckedTick = 0;
            IsActive = true;

            // Breaks the otherwise-real chicken-and-egg at tick 1: SimClock
            // won't execute tick T until RemoteMaxAckedTick >= T, and this
            // class's own ongoing Heartbeats (sent from SimClock.Update,
            // tagged CurrentTick + InputDelayTicks) only start once ticks
            // are already executing. Both peers unconditionally send this
            // exact same tick as part of finishing the handshake, with
            // nothing to wait on first - see SimClock.cs's gating comment.
            transport.SendHeartbeat(CommandBus.InputDelayTicks);
        }

        public static void OnRemoteTickSeen(int tick)
        {
            if (tick > RemoteMaxAckedTick)
            {
                RemoteMaxAckedTick = tick;
            }
        }

        // Called by CivilizationSetup.OnDestroy alongside its own
        // HasMatchStarted reset, so a returned-to-menu/replayed match
        // never carries a stale LocalFaction/Transport into a subsequent
        // single-player game.
        public static void End()
        {
            Transport?.Close();
            LocalFaction = FactionId.Player;
            RemoteFaction = FactionId.Enemy;
            IsActive = false;
            IsHost = false;
            PendingSeed = null;
            Transport = null;
            RemoteMaxAckedTick = 0;
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51 (lockstep foundation): schedules commands onto a future
    // SimClock tick and executes them all when that tick arrives, instead
    // of running the requested action immediately. This input-delay queue
    // is the actual lockstep pattern - real multiplayer would use the delay
    // to wait for every peer's commands to arrive before executing any of
    // them, so establishing the queue now (even single-process, with
    // nothing to wait on) is what future networking slots into without
    // reshaping how callers issue orders.
    public static class CommandBus
    {
        // How many ticks ahead a command is scheduled. A real lockstep peer
        // needs this to be at least "worst-case network round trip" worth
        // of ticks; 4 ticks (~200ms at TickRate=20) is a placeholder large
        // enough to be visibly non-instant (proving the queue is real) but
        // small enough not to make single-player orders feel laggy.
        public const int InputDelayTicks = 4;

        private static readonly Dictionary<int, List<Command>> _scheduled = new Dictionary<int, List<Command>>();
        private static bool _subscribed;
        private static int _localSequence;
        private static int _lastExecutedTick = -1;

        // Sequence stamped on the most recent local Enqueue; the caller sends
        // it with the wire envelope (NetworkMatch.SendCommand).
        public static int LastSequence { get; private set; }

        // Fresh queue/counters for a new match: without this a command
        // scheduled in a previous match could still fire in the next one.
        public static void ResetForNewMatch()
        {
            _scheduled.Clear();
            _localSequence = 0;
            LastSequence = 0;
            _lastExecutedTick = -1;
        }

        public static void EnsureSubscribed()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            SimClock.OnTick += ExecuteTick;
        }

        public static int Enqueue(Command command)
        {
            EnsureSubscribed();

            int executeTick = SimClock.CurrentTick + InputDelayTicks;
            command.Sequence = ++_localSequence;
            LastSequence = command.Sequence;
            EnqueueAt(executeTick, command);
            return executeTick;
        }

        // Test-only entry point: schedules at an explicit tick, bypassing
        // SimClock.CurrentTick-relative math, so a determinism test can pin
        // an exact tick sequence without depending on (or mutating) the live
        // static SimClock.CurrentTick. Internal rather than private for the
        // same reason ExecuteTick below is - see CommandBusDeterminismTests.
        internal static void EnqueueAt(int tick, Command command)
        {
            // A peer that has not issued any order yet must still execute the
            // remote peer's (previously only Enqueue subscribed to the clock).
            EnsureSubscribed();

            // A network command for a tick this peer already executed can
            // never run in the right order; refuse it loudly (it is a desync
            // in the making) instead of parking it in a dead bucket.
            if (NetworkMatch.IsActive && tick <= _lastExecutedTick)
            {
                NetworkDiagnostics.Report(NetworkIssue.LateCommand,
                    $"{command.GetType().Name} for tick {tick} arrived after tick {_lastExecutedTick} executed");
                return;
            }

            if (!_scheduled.TryGetValue(tick, out List<Command> commands))
            {
                commands = new List<Command>();
                _scheduled[tick] = commands;
            }

            commands.Add(command);
        }

        // Internal (not private) so an EditMode test can fire a tick
        // directly - SimClock's own Update() never runs in EditMode (gated
        // on CivilizationSetup.HasMatchStarted, always false there), so
        // there's no other way to exercise this deterministically without a
        // live Play Mode session. See AssemblyInfo.cs's InternalsVisibleTo
        // grant.
        internal static void ExecuteTick(int tick)
        {
            // Recorded even when nothing is scheduled: a command arriving
            // later for this tick is late whether or not the bucket existed.
            if (tick > _lastExecutedTick)
            {
                _lastExecutedTick = tick;
            }

            if (!_scheduled.TryGetValue(tick, out List<Command> commands))
            {
                return;
            }

            // Fixed enqueue order = fixed execution order, which is what
            // "same inputs -> same state" actually depends on - a peer that
            // executed the same tick's commands in a different order could
            // diverge even with identical inputs.
            // Canonical order: by sender slot then that sender's own sequence,
            // NOT arrival order - two peers can receive the same tick's
            // commands in different orders. OrderBy is stable, so commands
            // with equal keys (e.g. the unsequenced ones tests create) keep
            // insertion order.
            foreach (Command command in commands.OrderBy(c => (int)c.Faction).ThenBy(c => c.Sequence).ToList())
            {
                NetworkValidationLog.Record($"CMD {tick} {(int)command.Faction} {command.Sequence} {command.GetType().Name}");
                command.Execute();
            }

            _scheduled.Remove(tick);
        }
    }
}

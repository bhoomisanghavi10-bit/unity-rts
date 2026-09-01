using System.Collections.Generic;

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
            if (!_scheduled.TryGetValue(tick, out List<Command> commands))
            {
                return;
            }

            // Fixed enqueue order = fixed execution order, which is what
            // "same inputs -> same state" actually depends on - a peer that
            // executed the same tick's commands in a different order could
            // diverge even with identical inputs.
            foreach (Command command in commands)
            {
                command.Execute();
            }

            _scheduled.Remove(tick);
        }
    }
}

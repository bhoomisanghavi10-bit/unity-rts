using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51 (lockstep foundation): the unit of input a lockstep
    // simulation replays. In real lockstep, every peer broadcasts its
    // player's commands for a future tick, waits until it has every peer's
    // commands for that tick, then executes them all in the same
    // deterministic order - this is what makes "same inputs -> same
    // state" hold across machines without sending state itself.
    //
    // v1 scope: single-process, so there's nothing to broadcast/wait on
    // yet. Command still carries Faction (not just "whoever clicked") so
    // that requirement is visible in the type itself rather than assumed,
    // and Execute() is the seam a future network layer serializes/replays
    // through instead of calling gameplay code directly.
    public abstract class Command
    {
        public readonly FactionId Faction;

        protected Command(FactionId faction)
        {
            Faction = faction;
        }

        public abstract void Execute();
    }
}

using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51 (lockstep foundation): the one real command wired end-to-end
    // through CommandBus, proving the pattern against an actual gameplay
    // action rather than a synthetic test. SelectionManager's plain-move
    // branch (the else case in HandleMoveInput) enqueues this instead of
    // calling UnitMover.MoveTo directly - the order still reaches the unit,
    // just InputDelayTicks later, deterministically.
    public class MoveCommand : Command
    {
        private readonly UnitMover _mover;
        private readonly Vector3 _destination;

        public MoveCommand(FactionId faction, UnitMover mover, Vector3 destination) : base(faction)
        {
            _mover = mover;
            _destination = destination;
        }

        public override void Execute()
        {
            // The unit (or its mover) may have been destroyed in the delay
            // window between the click and this tick executing (killed,
            // despawned) - a stale command should silently no-op, not throw
            // into SimClock's OnTick invocation and break every other
            // command scheduled for the same tick.
            if (_mover == null)
            {
                return;
            }

            _mover.MoveTo(_destination);
        }
    }
}

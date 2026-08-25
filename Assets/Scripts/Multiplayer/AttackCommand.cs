using System;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51: a player-issued attack-move order, wired the same way
    // MoveCommand is - SelectionManager's attack branches enqueue this
    // instead of calling MeleeAttacker/BoatAttacker.AttackMove directly.
    // Same delegate-based shape as TrainCommand, for the same reason:
    // MeleeAttacker and BoatAttacker both expose AttackMove(Attackable) but
    // share no common attacker interface.
    //
    // Deliberately NOT used for StanceController's or AiController's own
    // AttackMove calls - those are automatic per-tick simulation decisions
    // (aggressive-stance auto-engage, AI target selection), not raw local-
    // player input. In lockstep, only actual player input needs to be
    // queued/replayed; a deterministic simulation decision produces the
    // same result on every peer once state matches, so routing it through
    // the input-delay queue would just add latency for no synchronization
    // benefit.
    public class AttackCommand : Command
    {
        private readonly UnityEngine.Object _attacker;
        private readonly Attackable _target;
        private readonly Action<Attackable> _attackMove;

        public AttackCommand(FactionId faction, UnityEngine.Object attacker, Attackable target, Action<Attackable> attackMove) : base(faction)
        {
            _attacker = attacker;
            _target = target;
            _attackMove = attackMove;
        }

        public override void Execute()
        {
            // Both the attacker and the target can go stale in the delay
            // window - the attacker could be destroyed, or the target could
            // already be dead (or destroyed outright) by the time this
            // tick executes.
            if (_attacker == null || _target == null || _target.IsDead)
            {
                return;
            }

            _attackMove(_target);
        }
    }
}

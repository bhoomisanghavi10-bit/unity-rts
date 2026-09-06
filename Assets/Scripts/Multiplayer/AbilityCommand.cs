using System;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Wave 4 item 27: a targeted-ability order, wired the same way
    // TrainCommand/TradeRouteCommand are - shared by both Heal (Vaidya)
    // and Convert (Purohita) orders rather than writing two near-duplicate
    // classes, since neither needs anything beyond "call this captured
    // no-arg effect, guarded by a staleness check."
    public class AbilityCommand : Command
    {
        private readonly UnityEngine.Object _source;
        private readonly Action _effect;

        public AbilityCommand(FactionId faction, UnityEngine.Object source, Action effect) : base(faction)
        {
            _source = source;
            _effect = effect;
        }

        public override void Execute()
        {
            // The acting unit may have been destroyed in the delay window
            // between the click and this tick executing - same
            // stale-reference guard as MoveCommand/TrainCommand/
            // TradeRouteCommand.
            if (_source == null)
            {
                return;
            }

            _effect();
        }
    }
}

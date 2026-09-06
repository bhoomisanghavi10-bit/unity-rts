using System;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Wave 4 item 26: a trade-route order, wired the same way TrainCommand
    // is - SelectionManager enqueues this instead of calling
    // Trader.SetTradeRoute/BoatTrader.SetTradeRoute directly. Trader and
    // BoatTrader share no common interface (same reasoning TrainCommand's
    // own comment gives for Barracks/TownCenter), so this carries the
    // actual request as a plain delegate.
    public class TradeRouteCommand : Command
    {
        private readonly UnityEngine.Object _source;
        private readonly Action _setRoute;

        public TradeRouteCommand(FactionId faction, UnityEngine.Object source, Action setRoute) : base(faction)
        {
            _source = source;
            _setRoute = setRoute;
        }

        public override void Execute()
        {
            // The trader unit may have been destroyed in the delay window
            // between the click and this tick executing - same
            // stale-reference guard as MoveCommand/TrainCommand.
            if (_source == null)
            {
                return;
            }

            _setRoute();
        }
    }
}

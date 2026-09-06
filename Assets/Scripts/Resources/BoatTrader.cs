using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.ResourceGathering
{
    // Wave 4 item 26: Trade Ship's trade-route state machine - mirrors
    // Trader.cs's shape exactly, but drives WaterMover and shuttles between
    // Docks instead of Markets. A separate component rather than a
    // generalized Trader, same reasoning as BoatGatherer/BoatAttacker: a
    // proven land pattern duplicated for water is lower risk than sharing
    // one generic implementation across both.
    [RequireComponent(typeof(WaterMover))]
    public class BoatTrader : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private float goldPerDistanceUnit = 0.15f;
        [SerializeField] private float minTradeGold = 10f;
        [SerializeField] private float maxTradeGold = 80f;

        private enum State { Idle, MovingToHome, MovingToDestination }

        private WaterMover _mover;
        private State _state = State.Idle;
        private Dock _home;
        private Dock _destination;
        private float _routeDistance;

        public bool HasRoute => _state != State.Idle;

        private WaterMover Mover => _mover != null ? _mover : (_mover = GetComponent<WaterMover>());

        public void SetTradeRoute(Dock destination)
        {
            if (destination == null || !destination.IsComplete)
            {
                return;
            }

            Dock home = FindNearestOwnedDock(transform.position, MyFaction(), destination);
            if (home == null)
            {
                return;
            }

            _home = home;
            _destination = destination;
            _routeDistance = Vector3.Distance(home.transform.position, destination.transform.position);
            _state = State.MovingToDestination;
            Mover.MoveTo(_destination.transform.position);
        }

        public void CancelRoute()
        {
            _home = null;
            _destination = null;
            _state = State.Idle;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.MovingToDestination:
                    TickLeg(_destination, State.MovingToHome);
                    break;
                case State.MovingToHome:
                    TickLeg(_home, State.MovingToDestination);
                    break;
            }
        }

        private void TickLeg(Dock target, State nextState)
        {
            if (target == null || !target.IsComplete)
            {
                CancelRoute();
                return;
            }

            if (WithinRange(target.transform.position))
            {
                ResourceStockpile.For(MyFaction()).Add(ResourceType.Gold, ComputeTradeGold(_routeDistance));
                _state = nextState;
                Mover.MoveTo((nextState == State.MovingToHome ? _home : _destination).transform.position);
            }
        }

        // internal (not private) so EditMode tests can exercise the routing
        // rule directly - same convention as BoatGatherer.FindNearestDock.
        internal static Dock FindNearestOwnedDock(Vector3 from, FactionId faction, Dock excluding)
        {
            Dock nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!(building is Dock dock) || dock == excluding)
                {
                    continue;
                }

                if (!dock.TryGetComponent(out FactionMember buildingFaction)
                    || buildingFaction.Faction != faction)
                {
                    continue;
                }

                float distance = Vector3.Distance(from, dock.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = dock;
                }
            }

            return nearest;
        }

        // Pure so it's directly EditMode-testable - not independently
        // balanced, first-pass numbers, see this item's own session notes.
        internal static float ComputeTradeGold(float distance, float ratePerUnit, float min, float max)
        {
            return Mathf.Clamp(distance * ratePerUnit, min, max);
        }

        private float ComputeTradeGold(float distance)
        {
            return ComputeTradeGold(distance, goldPerDistanceUnit, minTradeGold, maxTradeGold);
        }

        private FactionId MyFaction()
        {
            return TryGetComponent(out FactionMember factionMember)
                ? factionMember.Faction
                : FactionId.Player;
        }

        private bool WithinRange(Vector3 target)
        {
            return Vector3.Distance(transform.position, target) <= interactionRange;
        }
    }
}

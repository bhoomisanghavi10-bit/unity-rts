using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.ResourceGathering
{
    // Wave 4 item 26: Vanik's trade-route state machine - mirrors Gatherer's
    // walk/act/walk-back shape, but shuttles between two owned/allied
    // Markets forever instead of depleting a resource node. Pays Gold on
    // every leg's arrival (not just the round trip), proportional to route
    // distance - see ComputeTradeGold.
    [RequireComponent(typeof(UnitMover))]
    public class Trader : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;
        [SerializeField] private float goldPerDistanceUnit = 0.2f;
        [SerializeField] private float minTradeGold = 8f;
        [SerializeField] private float maxTradeGold = 60f;

        private enum State { Idle, MovingToHome, MovingToDestination }

        private UnitMover _mover;
        private State _state = State.Idle;
        private Market _home;
        private Market _destination;
        private float _routeDistance;

        public bool HasRoute => _state != State.Idle;

        // Resolved lazily, not cached in Awake - same "sibling component may
        // not exist yet" gotcha documented on Gatherer.Mover.
        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

        // Right-clicking a friendly Market with this unit selected calls
        // this. The "home" leg is always resolved to the nearest OTHER
        // Market owned by this unit's own faction (never an ally's, even
        // though the destination itself can be an ally's - see
        // SelectionManager.IsFriendlyToPlayer's own gate for that half of
        // the rule) - no-ops if no second Market exists yet, same
        // "keep waiting" tolerance Gatherer.FindNearestDropOff has for a
        // not-yet-built drop-off.
        public void SetTradeRoute(Market destination)
        {
            if (destination == null || !destination.IsComplete)
            {
                return;
            }

            Market home = FindNearestOwnedMarket(transform.position, MyFaction(), destination);
            if (home == null)
            {
                return;
            }

            _home = home;
            _destination = destination;
            _routeDistance = Vector3.Distance(home.transform.position, destination.transform.position);
            _state = State.MovingToDestination;
            Mover.MoveTo(ApproachPoint(_destination));
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

        private void TickLeg(Market target, State nextState)
        {
            if (target == null || !target.IsComplete)
            {
                CancelRoute();
                return;
            }

            if (WithinRange(ApproachPoint(target)))
            {
                ResourceStockpile.For(MyFaction()).Add(ResourceType.Gold, ComputeTradeGold(_routeDistance));
                _state = nextState;
                Mover.MoveTo(ApproachPoint(nextState == State.MovingToHome ? _home : _destination));
            }
        }

        // The Market's own footprint carves a NavMeshObstacle over it, same
        // as any other building - see Gatherer.ComputeDropOffApproachPoint's
        // identical reasoning.
        private Vector3 ApproachPoint(Market market)
        {
            return market.TryGetComponent(out BuildingFootprintTag footprintTag)
                ? footprintTag.GetNearestApproachPoint(transform.position, Mover.Radius + 0.1f)
                : market.transform.position;
        }

        // internal (not private) so EditMode tests can exercise the routing
        // rule directly without driving a full Trader state machine - see
        // AssemblyInfo.cs's InternalsVisibleTo grant, same convention as
        // Gatherer.AcceptsDropOff/FindNearestDropOff.
        internal static Market FindNearestOwnedMarket(Vector3 from, FactionId faction, Market excluding)
        {
            Market nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!(building is Market market) || market == excluding)
                {
                    continue;
                }

                if (!market.TryGetComponent(out FactionMember buildingFaction)
                    || buildingFaction.Faction != faction)
                {
                    continue;
                }

                float distance = Vector3.Distance(from, market.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = market;
                }
            }

            return nearest;
        }

        // Pure so it's directly EditMode-testable without a NavMeshAgent/
        // baked NavMesh, same reasoning as Gatherer.ComputeFleeDestination's
        // own pure-function test coverage. Not independently balanced -
        // first-pass numbers, see this item's own session notes.
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

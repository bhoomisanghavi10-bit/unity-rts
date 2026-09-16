using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.ResourceGathering
{
    // Wave 6 item 35: unit-side half of Relic collection. Same walk/act/
    // walk-back shape as Trader (mirrors its MovingToDestination/
    // MovingToHome split), but a one-shot pickup-then-deliver instead of an
    // endless shuttle - a Relic is consumed into the Monastery it's
    // delivered to, not re-collected. Added to every land unit factory that
    // already carries GarrisonSeeker (see those factories' own
    // AddComponent<RelicCarrier>() call) - any eligible unit, not a
    // dedicated collector type, matching the design decision's "carried by
    // any land unit" wording.
    [RequireComponent(typeof(UnitMover))]
    public class RelicCarrier : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;

        private enum State { Idle, MovingToRelic, SeekingMonastery }

        private UnitMover _mover;
        private State _state = State.Idle;
        private Relic _relic;
        private Monastery _monastery;
        private Vector3 _approachPoint;

        public bool IsCarrying => _state == State.SeekingMonastery;

        // Resolved lazily, not in Awake() - same "sibling component may not
        // exist yet" gotcha documented on GarrisonSeeker.Mover/Gatherer.Mover.
        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

        // Right-clicking an unclaimed Relic with this unit selected calls
        // this - see SelectionManager's hitRelic branch.
        public void PickUp(Relic relic)
        {
            if (relic == null || relic.IsHeld)
            {
                return;
            }

            _relic = relic;
            _monastery = null;
            _approachPoint = relic.transform.position;
            _state = State.MovingToRelic;
            Mover.MoveTo(_approachPoint);
        }

        // A Relic already being carried home finishes its trip rather than
        // being dropped mid-order - same carve-out Gatherer.CancelGather/
        // Trader.CancelRoute use for their own in-progress deliveries.
        public void CancelCarry()
        {
            if (_state == State.SeekingMonastery)
            {
                return;
            }

            _relic = null;
            _state = State.Idle;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.MovingToRelic:
                    TickMovingToRelic();
                    break;
                case State.SeekingMonastery:
                    TickSeekingMonastery();
                    break;
            }
        }

        private void TickMovingToRelic()
        {
            if (_relic == null || _relic.IsHeld)
            {
                _state = State.Idle;
                return;
            }

            if (!WithinRange(_approachPoint))
            {
                return;
            }

            _relic.SetHeld(true);
            // No dedicated carried-Relic art exists yet (flagging per the
            // flag-asset-needs convention) - parenting the world prop onto
            // the carrying unit is the visual stand-in: it stays visible
            // and simply rides along instead of vanishing, the same
            // "reparent, don't hide" approach as WeaponAttachment.
            _relic.transform.SetParent(transform, worldPositionStays: false);
            _relic.transform.localPosition = Vector3.up * 1.6f;
            _state = State.SeekingMonastery;
        }

        private void TickSeekingMonastery()
        {
            if (_relic == null)
            {
                _state = State.Idle;
                return;
            }

            if (_monastery == null)
            {
                _monastery = FindNearestOwnedMonastery(transform.position, MyFaction());
                if (_monastery == null)
                {
                    return; // no Monastery exists yet; keep waiting, same as Gatherer.FindNearestDropOff
                }
                _approachPoint = ComputeApproachPoint(_monastery);
                Mover.MoveTo(_approachPoint);
                return;
            }

            if (!_monastery.IsComplete)
            {
                _monastery = null; // was destroyed/demolished mid-route; re-resolve next tick
                return;
            }

            if (WithinRange(_approachPoint))
            {
                Deposit();
            }
        }

        private void Deposit()
        {
            _monastery.AddRelic();
            Destroy(_relic.gameObject);
            _relic = null;
            _monastery = null;
            _state = State.Idle;
        }

        // A carrier that dies mid-carry drops the Relic where it stood -
        // unparenting with worldPositionStays defaulted true keeps its
        // current world position, so it lands exactly where the unit died,
        // pickupable again by anyone.
        private void OnDestroy()
        {
            if (_relic != null)
            {
                _relic.transform.SetParent(null);
                _relic.SetHeld(false);
            }
        }

        // internal (not private) so EditMode tests can exercise the routing
        // rule directly - same convention as Trader.FindNearestOwnedMarket.
        internal static Monastery FindNearestOwnedMonastery(Vector3 from, FactionId faction)
        {
            Monastery nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!(building is Monastery monastery) || !monastery.IsComplete)
                {
                    continue;
                }

                if (!monastery.TryGetComponent(out FactionMember buildingFaction)
                    || buildingFaction.Faction != faction)
                {
                    continue;
                }

                float distance = Vector3.Distance(from, monastery.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = monastery;
                }
            }

            return nearest;
        }

        // Same footprint/NavMeshObstacle reasoning as Gatherer.
        // ComputeDropOffApproachPoint/Trader.ApproachPoint.
        private Vector3 ComputeApproachPoint(Monastery monastery)
        {
            return monastery.TryGetComponent(out BuildingFootprintTag footprintTag)
                ? footprintTag.GetNearestApproachPoint(transform.position, Mover.Radius + 0.1f)
                : monastery.transform.position;
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

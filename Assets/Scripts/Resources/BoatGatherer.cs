using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.ResourceGathering
{
    // Item 49: Fishing Boat's gathering state machine - mirrors Gatherer's
    // walk/gather/walk-back/deposit shape exactly, but drives WaterMover
    // instead of UnitMover and drops off at the nearest Dock instead of a
    // TownCenter (a Dock is the only building that sits at water's edge).
    // A separate component rather than a generalized Gatherer, same
    // reasoning as BoatAttacker/MeleeAttacker: Gatherer is
    // [RequireComponent(typeof(UnitMover))] and heavily depended on by
    // every land worker - duplicating a working, proven pattern is lower
    // risk than changing it.
    [RequireComponent(typeof(WaterMover))]
    public class BoatGatherer : MonoBehaviour
    {
        [SerializeField] private float gatherRate = 4f;
        [SerializeField] private float carryCapacity = 15f;
        [SerializeField] private float interactionRange = 2f;

        private enum State { Idle, MovingToNode, Gathering, MovingToDropOff }

        private WaterMover _mover;
        private State _state = State.Idle;
        private ResourceNode _targetNode;
        private Building _dropOff;
        private ResourceType _carriedType;
        private float _carriedAmount;

        public bool IsWorking => _state != State.Idle;

        private void Awake()
        {
            _mover = GetComponent<WaterMover>();
        }

        public void GatherFrom(ResourceNode node)
        {
            _targetNode = node;
            _dropOff = null;
            _mover.MoveTo(node.transform.position);
            _state = State.MovingToNode;
        }

        public void CancelGather()
        {
            if (_state == State.MovingToDropOff)
            {
                return;
            }

            _targetNode = null;
            _state = State.Idle;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.MovingToNode:
                    TickMovingToNode();
                    break;
                case State.Gathering:
                    TickGathering();
                    break;
                case State.MovingToDropOff:
                    TickMovingToDropOff();
                    break;
            }
        }

        private void TickMovingToNode()
        {
            if (_targetNode == null)
            {
                _state = State.Idle;
                return;
            }

            if (WithinRange(_targetNode.transform.position))
            {
                _state = State.Gathering;
            }
        }

        private void TickGathering()
        {
            if (_targetNode == null || _targetNode.IsDepleted)
            {
                _state = _carriedAmount > 0f ? State.MovingToDropOff : State.Idle;
                if (_state == State.MovingToDropOff)
                {
                    _dropOff = null;
                }
                return;
            }

            if (!WithinRange(_targetNode.transform.position))
            {
                _mover.MoveTo(_targetNode.transform.position);
                _state = State.MovingToNode;
                return;
            }

            _carriedType = _targetNode.ResourceType;
            _carriedAmount += _targetNode.Harvest(gatherRate * Time.deltaTime);

            if (_carriedAmount >= carryCapacity)
            {
                _dropOff = null;
                _state = State.MovingToDropOff;
            }
        }

        private void TickMovingToDropOff()
        {
            if (_dropOff == null)
            {
                _dropOff = FindNearestDock();
                if (_dropOff == null)
                {
                    return; // no Dock exists yet; keep waiting
                }
                _mover.MoveTo(_dropOff.transform.position);
            }

            if (WithinRange(_dropOff.transform.position))
            {
                Deposit();
            }
        }

        private void Deposit()
        {
            ResourceStockpile.For(MyFaction()).Add(_carriedType, _carriedAmount);
            _carriedAmount = 0f;

            if (_targetNode != null && !_targetNode.IsDepleted)
            {
                _mover.MoveTo(_targetNode.transform.position);
                _state = State.MovingToNode;
            }
            else
            {
                _state = State.Idle;
            }
        }

        private Building FindNearestDock()
        {
            FactionId faction = MyFaction();
            Building nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!(building is Dock))
                {
                    continue;
                }

                if (!building.TryGetComponent(out FactionMember buildingFaction)
                    || buildingFaction.Faction != faction)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = building;
                }
            }

            return nearest;
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

using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.ResourceGathering
{
    // Worker state machine: walk to a resource node, gather over time up to
    // a carry cap, walk the load to the nearest drop-off building, repeat
    // until the node is depleted.
    [RequireComponent(typeof(UnitMover))]
    public class Gatherer : MonoBehaviour
    {
        [SerializeField] private float gatherRate = 5f; // units per second
        [SerializeField] private float carryCapacity = 10f;
        [SerializeField] private float interactionRange = 2.5f;

        private enum State { Idle, MovingToNode, Gathering, MovingToDropOff }

        private UnitMover _mover;
        private State _state = State.Idle;
        private ResourceNode _targetNode;
        private Building _dropOff;
        private ResourceType _carriedType;
        private float _carriedAmount;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void GatherFrom(ResourceNode node)
        {
            _targetNode = node;
            _dropOff = null;
            _mover.MoveTo(node.transform.position);
            _state = State.MovingToNode;
        }

        // Interrupts gathering. If a load is already being carried to the
        // drop-off, let that finish rather than losing it.
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
                _dropOff = FindNearestDropOff();
                if (_dropOff == null)
                {
                    return; // no drop-off exists yet; keep waiting
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
            ResourceStockpile.Instance.Add(_carriedType, _carriedAmount);
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

        private Building FindNearestDropOff()
        {
            Building nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!(building is TownCenter))
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

        private bool WithinRange(Vector3 target)
        {
            return Vector3.Distance(transform.position, target) <= interactionRange;
        }
    }
}

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
        private float _stuckLogTimer;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void GatherFrom(ResourceNode node)
        {
            _targetNode = node;
            _dropOff = null;
            _mover.MoveTo(node.transform.position);
            SetState(State.MovingToNode);
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
            SetState(State.Idle);
        }

        // Temporary breadcrumb trail while chasing a gathering-loop bug.
        // Remove once the loop is confirmed solid.
        private void SetState(State newState)
        {
            if (newState != _state)
            {
                Debug.Log($"[Gatherer:{name}] {_state} -> {newState} (frame {Time.frameCount})");
            }
            _state = newState;
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
                SetState(State.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetNode.transform.position);
            if (distance <= interactionRange)
            {
                SetState(State.Gathering);
                return;
            }

            LogStuckPeriodically($"[Gatherer:{name}] MovingToNode, distance to node = {distance:F2}");
        }

        private void TickGathering()
        {
            if (_targetNode == null || _targetNode.IsDepleted)
            {
                SetState(_carriedAmount > 0f ? State.MovingToDropOff : State.Idle);
                return;
            }

            if (!WithinRange(_targetNode.transform.position))
            {
                _mover.MoveTo(_targetNode.transform.position);
                SetState(State.MovingToNode);
                return;
            }

            _carriedType = _targetNode.ResourceType;
            _carriedAmount += _targetNode.Harvest(gatherRate * Time.deltaTime);

            if (_carriedAmount >= carryCapacity)
            {
                SetState(State.MovingToDropOff);
            }
        }

        private void TickMovingToDropOff()
        {
            if (_dropOff == null)
            {
                _dropOff = FindNearestDropOff();
                if (_dropOff == null)
                {
                    LogStuckPeriodically($"[Gatherer:{name}] MovingToDropOff, no TownCenter found in Building.All");
                    return; // no drop-off exists yet; keep waiting
                }
                Debug.Log($"[Gatherer:{name}] heading to drop-off {_dropOff.name}");
                _mover.MoveTo(_dropOff.transform.position);
            }

            if (WithinRange(_dropOff.transform.position))
            {
                Deposit();
            }
            else
            {
                float distance = Vector3.Distance(transform.position, _dropOff.transform.position);
                LogStuckPeriodically($"[Gatherer:{name}] MovingToDropOff, distance to drop-off = {distance:F2}");
            }
        }

        private void LogStuckPeriodically(string message)
        {
            _stuckLogTimer += Time.deltaTime;
            if (_stuckLogTimer >= 1f)
            {
                _stuckLogTimer = 0f;
                Debug.Log(message);
            }
        }

        private void Deposit()
        {
            Debug.Log($"[Gatherer:{name}] depositing {_carriedAmount:F1} {_carriedType} at {_dropOff.name}");
            ResourceStockpile.Instance.Add(_carriedType, _carriedAmount);
            _carriedAmount = 0f;

            if (_targetNode != null && !_targetNode.IsDepleted)
            {
                Debug.Log($"[Gatherer:{name}] node still has resources, returning to it");
                _mover.MoveTo(_targetNode.transform.position);
                SetState(State.MovingToNode);
            }
            else
            {
                Debug.Log($"[Gatherer:{name}] node gone/depleted, no target, going idle");
                SetState(State.Idle);
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

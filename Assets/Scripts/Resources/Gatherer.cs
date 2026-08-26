using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Vfx;
using KingdomsOfBharat.Audio;

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
        private float _rateMultiplier = 1f;
        private float _carryCapacityMultiplier = 1f;
        private float _vfxTimer;
        private float _auraMultiplier = 1f;
        private float _auraCheckTimer;

        // For SelectedUnitPanel (UI) to show a status line - true for the
        // whole round trip (walking to the node, gathering, walking back),
        // matching AoE's convention of showing "Gathering" throughout.
        public bool IsWorking => _state != State.Idle;

        // For AnimationDriver: true only while actually in range and
        // harvesting, not during the walk there/back - IsWorking is too
        // broad for this (confirmed by testing: using it played the
        // gather/mine animation while still walking toward the node).
        public bool IsActivelyGathering => _state == State.Gathering;

        // For AnimationDriver to pick Mining vs. Gathering animation.
        public ResourceType? CurrentResourceType => _targetNode != null ? _targetNode.ResourceType : (ResourceType?)null;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        // Applied by WorkerFactory at spawn time from the worker's
        // civilization profile (e.g. Chola's faster gathering).
        public void SetRateMultiplier(float multiplier)
        {
            _rateMultiplier = multiplier;
        }

        // Phase 6 gap-close: same spawn-time-baked convention, this time
        // from EconomyTechProgress's ImprovedTools/PackMules techs rather
        // than the civ profile - non-retroactive like every other bonus in
        // this project, only benefits Workers trained after the tech
        // finishes.
        public void SetCarryCapacityMultiplier(float multiplier)
        {
            _carryCapacityMultiplier = multiplier;
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
            _auraCheckTimer -= Time.deltaTime;
            if (_auraCheckTimer <= 0f)
            {
                _auraCheckTimer = 0.5f;
                RefreshAuraMultiplier();
            }

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

        // For SelectedUnitPanel/tests to read the aura's current effect.
        public float AuraMultiplier => _auraMultiplier;

        // Polled rather than event-driven (see PillarEdictAura) - a worker
        // that walks out of a scholar's range simply reads 1x on its next
        // check, no explicit "left the aura" notification required. Public
        // so a test can force an immediate check instead of waiting on the
        // 0.5s timer.
        public void RefreshAuraMultiplier()
        {
            float multiplier = 1f;
            FactionId faction = MyFaction();

            foreach (PillarEdictAura aura in PillarEdictAura.All)
            {
                // A destroyed aura can briefly outlive its removal from
                // All (e.g. OnDisable ordering) - skip rather than throw.
                if (aura == null || aura.Faction != faction)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, aura.transform.position) <= PillarEdictAura.Radius)
                {
                    multiplier = PillarEdictAura.Multiplier;
                    break;
                }
            }

            _auraMultiplier = multiplier;
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
            _carriedAmount += _targetNode.Harvest(gatherRate * _rateMultiplier * _auraMultiplier * Time.deltaTime);

            _vfxTimer += Time.deltaTime;
            if (_vfxTimer >= 0.4f)
            {
                _vfxTimer = 0f;
                VfxFactory.SpawnBurst(_targetNode.transform.position + Vector3.up * 0.5f, new Color(0.7f, 0.6f, 0.4f), size: 0.1f, count: 3, speed: 0.6f, lifetime: 0.35f);
                SfxPlayer.PlayGather(_targetNode.transform.position);
            }

            if (_carriedAmount >= carryCapacity * _carryCapacityMultiplier)
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

        private Building FindNearestDropOff()
        {
            FactionId faction = MyFaction();
            Building nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!(building is TownCenter))
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

        // Now that Player and Enemy each have their own Town Center, a
        // plain nearest-distance search could hand a worker's load to the
        // wrong side's stockpile - this keeps drop-off (and the deposit
        // itself, above) scoped to the worker's own faction.
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

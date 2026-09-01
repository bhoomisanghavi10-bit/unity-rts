using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Vfx;
using KingdomsOfBharat.Audio;
using KingdomsOfBharat.Combat;

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
        [SerializeField] private float fleeDistance = 6f;

        private enum State { Idle, MovingToNode, Gathering, MovingToDropOff }

        private UnitMover _mover;
        private State _state = State.Idle;
        private ResourceNode _targetNode;
        private Building _dropOff;
        private Vector3 _dropOffApproachPoint;
        private ResourceType _carriedType;
        private float _carriedAmount;
        private float _rateMultiplier = 1f;
        private float _carryCapacityMultiplier = 1f;
        private float _vfxTimer;
        private float _auraMultiplier = 1f;
        private float _auraCheckTimer;
        private CombatResponse _combatResponse = CombatResponse.Fight;
        private Attackable _selfAttackable;
        private bool _subscribedToDamage;

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

        // Resolved lazily, not cached in Awake - same "sibling component may
        // not exist yet" gotcha documented on GarrisonPoint/Repairable/
        // MeleeAttacker.Self: an EditMode test's AddComponent<Gatherer>()
        // doesn't guarantee Awake has run on the RequireComponent-added
        // UnitMover before GatherFrom is called synchronously right after.
        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

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

        // Applied by WorkerFactory from WorkerCombatResponseDefaults - see
        // HandleDamaged for what each response actually does.
        public void SetCombatResponse(CombatResponse response)
        {
            _combatResponse = response;
        }

        public void GatherFrom(ResourceNode node)
        {
            _targetNode = node;
            _dropOff = null;
            Mover.MoveTo(node.transform.position);
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

        private void OnDestroy()
        {
            if (_subscribedToDamage && _selfAttackable != null)
            {
                _selfAttackable.OnDamaged -= HandleDamaged;
            }
        }

        // Roadmap Section 1 (worker self-defense/cross-awareness, AoE-parity
        // Phase 4.2): reacts to Attackable.OnDamaged. Only interrupts while
        // actively seeking/working a node (MovingToNode, Gathering) - a load
        // already being carried home (MovingToDropOff) finishes its trip
        // rather than losing it, same carve-out CancelGather already uses.
        // Internal (not private) so EditMode tests can call it directly
        // without needing to drive Attackable's event subscription timing -
        // see Assets/Scripts/AssemblyInfo.cs's InternalsVisibleTo grant.
        internal void HandleDamaged(Attackable attacker)
        {
            if (attacker == null || (_state != State.MovingToNode && _state != State.Gathering))
            {
                return;
            }

            _targetNode = null;
            _state = State.Idle;

            if (_combatResponse == CombatResponse.Fight)
            {
                if (TryGetComponent(out MeleeAttacker meleeAttacker))
                {
                    meleeAttacker.AttackMove(attacker);
                }
            }
            else
            {
                Mover.MoveTo(ComputeFleeDestination(transform.position, attacker.transform.position, fleeDistance));
            }
        }

        // Pure so it's directly EditMode-testable without a NavMeshAgent/
        // baked NavMesh (same reasoning as AcceptsDropOff's own pure-function
        // test coverage) - the actual pathing result is a live Play Mode
        // concern, this only has to prove the destination points the right
        // direction at the right distance.
        internal static Vector3 ComputeFleeDestination(Vector3 selfPosition, Vector3 attackerPosition, float distance)
        {
            Vector3 away = selfPosition - attackerPosition;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward;
            return selfPosition + away * distance;
        }

        private void Update()
        {
            // Subscribed lazily rather than in Awake - same sibling-
            // component-ordering gotcha as GarrisonPoint/Repairable's own
            // lazy resolution: WorkerFactory adds Gatherer before Attackable,
            // so an Awake-time GetComponent<Attackable> would find nothing.
            if (!_subscribedToDamage && TryGetComponent(out _selfAttackable))
            {
                _selfAttackable.OnDamaged += HandleDamaged;
                _subscribedToDamage = true;
            }

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
                Mover.MoveTo(_targetNode.transform.position);
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
                _dropOffApproachPoint = ComputeDropOffApproachPoint(_dropOff);
                Mover.MoveTo(_dropOffApproachPoint);
            }

            if (WithinRange(_dropOffApproachPoint))
            {
                Deposit();
            }
        }

        // The drop-off building's own footprint (BuildingFootprint.Attach)
        // carves a NavMeshObstacle over its footprint - the raw
        // transform.position used before this fix sits inside that
        // unwalkable space, which a NavMeshAgent can never actually reach.
        // GetNearestApproachPoint returns the nearest point on the
        // building's real walkable boundary instead, from whichever side
        // this worker is approaching from, plus a small buffer for the
        // worker's own NavMeshAgent radius so it doesn't clip the edge.
        // Falls back to the raw position for the (currently impossible,
        // since every *Factory tags its building) case of a drop-off with
        // no BuildingFootprintTag at all.
        private Vector3 ComputeDropOffApproachPoint(Building dropOff)
        {
            return dropOff.TryGetComponent(out BuildingFootprintTag footprintTag)
                ? footprintTag.GetNearestApproachPoint(transform.position, Mover.Radius + 0.1f)
                : dropOff.transform.position;
        }

        private void Deposit()
        {
            ResourceStockpile.For(MyFaction()).Add(_carriedType, _carriedAmount);
            _carriedAmount = 0f;

            if (_targetNode != null && !_targetNode.IsDepleted)
            {
                Mover.MoveTo(_targetNode.transform.position);
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
                if (!AcceptsDropOff(building, _carriedType))
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

        // Phase 3.1 (resource-specific drop-offs): TownCenter stays the
        // universal drop-off (AoE convention - it always accepts every
        // resource), but Lumber Camp/Mining Camp/Mill only accept their
        // own resource type, so a worker routes to whichever drop-off is
        // actually nearest for what it's carrying instead of always
        // defaulting to the TownCenter. Internal (not private) so
        // EditMode tests can exercise the routing rule directly without
        // driving a full Gatherer state machine - see AssemblyInfo.cs's
        // InternalsVisibleTo grant.
        internal static bool AcceptsDropOff(Building building, ResourceType type)
        {
            if (building is TownCenter)
            {
                return true;
            }

            switch (type)
            {
                case ResourceType.Wood:
                    return building is LumberCamp;
                case ResourceType.Gold:
                case ResourceType.Stone:
                    return building is MiningCamp;
                case ResourceType.Food:
                    return building is Mill;
                default:
                    return false;
            }
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

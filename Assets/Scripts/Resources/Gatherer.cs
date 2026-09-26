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

        // Explicit worker-order states (public for UI/tests/diagnostics).
        //  Idle -> MovingToResource -> Gathering -> MovingToDropOff -> (deposit)
        //  -> MovingToResource ... ; WaitingForDropOff while a full worker has
        //  no eligible drop-off; LastFailure records why an order ended badly.
        public enum WorkerOrderState { Idle, MovingToResource, Gathering, MovingToDropOff, WaitingForDropOff }

        private const float RetargetRadius = 30f;
        private const int MaxRecoveryAttempts = 2;

        private UnitMover _mover;
        private WorkerOrderState _state = WorkerOrderState.Idle;
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
        private readonly StuckWatchdog _watchdog = new StuckWatchdog();
        private int _attempts;
        private Vector3 _interactionPoint;
        private ResourceType _workType;
        private Vector3 _workPosition;
        private readonly System.Collections.Generic.HashSet<ResourceNode> _avoidNodes = new System.Collections.Generic.HashSet<ResourceNode>();
        private readonly System.Collections.Generic.HashSet<Building> _avoidDropOffs = new System.Collections.Generic.HashSet<Building>();
        private float _dropOffRecheck;
        private ResourceNode _resumeNode;
        private Attackable _resumeAttacker;
        private float _resumeDeadline;
        private const float ResumeWindowSeconds = 45f;
        public WorkerOrderState OrderState => _state;
        public ResourceNode TargetNode => _targetNode;
        public WorkerFailure LastFailure { get; private set; }
        // Why the worker last went Idle without a failure (diagnostics/tests).
        public string IdleReason { get; private set; } = "never started";
        public float CarriedAmount => _carriedAmount;
        public ResourceType CarriedType => _carriedType;

        // For SelectedUnitPanel (UI) to show a status line - true for the
        // whole round trip (walking to the node, gathering, walking back),
        // matching AoE's convention of showing "Gathering" throughout.
        public bool IsWorking => _state != WorkerOrderState.Idle;

        // For AnimationDriver: true only while actually in range and
        // harvesting, not during the walk there/back - IsWorking is too
        // broad for this (confirmed by testing: using it played the
        // gather/mine animation while still walking toward the node).
        public bool IsActivelyGathering => _state == WorkerOrderState.Gathering;

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
            if (node == null)
            {
                return;
            }

            _resumeNode = null;
            _resumeAttacker = null;
            _targetNode = node;
            _workType = node.ResourceType;
            _workPosition = node.transform.position;
            _dropOff = null;
            _attempts = 0;
            _avoidNodes.Clear();
            _avoidDropOffs.Clear();
            LastFailure = WorkerFailure.None;

            // Carrying a different resource: deliver that first, then
            // return to this node (Deposit resumes the target).
            if (_carriedAmount > 0f && _carriedType != node.ResourceType)
            {
                _state = WorkerOrderState.MovingToDropOff;
                _watchdog.Reset();
                return;
            }

            BeginMovingToResource();
        }

        // Interrupts gathering and stops walking. A carried load is kept
        // (the worker just stands there holding it) - the earlier "keep
        // walking to the drop-off" carve-out made a later move order strand
        // the worker in MovingToDropOff forever.
        public void CancelGather()
        {
            bool wasActive = _state != WorkerOrderState.Idle;
            _resumeNode = null;
            _resumeAttacker = null;
            _targetNode = null;
            _dropOff = null;
            _state = WorkerOrderState.Idle;
            IdleReason = "cancelled";
            if (wasActive)
            {
                Mover.Stop();
            }
        }

        // Fresh-order reset for rematch/load-style callers.
        public void ResetOrder()
        {
            _carriedAmount = 0f;
            _attempts = 0;
            LastFailure = WorkerFailure.None;
            _avoidNodes.Clear();
            _avoidDropOffs.Clear();
            CancelGather();
        }

        private void BeginMovingToResource()
        {
            _state = WorkerOrderState.MovingToResource;
            _watchdog.Reset();
            Vector3 point = WorkerNav.ClosestPoint(_targetNode, transform.position);
            if (Mover.TrySnap(point, 2.5f, out Vector3 snapped))
            {
                point = snapped;
            }

            _interactionPoint = point;
            Mover.MoveTo(point);
        }

        private void Fail(WorkerFailure failure, string detail)
        {
            LastFailure = failure;
            _targetNode = null;
            _dropOff = null;
            _state = WorkerOrderState.Idle;
            Mover.Stop();
            WorkerDiagnostics.Report(failure, name, detail);
        }

        // Resource gone (depleted/destroyed) or unreachable: continue with
        // the nearest reachable node of the same type, else stand down.
        private bool TryRetarget()
        {
            ResourceNode best = null;
            var candidates = new System.Collections.Generic.List<ResourceNode>();
            foreach (ResourceNode n in ResourceNode.All)
            {
                if (n != null && !n.IsDepleted && n.ResourceType == _workType && !_avoidNodes.Contains(n)
                    && Vector3.Distance(_workPosition, n.transform.position) <= RetargetRadius)
                {
                    candidates.Add(n);
                }
            }

            candidates.Sort((x, y) => Vector3.Distance(transform.position, x.transform.position)
                .CompareTo(Vector3.Distance(transform.position, y.transform.position)));

            for (int i = 0; i < candidates.Count && i < 4; i++)
            {
                if (Mover.CanReach(WorkerNav.ClosestPoint(candidates[i], transform.position)))
                {
                    best = candidates[i];
                    break;
                }
            }

            if (best == null)
            {
                return false;
            }

            int attempts = _attempts;
            GatherFrom(best);
            _attempts = attempts; // a retarget is not a fresh order for the retry budget
            return true;
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
            if (attacker == null || (_state != WorkerOrderState.MovingToResource && _state != WorkerOrderState.Gathering))
            {
                return;
            }

            // Remember what we were doing so the worker can go back to it
            // once the threat is over (see TickResumeAfterAttack).
            _resumeNode = _targetNode;
            _resumeAttacker = attacker;
            _resumeDeadline = Time.time + ResumeWindowSeconds;
            _targetNode = null;
            _state = WorkerOrderState.Idle;
            IdleReason = $"interrupted by attack from {attacker.name}";

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
            Step(Time.deltaTime);
        }

        // Time-injected body of Update so tests can drive the state machine.
        internal void Step(float dt)
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

            _auraCheckTimer -= dt;
            if (_auraCheckTimer <= 0f)
            {
                _auraCheckTimer = 0.5f;
                RefreshAuraMultiplier();
            }

            switch (_state)
            {
                case WorkerOrderState.MovingToResource:
                    TickMovingToNode(dt);
                    break;
                case WorkerOrderState.Gathering:
                    TickGathering(dt);
                    break;
                case WorkerOrderState.MovingToDropOff:
                    TickMovingToDropOff(dt);
                    break;
                case WorkerOrderState.WaitingForDropOff:
                    TickWaitingForDropOff(dt);
                    break;
                case WorkerOrderState.Idle:
                    TickResumeAfterAttack();
                    break;
            }
        }

        // A worker that was interrupted by an attack (fought back or fled)
        // goes back to its resource once the attacker is dead/gone, or - for
        // a fleeing worker - far enough away. Gives up after a window, and
        // any new order (GatherFrom/CancelGather) clears it.
        private void TickResumeAfterAttack()
        {
            if (_resumeAttacker == null && _resumeNode == null)
            {
                return;
            }

            if (Time.time > _resumeDeadline)
            {
                _resumeNode = null;
                _resumeAttacker = null;
                return;
            }

            bool threatOver = _resumeAttacker == null || _resumeAttacker.IsDead
                || (_combatResponse == CombatResponse.Flee
                    && Vector3.Distance(transform.position, _resumeAttacker.transform.position) > fleeDistance + 2f);
            if (!threatOver)
            {
                return;
            }

            if (TryGetComponent(out MeleeAttacker melee) && melee.IsAttacking)
            {
                return;
            }

            ResourceNode node = _resumeNode;
            _resumeNode = null;
            _resumeAttacker = null;
            if (node != null && !node.IsDepleted)
            {
                GatherFrom(node);
            }
            else if (_carriedAmount > 0f)
            {
                BeginDropOffTrip();
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

        private void TickMovingToNode(float dt)
        {
            if (_targetNode == null)
            {
                if (_carriedAmount > 0f)
                {
                    BeginDropOffTrip();
                }
                else if (!TryRetarget())
                {
                    _state = WorkerOrderState.Idle;
                    IdleReason = "resource vanished before arrival, no replacement";
                }
                return;
            }

            Vector3 edge = WorkerNav.ClosestPoint(_targetNode, transform.position);
            float distance = Vector3.Distance(transform.position, edge);
            if (distance <= interactionRange)
            {
                _state = WorkerOrderState.Gathering;
                return;
            }

            if (Mover.IsPathInvalid || _watchdog.Tick(dt, distance))
            {
                RecoverFromResourceStall();
            }
        }

        private void RecoverFromResourceStall()
        {
            _attempts++;
            _watchdog.Reset();

            if (_attempts == 1)
            {
                // Same node, freshly snapped approach point.
                BeginMovingToResource();
                return;
            }

            if (_attempts <= MaxRecoveryAttempts)
            {
                _avoidNodes.Add(_targetNode);
                if (TryRetarget())
                {
                    return;
                }
            }

            Fail(WorkerFailure.ResourceUnreachable, $"cannot reach {_workType} at {_workPosition}");
        }

        private void TickGathering(float dt)
        {
            if (_targetNode == null || _targetNode.IsDepleted)
            {
                _targetNode = null;
                if (_carriedAmount > 0f)
                {
                    BeginDropOffTrip();
                }
                else if (!TryRetarget())
                {
                    _state = WorkerOrderState.Idle;
                    IdleReason = "resource depleted, no replacement";
                }
                return;
            }

            if (Vector3.Distance(transform.position, WorkerNav.ClosestPoint(_targetNode, transform.position)) > interactionRange)
            {
                BeginMovingToResource();
                return;
            }

            _carriedType = _targetNode.ResourceType;
            _carriedAmount += _targetNode.Harvest(gatherRate * _rateMultiplier * _auraMultiplier * dt);

            _vfxTimer += dt;
            if (_vfxTimer >= 0.4f && _targetNode != null && Application.isPlaying)
            {
                _vfxTimer = 0f;
                VfxFactory.SpawnBurst(_targetNode.transform.position + Vector3.up * 0.5f, new Color(0.7f, 0.6f, 0.4f), size: 0.1f, count: 3, speed: 0.6f, lifetime: 0.35f);
                SfxPlayer.PlayGather(_targetNode.transform.position);
            }

            if (_carriedAmount >= carryCapacity * _carryCapacityMultiplier)
            {
                BeginDropOffTrip();
            }
        }

        private void BeginDropOffTrip()
        {
            _dropOff = null;
            _attempts = 0;
            _avoidDropOffs.Clear();
            _state = WorkerOrderState.MovingToDropOff;
            _watchdog.Reset();
        }

        private bool TryAssignDropOff()
        {
            _dropOff = FindNearestDropOff();
            if (_dropOff == null)
            {
                return false;
            }

            _dropOffApproachPoint = ComputeDropOffApproachPoint(_dropOff);
            Mover.MoveTo(_dropOffApproachPoint);
            _watchdog.Reset();
            return true;
        }

        private void TickMovingToDropOff(float dt)
        {
            // A destroyed drop-off reads as null here, so this also re-routes
            // when the target building dies mid-trip.
            if (_dropOff == null && !TryAssignDropOff())
            {
                _state = WorkerOrderState.WaitingForDropOff;
                _dropOffRecheck = 0f;
                if (LastFailure != WorkerFailure.NoDropOff)
                {
                    LastFailure = WorkerFailure.NoDropOff;
                    WorkerDiagnostics.Report(WorkerFailure.NoDropOff, name, $"carrying {_carriedAmount:0.#} {_carriedType} with no eligible drop-off");
                }
                return;
            }

            float distance = Vector3.Distance(transform.position, _dropOffApproachPoint);
            if (distance <= interactionRange)
            {
                Deposit();
                return;
            }

            if (Mover.IsPathInvalid || _watchdog.Tick(dt, distance))
            {
                RecoverFromDropOffStall();
            }
        }

        private void RecoverFromDropOffStall()
        {
            _attempts++;
            _watchdog.Reset();

            if (_attempts == 1)
            {
                _dropOffApproachPoint = ComputeDropOffApproachPoint(_dropOff);
                Mover.MoveTo(_dropOffApproachPoint);
                return;
            }

            if (_attempts <= MaxRecoveryAttempts)
            {
                _avoidDropOffs.Add(_dropOff);
                if (TryAssignDropOff())
                {
                    return;
                }
            }

            Fail(WorkerFailure.DropOffUnreachable, $"cannot reach a drop-off for {_carriedAmount:0.#} {_carriedType}");
        }

        // Full worker with no eligible drop-off: poll (not every frame)
        // until one is built, then resume the trip.
        private void TickWaitingForDropOff(float dt)
        {
            _dropOffRecheck -= dt;
            if (_dropOffRecheck > 0f)
            {
                return;
            }

            _dropOffRecheck = 1f;
            if (TryAssignDropOff())
            {
                LastFailure = WorkerFailure.None;
                _state = WorkerOrderState.MovingToDropOff;
            }
        }

        // The drop-off building's own footprint (BuildingFootprint.Attach)
        // carves a NavMeshObstacle over its footprint - the raw
        // transform.position sits inside that unwalkable space, which a
        // NavMeshAgent can never actually reach. GetNearestApproachPoint
        // returns the nearest point on the building's real walkable
        // boundary instead, from whichever side this worker is approaching
        // from, plus a small buffer for the agent's radius.
        private Vector3 ComputeDropOffApproachPoint(Building dropOff)
        {
            return dropOff.TryGetComponent(out BuildingFootprintTag footprintTag)
                ? footprintTag.GetNearestApproachPoint(transform.position, Mover.Radius + 0.1f)
                : dropOff.transform.position;
        }

        // Deposits the whole load exactly once, then resumes the previous
        // resource (or the nearest replacement) when there is one.
        private void Deposit()
        {
            ResourceStockpile stockpile = ResourceStockpile.For(MyFaction());
            if (stockpile != null && _carriedAmount > 0f)
            {
                stockpile.Add(_carriedType, _carriedAmount);
                _carriedAmount = 0f;
            }
            else if (stockpile == null)
            {
                Fail(WorkerFailure.NoDropOff, "no stockpile for this faction");
                return;
            }

            _dropOff = null;
            _attempts = 0;
            LastFailure = WorkerFailure.None;

            if (_targetNode != null && !_targetNode.IsDepleted)
            {
                BeginMovingToResource();
            }
            else
            {
                _targetNode = null;
                if (!TryRetarget())
                {
                    _state = WorkerOrderState.Idle;
                    IdleReason = "resource exhausted after deposit, no replacement";
                }
            }
        }

        private Building FindNearestDropOff()
        {
            FactionId faction = MyFaction();
            Building nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Building building in Building.All)
            {
                if (!AcceptsDropOff(building, _carriedType) || _avoidDropOffs.Contains(building))
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

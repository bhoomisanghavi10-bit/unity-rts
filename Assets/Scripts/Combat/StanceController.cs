using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Combat
{
    // AoE-style stance, simplified to the three that matter most:
    //  - Aggressive: auto-engages any hostile unit that wanders within range.
    //  - Defensive: same auto-engage, shorter range, and won't chase past a
    //    leash distance from its post (returns instead of running off).
    //  - StandGround: never auto-engages - today's original behavior,
    //    combat only happens on an explicit player order.
    public enum UnitStance
    {
        Aggressive,
        Defensive,
        StandGround,
    }

    // Layered on TOP of MeleeAttacker rather than modifying it: this only
    // ever calls AttackMove/CancelAttack, the same public entry points a
    // player's own right-click order uses, so it can never conflict with or
    // duplicate MeleeAttacker's own cooldown/targeting logic. While
    // MeleeAttacker already has a live target (player-ordered or
    // previously auto-engaged), this backs off entirely and does nothing -
    // an explicit order is never interrupted by a stance scan.
    //
    // Deliberately not added to Workers: an idle-between-gather-trips
    // Worker would otherwise get yanked into combat by Aggressive/Defensive
    // scanning regardless of its Gatherer assignment, since this component
    // has no notion of "busy with economy work." Only Soldiers/Archers get
    // one (see SoldierFactory/ArcherFactory).
    [RequireComponent(typeof(MeleeAttacker))]
    [RequireComponent(typeof(UnitMover))]
    public class StanceController : MonoBehaviour
    {
        [SerializeField] private UnitStance stance = UnitStance.Aggressive;
        [SerializeField] private float aggressiveRange = 8f;
        [SerializeField] private float defensiveRange = 4f;
        [SerializeField] private float leashDistance = 12f;
        [SerializeField] private float scanInterval = 0.5f;

        private MeleeAttacker _attacker;
        private UnitMover _mover;
        private FactionMember _faction;
        private bool _factionResolved;
        private Vector3 _post;
        private float _scanTimer;
        private bool _autoEngaged;

        public UnitStance Stance => stance;

        // Lazy, not eager in Awake: factories add FactionMember AFTER this
        // component (AddComponent<StanceController>() runs before
        // AddComponent<FactionMember>() in SoldierFactory/ArcherFactory),
        // and AddComponent fires Awake() synchronously - an eager
        // TryGetComponent here would always find nothing. Same shape as
        // Barracks/TownCenter's lazy FactionMember resolution.
        private FactionMember Faction
        {
            get
            {
                if (!_factionResolved)
                {
                    TryGetComponent(out _faction);
                    _factionResolved = true;
                }
                return _faction;
            }
        }

        public void SetStance(UnitStance newStance)
        {
            stance = newStance;
            if (stance == UnitStance.StandGround && _autoEngaged)
            {
                _attacker.CancelAttack();
                _autoEngaged = false;
            }
        }

        // Cycles Aggressive -> Defensive -> StandGround -> Aggressive, for a
        // single stance hotkey/button instead of three separate ones.
        public void CycleStance()
        {
            SetStance((UnitStance)(((int)stance + 1) % 3));
        }

        private void Awake()
        {
            _attacker = GetComponent<MeleeAttacker>();
            _mover = GetComponent<UnitMover>();
            _post = transform.position;
        }

        private void Update()
        {
            if (stance == UnitStance.StandGround)
            {
                return;
            }

            if (_attacker.IsAttacking)
            {
                if (_autoEngaged && stance == UnitStance.Defensive
                    && Vector3.Distance(transform.position, _post) > leashDistance)
                {
                    _attacker.CancelAttack();
                    _autoEngaged = false;
                    _mover.MoveTo(_post);
                }
                return;
            }

            _autoEngaged = false;
            _post = transform.position;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f)
            {
                return;
            }
            _scanTimer = scanInterval;

            float range = stance == UnitStance.Aggressive ? aggressiveRange : defensiveRange;
            Attackable target = FindNearestHostile(range);
            if (target == null)
            {
                return;
            }

            _attacker.AttackMove(target);
            _autoEngaged = true;
        }

        private Attackable FindNearestHostile(float range)
        {
            Attackable nearest = null;
            float bestDistance = range;

            foreach (Unit unit in Unit.All)
            {
                if (unit.gameObject == gameObject
                    || !unit.TryGetComponent(out Attackable candidate)
                    || candidate.IsDead
                    || !IsHostile(candidate))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        // Same fail-open convention as SelectionManager.IsHostileTarget: no
        // FactionMember on either side reads as hostile (matches TargetDummy
        // staying attackable/aggro-able regardless of the scanner's faction).
        private bool IsHostile(Attackable candidate)
        {
            if (!candidate.TryGetComponent(out FactionMember candidateFaction))
            {
                return true;
            }

            if (Faction == null)
            {
                return true;
            }

            return candidateFaction.Faction != Faction.Faction;
        }
    }
}

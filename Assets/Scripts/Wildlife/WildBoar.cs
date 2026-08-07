using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Wildlife
{
    // Dangerous wildlife (AoE-style boar, not a passive deer): wanders near
    // its spawn point, and fights back against any unit - Player's or the
    // AI's, no faction filter, wild animals are hostile to everyone - that
    // gets within aggroRange, but holds its ground rather than chasing
    // (doesn't move to close the distance itself - the human has to come
    // to it, and it stays put for the whole encounter once engaged).
    // Killing it (Attackable reaching zero HP) leaves behind a Food
    // carcass other units can gather from normally.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Attackable))]
    public class WildBoar : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 4f;
        [SerializeField] private float wanderInterval = 10f;
        // Chance, each time the wander timer fires, of the boar just
        // staying put instead of picking a new destination - the
        // difference between "occasionally roots around near its spawn"
        // and "runs somewhere new every few seconds," which read as
        // frantic once a real animated-pose model replaced the plain
        // capsule (a static model gets dragged along by NavMeshAgent with
        // no walk animation, so frequent repositioning looked like
        // sliding/darting rather than calm wandering).
        [SerializeField] private float idleChance = 0.6f;
        [SerializeField] private float aggroRange = 4f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private float attackInterval = 1.5f;
        [SerializeField] private float carcassFoodAmount = 60f;

        private NavMeshAgent _agent;
        private Attackable _selfAttackable;
        private Vector3 _homePosition;
        private float _wanderTimer;
        private float _attackCooldown;
        private Attackable _target;
        private bool _carcassSpawned;

        // For BoarAnimationDriver.
        public bool IsDead => _selfAttackable != null && _selfAttackable.IsDead;
        public bool IsAttacking { get; private set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _selfAttackable = GetComponent<Attackable>();
            _homePosition = transform.position;
        }

        private void Update()
        {
            // Checked every frame rather than via OnDestroy: TakeDamage
            // sets Health to 0 and calls Destroy(), but Destroy() doesn't
            // take effect until end of frame, so Update() is guaranteed to
            // see IsDead true at least once before removal - simpler and
            // more predictable than relying on OnDestroy() timing.
            if (_selfAttackable.IsDead)
            {
                if (!_carcassSpawned)
                {
                    _carcassSpawned = true;
                    CarcassFactory.Spawn(transform.position, carcassFoodAmount);
                }
                return;
            }

            // Recomputed every tick rather than only set true where damage
            // is dealt: this needs to stay true for the whole engagement
            // (Attack animation plays continuously), not just flicker on
            // for the instant a hit lands.
            IsAttacking = false;

            if (_target == null || _target.IsDead)
            {
                _target = FindTargetInRange();
            }

            if (_target != null)
            {
                Engage();
            }
            else
            {
                Wander();
            }
        }

        private void Engage()
        {
            // Holds ground rather than chasing - cancels any in-progress
            // wander movement the moment a target comes into range, and
            // never issues a new destination itself while engaged. If the
            // target isn't within attackRange yet, it just waits; the
            // human closing the distance (or not) decides what happens
            // next, not the boar.
            _agent.ResetPath();

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance > attackRange)
            {
                return;
            }

            IsAttacking = true;

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown <= 0f)
            {
                _target.TakeDamage(damage);
                _attackCooldown = attackInterval;
            }
        }

        private Attackable FindTargetInRange()
        {
            Attackable nearest = null;
            float bestDistance = aggroRange;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out Attackable attackable) || attackable.IsDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = attackable;
                }
            }

            return nearest;
        }

        private void Wander()
        {
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer > 0f)
            {
                return;
            }

            _wanderTimer = wanderInterval + Random.Range(-2f, 2f);

            if (Random.value < idleChance)
            {
                return;
            }

            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 target = _homePosition + new Vector3(offset.x, 0f, offset.y);
            _agent.SetDestination(target);
        }
    }
}

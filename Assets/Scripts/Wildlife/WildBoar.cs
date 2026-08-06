using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Wildlife
{
    // Dangerous wildlife (AoE-style boar, not a passive deer): wanders near
    // its spawn point, but breaks off to chase and attack any unit -
    // Player's or the AI's, no faction filter, wild animals are hostile to
    // everyone - that strays within aggroRange. Killing it (Attackable
    // reaching zero HP) leaves behind a Food carcass other units can
    // gather from normally.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Attackable))]
    public class WildBoar : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 6f;
        [SerializeField] private float wanderInterval = 4f;
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
            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance > attackRange)
            {
                _agent.SetDestination(_target.transform.position);
                return;
            }

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

            _wanderTimer = wanderInterval + Random.Range(-1f, 1f);
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 target = _homePosition + new Vector3(offset.x, 0f, offset.y);
            _agent.SetDestination(target);
        }
    }
}

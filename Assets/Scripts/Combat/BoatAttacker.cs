using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Combat
{
    // Item 49: naval combat, mirroring MeleeAttacker's walk-into-range/hit-
    // on-cooldown shape exactly - but MeleeAttacker is hard-wired to
    // UnitMover (RequireComponent + _mover.MoveTo), which a boat doesn't
    // have (see WaterMover), so this is a parallel component rather than a
    // shared/generalized one. Duplication over risking a change to
    // MeleeAttacker, which every land unit depends on.
    [RequireComponent(typeof(WaterMover))]
    public class BoatAttacker : MonoBehaviour
    {
        [SerializeField] private float damage = 8f;
        [SerializeField] private float attackRange = 4f;
        [SerializeField] private float attackInterval = 1.5f;
        [SerializeField] private DamageType damageType = DamageType.Pierce;
        [SerializeField] private UnitClass unitClass = UnitClass.Naval;

        private WaterMover _mover;
        private Attackable _target;
        private float _cooldown;
        private float _damageMultiplier = 1f;
        private float _damageBonus;

        public bool IsAttacking => _target != null;

        private void Awake()
        {
            _mover = GetComponent<WaterMover>();
        }

        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = multiplier;
        }

        public void SetBaseDamage(float newDamage)
        {
            damage = newDamage;
        }

        public void SetRange(float newRange)
        {
            attackRange = newRange;
        }

        public void SetDamageBonus(float bonus)
        {
            _damageBonus = bonus;
        }

        public void AttackMove(Attackable target)
        {
            _target = target;
            _cooldown = 0f;
            _mover.MoveTo(target.transform.position);
        }

        public void CancelAttack()
        {
            _target = null;
        }

        private void Update()
        {
            if (_target == null || _target.IsDead)
            {
                _target = null;
                return;
            }

            float distance = DistanceToTarget();
            if (distance > attackRange)
            {
                _mover.MoveTo(_target.transform.position);
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                float baseDamage = damage * _damageMultiplier + _damageBonus;
                float bonus = CombatBonus.Multiplier(unitClass, _target.Class);
                _target.TakeDamage(baseDamage * bonus, damageType);
                _cooldown = attackInterval;
            }
        }

        private float DistanceToTarget()
        {
            if (_target.TryGetComponent(out Collider targetCollider))
            {
                Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
                return Vector3.Distance(transform.position, closestPoint);
            }

            return Vector3.Distance(transform.position, _target.transform.position);
        }
    }
}

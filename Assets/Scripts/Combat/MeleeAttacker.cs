using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Combat
{
    // Simple melee combat: walk into range of a target, then hit it on a
    // cooldown until it dies or a new command is issued.
    [RequireComponent(typeof(UnitMover))]
    public class MeleeAttacker : MonoBehaviour
    {
        [SerializeField] private float damage = 5f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackInterval = 1f;

        private UnitMover _mover;
        private Attackable _target;
        private float _cooldown;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
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

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance > attackRange)
            {
                _mover.MoveTo(_target.transform.position);
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                _target.TakeDamage(damage);
                _cooldown = attackInterval;
            }
        }
    }
}

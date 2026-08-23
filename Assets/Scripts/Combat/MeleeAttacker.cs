using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Combat
{
    // Simple melee/ranged combat: walk into range of a target, then hit it
    // on a cooldown until it dies or a new command is issued. Also used for
    // Archers (see ArcherFactory) with a longer range and Pierce damage
    // type instead of duplicating this whole walk/cooldown/hit loop in a
    // second component - "Melee" in the name is a holdover from before
    // ranged units existed.
    [RequireComponent(typeof(UnitMover))]
    public class MeleeAttacker : MonoBehaviour
    {
        [SerializeField] private float damage = 5f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private DamageType damageType = DamageType.Melee;

        private UnitMover _mover;
        private Attackable _target;
        private float _cooldown;
        private float _damageMultiplier = 1f;
        private float _damageBonus;

        // For SelectedUnitPanel/HoverTooltip (UI) to show a status line.
        public bool IsAttacking => _target != null;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        // Applied by SoldierFactory at spawn time from the soldier's
        // civilization profile (e.g. Rajput's combat-power bonus).
        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = multiplier;
        }

        // Applied by WorkerFactory: unarmed workers can still fight back or
        // hunt (AoE-style villager combat), but noticeably weaker than a
        // dedicated Soldier's base damage.
        public void SetBaseDamage(float newDamage)
        {
            damage = newDamage;
        }

        // Applied by ArcherFactory: ranged units hit from further away and
        // deal Pierce instead of Melee damage, so armor resists them
        // differently (see Attackable.TakeDamage).
        public void SetRange(float newRange)
        {
            attackRange = newRange;
        }

        public void SetDamageType(DamageType newDamageType)
        {
            damageType = newDamageType;
        }

        // Applied by SoldierFactory/ArcherFactory at spawn time from
        // UpgradeProgress - a flat bonus baked in alongside the civ/age
        // multipliers, same "baked in at spawn, not retroactive" convention
        // documented on WorkerFactory.
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
                _target.TakeDamage(damage * _damageMultiplier + _damageBonus, damageType);
                _cooldown = attackInterval;
            }
        }

        // Distance to the target's collider SURFACE, not its transform
        // center - matters once buildings became attackable: a building's
        // footprint can be several units wide, so a soldier standing right
        // against its wall would otherwise still read as multiple units
        // away from its center and could never come "in range" at all.
        // Units are small/roughly centered on their own collider already,
        // so this doesn't meaningfully change unit-vs-unit combat.
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

using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Combat
{
    // Stationary ranged defense for Tower - same "scan, cooldown,
    // instant-hit" shape as StanceController's Aggressive stance, but for a
    // building with no UnitMover/NavMeshAgent to walk with, so it can't
    // reuse MeleeAttacker (which requires one). Attacks as UnitClass.Archer
    // for CombatBonus purposes (arrows from a tower, same as an Archer unit)
    // and deals Pierce damage - no travel-time projectile, matching
    // MeleeAttacker's own instant-hit combat.
    [RequireComponent(typeof(Attackable))]
    public class TowerAttacker : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 9f;
        [SerializeField] private float attackInterval = 1.2f;

        private FactionMember _faction;
        private bool _factionResolved;
        private float _cooldown;

        // For SelectedUnitPanel/HoverTooltip, same role as MeleeAttacker's.
        public bool IsAttacking { get; private set; }

        // Phase 6 gap-close: lets TowerFactory apply Vijayanagara's
        // "Towers get +1 attack range" bonus at spawn time - not
        // representable as a passiveBonuses StatModifier (no Building
        // entry in UnitCategory), so a hand-picked civ check at the
        // factory call site, same shape as Attackable.Configure.
        public void Configure(float newRange)
        {
            range = newRange;
        }

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

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            Attackable target = FindNearestHostile();
            IsAttacking = target != null;
            if (target == null)
            {
                return;
            }

            float bonus = CombatBonus.Multiplier(UnitClass.Archer, target.Class);
            target.TakeDamage(damage * bonus, DamageType.Pierce);
            _cooldown = attackInterval;
        }

        // Scans both Unit.All and Building.All - unlike StanceController
        // (units only, so a guarding soldier doesn't wander off to shoot a
        // building), a stationary tower has nothing better to do than also
        // threaten enemy buildings that wander into range.
        private Attackable FindNearestHostile()
        {
            Attackable nearest = null;
            float bestDistance = range;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out Attackable candidate) || candidate.IsDead || !IsHostile(candidate))
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

            foreach (Building building in Building.All)
            {
                if (!building.TryGetComponent(out Attackable candidate) || candidate.IsDead || !IsHostile(candidate))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

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

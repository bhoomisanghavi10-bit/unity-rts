using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;

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
        [SerializeField] private UnitClass unitClass = UnitClass.Infantry;
        [SerializeField] private float splashRadius;
        // Item 3 (Area of Effect / Trample): defaults to 1f so Siege's own
        // single-arg SetSplashRadius(2.25f) call keeps splash victims at
        // full primary-hit damage, byte-for-byte unchanged. Cavalry's
        // trample uses a reduced value here instead - see CavalryFactory.
        [SerializeField] private float splashDamageMultiplier = 1f;

        private UnitMover _mover;
        private Attackable _self;
        private Attackable _target;
        private FactionMember _faction;
        private bool _factionResolved;
        private float _cooldown;
        private float _damageMultiplier = 1f;
        private float _damageBonus;
        private readonly List<Attackable> _splashBuffer = new List<Attackable>();

        // For SelectedUnitPanel/HoverTooltip (UI) to show a status line.
        public bool IsAttacking => _target != null;

        // Resolved lazily, not cached in Awake - same "sibling component
        // may not exist yet" gotcha documented on GarrisonPoint/Repairable
        // (factories add Attackable after MeleeAttacker in several cases;
        // an EditMode test's AddComponent<MeleeAttacker>() doesn't
        // guarantee Awake has run on the RequireComponent-added UnitMover
        // before AttackMove is called synchronously right after).
        private Attackable Self => _self != null ? _self : (_self = GetComponent<Attackable>());
        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

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

        // For CombatBonus's counter matrix - what class of attacker this
        // is, looked up against the target's own Attackable.Class when a
        // hit lands. Defaults to Infantry (Worker/Soldier); ArcherFactory
        // overrides it to Archer.
        public void SetUnitClass(UnitClass newUnitClass)
        {
            unitClass = newUnitClass;
        }

        // Applied by SoldierFactory/ArcherFactory at spawn time from
        // UpgradeProgress - a flat bonus baked in alongside the civ/age
        // multipliers, same "baked in at spawn, not retroactive" convention
        // documented on WorkerFactory.
        public void SetDamageBonus(float bonus)
        {
            _damageBonus = bonus;
        }

        // Applied by SiegeFactory only (Roadmap Section 1 Phase 2.3 -
        // AoE-parity execution plan) - every other MeleeAttacker user
        // keeps the default 0 (disabled), so Soldier/Archer/Cavalry/
        // Spearman/Worker behavior is unchanged. A hit also damages any
        // other hostile Unit/Building within this radius of the primary
        // target's position (the impact point, not the Siege unit's own
        // position) - this is what makes Line vs. Staggered formation
        // spacing actually matter in combat, not just cosmetically
        // rearrange units on a move order.
        // Item 3 (Area of Effect / Trample): damageMultiplier defaults to
        // 1f (full primary-hit damage), matching every existing call site
        // before this parameter existed - only Cavalry passes a reduced
        // value, keeping its trample a minor secondary effect rather than
        // Siege-tier splash.
        public void SetSplashRadius(float radius, float damageMultiplier = 1f)
        {
            splashRadius = radius;
            splashDamageMultiplier = damageMultiplier;
        }

        public void AttackMove(Attackable target)
        {
            _target = target;
            _cooldown = 0f;
            Mover.MoveTo(target.transform.position);
        }

        public void CancelAttack()
        {
            _target = null;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Internal (not private) so EditMode tests can drive combat
        // resolution directly with an explicit deltaTime instead of
        // depending on Unity's Update loop actually ticking - same
        // convention as BuildingAttacker/Repairable/ConstructionSite's own
        // Tick/EnsureInitialized (see AssemblyInfo.cs's InternalsVisibleTo
        // grant). Pure refactor of the previous Update() body otherwise -
        // no behavior change for any existing (non-splash) caller.
        internal void Tick(float deltaTime)
        {
            if (_target == null || _target.IsDead)
            {
                _target = null;
                return;
            }

            float distance = DistanceToTarget();
            if (distance > attackRange)
            {
                Mover.MoveTo(_target.transform.position);
                return;
            }

            _cooldown -= deltaTime;
            if (_cooldown <= 0f)
            {
                ResolveHit(_target);
                if (splashRadius > 0f)
                {
                    ResolveSplash(_target);
                }
                _cooldown = attackInterval;
            }
        }

        // extraMultiplier defaults to 1f for the primary-target call site in
        // Tick() (unaffected by this item's own addition); ResolveSplash
        // below passes splashDamageMultiplier instead.
        private void ResolveHit(Attackable victim, float extraMultiplier = 1f)
        {
            float baseDamage = damage * _damageMultiplier + _damageBonus;
            float bonus = CombatBonus.Multiplier(unitClass, victim.Class);
            // Roadmap Section 5 item 3: a Durg Garrison unit inside this
            // building strips Siege's usual 3x anti-building bonus down to
            // a flat 1x - see Attackable.SiegeImmune.
            if (unitClass == UnitClass.Siege && victim.SiegeImmune)
            {
                bonus = 1f;
            }
            victim.TakeDamage(baseDamage * bonus * extraMultiplier, damageType, Self);
        }

        // Scans both Unit.All and Building.All - same registries and
        // hostile-filter convention as BuildingAttacker.FindNearestHostiles
        // (splash should hit a nearby enemy building same as a nearby
        // enemy unit, since Siege's whole job is anti-building and
        // CombatBonus's Siege->Building bonus should apply per splash
        // victim too). Centered on the primary target's position (the
        // impact point), not this unit's own position.
        private void ResolveSplash(Attackable primaryTarget)
        {
            _splashBuffer.Clear();
            Vector3 impactPoint = primaryTarget.transform.position;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out Attackable candidate) || candidate == primaryTarget || candidate.IsDead)
                {
                    continue;
                }

                if (HostileFilter.IsHostile(candidate, Faction) && Vector3.Distance(impactPoint, unit.transform.position) <= splashRadius)
                {
                    _splashBuffer.Add(candidate);
                }
            }

            foreach (Building building in Building.All)
            {
                if (!building.TryGetComponent(out Attackable candidate) || candidate == primaryTarget || candidate.IsDead)
                {
                    continue;
                }

                if (HostileFilter.IsHostile(candidate, Faction) && Vector3.Distance(impactPoint, building.transform.position) <= splashRadius)
                {
                    _splashBuffer.Add(candidate);
                }
            }

            foreach (Attackable victim in _splashBuffer)
            {
                ResolveHit(victim, splashDamageMultiplier);
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

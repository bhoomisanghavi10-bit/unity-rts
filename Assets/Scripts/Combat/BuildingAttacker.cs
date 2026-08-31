using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Combat
{
    // Stationary ranged defense for a building with no UnitMover/
    // NavMeshAgent to walk with, so it can't reuse MeleeAttacker (which
    // requires one) - same "scan, cooldown, instant-hit" shape as
    // StanceController's Aggressive stance. Originally Tower-only
    // (TowerAttacker, see git history); generalized 2026-09-01 for the
    // general garrisoning system (Roadmap Section 1) so TownCenter can
    // share the exact same scan-and-shoot logic instead of a duplicated
    // copy - TownCenter had no Attacker at all before this item. Attacks
    // as UnitClass.Archer for CombatBonus purposes (arrows/bolts from a
    // fortification) and deals Pierce damage - no travel-time projectile,
    // matching MeleeAttacker's own instant-hit combat.
    [RequireComponent(typeof(Attackable))]
    public class BuildingAttacker : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 9f;
        [SerializeField] private float attackInterval = 1.2f;
        [SerializeField] private int maxBonusShots;

        private GarrisonPoint _garrisonPoint;
        private FactionMember _faction;
        private bool _factionResolved;
        private float _cooldown;
        private readonly List<Attackable> _hostileBuffer = new List<Attackable>();
        private readonly List<(Attackable target, float distance)> _candidateBuffer = new List<(Attackable, float)>();

        // For SelectedUnitPanel/HoverTooltip, same role as MeleeAttacker's.
        public bool IsAttacking { get; private set; }

        // Phase 6 gap-close: lets TowerFactory apply Vijayanagara's
        // "Towers get +1 attack range" bonus at spawn time - not
        // representable as a passiveBonuses StatModifier (no Building
        // entry in UnitCategory), so a hand-picked civ check at the
        // factory call site, same shape as Attackable.Configure. Kept as
        // its own method (rather than folded into ConfigureStats below)
        // since Tower only ever needs to override range, never
        // damage/interval.
        public void Configure(float newRange)
        {
            range = newRange;
        }

        // For TownCenterFactory, whose baseline damage/range/interval are
        // all different from Tower's serialized defaults.
        public void ConfigureStats(float newDamage, float newRange, float newAttackInterval)
        {
            damage = newDamage;
            range = newRange;
            attackInterval = newAttackInterval;
        }

        // General garrisoning system (2026-09-01): each garrisoned unit in
        // garrisonPoint adds one extra simultaneous shot per attack
        // interval, up to maxBonusShots - the AoE IV "murder holes"
        // mechanic (more garrisoned units = more simultaneous arrows at
        // potentially different targets, not a flat damage multiplier).
        public void ConfigureGarrisonBonus(GarrisonPoint garrisonPoint, int newMaxBonusShots)
        {
            _garrisonPoint = garrisonPoint;
            maxBonusShots = newMaxBonusShots;
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
            Tick(Time.deltaTime);
        }

        // Internal (not private) so EditMode tests can drive this
        // directly with an explicit deltaTime instead of depending on
        // Unity's Update loop actually ticking - same convention as
        // Repairable/ConstructionSite's own Tick/EnsureInitialized (see
        // Assets/Scripts/AssemblyInfo.cs for the InternalsVisibleTo grant).
        internal void Tick(float deltaTime)
        {
            _cooldown -= deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            int shotCount = 1 + Mathf.Min(_garrisonPoint != null ? _garrisonPoint.Count : 0, maxBonusShots);
            FindNearestHostiles(shotCount, _hostileBuffer);
            IsAttacking = _hostileBuffer.Count > 0;
            if (!IsAttacking)
            {
                return;
            }

            foreach (Attackable target in _hostileBuffer)
            {
                float bonus = CombatBonus.Multiplier(UnitClass.Archer, target.Class);
                target.TakeDamage(damage * bonus, DamageType.Pierce);
            }

            _cooldown = attackInterval;
        }

        // Scans both Unit.All and Building.All - unlike StanceController
        // (units only, so a guarding soldier doesn't wander off to shoot a
        // building), a stationary defensive structure has nothing better
        // to do than also threaten enemy buildings that wander into range.
        // Fills `results` with up to `count` distinct hostiles, nearest
        // first - generalized from TowerAttacker's original
        // single-best-candidate scan so multiple simultaneous "arrows"
        // (see shotCount above) can each hit a different target instead
        // of piling every bonus shot onto the same one.
        private void FindNearestHostiles(int count, List<Attackable> results)
        {
            results.Clear();
            _candidateBuffer.Clear();
            if (count <= 0)
            {
                return;
            }

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out Attackable candidate) || candidate.IsDead || !IsHostile(candidate))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < range)
                {
                    _candidateBuffer.Add((candidate, distance));
                }
            }

            foreach (Building building in Building.All)
            {
                if (!building.TryGetComponent(out Attackable candidate) || candidate.IsDead || !IsHostile(candidate))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance < range)
                {
                    _candidateBuffer.Add((candidate, distance));
                }
            }

            _candidateBuffer.Sort((a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < _candidateBuffer.Count && i < count; i++)
            {
                results.Add(_candidateBuffer[i].target);
            }
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

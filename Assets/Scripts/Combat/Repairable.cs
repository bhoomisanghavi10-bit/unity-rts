using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Combat
{
    // Target-side half of repair (worker mechanics audit, 2026-08-29): a
    // damaged building/ship/siege unit a Repairer worker can walk up to and
    // pay Wood to heal, restoring HP over time proportional to resources
    // spent - AoE reference behavior. Mirrors Builder/ConstructionSite's
    // worker-side/target-side split exactly, and reuses
    // ConstructionSite.SpeedMultiplier for multiple simultaneous repairers
    // instead of duplicating that diminishing-returns formula.
    //
    // This project doesn't retain each building/unit instance's original
    // build/train cost at runtime (those consts live only in
    // BuildingPlacer/Barracks/Dock and are spent once at creation, never
    // stored on the spawned object) - deriving an exact "half of original
    // cost" per instance would mean threading cost data through every
    // factory's Place/Spawn signature. Instead this charges a flat
    // Wood-per-HP rate keyed off Attackable.Class - a deliberate, disclosed
    // approximation (Building cheapest, Naval mid, Siege priciest), not an
    // exact per-instance figure, same spirit as this project's other
    // documented compromises (e.g. Maurya's neutral-gray tint).
    [RequireComponent(typeof(Attackable))]
    public class Repairable : MonoBehaviour
    {
        private const float RepairRatePerSecond = 12f;

        private const float BuildingWoodCostPerHp = 0.4f;
        private const float NavalWoodCostPerHp = 0.6f;
        private const float SiegeWoodCostPerHp = 1.2f;
        private const float DefaultWoodCostPerHp = 0.5f;

        private Attackable _attackable;
        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;

        private int _activeRepairers;

        // Lazily resolved rather than cached in Awake - EditMode tests
        // don't reliably run Awake synchronously right after AddComponent
        // (see ConstructionSiteTests' own comment on this exact quirk), and
        // production code has the same "AddComponent ordering hazard"
        // Barracks/Dock already work around for Site/FactionMember (a
        // Factory can add Repairable before FactionMember exists yet).
        private Attackable Target
        {
            get
            {
                if (_attackable == null)
                {
                    TryGetComponent(out _attackable);
                }
                return _attackable;
            }
        }

        private ConstructionSite Site
        {
            get
            {
                if (!_siteResolved)
                {
                    TryGetComponent(out _site);
                    _siteResolved = true;
                }
                return _site;
            }
        }

        private FactionId Faction
        {
            get
            {
                if (_factionMember == null)
                {
                    TryGetComponent(out _factionMember);
                }
                return _factionMember != null ? _factionMember.Faction : FactionId.Player;
            }
        }

        // For SelectedUnitPanel/HoverTooltip (via UnitStatus) and
        // SelectionManager's right-click gating.
        public bool IsRepairable =>
            Target != null && !Target.IsDead && Target.Health < Target.MaxHealth
            && (Site == null || Site.IsComplete);

        public void BeginRepair()
        {
            _activeRepairers++;
        }

        public void StopRepair()
        {
            _activeRepairers = Mathf.Max(0, _activeRepairers - 1);
        }

        // Internal so tests can assert against the real per-class rate
        // instead of duplicating these constants.
        internal float WoodCostPerHp()
        {
            switch (Target.Class)
            {
                case UnitClass.Building:
                    return BuildingWoodCostPerHp;
                case UnitClass.Naval:
                    return NavalWoodCostPerHp;
                case UnitClass.Siege:
                    return SiegeWoodCostPerHp;
                default:
                    return DefaultWoodCostPerHp;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Internal (not private) so EditMode tests can drive this directly
        // with an explicit deltaTime instead of depending on Unity's Update
        // loop actually ticking - same convention as ConstructionSite's own
        // EnsureInitialized/BuildingPlacer.BuildingKind (see
        // Assets/Scripts/AssemblyInfo.cs for the InternalsVisibleTo grant).
        internal void Tick(float deltaTime)
        {
            if (_activeRepairers <= 0 || !IsRepairable)
            {
                return;
            }

            float hpToRestore = RepairRatePerSecond * ConstructionSite.SpeedMultiplier(_activeRepairers) * deltaTime;
            hpToRestore = Mathf.Min(hpToRestore, Target.MaxHealth - Target.Health);
            if (hpToRestore <= 0f)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            float cost = hpToRestore * WoodCostPerHp();
            if (stockpile == null || stockpile.GetTotal(ResourceType.Wood) < cost)
            {
                // Can't afford this tick's worth - stall silently rather
                // than partially heal/spend or error; tries again next
                // tick once the stockpile can afford it.
                return;
            }

            stockpile.Add(ResourceType.Wood, -cost);
            Target.Heal(hpToRestore);
        }
    }
}

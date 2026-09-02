using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // Passively generates Food for whichever faction owns it, but only
    // while at least one worker is staffed here (FarmWorker.StaffAt) - the
    // same "needs an active worker" shape as ConstructionSite/Builder.
    // Deliberately slower than gathering directly from a resource node
    // (farmland/fruit bush/hunted carcass) - the tradeoff is that a Farm
    // can be built anywhere, anytime, rather than depending on the map's
    // fixed resource scatter.
    //
    // ConstructionSite/FactionMember resolved lazily, same reasoning as
    // Barracks: a spawner adds these one AddComponent call at a time, and
    // an external caller (e.g. a future AI economy) could query this
    // Farm within the same frame it was created, before Awake()/Start()
    // for this component has necessarily run.
    public class Farm : Building
    {
        [SerializeField] private float foodPerSecondPerWorker = 0.6f;
        // Item 5 (Renewable Resource, docs/PARTIAL_ELEMENTS_FIX_PLAN.md):
        // 175 matches AoE II's own Dark-Age Farm Food value. A staffed Farm
        // used to produce Food forever with no cap - a genuinely different
        // economy model than AoE's actual (finite, depleting, reseedable)
        // Farm, confirmed by reading this file directly rather than
        // trusting the "unconfirmed" label, then retrofitted per the
        // user's explicit choice of the larger, AoE-accurate option.
        [SerializeField] private float maxFood = 175f;
        [SerializeField] private float reseedRatePerSecond = 15f;
        // Matches BuildingPlacer.farmWoodCost - reseeding a fully-depleted
        // Farm from empty costs exactly what building a fresh one costs,
        // the same "reseed = rebuild" logic AoE itself uses.
        private const float FullReseedWoodCost = 60f;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private int _activeWorkers;
        private int _activeReseeders;
        private float _remainingFood;
        private bool _initialized;

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
                    _factionMember = GetComponent<FactionMember>();
                }
                return _factionMember.Faction;
            }
        }

        public bool IsComplete => Site == null || Site.IsComplete;

        // Lazily resolved rather than set at field-declaration time - same
        // "AddComponent ordering hazard" ConstructionSite/Repairable's own
        // EnsureInitialized already guards against (a factory can query
        // RemainingFood the instant after AddComponent<Farm>(), before this
        // component's own Awake would otherwise have run).
        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _remainingFood = maxFood;
            _initialized = true;
        }

        public float RemainingFood
        {
            get
            {
                EnsureInitialized();
                return _remainingFood;
            }
        }

        public float MaxFood => maxFood;

        public bool IsDepleted => IsComplete && RemainingFood <= 0f;

        // Internal so tests can assert against the real rate instead of
        // duplicating FullReseedWoodCost/maxFood - same convention
        // Repairable.WoodCostPerHp() already documents doing.
        internal float WoodCostPerFood => FullReseedWoodCost / maxFood;

        public void BeginWorking()
        {
            _activeWorkers++;
        }

        public void StopWorking()
        {
            _activeWorkers = Mathf.Max(0, _activeWorkers - 1);
        }

        // Item 5 (Renewable Resource): worker-side mirror of BeginWorking/
        // StopWorking, for FarmWorker's reseed mode.
        public void BeginReseed()
        {
            _activeReseeders++;
        }

        public void StopReseed()
        {
            _activeReseeders = Mathf.Max(0, _activeReseeders - 1);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Internal (not private) so EditMode tests can drive this directly
        // with an explicit deltaTime instead of depending on Unity's Update
        // loop actually ticking - same convention as Repairable.Tick/
        // ConstructionSite's own EnsureInitialized (see AssemblyInfo.cs's
        // InternalsVisibleTo grant).
        internal void Tick(float deltaTime)
        {
            EnsureInitialized();

            if (!IsComplete)
            {
                return;
            }

            if (_activeWorkers > 0 && _remainingFood > 0f)
            {
                float amount = Mathf.Min(foodPerSecondPerWorker * _activeWorkers * deltaTime, _remainingFood);
                ResourceStockpile.For(Faction).Add(ResourceType.Food, amount);
                _remainingFood -= amount;
            }

            if (_activeReseeders > 0 && _remainingFood < maxFood)
            {
                float foodToRestore = Mathf.Min(
                    reseedRatePerSecond * ConstructionSite.SpeedMultiplier(_activeReseeders) * deltaTime,
                    maxFood - _remainingFood);
                if (foodToRestore <= 0f)
                {
                    return;
                }

                ResourceStockpile stockpile = ResourceStockpile.For(Faction);
                float cost = foodToRestore * WoodCostPerFood;
                if (stockpile == null || stockpile.GetTotal(ResourceType.Wood) < cost)
                {
                    // Can't afford this tick's worth - stall silently
                    // rather than partially restore/spend, same as
                    // Repairable.Tick's own affordability stall.
                    return;
                }

                stockpile.Add(ResourceType.Wood, -cost);
                _remainingFood += foodToRestore;
            }
        }
    }
}

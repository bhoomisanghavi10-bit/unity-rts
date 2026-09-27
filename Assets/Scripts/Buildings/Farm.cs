using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
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
        // Farming upgrade techs (Horse Collar/Heavy Plow/Crop Rotation):
        // how much of FarmTechProgress's live bonus this specific Farm
        // instance has already absorbed - tracked per-instance (not just
        // read fresh each tick) so a newly-completed tech's bonus can be
        // credited exactly once, proportionally, to every already-built
        // Farm, per the AoE reference's own worked example.
        private float _lastAppliedTechBonus;

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
        //
        // Starts at the FULL effective max (base + whatever farming-tech
        // bonus is already researched), not just the base - matches the
        // reference's "if the research is completed while a Farm is being
        // built, the full food bonus applies to that Farm" rule for free,
        // with no special-case code: a brand-new Farm just starts caught up.
        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _lastAppliedTechBonus = FarmTechProgress.MaxFoodBonus(Faction);
            _remainingFood = maxFood + _lastAppliedTechBonus;
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

        // Base 175 (AoE II's own Dark-Age value) plus whatever farming
        // upgrade techs this Farm's faction has researched (Horse Collar/
        // Heavy Plow/Crop Rotation - see FarmTechProgress), reaching 550 at
        // Imperial with all 3, matching the reference exactly.
        public float MaxFood => maxFood + FarmTechProgress.MaxFoodBonus(Faction);

        public bool IsDepleted => IsComplete && RemainingFood <= 0f;

        // Internal so tests can assert against the real rate instead of
        // duplicating FullReseedWoodCost/maxFood - same convention
        // Repairable.WoodCostPerHp() already documents doing.
        internal float WoodCostPerFood => FullReseedWoodCost / maxFood;

        // AoE reference: "Farms may only be gathered from by one Villager at
        // a time." - refuses a second worker outright (same-faction or not)
        // rather than the old uncapped counter that let food rate stack
        // linearly. "If a Farm is not currently being tended, another
        // player's Villager can capture it by simply starting to gather
        // from it" - a successful claim by a different faction reassigns
        // ownership via the same FactionMember.Configure mechanism
        // PurohitaConverter's conversion already uses; remaining food
        // carries over unchanged. No live re-tint of the model on capture -
        // matches this project's own already-documented, accepted gap for
        // unit conversion (no re-tint system exists yet).
        public bool BeginWorking(FactionId workerFaction)
        {
            if (_activeWorkers > 0)
            {
                return false;
            }

            if (workerFaction != Faction)
            {
                _factionMember.Configure(workerFaction);
            }

            _activeWorkers = 1;
            return true;
        }

        public void StopWorking()
        {
            _activeWorkers = Mathf.Max(0, _activeWorkers - 1);
        }

        // Only meaningful once IsComplete (a foundation is never a valid
        // capture/staff target - that's ConstructionSite's own job).
        public bool IsCapturable => IsComplete && _activeWorkers == 0;

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

            // Retroactive proportional top-up (Horse Collar/Heavy Plow/Crop
            // Rotation): reference - "Farms built before upgrades are
            // researched are affected proportionally... using the square of
            // the proportion of remaining food." Runs regardless of
            // IsComplete (a tech completing mid-construction should still
            // land on the foundation, same as the reference's own
            // full-bonus-while-being-built rule - proportion is 1.0 for an
            // untouched foundation anyway, so this naturally credits the
            // full delta in that case).
            float currentTechBonus = FarmTechProgress.MaxFoodBonus(Faction);
            if (currentTechBonus > _lastAppliedTechBonus)
            {
                float bonusDelta = currentTechBonus - _lastAppliedTechBonus;
                float priorEffectiveMax = maxFood + _lastAppliedTechBonus;
                float proportionRemaining = priorEffectiveMax > 0f
                    ? Mathf.Clamp01(_remainingFood / priorEffectiveMax)
                    : 0f;
                _remainingFood += bonusDelta * proportionRemaining * proportionRemaining;
                _lastAppliedTechBonus = currentTechBonus;
            }

            if (!IsComplete)
            {
                return;
            }

            float effectiveMaxFood = maxFood + currentTechBonus;

            // AoE reference: "Farms may only be gathered from by one
            // Villager at a time" - _activeWorkers is now 0-or-1 (see
            // BeginWorking), so this is always either "no one working it"
            // or "the one Villager working it," never a stacked rate.
            if (_activeWorkers > 0 && _remainingFood > 0f)
            {
                float amount = Mathf.Min(foodPerSecondPerWorker * deltaTime, _remainingFood);
                ResourceStockpile.For(Faction).Add(ResourceType.Food, amount);
                _remainingFood -= amount;
            }

            // Mill-queued auto-reseed: a faction with the toggle on (see
            // MillAutoReseedRegistry/Mill.RequestToggleAutoReseed) reseeds
            // any of its own depleted, worker-less Farms automatically, at
            // the same rate a single manually-assigned reseeder would -
            // matches the reference's "Farms can be automatically reseeded
            // if they are queued in the Mill (or equivalent)" without
            // needing a real per-Farm queue/linkage system.
            bool reseedRequested = _activeReseeders > 0 || MillAutoReseedRegistry.IsEnabled(Faction);
            int effectiveReseeders = Mathf.Max(_activeReseeders, reseedRequested ? 1 : 0);
            if (reseedRequested && _remainingFood < effectiveMaxFood)
            {
                float foodToRestore = Mathf.Min(
                    reseedRatePerSecond * ConstructionSite.SpeedMultiplier(effectiveReseeders) * deltaTime,
                    effectiveMaxFood - _remainingFood);
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

using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Converts Wood/Food/Stone <-> Gold for whichever faction owns this
    // Market, at a lossy AoE-style spread: selling pays out sellRate (70%)
    // of a unit's nominal Gold value, buying costs buyRate (130%) of it -
    // the 60-point gap is deliberate friction against spamming trades to
    // dodge a resource shortage instead of gathering/building for it.
    // Nominal value is a flat 1 Gold per unit for every resource - there's
    // no reason yet for Wood to be worth more/less than Stone, and no
    // fluctuating-price system (real AoE's Market has one) since nothing
    // in this project creates the kind of long, contested game a moving
    // price would matter for.
    public class Market : Building
    {
        [SerializeField] private float sellRate = 0.7f;
        [SerializeField] private float buyRate = 1.3f;

        // Wave 4 item 26: Vanik trains here, not Barracks - Market's own
        // unit. Single-slot queue, same shape as Dock's own training
        // (TrainingUnit enum with one value, since only one unit type
        // trains from a Market).
        [SerializeField] private float vanikWoodCost = 80f;
        [SerializeField] private float vanikGoldCost = 20f;
        [SerializeField] private float trainTime = 20f;
        [SerializeField] private float rallyDistance = 3f;
        private Vector3 _rallyOffset;
        private RallyPoint _rally;
        private float _remaining = -1f;

        private FactionMember _factionMember;

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

        private void Awake()
        {
            // Markets aren't water-adjacency-sensitive like Docks (see
            // Dock.Awake's WaterProximity-based direction) - a plain
            // forward offset is enough for a land rally point.
            _rallyOffset = transform.forward * rallyDistance;
            _rally = gameObject.AddComponent<RallyPoint>();
            _rally.Configure(_rallyOffset);
        }

        public bool IsComplete => !TryGetComponent(out ConstructionSite site) || site.IsComplete;
        public bool IsTraining => _remaining >= 0f;

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
            }
        }

        public void RequestTrainVanik()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Wood) < vanikWoodCost
                || stockpile.GetTotal(ResourceType.Gold) < vanikGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -vanikWoodCost);
            stockpile.Add(ResourceType.Gold, -vanikGoldCost);
            _remaining = ScaledTrainTime();
        }

        private float ScaledTrainTime()
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            return trainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier
                * ageTrainMultiplier;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                GameObject spawned = VanikFactory.Spawn(transform.position + _rallyOffset, Faction);
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }

        // Phase 6: Chola's unique tech narrows the spread symmetrically -
        // sellRate up, buyRate down by the same amount - rather than being
        // read once at spawn, since a Market can outlive the moment its
        // owner's tech finishes researching.
        // Exposed so BuildMenu's trade-button labels/gating read the same
        // live rate Sell/Buy actually charge, instead of duplicating the
        // unique-tech bonus lookup and risking drift.
        // AoE-parity Phase 3.2: Chola's team bonus - allied Markets get a
        // narrowed +/-5-point spread, unconditional (not gated on the ally
        // having researched Chola Trade Networks) and stacking with the
        // owner's own unique-tech term above. Naturally dynamic like the
        // rest of this property - no spawn-time baking needed, since an
        // alliance formed/broken later re-evaluates on the next read. See
        // TeamBonus.cs.
        public float EffectiveSellRate =>
            sellRate + (UniqueTechProgress.HasResearched(Faction) ? UniqueTechDefinition.For(CivilizationRegistry.For(Faction)).MarketRateBonus : 0f)
            + (TeamBonus.HasAlly(Faction, CivilizationId.Chola) ? TeamBonus.CholaMarketRateBonus : 0f);
        public float EffectiveBuyRate =>
            buyRate - (UniqueTechProgress.HasResearched(Faction) ? UniqueTechDefinition.For(CivilizationRegistry.For(Faction)).MarketRateBonus : 0f)
            - (TeamBonus.HasAlly(Faction, CivilizationId.Chola) ? TeamBonus.CholaMarketRateBonus : 0f);

        // Sells `amount` of `type` for Gold, at sellRate. No-op (returns
        // false) if there isn't enough of `type` on hand - never sells a
        // partial amount, same "have it or don't" convention
        // Barracks/BuildingPlacer already use for costs.
        public bool Sell(ResourceType type, float amount)
        {
            if (!IsComplete || type == ResourceType.Gold || amount <= 0f)
            {
                return false;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(type) < amount)
            {
                return false;
            }

            stockpile.Add(type, -amount);
            stockpile.Add(ResourceType.Gold, amount * EffectiveSellRate);
            return true;
        }

        // Buys `amount` of `type` with Gold, at buyRate. No-op if there
        // isn't enough Gold on hand.
        public bool Buy(ResourceType type, float amount)
        {
            if (!IsComplete || type == ResourceType.Gold || amount <= 0f)
            {
                return false;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            float cost = amount * EffectiveBuyRate;
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return false;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            stockpile.Add(type, amount);
            return true;
        }
    }
}

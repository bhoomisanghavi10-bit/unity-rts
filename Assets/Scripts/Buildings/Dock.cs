using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Item 49: trains Fishing Boat/War Galley units and serves as the water
    // drop-off point for Fishing Boats (see BoatGatherer), mirroring
    // Barracks' training shape (one shared queue slot, lazy Site/
    // FactionMember resolution for the same same-frame-creation reason
    // documented on Barracks). Deliberately no Attack/Armor research track
    // yet - a first naval pass, not full tech-tree parity with Barracks.
    public class Dock : Building
    {
        private enum TrainingUnit
        {
            FishingBoat,
            WarGalley,
            FireShip,
            TradeShip,
        }

        [SerializeField] private float fishingBoatFoodCost = 40f;
        [SerializeField] private float fishingBoatWoodCost = 30f;
        [SerializeField] private float warGalleyFoodCost = 60f;
        [SerializeField] private float warGalleyGoldCost = 60f;
        // Wave 4 item 25: Wood-only, no Food/Gold - matches
        // unit_roster_template.csv's "fire_ship" row and Scorpion's own
        // "a crafted vessel, not a fed crew" cost-model precedent.
        [SerializeField] private float fireShipWoodCost = 70f;
        // Wave 4 item 26: Trade Ship, the naval Trader - a crafted vessel
        // like Fishing Boat/War Galley, so Wood+Gold, no Food.
        [SerializeField] private float tradeShipWoodCost = 70f;
        [SerializeField] private float tradeShipGoldCost = 30f;
        [SerializeField] private float trainTime = 6f;
        // Was a fixed Vector3(0,0,3) - always spawned/rallied boats 3
        // units north regardless of which shore the Dock was actually
        // built on. A Dock built on the west shore (water to its east)
        // would send every trained boat north into dry land instead of
        // toward the water - the exact "ship is on land" bug this fixes.
        // Now a plain distance; the direction is computed at Awake from
        // WaterProximity.DirectionToNearestWater(transform.position),
        // since that's the one thing that actually varies per Dock.
        [SerializeField] private float rallyDistance = 3f;
        private Vector3 _rallyOffset;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private RallyPoint _rally;
        private float _remaining = -1f;
        private TrainingUnit _trainingUnit;
        // Wave 3 item 15: Naval tier ladder research track - lives on Dock
        // (where War Galley trains), same "independent research track,
        // doesn't block normal training" convention as Barracks' own
        // Infantry/Spearman/Archer/Cavalry/Siege tracks.
        private float _navalTierResearchRemaining = -1f;
        // Wave 4 item 25: Fire Ship tier ladder research track - an
        // independent track alongside NavalLineProgress's own (both live
        // on Dock, since Fire Ship trains here too), same "one building,
        // several independent tier tracks" convention Barracks already
        // established.
        private float _fireShipTierResearchRemaining = -1f;

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

        private void Awake()
        {
            _rallyOffset = WaterProximity.DirectionToNearestWater(transform.position) * rallyDistance;

            _rally = gameObject.AddComponent<RallyPoint>();
            _rally.Configure(_rallyOffset);
        }

        public bool IsComplete => Site == null || Site.IsComplete;
        public bool IsTraining => _remaining >= 0f;

        public bool IsResearchingNavalTier => _navalTierResearchRemaining >= 0f;
        public float NavalTierResearchProgress => IsResearchingNavalTier
            ? 1f - (_navalTierResearchRemaining / NavalLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingFireShipTier => _fireShipTierResearchRemaining >= 0f;
        public float FireShipTierResearchProgress => IsResearchingFireShipTier
            ? 1f - (_fireShipTierResearchRemaining / FireShipLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
            }

            if (IsResearchingNavalTier)
            {
                TickNavalTierResearch();
            }

            if (IsResearchingFireShipTier)
            {
                TickFireShipTierResearch();
            }
        }

        public void RequestTrainFishingBoat()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < fishingBoatFoodCost
                || stockpile.GetTotal(ResourceType.Wood) < fishingBoatWoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -fishingBoatFoodCost);
            stockpile.Add(ResourceType.Wood, -fishingBoatWoodCost);
            _trainingUnit = TrainingUnit.FishingBoat;
            _remaining = ScaledTrainTime();
        }

        public void RequestTrainWarGalley()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < warGalleyFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < warGalleyGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -warGalleyFoodCost);
            stockpile.Add(ResourceType.Gold, -warGalleyGoldCost);
            _trainingUnit = TrainingUnit.WarGalley;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 25: no Food/Gold, matching unit_roster_template.csv's
        // "fire_ship" row. No age gate of its own - tier 0's Classical
        // RequiredAge is descriptive only, same convention every other
        // line's own RequestTrain* already established.
        public void RequestTrainFireShip()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Wood) < fireShipWoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -fireShipWoodCost);
            _trainingUnit = TrainingUnit.FireShip;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 26: Trade Ship, the naval Trader - see Dock.
        // RequestTrainFireShip's own comment for why no age gate of its
        // own is needed.
        public void RequestTrainTradeShip()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Wood) < tradeShipWoodCost
                || stockpile.GetTotal(ResourceType.Gold) < tradeShipGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -tradeShipWoodCost);
            stockpile.Add(ResourceType.Gold, -tradeShipGoldCost);
            _trainingUnit = TrainingUnit.TradeShip;
            _remaining = ScaledTrainTime();
        }

        private float ScaledTrainTime()
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            // Phase 6 gap-close: Maratha's Naval-only train-time bonus, an
            // independent factor alongside the existing civ-wide
            // TrainTimeMultiplier (not a replacement for it) - see
            // CivilizationProfile.FindCategoryMultiplier.
            float navalTrainMultiplier = CivilizationProfile.FindCategoryMultiplier(
                CivilizationRegistry.For(Faction), StatType.TrainTime, UnitClass.Naval);
            return trainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier
                * ageTrainMultiplier * navalTrainMultiplier;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                GameObject spawned = _trainingUnit switch
                {
                    TrainingUnit.WarGalley => WarGalleyFactory.Spawn(transform.position + _rallyOffset, Faction),
                    TrainingUnit.FireShip => FireShipFactory.Spawn(transform.position + _rallyOffset, Faction),
                    TrainingUnit.TradeShip => TradeShipFactory.Spawn(transform.position + _rallyOffset, Faction),
                    _ => FishingBoatFactory.Spawn(transform.position + _rallyOffset, Faction),
                };
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }

        public void RequestResearchNavalTier()
        {
            if (!IsComplete || IsResearchingNavalTier
                || !NavalLineProgress.HasNextTier(Faction)
                || !NavalLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            NavalTierData next = NavalLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _navalTierResearchRemaining = next.ResearchTime;
        }

        private void TickNavalTierResearch()
        {
            _navalTierResearchRemaining -= Time.deltaTime;
            if (_navalTierResearchRemaining <= 0f)
            {
                NavalLineProgress.AdvanceTier(Faction);
                _navalTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item 25: Fire Ship tier ladder research - same shape as
        // RequestResearchNavalTier above, an independent track on the same
        // building.
        public void RequestResearchFireShipTier()
        {
            if (!IsComplete || IsResearchingFireShipTier
                || !FireShipLineProgress.HasNextTier(Faction)
                || !FireShipLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            FireShipTierData next = FireShipLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _fireShipTierResearchRemaining = next.ResearchTime;
        }

        private void TickFireShipTierResearch()
        {
            _fireShipTierResearchRemaining -= Time.deltaTime;
            if (_fireShipTierResearchRemaining <= 0f)
            {
                FireShipLineProgress.AdvanceTier(Faction);
                _fireShipTierResearchRemaining = -1f;
            }
        }
    }
}

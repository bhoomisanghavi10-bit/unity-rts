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
        }

        [SerializeField] private KeyCode trainKey = KeyCode.B;
        [SerializeField] private float fishingBoatFoodCost = 40f;
        [SerializeField] private float fishingBoatWoodCost = 30f;
        [SerializeField] private float warGalleyFoodCost = 60f;
        [SerializeField] private float warGalleyGoldCost = 60f;
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
            trainKey = GameSettings.GetKey("TrainDockUnit", trainKey);

            _rallyOffset = WaterProximity.DirectionToNearestWater(transform.position) * rallyDistance;

            _rally = gameObject.AddComponent<RallyPoint>();
            _rally.Configure(_rallyOffset);
        }

        public bool IsComplete => Site == null || Site.IsComplete;
        public bool IsTraining => _remaining >= 0f;

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
            }

            if (Faction == FactionId.Player && !IsTraining && Input.GetKeyDown(trainKey))
            {
                RequestTrainFishingBoat();
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

        private float ScaledTrainTime()
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            // Phase 6 gap-close: Maratha's Naval-only train-time bonus, an
            // independent factor alongside the existing civ-wide
            // TrainTimeMultiplier (not a replacement for it) - see
            // CivilizationProfile.FindCategoryMultiplier.
            float navalTrainMultiplier = CivilizationProfile.FindCategoryMultiplier(
                CivilizationRegistry.For(Faction), StatType.TrainTime, UnitCategory.Naval);
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
                    _ => FishingBoatFactory.Spawn(transform.position + _rallyOffset, Faction),
                };
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }
    }
}

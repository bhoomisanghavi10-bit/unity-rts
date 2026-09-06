using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Wave 4 item 27: trains the two Support units splitting AoE's Monk -
    // Vaidya (healer) and Purohita (converter). Single shared queue slot,
    // same multi-kind-single-queue shape as Dock's own
    // FishingBoat/WarGalley/FireShip/TradeShip training. Gold-only costs -
    // a religious specialist, not a fed worker, same cost-model precedent
    // several other Wave 4 units already established. RallyPoint uses a
    // plain forward offset (not WaterProximity-based) since Monastery isn't
    // water-adjacent, same convention as Market's own Awake.
    public class Monastery : Building
    {
        private enum TrainingUnit
        {
            Vaidya,
            Purohita,
        }

        [SerializeField] private float vaidyaGoldCost = 100f;
        [SerializeField] private float purohitaGoldCost = 120f;
        [SerializeField] private float trainTime = 30f;
        [SerializeField] private float rallyDistance = 3f;
        private Vector3 _rallyOffset;
        private RallyPoint _rally;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
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
            _rallyOffset = transform.forward * rallyDistance;
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
        }

        public void RequestTrainVaidya()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < vaidyaGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -vaidyaGoldCost);
            _trainingUnit = TrainingUnit.Vaidya;
            _remaining = ScaledTrainTime();
        }

        public void RequestTrainPurohita()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < purohitaGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -purohitaGoldCost);
            _trainingUnit = TrainingUnit.Purohita;
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
                GameObject spawned = _trainingUnit switch
                {
                    TrainingUnit.Purohita => PurohitaFactory.Spawn(transform.position + _rallyOffset, Faction),
                    _ => VaidyaFactory.Spawn(transform.position + _rallyOffset, Faction),
                };
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }
    }
}

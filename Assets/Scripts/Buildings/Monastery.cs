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
        // Wave 6 item 35: passive Gold trickle per Relic currently stored
        // here - a first-pass rate (30 Gold/min per Relic), not
        // independently balanced, same disclosed convention as Trader's own
        // ComputeTradeGold rate.
        [SerializeField] private float relicGoldPerSecond = 0.5f;
        private Vector3 _rallyOffset;
        private RallyPoint _rally;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private float _remaining = -1f;
        private TrainingUnit _trainingUnit;
        private int _relicCount;

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
        public int RelicCount => _relicCount;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // internal (not private) so EditMode tests can drive the relic
        // trickle deterministically without waiting on a real Update() loop
        // - same convention as Farm.Tick.
        internal void Tick(float deltaTime)
        {
            if (IsTraining)
            {
                TickTraining(deltaTime);
            }

            if (IsComplete && _relicCount > 0)
            {
                ResourceStockpile.For(Faction).Add(ResourceType.Gold, RelicGoldPerTick(relicGoldPerSecond, _relicCount, deltaTime));
            }
        }

        // Pure so it's directly EditMode-testable - same reasoning as
        // Trader.ComputeTradeGold's own pure-function split.
        internal static float RelicGoldPerTick(float goldPerSecond, int relicCount, float deltaTime)
        {
            return goldPerSecond * relicCount * deltaTime;
        }

        // Called by RelicCarrier.Deposit once a carried Relic reaches this
        // Monastery - permanent (no un-delivery), matches AoE's own "stored
        // relics stay stored" convention.
        public void AddRelic()
        {
            _relicCount++;
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

        private void TickTraining(float deltaTime)
        {
            _remaining -= deltaTime;
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

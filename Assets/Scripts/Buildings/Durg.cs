using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Wave 2 item 7: the Durg building - AoE's Castle-equivalent. Trains
    // each civ's unique unit(s) (moved off Barracks this session - see
    // docs/IMPLEMENTATION_ROADMAP.md Wave 2 item 7 and
    // Assets/Tests/EditMode/DurgTests.cs) and, via DurgFactory, carries the
    // strongest garrison/defensive stats of any building in the game.
    //
    // Deliberately narrower than Barracks: only unique-unit training lives
    // here. Attack/Armor/UniqueTech research and every other unit type stay
    // on Barracks, untouched - this is a structural unlock, not a second
    // Barracks.
    //
    // Same lazy-resolved Site/FactionMember and Awake-added RallyPoint
    // convention as Barracks - see that class's own comments for why
    // (AddComponent fires Awake() immediately, before a factory has
    // necessarily added every sibling component yet).
    public class Durg : Building
    {
        private enum TrainingUnit
        {
            UniqueUnit,
            UniqueUnit2,
        }

        [SerializeField] private Vector3 rallyOffset = new Vector3(3f, 0f, 3f);

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
            _rally = gameObject.AddComponent<RallyPoint>();
            _rally.Configure(rallyOffset);
        }

        public bool IsComplete => Site == null || Site.IsComplete;
        public bool IsTraining => _remaining >= 0f;

        // Same shape as Barracks.UniqueUnit/UniqueUnitCount/UniqueUnitAt -
        // exposed so BuildMenu can show the owning faction's actual
        // civ-specific unit name/cost without duplicating the
        // CivilizationRegistry lookup.
        public UniqueUnitDefinition UniqueUnit => UniqueUnitDefinition.For(CivilizationRegistry.For(Faction));
        public int UniqueUnitCount => UniqueUnitDefinition.CountFor(CivilizationRegistry.For(Faction));
        public UniqueUnitDefinition UniqueUnitAt(int slot) => UniqueUnitDefinition.For(CivilizationRegistry.For(Faction), slot);

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
            }
        }

        public void RequestTrainUniqueUnit()
        {
            RequestTrainUniqueUnit(0);
        }

        public void RequestTrainUniqueUnit(int slot)
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UniqueUnitDefinition unique = UniqueUnitDefinition.For(CivilizationRegistry.For(Faction), slot);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < unique.FoodCost
                || stockpile.GetTotal(ResourceType.Gold) < unique.GoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -unique.FoodCost);
            stockpile.Add(ResourceType.Gold, -unique.GoldCost);
            _trainingUnit = slot == 0 ? TrainingUnit.UniqueUnit : TrainingUnit.UniqueUnit2;
            _remaining = ScaledTrainTime(unique.TrainTimeSeconds);
        }

        private float ScaledTrainTime(float baseTrainTime)
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            return baseTrainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier * ageTrainMultiplier;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                int slot = _trainingUnit == TrainingUnit.UniqueUnit ? 0 : 1;
                GameObject spawned = UniqueUnitDefinition.For(CivilizationRegistry.For(Faction), slot).Spawn(transform.position + rallyOffset, Faction);
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }
    }
}

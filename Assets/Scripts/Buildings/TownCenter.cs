using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Identifies this building as a valid drop-off point for gathered
    // resources, and trains Worker units - the Player's via the train
    // hotkey or BuildMenu's button, the AI's via AiController, both
    // funneling through RequestTrain(). Mirrors Barracks' training shape
    // exactly (lazy FactionMember resolution for the same same-frame-
    // creation reason documented there), plus a Population.HasRoom() check
    // Barracks now shares too - AoE-style, training blocks at the
    // population cap until a House is built.
    //
    // Also researches Age advancement (RequestAgeUp()) - deliberately its
    // own independent countdown rather than sharing Worker training's
    // _remaining slot, so a faction can train Workers and research an Age
    // at the same time, same as real AoE's parallel queues.
    public class TownCenter : Building
    {
        [SerializeField] private KeyCode trainKey = KeyCode.G;
        [SerializeField] private float workerFoodCost = 50f;
        [SerializeField] private float trainTime = 6f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(-3f, 0f, 3f);

        private FactionMember _factionMember;
        private RallyPoint _rally;
        private float _remaining = -1f;
        private float _ageUpRemaining = -1f;
        private AgeId _ageUpTarget;

        // Self-added in Awake (not lazily like ConstructionSite/FactionMember
        // below) rather than requiring a Factory change: RallyPoint only
        // needs this component's own Building/rallyOffset, both already
        // available at Awake time, unlike the siblings a Factory adds
        // moments later - and it needs to exist as soon as this building is
        // selectable, since a player can right-click a rally point before
        // ever training anything.
        private void Awake()
        {
            trainKey = GameSettings.GetKey("TrainWorker", trainKey);

            _rally = gameObject.AddComponent<RallyPoint>();
            _rally.Configure(rallyOffset);
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

        public bool IsTraining => _remaining >= 0f;
        public bool IsAgingUp => _ageUpRemaining >= 0f;
        public float AgeUpProgress => IsAgingUp ? 1f - (_ageUpRemaining / AgeProfile.For(_ageUpTarget).ResearchTime) : 0f;

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
            }

            if (IsAgingUp)
            {
                TickAgeUp();
            }

            if (Faction == FactionId.Player && !IsTraining && Input.GetKeyDown(trainKey))
            {
                RequestTrain();
            }
        }

        public void RequestTrain()
        {
            if (IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < workerFoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -workerFoodCost);
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            _remaining = trainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier * ageTrainMultiplier;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                GameObject spawned = WorkerFactory.Spawn(transform.position + rallyOffset, Faction);
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }

        public void RequestAgeUp()
        {
            if (IsAgingUp || !AgeProgress.HasNextAge(Faction))
            {
                return;
            }

            AgeId next = AgeProgress.NextAge(Faction);
            AgeProfile profile = AgeProfile.For(next);

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Wood) < profile.WoodCost
                || stockpile.GetTotal(ResourceType.Stone) < profile.StoneCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -profile.WoodCost);
            stockpile.Add(ResourceType.Stone, -profile.StoneCost);
            _ageUpTarget = next;
            _ageUpRemaining = profile.ResearchTime;
        }

        private void TickAgeUp()
        {
            _ageUpRemaining -= Time.deltaTime;
            if (_ageUpRemaining <= 0f)
            {
                AgeProgress.Advance(Faction, _ageUpTarget);
                _ageUpRemaining = -1f;
            }
        }
    }
}

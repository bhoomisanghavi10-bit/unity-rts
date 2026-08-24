using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Trains Soldier/Archer units and researches Attack/Armor upgrades for
    // whichever faction owns this Barracks - the Player's via the train
    // hotkey (Soldier only) or BuildMenu's buttons, the AI's via
    // AiController - all funnel through the same entry points.
    //
    // Training (Soldier/Archer) shares one queue slot (_remaining), same as
    // before Archers existed - only one unit trains at a time. Research
    // (Attack/Armor) is its own independent countdown per track, mirroring
    // TownCenter's Age-up-alongside-Worker-training shape: a faction can
    // train a unit and research an upgrade at the same time, and even
    // research both tracks at once, since real AoE's Blacksmith queues
    // don't block each other either.
    //
    // Deliberately NOT [RequireComponent(typeof(FactionMember))]: that would
    // auto-add a default (Player) FactionMember the instant AddComponent
    // <Barracks>() runs, before a factory gets the chance to add its own
    // Configure()'d one - producing two FactionMember components on the same
    // GameObject and silently picking the wrong (default Player) one via
    // GetComponent.
    //
    // ConstructionSite/FactionMember are resolved lazily (on first actual
    // use), not in Awake()/Start(): a spawner adds Barracks and its siblings
    // one AddComponent call at a time within a single synchronous method,
    // and AddComponent fires Awake() immediately - so an eager Awake()-time
    // GetComponent for a sibling added moments later would find nothing.
    // Start() would fix that (guaranteed to run after every Awake()), but
    // AiController can call RequestTrain() on a Barracks it just created
    // within the very same Update() tick, before that Barracks' own Start()
    // has even run yet for the first time. Lazy resolution sidesteps both
    // problems: by the time anything actually reads these, every sibling
    // from that same synchronous spawn call already exists on the
    // GameObject, regardless of which lifecycle callback has or hasn't
    // fired.
    public class Barracks : Building
    {
        [SerializeField] private KeyCode trainKey = KeyCode.T;
        [SerializeField] private float soldierFoodCost = 50f;
        [SerializeField] private float soldierGoldCost = 20f;
        [SerializeField] private float archerFoodCost = 40f;
        [SerializeField] private float archerGoldCost = 35f;
        [SerializeField] private float trainTime = 5f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(3f, 0f, 3f);
        [SerializeField] private float upgradeGoldCostPerTier = 80f;
        [SerializeField] private float upgradeResearchTimePerTier = 15f;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private RallyPoint _rally;
        private float _remaining = -1f;
        private bool _trainingArcher;
        private float _attackResearchRemaining = -1f;
        private float _armorResearchRemaining = -1f;

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

        // Self-added in Awake, not lazily - see TownCenter's identical
        // comment on why this is safe to do eagerly (unlike
        // ConstructionSite/FactionMember below) and needs to be.
        private void Awake()
        {
            _rally = gameObject.AddComponent<RallyPoint>();
            _rally.Configure(rallyOffset);
        }

        public bool IsComplete => Site == null || Site.IsComplete;
        public bool IsTraining => _remaining >= 0f;
        public bool IsResearchingAttack => _attackResearchRemaining >= 0f;
        public bool IsResearchingArmor => _armorResearchRemaining >= 0f;
        public float AttackResearchProgress => IsResearchingAttack
            ? 1f - (_attackResearchRemaining / (upgradeResearchTimePerTier * (UpgradeProgress.AttackTier(Faction) + 1)))
            : 0f;
        public float ArmorResearchProgress => IsResearchingArmor
            ? 1f - (_armorResearchRemaining / (upgradeResearchTimePerTier * (UpgradeProgress.ArmorTier(Faction) + 1)))
            : 0f;

        // Exposed so BuildMenu's cost label reads the same number
        // RequestResearchAttack/Armor actually charge, instead of
        // duplicating the *(tier+1) formula and risking the two drifting
        // apart if upgradeGoldCostPerTier is ever tuned in the Inspector.
        public float NextAttackUpgradeCost => upgradeGoldCostPerTier * (UpgradeProgress.AttackTier(Faction) + 1);
        public float NextArmorUpgradeCost => upgradeGoldCostPerTier * (UpgradeProgress.ArmorTier(Faction) + 1);

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
            }

            if (IsResearchingAttack)
            {
                TickAttackResearch();
            }

            if (IsResearchingArmor)
            {
                TickArmorResearch();
            }

            if (Faction == FactionId.Player && !IsTraining && Input.GetKeyDown(trainKey))
            {
                RequestTrain();
            }
        }

        public void RequestTrain()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < soldierFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < soldierGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -soldierFoodCost);
            stockpile.Add(ResourceType.Gold, -soldierGoldCost);
            _trainingArcher = false;
            _remaining = ScaledTrainTime();
        }

        public void RequestTrainArcher()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < archerFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < archerGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -archerFoodCost);
            stockpile.Add(ResourceType.Gold, -archerGoldCost);
            _trainingArcher = true;
            _remaining = ScaledTrainTime();
        }

        private float ScaledTrainTime()
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            return trainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier * ageTrainMultiplier;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                GameObject spawned = _trainingArcher
                    ? ArcherFactory.Spawn(transform.position + rallyOffset, Faction)
                    : SoldierFactory.Spawn(transform.position + rallyOffset, Faction);
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }

        public void RequestResearchAttack()
        {
            if (!IsComplete || IsResearchingAttack || !UpgradeProgress.HasNextAttackTier(Faction))
            {
                return;
            }

            int tier = UpgradeProgress.AttackTier(Faction);
            float cost = upgradeGoldCostPerTier * (tier + 1);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            _attackResearchRemaining = upgradeResearchTimePerTier * (tier + 1);
        }

        public void RequestResearchArmor()
        {
            if (!IsComplete || IsResearchingArmor || !UpgradeProgress.HasNextArmorTier(Faction))
            {
                return;
            }

            int tier = UpgradeProgress.ArmorTier(Faction);
            float cost = upgradeGoldCostPerTier * (tier + 1);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            _armorResearchRemaining = upgradeResearchTimePerTier * (tier + 1);
        }

        private void TickAttackResearch()
        {
            _attackResearchRemaining -= Time.deltaTime;
            if (_attackResearchRemaining <= 0f)
            {
                UpgradeProgress.AdvanceAttack(Faction);
                _attackResearchRemaining = -1f;
            }
        }

        private void TickArmorResearch()
        {
            _armorResearchRemaining -= Time.deltaTime;
            if (_armorResearchRemaining <= 0f)
            {
                UpgradeProgress.AdvanceArmor(Faction);
                _armorResearchRemaining = -1f;
            }
        }
    }
}

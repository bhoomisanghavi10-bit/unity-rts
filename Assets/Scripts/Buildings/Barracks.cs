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
        // Which unit type _remaining is counting down for - was a single
        // _trainingArcher bool before Cavalry/Siege made it a four-way
        // choice.
        private enum TrainingUnit
        {
            Soldier,
            Archer,
            Cavalry,
            Siege,
            UniqueUnit,
        }

        [SerializeField] private KeyCode trainKey = KeyCode.T;
        [SerializeField] private float soldierFoodCost = 50f;
        [SerializeField] private float soldierGoldCost = 20f;
        [SerializeField] private float archerFoodCost = 40f;
        [SerializeField] private float archerGoldCost = 35f;
        [SerializeField] private float cavalryFoodCost = 70f;
        [SerializeField] private float cavalryGoldCost = 50f;
        [SerializeField] private float siegeFoodCost = 90f;
        [SerializeField] private float siegeGoldCost = 75f;
        [SerializeField] private float trainTime = 5f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(3f, 0f, 3f);
        [SerializeField] private float upgradeGoldCostPerTier = 80f;
        [SerializeField] private float upgradeResearchTimePerTier = 15f;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private RallyPoint _rally;
        private float _remaining = -1f;
        private TrainingUnit _trainingUnit;
        private float _attackResearchRemaining = -1f;
        private float _armorResearchRemaining = -1f;

        // Item 40's additive layer - one more independent research track
        // each for Attack/Armor, same "doesn't block the others" shape as
        // the flat tracks above, but targeting a specific UnitClass
        // (RequestResearchClassAttack/Armor) instead of every unit
        // equally. _classAttackTarget/_classArmorTarget record which
        // class each track is currently researching.
        private float _classAttackResearchRemaining = -1f;
        private float _classArmorResearchRemaining = -1f;
        private UnitClass _classAttackTarget;
        private UnitClass _classArmorTarget;

        // Phase 6 (civ asymmetry): one more independent research track,
        // same "doesn't block the others" shape - but a one-time flag via
        // UniqueTechProgress rather than a tiered counter, since a unique
        // tech has exactly one level (see UniqueTechDefinition).
        private float _uniqueTechResearchRemaining = -1f;

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
            trainKey = GameSettings.GetKey("TrainUnit", trainKey);

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

        public bool IsResearchingClassAttack => _classAttackResearchRemaining >= 0f;
        public bool IsResearchingClassArmor => _classArmorResearchRemaining >= 0f;

        public float NextClassAttackUpgradeCost(UnitClass unitClass) =>
            upgradeGoldCostPerTier * (UpgradeProgress.ClassAttackTier(Faction, unitClass) + 1);
        public float NextClassArmorUpgradeCost(UnitClass unitClass) =>
            upgradeGoldCostPerTier * (UpgradeProgress.ClassArmorTier(Faction, unitClass) + 1);

        public bool IsResearchingUniqueTech => _uniqueTechResearchRemaining >= 0f;
        public bool HasResearchedUniqueTech => UniqueTechProgress.HasResearched(Faction);
        public UniqueTechDefinition UniqueTech => UniqueTechDefinition.For(CivilizationRegistry.For(Faction));
        public float UniqueTechResearchProgress => IsResearchingUniqueTech
            ? 1f - (_uniqueTechResearchRemaining / UniqueTech.ResearchTime)
            : 0f;

        // Phase 6 unique unit: exposed the same way UniqueTech is, so
        // BuildMenu can show the owning faction's actual civ-specific unit
        // name/cost without duplicating the CivilizationRegistry lookup.
        public UniqueUnitDefinition UniqueUnit => UniqueUnitDefinition.For(CivilizationRegistry.For(Faction));

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

            if (IsResearchingClassAttack)
            {
                TickClassAttackResearch();
            }

            if (IsResearchingClassArmor)
            {
                TickClassArmorResearch();
            }

            if (IsResearchingUniqueTech)
            {
                TickUniqueTechResearch();
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
            _trainingUnit = TrainingUnit.Soldier;
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
            _trainingUnit = TrainingUnit.Archer;
            _remaining = ScaledTrainTime();
        }

        public void RequestTrainCavalry()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < cavalryFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < cavalryGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -cavalryFoodCost);
            stockpile.Add(ResourceType.Gold, -cavalryGoldCost);
            _trainingUnit = TrainingUnit.Cavalry;
            _remaining = ScaledTrainTime();
        }

        public void RequestTrainSiege()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < siegeFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < siegeGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -siegeFoodCost);
            stockpile.Add(ResourceType.Gold, -siegeGoldCost);
            _trainingUnit = TrainingUnit.Siege;
            _remaining = ScaledTrainTime();
        }

        // Phase 6: same shape as every other RequestTrain* above, but cost
        // and the spawn call both come from the owning faction's civ
        // (UniqueUnitDefinition) instead of a fixed constant - there's
        // only one unique unit, but which one depends on who's asking.
        public void RequestTrainUniqueUnit()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UniqueUnitDefinition unique = UniqueUnitDefinition.For(CivilizationRegistry.For(Faction));
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < unique.FoodCost
                || stockpile.GetTotal(ResourceType.Gold) < unique.GoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -unique.FoodCost);
            stockpile.Add(ResourceType.Gold, -unique.GoldCost);
            _trainingUnit = TrainingUnit.UniqueUnit;
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
                GameObject spawned = _trainingUnit switch
                {
                    TrainingUnit.Archer => ArcherFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.Cavalry => CavalryFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.Siege => SiegeFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.UniqueUnit => UniqueUnitDefinition.For(CivilizationRegistry.For(Faction)).Spawn(transform.position + rallyOffset, Faction),
                    _ => SoldierFactory.Spawn(transform.position + rallyOffset, Faction),
                };
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

        // Item 40's additive per-class research - same cost/no-blocking
        // shape as RequestResearchAttack/Armor above, just keyed to a
        // specific UnitClass instead of applying to everyone.
        public void RequestResearchClassAttack(UnitClass unitClass)
        {
            if (!IsComplete || IsResearchingClassAttack || !UpgradeProgress.HasNextClassAttackTier(Faction, unitClass))
            {
                return;
            }

            int tier = UpgradeProgress.ClassAttackTier(Faction, unitClass);
            float cost = upgradeGoldCostPerTier * (tier + 1);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            _classAttackTarget = unitClass;
            _classAttackResearchRemaining = upgradeResearchTimePerTier * (tier + 1);
        }

        public void RequestResearchClassArmor(UnitClass unitClass)
        {
            if (!IsComplete || IsResearchingClassArmor || !UpgradeProgress.HasNextClassArmorTier(Faction, unitClass))
            {
                return;
            }

            int tier = UpgradeProgress.ClassArmorTier(Faction, unitClass);
            float cost = upgradeGoldCostPerTier * (tier + 1);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            _classArmorTarget = unitClass;
            _classArmorResearchRemaining = upgradeResearchTimePerTier * (tier + 1);
        }

        private void TickClassAttackResearch()
        {
            _classAttackResearchRemaining -= Time.deltaTime;
            if (_classAttackResearchRemaining <= 0f)
            {
                UpgradeProgress.AdvanceClassAttack(Faction, _classAttackTarget);
                _classAttackResearchRemaining = -1f;
            }
        }

        private void TickClassArmorResearch()
        {
            _classArmorResearchRemaining -= Time.deltaTime;
            if (_classArmorResearchRemaining <= 0f)
            {
                UpgradeProgress.AdvanceClassArmor(Faction, _classArmorTarget);
                _classArmorResearchRemaining = -1f;
            }
        }

        // Phase 6: one-time civ unique tech - same cost/gating shape as
        // RequestResearchAttack/Armor, but there's only ever one tier, so
        // gating is HasResearchedUniqueTech rather than a HasNextTier check.
        public void RequestResearchUniqueTech()
        {
            if (!IsComplete || IsResearchingUniqueTech || HasResearchedUniqueTech)
            {
                return;
            }

            UniqueTechDefinition tech = UniqueTech;
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < tech.GoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -tech.GoldCost);
            _uniqueTechResearchRemaining = tech.ResearchTime;
        }

        private void TickUniqueTechResearch()
        {
            _uniqueTechResearchRemaining -= Time.deltaTime;
            if (_uniqueTechResearchRemaining <= 0f)
            {
                UniqueTechProgress.MarkResearched(Faction);
                _uniqueTechResearchRemaining = -1f;
            }
        }
    }
}

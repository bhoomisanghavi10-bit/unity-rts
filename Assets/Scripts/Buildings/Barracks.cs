using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Trains Soldier/Archer/Cavalry/Siege/Spearman units and researches the
    // per-class Attack/Armor tracks (item 40) plus each civ's own UniqueTech
    // for whichever faction owns this Barracks - the Player's via
    // BuildMenu's hotkeys/buttons (BuildMenu owns hotkey dispatch, gated on
    // selection), the AI's via AiController - all funnel through the same
    // entry points. Wave 2 item 8 moved the flat Attack/Armor tracks'
    // RequestResearchAttack/Armor off this class onto the new Karmashala
    // building (AoE's Blacksmith-equivalent) - see Karmashala.cs.
    //
    // Training (Soldier/Archer/...) shares one queue slot (_remaining), same
    // as before Archers existed - only one unit trains at a time. Research
    // (ClassAttack/ClassArmor/UniqueTech) is its own independent countdown
    // per track, mirroring TownCenter's Age-up-alongside-Worker-training
    // shape: a faction can train a unit and research an upgrade at the same
    // time, and even research multiple tracks at once, since real AoE's
    // Blacksmith/Castle queues don't block each other either.
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
            Spearman,
            Chara,
            Skirmisher,
            BatteringRam,
            CavalryArcher,
            CamelRider,
            Scorpion,
        }

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
        // Wave 2 item 8: RequestResearchAttack/Armor themselves moved to
        // Karmashala, but both fields stay here - ClassAttack/ClassArmor
        // below still use them for their own cost/timing.
        [SerializeField] private float upgradeGoldCostPerTier = 80f;
        [SerializeField] private float upgradeResearchTimePerTier = 15f;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private RallyPoint _rally;
        private float _remaining = -1f;
        private TrainingUnit _trainingUnit;

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

        // Wave 3 item 9: the Infantry tier ladder (Padati -> Senani ->
        // Khandayata -> Maha Khandayata -> Vir Yodha) - same independent,
        // non-blocking research-track shape as the tracks above. Lives on
        // Barracks (not Karmashala) since it upgrades what Barracks itself
        // trains, matching AoE II's own Barracks-researches-infantry-line
        // convention.
        private float _infantryTierResearchRemaining = -1f;

        // Wave 3 item 10: the Spearman tier ladder (Bhaladhari ->
        // Trishuladhari -> Maha Trishuladhari) - same independent,
        // non-blocking research-track shape as InfantryTier above, also
        // living on Barracks since it upgrades what Barracks itself trains.
        private float _spearmanTierResearchRemaining = -1f;

        // Wave 3 item 11: the Archer tier ladder (Dhanurdhara -> Yantra
        // Dhanurdhara -> Maha Dhanurdhara) - same independent, non-blocking
        // research-track shape as InfantryTier/SpearmanTier above, also
        // living on Barracks since it upgrades what Barracks itself trains.
        private float _archerTierResearchRemaining = -1f;

        // Wave 3 item 12: the Cavalry tier ladder (Ashvarohi -> Maha
        // Ashvarohi -> Vir Ashvarohi) - same independent, non-blocking
        // research-track shape as InfantryTier/SpearmanTier/ArcherTier
        // above, also living on Barracks since it upgrades what Barracks
        // itself trains.
        private float _cavalryTierResearchRemaining = -1f;

        // Wave 3 item 14: the Siege tier ladder (Shilakshepaka -> Maha
        // Shilakshepaka -> Vajra Shilakshepaka) - same independent,
        // non-blocking research-track shape as InfantryTier/SpearmanTier/
        // ArcherTier/CavalryTier above, also living on Barracks since it
        // upgrades what Barracks itself trains.
        private float _siegeTierResearchRemaining = -1f;

        // Wave 4 item 18: the Scout tier ladder (Chara -> Vega Ashvarohi ->
        // Maha Vega Ashvarohi) - same independent, non-blocking
        // research-track shape as InfantryTier/SpearmanTier/ArcherTier/
        // CavalryTier/SiegeTier above, also living on Barracks since it
        // upgrades what Barracks itself trains.
        private float _charaTierResearchRemaining = -1f;

        // Wave 4 item 19: the Skirmisher tier ladder (Pratirodhi Dhanurdhara
        // -> Maha Pratirodhi Dhanurdhara) - same independent, non-blocking
        // research-track shape as InfantryTier/SpearmanTier/ArcherTier/
        // CavalryTier/SiegeTier/CharaTier above, also living on Barracks
        // since it upgrades what Barracks itself trains.
        private float _skirmisherTierResearchRemaining = -1f;

        // Wave 4 item 20: the Battering Ram tier ladder (Dwarabhanjaka ->
        // Maha Dwarabhanjaka -> Vajra Dwarabhanjaka) - same independent,
        // non-blocking research-track shape as every other tier line above,
        // also living on Barracks since it upgrades what Barracks itself
        // trains.
        private float _batteringRamTierResearchRemaining = -1f;

        // Wave 4 item: the Cavalry Archer tier ladder (Ashva Dhanurdhara ->
        // Maha Ashva Dhanurdhara) - same independent, non-blocking
        // research-track shape as every other tier line above, also living
        // on Barracks since it upgrades what Barracks itself trains.
        private float _cavalryArcherTierResearchRemaining = -1f;

        // Wave 4 item 22: the Camel Rider tier ladder (Ushtrarohi -> Maha
        // Ushtrarohi) - same independent, non-blocking research-track
        // shape as every other tier line above, also living on Barracks
        // since it upgrades what Barracks itself trains.
        private float _camelRiderTierResearchRemaining = -1f;

        // Wave 4 item 23: the Scorpion tier ladder (Bana Yantra -> Maha
        // Bana Yantra) - same independent, non-blocking research-track
        // shape as every other tier line above, also living on Barracks
        // since it upgrades what Barracks itself trains.
        private float _scorpionTierResearchRemaining = -1f;

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

        public bool IsResearchingInfantryTier => _infantryTierResearchRemaining >= 0f;
        public float InfantryTierResearchProgress => IsResearchingInfantryTier
            ? 1f - (_infantryTierResearchRemaining / InfantryLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingSpearmanTier => _spearmanTierResearchRemaining >= 0f;
        public float SpearmanTierResearchProgress => IsResearchingSpearmanTier
            ? 1f - (_spearmanTierResearchRemaining / SpearmanLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingArcherTier => _archerTierResearchRemaining >= 0f;
        public float ArcherTierResearchProgress => IsResearchingArcherTier
            ? 1f - (_archerTierResearchRemaining / ArcherLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingCavalryTier => _cavalryTierResearchRemaining >= 0f;
        public float CavalryTierResearchProgress => IsResearchingCavalryTier
            ? 1f - (_cavalryTierResearchRemaining / CavalryLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingSiegeTier => _siegeTierResearchRemaining >= 0f;
        public float SiegeTierResearchProgress => IsResearchingSiegeTier
            ? 1f - (_siegeTierResearchRemaining / SiegeLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingCharaTier => _charaTierResearchRemaining >= 0f;
        public float CharaTierResearchProgress => IsResearchingCharaTier
            ? 1f - (_charaTierResearchRemaining / ScoutLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingSkirmisherTier => _skirmisherTierResearchRemaining >= 0f;
        public float SkirmisherTierResearchProgress => IsResearchingSkirmisherTier
            ? 1f - (_skirmisherTierResearchRemaining / SkirmisherLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingBatteringRamTier => _batteringRamTierResearchRemaining >= 0f;
        public float BatteringRamTierResearchProgress => IsResearchingBatteringRamTier
            ? 1f - (_batteringRamTierResearchRemaining / BatteringRamLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingCavalryArcherTier => _cavalryArcherTierResearchRemaining >= 0f;
        public float CavalryArcherTierResearchProgress => IsResearchingCavalryArcherTier
            ? 1f - (_cavalryArcherTierResearchRemaining / CavalryArcherLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingCamelRiderTier => _camelRiderTierResearchRemaining >= 0f;
        public float CamelRiderTierResearchProgress => IsResearchingCamelRiderTier
            ? 1f - (_camelRiderTierResearchRemaining / CamelRiderLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        public bool IsResearchingScorpionTier => _scorpionTierResearchRemaining >= 0f;
        public float ScorpionTierResearchProgress => IsResearchingScorpionTier
            ? 1f - (_scorpionTierResearchRemaining / ScorpionLineProgress.NextTierData(Faction).ResearchTime)
            : 0f;

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
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

            if (IsResearchingInfantryTier)
            {
                TickInfantryTierResearch();
            }

            if (IsResearchingSpearmanTier)
            {
                TickSpearmanTierResearch();
            }

            if (IsResearchingArcherTier)
            {
                TickArcherTierResearch();
            }

            if (IsResearchingCavalryTier)
            {
                TickCavalryTierResearch();
            }

            if (IsResearchingSiegeTier)
            {
                TickSiegeTierResearch();
            }

            if (IsResearchingCharaTier)
            {
                TickCharaTierResearch();
            }

            if (IsResearchingSkirmisherTier)
            {
                TickSkirmisherTierResearch();
            }

            if (IsResearchingBatteringRamTier)
            {
                TickBatteringRamTierResearch();
            }

            if (IsResearchingCavalryArcherTier)
            {
                TickCavalryArcherTierResearch();
            }

            if (IsResearchingCamelRiderTier)
            {
                TickCamelRiderTierResearch();
            }

            if (IsResearchingScorpionTier)
            {
                TickScorpionTierResearch();
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

            // Phase 6 gap-close: Rajput's "Cavalry cost 15% less Gold" bonus.
            // StatModifier has no resource-type field (Wood/Gold/Stone/Food
            // are indistinguishable in the data), so applying this to Gold
            // specifically is a hand-picked reading of the CSV's own
            // description text, not something the data itself encodes.
            float goldCost = cavalryGoldCost * CivilizationProfile.FindCategoryMultiplier(
                CivilizationRegistry.For(Faction), StatType.ResourceCost, UnitClass.Cavalry);

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < cavalryFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < goldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -cavalryFoodCost);
            stockpile.Add(ResourceType.Gold, -goldCost);
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

        // Spearman (Phase 2 content addition): the first Barracks-trained
        // unit with a real Wood cost, so unlike Soldier/Archer/Cavalry/
        // Siege above (whose Food/Gold costs are fixed [SerializeField]
        // literals already matching the CSV) this reads cost straight from
        // DataRegistry rather than adding yet another pair of Inspector
        // fields for a single unit. Falls back to unit_roster_template.csv's
        // known values (35 Food / 15 Wood) if the generated asset is ever
        // missing, same defensive pattern used everywhere else in Phase 2.
        public void RequestTrainSpearman()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("spearman");
            float foodCost = def != null ? def.cost.food : 35f;
            float woodCost = def != null ? def.cost.wood : 15f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < foodCost
                || stockpile.GetTotal(ResourceType.Wood) < woodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -foodCost);
            stockpile.Add(ResourceType.Wood, -woodCost);
            _trainingUnit = TrainingUnit.Spearman;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 18: Scout (Chara) - the second Barracks-trained unit
        // read straight from DataRegistry rather than fixed Inspector
        // fields, same reasoning as RequestTrainSpearman above. Falls back
        // to unit_roster_template.csv's known values (50 Food, 0 Gold) if
        // the generated asset is ever missing.
        public void RequestTrainChara()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("chara");
            float foodCost = def != null ? def.cost.food : 50f;
            float goldCost = def != null ? def.cost.gold : 0f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < foodCost
                || stockpile.GetTotal(ResourceType.Gold) < goldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -foodCost);
            stockpile.Add(ResourceType.Gold, -goldCost);
            _trainingUnit = TrainingUnit.Chara;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 19: Skirmisher - the second wholly-new Wave 4 unit,
        // read straight from DataRegistry rather than fixed Inspector
        // fields, same reasoning as RequestTrainSpearman/RequestTrainChara
        // above. Falls back to unit_roster_template.csv's known values (35
        // Food, 25 Gold) if the generated asset is ever missing.
        public void RequestTrainSkirmisher()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("skirmisher");
            float foodCost = def != null ? def.cost.food : 35f;
            float goldCost = def != null ? def.cost.gold : 25f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < foodCost
                || stockpile.GetTotal(ResourceType.Gold) < goldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -foodCost);
            stockpile.Add(ResourceType.Gold, -goldCost);
            _trainingUnit = TrainingUnit.Skirmisher;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 20: Battering Ram - the third wholly-new Wave 4 unit,
        // read straight from DataRegistry rather than fixed Inspector
        // fields, same reasoning as RequestTrainSpearman/RequestTrainChara/
        // RequestTrainSkirmisher above. Falls back to
        // unit_roster_template.csv's known values (60 Food, 120 Wood, no
        // Gold - a wood-heavy siege engine) if the generated asset is ever
        // missing.
        public void RequestTrainBatteringRam()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("battering_ram");
            float foodCost = def != null ? def.cost.food : 60f;
            float woodCost = def != null ? def.cost.wood : 120f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < foodCost
                || stockpile.GetTotal(ResourceType.Wood) < woodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -foodCost);
            stockpile.Add(ResourceType.Wood, -woodCost);
            _trainingUnit = TrainingUnit.BatteringRam;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item: Cavalry Archer - the fourth wholly-new Wave 4 unit,
        // read straight from DataRegistry rather than fixed Inspector
        // fields, same reasoning as RequestTrainSpearman/RequestTrainChara/
        // RequestTrainSkirmisher/RequestTrainBatteringRam above. Falls back
        // to unit_roster_template.csv's known values (60 Food, 40 Gold) if
        // the generated asset is ever missing. No age gate of its own -
        // tier 0's Durg RequiredAge is descriptive only, same convention
        // RequestTrainCavalry already established.
        public void RequestTrainCavalryArcher()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("cavalry_archer");
            float foodCost = def != null ? def.cost.food : 60f;
            float goldCost = def != null ? def.cost.gold : 40f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < foodCost
                || stockpile.GetTotal(ResourceType.Gold) < goldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -foodCost);
            stockpile.Add(ResourceType.Gold, -goldCost);
            _trainingUnit = TrainingUnit.CavalryArcher;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 22: Camel Rider - the fifth wholly-new Wave 4 unit,
        // read straight from DataRegistry rather than fixed Inspector
        // fields, same reasoning as RequestTrainSpearman/RequestTrainChara/
        // RequestTrainSkirmisher/RequestTrainBatteringRam/
        // RequestTrainCavalryArcher above. Falls back to
        // unit_roster_template.csv's known values (35 Food, 15 Wood, no
        // Gold - reuses Spearman's own Food+Wood-only cost model) if the
        // generated asset is ever missing. No age gate of its own - tier
        // 0's Durg RequiredAge is descriptive only, same convention
        // RequestTrainCavalryArcher already established.
        public void RequestTrainCamelRider()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("camel_rider");
            float foodCost = def != null ? def.cost.food : 35f;
            float woodCost = def != null ? def.cost.wood : 15f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < foodCost
                || stockpile.GetTotal(ResourceType.Wood) < woodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -foodCost);
            stockpile.Add(ResourceType.Wood, -woodCost);
            _trainingUnit = TrainingUnit.CamelRider;
            _remaining = ScaledTrainTime();
        }

        // Wave 4 item 23: same "read cost from the generated UnitDefinition,
        // fall back to unit_roster_template.csv's known values" convention
        // as every other RequestTrain* above. Falls back to 100 Wood/60
        // Gold if the generated asset is ever missing. No Food cost - a
        // machine, not a person, matching Siege's own no-Wood/Food+Gold
        // convention loosely but with Wood+Gold instead (a crafted engine,
        // not a fed crew). No age gate of its own - tier 0's Durg
        // RequiredAge is descriptive only, same convention every other
        // line already established.
        public void RequestTrainScorpion()
        {
            if (!IsComplete || IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            UnitDefinition def = DataRegistry.GetUnit("scorpion");
            float woodCost = def != null ? def.cost.wood : 100f;
            float goldCost = def != null ? def.cost.gold : 60f;

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Wood) < woodCost
                || stockpile.GetTotal(ResourceType.Gold) < goldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -woodCost);
            stockpile.Add(ResourceType.Gold, -goldCost);
            _trainingUnit = TrainingUnit.Scorpion;
            _remaining = ScaledTrainTime();
        }

        private float ScaledTrainTime(float baseTrainTime = -1f)
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            float baseTime = baseTrainTime >= 0f ? baseTrainTime : trainTime;
            return baseTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier * ageTrainMultiplier;
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
                    TrainingUnit.Spearman => SpearmanFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.Chara => ScoutFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.Skirmisher => SkirmisherFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.BatteringRam => BatteringRamFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.CavalryArcher => CavalryArcherFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.CamelRider => CamelRiderFactory.Spawn(transform.position + rallyOffset, Faction),
                    TrainingUnit.Scorpion => ScorpionFactory.Spawn(transform.position + rallyOffset, Faction),
                    _ => SoldierFactory.Spawn(transform.position + rallyOffset, Faction),
                };
                _rally.ApplyTo(spawned);
                _remaining = -1f;
            }
        }

        // Item 40's additive per-class research - same cost/no-blocking
        // shape RequestResearchAttack/Armor used to have before Wave 2 item
        // 8 moved those two off Barracks onto Karmashala, just keyed to a
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

        // Wave 3 item 9: Infantry tier ladder research - gated on both the
        // next tier's own required age (InfantryLineProgress.
        // NextTierAgeRequirementMet) and affordability, same shape as every
        // other research track above.
        public void RequestResearchInfantryTier()
        {
            if (!IsComplete || IsResearchingInfantryTier
                || !InfantryLineProgress.HasNextTier(Faction)
                || !InfantryLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            InfantryTierData next = InfantryLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _infantryTierResearchRemaining = next.ResearchTime;
        }

        private void TickInfantryTierResearch()
        {
            _infantryTierResearchRemaining -= Time.deltaTime;
            if (_infantryTierResearchRemaining <= 0f)
            {
                InfantryLineProgress.AdvanceTier(Faction);
                _infantryTierResearchRemaining = -1f;
            }
        }

        // Wave 3 item 10: Spearman tier ladder research - same shape as
        // RequestResearchInfantryTier above.
        public void RequestResearchSpearmanTier()
        {
            if (!IsComplete || IsResearchingSpearmanTier
                || !SpearmanLineProgress.HasNextTier(Faction)
                || !SpearmanLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            SpearmanTierData next = SpearmanLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _spearmanTierResearchRemaining = next.ResearchTime;
        }

        private void TickSpearmanTierResearch()
        {
            _spearmanTierResearchRemaining -= Time.deltaTime;
            if (_spearmanTierResearchRemaining <= 0f)
            {
                SpearmanLineProgress.AdvanceTier(Faction);
                _spearmanTierResearchRemaining = -1f;
            }
        }

        // Wave 3 item 11: Archer tier ladder research - same shape as
        // RequestResearchInfantryTier/RequestResearchSpearmanTier above.
        public void RequestResearchArcherTier()
        {
            if (!IsComplete || IsResearchingArcherTier
                || !ArcherLineProgress.HasNextTier(Faction)
                || !ArcherLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            ArcherTierData next = ArcherLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _archerTierResearchRemaining = next.ResearchTime;
        }

        private void TickArcherTierResearch()
        {
            _archerTierResearchRemaining -= Time.deltaTime;
            if (_archerTierResearchRemaining <= 0f)
            {
                ArcherLineProgress.AdvanceTier(Faction);
                _archerTierResearchRemaining = -1f;
            }
        }

        // Wave 3 item 12: Cavalry tier ladder research - same shape as
        // RequestResearchInfantryTier/RequestResearchSpearmanTier/
        // RequestResearchArcherTier above.
        public void RequestResearchCavalryTier()
        {
            if (!IsComplete || IsResearchingCavalryTier
                || !CavalryLineProgress.HasNextTier(Faction)
                || !CavalryLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            CavalryTierData next = CavalryLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _cavalryTierResearchRemaining = next.ResearchTime;
        }

        private void TickCavalryTierResearch()
        {
            _cavalryTierResearchRemaining -= Time.deltaTime;
            if (_cavalryTierResearchRemaining <= 0f)
            {
                CavalryLineProgress.AdvanceTier(Faction);
                _cavalryTierResearchRemaining = -1f;
            }
        }

        // Wave 3 item 14: Siege tier ladder research - same shape as
        // RequestResearchInfantryTier/RequestResearchSpearmanTier/
        // RequestResearchArcherTier/RequestResearchCavalryTier above.
        public void RequestResearchSiegeTier()
        {
            if (!IsComplete || IsResearchingSiegeTier
                || !SiegeLineProgress.HasNextTier(Faction)
                || !SiegeLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            SiegeTierData next = SiegeLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _siegeTierResearchRemaining = next.ResearchTime;
        }

        private void TickSiegeTierResearch()
        {
            _siegeTierResearchRemaining -= Time.deltaTime;
            if (_siegeTierResearchRemaining <= 0f)
            {
                SiegeLineProgress.AdvanceTier(Faction);
                _siegeTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item 18: Scout tier ladder research - same shape as
        // RequestResearchInfantryTier/RequestResearchSpearmanTier/
        // RequestResearchArcherTier/RequestResearchCavalryTier/
        // RequestResearchSiegeTier above.
        public void RequestResearchCharaTier()
        {
            if (!IsComplete || IsResearchingCharaTier
                || !ScoutLineProgress.HasNextTier(Faction)
                || !ScoutLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            ScoutTierData next = ScoutLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _charaTierResearchRemaining = next.ResearchTime;
        }

        private void TickCharaTierResearch()
        {
            _charaTierResearchRemaining -= Time.deltaTime;
            if (_charaTierResearchRemaining <= 0f)
            {
                ScoutLineProgress.AdvanceTier(Faction);
                _charaTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item 19: Skirmisher tier ladder research - same shape as
        // RequestResearchInfantryTier/RequestResearchSpearmanTier/
        // RequestResearchArcherTier/RequestResearchCavalryTier/
        // RequestResearchSiegeTier/RequestResearchCharaTier above.
        public void RequestResearchSkirmisherTier()
        {
            if (!IsComplete || IsResearchingSkirmisherTier
                || !SkirmisherLineProgress.HasNextTier(Faction)
                || !SkirmisherLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            SkirmisherTierData next = SkirmisherLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _skirmisherTierResearchRemaining = next.ResearchTime;
        }

        private void TickSkirmisherTierResearch()
        {
            _skirmisherTierResearchRemaining -= Time.deltaTime;
            if (_skirmisherTierResearchRemaining <= 0f)
            {
                SkirmisherLineProgress.AdvanceTier(Faction);
                _skirmisherTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item 20: Battering Ram tier ladder research - same shape as
        // RequestResearchInfantryTier/RequestResearchSpearmanTier/
        // RequestResearchArcherTier/RequestResearchCavalryTier/
        // RequestResearchSiegeTier/RequestResearchCharaTier/
        // RequestResearchSkirmisherTier above.
        public void RequestResearchBatteringRamTier()
        {
            if (!IsComplete || IsResearchingBatteringRamTier
                || !BatteringRamLineProgress.HasNextTier(Faction)
                || !BatteringRamLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            BatteringRamTierData next = BatteringRamLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _batteringRamTierResearchRemaining = next.ResearchTime;
        }

        private void TickBatteringRamTierResearch()
        {
            _batteringRamTierResearchRemaining -= Time.deltaTime;
            if (_batteringRamTierResearchRemaining <= 0f)
            {
                BatteringRamLineProgress.AdvanceTier(Faction);
                _batteringRamTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item: Cavalry Archer tier ladder research - same shape as
        // every other tier line above.
        public void RequestResearchCavalryArcherTier()
        {
            if (!IsComplete || IsResearchingCavalryArcherTier
                || !CavalryArcherLineProgress.HasNextTier(Faction)
                || !CavalryArcherLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            CavalryArcherTierData next = CavalryArcherLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _cavalryArcherTierResearchRemaining = next.ResearchTime;
        }

        private void TickCavalryArcherTierResearch()
        {
            _cavalryArcherTierResearchRemaining -= Time.deltaTime;
            if (_cavalryArcherTierResearchRemaining <= 0f)
            {
                CavalryArcherLineProgress.AdvanceTier(Faction);
                _cavalryArcherTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item 22: Camel Rider tier ladder research - same shape as
        // every other tier line above.
        public void RequestResearchCamelRiderTier()
        {
            if (!IsComplete || IsResearchingCamelRiderTier
                || !CamelRiderLineProgress.HasNextTier(Faction)
                || !CamelRiderLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            CamelRiderTierData next = CamelRiderLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _camelRiderTierResearchRemaining = next.ResearchTime;
        }

        private void TickCamelRiderTierResearch()
        {
            _camelRiderTierResearchRemaining -= Time.deltaTime;
            if (_camelRiderTierResearchRemaining <= 0f)
            {
                CamelRiderLineProgress.AdvanceTier(Faction);
                _camelRiderTierResearchRemaining = -1f;
            }
        }

        // Wave 4 item 23: Scorpion tier ladder research - same shape as
        // every other tier line above.
        public void RequestResearchScorpionTier()
        {
            if (!IsComplete || IsResearchingScorpionTier
                || !ScorpionLineProgress.HasNextTier(Faction)
                || !ScorpionLineProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            ScorpionTierData next = ScorpionLineProgress.NextTierData(Faction);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < next.GoldCost
                || stockpile.GetTotal(ResourceType.Wood) < next.WoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -next.GoldCost);
            stockpile.Add(ResourceType.Wood, -next.WoodCost);
            _scorpionTierResearchRemaining = next.ResearchTime;
        }

        private void TickScorpionTierResearch()
        {
            _scorpionTierResearchRemaining -= Time.deltaTime;
            if (_scorpionTierResearchRemaining <= 0f)
            {
                ScorpionLineProgress.AdvanceTier(Faction);
                _scorpionTierResearchRemaining = -1f;
            }
        }
    }
}

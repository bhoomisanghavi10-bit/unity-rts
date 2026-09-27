using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Identifies this building as a valid drop-off point for gathered
    // resources, and trains Worker units - the Player's via BuildMenu's
    // hotkey/button (BuildMenu owns hotkey dispatch, gated on selection),
    // the AI's via AiController, both funneling through RequestTrain().
    // Mirrors Barracks' training shape
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
        [SerializeField] private float workerFoodCost = 50f;
        [SerializeField] private float trainTime = 6f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(-3f, 0f, 3f);

        private FactionMember _factionMember;
        private RallyPoint _rally;
        private readonly ProductionQueue _queue = new ProductionQueue();
        private float _ageUpRemaining = -1f;
        private AgeId _ageUpTarget;

        // Phase 6 gap-close (deeper tech tree): one shared research slot
        // for economy techs, same "one at a time" shape as Worker training
        // rather than an independent track per tech - deliberately, so
        // researching ImprovedTools vs. PackMules vs. TradeDiscounts first
        // is a real prioritization choice, not something to just start all
        // three of in parallel. Independent from Worker training and
        // Age-up (both already run in parallel here) - a faction can train
        // a Worker, age up, and research an economy tech all at once.
        private float _economyTechRemaining = -1f;
        private EconomyTech _economyTechTarget;

        // Farming upgrade techs (Horse Collar/Heavy Plow/Crop Rotation) -
        // its own independent track, same reasoning as EconomyTech's own
        // comment: age-gated and sequential (see FarmTechProgress), so it
        // runs in parallel with Worker training/Age-up/EconomyTech rather
        // than sharing any of their slots.
        private float _farmTechRemaining = -1f;

        // Self-added in Awake (not lazily like ConstructionSite/FactionMember
        // below) rather than requiring a Factory change: RallyPoint only
        // needs this component's own Building/rallyOffset, both already
        // available at Awake time, unlike the siblings a Factory adds
        // moments later - and it needs to exist as soon as this building is
        // selectable, since a player can right-click a rally point before
        // ever training anything.
        private void Awake()
        {
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

        public bool IsTraining => !_queue.IsEmpty;
        public ProductionQueue Queue => _queue;
        public string LastTrainFailure => _queue.LastFailure;

        // Save/load only: raw countdown of the active item (-1 = none).
        internal float TrainingRemaining => _queue.Active != null ? _queue.Active.Remaining : -1f;

        // Save/load only (legacy single-slot saves): restores one in-progress
        // Worker without re-running the cost checks - already paid.
        internal void RestoreTraining(float remaining)
        {
            _queue.Clear();
            _queue.RestoreItem(new ProductionItem
            {
                Kind = 0,
                Label = "Worker",
                CostTypes = new[] { ResourceType.Food },
                CostAmounts = new[] { workerFoodCost },
                Total = remaining,
                Remaining = remaining,
            });
        }

        // Save/load only: replaces the whole queue with already-paid items.
        internal void RestoreQueue(System.Collections.Generic.IEnumerable<ProductionItem> items)
        {
            _queue.Clear();
            foreach (ProductionItem item in items)
            {
                _queue.RestoreItem(item);
            }
        }

        // Wiping a match for a load/reset: drop the queue with NO refund
        // (the snapshot being restored carries its own resources).
        internal void DiscardQueue()
        {
            _queue.Clear();
        }

        // Destroyed building: refund whatever was still queued.
        internal void RefundQueueForDestruction()
        {
            if (!_queue.IsEmpty && TryGetComponent(out FactionMember member))
            {
                _queue.CancelAll(member.Faction);
            }
        }

        private void OnDestroy()
        {
            // A scene reload (rematch) also destroys this object; the queue
            // simply goes away with it, no refund into a dying scene.
            if (gameObject.scene.isLoaded)
            {
                RefundQueueForDestruction();
            }
        }

        public bool IsAgingUp => _ageUpRemaining >= 0f;
        public AgeId AgeUpTarget => _ageUpTarget;
        public float AgeUpProgress => IsAgingUp ? 1f - (_ageUpRemaining / AgeProfile.For(_ageUpTarget).ResearchTime) : 0f;

        // Item 32 (always-visible Age/research readout): the HUD needs to
        // find "the" TownCenter that's currently aging up for a faction
        // without the player having it selected - scans Building.All the
        // same way AgeUpRequirement.IsMet already does, rather than adding
        // a second TownCenter-specific registry. A faction only ever has
        // one Age-up in flight in practice (AgeProgress is faction-wide),
        // but nothing stops two TownCenters from both being mid-countdown
        // at once - returns the first one found, which is what the readout
        // shows.
        public static TownCenter FindAgingUp(FactionId faction)
        {
            foreach (Building building in All)
            {
                if (building is TownCenter townCenter
                    && townCenter.IsAgingUp
                    && townCenter.TryGetComponent(out FactionMember member)
                    && member.Faction == faction)
                {
                    return townCenter;
                }
            }

            return null;
        }

        public bool IsResearchingEconomyTech => _economyTechRemaining >= 0f;
        public EconomyTech EconomyTechTarget => _economyTechTarget;
        public float EconomyTechResearchProgress => IsResearchingEconomyTech
            ? 1f - (_economyTechRemaining / EconomyTechDefinition.For(_economyTechTarget).ResearchTime)
            : 0f;
        public bool HasResearchedEconomyTech(EconomyTech tech) => EconomyTechProgress.HasResearched(Faction, tech);

        public bool IsResearchingFarmTech => _farmTechRemaining >= 0f;
        public float FarmTechResearchProgress => IsResearchingFarmTech
            ? 1f - (_farmTechRemaining / FarmTechProgress.NextTierResearchTime(Faction))
            : 0f;

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

            if (IsResearchingEconomyTech)
            {
                TickEconomyTech();
            }

            if (IsResearchingFarmTech)
            {
                TickFarmTech();
            }
        }

        public void RequestTrain()
        {
            float ageTrainMultiplier = AgeProfile.For(AgeProgress.CurrentAge(Faction)).TrainTimeMultiplier;
            float time = trainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier * ageTrainMultiplier;
            _queue.TryEnqueue(Faction, 0, "Worker", time, (ResourceType.Food, workerFoodCost));
        }

        private void TickTraining()
        {
            _queue.Tick(Faction, Time.deltaTime, _ =>
            {
                GameObject spawned = WorkerFactory.Spawn(transform.position + rallyOffset, Faction);
                _rally.ApplyTo(spawned);
            });
        }

        public void RequestAgeUp()
        {
            if (IsAgingUp || !AgeProgress.HasNextAge(Faction))
            {
                return;
            }

            AgeId current = AgeProgress.CurrentAge(Faction);
            if (AgeUpRequirement.AppliesTo(current) && !AgeUpRequirement.IsMet(Faction))
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

            // Maurya's Arthashastra Statecraft unique tech (UniqueTechDefinition):
            // -25% Age-up research time, once researched.
            float ageUpTimeMultiplier = UniqueTechProgress.HasResearched(Faction)
                ? UniqueTechDefinition.For(CivilizationRegistry.For(Faction)).AgeUpResearchTimeMultiplier
                : 1f;
            _ageUpRemaining = profile.ResearchTime * ageUpTimeMultiplier;
        }

        private void TickAgeUp()
        {
            _ageUpRemaining -= Time.deltaTime;
            if (_ageUpRemaining <= 0f)
            {
                AgeProgress.Advance(Faction, _ageUpTarget);
                // User-confirmed retroactive re-skin: every standing
                // TownCenter/Tower/Wall this faction owns rebuilds its
                // visual mesh in place right now - see
                // AgeTieredBuildingVisual.
                AgeTieredBuildingVisual.RefreshAllForFaction(Faction, _ageUpTarget);
                _ageUpRemaining = -1f;
            }
        }

        // Phase 6: same gating shape as Barracks' unique-tech research -
        // one-time flag via EconomyTechProgress, not a tiered track.
        public void RequestResearchEconomyTech(EconomyTech tech)
        {
            if (IsResearchingEconomyTech || HasResearchedEconomyTech(tech))
            {
                return;
            }

            EconomyTechDefinition definition = EconomyTechDefinition.For(tech);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Wood) < definition.WoodCost
                || stockpile.GetTotal(ResourceType.Gold) < definition.GoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -definition.WoodCost);
            stockpile.Add(ResourceType.Gold, -definition.GoldCost);
            _economyTechTarget = tech;
            _economyTechRemaining = definition.ResearchTime;
        }

        private void TickEconomyTech()
        {
            _economyTechRemaining -= Time.deltaTime;
            if (_economyTechRemaining <= 0f)
            {
                EconomyTechProgress.MarkResearched(Faction, _economyTechTarget);
                _economyTechRemaining = -1f;
            }
        }

        // Horse Collar/Heavy Plow/Crop Rotation - same shape as
        // RequestResearchEconomyTech but age-gated and sequential (see
        // FarmTechProgress.NextTierAgeRequirementMet), so this refuses
        // outright rather than just checking affordability.
        public void RequestResearchFarmTech()
        {
            if (IsResearchingFarmTech || !FarmTechProgress.NextTierAgeRequirementMet(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            float goldCost = FarmTechProgress.NextTierGoldCost(Faction);
            float woodCost = FarmTechProgress.NextTierWoodCost(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < goldCost
                || stockpile.GetTotal(ResourceType.Wood) < woodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -goldCost);
            stockpile.Add(ResourceType.Wood, -woodCost);
            _farmTechRemaining = FarmTechProgress.NextTierResearchTime(Faction);
        }

        private void TickFarmTech()
        {
            _farmTechRemaining -= Time.deltaTime;
            if (_farmTechRemaining <= 0f)
            {
                FarmTechProgress.Advance(Faction);
                _farmTechRemaining = -1f;
            }
        }
    }
}

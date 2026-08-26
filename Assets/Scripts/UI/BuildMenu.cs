using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.UI
{
    // Context-sensitive build/train menu, bottom-right, AoE-style: which
    // buttons are even visible depends on what's currently selected, not
    // just whether they're enabled.
    //  - A Builder-capable unit selected (and no building selected): shows
    //    Build Barracks/Farm/House/Wall/Gate/Tower/Dock - placement still
    //    needs that worker to walk over and build it afterward. Dock is
    //    additionally gated on WaterProximity.HasWater (see
    //    BuildingPlacer.CanPlaceDock) - no point offering it on a map with
    //    no water rectangle.
    //  - The Player's own Town Center selected: shows Train Worker + Advance
    //    Age, acting on that specific Town Center.
    //  - The Player's own Barracks selected: shows Train Soldier, acting on
    //    that specific Barracks.
    //  - Anything else selected (a Farm/House, an enemy building, nothing):
    //    no action buttons.
    // uGUI/TMP - buttons are real Canvas children wired to these entry
    // points once in Awake via onClick; Update() toggles active/interactable
    // every frame based on current selection instead of re-issuing GUI.Button
    // draw calls.
    public class BuildMenu : MonoBehaviour
    {
        [SerializeField] private Button barracksButton;
        [SerializeField] private TMP_Text barracksLabel;
        [SerializeField] private Button farmButton;
        [SerializeField] private Button houseButton;
        [SerializeField] private Button wallButton;
        [SerializeField] private Button gateButton;
        [SerializeField] private Button towerButton;
        [SerializeField] private Button marketButton;
        [SerializeField] private Button dockButton;
        [SerializeField] private TMP_Text dockLabel;
        [SerializeField] private Button workerButton;
        [SerializeField] private Button soldierButton;
        [SerializeField] private Button archerButton;
        [SerializeField] private Button cavalryButton;
        [SerializeField] private Button siegeButton;
        [SerializeField] private Button spearmanButton;
        [SerializeField] private Button uniqueUnitButton;
        [SerializeField] private TMP_Text uniqueUnitLabel;
        // Roadmap Section 5 item 3: Maurya/Maratha each have a 2nd unique
        // unit (see UniqueUnitDefinition/Barracks.UniqueUnitCount) - hidden
        // entirely for the 3 civs that only have 1 (see Update()).
        [SerializeField] private Button uniqueUnitButton2;
        [SerializeField] private TMP_Text uniqueUnitLabel2;
        [SerializeField] private Button ungarrisonButton;
        [SerializeField] private Button fishingBoatButton;
        [SerializeField] private Button warGalleyButton;
        [SerializeField] private Button sellWoodButton;
        [SerializeField] private TMP_Text sellWoodLabel;
        [SerializeField] private Button buyWoodButton;
        [SerializeField] private TMP_Text buyWoodLabel;
        [SerializeField] private Button sellFoodButton;
        [SerializeField] private TMP_Text sellFoodLabel;
        [SerializeField] private Button buyFoodButton;
        [SerializeField] private TMP_Text buyFoodLabel;
        [SerializeField] private Button sellStoneButton;
        [SerializeField] private TMP_Text sellStoneLabel;
        [SerializeField] private Button buyStoneButton;
        [SerializeField] private TMP_Text buyStoneLabel;
        [SerializeField] private Button attackUpgradeButton;
        [SerializeField] private TMP_Text attackUpgradeLabel;
        [SerializeField] private Button armorUpgradeButton;
        [SerializeField] private TMP_Text armorUpgradeLabel;
        [SerializeField] private Button uniqueTechButton;
        [SerializeField] private TMP_Text uniqueTechLabel;
        [SerializeField] private Button ageButton;
        [SerializeField] private TMP_Text ageLabel;
        [SerializeField] private Button improvedToolsButton;
        [SerializeField] private TMP_Text improvedToolsLabel;
        [SerializeField] private Button packMulesButton;
        [SerializeField] private TMP_Text packMulesLabel;
        [SerializeField] private Button tradeDiscountsButton;
        [SerializeField] private TMP_Text tradeDiscountsLabel;

        // Fixed per-click trade increment - Market.Buy/Sell take an arbitrary
        // amount but nothing in the project has established a convention for
        // letting the player choose one, so BuildMenu picks a flat number.
        private const float MarketTradeAmount = 50f;

        private BuildingPlacer _placer;
        private SelectionManager _selectionManager;

        private void Awake()
        {
            _placer = FindFirstObjectByType<BuildingPlacer>();
            _selectionManager = FindFirstObjectByType<SelectionManager>();

            barracksButton.onClick.AddListener(() => _placer.BeginPlacementBarracks());
            farmButton.onClick.AddListener(() => _placer.BeginPlacementFarm());
            houseButton.onClick.AddListener(() => _placer.BeginPlacementHouse());
            wallButton.onClick.AddListener(() => _placer.BeginPlacementWall());
            gateButton.onClick.AddListener(() => _placer.BeginPlacementGate());
            towerButton.onClick.AddListener(() => _placer.BeginPlacementTower());
            marketButton.onClick.AddListener(() => _placer.BeginPlacementMarket());
            dockButton.onClick.AddListener(() => _placer.BeginPlacementDock());
            workerButton.onClick.AddListener(TrainWorkerAtSelected);
            soldierButton.onClick.AddListener(TrainSoldierAtSelected);
            archerButton.onClick.AddListener(TrainArcherAtSelected);
            cavalryButton.onClick.AddListener(TrainCavalryAtSelected);
            siegeButton.onClick.AddListener(TrainSiegeAtSelected);
            spearmanButton.onClick.AddListener(TrainSpearmanAtSelected);
            uniqueUnitButton.onClick.AddListener(TrainUniqueUnitAtSelected);
            uniqueUnitButton2.onClick.AddListener(TrainUniqueUnit2AtSelected);
            ungarrisonButton.onClick.AddListener(UngarrisonAtSelected);
            fishingBoatButton.onClick.AddListener(TrainFishingBoatAtSelected);
            warGalleyButton.onClick.AddListener(TrainWarGalleyAtSelected);
            sellWoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Wood, sell: true));
            buyWoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Wood, sell: false));
            sellFoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Food, sell: true));
            buyFoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Food, sell: false));
            sellStoneButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Stone, sell: true));
            buyStoneButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Stone, sell: false));
            attackUpgradeButton.onClick.AddListener(ResearchAttackAtSelected);
            armorUpgradeButton.onClick.AddListener(ResearchArmorAtSelected);
            uniqueTechButton.onClick.AddListener(ResearchUniqueTechAtSelected);
            ageButton.onClick.AddListener(RequestAgeUpAtSelected);
            improvedToolsButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.ImprovedTools));
            packMulesButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.PackMules));
            tradeDiscountsButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.TradeDiscounts));
        }

        private void Update()
        {
            Building selected = _selectionManager != null ? _selectionManager.SelectedBuilding : null;
            bool ownsSelected = selected != null
                && selected.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == FactionId.Player;

            bool showPlacement = selected == null
                && _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected();
            TownCenter townCenter = ownsSelected ? selected as TownCenter : null;
            Barracks barracks = ownsSelected ? selected as Barracks : null;
            Dock dock = ownsSelected ? selected as Dock : null;
            Market market = ownsSelected ? selected as Market : null;
            // Roadmap Section 5 item 3: Wall/Tower didn't show any panel
            // before this - Garrison is the only reason either needs one.
            Garrison garrison = (ownsSelected && (selected is Wall || selected is Tower))
                ? selected.GetComponent<Garrison>()
                : null;

            SetPlacementButtonsActive(showPlacement);
            workerButton.gameObject.SetActive(townCenter != null);
            ageButton.gameObject.SetActive(townCenter != null);
            improvedToolsButton.gameObject.SetActive(townCenter != null);
            packMulesButton.gameObject.SetActive(townCenter != null);
            tradeDiscountsButton.gameObject.SetActive(townCenter != null);
            soldierButton.gameObject.SetActive(barracks != null);
            archerButton.gameObject.SetActive(barracks != null);
            cavalryButton.gameObject.SetActive(barracks != null);
            siegeButton.gameObject.SetActive(barracks != null);
            spearmanButton.gameObject.SetActive(barracks != null);
            uniqueUnitButton.gameObject.SetActive(barracks != null);
            uniqueUnitButton2.gameObject.SetActive(barracks != null && barracks.UniqueUnitCount > 1);
            ungarrisonButton.gameObject.SetActive(garrison != null && garrison.HasDurgGarrison);
            attackUpgradeButton.gameObject.SetActive(barracks != null);
            armorUpgradeButton.gameObject.SetActive(barracks != null);
            uniqueTechButton.gameObject.SetActive(barracks != null);
            fishingBoatButton.gameObject.SetActive(dock != null);
            warGalleyButton.gameObject.SetActive(dock != null);
            sellWoodButton.gameObject.SetActive(market != null);
            buyWoodButton.gameObject.SetActive(market != null);
            sellFoodButton.gameObject.SetActive(market != null);
            buyFoodButton.gameObject.SetActive(market != null);
            sellStoneButton.gameObject.SetActive(market != null);
            buyStoneButton.gameObject.SetActive(market != null);

            if (showPlacement)
            {
                barracksButton.interactable = BuildingPlacer.CanPlaceBarracks;
                barracksLabel.text = BuildingPlacer.CanPlaceBarracks
                    ? "Build Barracks (100 Wood, 50 Stone)"
                    : "Build Barracks (Requires Classical Age)";
                farmButton.interactable = true;
                houseButton.interactable = true;
                wallButton.interactable = true;
                gateButton.interactable = true;
                towerButton.interactable = true;
                marketButton.interactable = true;
                dockButton.interactable = BuildingPlacer.CanPlaceDock;
                dockLabel.text = BuildingPlacer.CanPlaceDock
                    ? "Build Dock (80 Wood, 20 Stone)"
                    : "Build Dock (Requires Water)";
            }

            if (townCenter != null)
            {
                UpdateTownCenterButtons(townCenter);
            }

            if (barracks != null)
            {
                UpdateBarracksButtons(barracks);
            }

            if (dock != null)
            {
                UpdateDockButtons(dock);
            }

            if (market != null)
            {
                UpdateMarketButtons(market);
            }
        }

        private void UpdateBarracksButtons(Barracks barracks)
        {
            bool canTrain = barracks.IsComplete && !barracks.IsTraining;
            soldierButton.interactable = canTrain;
            archerButton.interactable = canTrain;
            cavalryButton.interactable = canTrain;
            siegeButton.interactable = canTrain;
            spearmanButton.interactable = canTrain;
            uniqueUnitButton.interactable = canTrain;
            uniqueUnitLabel.text = $"Train {barracks.UniqueUnit.Name} ({(int)barracks.UniqueUnit.FoodCost} Food, {(int)barracks.UniqueUnit.GoldCost} Gold)";

            if (barracks.UniqueUnitCount > 1)
            {
                UniqueUnitDefinition unique2 = barracks.UniqueUnitAt(1);
                uniqueUnitButton2.interactable = canTrain;
                uniqueUnitLabel2.text = $"Train {unique2.Name} ({(int)unique2.FoodCost} Food, {(int)unique2.GoldCost} Gold)";
            }

            UpdateUpgradeButton(
                attackUpgradeButton, attackUpgradeLabel, "Attack",
                barracks.IsComplete, barracks.IsResearchingAttack, barracks.AttackResearchProgress,
                UpgradeProgress.AttackTier(FactionId.Player), UpgradeProgress.HasNextAttackTier(FactionId.Player),
                barracks.NextAttackUpgradeCost);

            UpdateUpgradeButton(
                armorUpgradeButton, armorUpgradeLabel, "Armor",
                barracks.IsComplete, barracks.IsResearchingArmor, barracks.ArmorResearchProgress,
                UpgradeProgress.ArmorTier(FactionId.Player), UpgradeProgress.HasNextArmorTier(FactionId.Player),
                barracks.NextArmorUpgradeCost);

            UpdateUniqueTechButton(barracks);
        }

        private void UpdateDockButtons(Dock dock)
        {
            bool canTrain = dock.IsComplete && !dock.IsTraining;
            fishingBoatButton.interactable = canTrain;
            warGalleyButton.interactable = canTrain;
        }

        // Market trade panel: gating reads the owning faction's stockpile
        // directly rather than adding read-only accessors to Market, same
        // "BuildMenu reaches into the sim state it needs" approach already
        // used for TownCenter/Barracks cost labels above - Market.Sell/Buy
        // stay the only mutators.
        private void UpdateMarketButtons(Market market)
        {
            FactionId faction = BuildingFaction(market);
            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            float goldCost = MarketTradeAmount * market.EffectiveBuyRate;
            float goldPayout = MarketTradeAmount * market.EffectiveSellRate;

            UpdateTradeButton(sellWoodButton, sellWoodLabel, "Sell", "Wood", stockpile.GetTotal(ResourceType.Wood) >= MarketTradeAmount, goldPayout);
            UpdateTradeButton(buyWoodButton, buyWoodLabel, "Buy", "Wood", stockpile.GetTotal(ResourceType.Gold) >= goldCost, goldCost);
            UpdateTradeButton(sellFoodButton, sellFoodLabel, "Sell", "Food", stockpile.GetTotal(ResourceType.Food) >= MarketTradeAmount, goldPayout);
            UpdateTradeButton(buyFoodButton, buyFoodLabel, "Buy", "Food", stockpile.GetTotal(ResourceType.Gold) >= goldCost, goldCost);
            UpdateTradeButton(sellStoneButton, sellStoneLabel, "Sell", "Stone", stockpile.GetTotal(ResourceType.Stone) >= MarketTradeAmount, goldPayout);
            UpdateTradeButton(buyStoneButton, buyStoneLabel, "Buy", "Stone", stockpile.GetTotal(ResourceType.Gold) >= goldCost, goldCost);
        }

        private static void UpdateTradeButton(Button button, TMP_Text label, string verb, string resourceName, bool canAfford, float goldAmount)
        {
            button.interactable = canAfford;
            label.text = verb == "Sell"
                ? $"Sell {(int)MarketTradeAmount} {resourceName} ({(int)goldAmount} Gold)"
                : $"Buy {(int)MarketTradeAmount} {resourceName} ({(int)goldAmount} Gold)";
        }

        // Phase 6: separate from UpdateUpgradeButton since a unique tech
        // has exactly one level (no tier/hasNextTier concept) - "Max" would
        // be a confusing label for something that was never tiered at all.
        private void UpdateUniqueTechButton(Barracks barracks)
        {
            UniqueTechDefinition tech = barracks.UniqueTech;

            if (barracks.IsResearchingUniqueTech)
            {
                uniqueTechButton.interactable = false;
                uniqueTechLabel.text = $"Researching {tech.Name}... {(int)(barracks.UniqueTechResearchProgress * 100f)}%";
                return;
            }

            if (barracks.HasResearchedUniqueTech)
            {
                uniqueTechButton.interactable = false;
                uniqueTechLabel.text = $"{tech.Name} (Researched)";
                return;
            }

            uniqueTechButton.interactable = barracks.IsComplete;
            uniqueTechLabel.text = $"{tech.Name} ({(int)tech.GoldCost} Gold)";
        }

        private static void UpdateUpgradeButton(
            Button button, TMP_Text label, string trackName,
            bool barracksComplete, bool isResearching, float progress, int tier, bool hasNextTier, float goldCost)
        {
            if (isResearching)
            {
                button.interactable = false;
                label.text = $"Researching {trackName}... {(int)(progress * 100f)}%";
                return;
            }

            if (!hasNextTier)
            {
                button.interactable = false;
                label.text = $"{trackName} (Max)";
                return;
            }

            button.interactable = barracksComplete;
            label.text = $"Upgrade {trackName} (Tier {tier + 1}, {(int)goldCost} Gold)";
        }

        private void SetPlacementButtonsActive(bool active)
        {
            barracksButton.gameObject.SetActive(active);
            farmButton.gameObject.SetActive(active);
            houseButton.gameObject.SetActive(active);
            wallButton.gameObject.SetActive(active);
            gateButton.gameObject.SetActive(active);
            towerButton.gameObject.SetActive(active);
            marketButton.gameObject.SetActive(active);
            dockButton.gameObject.SetActive(active);
        }

        private void UpdateTownCenterButtons(TownCenter townCenter)
        {
            workerButton.interactable = !townCenter.IsTraining;

            UpdateEconomyTechButton(improvedToolsButton, improvedToolsLabel, townCenter, EconomyTech.ImprovedTools);
            UpdateEconomyTechButton(packMulesButton, packMulesLabel, townCenter, EconomyTech.PackMules);
            UpdateEconomyTechButton(tradeDiscountsButton, tradeDiscountsLabel, townCenter, EconomyTech.TradeDiscounts);

            if (townCenter.IsAgingUp)
            {
                ageButton.interactable = false;
                ageLabel.text = $"Researching Age... {(int)(townCenter.AgeUpProgress * 100f)}%";
                return;
            }

            if (!AgeProgress.HasNextAge(FactionId.Player))
            {
                ageButton.interactable = false;
                ageLabel.text = "Imperial Age (Max)";
                return;
            }

            AgeProfile next = AgeProfile.For(AgeProgress.NextAge(FactionId.Player));
            ageButton.interactable = true;
            ageLabel.text = $"Advance to {next.DisplayName} ({(int)next.WoodCost} Wood, {(int)next.StoneCost} Stone)";
        }

        // Phase 6 gap-close (deeper tech tree): economy techs share one
        // research slot on TownCenter (see TownCenter.IsResearchingEconomyTech),
        // so a button for a tech that ISN'T the one currently researching
        // still shows its own cost but goes non-interactable - "something
        // else is already using the slot," not "this specific tech is
        // unavailable."
        private static void UpdateEconomyTechButton(Button button, TMP_Text label, TownCenter townCenter, EconomyTech tech)
        {
            EconomyTechDefinition definition = EconomyTechDefinition.For(tech);

            if (townCenter.IsResearchingEconomyTech && townCenter.EconomyTechTarget == tech)
            {
                button.interactable = false;
                label.text = $"Researching {definition.Name}... {(int)(townCenter.EconomyTechResearchProgress * 100f)}%";
                return;
            }

            if (townCenter.HasResearchedEconomyTech(tech))
            {
                button.interactable = false;
                label.text = $"{definition.Name} (Researched)";
                return;
            }

            button.interactable = !townCenter.IsResearchingEconomyTech;
            label.text = $"{definition.Name} ({(int)definition.WoodCost} Wood, {(int)definition.GoldCost} Gold)";
        }

        private void TrainWorkerAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is TownCenter townCenter)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(townCenter), townCenter, townCenter.RequestTrain));
            }
        }

        private void TrainSoldierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, barracks.RequestTrain));
            }
        }

        private void TrainArcherAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, barracks.RequestTrainArcher));
            }
        }

        private void TrainCavalryAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, barracks.RequestTrainCavalry));
            }
        }

        private void TrainSiegeAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, barracks.RequestTrainSiege));
            }
        }

        private void TrainSpearmanAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, barracks.RequestTrainSpearman));
            }
        }

        private void TrainUniqueUnitAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, barracks.RequestTrainUniqueUnit));
            }
        }

        private void TrainUniqueUnit2AtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(barracks), barracks, () => barracks.RequestTrainUniqueUnit(1)));
            }
        }

        // Roadmap Section 5 item 3: no train cost/command needed - same
        // direct-call convention TradeAtSelected uses for Market.Sell/Buy.
        private void UngarrisonAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding != null
                && _selectionManager.SelectedBuilding.TryGetComponent(out Garrison garrison))
            {
                garrison.Ungarrison();
            }
        }

        private void TrainFishingBoatAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(dock), dock, dock.RequestTrainFishingBoat));
            }
        }

        private void TrainWarGalleyAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                CommandBus.Enqueue(new TrainCommand(BuildingFaction(dock), dock, dock.RequestTrainWarGalley));
            }
        }

        private void TradeAtSelected(ResourceType type, bool sell)
        {
            if (_selectionManager == null || _selectionManager.SelectedBuilding is not Market market)
            {
                return;
            }

            if (sell)
            {
                market.Sell(type, MarketTradeAmount);
            }
            else
            {
                market.Buy(type, MarketTradeAmount);
            }
        }

        private static FactionId BuildingFaction(Component building)
        {
            return building.TryGetComponent(out FactionMember member) ? member.Faction : FactionId.Player;
        }

        private void ResearchAttackAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchAttack();
            }
        }

        private void ResearchArmorAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchArmor();
            }
        }

        private void ResearchUniqueTechAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchUniqueTech();
            }
        }

        private void RequestAgeUpAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is TownCenter townCenter
                && !townCenter.IsAgingUp && AgeProgress.HasNextAge(FactionId.Player))
            {
                townCenter.RequestAgeUp();
            }
        }

        private void ResearchEconomyTechAtSelected(EconomyTech tech)
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is TownCenter townCenter)
            {
                townCenter.RequestResearchEconomyTech(tech);
            }
        }

        private bool HasBuilderSelected()
        {
            if (_selectionManager == null)
            {
                return false;
            }

            foreach (Unit unit in _selectionManager.Selected)
            {
                if (unit.TryGetComponent(out Builder _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

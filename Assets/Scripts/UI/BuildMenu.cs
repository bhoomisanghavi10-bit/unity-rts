using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.UI
{
    // Context-sensitive build/train menu, AoE-style: which buttons are even
    // visible depends on what's currently selected, not just whether they're
    // enabled. The command panel (leftmost) of the shared bottom-docked bar
    // (Roadmap item 30) - the info panel (SelectedUnitPanel + MatchStatus)
    // sits center, the minimap right. Still the same tall vertical button
    // column it always was; only where it docks changed.
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
        // Phase 3.1 (resource-specific drop-offs): Lumber Camp/Mining
        // Camp/Mill - no matching icon asset yet, stay text-only same as
        // ungarrisonButton/fishingBoatButton/etc.
        [SerializeField] private Button lumberCampButton;
        [SerializeField] private Button miningCampButton;
        [SerializeField] private Button millButton;
        // Wave 2 item 7: the Durg building - placement button, gated the
        // same way barracksButton/dockButton are (interactable +
        // requirement-text label when not yet available).
        [SerializeField] private Button durgButton;
        [SerializeField] private TMP_Text durgLabel;
        // Wave 2 item 8: Karmashala (Blacksmith-equivalent) - placement
        // button, same gating shape as durgButton/barracksButton above.
        [SerializeField] private Button karmashalaButton;
        [SerializeField] private TMP_Text karmashalaLabel;
        // Wave 4 item 27: Monastery - placement button, same gating shape
        // as durgButton/karmashalaButton above.
        [SerializeField] private Button monasteryButton;
        [SerializeField] private TMP_Text monasteryLabel;
        [SerializeField] private Button workerButton;
        [SerializeField] private Button soldierButton;
        [SerializeField] private Button archerButton;
        [SerializeField] private Button cavalryButton;
        [SerializeField] private Button siegeButton;
        [SerializeField] private Button spearmanButton;
        // Wave 4 item 18: Scout (Chara) - the first Wave 4 item, a
        // wholly-new trainable unit, not a tier upgrade to an existing one
        // (see spearmanButton for the last precedent of this shape).
        [SerializeField] private Button charaButton;
        // Wave 4 item 19: Skirmisher - the second wholly-new Wave 4 unit
        // (see charaButton for the prior precedent of this shape).
        [SerializeField] private Button skirmisherButton;
        // Wave 4 item 20: Battering Ram - the third wholly-new Wave 4 unit
        // (see charaButton/skirmisherButton for the prior precedent of this
        // shape).
        [SerializeField] private Button batteringRamButton;
        // Wave 4 item: Cavalry Archer - the fourth wholly-new Wave 4 unit
        // (see charaButton/skirmisherButton/batteringRamButton for the
        // prior precedent of this shape).
        [SerializeField] private Button cavalryArcherButton;
        // Wave 4 item 22: Camel Rider - the fifth wholly-new Wave 4 unit
        // (see charaButton/skirmisherButton/batteringRamButton/
        // cavalryArcherButton for the prior precedent of this shape).
        [SerializeField] private Button camelRiderButton;
        // Wave 4 item 23: Scorpion - the sixth wholly-new Wave 4 unit (see
        // charaButton/skirmisherButton/batteringRamButton/
        // cavalryArcherButton/camelRiderButton for the prior precedent of
        // this shape).
        [SerializeField] private Button scorpionButton;
        // Wave 4 item 24: Trebuchet - a Barracks train button with
        // deliberately no matching tier button (single-tier unit, same
        // "train-only" shape as charaButton), and the first whose own
        // interactable state depends on the current Age directly (Imperial
        // only - see UpdateBarracksButtons).
        [SerializeField] private Button trebuchetButton;
        [SerializeField] private TMP_Text trebuchetLabel;
        [SerializeField] private Button uniqueUnitButton;
        [SerializeField] private TMP_Text uniqueUnitLabel;
        // Roadmap Section 5 item 3: Maurya/Maratha each have a 2nd unique
        // unit (see UniqueUnitDefinition/Durg.UniqueUnitCount - moved from
        // Barracks in Wave 2 item 7) - hidden entirely for the 3 civs that
        // only have 1 (see Update()).
        [SerializeField] private Button uniqueUnitButton2;
        [SerializeField] private TMP_Text uniqueUnitLabel2;
        // Wave 4 item 28: Maharaja hero training - acts on a selected Durg
        // like the unique-unit buttons above, but an independent track
        // (Durg.RequestTrainHero/IsTrainingHero) that doesn't block or get
        // blocked by unique-unit training, and has no per-civ variant (one
        // shared unit for all 5 civs, no slot-2 button needed).
        [SerializeField] private Button heroButton;
        [SerializeField] private TMP_Text heroLabel;
        [SerializeField] private Button ungarrisonButton;
        [SerializeField] private Button fishingBoatButton;
        [SerializeField] private Button warGalleyButton;
        // Wave 3 item 15: Naval tier ladder research button, same gating
        // shape as siegeTierButton - acts on a selected Dock (War Galley
        // trains there, not Barracks).
        [SerializeField] private Button navalTierButton;
        [SerializeField] private TMP_Text navalTierLabel;
        // Wave 4 item 25: Fire Ship - a wholly-new trainable unit, not a
        // tier upgrade to an existing one (see charaButton for the first
        // precedent of this shape) - and its own tier ladder button, same
        // gating shape as navalTierButton, an independent research track
        // on the same Dock.
        [SerializeField] private Button fireShipButton;
        [SerializeField] private Button fireShipTierButton;
        [SerializeField] private TMP_Text fireShipTierLabel;
        // Wave 4 item 26: Trade Ship, the naval Trader - trains on the same
        // selected Dock as every other naval unit, no tier ladder of its
        // own.
        [SerializeField] private Button tradeShipButton;
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
        // Wave 4 item 26: Vanik, the land Trader - trains on the same
        // selected Market as the Sell/Buy buttons above, no tier ladder of
        // its own.
        [SerializeField] private Button vanikButton;
        [SerializeField] private TMP_Text vanikLabel;
        // Wave 4 item 27: Vaidya/Purohita - train on a selected Monastery,
        // same "no tier ladder" shape as vanikButton.
        [SerializeField] private Button vaidyaButton;
        [SerializeField] private Button purohitaButton;
        [SerializeField] private Button attackUpgradeButton;
        [SerializeField] private TMP_Text attackUpgradeLabel;
        [SerializeField] private Button armorUpgradeButton;
        [SerializeField] private TMP_Text armorUpgradeLabel;
        [SerializeField] private Button uniqueTechButton;
        [SerializeField] private TMP_Text uniqueTechLabel;
        // Wave 3 item 9: Infantry tier ladder research button, same gating
        // shape as uniqueTechButton - acts on a selected Barracks.
        [SerializeField] private Button infantryTierButton;
        [SerializeField] private TMP_Text infantryTierLabel;
        // Wave 3 item 10: Spearman tier ladder research button, same
        // gating shape as infantryTierButton - acts on a selected Barracks.
        [SerializeField] private Button spearmanTierButton;
        [SerializeField] private TMP_Text spearmanTierLabel;
        // Wave 3 item 11: Archer tier ladder research button, same gating
        // shape as spearmanTierButton - acts on a selected Barracks.
        [SerializeField] private Button archerTierButton;
        [SerializeField] private TMP_Text archerTierLabel;
        // Wave 3 item 12: Cavalry tier ladder research button, same gating
        // shape as archerTierButton - acts on a selected Barracks.
        [SerializeField] private Button cavalryTierButton;
        [SerializeField] private TMP_Text cavalryTierLabel;
        // Wave 3 item 14: Siege tier ladder research button, same gating
        // shape as cavalryTierButton - acts on a selected Barracks.
        [SerializeField] private Button siegeTierButton;
        [SerializeField] private TMP_Text siegeTierLabel;
        // Wave 3 item 13: Elephant tier ladder research button - unlike
        // every other tier button, acts on a selected Durg (War Elephants
        // train there, not Barracks), and only shown when that civ's Durg
        // actually trains a War Elephant (Durg.TrainsElephant).
        [SerializeField] private Button elephantTierButton;
        [SerializeField] private TMP_Text elephantTierLabel;
        // Wave 3 item 16: Unique-unit Elite tier research buttons - acts on
        // a selected Durg like elephantTierButton, but gated per-slot via
        // Durg.TrainsEliteEligible (data-driven, not a civ switch) since
        // eligibility depends on which unitId trains at that slot, not the
        // slot index itself (e.g. Maurya's slot 0 - War Elephant - is NOT
        // eligible, only slot 1 - Pillar Edict Scholar - is).
        [SerializeField] private Button eliteTierButton;
        [SerializeField] private TMP_Text eliteTierLabel;
        [SerializeField] private Button eliteTierButton2;
        [SerializeField] private TMP_Text eliteTierLabel2;
        // Wave 4 item 18: Scout tier ladder research button, same gating
        // shape as siegeTierButton - acts on a selected Barracks.
        [SerializeField] private Button charaTierButton;
        [SerializeField] private TMP_Text charaTierLabel;
        // Wave 4 item 19: Skirmisher tier ladder research button, same
        // gating shape as charaTierButton - acts on a selected Barracks.
        [SerializeField] private Button skirmisherTierButton;
        [SerializeField] private TMP_Text skirmisherTierLabel;
        // Wave 4 item 20: Battering Ram tier ladder research button, same
        // gating shape as skirmisherTierButton - acts on a selected
        // Barracks.
        [SerializeField] private Button batteringRamTierButton;
        [SerializeField] private TMP_Text batteringRamTierLabel;
        // Wave 4 item: Cavalry Archer tier ladder research button, same
        // gating shape as batteringRamTierButton - acts on a selected
        // Barracks.
        [SerializeField] private Button cavalryArcherTierButton;
        [SerializeField] private TMP_Text cavalryArcherTierLabel;
        // Wave 4 item 22: Camel Rider tier ladder research button, same
        // gating shape as cavalryArcherTierButton - acts on a selected
        // Barracks.
        [SerializeField] private Button camelRiderTierButton;
        [SerializeField] private TMP_Text camelRiderTierLabel;
        // Wave 4 item 23: Scorpion tier ladder research button, same
        // gating shape as camelRiderTierButton - acts on a selected
        // Barracks.
        [SerializeField] private Button scorpionTierButton;
        [SerializeField] private TMP_Text scorpionTierLabel;
        [SerializeField] private Button ageButton;
        [SerializeField] private TMP_Text ageLabel;
        [SerializeField] private Button improvedToolsButton;
        [SerializeField] private TMP_Text improvedToolsLabel;
        [SerializeField] private Button packMulesButton;
        [SerializeField] private TMP_Text packMulesLabel;
        [SerializeField] private Button tradeDiscountsButton;
        [SerializeField] private TMP_Text tradeDiscountsLabel;

        // Roadmap item 31: command-panel icon grid + paging, replacing the
        // old fixed-Y vertical stack. gridPageLabel/nav buttons are hidden
        // whenever everything fits on one page (LayoutCommandGrid).
        [SerializeField] private Button gridPrevButton;
        [SerializeField] private Button gridNextButton;
        [SerializeField] private TMP_Text gridPageLabel;

        // Fixed per-click trade increment - Market.Buy/Sell take an arbitrary
        // amount but nothing in the project has established a convention for
        // letting the player choose one, so BuildMenu picks a flat number.
        private const float MarketTradeAmount = 50f;

        // Item 1 (Hotkeys): dispatch lives here, not on each building's own
        // Update(), because this is the one place that already knows which
        // building is actually SELECTED. The old per-building
        // Input.GetKeyDown(trainKey) checks (Barracks/Dock/TownCenter) only
        // checked "do I belong to the Player", so pressing e.g. G trained a
        // Worker at every idle Player TownCenter at once, not just the
        // selected one - a real bug, not just a coverage gap. Letters are
        // reused freely across TownCenter/Barracks/Dock/Garrison contexts
        // below since those selections are mutually exclusive; none of them
        // reuse the 3 truly-global keys (V/R/C on SelectionManager, which
        // act on unit selection regardless of what building is selected).
        private KeyCode _keyTrainWorker;
        private KeyCode _keyAdvanceAge;
        private KeyCode _keyResearchImprovedTools;
        private KeyCode _keyResearchPackMules;
        private KeyCode _keyResearchTradeDiscounts;
        private KeyCode _keyTrainSoldier;
        private KeyCode _keyTrainArcher;
        private KeyCode _keyTrainCavalry;
        private KeyCode _keyTrainSiege;
        private KeyCode _keyTrainSpearman;
        private KeyCode _keyTrainChara;
        private KeyCode _keyTrainSkirmisher;
        private KeyCode _keyTrainBatteringRam;
        private KeyCode _keyTrainCavalryArcher;
        private KeyCode _keyTrainCamelRider;
        private KeyCode _keyTrainScorpion;
        private KeyCode _keyTrainTrebuchet;
        private KeyCode _keyTrainUniqueUnit;
        private KeyCode _keyTrainUniqueUnit2;
        private KeyCode _keyTrainHero;
        private KeyCode _keyResearchAttack;
        private KeyCode _keyResearchArmor;
        private KeyCode _keyResearchUniqueTech;
        private KeyCode _keyResearchInfantryTier;
        private KeyCode _keyResearchSpearmanTier;
        private KeyCode _keyResearchArcherTier;
        private KeyCode _keyResearchCavalryTier;
        private KeyCode _keyResearchSiegeTier;
        private KeyCode _keyResearchElephantTier;
        private KeyCode _keyResearchEliteTier;
        private KeyCode _keyResearchEliteTier2;
        private KeyCode _keyResearchCharaTier;
        private KeyCode _keyResearchSkirmisherTier;
        private KeyCode _keyResearchBatteringRamTier;
        private KeyCode _keyResearchCavalryArcherTier;
        private KeyCode _keyResearchCamelRiderTier;
        private KeyCode _keyResearchScorpionTier;
        private KeyCode _keyTrainFishingBoat;
        private KeyCode _keyTrainWarGalley;
        private KeyCode _keyResearchNavalTier;
        private KeyCode _keyTrainFireShip;
        private KeyCode _keyResearchFireShipTier;
        // Wave 4 item 26.
        private KeyCode _keyTrainTradeShip;
        private KeyCode _keyTrainVanik;
        // Wave 4 item 27.
        private KeyCode _keyTrainVaidya;
        private KeyCode _keyTrainPurohita;
        private KeyCode _keyUngarrison;

        private BuildingPlacer _placer;
        private SelectionManager _selectionManager;

        // Roadmap item 31: fixed declared order for the icon grid - every
        // button below in one flat array, in the same order they're already
        // declared as fields above (which already groups them sensibly by
        // context, since only one context's buttons are ever active at
        // once, save for the always-available ungarrisonButton). Built once
        // in Awake so LayoutCommandGrid never allocates per-frame.
        private Button[] _allGridButtons;
        private const int GridColumns = 4;
        private const float GridCellSize = 48f;
        private const float GridGap = 4f;
        private const float GridMargin = 8f;
        // Rows that fit under the panel's own height (490) once the bottom
        // strip is reserved for the page nav row - comfortably covers
        // today's real worst case (Barracks, 23 buttons) in a single page;
        // paging exists as a safety valve for whatever wave adds the 33rd.
        private const int GridRows = 7;
        private const int GridCapacity = GridColumns * GridRows;
        private int _gridPage;
        private object _lastGridContextKey;
        private static Sprite _placeholderIcon;

        private void Awake()
        {
            _placer = FindFirstObjectByType<BuildingPlacer>();
            _selectionManager = FindFirstObjectByType<SelectionManager>();

            ApplyKeySettings();
            ApplyTheme();

            barracksButton.onClick.AddListener(() => _placer.BeginPlacementBarracks());
            farmButton.onClick.AddListener(() => _placer.BeginPlacementFarm());
            houseButton.onClick.AddListener(() => _placer.BeginPlacementHouse());
            wallButton.onClick.AddListener(() => _placer.BeginPlacementWall());
            gateButton.onClick.AddListener(() => _placer.BeginPlacementGate());
            towerButton.onClick.AddListener(() => _placer.BeginPlacementTower());
            marketButton.onClick.AddListener(() => _placer.BeginPlacementMarket());
            dockButton.onClick.AddListener(() => _placer.BeginPlacementDock());
            lumberCampButton.onClick.AddListener(() => _placer.BeginPlacementLumberCamp());
            miningCampButton.onClick.AddListener(() => _placer.BeginPlacementMiningCamp());
            millButton.onClick.AddListener(() => _placer.BeginPlacementMill());
            durgButton.onClick.AddListener(() => _placer.BeginPlacementDurg());
            karmashalaButton.onClick.AddListener(() => _placer.BeginPlacementKarmashala());
            monasteryButton.onClick.AddListener(() => _placer.BeginPlacementMonastery());
            workerButton.onClick.AddListener(TrainWorkerAtSelected);
            soldierButton.onClick.AddListener(TrainSoldierAtSelected);
            archerButton.onClick.AddListener(TrainArcherAtSelected);
            cavalryButton.onClick.AddListener(TrainCavalryAtSelected);
            siegeButton.onClick.AddListener(TrainSiegeAtSelected);
            spearmanButton.onClick.AddListener(TrainSpearmanAtSelected);
            charaButton.onClick.AddListener(TrainCharaAtSelected);
            skirmisherButton.onClick.AddListener(TrainSkirmisherAtSelected);
            batteringRamButton.onClick.AddListener(TrainBatteringRamAtSelected);
            cavalryArcherButton.onClick.AddListener(TrainCavalryArcherAtSelected);
            camelRiderButton.onClick.AddListener(TrainCamelRiderAtSelected);
            scorpionButton.onClick.AddListener(TrainScorpionAtSelected);
            trebuchetButton.onClick.AddListener(TrainTrebuchetAtSelected);
            uniqueUnitButton.onClick.AddListener(TrainUniqueUnitAtSelected);
            uniqueUnitButton2.onClick.AddListener(TrainUniqueUnit2AtSelected);
            heroButton.onClick.AddListener(TrainHeroAtSelected);
            ungarrisonButton.onClick.AddListener(UngarrisonAtSelected);
            fishingBoatButton.onClick.AddListener(TrainFishingBoatAtSelected);
            warGalleyButton.onClick.AddListener(TrainWarGalleyAtSelected);
            navalTierButton.onClick.AddListener(ResearchNavalTierAtSelected);
            fireShipButton.onClick.AddListener(TrainFireShipAtSelected);
            fireShipTierButton.onClick.AddListener(ResearchFireShipTierAtSelected);
            tradeShipButton.onClick.AddListener(TrainTradeShipAtSelected);
            vanikButton.onClick.AddListener(TrainVanikAtSelected);
            vaidyaButton.onClick.AddListener(TrainVaidyaAtSelected);
            purohitaButton.onClick.AddListener(TrainPurohitaAtSelected);
            sellWoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Wood, sell: true));
            buyWoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Wood, sell: false));
            sellFoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Food, sell: true));
            buyFoodButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Food, sell: false));
            sellStoneButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Stone, sell: true));
            buyStoneButton.onClick.AddListener(() => TradeAtSelected(ResourceType.Stone, sell: false));
            attackUpgradeButton.onClick.AddListener(ResearchAttackAtSelected);
            armorUpgradeButton.onClick.AddListener(ResearchArmorAtSelected);
            uniqueTechButton.onClick.AddListener(ResearchUniqueTechAtSelected);
            infantryTierButton.onClick.AddListener(ResearchInfantryTierAtSelected);
            spearmanTierButton.onClick.AddListener(ResearchSpearmanTierAtSelected);
            archerTierButton.onClick.AddListener(ResearchArcherTierAtSelected);
            cavalryTierButton.onClick.AddListener(ResearchCavalryTierAtSelected);
            siegeTierButton.onClick.AddListener(ResearchSiegeTierAtSelected);
            elephantTierButton.onClick.AddListener(ResearchElephantTierAtSelected);
            eliteTierButton.onClick.AddListener(ResearchEliteTierAtSelected);
            eliteTierButton2.onClick.AddListener(ResearchEliteTier2AtSelected);
            charaTierButton.onClick.AddListener(ResearchCharaTierAtSelected);
            skirmisherTierButton.onClick.AddListener(ResearchSkirmisherTierAtSelected);
            batteringRamTierButton.onClick.AddListener(ResearchBatteringRamTierAtSelected);
            cavalryArcherTierButton.onClick.AddListener(ResearchCavalryArcherTierAtSelected);
            camelRiderTierButton.onClick.AddListener(ResearchCamelRiderTierAtSelected);
            scorpionTierButton.onClick.AddListener(ResearchScorpionTierAtSelected);
            ageButton.onClick.AddListener(RequestAgeUpAtSelected);
            improvedToolsButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.ImprovedTools));
            packMulesButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.PackMules));
            tradeDiscountsButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.TradeDiscounts));

            // Roadmap item 31: fixed order used by LayoutCommandGrid every
            // frame - matches the field declaration order above.
            _allGridButtons = new[]
            {
                barracksButton, farmButton, houseButton, wallButton, gateButton, towerButton,
                marketButton, dockButton, lumberCampButton, miningCampButton, millButton,
                durgButton, karmashalaButton, monasteryButton, workerButton, soldierButton, archerButton,
                cavalryButton, siegeButton, spearmanButton, charaButton, skirmisherButton,
                batteringRamButton, cavalryArcherButton, camelRiderButton, scorpionButton, trebuchetButton,
                uniqueUnitButton, uniqueUnitButton2, heroButton, ungarrisonButton, fishingBoatButton,
                warGalleyButton, navalTierButton, fireShipButton, fireShipTierButton,
                tradeShipButton,
                sellWoodButton, buyWoodButton, sellFoodButton, buyFoodButton, sellStoneButton,
                buyStoneButton, vanikButton, vaidyaButton, purohitaButton, attackUpgradeButton, armorUpgradeButton, uniqueTechButton,
                infantryTierButton, spearmanTierButton, archerTierButton, cavalryTierButton,
                siegeTierButton, elephantTierButton, eliteTierButton, eliteTierButton2,
                charaTierButton, skirmisherTierButton, batteringRamTierButton,
                cavalryArcherTierButton, camelRiderTierButton, scorpionTierButton, ageButton,
                improvedToolsButton, packMulesButton, tradeDiscountsButton,
            };

            if (gridPrevButton != null)
            {
                gridPrevButton.onClick.AddListener(() => _gridPage--);
            }

            if (gridNextButton != null)
            {
                gridNextButton.onClick.AddListener(() => _gridPage++);
            }
        }

        // Item 1 (Hotkeys): same per-field GameSettings override pattern
        // BuildingPlacer.ApplyKeySettings uses for placement keys, just for
        // the train/research/ungarrison actions instead.
        private void ApplyKeySettings()
        {
            _keyTrainWorker = GameSettings.GetKey("TrainWorker", KeyCode.G);
            _keyAdvanceAge = GameSettings.GetKey("AdvanceAge", KeyCode.Y);
            _keyResearchImprovedTools = GameSettings.GetKey("ResearchImprovedTools", KeyCode.I);
            _keyResearchPackMules = GameSettings.GetKey("ResearchPackMules", KeyCode.P);
            _keyResearchTradeDiscounts = GameSettings.GetKey("ResearchTradeDiscounts", KeyCode.D);
            _keyTrainSoldier = GameSettings.GetKey("TrainUnit", KeyCode.T);
            _keyTrainArcher = GameSettings.GetKey("TrainArcher", KeyCode.A);
            _keyTrainCavalry = GameSettings.GetKey("TrainCavalry", KeyCode.N);
            _keyTrainSiege = GameSettings.GetKey("TrainSiege", KeyCode.S);
            _keyTrainSpearman = GameSettings.GetKey("TrainSpearman", KeyCode.E);
            _keyTrainChara = GameSettings.GetKey("TrainChara", KeyCode.F);
            _keyTrainSkirmisher = GameSettings.GetKey("TrainSkirmisher", KeyCode.C);
            // Wave 4 item 20: D/R aren't used anywhere in the Barracks
            // context yet (D = TradeDiscounts/PlaceDurg, R = ElephantTier/
            // PlaceKarmashala - all in mutually exclusive TownCenter/Durg
            // contexts) - reused freely per this file's own established
            // convention (see HandleHotkeys' header comment).
            _keyTrainBatteringRam = GameSettings.GetKey("TrainBatteringRam", KeyCode.D);
            // Wave 4 item: K/P aren't used anywhere in the Barracks context
            // yet (K = ResearchArmor on Karmashala, P = ResearchPackMules
            // on TownCenter - both mutually exclusive with a selected
            // Barracks) - reused freely per this file's own established
            // convention (see HandleHotkeys' header comment).
            _keyTrainCavalryArcher = GameSettings.GetKey("TrainCavalryArcher", KeyCode.K);
            _keyTrainCamelRider = GameSettings.GetKey("TrainCamelRider", KeyCode.U);
            _keyTrainScorpion = GameSettings.GetKey("TrainScorpion", KeyCode.W);
            // Wave 4 item 24: Q is unused within the Barracks context
            // specifically (TrainUniqueUnit on Durg - mutually exclusive
            // selection context) - reused freely per this file's own
            // established convention.
            _keyTrainTrebuchet = GameSettings.GetKey("TrainTrebuchet", KeyCode.Q);
            _keyTrainUniqueUnit = GameSettings.GetKey("TrainUniqueUnit", KeyCode.Q);
            _keyTrainUniqueUnit2 = GameSettings.GetKey("TrainUniqueUnit2", KeyCode.Z);
            // Wave 4 item 28: M is unused within the Durg context
            // specifically (ResearchCavalryTier on Barracks - mutually
            // exclusive) - reused freely per this file's own established
            // convention.
            _keyTrainHero = GameSettings.GetKey("TrainHero", KeyCode.M);
            _keyResearchAttack = GameSettings.GetKey("ResearchAttack", KeyCode.U);
            _keyResearchArmor = GameSettings.GetKey("ResearchArmor", KeyCode.K);
            _keyResearchUniqueTech = GameSettings.GetKey("ResearchUniqueTech", KeyCode.J);
            _keyResearchInfantryTier = GameSettings.GetKey("ResearchInfantryTier", KeyCode.I);
            _keyResearchSpearmanTier = GameSettings.GetKey("ResearchSpearmanTier", KeyCode.L);
            _keyResearchArcherTier = GameSettings.GetKey("ResearchArcherTier", KeyCode.H);
            _keyResearchCavalryTier = GameSettings.GetKey("ResearchCavalryTier", KeyCode.M);
            _keyResearchSiegeTier = GameSettings.GetKey("ResearchSiegeTier", KeyCode.O);
            _keyResearchElephantTier = GameSettings.GetKey("ResearchElephantTier", KeyCode.R);
            _keyResearchEliteTier = GameSettings.GetKey("ResearchEliteTier", KeyCode.F);
            _keyResearchEliteTier2 = GameSettings.GetKey("ResearchEliteTier2", KeyCode.G);
            _keyResearchCharaTier = GameSettings.GetKey("ResearchCharaTier", KeyCode.G);
            _keyResearchSkirmisherTier = GameSettings.GetKey("ResearchSkirmisherTier", KeyCode.V);
            _keyResearchBatteringRamTier = GameSettings.GetKey("ResearchBatteringRamTier", KeyCode.R);
            _keyResearchCavalryArcherTier = GameSettings.GetKey("ResearchCavalryArcherTier", KeyCode.P);
            _keyResearchCamelRiderTier = GameSettings.GetKey("ResearchCamelRiderTier", KeyCode.B);
            _keyResearchScorpionTier = GameSettings.GetKey("ResearchScorpionTier", KeyCode.X);
            _keyTrainFishingBoat = GameSettings.GetKey("TrainDockUnit", KeyCode.B);
            _keyTrainWarGalley = GameSettings.GetKey("TrainWarGalley", KeyCode.W);
            _keyResearchNavalTier = GameSettings.GetKey("ResearchNavalTier", KeyCode.X);
            // Wave 4 item 25: Y/Z aren't used anywhere in the Dock context
            // yet (Y = AdvanceAge on TownCenter, Z = TrainUniqueUnit2 on
            // Durg - both mutually exclusive with a selected Dock) -
            // reused freely per this file's own established convention
            // (see HandleHotkeys' header comment).
            _keyTrainFireShip = GameSettings.GetKey("TrainFireShip", KeyCode.Y);
            _keyResearchFireShipTier = GameSettings.GetKey("ResearchFireShipTier", KeyCode.Z);
            // Wave 4 item 26: T isn't used anywhere in the Dock context yet
            // (T = TrainUnit on Barracks, mutually exclusive with a
            // selected Dock) - reused freely per this file's own
            // established convention (see HandleHotkeys' header comment).
            _keyTrainTradeShip = GameSettings.GetKey("TrainTradeShip", KeyCode.T);
            // Wave 4 item 26: V isn't used anywhere in the Market context
            // yet (no Market hotkeys existed before this item; V =
            // ResearchSkirmisherTier on Barracks, mutually exclusive with a
            // selected Market) - reused freely per the same convention.
            _keyTrainVanik = GameSettings.GetKey("TrainVanik", KeyCode.V);
            // Wave 4 item 27: H/C are fresh in the brand-new Monastery-
            // selected context - nothing claimed there before this item.
            _keyTrainVaidya = GameSettings.GetKey("TrainVaidya", KeyCode.H);
            _keyTrainPurohita = GameSettings.GetKey("TrainPurohita", KeyCode.C);
            _keyUngarrison = GameSettings.GetKey("Ungarrison", KeyCode.U);
        }

        // Command-card buttons get their own dedicated 4-state sprite set
        // (Icons/CommandCardButton/*) rather than the shared UIStyleTheme
        // .ApplyButton - that one is for generic modal buttons
        // (Settings/Diplomacy/MissionSelect), a visually distinct family
        // from these. Listed explicitly rather than reflected over the
        // fields so a future button doesn't silently opt out just because
        // it wasn't added here.
        private void ApplyTheme()
        {
            Sprite normal = Resources.Load<Sprite>("UI/Icons/CommandCardButton/normal");
            Sprite hover = Resources.Load<Sprite>("UI/Icons/CommandCardButton/hover");
            Sprite pressed = Resources.Load<Sprite>("UI/Icons/CommandCardButton/pressed");
            Sprite disabled = Resources.Load<Sprite>("UI/Icons/CommandCardButton/disabled");

            Button[] buttons =
            {
                barracksButton, farmButton, houseButton, wallButton, gateButton, towerButton,
                marketButton, dockButton, lumberCampButton, miningCampButton, millButton, durgButton,
                karmashalaButton, monasteryButton,
                workerButton, soldierButton, archerButton, cavalryButton,
                siegeButton, spearmanButton, charaButton, skirmisherButton, batteringRamButton, trebuchetButton, cavalryArcherButton, camelRiderButton, scorpionButton, uniqueUnitButton, uniqueUnitButton2, heroButton, ungarrisonButton,
                fishingBoatButton, warGalleyButton, sellWoodButton, buyWoodButton, sellFoodButton,
                buyFoodButton, sellStoneButton, buyStoneButton, vanikButton, vaidyaButton, purohitaButton, attackUpgradeButton, armorUpgradeButton,
                uniqueTechButton, infantryTierButton, spearmanTierButton, archerTierButton, cavalryTierButton, siegeTierButton, elephantTierButton, eliteTierButton, eliteTierButton2, charaTierButton, skirmisherTierButton, batteringRamTierButton, cavalryArcherTierButton, camelRiderTierButton, scorpionTierButton, navalTierButton, fireShipButton, fireShipTierButton, tradeShipButton, ageButton, improvedToolsButton, packMulesButton, tradeDiscountsButton,
            };

            foreach (Button button in buttons)
            {
                if (normal == null)
                {
                    UIStyleTheme.Current.ApplyButton(button.image);
                    continue;
                }

                button.image.sprite = normal;
                // 2026-09-12 ornate HUD reskin: reset to full white - these
                // buttons carry a leftover dark placeholder tint
                // (~0.25,0.25,0.25) authored before this art existed, which
                // otherwise muddies the new sprite's own carved gold/brown
                // coloring instead of letting it render as-authored.
                button.image.color = Color.white;
                // 2026-09-12 ornate HUD reskin: these grid-cell buttons are
                // always exactly square (GridCellSize=48, item 31's icon
                // grid) and the source art is already square (128x128) -
                // Simple is a clean uniform downscale that keeps every
                // carved/ornate detail crisp. Sliced fights a 9-slice
                // border sized for 128px against a 48px target and either
                // overlaps (thick border) or washes out the detail (thin
                // border) - there's no varying aspect ratio here that would
                // actually need slicing.
                button.image.type = Image.Type.Simple;
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = hover,
                    pressedSprite = pressed,
                    disabledSprite = disabled,
                };
            }

            // Roadmap item 31: every button becomes an icon-only grid cell.
            // 25 of the 60 have a real icon asset; the other 35 (every
            // tier-upgrade/research button, most Wave 4 units, and every
            // building with no bespoke art yet) get the shared placeholder
            // - flagged directly, per this project's own "flag asset needs"
            // convention, as needing real per-unit/per-tech icon art later.
            SetupGridCell(barracksButton, "build_barracks");
            SetupGridCell(farmButton, "build_farm");
            SetupGridCell(houseButton, "build_house");
            SetupGridCell(wallButton, "build_wall");
            SetupGridCell(gateButton, "build_gate");
            SetupGridCell(towerButton, "build_tower");
            SetupGridCell(marketButton, "build_market");
            SetupGridCell(dockButton, "build_dock");
            SetupGridCell(lumberCampButton, "build_lumbercamp");
            SetupGridCell(miningCampButton, "build_miningcamp");
            SetupGridCell(millButton, "build_mill");
            SetupGridCell(durgButton, "build_durg");
            SetupGridCell(karmashalaButton, "build_karmashala");
            SetupGridCell(monasteryButton, null);
            SetupGridCell(workerButton, "train_worker");
            SetupGridCell(soldierButton, "train_soldier");
            SetupGridCell(archerButton, "train_archer");
            SetupGridCell(cavalryButton, "train_cavalry");
            SetupGridCell(siegeButton, "train_siege");
            SetupGridCell(spearmanButton, "train_spearman");
            SetupGridCell(charaButton, "train_chara");
            SetupGridCell(skirmisherButton, "train_skirmisher");
            SetupGridCell(batteringRamButton, "train_batteringram");
            SetupGridCell(cavalryArcherButton, "train_cavalryarcher");
            SetupGridCell(camelRiderButton, "train_camelrider");
            SetupGridCell(scorpionButton, "train_scorpion");
            SetupGridCell(trebuchetButton, "train_trebuchet");
            SetupGridCell(uniqueUnitButton, "train_unique_1");
            SetupGridCell(uniqueUnitButton2, "train_unique_2");
            SetupGridCell(heroButton, "train_hero");
            SetupGridCell(ungarrisonButton, null);
            SetupGridCell(fishingBoatButton, "train_fishingboat");
            SetupGridCell(warGalleyButton, "train_wargalley");
            SetupGridCell(navalTierButton, "tier_naval_1");
            SetupGridCell(fireShipButton, "train_fireship");
            SetupGridCell(fireShipTierButton, "train_fireship");
            SetupGridCell(tradeShipButton, null);
            SetupGridCell(sellWoodButton, "resource_wood");
            SetupGridCell(buyWoodButton, "resource_wood");
            SetupGridCell(sellFoodButton, "resource_food");
            SetupGridCell(buyFoodButton, "resource_food");
            SetupGridCell(sellStoneButton, "resource_stone");
            SetupGridCell(buyStoneButton, "resource_stone");
            SetupGridCell(vanikButton, "train_vanik");
            SetupGridCell(vaidyaButton, "train_vaidya");
            SetupGridCell(purohitaButton, "train_purohita");
            SetupGridCell(attackUpgradeButton, "upgrade_attack");
            SetupGridCell(armorUpgradeButton, "upgrade_armor");
            SetupGridCell(uniqueTechButton, "uniquetech_maurya_base");
            SetupGridCell(infantryTierButton, "tier_infantry_1");
            SetupGridCell(spearmanTierButton, "tier_spearman_1");
            SetupGridCell(archerTierButton, "tier_archer_1");
            SetupGridCell(cavalryTierButton, "tier_cavalry_1");
            SetupGridCell(siegeTierButton, "tier_siege_1");
            SetupGridCell(elephantTierButton, "tier_elephant_1");
            SetupGridCell(eliteTierButton, "badge_elite");
            SetupGridCell(eliteTierButton2, "badge_elite");
            SetupGridCell(charaTierButton, "train_chara");
            SetupGridCell(skirmisherTierButton, "train_skirmisher");
            SetupGridCell(batteringRamTierButton, "train_batteringram");
            SetupGridCell(cavalryArcherTierButton, "train_cavalryarcher");
            SetupGridCell(camelRiderTierButton, "train_camelrider");
            SetupGridCell(scorpionTierButton, "train_scorpion");
            SetupGridCell(ageButton, "advance_age");
            SetupGridCell(improvedToolsButton, null);
            SetupGridCell(packMulesButton, null);
            SetupGridCell(tradeDiscountsButton, null);
        }

        // Roadmap item 31: replaces the old AddCommandIcon (icon-left/
        // text-right row layout) with an icon-only square grid cell. The
        // button's existing label (found via GetComponentInChildren, same
        // lookup AddCommandIcon used - covers both a dedicated [SerializeField]
        // label and a plain inline child) is disabled so it no longer
        // renders, but every one of the ~40 existing Update* methods keeps
        // writing to it exactly as before - LayoutCommandGrid never touches
        // that text, only whether/where the button itself is drawn, and
        // TooltipTrigger reads that same live text on hover. This is why
        // none of those ~40 methods needed to change for this redesign.
        private static void SetupGridCell(Button button, string iconKey)
        {
            Sprite icon = iconKey != null ? Resources.Load<Sprite>("UI/Icons/" + iconKey) : null;
            if (icon == null)
            {
                icon = PlaceholderIcon();
            }

            GameObject iconGo = new GameObject("GridIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(GridCellSize - 12f, GridCellSize - 12f);
            iconRect.anchoredPosition = Vector2.zero;
            iconGo.GetComponent<Image>().sprite = icon;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.enabled = false;
            }

            TooltipTrigger trigger = button.gameObject.AddComponent<TooltipTrigger>();
            trigger.Source = label;
        }

        // Wave 5 icon-art pass: uniqueUnitButton/uniqueUnitButton2/
        // uniqueTechButton are the only 3 grid cells whose correct icon
        // depends on which civ is selected, not a fixed unit/tier - every
        // other SetupGridCell call above is a one-time Awake() assignment,
        // but these need their sprite swapped at Update() time alongside
        // their label text. Reuses the "GridIcon" child SetupGridCell
        // already creates rather than adding a second Image reference.
        private static readonly Dictionary<string, string> UniqueUnitIconKeys = new Dictionary<string, string>
        {
            { "chola_naval_raider", "train_unique_chola" },
            { "vijayanagara_war_elephant", "train_unique_vijayanagara_elephant" },
            { "rajput_royal_guard", "train_unique_rajput" },
            { "maurya_war_elephant", "train_unique_maurya_elephant" },
            { "pillar_edict_scholar", "train_unique_maurya_scholar" },
            { "maratha_mavla_raider", "train_unique_maratha_mavla" },
            { "maratha_durg_garrison", "train_unique_maratha_durggarrison" },
        };

        private static readonly Dictionary<CivilizationId, string> UniqueTechIconKeys = new Dictionary<CivilizationId, string>
        {
            { CivilizationId.Chola, "uniquetech_chola_base" },
            { CivilizationId.Vijayanagara, "uniquetech_vijayanagara_base" },
            { CivilizationId.Rajput, "uniquetech_rajput_base" },
            { CivilizationId.Maurya, "uniquetech_maurya_base" },
            { CivilizationId.Maratha, "uniquetech_maratha_base" },
        };

        private static void SetGridIcon(Button button, string iconKey)
        {
            Transform iconTransform = button.transform.Find("GridIcon");
            if (iconTransform == null)
            {
                return;
            }

            Sprite icon = iconKey != null ? Resources.Load<Sprite>("UI/Icons/" + iconKey) : null;
            if (icon == null)
            {
                icon = PlaceholderIcon();
            }

            iconTransform.GetComponent<Image>().sprite = icon;
        }

        // A single flat generated square, cached after first build - not
        // sourced art, the same "generic procedural shape" fallback
        // convention this project already uses for buildings with no 3D
        // model yet (BuildingModelFactory), applied here to 2D icons. 35 of
        // the 60 grid cells use this today; real per-unit/per-tech icon art
        // is still needed eventually (flagged directly, not glossed over).
        private static Sprite PlaceholderIcon()
        {
            if (_placeholderIcon != null)
            {
                return _placeholderIcon;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color fill = new Color(0.55f, 0.45f, 0.25f, 1f);
            Color border = new Color(0.85f, 0.72f, 0.4f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onBorder = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    texture.SetPixel(x, y, onBorder ? border : fill);
                }
            }

            texture.Apply();
            _placeholderIcon = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _placeholderIcon;
        }

        private void Update()
        {
            Building selected = _selectionManager != null ? _selectionManager.SelectedBuilding : null;
            bool ownsSelected = selected != null
                && selected.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == NetworkMatch.LocalFaction;

            bool showPlacement = selected == null
                && _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected();
            TownCenter townCenter = ownsSelected ? selected as TownCenter : null;
            Barracks barracks = ownsSelected ? selected as Barracks : null;
            Dock dock = ownsSelected ? selected as Dock : null;
            Market market = ownsSelected ? selected as Market : null;
            // Wave 2 item 7: unique-unit training lives on Durg, not
            // Barracks, from this session on.
            Durg durg = ownsSelected ? selected as Durg : null;
            // Wave 2 item 8: flat Attack/Armor research lives on Karmashala,
            // not Barracks, from this session on.
            Karmashala karmashala = ownsSelected ? selected as Karmashala : null;
            // Wave 4 item 27: Vaidya/Purohita training lives on the new
            // Monastery building.
            Monastery monastery = ownsSelected ? selected as Monastery : null;
            // General garrisoning system (2026-09-01): every building with
            // a GarrisonPoint (TownCenter/Tower/Wall) shows the Ungarrison
            // button when occupied - no longer restricted to Wall/Tower's
            // type, since TownCenter now has one too.
            GarrisonPoint garrisonPoint = ownsSelected ? selected.GetComponent<GarrisonPoint>() : null;

            HandleHotkeys(townCenter, barracks, dock, durg, karmashala, garrisonPoint, market, monastery);

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
            charaButton.gameObject.SetActive(barracks != null);
            skirmisherButton.gameObject.SetActive(barracks != null);
            batteringRamButton.gameObject.SetActive(barracks != null);
            // Found while wiring scorpionButton (Wave 4 item 23): these 2
            // were never added here by their own sessions, so a
            // Cavalry Archer/Camel Rider button (and its own tier button)
            // could stay visible even with no Barracks selected at all -
            // real pre-existing gap, fixed alongside this item's own
            // wiring rather than left in place.
            cavalryArcherButton.gameObject.SetActive(barracks != null);
            camelRiderButton.gameObject.SetActive(barracks != null);
            cavalryArcherTierButton.gameObject.SetActive(barracks != null);
            camelRiderTierButton.gameObject.SetActive(barracks != null);
            scorpionButton.gameObject.SetActive(barracks != null);
            scorpionTierButton.gameObject.SetActive(barracks != null);
            trebuchetButton.gameObject.SetActive(barracks != null);
            uniqueUnitButton.gameObject.SetActive(durg != null);
            uniqueUnitButton2.gameObject.SetActive(durg != null && durg.UniqueUnitCount > 1);
            heroButton.gameObject.SetActive(durg != null);
            elephantTierButton.gameObject.SetActive(durg != null && durg.TrainsElephant);
            eliteTierButton.gameObject.SetActive(durg != null && durg.TrainsEliteEligible(0));
            eliteTierButton2.gameObject.SetActive(durg != null && durg.TrainsEliteEligible(1));
            ungarrisonButton.gameObject.SetActive(garrisonPoint != null && garrisonPoint.Count > 0);
            attackUpgradeButton.gameObject.SetActive(karmashala != null);
            armorUpgradeButton.gameObject.SetActive(karmashala != null);
            uniqueTechButton.gameObject.SetActive(barracks != null);
            infantryTierButton.gameObject.SetActive(barracks != null);
            spearmanTierButton.gameObject.SetActive(barracks != null);
            archerTierButton.gameObject.SetActive(barracks != null);
            cavalryTierButton.gameObject.SetActive(barracks != null);
            siegeTierButton.gameObject.SetActive(barracks != null);
            charaTierButton.gameObject.SetActive(barracks != null);
            skirmisherTierButton.gameObject.SetActive(barracks != null);
            batteringRamTierButton.gameObject.SetActive(barracks != null);
            fishingBoatButton.gameObject.SetActive(dock != null);
            warGalleyButton.gameObject.SetActive(dock != null);
            navalTierButton.gameObject.SetActive(dock != null);
            fireShipButton.gameObject.SetActive(dock != null);
            fireShipTierButton.gameObject.SetActive(dock != null);
            tradeShipButton.gameObject.SetActive(dock != null);
            sellWoodButton.gameObject.SetActive(market != null);
            buyWoodButton.gameObject.SetActive(market != null);
            sellFoodButton.gameObject.SetActive(market != null);
            buyFoodButton.gameObject.SetActive(market != null);
            sellStoneButton.gameObject.SetActive(market != null);
            buyStoneButton.gameObject.SetActive(market != null);
            vanikButton.gameObject.SetActive(market != null);
            vaidyaButton.gameObject.SetActive(monastery != null);
            purohitaButton.gameObject.SetActive(monastery != null);

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
                lumberCampButton.interactable = true;
                miningCampButton.interactable = true;
                millButton.interactable = true;
                durgButton.interactable = BuildingPlacer.CanPlaceDurg;
                durgLabel.text = BuildingPlacer.CanPlaceDurg
                    ? "Build Durg (200 Wood, 150 Stone)"
                    : "Build Durg (Requires Durg Age)";
                karmashalaButton.interactable = BuildingPlacer.CanPlaceKarmashala;
                karmashalaLabel.text = BuildingPlacer.CanPlaceKarmashala
                    ? "Build Karmashala (150 Wood)"
                    : "Build Karmashala (Requires Classical Age)";
                monasteryButton.interactable = BuildingPlacer.CanPlaceMonastery;
                monasteryLabel.text = BuildingPlacer.CanPlaceMonastery
                    ? "Build Monastery (175 Wood, 100 Stone)"
                    : "Build Monastery (Requires Durg Age)";
            }

            if (townCenter != null)
            {
                UpdateTownCenterButtons(townCenter);
            }

            if (barracks != null)
            {
                UpdateBarracksButtons(barracks);
            }

            if (durg != null)
            {
                UpdateDurgButtons(durg);
            }

            if (karmashala != null)
            {
                UpdateKarmashalaButtons(karmashala);
            }

            if (dock != null)
            {
                UpdateDockButtons(dock);
            }

            if (market != null)
            {
                UpdateMarketButtons(market);
            }

            if (monastery != null)
            {
                UpdateMonasteryButtons(monastery);
            }

            // Roadmap item 31: runs last, after every context branch above
            // has finished deciding each button's final activeSelf/
            // interactable/label state for this frame - narrows that set
            // down to a positioned, paged icon grid without any of the
            // logic above needing to know the grid exists.
            LayoutCommandGrid(selected, showPlacement);
        }

        // Roadmap item 31: positions the current page's active buttons into
        // a fixed-size icon grid and hides the rest for this frame only
        // (their own activeSelf will be reasserted by the context logic
        // above next frame regardless - this never permanently overrides
        // it). Resets to page 0 whenever the selection/context changes so a
        // stale page index from a previous context never persists.
        private void LayoutCommandGrid(Building selected, bool showPlacement)
        {
            object contextKey = (object)selected ?? (showPlacement ? "placement" : null);
            if (!Equals(contextKey, _lastGridContextKey))
            {
                _gridPage = 0;
                _lastGridContextKey = contextKey;
            }

            var activeFlags = new bool[_allGridButtons.Length];
            for (int i = 0; i < _allGridButtons.Length; i++)
            {
                activeFlags[i] = _allGridButtons[i].gameObject.activeSelf;
            }

            (List<int> visible, int pageCount, int clampedPage) = ComputeGridPage(activeFlags, GridCapacity, _gridPage);
            _gridPage = clampedPage;

            for (int slot = 0; slot < visible.Count; slot++)
            {
                Button button = _allGridButtons[visible[slot]];
                int col = slot % GridColumns;
                int row = slot / GridColumns;
                RectTransform rect = button.transform as RectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(GridCellSize, GridCellSize);
                rect.anchoredPosition = new Vector2(
                    GridMargin + col * (GridCellSize + GridGap),
                    -GridMargin - row * (GridCellSize + GridGap));
            }

            var visibleSet = new HashSet<int>(visible);
            for (int i = 0; i < _allGridButtons.Length; i++)
            {
                if (activeFlags[i] && !visibleSet.Contains(i))
                {
                    _allGridButtons[i].gameObject.SetActive(false);
                }
            }

            bool needsPaging = pageCount > 1;
            if (gridPrevButton != null)
            {
                gridPrevButton.gameObject.SetActive(needsPaging);
                gridPrevButton.interactable = _gridPage > 0;
            }

            if (gridNextButton != null)
            {
                gridNextButton.gameObject.SetActive(needsPaging);
                gridNextButton.interactable = _gridPage < pageCount - 1;
            }

            if (gridPageLabel != null)
            {
                gridPageLabel.gameObject.SetActive(needsPaging);
                if (needsPaging)
                {
                    gridPageLabel.text = $"Page {_gridPage + 1}/{pageCount}";
                }
            }
        }

        // Pure pagination math, independent of any live button/scene state -
        // unit-tested directly (CommandGridLayoutTests.cs). activeFlags is
        // indexed in the same fixed order as _allGridButtons.
        internal static (List<int> visible, int pageCount, int clampedPage) ComputeGridPage(
            IReadOnlyList<bool> activeFlags, int capacity, int requestedPage)
        {
            var activeIndices = new List<int>();
            for (int i = 0; i < activeFlags.Count; i++)
            {
                if (activeFlags[i])
                {
                    activeIndices.Add(i);
                }
            }

            int pageCount = activeIndices.Count == 0 ? 1 : (activeIndices.Count + capacity - 1) / capacity;
            int clampedPage = Mathf.Clamp(requestedPage, 0, pageCount - 1);

            var visible = new List<int>();
            int start = clampedPage * capacity;
            int end = Mathf.Min(start + capacity, activeIndices.Count);
            for (int i = start; i < end; i++)
            {
                visible.Add(activeIndices[i]);
            }

            return (visible, pageCount, clampedPage);
        }

        // Item 1 (Hotkeys): each check calls the exact same handler its
        // matching button's onClick uses, so a hotkey press when the action
        // is unavailable is a harmless no-op (RequestTrain*/RequestResearch*
        // already self-guard) - identical behavior to the button being
        // disabled. Gated per-parameter (not a single "selected something"
        // check) so a key only ever acts on the currently selected building
        // of the matching type, fixing the old per-building Update() bug.
        private void HandleHotkeys(TownCenter townCenter, Barracks barracks, Dock dock, Durg durg, Karmashala karmashala, GarrisonPoint garrisonPoint, Market market, Monastery monastery)
        {
            if (townCenter != null)
            {
                if (Input.GetKeyDown(_keyTrainWorker)) TrainWorkerAtSelected();
                if (Input.GetKeyDown(_keyAdvanceAge)) RequestAgeUpAtSelected();
                if (Input.GetKeyDown(_keyResearchImprovedTools)) ResearchEconomyTechAtSelected(EconomyTech.ImprovedTools);
                if (Input.GetKeyDown(_keyResearchPackMules)) ResearchEconomyTechAtSelected(EconomyTech.PackMules);
                if (Input.GetKeyDown(_keyResearchTradeDiscounts)) ResearchEconomyTechAtSelected(EconomyTech.TradeDiscounts);
            }

            if (barracks != null)
            {
                if (Input.GetKeyDown(_keyTrainSoldier)) TrainSoldierAtSelected();
                if (Input.GetKeyDown(_keyTrainArcher)) TrainArcherAtSelected();
                if (Input.GetKeyDown(_keyTrainCavalry)) TrainCavalryAtSelected();
                if (Input.GetKeyDown(_keyTrainSiege)) TrainSiegeAtSelected();
                if (Input.GetKeyDown(_keyTrainSpearman)) TrainSpearmanAtSelected();
                if (Input.GetKeyDown(_keyTrainChara)) TrainCharaAtSelected();
                if (Input.GetKeyDown(_keyResearchCharaTier)) ResearchCharaTierAtSelected();
                if (Input.GetKeyDown(_keyTrainSkirmisher)) TrainSkirmisherAtSelected();
                if (Input.GetKeyDown(_keyResearchSkirmisherTier)) ResearchSkirmisherTierAtSelected();
                if (Input.GetKeyDown(_keyTrainBatteringRam)) TrainBatteringRamAtSelected();
                if (Input.GetKeyDown(_keyResearchBatteringRamTier)) ResearchBatteringRamTierAtSelected();
                if (Input.GetKeyDown(_keyTrainCavalryArcher)) TrainCavalryArcherAtSelected();
                if (Input.GetKeyDown(_keyResearchCavalryArcherTier)) ResearchCavalryArcherTierAtSelected();
                if (Input.GetKeyDown(_keyTrainCamelRider)) TrainCamelRiderAtSelected();
                if (Input.GetKeyDown(_keyResearchCamelRiderTier)) ResearchCamelRiderTierAtSelected();
                if (Input.GetKeyDown(_keyTrainScorpion)) TrainScorpionAtSelected();
                if (Input.GetKeyDown(_keyTrainTrebuchet)) TrainTrebuchetAtSelected();
                if (Input.GetKeyDown(_keyResearchScorpionTier)) ResearchScorpionTierAtSelected();
                if (Input.GetKeyDown(_keyResearchUniqueTech)) ResearchUniqueTechAtSelected();
                if (Input.GetKeyDown(_keyResearchInfantryTier)) ResearchInfantryTierAtSelected();
                if (Input.GetKeyDown(_keyResearchSpearmanTier)) ResearchSpearmanTierAtSelected();
                if (Input.GetKeyDown(_keyResearchArcherTier)) ResearchArcherTierAtSelected();
                if (Input.GetKeyDown(_keyResearchCavalryTier)) ResearchCavalryTierAtSelected();
                if (Input.GetKeyDown(_keyResearchSiegeTier)) ResearchSiegeTierAtSelected();
            }

            // Wave 2 item 7: unique-unit training hotkeys now act on a
            // selected Durg instead of a selected Barracks.
            if (durg != null)
            {
                if (Input.GetKeyDown(_keyTrainUniqueUnit)) TrainUniqueUnitAtSelected();
                if (durg.UniqueUnitCount > 1 && Input.GetKeyDown(_keyTrainUniqueUnit2)) TrainUniqueUnit2AtSelected();
                if (Input.GetKeyDown(_keyTrainHero)) TrainHeroAtSelected();
                if (durg.TrainsElephant && Input.GetKeyDown(_keyResearchElephantTier)) ResearchElephantTierAtSelected();
                if (durg.TrainsEliteEligible(0) && Input.GetKeyDown(_keyResearchEliteTier)) ResearchEliteTierAtSelected();
                if (durg.TrainsEliteEligible(1) && Input.GetKeyDown(_keyResearchEliteTier2)) ResearchEliteTier2AtSelected();
            }

            // Wave 2 item 8: flat Attack/Armor research hotkeys now act on a
            // selected Karmashala instead of a selected Barracks.
            if (karmashala != null)
            {
                if (Input.GetKeyDown(_keyResearchAttack)) ResearchAttackAtSelected();
                if (Input.GetKeyDown(_keyResearchArmor)) ResearchArmorAtSelected();
            }

            if (dock != null)
            {
                if (Input.GetKeyDown(_keyTrainFishingBoat)) TrainFishingBoatAtSelected();
                if (Input.GetKeyDown(_keyTrainWarGalley)) TrainWarGalleyAtSelected();
                if (Input.GetKeyDown(_keyResearchNavalTier)) ResearchNavalTierAtSelected();
                if (Input.GetKeyDown(_keyTrainFireShip)) TrainFireShipAtSelected();
                if (Input.GetKeyDown(_keyResearchFireShipTier)) ResearchFireShipTierAtSelected();
                if (Input.GetKeyDown(_keyTrainTradeShip)) TrainTradeShipAtSelected();
            }

            if (market != null)
            {
                if (Input.GetKeyDown(_keyTrainVanik)) TrainVanikAtSelected();
            }

            if (monastery != null)
            {
                if (Input.GetKeyDown(_keyTrainVaidya)) TrainVaidyaAtSelected();
                if (Input.GetKeyDown(_keyTrainPurohita)) TrainPurohitaAtSelected();
            }

            if (garrisonPoint != null && garrisonPoint.Count > 0 && Input.GetKeyDown(_keyUngarrison))
            {
                UngarrisonAtSelected();
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
            charaButton.interactable = canTrain;
            skirmisherButton.interactable = canTrain;
            batteringRamButton.interactable = canTrain;
            cavalryArcherButton.interactable = canTrain;
            camelRiderButton.interactable = canTrain;
            scorpionButton.interactable = canTrain;
            UpdateTrebuchetButton(barracks, canTrain);

            UpdateUniqueTechButton(barracks);
            UpdateInfantryTierButton(barracks);
            UpdateSpearmanTierButton(barracks);
            UpdateArcherTierButton(barracks);
            UpdateCavalryTierButton(barracks);
            UpdateSiegeTierButton(barracks);
            UpdateCharaTierButton(barracks);
            UpdateSkirmisherTierButton(barracks);
            UpdateBatteringRamTierButton(barracks);
            UpdateCavalryArcherTierButton(barracks);
            UpdateCamelRiderTierButton(barracks);
            UpdateScorpionTierButton(barracks);
        }

        // Wave 3 item 9: Infantry tier ladder research button state - same
        // "Researching.../(Max)/Upgrade to X" shape as UpdateUpgradeButton,
        // plus an age-gate branch (mirrors the Age-up button's own
        // "(needs 2 buildings)" pattern) since the next tier can be
        // unresearchable for a reason other than cost.
        private void UpdateInfantryTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingInfantryTier)
            {
                infantryTierButton.interactable = false;
                infantryTierLabel.text = $"Researching Infantry... {(int)(barracks.InfantryTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!InfantryLineProgress.HasNextTier(faction))
            {
                infantryTierButton.interactable = false;
                infantryTierLabel.text = "Infantry (Max Tier)";
                return;
            }

            InfantryTierData next = InfantryLineProgress.NextTierData(faction);
            if (!InfantryLineProgress.NextTierAgeRequirementMet(faction))
            {
                infantryTierButton.interactable = false;
                infantryTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            infantryTierButton.interactable = barracks.IsComplete;
            infantryTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 3 item 10: Spearman tier ladder research button state -
        // identical shape to UpdateInfantryTierButton.
        private void UpdateSpearmanTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingSpearmanTier)
            {
                spearmanTierButton.interactable = false;
                spearmanTierLabel.text = $"Researching Spearman... {(int)(barracks.SpearmanTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!SpearmanLineProgress.HasNextTier(faction))
            {
                spearmanTierButton.interactable = false;
                spearmanTierLabel.text = "Spearman (Max Tier)";
                return;
            }

            SpearmanTierData next = SpearmanLineProgress.NextTierData(faction);
            if (!SpearmanLineProgress.NextTierAgeRequirementMet(faction))
            {
                spearmanTierButton.interactable = false;
                spearmanTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            spearmanTierButton.interactable = barracks.IsComplete;
            spearmanTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 3 item 11: Archer tier ladder research button state -
        // identical shape to UpdateInfantryTierButton/UpdateSpearmanTierButton.
        private void UpdateArcherTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingArcherTier)
            {
                archerTierButton.interactable = false;
                archerTierLabel.text = $"Researching Archer... {(int)(barracks.ArcherTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!ArcherLineProgress.HasNextTier(faction))
            {
                archerTierButton.interactable = false;
                archerTierLabel.text = "Archer (Max Tier)";
                return;
            }

            ArcherTierData next = ArcherLineProgress.NextTierData(faction);
            if (!ArcherLineProgress.NextTierAgeRequirementMet(faction))
            {
                archerTierButton.interactable = false;
                archerTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            archerTierButton.interactable = barracks.IsComplete;
            archerTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 3 item 12: Cavalry tier ladder research button state -
        // identical shape to UpdateInfantryTierButton/UpdateSpearmanTierButton/
        // UpdateArcherTierButton.
        private void UpdateCavalryTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingCavalryTier)
            {
                cavalryTierButton.interactable = false;
                cavalryTierLabel.text = $"Researching Cavalry... {(int)(barracks.CavalryTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!CavalryLineProgress.HasNextTier(faction))
            {
                cavalryTierButton.interactable = false;
                cavalryTierLabel.text = "Cavalry (Max Tier)";
                return;
            }

            CavalryTierData next = CavalryLineProgress.NextTierData(faction);
            if (!CavalryLineProgress.NextTierAgeRequirementMet(faction))
            {
                cavalryTierButton.interactable = false;
                cavalryTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            cavalryTierButton.interactable = barracks.IsComplete;
            cavalryTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 3 item 14: Siege tier ladder research button state -
        // identical shape to UpdateInfantryTierButton/UpdateSpearmanTierButton/
        // UpdateArcherTierButton/UpdateCavalryTierButton.
        private void UpdateSiegeTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingSiegeTier)
            {
                siegeTierButton.interactable = false;
                siegeTierLabel.text = $"Researching Siege... {(int)(barracks.SiegeTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!SiegeLineProgress.HasNextTier(faction))
            {
                siegeTierButton.interactable = false;
                siegeTierLabel.text = "Siege (Max Tier)";
                return;
            }

            SiegeTierData next = SiegeLineProgress.NextTierData(faction);
            if (!SiegeLineProgress.NextTierAgeRequirementMet(faction))
            {
                siegeTierButton.interactable = false;
                siegeTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            siegeTierButton.interactable = barracks.IsComplete;
            siegeTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item 18: Scout tier ladder research button state -
        // identical shape to UpdateSiegeTierButton/UpdateCavalryTierButton.
        private void UpdateCharaTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingCharaTier)
            {
                charaTierButton.interactable = false;
                charaTierLabel.text = $"Researching Scout... {(int)(barracks.CharaTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!ScoutLineProgress.HasNextTier(faction))
            {
                charaTierButton.interactable = false;
                charaTierLabel.text = "Scout (Max Tier)";
                return;
            }

            ScoutTierData next = ScoutLineProgress.NextTierData(faction);
            if (!ScoutLineProgress.NextTierAgeRequirementMet(faction))
            {
                charaTierButton.interactable = false;
                charaTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            charaTierButton.interactable = barracks.IsComplete;
            charaTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item 19: Skirmisher tier ladder research button state -
        // identical shape to UpdateCharaTierButton/UpdateSiegeTierButton.
        private void UpdateSkirmisherTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingSkirmisherTier)
            {
                skirmisherTierButton.interactable = false;
                skirmisherTierLabel.text = $"Researching Skirmisher... {(int)(barracks.SkirmisherTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!SkirmisherLineProgress.HasNextTier(faction))
            {
                skirmisherTierButton.interactable = false;
                skirmisherTierLabel.text = "Skirmisher (Max Tier)";
                return;
            }

            SkirmisherTierData next = SkirmisherLineProgress.NextTierData(faction);
            if (!SkirmisherLineProgress.NextTierAgeRequirementMet(faction))
            {
                skirmisherTierButton.interactable = false;
                skirmisherTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            skirmisherTierButton.interactable = barracks.IsComplete;
            skirmisherTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item 20: Battering Ram tier ladder research button state -
        // identical shape to UpdateSkirmisherTierButton/UpdateCharaTierButton.
        private void UpdateBatteringRamTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingBatteringRamTier)
            {
                batteringRamTierButton.interactable = false;
                batteringRamTierLabel.text = $"Researching Battering Ram... {(int)(barracks.BatteringRamTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!BatteringRamLineProgress.HasNextTier(faction))
            {
                batteringRamTierButton.interactable = false;
                batteringRamTierLabel.text = "Battering Ram (Max Tier)";
                return;
            }

            BatteringRamTierData next = BatteringRamLineProgress.NextTierData(faction);
            if (!BatteringRamLineProgress.NextTierAgeRequirementMet(faction))
            {
                batteringRamTierButton.interactable = false;
                batteringRamTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            batteringRamTierButton.interactable = barracks.IsComplete;
            batteringRamTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item: Cavalry Archer tier ladder research button state -
        // identical shape to UpdateBatteringRamTierButton/UpdateSkirmisherTierButton.
        private void UpdateCavalryArcherTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingCavalryArcherTier)
            {
                cavalryArcherTierButton.interactable = false;
                cavalryArcherTierLabel.text = $"Researching Cavalry Archer... {(int)(barracks.CavalryArcherTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!CavalryArcherLineProgress.HasNextTier(faction))
            {
                cavalryArcherTierButton.interactable = false;
                cavalryArcherTierLabel.text = "Cavalry Archer (Max Tier)";
                return;
            }

            CavalryArcherTierData next = CavalryArcherLineProgress.NextTierData(faction);
            if (!CavalryArcherLineProgress.NextTierAgeRequirementMet(faction))
            {
                cavalryArcherTierButton.interactable = false;
                cavalryArcherTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            cavalryArcherTierButton.interactable = barracks.IsComplete;
            cavalryArcherTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item 22: Camel Rider tier ladder research button state -
        // identical shape to UpdateCavalryArcherTierButton/UpdateBatteringRamTierButton.
        private void UpdateCamelRiderTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingCamelRiderTier)
            {
                camelRiderTierButton.interactable = false;
                camelRiderTierLabel.text = $"Researching Camel Rider... {(int)(barracks.CamelRiderTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!CamelRiderLineProgress.HasNextTier(faction))
            {
                camelRiderTierButton.interactable = false;
                camelRiderTierLabel.text = "Camel Rider (Max Tier)";
                return;
            }

            CamelRiderTierData next = CamelRiderLineProgress.NextTierData(faction);
            if (!CamelRiderLineProgress.NextTierAgeRequirementMet(faction))
            {
                camelRiderTierButton.interactable = false;
                camelRiderTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            camelRiderTierButton.interactable = barracks.IsComplete;
            camelRiderTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item 23: Scorpion tier ladder research button state -
        // identical shape to UpdateCamelRiderTierButton/UpdateCavalryArcherTierButton.
        private void UpdateScorpionTierButton(Barracks barracks)
        {
            if (barracks.IsResearchingScorpionTier)
            {
                scorpionTierButton.interactable = false;
                scorpionTierLabel.text = $"Researching Scorpion... {(int)(barracks.ScorpionTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!ScorpionLineProgress.HasNextTier(faction))
            {
                scorpionTierButton.interactable = false;
                scorpionTierLabel.text = "Scorpion (Max Tier)";
                return;
            }

            ScorpionTierData next = ScorpionLineProgress.NextTierData(faction);
            if (!ScorpionLineProgress.NextTierAgeRequirementMet(faction))
            {
                scorpionTierButton.interactable = false;
                scorpionTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            scorpionTierButton.interactable = barracks.IsComplete;
            scorpionTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 2 item 8: flat Attack/Armor research button state, split out
        // of UpdateBarracksButtons now that it lives on Karmashala instead.
        private void UpdateKarmashalaButtons(Karmashala karmashala)
        {
            FactionId faction = NetworkMatch.LocalFaction;

            UpdateUpgradeButton(
                attackUpgradeButton, attackUpgradeLabel, "Attack",
                karmashala.IsComplete, karmashala.IsResearchingAttack, karmashala.AttackResearchProgress,
                UpgradeProgress.AttackTier(faction), UpgradeProgress.HasNextAttackTier(faction),
                karmashala.NextAttackUpgradeCost,
                UpgradeProgress.NextAttackTierAgeRequirementMet(faction), UpgradeProgress.NextAttackTierRequiredAge(faction));

            UpdateUpgradeButton(
                armorUpgradeButton, armorUpgradeLabel, "Armor",
                karmashala.IsComplete, karmashala.IsResearchingArmor, karmashala.ArmorResearchProgress,
                UpgradeProgress.ArmorTier(faction), UpgradeProgress.HasNextArmorTier(faction),
                karmashala.NextArmorUpgradeCost,
                UpgradeProgress.NextArmorTierAgeRequirementMet(faction), UpgradeProgress.NextArmorTierRequiredAge(faction));
        }

        // Wave 2 item 7: unique-unit training button state, split out of
        // UpdateBarracksButtons now that it lives on Durg instead.
        private void UpdateDurgButtons(Durg durg)
        {
            bool canTrain = durg.IsComplete && !durg.IsTraining;
            UniqueUnitDefinition unique1 = durg.UniqueUnit;
            uniqueUnitButton.interactable = canTrain;
            uniqueUnitLabel.text = $"Train {unique1.Name} ({(int)unique1.FoodCost} Food, {(int)unique1.GoldCost} Gold)";
            SetGridIcon(uniqueUnitButton, UniqueUnitIconKeys.TryGetValue(unique1.UnitId, out string icon1) ? icon1 : null);

            if (durg.UniqueUnitCount > 1)
            {
                UniqueUnitDefinition unique2 = durg.UniqueUnitAt(1);
                uniqueUnitButton2.interactable = canTrain;
                uniqueUnitLabel2.text = $"Train {unique2.Name} ({(int)unique2.FoodCost} Food, {(int)unique2.GoldCost} Gold)";
                SetGridIcon(uniqueUnitButton2, UniqueUnitIconKeys.TryGetValue(unique2.UnitId, out string icon2) ? icon2 : null);
            }

            if (durg.TrainsElephant)
            {
                UpdateElephantTierButton(durg);
            }

            if (durg.TrainsEliteEligible(0))
            {
                UpdateEliteTierButton(durg, 0, eliteTierButton, eliteTierLabel);
            }

            if (durg.TrainsEliteEligible(1))
            {
                UpdateEliteTierButton(durg, 1, eliteTierButton2, eliteTierLabel2);
            }

            UpdateHeroButton(durg);
        }

        // Wave 4 item 28: Maharaja button state - independent of canTrain
        // above (that's the unique-unit queue's own IsTraining, not
        // Durg.IsTrainingHero) since the two tracks run alongside each
        // other without blocking.
        private void UpdateHeroButton(Durg durg)
        {
            if (durg.IsTrainingHero)
            {
                heroButton.interactable = false;
                heroLabel.text = "Training Maharaja...";
                return;
            }

            if (HeroProgress.IsAlive(NetworkMatch.LocalFaction))
            {
                heroButton.interactable = false;
                heroLabel.text = "Maharaja (Already Trained)";
                return;
            }

            UnitDefinition heroDef = DataRegistry.GetUnit("maharaja");
            float heroFoodCost = heroDef != null ? heroDef.cost.food : 220f;
            float heroGoldCost = heroDef != null ? heroDef.cost.gold : 180f;
            heroButton.interactable = durg.IsComplete;
            heroLabel.text = $"Train Maharaja ({(int)heroFoodCost} Food, {(int)heroGoldCost} Gold)";
        }

        // Wave 3 item 13: Elephant tier ladder research button state -
        // identical shape to UpdateCavalryTierButton, except it acts on a
        // selected Durg (where War Elephants train) instead of Barracks.
        // Only ever called when durg.TrainsElephant is true (see
        // UpdateDurgButtons), so no separate "this civ has no elephant"
        // branch is needed here.
        private void UpdateElephantTierButton(Durg durg)
        {
            if (durg.IsResearchingElephantTier)
            {
                elephantTierButton.interactable = false;
                elephantTierLabel.text = $"Researching Elephant... {(int)(durg.ElephantTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!ElephantLineProgress.HasNextTier(faction))
            {
                elephantTierButton.interactable = false;
                elephantTierLabel.text = "Elephant (Max Tier)";
                return;
            }

            ElephantTierData next = ElephantLineProgress.NextTierData(faction);
            if (!ElephantLineProgress.NextTierAgeRequirementMet(faction))
            {
                elephantTierButton.interactable = false;
                elephantTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            elephantTierButton.interactable = durg.IsComplete;
            elephantTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 3 item 16: Elite tier button state for a given unique-unit
        // slot - identical shape to UpdateElephantTierButton, parameterized
        // by slot/button/label since a Durg can have up to 2 independent
        // elite tracks (e.g. Maratha's Mavla Raider and Durg Garrison).
        // Only ever called when durg.TrainsEliteEligible(slot) is true (see
        // UpdateDurgButtons), so no separate "not eligible" branch is
        // needed here.
        private void UpdateEliteTierButton(Durg durg, int slot, Button button, TMP_Text label)
        {
            string unitId = durg.UniqueUnitAt(slot).UnitId;

            if (durg.IsResearchingEliteTier(slot))
            {
                button.interactable = false;
                label.text = $"Researching Elite... {(int)(durg.EliteTierResearchProgress(slot) * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!UniqueUnitEliteProgress.HasNextTier(faction, unitId))
            {
                button.interactable = false;
                label.text = "Elite (Max Tier)";
                return;
            }

            EliteTierData next = UniqueUnitEliteProgress.DataFor(unitId);
            if (!UniqueUnitEliteProgress.NextTierAgeRequirementMet(faction, unitId))
            {
                button.interactable = false;
                label.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            button.interactable = durg.IsComplete;
            label.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        private void UpdateDockButtons(Dock dock)
        {
            bool canTrain = dock.IsComplete && !dock.IsTraining;
            fishingBoatButton.interactable = canTrain;
            warGalleyButton.interactable = canTrain;
            fireShipButton.interactable = canTrain;
            tradeShipButton.interactable = canTrain;

            UpdateNavalTierButton(dock);
            UpdateFireShipTierButton(dock);
        }

        // Wave 3 item 15: Naval tier ladder research button state -
        // identical shape to UpdateInfantryTierButton/UpdateArcherTierButton/
        // UpdateSiegeTierButton, except it acts on a selected Dock (where
        // War Galley trains) instead of Barracks.
        private void UpdateNavalTierButton(Dock dock)
        {
            if (dock.IsResearchingNavalTier)
            {
                navalTierButton.interactable = false;
                navalTierLabel.text = $"Researching Naval... {(int)(dock.NavalTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!NavalLineProgress.HasNextTier(faction))
            {
                navalTierButton.interactable = false;
                navalTierLabel.text = "Naval (Max Tier)";
                return;
            }

            NavalTierData next = NavalLineProgress.NextTierData(faction);
            if (!NavalLineProgress.NextTierAgeRequirementMet(faction))
            {
                navalTierButton.interactable = false;
                navalTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            navalTierButton.interactable = dock.IsComplete;
            navalTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
        }

        // Wave 4 item 25: Fire Ship tier ladder research button state -
        // identical shape to UpdateNavalTierButton, an independent track
        // on the same selected Dock.
        private void UpdateFireShipTierButton(Dock dock)
        {
            if (dock.IsResearchingFireShipTier)
            {
                fireShipTierButton.interactable = false;
                fireShipTierLabel.text = $"Researching Fire Ship... {(int)(dock.FireShipTierResearchProgress * 100f)}%";
                return;
            }

            FactionId faction = NetworkMatch.LocalFaction;
            if (!FireShipLineProgress.HasNextTier(faction))
            {
                fireShipTierButton.interactable = false;
                fireShipTierLabel.text = "Fire Ship (Max Tier)";
                return;
            }

            FireShipTierData next = FireShipLineProgress.NextTierData(faction);
            if (!FireShipLineProgress.NextTierAgeRequirementMet(faction))
            {
                fireShipTierButton.interactable = false;
                fireShipTierLabel.text = $"Upgrade to {next.Name} (needs {next.RequiredAge} Age)";
                return;
            }

            fireShipTierButton.interactable = dock.IsComplete;
            fireShipTierLabel.text = $"Upgrade to {next.Name} ({(int)next.GoldCost} Gold, {(int)next.WoodCost} Wood)";
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

            UpdateVanikButton(market);
        }

        // Wave 4 item 26: Vanik has no tier ladder - a flat interactable/
        // cost label, closer to fishingBoatButton's own simplicity than
        // any of the *TierButton methods above.
        private void UpdateVanikButton(Market market)
        {
            bool canTrain = market.IsComplete && !market.IsTraining;
            vanikButton.interactable = canTrain;
            if (vanikLabel != null)
            {
                vanikLabel.text = market.IsTraining ? "Training Vanik..." : "Train Vanik (80 Wood, 20 Gold)";
            }
        }

        // Wave 4 item 27: Vaidya/Purohita, like Vanik, have no tier ladder -
        // a flat interactable toggle for both, no dedicated cost label
        // (neither vaidyaButton nor purohitaButton has one wired, matching
        // tradeShipButton's own "flat cost like warGalleyButton" shape).
        private void UpdateMonasteryButtons(Monastery monastery)
        {
            bool canTrain = monastery.IsComplete && !monastery.IsTraining;
            vaidyaButton.interactable = canTrain;
            purohitaButton.interactable = canTrain;
        }

        private static void UpdateTradeButton(Button button, TMP_Text label, string verb, string resourceName, bool canAfford, float goldAmount)
        {
            button.interactable = canAfford;
            label.text = verb == "Sell"
                ? $"Sell {(int)MarketTradeAmount} {resourceName} ({(int)goldAmount} Gold)"
                : $"Buy {(int)MarketTradeAmount} {resourceName} ({(int)goldAmount} Gold)";
        }

        // Wave 4 item 24: Trebuchet - the first Barracks train button whose
        // own interactable state depends on the current Age directly
        // (Imperial only), not just canTrain, since it has no tier ladder
        // to hang an age gate on the way every other tiered unit does.
        private void UpdateTrebuchetButton(Barracks barracks, bool canTrain)
        {
            bool ageReady = AgeProgress.CurrentAge(NetworkMatch.LocalFaction) == AgeId.Imperial;
            trebuchetButton.interactable = canTrain && ageReady;
            trebuchetLabel.text = ageReady
                ? "Train Trebuchet (200 Wood, 150 Gold)"
                : "Train Trebuchet (Requires Imperial Age)";
        }

        // Phase 6: separate from UpdateUpgradeButton since a unique tech
        // has exactly one level (no tier/hasNextTier concept) - "Max" would
        // be a confusing label for something that was never tiered at all.
        private void UpdateUniqueTechButton(Barracks barracks)
        {
            UniqueTechDefinition tech = barracks.UniqueTech;
            CivilizationId civ = CivilizationRegistry.For(BuildingFaction(barracks));
            SetGridIcon(uniqueTechButton, UniqueTechIconKeys.TryGetValue(civ, out string icon) ? icon : null);

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
            bool barracksComplete, bool isResearching, float progress, int tier, bool hasNextTier, float goldCost,
            bool ageRequirementMet, AgeId requiredAge)
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

            // Wave 3 item 17: each of the 3 tiers now needs its own age
            // (Classical/Durg/Imperial) - same age-gate branch shape every
            // other tier button's own Update*TierButton method already has.
            if (!ageRequirementMet)
            {
                button.interactable = false;
                label.text = $"Upgrade {trackName} (needs {requiredAge} Age)";
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
            lumberCampButton.gameObject.SetActive(active);
            miningCampButton.gameObject.SetActive(active);
            millButton.gameObject.SetActive(active);
            durgButton.gameObject.SetActive(active);
            karmashalaButton.gameObject.SetActive(active);
            monasteryButton.gameObject.SetActive(active);
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

            if (!AgeProgress.HasNextAge(NetworkMatch.LocalFaction))
            {
                ageButton.interactable = false;
                ageLabel.text = "Imperial Age (Max)";
                return;
            }

            AgeId current = AgeProgress.CurrentAge(NetworkMatch.LocalFaction);
            AgeProfile next = AgeProfile.For(AgeProgress.NextAge(NetworkMatch.LocalFaction));
            if (AgeUpRequirement.AppliesTo(current) && !AgeUpRequirement.IsMet(NetworkMatch.LocalFaction))
            {
                ageButton.interactable = false;
                ageLabel.text = $"Advance to {next.DisplayName} (needs {AgeUpRequirement.RequiredBuildingCount} buildings)";
                return;
            }

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
                EnqueueTrain(townCenter, townCenter.RequestTrain, NetTrainKind.Soldier);
            }
        }

        private void TrainSoldierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrain, NetTrainKind.Soldier);
            }
        }

        private void TrainArcherAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainArcher, NetTrainKind.Archer);
            }
        }

        private void TrainCavalryAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainCavalry, NetTrainKind.Cavalry);
            }
        }

        private void TrainSiegeAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainSiege, NetTrainKind.Siege);
            }
        }

        private void TrainSpearmanAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainSpearman, NetTrainKind.Spearman);
            }
        }

        private void TrainCharaAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainChara, NetTrainKind.Chara);
            }
        }

        private void TrainSkirmisherAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainSkirmisher, NetTrainKind.Skirmisher);
            }
        }

        private void TrainBatteringRamAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainBatteringRam, NetTrainKind.BatteringRam);
            }
        }

        private void TrainCavalryArcherAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainCavalryArcher, NetTrainKind.CavalryArcher);
            }
        }

        private void TrainCamelRiderAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainCamelRider, NetTrainKind.CamelRider);
            }
        }

        private void TrainScorpionAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainScorpion, NetTrainKind.Scorpion);
            }
        }

        private void TrainTrebuchetAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                EnqueueTrain(barracks, barracks.RequestTrainTrebuchet, NetTrainKind.Trebuchet);
            }
        }

        private void TrainUniqueUnitAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Durg durg)
            {
                EnqueueTrain(durg, durg.RequestTrainUniqueUnit, NetTrainKind.UniqueUnit);
            }
        }

        private void TrainUniqueUnit2AtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Durg durg)
            {
                EnqueueTrain(durg, () => durg.RequestTrainUniqueUnit(1), NetTrainKind.UniqueUnitSlot1);
            }
        }

        // Wave 4 item 28: same EnqueueTrain convention as every other
        // trainable unit - routes through CommandBus's lockstep queue.
        private void TrainHeroAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Durg durg)
            {
                EnqueueTrain(durg, durg.RequestTrainHero, NetTrainKind.Hero);
            }
        }

        // General garrisoning system (2026-09-01): no train cost/command
        // needed - same direct-call convention TradeAtSelected uses for
        // Market.Sell/Buy. Empties every occupant at once (TownCenter/
        // Tower/Wall alike) rather than one at a time.
        private void UngarrisonAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding != null
                && _selectionManager.SelectedBuilding.TryGetComponent(out GarrisonPoint garrisonPoint))
            {
                garrisonPoint.UngarrisonAll();
            }
        }

        private void TrainFishingBoatAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                EnqueueTrain(dock, dock.RequestTrainFishingBoat, NetTrainKind.FishingBoat);
            }
        }

        private void TrainWarGalleyAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                EnqueueTrain(dock, dock.RequestTrainWarGalley, NetTrainKind.WarGalley);
            }
        }

        private void TrainFireShipAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                EnqueueTrain(dock, dock.RequestTrainFireShip, NetTrainKind.FireShip);
            }
        }

        // Wave 4 item 26.
        private void TrainTradeShipAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                EnqueueTrain(dock, dock.RequestTrainTradeShip, NetTrainKind.TradeShip);
            }
        }

        private void TrainVanikAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Market market)
            {
                EnqueueTrain(market, market.RequestTrainVanik, NetTrainKind.Vanik);
            }
        }

        // Wave 4 item 27.
        private void TrainVaidyaAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Monastery monastery)
            {
                EnqueueTrain(monastery, monastery.RequestTrainVaidya, NetTrainKind.Vaidya);
            }
        }

        private void TrainPurohitaAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Monastery monastery)
            {
                EnqueueTrain(monastery, monastery.RequestTrainPurohita, NetTrainKind.Purohita);
            }
        }

        // Phase 5 LAN transport MVP: every Train button funnels through
        // here instead of calling CommandBus.Enqueue directly, so the
        // matching NetTrainCommand only needs writing once. No-op network
        // send in single-player (NetworkMatch.IsActive stays false) - see
        // NetworkMatch.cs.
        private static void EnqueueTrain(Building source, System.Action requestTrain, NetTrainKind netKind)
        {
            FactionId faction = BuildingFaction(source);
            int tick = CommandBus.Enqueue(new TrainCommand(faction, source, requestTrain));

            if (NetworkMatch.IsActive)
            {
                NetworkMatch.Transport.Send(CommandSerializer.ForTrain(tick, faction, source, netKind));
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
            return building.TryGetComponent(out FactionMember member) ? member.Faction : NetworkMatch.LocalFaction;
        }

        private void ResearchAttackAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Karmashala karmashala)
            {
                karmashala.RequestResearchAttack();
            }
        }

        private void ResearchArmorAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Karmashala karmashala)
            {
                karmashala.RequestResearchArmor();
            }
        }

        private void ResearchUniqueTechAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchUniqueTech();
            }
        }

        private void ResearchInfantryTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchInfantryTier();
            }
        }

        private void ResearchSpearmanTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchSpearmanTier();
            }
        }

        private void ResearchArcherTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchArcherTier();
            }
        }

        private void ResearchCavalryTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchCavalryTier();
            }
        }

        private void ResearchSiegeTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchSiegeTier();
            }
        }

        private void ResearchCharaTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchCharaTier();
            }
        }

        private void ResearchSkirmisherTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchSkirmisherTier();
            }
        }

        private void ResearchBatteringRamTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchBatteringRamTier();
            }
        }

        private void ResearchCavalryArcherTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchCavalryArcherTier();
            }
        }

        private void ResearchCamelRiderTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchCamelRiderTier();
            }
        }

        private void ResearchScorpionTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestResearchScorpionTier();
            }
        }

        // Wave 3 item 15: unlike most tier buttons, acts on a selected
        // Dock - War Galleys train there, not Barracks (same deviation
        // item 13's Elephant line already established for Durg).
        private void ResearchNavalTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                dock.RequestResearchNavalTier();
            }
        }

        // Wave 4 item 25: Fire Ship tier ladder - also acts on a selected
        // Dock, an independent track from ResearchNavalTierAtSelected.
        private void ResearchFireShipTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Dock dock)
            {
                dock.RequestResearchFireShipTier();
            }
        }

        // Wave 3 item 13: unlike every other tier button, acts on a
        // selected Durg - War Elephants train there, not Barracks.
        private void ResearchElephantTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Durg durg)
            {
                durg.RequestResearchElephantTier();
            }
        }

        // Wave 3 item 16: Elite tier research for slot 0 - like
        // ResearchElephantTierAtSelected, acts on a selected Durg.
        private void ResearchEliteTierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Durg durg)
            {
                durg.RequestResearchEliteTier(0);
            }
        }

        private void ResearchEliteTier2AtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Durg durg)
            {
                durg.RequestResearchEliteTier(1);
            }
        }

        private void RequestAgeUpAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is TownCenter townCenter
                && !townCenter.IsAgingUp && AgeProgress.HasNextAge(NetworkMatch.LocalFaction))
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

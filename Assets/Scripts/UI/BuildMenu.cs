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
        [SerializeField] private Button uniqueUnitButton;
        [SerializeField] private TMP_Text uniqueUnitLabel;
        // Roadmap Section 5 item 3: Maurya/Maratha each have a 2nd unique
        // unit (see UniqueUnitDefinition/Durg.UniqueUnitCount - moved from
        // Barracks in Wave 2 item 7) - hidden entirely for the 3 civs that
        // only have 1 (see Update()).
        [SerializeField] private Button uniqueUnitButton2;
        [SerializeField] private TMP_Text uniqueUnitLabel2;
        [SerializeField] private Button ungarrisonButton;
        [SerializeField] private Button fishingBoatButton;
        [SerializeField] private Button warGalleyButton;
        // Wave 3 item 15: Naval tier ladder research button, same gating
        // shape as siegeTierButton - acts on a selected Dock (War Galley
        // trains there, not Barracks).
        [SerializeField] private Button navalTierButton;
        [SerializeField] private TMP_Text navalTierLabel;
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
        private KeyCode _keyTrainUniqueUnit;
        private KeyCode _keyTrainUniqueUnit2;
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
        private KeyCode _keyTrainFishingBoat;
        private KeyCode _keyTrainWarGalley;
        private KeyCode _keyResearchNavalTier;
        private KeyCode _keyUngarrison;

        private BuildingPlacer _placer;
        private SelectionManager _selectionManager;

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
            uniqueUnitButton.onClick.AddListener(TrainUniqueUnitAtSelected);
            uniqueUnitButton2.onClick.AddListener(TrainUniqueUnit2AtSelected);
            ungarrisonButton.onClick.AddListener(UngarrisonAtSelected);
            fishingBoatButton.onClick.AddListener(TrainFishingBoatAtSelected);
            warGalleyButton.onClick.AddListener(TrainWarGalleyAtSelected);
            navalTierButton.onClick.AddListener(ResearchNavalTierAtSelected);
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
            ageButton.onClick.AddListener(RequestAgeUpAtSelected);
            improvedToolsButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.ImprovedTools));
            packMulesButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.PackMules));
            tradeDiscountsButton.onClick.AddListener(() => ResearchEconomyTechAtSelected(EconomyTech.TradeDiscounts));
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
            _keyTrainUniqueUnit = GameSettings.GetKey("TrainUniqueUnit", KeyCode.Q);
            _keyTrainUniqueUnit2 = GameSettings.GetKey("TrainUniqueUnit2", KeyCode.Z);
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
            _keyTrainFishingBoat = GameSettings.GetKey("TrainDockUnit", KeyCode.B);
            _keyTrainWarGalley = GameSettings.GetKey("TrainWarGalley", KeyCode.W);
            _keyResearchNavalTier = GameSettings.GetKey("ResearchNavalTier", KeyCode.X);
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
                karmashalaButton,
                workerButton, soldierButton, archerButton, cavalryButton,
                siegeButton, spearmanButton, charaButton, skirmisherButton, batteringRamButton, uniqueUnitButton, uniqueUnitButton2, ungarrisonButton,
                fishingBoatButton, warGalleyButton, sellWoodButton, buyWoodButton, sellFoodButton,
                buyFoodButton, sellStoneButton, buyStoneButton, attackUpgradeButton, armorUpgradeButton,
                uniqueTechButton, infantryTierButton, spearmanTierButton, archerTierButton, cavalryTierButton, siegeTierButton, elephantTierButton, eliteTierButton, eliteTierButton2, charaTierButton, skirmisherTierButton, batteringRamTierButton, navalTierButton, ageButton, improvedToolsButton, packMulesButton, tradeDiscountsButton,
            };

            foreach (Button button in buttons)
            {
                if (normal == null)
                {
                    UIStyleTheme.Current.ApplyButton(button.image);
                    continue;
                }

                button.image.sprite = normal;
                button.image.type = Image.Type.Sliced;
                // Source art is 669x679 for a much larger button than these
                // 204x28 command-card rows - shrinks the 9-slice border to
                // fit without overlapping into the button's center.
                button.image.pixelsPerUnitMultiplier = 18f;
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = hover,
                    pressedSprite = pressed,
                    disabledSprite = disabled,
                };
            }

            AddCommandIcon(barracksButton, "build_barracks");
            AddCommandIcon(farmButton, "build_farm");
            AddCommandIcon(houseButton, "build_house");
            AddCommandIcon(wallButton, "build_wall");
            AddCommandIcon(gateButton, "build_gate");
            AddCommandIcon(towerButton, "build_tower");
            AddCommandIcon(marketButton, "build_market");
            AddCommandIcon(dockButton, "build_dock");
            AddCommandIcon(workerButton, "train_worker");
            AddCommandIcon(soldierButton, "train_soldier");
            AddCommandIcon(archerButton, "train_archer");
            AddCommandIcon(cavalryButton, "train_cavalry");
            AddCommandIcon(siegeButton, "train_siege");
            AddCommandIcon(spearmanButton, "train_spearman");
            AddCommandIcon(charaButton, "train_chara");
            AddCommandIcon(uniqueUnitButton, "train_unique_1");
            AddCommandIcon(uniqueUnitButton2, "train_unique_2");
            AddCommandIcon(attackUpgradeButton, "upgrade_attack");
            AddCommandIcon(armorUpgradeButton, "upgrade_armor");
            AddCommandIcon(ageButton, "advance_age");
            AddCommandIcon(sellWoodButton, "resource_wood");
            AddCommandIcon(buyWoodButton, "resource_wood");
            AddCommandIcon(sellFoodButton, "resource_food");
            AddCommandIcon(buyFoodButton, "resource_food");
            AddCommandIcon(sellStoneButton, "resource_stone");
            AddCommandIcon(buyStoneButton, "resource_stone");
            // No matching icon asset (not in the original spec) - stay
            // text-only: ungarrisonButton, fishingBoatButton, warGalleyButton,
            // uniqueTechButton, improvedToolsButton, packMulesButton,
            // tradeDiscountsButton, lumberCampButton, miningCampButton,
            // millButton, durgButton (Wave 2 item 7), karmashalaButton
            // (Wave 2 item 8 - no bespoke art yet, see KarmashalaFactory's
            // own asset-gap note), infantryTierButton (Wave 3 item 9),
            // spearmanTierButton (Wave 3 item 10), archerTierButton
            // (Wave 3 item 11), cavalryTierButton (Wave 3 item 12),
            // siegeTierButton (Wave 3 item 14), navalTierButton
            // (Wave 3 item 15), eliteTierButton/eliteTierButton2
            // (Wave 3 item 16), charaTierButton (Wave 4 item 18).
            // No "train_chara" icon asset exists either (Wave 4 item 18) -
            // charaButton stays text-only too, same "no matching art yet"
            // fallback as the buttons above (AddCommandIcon no-ops
            // harmlessly when the sprite is missing). Same for
            // skirmisherButton/skirmisherTierButton (Wave 4 item 19) - no
            // "train_skirmisher" icon asset exists either. Same for
            // batteringRamButton/batteringRamTierButton (Wave 4 item 20) -
            // no "train_battering_ram" icon asset exists either.
        }

        // Adds a small icon to the left edge of a command-card button and
        // insets its text label by the same amount so they don't overlap.
        // These buttons are thin 204x28 rows and some labels (dynamic cost
        // strings like Barracks') already sit close to the button's full
        // width - insetting 26px can push the longest labels to wrap onto a
        // 2nd line. TMP's overflow mode on these labels is Overflow (not
        // clipped), so the accepted worst case is a couple of buttons
        // showing slightly-taller wrapped text, not lost/clipped info.
        private static void AddCommandIcon(Button button, string iconName)
        {
            Sprite icon = Resources.Load<Sprite>("UI/Icons/" + iconName);
            if (icon == null)
            {
                return;
            }

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(20f, 20f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            iconGo.GetComponent<Image>().sprite = icon;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.offsetMin = new Vector2(26f, labelRect.offsetMin.y);
            }
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
            // General garrisoning system (2026-09-01): every building with
            // a GarrisonPoint (TownCenter/Tower/Wall) shows the Ungarrison
            // button when occupied - no longer restricted to Wall/Tower's
            // type, since TownCenter now has one too.
            GarrisonPoint garrisonPoint = ownsSelected ? selected.GetComponent<GarrisonPoint>() : null;

            HandleHotkeys(townCenter, barracks, dock, durg, karmashala, garrisonPoint);

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
            uniqueUnitButton.gameObject.SetActive(durg != null);
            uniqueUnitButton2.gameObject.SetActive(durg != null && durg.UniqueUnitCount > 1);
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
        }

        // Item 1 (Hotkeys): each check calls the exact same handler its
        // matching button's onClick uses, so a hotkey press when the action
        // is unavailable is a harmless no-op (RequestTrain*/RequestResearch*
        // already self-guard) - identical behavior to the button being
        // disabled. Gated per-parameter (not a single "selected something"
        // check) so a key only ever acts on the currently selected building
        // of the matching type, fixing the old per-building Update() bug.
        private void HandleHotkeys(TownCenter townCenter, Barracks barracks, Dock dock, Durg durg, Karmashala karmashala, GarrisonPoint garrisonPoint)
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
            uniqueUnitButton.interactable = canTrain;
            uniqueUnitLabel.text = $"Train {durg.UniqueUnit.Name} ({(int)durg.UniqueUnit.FoodCost} Food, {(int)durg.UniqueUnit.GoldCost} Gold)";

            if (durg.UniqueUnitCount > 1)
            {
                UniqueUnitDefinition unique2 = durg.UniqueUnitAt(1);
                uniqueUnitButton2.interactable = canTrain;
                uniqueUnitLabel2.text = $"Train {unique2.Name} ({(int)unique2.FoodCost} Food, {(int)unique2.GoldCost} Gold)";
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

            UpdateNavalTierButton(dock);
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Multiplayer;

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
        [SerializeField] private Button dockButton;
        [SerializeField] private TMP_Text dockLabel;
        [SerializeField] private Button workerButton;
        [SerializeField] private Button soldierButton;
        [SerializeField] private Button archerButton;
        [SerializeField] private Button attackUpgradeButton;
        [SerializeField] private TMP_Text attackUpgradeLabel;
        [SerializeField] private Button armorUpgradeButton;
        [SerializeField] private TMP_Text armorUpgradeLabel;
        [SerializeField] private Button ageButton;
        [SerializeField] private TMP_Text ageLabel;

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
            dockButton.onClick.AddListener(() => _placer.BeginPlacementDock());
            workerButton.onClick.AddListener(TrainWorkerAtSelected);
            soldierButton.onClick.AddListener(TrainSoldierAtSelected);
            archerButton.onClick.AddListener(TrainArcherAtSelected);
            attackUpgradeButton.onClick.AddListener(ResearchAttackAtSelected);
            armorUpgradeButton.onClick.AddListener(ResearchArmorAtSelected);
            ageButton.onClick.AddListener(RequestAgeUpAtSelected);
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

            SetPlacementButtonsActive(showPlacement);
            workerButton.gameObject.SetActive(townCenter != null);
            ageButton.gameObject.SetActive(townCenter != null);
            soldierButton.gameObject.SetActive(barracks != null);
            archerButton.gameObject.SetActive(barracks != null);
            attackUpgradeButton.gameObject.SetActive(barracks != null);
            armorUpgradeButton.gameObject.SetActive(barracks != null);

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
        }

        private void UpdateBarracksButtons(Barracks barracks)
        {
            bool canTrain = barracks.IsComplete && !barracks.IsTraining;
            soldierButton.interactable = canTrain;
            archerButton.interactable = canTrain;

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
            dockButton.gameObject.SetActive(active);
        }

        private void UpdateTownCenterButtons(TownCenter townCenter)
        {
            workerButton.interactable = !townCenter.IsTraining;

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

        private void RequestAgeUpAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is TownCenter townCenter
                && !townCenter.IsAgingUp && AgeProgress.HasNextAge(FactionId.Player))
            {
                townCenter.RequestAgeUp();
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

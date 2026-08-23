using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.UI
{
    // Context-sensitive build/train menu, bottom-right, AoE-style: which
    // buttons are even visible depends on what's currently selected, not
    // just whether they're enabled.
    //  - A Builder-capable unit selected (and no building selected): shows
    //    Build Barracks/Farm/House - placement still needs that worker to
    //    walk over and build it afterward.
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
        [SerializeField] private Button workerButton;
        [SerializeField] private Button soldierButton;
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
            workerButton.onClick.AddListener(TrainWorkerAtSelected);
            soldierButton.onClick.AddListener(TrainSoldierAtSelected);
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

            if (showPlacement)
            {
                barracksButton.interactable = BuildingPlacer.CanPlaceBarracks;
                barracksLabel.text = BuildingPlacer.CanPlaceBarracks
                    ? "Build Barracks (100 Wood, 50 Stone)"
                    : "Build Barracks (Requires Classical Age)";
                farmButton.interactable = true;
                houseButton.interactable = true;
            }

            if (townCenter != null)
            {
                UpdateTownCenterButtons(townCenter);
            }

            if (barracks != null)
            {
                soldierButton.interactable = barracks.IsComplete && !barracks.IsTraining;
            }
        }

        private void SetPlacementButtonsActive(bool active)
        {
            barracksButton.gameObject.SetActive(active);
            farmButton.gameObject.SetActive(active);
            houseButton.gameObject.SetActive(active);
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
                townCenter.RequestTrain();
            }
        }

        private void TrainSoldierAtSelected()
        {
            if (_selectionManager != null && _selectionManager.SelectedBuilding is Barracks barracks)
            {
                barracks.RequestTrain();
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

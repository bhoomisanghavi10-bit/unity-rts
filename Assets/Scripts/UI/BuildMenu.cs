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
    // Minimal build/train menu, bottom-right: buttons that call the same
    // entry points the B/F/H/G/T hotkeys already use. Population: X/Y is
    // shown by ResourceHUD instead, alongside the other resource counters.
    // Build Barracks/Farm/House only enable with a worker (a unit with
    // Builder) selected, since placement still needs one to actually build
    // it afterward. uGUI/TMP replacement for the original OnGUI version -
    // buttons are real Canvas children wired to these entry points once in
    // Awake via onClick, this just toggles interactable/text every frame
    // instead of re-issuing GUI.Button draw calls (and re-deciding whether
    // a click landed) regardless of state.
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
            workerButton.onClick.AddListener(TrainAtAllReadyTownCenters);
            soldierButton.onClick.AddListener(TrainAtAllReadyBarracks);
            ageButton.onClick.AddListener(RequestAgeUp);
        }

        private void Update()
        {
            bool canBuild = _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected();

            barracksButton.interactable = canBuild && BuildingPlacer.CanPlaceBarracks;
            barracksLabel.text = BuildingPlacer.CanPlaceBarracks
                ? "Build Barracks (100 Wood, 50 Stone)"
                : "Build Barracks (Requires Classical Age)";

            farmButton.interactable = canBuild;
            houseButton.interactable = canBuild;

            UpdateAgeButton();
        }

        // Age-up runs on its own independent countdown on TownCenter (see
        // TownCenter.RequestAgeUp/IsAgingUp) - parallel to Worker training,
        // not sharing its busy slot, so the button stays live/showing
        // progress even while a Worker is also being trained.
        private void UpdateAgeButton()
        {
            TownCenter townCenter = FindPlayerTownCenter();
            if (townCenter == null)
            {
                ageButton.interactable = false;
                ageLabel.text = "Advance Age";
                return;
            }

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

        private void RequestAgeUp()
        {
            TownCenter townCenter = FindPlayerTownCenter();
            if (townCenter != null && !townCenter.IsAgingUp && AgeProgress.HasNextAge(FactionId.Player))
            {
                townCenter.RequestAgeUp();
            }
        }

        private static TownCenter FindPlayerTownCenter()
        {
            foreach (Building building in Building.All)
            {
                if (building is TownCenter townCenter
                    && building.TryGetComponent(out FactionMember factionMember)
                    && factionMember.Faction == FactionId.Player)
                {
                    return townCenter;
                }
            }

            return null;
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

        private static void TrainAtAllReadyBarracks()
        {
            foreach (Building building in Building.All)
            {
                if (!(building is Barracks barracks))
                {
                    continue;
                }

                if (!building.TryGetComponent(out FactionMember factionMember)
                    || factionMember.Faction != FactionId.Player)
                {
                    continue;
                }

                barracks.RequestTrain();
            }
        }

        private static void TrainAtAllReadyTownCenters()
        {
            foreach (Building building in Building.All)
            {
                if (!(building is TownCenter townCenter))
                {
                    continue;
                }

                if (!building.TryGetComponent(out FactionMember factionMember)
                    || factionMember.Faction != FactionId.Player)
                {
                    continue;
                }

                townCenter.RequestTrain();
            }
        }
    }
}

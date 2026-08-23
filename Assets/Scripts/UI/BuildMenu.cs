using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.UI
{
    // Context-sensitive build/train menu, bottom-right - AoE-style: only
    // shows commands relevant to whatever is currently selected, instead of
    // one fixed panel of every action. A selected Worker (has Builder)
    // shows Build Barracks/Farm/House; the Player's own selected TownCenter
    // shows Train Worker + Advance Age; the Player's own selected Barracks
    // shows Train Soldier. Nothing selected (or an enemy building, or a
    // non-Builder unit) draws nothing. SelectionManager enforces that unit
    // selection and building selection are mutually exclusive, so at most
    // one of these panels is ever relevant at a time.
    public class BuildMenu : MonoBehaviour
    {
        private BuildingPlacer _placer;
        private SelectionManager _selectionManager;

        private void Awake()
        {
            _placer = FindFirstObjectByType<BuildingPlacer>();
            _selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        private void OnGUI()
        {
            if (_selectionManager == null)
            {
                return;
            }

            if (HasBuilderSelected())
            {
                DrawBuilderMenu();
            }
            else if (_selectionManager.SelectedBuilding is TownCenter townCenter && IsPlayerOwned(townCenter))
            {
                DrawTownCenterMenu(townCenter);
            }
            else if (_selectionManager.SelectedBuilding is Barracks barracks && IsPlayerOwned(barracks))
            {
                DrawBarracksMenu(barracks);
            }
        }

        private void DrawBuilderMenu()
        {
            const float width = 220f;
            const float height = 112f;
            float x = Screen.width - width - 8f;
            float y = Screen.height - height - 8f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUI.enabled = _placer != null && !BuildingPlacer.IsPlacing && BuildingPlacer.CanPlaceBarracks;
            string barracksLabel = BuildingPlacer.CanPlaceBarracks
                ? "Build Barracks (100 Wood, 50 Stone)"
                : "Build Barracks (Requires Classical Age)";
            if (GUI.Button(new Rect(x + 8, y + 4, width - 16, 28), barracksLabel))
            {
                _placer.BeginPlacementBarracks();
            }

            GUI.enabled = _placer != null && !BuildingPlacer.IsPlacing;
            if (GUI.Button(new Rect(x + 8, y + 40, width - 16, 28), "Build Farm (60 Wood)"))
            {
                _placer.BeginPlacementFarm();
            }

            if (GUI.Button(new Rect(x + 8, y + 76, width - 16, 28), "Build House (30 Wood)"))
            {
                _placer.BeginPlacementHouse();
            }

            GUI.enabled = true;
        }

        private void DrawTownCenterMenu(TownCenter townCenter)
        {
            const float width = 220f;
            const float height = 76f;
            float x = Screen.width - width - 8f;
            float y = Screen.height - height - 8f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            if (GUI.Button(new Rect(x + 8, y + 4, width - 16, 28), "Train Worker (50 Food)"))
            {
                townCenter.RequestTrain();
            }

            DrawAgeButton(townCenter, x, y + 40, width);
        }

        private void DrawBarracksMenu(Barracks barracks)
        {
            const float width = 220f;
            const float height = 40f;
            float x = Screen.width - width - 8f;
            float y = Screen.height - height - 8f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            if (GUI.Button(new Rect(x + 8, y + 4, width - 16, 28), "Train Soldier (50 Food, 20 Gold)"))
            {
                barracks.RequestTrain();
            }
        }

        // Age-up runs on its own independent countdown on TownCenter (see
        // TownCenter.RequestAgeUp/IsAgingUp) - parallel to Worker training,
        // not sharing its busy slot, so the button stays live/showing
        // progress even while a Worker is also being trained.
        private static void DrawAgeButton(TownCenter townCenter, float x, float y, float width)
        {
            if (townCenter.IsAgingUp)
            {
                GUI.enabled = false;
                GUI.Button(new Rect(x + 8, y, width - 16, 28), $"Researching Age... {(int)(townCenter.AgeUpProgress * 100f)}%");
                GUI.enabled = true;
                return;
            }

            if (!AgeProgress.HasNextAge(FactionId.Player))
            {
                GUI.enabled = false;
                GUI.Button(new Rect(x + 8, y, width - 16, 28), "Imperial Age (Max)");
                GUI.enabled = true;
                return;
            }

            AgeProfile next = AgeProfile.For(AgeProgress.NextAge(FactionId.Player));
            if (GUI.Button(new Rect(x + 8, y, width - 16, 28), $"Advance to {next.DisplayName} ({(int)next.WoodCost} Wood, {(int)next.StoneCost} Stone)"))
            {
                townCenter.RequestAgeUp();
            }
        }

        private static bool IsPlayerOwned(Building building)
        {
            return building.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == FactionId.Player;
        }

        private bool HasBuilderSelected()
        {
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

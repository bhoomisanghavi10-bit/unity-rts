using UnityEngine;
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
    // it afterward.
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
            const float width = 220f;
            const float height = 238f;
            float x = Screen.width - width - 8f;
            float y = Screen.height - height - 8f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUI.enabled = _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected() && BuildingPlacer.CanPlaceBarracks;
            string barracksLabel = BuildingPlacer.CanPlaceBarracks
                ? "Build Barracks (100 Wood, 50 Stone)"
                : "Build Barracks (Requires Classical Age)";
            if (GUI.Button(new Rect(x + 8, y + 4, width - 16, 28), barracksLabel))
            {
                _placer.BeginPlacementBarracks();
            }

            GUI.enabled = _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected();
            if (GUI.Button(new Rect(x + 8, y + 40, width - 16, 28), "Build Farm (60 Wood)"))
            {
                _placer.BeginPlacementFarm();
            }

            if (GUI.Button(new Rect(x + 8, y + 76, width - 16, 28), "Build House (30 Wood)"))
            {
                _placer.BeginPlacementHouse();
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(x + 8, y + 112, width - 16, 28), "Train Worker (50 Food)"))
            {
                TrainAtAllReadyTownCenters();
            }

            if (GUI.Button(new Rect(x + 8, y + 148, width - 16, 28), "Train Soldier (50 Food, 20 Gold)"))
            {
                TrainAtAllReadyBarracks();
            }

            DrawAgeButton(x, y + 184, width);
        }

        // Age-up runs on its own independent countdown on TownCenter (see
        // TownCenter.RequestAgeUp/IsAgingUp) - parallel to Worker training,
        // not sharing its busy slot, so the button stays live/showing
        // progress even while a Worker is also being trained.
        private void DrawAgeButton(float x, float y, float width)
        {
            TownCenter townCenter = FindPlayerTownCenter();
            if (townCenter == null)
            {
                return;
            }

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

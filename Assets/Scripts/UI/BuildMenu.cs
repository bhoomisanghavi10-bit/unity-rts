using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;

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
            const float height = 202f;
            float x = Screen.width - width - 8f;
            float y = Screen.height - height - 8f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUI.enabled = _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected();
            if (GUI.Button(new Rect(x + 8, y + 4, width - 16, 28), "Build Barracks (100 Wood, 50 Stone)"))
            {
                _placer.BeginPlacementBarracks();
            }

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

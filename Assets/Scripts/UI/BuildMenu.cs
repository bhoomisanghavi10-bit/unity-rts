using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.UI
{
    // Minimal build/train menu, bottom-right: a couple of buttons that call
    // the same entry points the B/T hotkeys already use. Build Barracks
    // only enables with a worker (a unit with Builder) selected, since
    // placement still needs one to actually build it afterward.
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
            const float width = 190f;
            const float height = 82f;
            float x = Screen.width - width - 8f;
            float y = Screen.height - height - 8f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUI.enabled = _placer != null && !BuildingPlacer.IsPlacing && HasBuilderSelected();
            if (GUI.Button(new Rect(x + 8, y + 8, width - 16, 28), "Build Barracks (100 Wood)"))
            {
                _placer.BeginPlacement();
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(x + 8, y + 44, width - 16, 28), "Train Soldier (50 Food)"))
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
                if (building is Barracks barracks)
                {
                    barracks.RequestTrain();
                }
            }
        }
    }
}

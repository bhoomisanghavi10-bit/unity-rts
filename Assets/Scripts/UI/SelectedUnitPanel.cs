using UnityEngine;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.UI
{
    // Bottom-left info panel for whatever SelectionManager currently has
    // selected: a single unit's name/status/HP, a headcount for a group, or
    // a selected building's name/HP/construction progress (own or enemy -
    // lets the Player inspect an enemy building's HP before committing an
    // army to raze it, same as HoverTooltip already shows on hover).
    public class SelectedUnitPanel : MonoBehaviour
    {
        private SelectionManager _selectionManager;
        private GUIStyle _style;

        private void Awake()
        {
            _selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        private void OnGUI()
        {
            if (_selectionManager == null)
            {
                return;
            }

            if (_selectionManager.Selected.Count > 0)
            {
                DrawUnitPanel();
            }
            else if (_selectionManager.SelectedBuilding != null)
            {
                DrawBuildingPanel(_selectionManager.SelectedBuilding);
            }
        }

        private void DrawUnitPanel()
        {
            EnsureStyle();

            const float panelHeight = 70f;
            const float panelWidth = 220f;
            float y = Screen.height - panelHeight - 8f;

            GUI.Box(new Rect(8, y, panelWidth, panelHeight), GUIContent.none);

            if (_selectionManager.Selected.Count == 1)
            {
                DrawSingle(_selectionManager.Selected[0], y);
            }
            else
            {
                GUI.Label(new Rect(16, y + 4, panelWidth - 16, 20),
                    $"{_selectionManager.Selected.Count} units selected", _style);
            }
        }

        private void DrawSingle(Unit unit, float y)
        {
            GUI.Label(new Rect(16, y + 4, 200, 20), unit.gameObject.name, _style);
            GUI.Label(new Rect(16, y + 24, 200, 20), UnitStatus.Describe(unit), _style);

            if (unit.TryGetComponent(out Attackable attackable))
            {
                GUI.Label(new Rect(16, y + 44, 200, 20), $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}", _style);
            }
        }

        private void DrawBuildingPanel(Building building)
        {
            EnsureStyle();

            const float panelHeight = 70f;
            const float panelWidth = 220f;
            float y = Screen.height - panelHeight - 8f;

            GUI.Box(new Rect(8, y, panelWidth, panelHeight), GUIContent.none);

            GUI.Label(new Rect(16, y + 4, 200, 20), building.gameObject.name, _style);

            if (building.TryGetComponent(out ConstructionSite site) && !site.IsComplete)
            {
                GUI.Label(new Rect(16, y + 24, 200, 20), $"Building: {(int)(site.Progress * 100f)}%", _style);
            }

            if (building.TryGetComponent(out Attackable attackable))
            {
                GUI.Label(new Rect(16, y + 44, 200, 20), $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}", _style);
            }
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _style.normal.textColor = Color.white;
        }
    }
}

using UnityEngine;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.UI
{
    // Bottom-left info panel for whatever SelectionManager currently has
    // selected: a single unit's name/status/HP, or a headcount for a group.
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
            if (_selectionManager == null || _selectionManager.Selected.Count == 0)
            {
                return;
            }

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

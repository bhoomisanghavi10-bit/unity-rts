using UnityEngine;
using TMPro;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.UI
{
    // Bottom-left info panel for whatever SelectionManager currently has
    // selected: a single unit's name/status/HP, a building's name/HP/build
    // progress, or a headcount for a group of units.
    // uGUI/TMP replacement for the original OnGUI version - the panel
    // background and labels are real Canvas children wired up in the
    // Inspector; this just toggles which ones are active and pushes text
    // into them instead of issuing GUI.Box/GUI.Label draw calls every
    // frame regardless of selection state.
    public class SelectedUnitPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text hpLabel;
        [SerializeField] private TMP_Text groupCountLabel;

        private SelectionManager _selectionManager;

        private void Awake()
        {
            _selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        private void Update()
        {
            Building selectedBuilding = _selectionManager != null ? _selectionManager.SelectedBuilding : null;
            bool hasUnitSelection = _selectionManager != null && _selectionManager.Selected.Count > 0;
            bool hasSelection = hasUnitSelection || selectedBuilding != null;
            panelRoot.SetActive(hasSelection);
            if (!hasSelection)
            {
                return;
            }

            if (selectedBuilding != null)
            {
                nameLabel.gameObject.SetActive(true);
                statusLabel.gameObject.SetActive(true);
                groupCountLabel.gameObject.SetActive(false);
                DrawBuilding(selectedBuilding);
                return;
            }

            bool single = _selectionManager.Selected.Count == 1;
            nameLabel.gameObject.SetActive(single);
            statusLabel.gameObject.SetActive(single);
            groupCountLabel.gameObject.SetActive(!single);

            if (single)
            {
                DrawSingle(_selectionManager.Selected[0]);
            }
            else
            {
                groupCountLabel.text = $"{_selectionManager.Selected.Count} units selected";
            }
        }

        private void DrawSingle(Unit unit)
        {
            nameLabel.text = unit.gameObject.name;
            statusLabel.text = UnitStatus.Describe(unit);

            bool hasAttackable = unit.TryGetComponent(out Attackable attackable);
            hpLabel.gameObject.SetActive(hasAttackable);
            if (hasAttackable)
            {
                hpLabel.text = $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}";
            }
        }

        private void DrawBuilding(Building building)
        {
            nameLabel.text = building.gameObject.name;
            statusLabel.text = building.TryGetComponent(out ConstructionSite site) && !site.IsComplete
                ? $"Building... {(int)(site.Progress * 100f)}%"
                : "Complete";

            bool hasAttackable = building.TryGetComponent(out Attackable attackable);
            hpLabel.gameObject.SetActive(hasAttackable);
            if (hasAttackable)
            {
                hpLabel.text = $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}";
            }
        }
    }
}

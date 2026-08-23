using UnityEngine;
using TMPro;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.UI
{
    // Bottom-left info panel for whatever SelectionManager currently has
    // selected: a single unit's name/status/HP, or a headcount for a group.
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
            bool hasSelection = _selectionManager != null && _selectionManager.Selected.Count > 0;
            panelRoot.SetActive(hasSelection);
            if (!hasSelection)
            {
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
    }
}

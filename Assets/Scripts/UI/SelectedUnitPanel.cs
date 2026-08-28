using UnityEngine;
using UnityEngine.UI;
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

            // Dedicated art, not the shared UIStyleTheme.PanelFrameSprite -
            // that one is modal_frame.png, meant for the 4 true popups
            // (Settings/Diplomacy/MissionSelect/Objective). This panel has
            // its own purpose-made frame.
            Sprite frame = Resources.Load<Sprite>("UI/Panels/panel_selected_unit");
            if (panelRoot.TryGetComponent(out Image background))
            {
                background.color = Color.white;
                if (frame != null)
                {
                    background.sprite = frame;
                    background.type = Image.Type.Sliced;
                    // Source art is authored at 2752x1536 for a much larger
                    // canvas than this 220x70 panel - without shrinking the
                    // border's effective size to match, the 9-slice corners
                    // would overlap into the panel's center. 65 was picked by
                    // matching the sprite's authored border width against
                    // this panel's actual pixel height (see Roadmap Section
                    // 4.3 session log for the full multiplier table).
                    background.pixelsPerUnitMultiplier = 65f;
                }
            }

            SetUpHealthBar();
        }

        // hp_bar_frame/hp_bar_fill are siblings of hpLabel (same parent,
        // same rect) rather than children of it - a Graphic's own children
        // always render after its own graphic in uGUI's depth-first draw
        // order, which would have put the bar ON TOP of the text instead of
        // behind it. As siblings, inserting them at hpLabel's own sibling
        // index (pushing hpLabel one slot later) puts them behind it.
        // _hpBarRoot's active state is kept in lockstep with hpLabel's
        // wherever that's toggled (DrawSingle/DrawBuilding) since it's a
        // sibling, not an automatic parent-child hide.
        private GameObject _hpBarRoot;
        private Image _hpFill;

        private void SetUpHealthBar()
        {
            Sprite frameSprite = Resources.Load<Sprite>("UI/Panels/hp_bar_frame");
            Sprite fillSprite = Resources.Load<Sprite>("UI/Panels/hp_bar_fill");
            if (frameSprite == null || fillSprite == null || hpLabel == null)
            {
                return;
            }

            RectTransform hpRect = hpLabel.GetComponent<RectTransform>();
            _hpBarRoot = new GameObject("HpBar", typeof(RectTransform));
            _hpBarRoot.transform.SetParent(hpLabel.transform.parent, false);
            RectTransform rootRect = _hpBarRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = hpRect.anchorMin;
            rootRect.anchorMax = hpRect.anchorMax;
            rootRect.pivot = hpRect.pivot;
            rootRect.anchoredPosition = hpRect.anchoredPosition;
            rootRect.sizeDelta = hpRect.sizeDelta;
            _hpBarRoot.transform.SetSiblingIndex(hpLabel.transform.GetSiblingIndex());

            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(_hpBarRoot.transform, false);
            RectTransform frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            Image frameImage = frameGo.GetComponent<Image>();
            frameImage.sprite = frameSprite;
            frameImage.type = Image.Type.Sliced;
            // hp_bar_frame is authored at 2620x276 for roughly this rect's
            // aspect ratio, so a much smaller multiplier suffices than the
            // other, much-larger-canvas panels wired in this session.
            frameImage.pixelsPerUnitMultiplier = 13f;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(_hpBarRoot.transform, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _hpFill = fillGo.GetComponent<Image>();
            _hpFill.sprite = fillSprite;
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillAmount = 1f;
        }

        private void SetHealthBar(bool visible, Attackable attackable)
        {
            if (_hpBarRoot == null)
            {
                return;
            }

            _hpBarRoot.SetActive(visible);
            if (visible)
            {
                _hpFill.fillAmount = attackable.MaxHealth > 0f ? attackable.Health / attackable.MaxHealth : 0f;
            }
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
            if (unit.TryGetComponent(out StanceController stance))
            {
                statusLabel.text += $" | Stance: {stance.Stance} (V to cycle)";
            }

            bool hasAttackable = unit.TryGetComponent(out Attackable attackable);
            hpLabel.gameObject.SetActive(hasAttackable);
            SetHealthBar(hasAttackable, attackable);
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
            SetHealthBar(hasAttackable, attackable);
            if (hasAttackable)
            {
                hpLabel.text = $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}";
            }
        }
    }
}

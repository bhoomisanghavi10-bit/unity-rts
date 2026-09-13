using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.UI
{
    // Bottom-center info panel for whatever SelectionManager currently has
    // selected: a single unit's name/status/HP, a building's name/HP/build
    // progress, or a headcount for a group of units. The middle section of
    // the shared bottom-docked bar (Roadmap item 30) - BuildMenu (command
    // panel) on the left, this plus MatchStatus (civ/population/age, from
    // ResourceHUD) stacked above it in the middle, the minimap on the right.
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
        private BuildMenu _buildMenu;

        private void Awake()
        {
            _selectionManager = FindFirstObjectByType<SelectionManager>();
            // BuildMenu's own panel height is now dynamic (fits however
            // many grid rows the current context needs, 2026-09-12) - this
            // panel matches it every frame so the two bottom-bar panels
            // stay the same height instead of drifting out of sync
            // whenever BuildMenu grows/shrinks. `Panel` (this script's own
            // `panelRoot`) is anchor-stretched to fill this GameObject's
            // own RectTransform, so resizing `transform` here is enough -
            // no separate resize needed on `panelRoot` itself.
            _buildMenu = FindFirstObjectByType<BuildMenu>();

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
                    // 2026-09-12 Tier 2 UI art delivery: purpose-made
                    // 1729x806 frame with a real circular portrait notch
                    // positioned at the vertical MIDDLE of the left edge -
                    // unlike every other 9-sliced HUD panel, that notch sits
                    // in the stretchable middle band, not a non-stretching
                    // corner, so Image.Type.Sliced squishes it into a thin
                    // sliver at this panel's fixed 220x70 display size (live-
                    // verified, not assumed). Same fix as BuildMenu.cs's
                    // command-card buttons: Image.Type.Simple, a clean
                    // uniform stretch that keeps the notch a recognizable
                    // circle (mildly ovalized by the aspect mismatch, not
                    // squashed flat) since this panel never renders at any
                    // size but this one.
                    background.type = Image.Type.Simple;
                }
            }

            SetUpHealthBar();
            SetUpGroupIconRow();
            SetUpPortrait();
        }

        // 2026-09-14: displays the selected unit's portrait (Resources/UI/
        // Portraits/{IconKey}.png, the Tier 4 delivery staged unwired since
        // 2026-09-13) inside panel_selected_unit.png's real circular notch.
        // Anchors below are pixel-measured against the actual 1729x806
        // source texture (notch at x=[45,321), y=[213,506) from the
        // texture's bottom-left origin - not guessed - then expressed as
        // fractions of the full sprite so they track correctly regardless
        // of Panel's own dynamic height (it stretches to match BuildMenu's,
        // see the Awake() comment above, and this background Image is
        // Type.Simple with no 9-slice border, so a fraction of the full
        // rect is exactly where the notch renders at any size). A small
        // inset (0.01 fraction each edge) keeps the portrait's own square
        // corners from poking past the ring art into the flat parchment.
        private static readonly Vector2 PortraitAnchorMin = new Vector2(0.0360f, 0.2743f);
        private static readonly Vector2 PortraitAnchorMax = new Vector2(0.1757f, 0.6178f);

        private Image _portraitImage;

        private void SetUpPortrait()
        {
            if (panelRoot == null)
            {
                return;
            }

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(panelRoot.transform, false);
            var rect = (RectTransform)portraitGo.transform;
            rect.anchorMin = PortraitAnchorMin;
            rect.anchorMax = PortraitAnchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _portraitImage = portraitGo.GetComponent<Image>();
            _portraitImage.raycastTarget = false;
            _portraitImage.preserveAspect = false;
            portraitGo.SetActive(false);
        }

        private void SetPortrait(string iconKey)
        {
            if (_portraitImage == null)
            {
                return;
            }

            Sprite portrait = string.IsNullOrEmpty(iconKey)
                ? null
                : Resources.Load<Sprite>("UI/Portraits/" + iconKey);
            _portraitImage.gameObject.SetActive(portrait != null);
            if (portrait != null)
            {
                _portraitImage.sprite = portrait;
            }
        }

        // 2026-09-13: lets the player pick a specific unit out of a
        // multi-unit selection instead of only ever seeing "N units
        // selected" - a single horizontal scrollable row of per-unit
        // icons (user's own choice over a wrapping grid, which the
        // panel's tight width made awkward for large groups) underneath
        // groupCountLabel; clicking one narrows the whole selection down
        // to just that unit via SelectionManager.SelectOnly.
        private const float GroupIconSize = 28f;
        private const float GroupIconGap = 4f;
        // Matches groupCountLabel's own row height (20) plus a small gap -
        // same distance StatusLabel already sits below NameLabel in the
        // single-unit layout, so this row continues that same rhythm.
        private const float GroupRowTopOffset = 59f;

        private GameObject _groupIconRoot;
        private RectTransform _groupIconContent;
        private readonly List<Unit> _lastGroupSnapshot = new List<Unit>();

        private void SetUpGroupIconRow()
        {
            if (groupCountLabel == null)
            {
                return;
            }

            RectTransform labelRect = groupCountLabel.GetComponent<RectTransform>();

            _groupIconRoot = new GameObject("GroupIconScroll", typeof(RectTransform));
            _groupIconRoot.transform.SetParent(labelRect.parent, false);
            RectTransform rootRect = _groupIconRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, -GroupRowTopOffset);
            rootRect.sizeDelta = new Vector2(labelRect.sizeDelta.x, GroupIconSize);

            var scroll = _groupIconRoot.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = GroupIconSize;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(_groupIconRoot.transform, false);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            // Near-transparent rather than fully transparent so this Image
            // still catches drag/scroll input over the whole viewport, not
            // just where an icon's own Image sits - same convention
            // SettingsMenu's Key Bindings scroll already established.
            Image viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _groupIconContent = contentGo.GetComponent<RectTransform>();
            _groupIconContent.anchorMin = new Vector2(0f, 0.5f);
            _groupIconContent.anchorMax = new Vector2(0f, 0.5f);
            _groupIconContent.pivot = new Vector2(0f, 0.5f);
            _groupIconContent.anchoredPosition = Vector2.zero;
            _groupIconContent.sizeDelta = new Vector2(0f, GroupIconSize);

            scroll.viewport = viewportRect;
            scroll.content = _groupIconContent;

            _groupIconRoot.SetActive(false);
        }

        // Only rebuilds the icon buttons when the actual set of selected
        // units changed (not every frame) - a fresh GameObject per icon
        // every Update() would be wasteful, and would also scroll the
        // row back to the start on every single frame.
        private void RefreshGroupIconRow(IReadOnlyList<Unit> units)
        {
            if (_groupIconRoot == null)
            {
                return;
            }

            _groupIconRoot.SetActive(true);
            if (GroupSnapshotMatches(units))
            {
                return;
            }

            _lastGroupSnapshot.Clear();
            _lastGroupSnapshot.AddRange(units);

            for (int i = _groupIconContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_groupIconContent.GetChild(i).gameObject);
            }

            float x = 0f;
            foreach (Unit unit in units)
            {
                Unit capturedUnit = unit;
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(Button));
                iconGo.transform.SetParent(_groupIconContent, false);
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(GroupIconSize, GroupIconSize);
                iconRect.anchoredPosition = new Vector2(x, 0f);

                Image image = iconGo.GetComponent<Image>();
                Sprite icon = string.IsNullOrEmpty(capturedUnit.IconKey)
                    ? null
                    : Resources.Load<Sprite>("UI/Icons/" + capturedUnit.IconKey);
                if (icon != null)
                {
                    image.sprite = icon;
                    image.color = Color.white;
                }
                else
                {
                    // No icon art for this unit type yet - flat placeholder
                    // square, same fallback convention BuildMenu.cs's own
                    // command grid already uses for icon-less buttons.
                    image.color = new Color(0.6f, 0.55f, 0.4f, 1f);
                }

                Button button = iconGo.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    if (capturedUnit != null && _selectionManager != null)
                    {
                        _selectionManager.SelectOnly(capturedUnit);
                    }
                });

                x += GroupIconSize + GroupIconGap;
            }

            _groupIconContent.sizeDelta = new Vector2(Mathf.Max(0f, x - GroupIconGap), GroupIconSize);
        }

        private bool GroupSnapshotMatches(IReadOnlyList<Unit> units)
        {
            if (units.Count != _lastGroupSnapshot.Count)
            {
                return false;
            }

            for (int i = 0; i < units.Count; i++)
            {
                if (!ReferenceEquals(units[i], _lastGroupSnapshot[i]))
                {
                    return false;
                }
            }

            return true;
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
            // 2026-09-12 Tier 2 UI art delivery: purpose-made 1772x187
            // frame replacing the ornate-reskin placeholder. This bar
            // renders at hpLabel's own rect size (204x20) - 9.35 matches
            // the downscale ratio (187/20, this sprite's real native
            // height over the live display height).
            frameImage.pixelsPerUnitMultiplier = 9.35f;

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
            if (_buildMenu != null)
            {
                var buildMenuRect = (RectTransform)_buildMenu.transform;
                var selfRect = (RectTransform)transform;
                selfRect.sizeDelta = new Vector2(selfRect.sizeDelta.x, buildMenuRect.sizeDelta.y);
            }

            Building selectedBuilding = _selectionManager != null ? _selectionManager.SelectedBuilding : null;
            bool hasUnitSelection = _selectionManager != null && _selectionManager.Selected.Count > 0;
            bool hasSelection = hasUnitSelection || selectedBuilding != null;
            panelRoot.SetActive(hasSelection);
            if (!hasSelection)
            {
                _groupIconRoot?.SetActive(false);
                return;
            }

            if (selectedBuilding != null)
            {
                nameLabel.gameObject.SetActive(true);
                statusLabel.gameObject.SetActive(true);
                groupCountLabel.gameObject.SetActive(false);
                _groupIconRoot?.SetActive(false);
                SetPortrait(null);
                DrawBuilding(selectedBuilding);
                return;
            }

            bool single = _selectionManager.Selected.Count == 1;
            nameLabel.gameObject.SetActive(single);
            statusLabel.gameObject.SetActive(single);
            groupCountLabel.gameObject.SetActive(!single);

            if (single)
            {
                _groupIconRoot?.SetActive(false);
                DrawSingle(_selectionManager.Selected[0]);
            }
            else
            {
                groupCountLabel.text = $"{_selectionManager.Selected.Count} units selected";
                // DrawSingle/DrawBuilding are the only two places that
                // toggle hpLabel/the HP bar, so a group selection (neither
                // one) left them showing whatever the PREVIOUS selection
                // last set - a stale HP bar/number from an earlier single
                // unit or building, not anything about the current group.
                // Confirmed live: selecting a TownCenter then a group of
                // workers left "HP: 500/500" and a full bar rendering
                // behind "4 units selected".
                hpLabel.gameObject.SetActive(false);
                SetHealthBar(false, null);
                SetPortrait(null);
                RefreshGroupIconRow(_selectionManager.Selected);
            }
        }

        private void DrawSingle(Unit unit)
        {
            nameLabel.text = unit.gameObject.name;
            statusLabel.text = UnitStatus.Describe(unit);
            SetPortrait(unit.IconKey);
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

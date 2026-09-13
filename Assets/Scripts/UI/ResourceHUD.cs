using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.UI
{
    // Always-on Wood/Food/Gold/Stone/Population/Civilization/Age panel,
    // top-left corner, laid out as ONE HORIZONTAL ROW (icon+number pairs
    // left to right, matching the user's AoE reference screenshots - a
    // vertically-stacked column doesn't match either reference). Hardcoded
    // to the Player's stockpile specifically (not a generic lookup) so it
    // can never accidentally end up displaying the AI's economy. uGUI/TMP
    // replacement for the original OnGUI version - the labels are real
    // Canvas children wired up in the Inspector, this computes their
    // horizontal positions in code every time it runs (rather than trusting
    // fixed scene-authored positions) since the row's total width already
    // depends on which items exist.
    //
    // 2026-09-12: Civilization/Population/Age used to live in their own
    // separate MatchStatus panel down in the bottom-docked bar (Roadmap
    // item 30) - folded back into this top-left panel, at the user's
    // explicit direction, to match their AoE reference screenshots (neither
    // shows a separate civ/pop/age box; both show them merged into the top
    // resource strip). civLabel/populationLabel/ageLabel are now scene
    // children of this same GameObject, one shared background.
    public class ResourceHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text civLabel;
        [SerializeField] private TMP_Text woodLabel;
        [SerializeField] private TMP_Text foodLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text stoneLabel;
        [SerializeField] private TMP_Text populationLabel;
        [SerializeField] private TMP_Text ageLabel;

        private const float RowY = -11f;
        private const float RowHeight = 22f;
        private const float IconSize = 18f;
        private const float NumberWidth = 44f;
        private const float ItemGap = 14f;
        private const float LeftMargin = 10f;
        private const float TopMargin = 10f;
        private const float BottomMargin = 10f;

        // Item 32 (Age/research always-visible readout): a second row,
        // built in code like the resource icons above rather than wired in
        // the scene, that only appears while a TownCenter is aging up -
        // AoE's own "torch" readout, scoped to Age-up specifically (the
        // one research every player is always tracking) rather than
        // surfacing every concurrent tier/upgrade research track this
        // project supports (those stay visible on their own building's
        // selection UI, same as before this item).
        private const float ResearchGap = 6f;
        private const float ResearchLabelHeight = 18f;
        private const float ResearchBarHeight = 10f;

        private RectTransform _panelRect;
        private GameObject _researchRow;
        private TMP_Text _researchLabel;
        private Image _researchFill;

        private void Awake()
        {
            // The root already carries its own background Image (a flat
            // dark backdrop authored in the scene) - swap in the dedicated
            // resource-bar frame art rather than the shared modal frame
            // (see SelectedUnitPanel.cs for why panels each get their own).
            Sprite frame = Resources.Load<Sprite>("UI/Panels/panel_resource_bar");
            RectTransform panelRect = (RectTransform)transform;
            if (TryGetComponent(out Image background) && frame != null)
            {
                background.color = Color.white;
                background.sprite = frame;
                background.type = Image.Type.Sliced;
                // 2026-09-12 Tier 1 art delivery: source art is 1776x578
                // (spriteBorder 180/120/180/200) - the border needs a
                // multiplier to shrink to a readable screen-pixel thickness
                // (displayed border = source border / multiplier). Retuned
                // for this row's own ~44-tall display rect (was 12.5,
                // tuned for the old 200x120 vertical-stack panel).
                background.pixelsPerUnitMultiplier = 40f;
            }

            float x = LeftMargin;
            x = LayoutResourceItem(woodLabel, "resource_wood", x);
            x = LayoutResourceItem(foodLabel, "resource_food", x);
            x = LayoutResourceItem(goldLabel, "resource_gold", x);
            x = LayoutResourceItem(stoneLabel, "resource_stone", x);
            x = LayoutResourceItem(populationLabel, "resource_population", x);
            x = LayoutTextItem(civLabel, x, 170f);
            x = LayoutTextItem(ageLabel, x, 110f);

            float panelWidth = x - ItemGap + LeftMargin;
            _panelRect = panelRect;
            panelRect.sizeDelta = new Vector2(panelWidth, PanelHeight(researching: false));

            SetUpResearchRow(panelWidth - LeftMargin * 2f);

            // "Civilization: Vijayanagara" (the longest civ name) measures
            // wider than civLabel's own box at a fixed font size, and with
            // word-wrap on (TMP's own default) that pushed a 2nd line down
            // past this single-row layout - confirmed live, not assumed.
            // Auto-sizing shrinks the font just enough to keep every civ
            // name on one line instead of wrapping; short names (Chola,
            // Maratha, ...) render unaffected at the max size.
            ConfigureSingleLineAutoSize(civLabel);
            ConfigureSingleLineAutoSize(ageLabel);
        }

        private static void ConfigureSingleLineAutoSize(TMP_Text label)
        {
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = label.fontSize;
        }

        // Places a small icon at x followed by a fixed-width number box
        // immediately to its right, and returns the x for the NEXT item
        // (icon + number + gap). Text itself is just the number (no
        // "Wood: " prefix) - matches the reference screenshots, which show
        // only an icon and a value for each resource, and keeps the row's
        // total width sane regardless of how large a stockpile gets.
        private static float LayoutResourceItem(TMP_Text label, string iconName, float x)
        {
            Sprite icon = Resources.Load<Sprite>("UI/Icons/" + iconName);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            float textX = x;

            if (icon != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(labelRect.parent, false);
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.sizeDelta = new Vector2(IconSize, IconSize);
                iconRect.anchoredPosition = new Vector2(x, RowY - (RowHeight - IconSize) / 2f);
                iconGo.GetComponent<Image>().sprite = icon;
                textX = x + IconSize + 4f;
            }

            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(NumberWidth, RowHeight);
            labelRect.anchoredPosition = new Vector2(textX, RowY);

            return textX + NumberWidth + ItemGap;
        }

        // Civilization/Age have no matching icon art - a plain text box of
        // the given width, same row, same left-aligned convention.
        private static float LayoutTextItem(TMP_Text label, float x, float width)
        {
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(width, RowHeight);
            labelRect.anchoredPosition = new Vector2(x, RowY);
            return x + width + ItemGap;
        }

        private static float PanelHeight(bool researching)
        {
            float baseHeight = TopMargin + RowHeight + BottomMargin;
            if (!researching)
            {
                return baseHeight;
            }

            return baseHeight + ResearchGap + ResearchLabelHeight + ResearchBarHeight;
        }

        // Builds the Age-up readout row (label + fill bar), a sibling row
        // below the main resource strip, hidden until Update() finds a
        // faction TownCenter actually aging up. Reuses SelectedUnitPanel's
        // own hp_bar_frame/hp_bar_fill art (the only 9-slice progress-bar
        // asset in the project) rather than sourcing new art for a second
        // bar style.
        private void SetUpResearchRow(float rowWidth)
        {
            _researchRow = new GameObject("ResearchRow", typeof(RectTransform));
            RectTransform rowRect = _researchRow.GetComponent<RectTransform>();
            rowRect.SetParent(transform, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(LeftMargin, -(TopMargin + RowHeight + ResearchGap));
            rowRect.sizeDelta = new Vector2(rowWidth, ResearchLabelHeight + ResearchBarHeight);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(rowRect, false);
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(rowWidth, ResearchLabelHeight);
            _researchLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _researchLabel.fontSize = 14f;
            _researchLabel.color = Color.white;
            _researchLabel.raycastTarget = false;
            _researchLabel.enableWordWrapping = false;

            Sprite frameSprite = Resources.Load<Sprite>("UI/Panels/hp_bar_frame");
            Sprite fillSprite = Resources.Load<Sprite>("UI/Panels/hp_bar_fill");
            if (frameSprite == null || fillSprite == null)
            {
                _researchRow.SetActive(false);
                return;
            }

            GameObject barGo = new GameObject("Bar", typeof(RectTransform));
            RectTransform barRect = barGo.GetComponent<RectTransform>();
            barRect.SetParent(rowRect, false);
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -ResearchLabelHeight);
            barRect.sizeDelta = new Vector2(rowWidth, ResearchBarHeight);

            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            RectTransform frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.SetParent(barRect, false);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            Image frameImage = frameGo.GetComponent<Image>();
            frameImage.sprite = frameSprite;
            frameImage.type = Image.Type.Sliced;
            // Multiplier = the sprite's own native height / this bar's
            // display height, same formula SelectedUnitPanel.SetUpHealthBar
            // uses for its own (taller) HP bar - computed from the sprite's
            // real size rather than a value copied from that other display
            // height, since this bar is a different height.
            frameImage.pixelsPerUnitMultiplier = frameSprite.rect.height / ResearchBarHeight;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.SetParent(barRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _researchFill = fillGo.GetComponent<Image>();
            _researchFill.sprite = fillSprite;
            _researchFill.type = Image.Type.Filled;
            _researchFill.fillMethod = Image.FillMethod.Horizontal;
            _researchFill.fillAmount = 0f;

            _researchRow.SetActive(false);
        }

        private void Update()
        {
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);
            if (stockpile == null)
            {
                return;
            }

            string civName = CivilizationProfile.For(CivilizationRegistry.For(FactionId.Player)).DisplayName;
            string ageName = AgeProfile.For(AgeProgress.CurrentAge(FactionId.Player)).DisplayName;

            civLabel.text = $"Civilization: {civName}";
            woodLabel.text = $"{(int)stockpile.GetTotal(ResourceType.Wood)}";
            foodLabel.text = $"{(int)stockpile.GetTotal(ResourceType.Food)}";
            goldLabel.text = $"{(int)stockpile.GetTotal(ResourceType.Gold)}";
            stoneLabel.text = $"{(int)stockpile.GetTotal(ResourceType.Stone)}";
            populationLabel.text = $"{Population.Current(FactionId.Player)}/{Population.Cap(FactionId.Player)}";
            ageLabel.text = $"Age: {ageName}";

            UpdateResearchRow();
        }

        private void UpdateResearchRow()
        {
            if (_researchRow == null)
            {
                return;
            }

            TownCenter agingUp = TownCenter.FindAgingUp(FactionId.Player);
            bool researching = agingUp != null;
            _researchRow.SetActive(researching);
            if (_panelRect != null)
            {
                _panelRect.sizeDelta = new Vector2(_panelRect.sizeDelta.x, PanelHeight(researching));
            }

            if (!researching)
            {
                return;
            }

            string targetAgeName = AgeProfile.For(agingUp.AgeUpTarget).DisplayName;
            int percent = Mathf.RoundToInt(agingUp.AgeUpProgress * 100f);
            _researchLabel.text = $"Researching: {targetAgeName} ({percent}%)";
            if (_researchFill != null)
            {
                _researchFill.fillAmount = agingUp.AgeUpProgress;
            }
        }
    }
}

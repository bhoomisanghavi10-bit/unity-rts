using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.UI
{
    // Always-on Wood/Food/Gold/Stone/Civilization/Population/Age panel,
    // top-left corner. Hardcoded to the Player's stockpile specifically
    // (not a generic lookup) so it can never accidentally end up displaying
    // the AI's economy. uGUI/TMP replacement for the original OnGUI version
    // - the labels are real Canvas children wired up in the Inspector, this
    // just pushes text into them every frame instead of issuing GUI.Label
    // draw calls.
    //
    // 2026-09-12: Civilization/Population/Age used to live in their own
    // separate MatchStatus panel down in the bottom-docked bar (Roadmap
    // item 30) - folded back into this top-left panel as rows 5-7, at the
    // user's explicit direction, to match their AoE reference screenshots
    // (neither shows a separate civ/pop/age box; both show them merged
    // into the top resource strip). civLabel/populationLabel/ageLabel are
    // now scene children of this same GameObject, one shared background.
    public class ResourceHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text civLabel;
        [SerializeField] private TMP_Text woodLabel;
        [SerializeField] private TMP_Text foodLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text stoneLabel;
        [SerializeField] private TMP_Text populationLabel;
        [SerializeField] private TMP_Text ageLabel;

        private void Awake()
        {
            // The root already carries its own background Image (a flat
            // dark backdrop authored in the scene) - swap in the dedicated
            // resource-bar frame art rather than the shared modal frame
            // (see SelectedUnitPanel.cs for why panels each get their own).
            Sprite frame = Resources.Load<Sprite>("UI/Panels/panel_resource_bar");
            if (TryGetComponent(out Image background) && frame != null)
            {
                background.color = Color.white;
                background.sprite = frame;
                background.type = Image.Type.Sliced;
                // 2026-09-12 Tier 1 art delivery: source art is 1776x578
                // (spriteBorder 180/120/180/200), much larger than this
                // panel's 200x120 display rect, so the border needs a
                // multiplier to shrink to a readable screen-pixel thickness
                // (displayed border = source border / multiplier).
                background.pixelsPerUnitMultiplier = 12.5f;
            }

            AddResourceIcon(woodLabel, "resource_wood");
            AddResourceIcon(foodLabel, "resource_food");
            AddResourceIcon(goldLabel, "resource_gold");
            AddResourceIcon(stoneLabel, "resource_stone");
            AddResourceIcon(populationLabel, "resource_population");

            // "Civilization: Vijayanagara" (the longest civ name) measures
            // wider than civLabel's 184-unit box at a fixed font size, and
            // with word-wrap on (the label's own default) that pushed a
            // 2nd line down into the populationLabel row directly below -
            // confirmed live, not assumed. Auto-sizing shrinks the font
            // just enough to keep every civ name on one line instead,
            // rather than wrapping into the row below it; short names
            // (Chola, Maratha, ...) render unaffected at the max size.
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

        // Adds a small icon to the left of a resource label and shifts the
        // label right by the same amount, keeping its right edge fixed -
        // civLabel/ageLabel have no matching icon asset and are left
        // untouched.
        private static void AddResourceIcon(TMP_Text label, string iconName)
        {
            Sprite icon = Resources.Load<Sprite>("UI/Icons/" + iconName);
            if (icon == null)
            {
                return;
            }

            RectTransform labelRect = label.GetComponent<RectTransform>();
            const float iconWidth = 22f;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(labelRect.parent, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = labelRect.anchorMin;
            iconRect.anchorMax = labelRect.anchorMax;
            iconRect.pivot = labelRect.pivot;
            iconRect.sizeDelta = new Vector2(18f, 18f);
            iconRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, labelRect.anchoredPosition.y - 2f);
            iconGo.transform.SetSiblingIndex(labelRect.GetSiblingIndex());
            iconGo.GetComponent<Image>().sprite = icon;

            labelRect.anchoredPosition += new Vector2(iconWidth, 0f);
            labelRect.sizeDelta -= new Vector2(iconWidth, 0f);
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
            woodLabel.text = $"Wood: {(int)stockpile.GetTotal(ResourceType.Wood)}";
            foodLabel.text = $"Food: {(int)stockpile.GetTotal(ResourceType.Food)}";
            goldLabel.text = $"Gold: {(int)stockpile.GetTotal(ResourceType.Gold)}";
            stoneLabel.text = $"Stone: {(int)stockpile.GetTotal(ResourceType.Stone)}";
            populationLabel.text = $"Population: {Population.Current(FactionId.Player)}/{Population.Cap(FactionId.Player)}";
            ageLabel.text = $"Age: {ageName}";
        }
    }
}

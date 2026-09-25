using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.UI
{
    // Pre-match, full-screen blocking overlay: pick a civilization, then
    // Confirm releases the actual match content (CivilizationSetup.
    // BeginMatch activates every gated spawner/AI GameObject). Three cards
    // (Chola/Vijayanagara/Rajput) act like radio buttons - clicking one
    // selects it and deselects the others; Confirm is disabled until one
    // is picked. Destroys itself (and its blocking full-screen background)
    // once the match starts, rather than just hiding, since there's no
    // reason to ever show it again this session.
    public class CivPicker : MonoBehaviour
    {
        [SerializeField] private CivilizationSetup civilizationSetup;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private CivilizationId[] cardCivilizations;
        [SerializeField] private Image[] cardBackgrounds;
        [SerializeField] private Button confirmButton;

        private CivilizationId? _selected;

        // Every MapId, with a friendly display name - index 0 (RiverValley)
        // is the default, so a player who never touches this row gets
        // exactly today's behavior. Built here rather than reading
        // CivilizationSetup's own Inspector default, since that field is
        // private and this row should always start at the same place
        // regardless of what a given scene happens to have wired.
        private static readonly (MapId Id, string Label)[] MapChoices =
        {
            (MapId.RiverValley, "River Valley"),
            (MapId.Highlands, "Highlands"),
            (MapId.Coastal, "Coastal"),
            (MapId.SkirmishMedium, "Crossroad Valleys"),
            (MapId.SkirmishDividedRiverbed, "Divided Riverbed"),
            (MapId.SkirmishMountainPass, "Mountain Pass"),
            (MapId.SkirmishHighlandFoothills, "Highland Foothills"),
            (MapId.SkirmishClearing, "Clearing"),
            (MapId.SkirmishSmall, "Crossroad Valleys (Small)"),
            (MapId.SkirmishLarge, "Crossroad Valleys (Large)"),
        };

        private int _mapIndex;
        private TMP_Text _mapLabel;

        private void Awake()
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                CivilizationId civ = cardCivilizations[i];
                cardButtons[i].onClick.AddListener(() => Select(civ));
                AddCrest(cardButtons[i], civ);
            }

            confirmButton.onClick.AddListener(Confirm);
            confirmButton.interactable = false;
            RefreshCardVisuals();

            BuildMapRow();
        }

        // A small "< Map Name >" row built entirely in code (not scene-
        // wired) - avoids this project's own recurring "new SerializeField
        // null in the scene" gotcha for a control added after the scene
        // was authored. Sits in the gap between the civ cards' bottom edge
        // and the Confirm button, centred like the cards themselves.
        private void BuildMapRow()
        {
            const float rowY = -167f;
            const float rowHeight = 36f;

            Button prev = CreateArrowButton("MapPrevButton", "<", new Vector2(-140f, rowY));
            prev.onClick.AddListener(() => CycleMap(-1));

            GameObject labelGo = new GameObject("MapLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(220f, rowHeight);
            labelRect.anchoredPosition = new Vector2(0f, rowY);
            _mapLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _mapLabel.alignment = TextAlignmentOptions.Center;
            _mapLabel.fontSize = 20f;
            _mapLabel.color = Color.white;

            Button next = CreateArrowButton("MapNextButton", ">", new Vector2(140f, rowY));
            next.onClick.AddListener(() => CycleMap(1));

            RefreshMapLabel();
        }

        private Button CreateArrowButton(string name, string glyph, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(36f, 36f);
            rect.anchoredPosition = anchoredPosition;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = glyph;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 24f;

            return go.GetComponent<Button>();
        }

        private void CycleMap(int direction)
        {
            _mapIndex = (_mapIndex + direction + MapChoices.Length) % MapChoices.Length;
            RefreshMapLabel();
        }

        private void RefreshMapLabel()
        {
            _mapLabel.text = MapChoices[_mapIndex].Label;
        }

        // Each card has a top-anchored "Name" strip and a "Blurb" that
        // stretches to fill nearly the whole card underneath it - carves out
        // a 64px strip above both for the crest by shifting Name down and
        // insetting Blurb's top edge by the same amount, rather than just
        // overlaying the crest on top of existing content.
        private static void AddCrest(Button card, CivilizationId civId)
        {
            Sprite crest = Resources.Load<Sprite>("UI/Menu/crest_" + civId.ToString().ToLowerInvariant());
            if (crest == null)
            {
                return;
            }

            const float crestSpace = 64f;

            Transform name = card.transform.Find("Name");
            if (name != null && name.TryGetComponent(out RectTransform nameRect))
            {
                nameRect.anchoredPosition -= new Vector2(0f, crestSpace);
            }

            Transform blurb = card.transform.Find("Blurb");
            if (blurb != null && blurb.TryGetComponent(out RectTransform blurbRect))
            {
                Vector2 offsetMax = blurbRect.offsetMax;
                offsetMax.y -= crestSpace;
                blurbRect.offsetMax = offsetMax;
            }

            GameObject crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            crestGo.transform.SetParent(card.transform, false);
            RectTransform crestRect = crestGo.GetComponent<RectTransform>();
            crestRect.anchorMin = new Vector2(0.5f, 1f);
            crestRect.anchorMax = new Vector2(0.5f, 1f);
            crestRect.pivot = new Vector2(0.5f, 1f);
            crestRect.sizeDelta = new Vector2(64f, 64f);
            crestRect.anchoredPosition = new Vector2(0f, 0f);
            Image crestImage = crestGo.GetComponent<Image>();
            crestImage.sprite = crest;
            // Real per-civ crest art isn't perfectly square after alpha-
            // keying/content-cropping (e.g. Chola's is 1099x1213) - without
            // this, a fixed 64x64 box would stretch it off-aspect.
            crestImage.preserveAspect = true;
        }

        private void Select(CivilizationId civ)
        {
            _selected = civ;
            confirmButton.interactable = true;
            RefreshCardVisuals();
        }

        private void RefreshCardVisuals()
        {
            for (int i = 0; i < cardBackgrounds.Length; i++)
            {
                bool isSelected = _selected == cardCivilizations[i];
                CivilizationProfile profile = CivilizationProfile.For(cardCivilizations[i]);
                cardBackgrounds[i].color = isSelected
                    ? profile.PrimaryColor
                    : Color.Lerp(profile.PrimaryColor, Color.black, 0.65f);
            }
        }

        private void Confirm()
        {
            if (!_selected.HasValue)
            {
                return;
            }

            civilizationSetup.SetMap(MapChoices[_mapIndex].Id);
            civilizationSetup.BeginMatch(_selected.Value);
            Destroy(gameObject);
        }
    }
}

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
            crestGo.GetComponent<Image>().sprite = crest;
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

            civilizationSetup.BeginMatch(_selected.Value);
            Destroy(gameObject);
        }
    }
}

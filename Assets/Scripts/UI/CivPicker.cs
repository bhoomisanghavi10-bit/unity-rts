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
            }

            confirmButton.onClick.AddListener(Confirm);
            confirmButton.interactable = false;
            RefreshCardVisuals();
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

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomsOfBharat.UI
{
    // Roadmap item 31: added to every BuildMenu grid-cell button by
    // SetupGridCell. Reads Source.text fresh on every pointer-enter rather
    // than caching it, since the same label keeps being overwritten every
    // frame by whichever Update* method owns that button's dynamic text
    // (cost/progress-percent/age-gate) - this component never computes or
    // stores that text itself, only relays it to ButtonTooltip on hover.
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text Source { get; set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Source != null && ButtonTooltip.Instance != null)
            {
                ButtonTooltip.Instance.Show(Source.text);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ButtonTooltip.Instance != null)
            {
                ButtonTooltip.Instance.Hide();
            }
        }

        private void OnDisable()
        {
            if (ButtonTooltip.Instance != null)
            {
                ButtonTooltip.Instance.Hide();
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomsOfBharat.UI
{
    // Roadmap item 31: a single Canvas-level tooltip for UI widgets,
    // triggered by TooltipTrigger's pointer-enter/exit events - deliberately
    // a separate class from HoverTooltip.cs rather than merged into it,
    // since that one is driven by a 3D physics raycast against world
    // objects and has a fundamentally different trigger mechanism (same
    // "keep topically-related but mechanically-different systems separate"
    // precedent as CombatBonus/CounterMatrix). Follows the mouse the same
    // way HoverTooltip.cs's own panelRoot does - a Screen Space Overlay
    // canvas child's transform.position IS screen pixels, so no coordinate
    // conversion is needed.
    public class ButtonTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TMP_Text label;

        public static ButtonTooltip Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            if (panelRoot != null && panelRoot.TryGetComponent(out Image background))
            {
                background.color = new Color(0f, 0f, 0f, 0.85f);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string text)
        {
            if (panelRoot == null || label == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            label.text = text;
            panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.gameObject.activeSelf)
            {
                panelRoot.position = new Vector3(Input.mousePosition.x + 16f, Input.mousePosition.y - 16f, 0f);
            }
        }
    }
}

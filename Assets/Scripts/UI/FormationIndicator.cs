using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.UI
{
    // Phase 6 gap-close: the only feedback a player had for which
    // formation was active was none at all - SelectionManager tracked
    // _currentFormation, but nothing showed it. A small always-visible
    // corner label, code-generated at runtime (same pattern as
    // ObjectivePanel/SettingsMenu/MissionToast - never touches Main.unity)
    // rather than a full formation-picker UI, matching the "cheapest
    // realistic version that's still a real player-facing choice" framing
    // this item was scoped around.
    public class FormationIndicator : MonoBehaviour
    {
        private TMP_Text _text;
        private SelectionManager _selectionManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<FormationIndicator>() != null)
            {
                return;
            }

            var go = new GameObject("FormationIndicator");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<FormationIndicator>();
        }

        private void Awake()
        {
            BuildUi();
        }

        private void Update()
        {
            if (_selectionManager == null)
            {
                _selectionManager = FindFirstObjectByType<SelectionManager>();
                if (_selectionManager == null)
                {
                    return;
                }
            }

            string composedSuffix = _selectionManager.IsComposedFormationActive
                ? $" | {_selectionManager.ComposedFormationName} ON (C to toggle off)"
                : " (C to toggle composed formation)";
            _text.text = $"Formation: {_selectionManager.CurrentFormation} (R to cycle){composedSuffix}";
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("FormationIndicatorCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 16f;
            _text.color = Color.white;
            _text.alignment = TextAlignmentOptions.TopLeft;

            RectTransform rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // ResourceHUD occupies the same top-left corner at (8,-8) with a
            // 200x190 footprint - anchoring here too used to render both
            // texts on top of each other. Sitting just below it instead.
            rt.anchoredPosition = new Vector2(8f, -206f);
            rt.sizeDelta = new Vector2(300f, 30f);
        }
    }
}

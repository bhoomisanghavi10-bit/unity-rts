using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.UI
{
    // Item 48: lets the player see and change relations with every other
    // faction - unilateral for now (no AI negotiation/acceptance, the
    // same simplification this project has shipped for other first-pass
    // systems: AiDifficulty is Inspector-only, Market's AI wiring was
    // deferred, etc.). Fully code-generated at runtime, same pattern as
    // SettingsMenu.cs, so it never touches the shared Main.unity scene
    // file. F11 (itself rebindable via GameSettings) opens it.
    public class DiplomacyMenu : MonoBehaviour
    {
        private static readonly FactionId[] OtherFactions = { FactionId.Enemy, FactionId.Enemy2 };
        private const string ToggleActionId = "ToggleDiplomacy";
        private static readonly KeyCode ToggleDefault = KeyCode.F11;

        private GameObject _panel;
        private readonly TMP_Text[] _relationTexts = new TMP_Text[OtherFactions.Length];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("DiplomacyMenu");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DiplomacyMenu>();
        }

        private void Awake()
        {
            BuildUi();
            _panel.SetActive(false);
        }

        private void Update()
        {
            KeyCode toggleKey = GameSettings.GetKey(ToggleActionId, ToggleDefault);
            if (Input.GetKeyDown(toggleKey))
            {
                _panel.SetActive(!_panel.activeSelf);
                if (_panel.activeSelf)
                {
                    RefreshDisplayedValues();
                }
            }
        }

        private void ToggleAlliance(FactionId other)
        {
            bool nowAllied = !DiplomacyRegistry.AreAllied(FactionId.Player, other);
            DiplomacyRegistry.SetAllied(FactionId.Player, other, nowAllied);
            RefreshDisplayedValues();
        }

        private void RefreshDisplayedValues()
        {
            for (int i = 0; i < OtherFactions.Length; i++)
            {
                bool allied = DiplomacyRegistry.AreAllied(FactionId.Player, OtherFactions[i]);
                _relationTexts[i].text = allied ? "Allied" : "War";
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("DiplomacyMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190; // just under SettingsMenu (200), above match HUD
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var backdropImage = _panel.AddComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.6f);
            var backdropRect = _panel.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            var boxGo = new GameObject("Box");
            boxGo.transform.SetParent(_panel.transform, false);
            var boxImage = boxGo.AddComponent<Image>();
            boxImage.color = new Color(0.12f, 0.12f, 0.14f, 0.97f);
            var boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(460f, 260f);
            boxRect.anchoredPosition = Vector2.zero;

            float y = 90f;
            CreateLabel(boxGo.transform, "Diplomacy", new Vector2(0f, y), 26, TextAlignmentOptions.Center);
            y -= 50f;

            for (int i = 0; i < OtherFactions.Length; i++)
            {
                FactionId other = OtherFactions[i];
                CreateLabel(boxGo.transform, other.ToString(), new Vector2(-140f, y), 17, TextAlignmentOptions.Left);
                _relationTexts[i] = CreateButton(boxGo.transform, "", new Vector2(110f, y), new Vector2(160f, 32f),
                    () => ToggleAlliance(other));
                y -= 44f;
            }

            y -= 10f;
            CreateButton(boxGo.transform, "Close", new Vector2(0f, y), new Vector2(140f, 34f),
                () => _panel.SetActive(false));

            RefreshDisplayedValues();
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 30f);
            rect.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            return tmp;
        }

        private static TMP_Text CreateButton(Transform parent, string text, Vector2 position, Vector2 size, System.Action onClick)
        {
            var go = new GameObject("Button_" + (string.IsNullOrEmpty(text) ? "Value" : text));
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}

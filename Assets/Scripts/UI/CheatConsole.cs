using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.UI
{
    // Wave 6 item 39 (Cheat codes): a Quake-style typed-command console,
    // "genuinely useful for testing your own scenarios" per the roadmap's
    // own one-line spec. Same self-bootstrapping runtime-built-Canvas +
    // GameSettings-driven-hotkey pattern as SettingsMenu/HotkeyOverlay;
    // reuses ScenarioEditorMenu's own CreateInputField shape (the only
    // TMP_InputField precedent in this project) rather than inventing a
    // second one.
    //
    // Deliberately refuses to execute anything while NetworkMatch.IsActive
    // - every cheat mutates state outside CommandBus (see CheatCodes.cs's
    // own header comment), which would desync a real LAN match the instant
    // one peer typed a command the other never saw.
    public class CheatConsole : MonoBehaviour
    {
        private const string ToggleActionId = "ToggleCheatConsole";
        private static readonly KeyCode ToggleDefault = KeyCode.BackQuote;

        private GameObject _panel;
        private TMP_InputField _inputField;
        private TMP_Text _feedbackLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("CheatConsole");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<CheatConsole>();
        }

        private void Awake()
        {
            BuildUi();
            _panel.SetActive(false);
        }

        private void Update()
        {
            if (_panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                _panel.SetActive(false);
                return;
            }

            KeyCode toggleKey = GameSettings.GetKey(ToggleActionId, ToggleDefault);
            if (Input.GetKeyDown(toggleKey))
            {
                bool open = !_panel.activeSelf;
                _panel.SetActive(open);
                if (open)
                {
                    _feedbackLabel.text = NetworkMatch.IsActive
                        ? "Cheats are disabled during a LAN multiplayer match."
                        : "Type a command and press Enter. Try 'help'.";
                    _inputField.text = string.Empty;
                    EventSystem.current?.SetSelectedGameObject(_inputField.gameObject);
                    _inputField.ActivateInputField();
                }
            }
        }

        private void OnSubmit(string rawInput)
        {
            if (!_panel.activeSelf)
            {
                return;
            }

            _inputField.text = string.Empty;
            EventSystem.current?.SetSelectedGameObject(_inputField.gameObject);
            _inputField.ActivateInputField();

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return;
            }

            if (NetworkMatch.IsActive)
            {
                _feedbackLabel.text = "Cheats are disabled during a LAN multiplayer match.";
                return;
            }

            CheatCommand command = CheatCommandParser.Parse(rawInput);
            _feedbackLabel.text = CheatCodes.Execute(command);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("CheatConsoleCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // same modal layer as SettingsMenu/HotkeyOverlay
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(560f, 90f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            var panelImage = _panel.AddComponent<Image>();
            UIStyleTheme.Current.ApplyPanel(panelImage);

            _feedbackLabel = CreateLabel(_panel.transform, string.Empty, new Vector2(0f, -20f));

            _inputField = CreateInputField(_panel.transform, new Vector2(0f, 12f));
            _inputField.onSubmit.AddListener(OnSubmit);
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position)
        {
            var go = new GameObject("Feedback");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(520f, 26f);
            rect.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        // Same shape as ScenarioEditorMenu.CreateInputField (the project's
        // only other TMP_InputField), inlined rather than shared across
        // asmdef-visible files since it's a private UI helper on both
        // sides, not a public API.
        private static TMP_InputField CreateInputField(Transform parent, Vector2 position)
        {
            var go = new GameObject("CommandInput");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(520f, 32f);
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            UIStyleTheme.Current.ApplyButton(image);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(8f, 2f);
            placeholderRect.offsetMax = new Vector2(-8f, -2f);
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "Cheat command...";
            placeholderTmp.fontSize = 14;
            placeholderTmp.color = UIStyleTheme.Current.TextSecondary;
            placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderTmp.fontStyle = FontStyles.Italic;

            var field = go.AddComponent<TMP_InputField>();
            field.textViewport = rect;
            field.textComponent = tmp;
            field.placeholder = placeholderTmp;
            return field;
        }
    }
}

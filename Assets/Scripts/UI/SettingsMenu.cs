using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.AI;

namespace KingdomsOfBharat.UI
{
    // Item 46: accessibility/settings menu - difficulty, colorblind mode,
    // and key rebinding, all persisted via GameSettings (PlayerPrefs).
    // Fully self-constructed at runtime (same established pattern as
    // HealthBar/MinimapController's code-generated Canvases) rather than
    // authored in Main.unity, so this never touches the shared scene file
    // - just one RuntimeInitializeOnLoadMethod bootstrapping its own
    // DontDestroyOnLoad GameObject, independent of CivilizationSetup's
    // gated match content (a player should be able to open Settings before
    // a match even starts).
    public class SettingsMenu : MonoBehaviour
    {
        private struct RebindableAction
        {
            public string Id;
            public string Label;
            public KeyCode Default;

            public RebindableAction(string id, string label, KeyCode defaultKey)
            {
                Id = id;
                Label = label;
                Default = defaultKey;
            }
        }

        private static readonly RebindableAction[] Actions =
        {
            new RebindableAction("CycleStance", "Cycle Stance", KeyCode.V),
            new RebindableAction("TrainWorker", "Train Worker (Town Center)", KeyCode.G),
            new RebindableAction("TrainUnit", "Train Unit (Barracks)", KeyCode.T),
            new RebindableAction("PlaceBarracks", "Place Barracks", KeyCode.B),
            new RebindableAction("PlaceFarm", "Place Farm", KeyCode.F),
            new RebindableAction("PlaceHouse", "Place House", KeyCode.H),
            new RebindableAction("PlaceWall", "Place Wall", KeyCode.L),
            new RebindableAction("PlaceGate", "Place Gate", KeyCode.K),
            new RebindableAction("PlaceTower", "Place Tower", KeyCode.O),
            new RebindableAction("PlaceMarket", "Place Market", KeyCode.M),
        };

        private const string ToggleActionId = "ToggleSettings";
        private static readonly KeyCode ToggleDefault = KeyCode.F10;

        private GameObject _panel;
        private TMP_Text _difficultyValueText;
        private TMP_Text _colorblindValueText;
        private readonly TMP_Text[] _keyButtonTexts = new TMP_Text[Actions.Length];
        private string _rebindingActionId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("SettingsMenu");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SettingsMenu>();
        }

        private void Awake()
        {
            BuildUi();
            _panel.SetActive(false);
        }

        private void Update()
        {
            if (_rebindingActionId != null)
            {
                TickRebind();
                return;
            }

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

        // Scans every KeyCode rather than a fixed shortlist so any key the
        // player wants (including ones no default binding uses) can be
        // picked - cheap enough since this only runs while the rebind
        // button is actively waiting for input, not every frame.
        private void TickRebind()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _rebindingActionId = null;
                RefreshDisplayedValues();
                return;
            }

            foreach (KeyCode code in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (code == KeyCode.Escape || code == KeyCode.None)
                {
                    continue;
                }

                if (Input.GetKeyDown(code))
                {
                    GameSettings.SetKey(_rebindingActionId, code);
                    GameSettings.Save();
                    _rebindingActionId = null;
                    RefreshDisplayedValues();
                    return;
                }
            }
        }

        private void RefreshDisplayedValues()
        {
            _difficultyValueText.text = GameSettings.Difficulty.ToString();
            _colorblindValueText.text = GameSettings.ColorblindMode ? "On" : "Off";

            for (int i = 0; i < Actions.Length; i++)
            {
                bool isRebindingThis = _rebindingActionId == Actions[i].Id;
                _keyButtonTexts[i].text = isRebindingThis
                    ? "Press a key..."
                    : GameSettings.GetKey(Actions[i].Id, Actions[i].Default).ToString();
            }
        }

        private void CycleDifficulty()
        {
            AiDifficulty next = GameSettings.Difficulty switch
            {
                AiDifficulty.Easy => AiDifficulty.Normal,
                AiDifficulty.Normal => AiDifficulty.Hard,
                _ => AiDifficulty.Easy,
            };
            GameSettings.Difficulty = next;
            GameSettings.Save();
            RefreshDisplayedValues();
        }

        private void ToggleColorblind()
        {
            GameSettings.ColorblindMode = !GameSettings.ColorblindMode;
            GameSettings.Save();
            RefreshDisplayedValues();
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("SettingsMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // above BuildMenu/CivPicker/SelectedUnitPanel
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-screen dim backdrop, also eats clicks so the game world
            // underneath doesn't receive them while the menu is open.
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
            boxRect.sizeDelta = new Vector2(520f, 620f);
            boxRect.anchoredPosition = Vector2.zero;

            float y = 270f;
            CreateLabel(boxGo.transform, "Settings", new Vector2(0f, y), 28, TextAlignmentOptions.Center);
            y -= 50f;

            CreateLabel(boxGo.transform, "Difficulty", new Vector2(-140f, y), 18, TextAlignmentOptions.Left);
            _difficultyValueText = CreateButton(boxGo.transform, "", new Vector2(120f, y), new Vector2(160f, 32f), CycleDifficulty);
            y -= 44f;

            CreateLabel(boxGo.transform, "Colorblind Mode", new Vector2(-140f, y), 18, TextAlignmentOptions.Left);
            _colorblindValueText = CreateButton(boxGo.transform, "", new Vector2(120f, y), new Vector2(160f, 32f), ToggleColorblind);
            y -= 50f;

            CreateLabel(boxGo.transform, "Key Bindings", new Vector2(0f, y), 20, TextAlignmentOptions.Center);
            y -= 36f;

            for (int i = 0; i < Actions.Length; i++)
            {
                string actionId = Actions[i].Id;
                CreateLabel(boxGo.transform, Actions[i].Label, new Vector2(-100f, y), 15, TextAlignmentOptions.Left);
                _keyButtonTexts[i] = CreateButton(boxGo.transform, "", new Vector2(170f, y), new Vector2(120f, 28f),
                    () => { _rebindingActionId = actionId; RefreshDisplayedValues(); });
                y -= 32f;
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

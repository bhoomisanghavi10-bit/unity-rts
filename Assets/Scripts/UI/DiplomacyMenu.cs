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
        // Enemy2 is only ever assigned a civilization (CivilizationRegistry
        // .Assign) when CivilizationSetup's enableThirdFaction is on -
        // listing it unconditionally showed a working War/Allied row for a
        // faction with no army, no buildings, and (via CivilizationRegistry
        // .For's Chola fallback, since Enemy2 was never actually assigned
        // anything) a civilization that looked real but wasn't - read by a
        // player as a second, unexplained enemy. Filtered to only the
        // factions CivilizationSetup actually assigned, computed fresh each
        // time the menu opens rather than cached at Awake, since Enemy2 can
        // go from unassigned to assigned within the same DontDestroyOnLoad
        // instance's lifetime (a new match can turn enableThirdFaction on).
        private static readonly FactionId[] AllOtherFactions = { FactionId.Enemy, FactionId.Enemy2 };
        private const string ToggleActionId = "ToggleDiplomacy";
        private static readonly KeyCode ToggleDefault = KeyCode.F11;

        private GameObject _panel;
        private FactionId[] _otherFactions = System.Array.Empty<FactionId>();
        private TMP_Text[] _relationTexts = System.Array.Empty<TMP_Text>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("DiplomacyMenu");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DiplomacyMenu>();
        }

        private Transform _rowContainer;

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
                    RebuildRows();
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
            for (int i = 0; i < _otherFactions.Length; i++)
            {
                bool allied = DiplomacyRegistry.AreAllied(FactionId.Player, _otherFactions[i]);
                _relationTexts[i].text = allied ? "Allied" : "War";
            }
        }

        // Recomputes which factions actually have a row - only ones
        // CivilizationSetup has assigned a civilization to (Enemy always
        // is; Enemy2 only when enableThirdFaction is on). Called each time
        // the panel opens rather than just once at Awake, since a later
        // match can turn the 3rd faction on/off within this DontDestroy
        // OnLoad instance's lifetime.
        private void RebuildRows()
        {
            // Same stale-reference class of bug fixed in SettingsMenu: if a
            // script recompiles while this DontDestroyOnLoad instance is
            // still alive in a running Play session, Unity's domain-reload
            // backup doesn't reliably restore every private reference
            // field - confirmed live here too (_rowContainer came back
            // null while _panel, on the same instance, didn't). The
            // existing UI is still there and visible, just unreachable
            // through this field, so tear down the whole canvas and let
            // BuildUi() recreate it (which calls back into this method
            // once _rowContainer is valid again) rather than trying to
            // patch just this one field.
            if (_rowContainer == null)
            {
                if (_panel != null)
                {
                    Destroy(_panel.transform.parent.gameObject);
                }
                BuildUi();
                return;
            }

            for (int i = _rowContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_rowContainer.GetChild(i).gameObject);
            }

            var active = new System.Collections.Generic.List<FactionId>();
            foreach (FactionId candidate in AllOtherFactions)
            {
                if (CivilizationRegistry.IsAssigned(candidate))
                {
                    active.Add(candidate);
                }
            }
            _otherFactions = active.ToArray();
            _relationTexts = new TMP_Text[_otherFactions.Length];

            float y = 40f;
            for (int i = 0; i < _otherFactions.Length; i++)
            {
                FactionId other = _otherFactions[i];
                CreateLabel(_rowContainer, other.ToString(), new Vector2(-140f, y), 17, TextAlignmentOptions.Left);
                _relationTexts[i] = CreateButton(_rowContainer, "", new Vector2(110f, y), new Vector2(160f, 32f),
                    () => ToggleAlliance(other));
                y -= 44f;
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
            backdropImage.color = UIStyleTheme.Current.PanelBackdrop;
            var backdropRect = _panel.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            var boxGo = new GameObject("Box");
            boxGo.transform.SetParent(_panel.transform, false);
            var boxImage = boxGo.AddComponent<Image>();
            UIStyleTheme.Current.ApplyPanel(boxImage);
            var boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(460f, 260f);
            boxRect.anchoredPosition = Vector2.zero;

            CreateLabel(boxGo.transform, "Diplomacy", new Vector2(0f, 90f), 26, TextAlignmentOptions.Center);

            var rowContainerGo = new GameObject("Rows");
            rowContainerGo.transform.SetParent(boxGo.transform, false);
            var rowContainerRect = rowContainerGo.AddComponent<RectTransform>();
            rowContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowContainerRect.sizeDelta = Vector2.zero;
            rowContainerRect.anchoredPosition = Vector2.zero;
            _rowContainer = rowContainerGo.transform;

            CreateButton(boxGo.transform, "Close", new Vector2(0f, -90f), new Vector2(140f, 34f),
                () => _panel.SetActive(false));

            RebuildRows();
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
            tmp.color = UIStyleTheme.Current.TextPrimary;
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
            UIStyleTheme.Current.ApplyButton(image);

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
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}

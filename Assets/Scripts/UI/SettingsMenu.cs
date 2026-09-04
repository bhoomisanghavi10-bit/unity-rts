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
            new RebindableAction("CycleFormation", "Cycle Formation", KeyCode.R),
            new RebindableAction("ToggleComposedFormation", "Toggle Composed Formation", KeyCode.C),
            new RebindableAction("PlaceBarracks", "Place Barracks", KeyCode.B),
            new RebindableAction("PlaceFarm", "Place Farm", KeyCode.F),
            new RebindableAction("PlaceHouse", "Place House", KeyCode.H),
            new RebindableAction("PlaceWall", "Place Wall", KeyCode.L),
            new RebindableAction("PlaceGate", "Place Gate", KeyCode.K),
            new RebindableAction("PlaceTower", "Place Tower", KeyCode.O),
            new RebindableAction("PlaceMarket", "Place Market", KeyCode.M),
            // Item 1 (Hotkeys): these 4 were already functional via
            // BuildingPlacer/GameSettings but missing from this list, so
            // they were invisible/unrebindable in the Settings UI.
            new RebindableAction("PlaceDock", "Place Dock", KeyCode.N),
            new RebindableAction("PlaceLumberCamp", "Place Lumber Camp", KeyCode.J),
            new RebindableAction("PlaceMiningCamp", "Place Mining Camp", KeyCode.U),
            new RebindableAction("PlaceMill", "Place Mill", KeyCode.P),
            new RebindableAction("PlaceDurg", "Place Durg", KeyCode.D),
            new RebindableAction("PlaceKarmashala", "Place Karmashala", KeyCode.R),
            new RebindableAction("TrainWorker", "Train Worker (Town Center)", KeyCode.G),
            new RebindableAction("AdvanceAge", "Advance Age (Town Center)", KeyCode.Y),
            new RebindableAction("ResearchImprovedTools", "Research Improved Tools (Town Center)", KeyCode.I),
            new RebindableAction("ResearchPackMules", "Research Pack Mules (Town Center)", KeyCode.P),
            new RebindableAction("ResearchTradeDiscounts", "Research Trade Discounts (Town Center)", KeyCode.D),
            new RebindableAction("TrainUnit", "Train Soldier (Barracks)", KeyCode.T),
            new RebindableAction("TrainArcher", "Train Archer (Barracks)", KeyCode.A),
            new RebindableAction("TrainCavalry", "Train Cavalry (Barracks)", KeyCode.N),
            new RebindableAction("TrainSiege", "Train Siege (Barracks)", KeyCode.S),
            new RebindableAction("TrainSpearman", "Train Spearman (Barracks)", KeyCode.E),
            new RebindableAction("TrainUniqueUnit", "Train Unique Unit (Durg)", KeyCode.Q),
            new RebindableAction("TrainUniqueUnit2", "Train 2nd Unique Unit (Durg)", KeyCode.Z),
            new RebindableAction("ResearchAttack", "Research Attack (Karmashala)", KeyCode.U),
            new RebindableAction("ResearchArmor", "Research Armor (Karmashala)", KeyCode.K),
            new RebindableAction("ResearchUniqueTech", "Research Unique Tech (Barracks)", KeyCode.J),
            new RebindableAction("ResearchInfantryTier", "Upgrade Infantry Tier (Barracks)", KeyCode.I),
            new RebindableAction("TrainDockUnit", "Train Fishing Boat (Dock)", KeyCode.B),
            new RebindableAction("TrainWarGalley", "Train War Galley (Dock)", KeyCode.W),
            new RebindableAction("Ungarrison", "Ungarrison", KeyCode.U),
            new RebindableAction("ToggleHotkeyOverlay", "Toggle Hotkey Reference (Overlay)", KeyCode.F1),
            new RebindableAction("SaveGame", "Quicksave", KeyCode.F5),
            new RebindableAction("LoadGame", "Quickload", KeyCode.F9),
            new RebindableAction("ToggleDiplomacy", "Toggle Diplomacy", KeyCode.F11),
        };

        private const string ToggleActionId = "ToggleSettings";
        private static readonly KeyCode ToggleDefault = KeyCode.F10;

        // Item 2 (Victory Conditions): 0 = Off, matching GameSettings.
        // TimeLimitMinutes' own default-means-old-behavior convention.
        private static readonly int[] TimeLimitPresets = { 0, 15, 30, 45, 60 };

        private GameObject _panel;
        private TMP_Text _difficultyValueText;
        private TMP_Text _colorblindValueText;
        private TMP_Text _timeLimitValueText;
        private TMP_Text[] _keyButtonTexts = new TMP_Text[Actions.Length];
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
            // _keyButtonTexts is a plain (non-serialized) array of Object
            // references - if a script recompiles while this DontDestroy
            // OnLoad instance is still alive in a running Play session (a
            // routine part of iterating in the Editor, not something an
            // actual build ever does), Unity's domain-reload state backup
            // doesn't restore array elements the way it restores single
            // reference fields like _panel, leaving every entry null even
            // though the underlying UI GameObjects are still there and
            // still visible - this NREd on exactly that stale-reference
            // read. The existing UI is intact, just unreachable through
            // this array, so tearing it down and calling BuildUi() again
            // (which repopulates the array from scratch) is enough - no
            // need to hunt down and reuse the orphaned hierarchy.
            if (_keyButtonTexts.Length > 0 && _keyButtonTexts[0] == null)
            {
                bool wasOpen = _panel != null && _panel.activeSelf;
                if (_panel != null)
                {
                    Destroy(_panel.transform.parent.gameObject);
                }
                BuildUi();
                _panel.SetActive(wasOpen);
                return;
            }

            _difficultyValueText.text = GameSettings.Difficulty.ToString();
            _colorblindValueText.text = GameSettings.ColorblindMode ? "On" : "Off";
            int timeLimit = GameSettings.TimeLimitMinutes;
            _timeLimitValueText.text = timeLimit <= 0 ? "Off" : $"{timeLimit} min";

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

        // Item 2 (Victory Conditions): same cycle-through-presets shape as
        // CycleDifficulty above.
        private void CycleTimeLimit()
        {
            int currentIndex = System.Array.IndexOf(TimeLimitPresets, GameSettings.TimeLimitMinutes);
            int nextIndex = (currentIndex + 1) % TimeLimitPresets.Length; // -1 (unknown/corrupt value) wraps to 0 = Off, a safe fallback
            GameSettings.TimeLimitMinutes = TimeLimitPresets[nextIndex];
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
            // Grown from the original 520x620 (Item 2, Victory Conditions
            // session): Actions.Length grew from 12 to 34 across the prior
            // hotkey-coverage session without the box or layout being
            // resized to match, so most rows rendered below the panel's own
            // background - a real regression, caught via a live screenshot
            // while adding this item's own new row. Fixed properly below by
            // scrolling the list instead of just growing the box further
            // (34+ rows still wouldn't fit any reasonably-sized single
            // screen), but the extra height/width here gives the fixed rows
            // above the list (Difficulty/Colorblind/Time Limit) more room.
            boxRect.sizeDelta = new Vector2(560f, 700f);
            boxRect.anchoredPosition = Vector2.zero;

            float y = 320f;
            CreateLabel(boxGo.transform, "Settings", new Vector2(0f, y), 28, TextAlignmentOptions.Center);
            y -= 50f;

            CreateLabel(boxGo.transform, "Difficulty", new Vector2(-150f, y), 18, TextAlignmentOptions.Left);
            _difficultyValueText = CreateButton(boxGo.transform, "", new Vector2(130f, y), new Vector2(160f, 32f), CycleDifficulty);
            y -= 44f;

            CreateLabel(boxGo.transform, "Colorblind Mode", new Vector2(-150f, y), 18, TextAlignmentOptions.Left);
            _colorblindValueText = CreateButton(boxGo.transform, "", new Vector2(130f, y), new Vector2(160f, 32f), ToggleColorblind);
            y -= 44f;

            CreateLabel(boxGo.transform, "Time Limit", new Vector2(-150f, y), 18, TextAlignmentOptions.Left);
            _timeLimitValueText = CreateButton(boxGo.transform, "", new Vector2(130f, y), new Vector2(160f, 32f), CycleTimeLimit);
            y -= 50f;

            CreateLabel(boxGo.transform, "Key Bindings", new Vector2(0f, y), 20, TextAlignmentOptions.Center);
            y -= 26f;

            // Item 2 (Victory Conditions): the list itself scrolls instead
            // of growing the box to fit every row - Actions.Length (34+) at
            // any readable row height doesn't fit a single screen. Standard
            // uGUI ScrollRect + Viewport (RectMask2D-clipped) + Content
            // (sized to the full row count, top-pivoted so rows can be
            // positioned by distance-from-top exactly like the old
            // unscrolled loop was). Mouse-wheel and drag-scroll both work
            // for free via ScrollRect - no separate Scrollbar needed for a
            // functional fix.
            const float rowHeight = 32f;
            const float scrollWidth = 480f;
            float scrollBottom = -290f;
            float scrollHeight = y - scrollBottom;

            var scrollGo = new GameObject("KeyBindingsScroll");
            scrollGo.transform.SetParent(boxGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            // Anchored to the box's CENTER (0.5,0.5), matching every other
            // row above - NOT the box's top edge (0.5,1f), which was this
            // section's first-draft bug: anchoredPosition.y is measured
            // from wherever anchorMin/Max place the reference point, so a
            // top-edge anchor made `y` (already center-relative, like every
            // other row's own y) put the whole scroll area far too high,
            // overlapping Difficulty/Colorblind/Time Limit - caught via a
            // live screenshot. Pivot stays top (0.5,1f) so anchoredPosition
            // still refers to this rect's own TOP edge, letting it hang
            // downward from y by scrollHeight.
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.sizeDelta = new Vector2(scrollWidth, scrollHeight);
            scrollRect.anchoredPosition = new Vector2(0f, y);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = rowHeight;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            // Near-transparent rather than fully transparent so this Image
            // still catches drag/scroll input over the whole viewport, not
            // just where a row's own Button graphic sits.
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, Actions.Length * rowHeight);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            float rowY = 0f;
            for (int i = 0; i < Actions.Length; i++)
            {
                string actionId = Actions[i].Id;
                CreateLabel(contentGo.transform, Actions[i].Label, new Vector2(-100f, rowY), 15, TextAlignmentOptions.Left, anchorY: 1f);
                _keyButtonTexts[i] = CreateButton(contentGo.transform, "", new Vector2(170f, rowY), new Vector2(120f, 28f),
                    () => { _rebindingActionId = actionId; RefreshDisplayedValues(); }, anchorY: 1f);
                rowY -= rowHeight;
            }

            CreateButton(boxGo.transform, "Close", new Vector2(0f, scrollBottom - 30f), new Vector2(140f, 34f),
                () => _panel.SetActive(false));

            RefreshDisplayedValues();
        }

        // anchorY: 0.5 (default) for rows parented directly to the box,
        // matching every pre-existing call site exactly. 1f for rows
        // parented to Content in the scrollable Key Bindings list, so
        // anchoredPosition.y is measured from Content's top edge (matching
        // Content's own top pivot) rather than its center, which is what
        // lets `rowY -= rowHeight` behave the same way the old unscrolled
        // loop's `y -= 32f` did.
        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment, float anchorY = 0.5f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);
            rect.sizeDelta = new Vector2(300f, 30f);
            rect.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = alignment;
            return tmp;
        }

        private static TMP_Text CreateButton(Transform parent, string text, Vector2 position, Vector2 size, System.Action onClick, float anchorY = 0.5f)
        {
            var go = new GameObject("Button_" + (string.IsNullOrEmpty(text) ? "Value" : text));
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.UI
{
    // Item 1 (Hotkeys): read-only F1 reference panel listing every binding in
    // the game, grouped by the context it fires in (mirrors BuildMenu's own
    // Global/Placement/Town Center/Barracks/Dock/Garrison grouping - see
    // BuildMenu.ApplyKeySettings and BuildingPlacer.ApplyKeySettings for
    // where these same ids/defaults are actually consumed). Same
    // self-bootstrapping runtime-built-Canvas pattern as SettingsMenu (which
    // remains the only place a binding can actually be changed - this panel
    // never writes to GameSettings, only reads, so it's safe to have open at
    // the same time). Values are re-read from GameSettings on every open, so
    // a rebind made in Settings shows up immediately here.
    public class HotkeyOverlay : MonoBehaviour
    {
        private readonly struct Entry
        {
            public readonly string Label;
            public readonly string ActionId;
            public readonly KeyCode Default;

            public Entry(string label, string actionId, KeyCode defaultKey)
            {
                Label = label;
                ActionId = actionId;
                Default = defaultKey;
            }
        }

        private readonly struct Group
        {
            public readonly string Title;
            public readonly Entry[] Entries;

            public Group(string title, Entry[] entries)
            {
                Title = title;
                Entries = entries;
            }
        }

        private static readonly Group GlobalGroup = new Group("Global", new[]
        {
            new Entry("Cycle Stance", "CycleStance", KeyCode.V),
            new Entry("Cycle Formation", "CycleFormation", KeyCode.R),
            new Entry("Composed Formation", "ToggleComposedFormation", KeyCode.C),
            new Entry("Save", "SaveGame", KeyCode.F5),
            new Entry("Load", "LoadGame", KeyCode.F9),
            new Entry("Settings", "ToggleSettings", KeyCode.F10),
            new Entry("Diplomacy", "ToggleDiplomacy", KeyCode.F11),
            new Entry("Hotkey Reference", "ToggleHotkeyOverlay", KeyCode.F1),
        });

        private static readonly Group PlacementGroup = new Group("Placement (Builder Selected)", new[]
        {
            new Entry("Place Barracks", "PlaceBarracks", KeyCode.B),
            new Entry("Place Farm", "PlaceFarm", KeyCode.F),
            new Entry("Place House", "PlaceHouse", KeyCode.H),
            new Entry("Place Wall", "PlaceWall", KeyCode.L),
            new Entry("Place Gate", "PlaceGate", KeyCode.K),
            new Entry("Place Tower", "PlaceTower", KeyCode.O),
            new Entry("Place Market", "PlaceMarket", KeyCode.M),
            new Entry("Place Dock", "PlaceDock", KeyCode.N),
            new Entry("Place Lumber Camp", "PlaceLumberCamp", KeyCode.J),
            new Entry("Place Mining Camp", "PlaceMiningCamp", KeyCode.U),
            new Entry("Place Mill", "PlaceMill", KeyCode.P),
            new Entry("Place Durg", "PlaceDurg", KeyCode.D),
            new Entry("Place Karmashala", "PlaceKarmashala", KeyCode.R),
            new Entry("Place Monastery", "PlaceMonastery", KeyCode.G),
        });

        private static readonly Group TownCenterGroup = new Group("Town Center Selected", new[]
        {
            new Entry("Train Worker", "TrainWorker", KeyCode.G),
            new Entry("Advance Age", "AdvanceAge", KeyCode.Y),
            new Entry("Improved Tools", "ResearchImprovedTools", KeyCode.I),
            new Entry("Pack Mules", "ResearchPackMules", KeyCode.P),
            new Entry("Trade Discounts", "ResearchTradeDiscounts", KeyCode.D),
        });

        private static readonly Group BarracksGroup = new Group("Barracks Selected", new[]
        {
            new Entry("Train Soldier", "TrainUnit", KeyCode.T),
            new Entry("Train Archer", "TrainArcher", KeyCode.A),
            new Entry("Train Cavalry", "TrainCavalry", KeyCode.N),
            new Entry("Train Siege", "TrainSiege", KeyCode.S),
            new Entry("Train Spearman", "TrainSpearman", KeyCode.E),
            new Entry("Train Scout", "TrainChara", KeyCode.F),
            new Entry("Train Skirmisher", "TrainSkirmisher", KeyCode.C),
            new Entry("Train Battering Ram", "TrainBatteringRam", KeyCode.D),
            new Entry("Train Cavalry Archer", "TrainCavalryArcher", KeyCode.K),
            new Entry("Train Camel Rider", "TrainCamelRider", KeyCode.U),
            new Entry("Unique Tech", "ResearchUniqueTech", KeyCode.J),
            new Entry("Upgrade Infantry Tier", "ResearchInfantryTier", KeyCode.I),
            new Entry("Upgrade Spearman Tier", "ResearchSpearmanTier", KeyCode.L),
            new Entry("Upgrade Archer Tier", "ResearchArcherTier", KeyCode.H),
            new Entry("Upgrade Cavalry Tier", "ResearchCavalryTier", KeyCode.M),
            new Entry("Upgrade Siege Tier", "ResearchSiegeTier", KeyCode.O),
            new Entry("Upgrade Scout Tier", "ResearchCharaTier", KeyCode.G),
            new Entry("Upgrade Skirmisher Tier", "ResearchSkirmisherTier", KeyCode.V),
            new Entry("Upgrade Battering Ram Tier", "ResearchBatteringRamTier", KeyCode.R),
            new Entry("Upgrade Cavalry Archer Tier", "ResearchCavalryArcherTier", KeyCode.P),
            new Entry("Upgrade Camel Rider Tier", "ResearchCamelRiderTier", KeyCode.B),
            new Entry("Train Scorpion", "TrainScorpion", KeyCode.W),
            new Entry("Train Trebuchet", "TrainTrebuchet", KeyCode.Q),
            new Entry("Upgrade Scorpion Tier", "ResearchScorpionTier", KeyCode.X),
        });

        // Wave 2 item 7: unique-unit training moved off Barracks onto the
        // new Durg building - own group, same shape as BarracksGroup.
        private static readonly Group DurgGroup = new Group("Durg Selected", new[]
        {
            new Entry("Unique Unit", "TrainUniqueUnit", KeyCode.Q),
            new Entry("2nd Unique Unit", "TrainUniqueUnit2", KeyCode.Z),
            new Entry("Upgrade Elephant Tier", "ResearchElephantTier", KeyCode.R),
            new Entry("Upgrade Elite Tier", "ResearchEliteTier", KeyCode.F),
            new Entry("Upgrade 2nd Elite Tier", "ResearchEliteTier2", KeyCode.G),
            new Entry("Train Maharaja", "TrainHero", KeyCode.M),
        });

        // Wave 2 item 8: flat Attack/Armor research moved off Barracks onto
        // the new Karmashala building - own group, same shape as DurgGroup's
        // own split from BarracksGroup last session.
        private static readonly Group KarmashalaGroup = new Group("Karmashala Selected", new[]
        {
            new Entry("Attack Upgrade", "ResearchAttack", KeyCode.U),
            new Entry("Armor Upgrade", "ResearchArmor", KeyCode.K),
        });

        private static readonly Group DockGroup = new Group("Dock Selected", new[]
        {
            new Entry("Train Fishing Boat", "TrainDockUnit", KeyCode.B),
            new Entry("Train War Galley", "TrainWarGalley", KeyCode.W),
            new Entry("Upgrade Naval Tier", "ResearchNavalTier", KeyCode.X),
            new Entry("Train Fire Ship", "TrainFireShip", KeyCode.Y),
            new Entry("Upgrade Fire Ship Tier", "ResearchFireShipTier", KeyCode.Z),
            new Entry("Train Trade Ship", "TrainTradeShip", KeyCode.T),
        });

        // Wave 4 item 26: first Market-context hotkeys - own group, same
        // shape as KarmashalaGroup's own split from Barracks.
        private static readonly Group MarketGroup = new Group("Market Selected", new[]
        {
            new Entry("Train Vanik", "TrainVanik", KeyCode.V),
        });

        // Wave 4 item 27: first Monastery-context hotkeys - own group,
        // same shape as MarketGroup.
        private static readonly Group MonasteryGroup = new Group("Monastery Selected", new[]
        {
            new Entry("Train Vaidya", "TrainVaidya", KeyCode.H),
            new Entry("Train Purohita", "TrainPurohita", KeyCode.C),
        });

        private static readonly Group GarrisonGroup = new Group("Garrisoned Building Selected", new[]
        {
            new Entry("Ungarrison", "Ungarrison", KeyCode.U),
        });

        // Balanced across 3 columns by row count (header + entries), not by
        // group identity - purely a layout concern.
        private static readonly Group[][] Columns =
        {
            new[] { GlobalGroup, DockGroup, MarketGroup, MonasteryGroup, GarrisonGroup },
            new[] { PlacementGroup },
            new[] { TownCenterGroup, BarracksGroup, DurgGroup, KarmashalaGroup },
        };

        private GameObject _panel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("HotkeyOverlay");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<HotkeyOverlay>();
        }

        private void Awake()
        {
            BuildUi();
            _panel.SetActive(false);
        }

        private void Update()
        {
            KeyCode toggleKey = GameSettings.GetKey("ToggleHotkeyOverlay", KeyCode.F1);
            if (Input.GetKeyDown(toggleKey))
            {
                bool open = !_panel.activeSelf;
                _panel.SetActive(open);
                if (open)
                {
                    RefreshDisplayedValues();
                }
            }
        }

        // Re-reads every entry's currently-bound key so a rebind made in
        // Settings (or loaded from PlayerPrefs) is reflected the next time
        // this panel is opened, without needing to rebuild the whole UI.
        private void RefreshDisplayedValues()
        {
            foreach (TMP_Text keyText in _panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (keyText.TryGetComponent(out HotkeyOverlayKeyLabel binding))
                {
                    keyText.text = GameSettings.GetKey(binding.ActionId, binding.Default).ToString();
                }
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("HotkeyOverlayCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // same layer as SettingsMenu - both are modal reference panels
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
            boxRect.sizeDelta = new Vector2(900f, 620f);
            boxRect.anchoredPosition = Vector2.zero;

            CreateLabel(boxGo.transform, "Hotkey Reference", new Vector2(0f, 280f), 26, TextAlignmentOptions.Center);

            // Column 3 (Town Center/Barracks/Durg/Karmashala) has grown to
            // ~46 rows across many sessions adding new trainable units -
            // far taller than this box's own fixed height, so it used to
            // spill straight off the bottom of the screen uncontained (no
            // mask at all). Same ScrollRect+Viewport(RectMask2D)+Content
            // fix SettingsMenu's own Key Bindings list already uses:
            // content sized to the TALLEST column, top-pivoted so rows can
            // be positioned by distance-from-top exactly like the old
            // unscrolled loop was, and only vertical scroll is needed since
            // content width matches the viewport (columnX values are still
            // valid unchanged, relative to content's own center).
            const float rowHeight = 20f;
            const float groupHeaderHeight = 26f;
            const float groupSpacing = 10f;
            const float scrollWidth = 900f;
            float scrollTop = 230f;
            float scrollBottom = -260f;
            float scrollHeight = scrollTop - scrollBottom;

            var scrollGo = new GameObject("HotkeyScroll");
            scrollGo.transform.SetParent(boxGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.sizeDelta = new Vector2(scrollWidth, scrollHeight);
            scrollRect.anchoredPosition = new Vector2(0f, scrollTop);
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
            // just where a row's own text sits.
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            float[] columnX = { -300f, 0f, 300f };
            float maxColumnHeight = 0f;
            for (int c = 0; c < Columns.Length; c++)
            {
                float y = 0f;
                foreach (Group group in Columns[c])
                {
                    CreateLabel(contentGo.transform, group.Title, new Vector2(columnX[c], y), 15, TextAlignmentOptions.Center, 220f, 1f);
                    y -= groupHeaderHeight;

                    foreach (Entry entry in group.Entries)
                    {
                        // Narrower than the 220-wide default (which is fine
                        // for a single centered group title) - at the full
                        // width, adjacent columns' label/key pairs
                        // overlapped each other horizontally (caught via a
                        // live screenshot: "Place Wall" visibly bled into
                        // the Global column's "F10" at the same row height).
                        CreateLabel(contentGo.transform, entry.Label, new Vector2(columnX[c] - 75f, y), 12, TextAlignmentOptions.Left, 150f, 1f);
                        TMP_Text keyLabel = CreateLabel(contentGo.transform, "", new Vector2(columnX[c] + 60f, y), 12, TextAlignmentOptions.Right, 80f, 1f);
                        var binding = keyLabel.gameObject.AddComponent<HotkeyOverlayKeyLabel>();
                        binding.ActionId = entry.ActionId;
                        binding.Default = entry.Default;
                        keyLabel.text = GameSettings.GetKey(entry.ActionId, entry.Default).ToString();
                        y -= rowHeight;
                    }

                    y -= groupSpacing;
                }

                maxColumnHeight = Mathf.Max(maxColumnHeight, -y);
            }

            contentRect.sizeDelta = new Vector2(0f, maxColumnHeight);

            CreateButton(boxGo.transform, "Close", new Vector2(0f, -290f), new Vector2(140f, 34f),
                () => _panel.SetActive(false));
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment, float width = 220f, float anchorY = 0.5f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);
            rect.sizeDelta = new Vector2(width, 22f);
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
            var go = new GameObject("Button_" + text);
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

    // Tiny tag component so RefreshDisplayedValues can find each key-value
    // label and re-read its current binding without keeping a parallel
    // array in sync by hand (the pattern SettingsMenu uses) - there are too
    // many entries here for that to stay readable.
    internal class HotkeyOverlayKeyLabel : MonoBehaviour
    {
        public string ActionId;
        public KeyCode Default;
    }
}

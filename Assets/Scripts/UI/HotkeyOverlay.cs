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
            new Entry("Unique Tech", "ResearchUniqueTech", KeyCode.J),
        });

        // Wave 2 item 7: unique-unit training moved off Barracks onto the
        // new Durg building - own group, same shape as BarracksGroup.
        private static readonly Group DurgGroup = new Group("Durg Selected", new[]
        {
            new Entry("Unique Unit", "TrainUniqueUnit", KeyCode.Q),
            new Entry("2nd Unique Unit", "TrainUniqueUnit2", KeyCode.Z),
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
        });

        private static readonly Group GarrisonGroup = new Group("Garrisoned Building Selected", new[]
        {
            new Entry("Ungarrison", "Ungarrison", KeyCode.U),
        });

        // Balanced across 3 columns by row count (header + entries), not by
        // group identity - purely a layout concern.
        private static readonly Group[][] Columns =
        {
            new[] { GlobalGroup, DockGroup, GarrisonGroup },
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

            float[] columnX = { -300f, 0f, 300f };
            for (int c = 0; c < Columns.Length; c++)
            {
                float y = 230f;
                foreach (Group group in Columns[c])
                {
                    CreateLabel(boxGo.transform, group.Title, new Vector2(columnX[c], y), 15, TextAlignmentOptions.Center);
                    y -= 26f;

                    foreach (Entry entry in group.Entries)
                    {
                        // Narrower than the 220-wide default (which is fine
                        // for a single centered group title) - at the full
                        // width, adjacent columns' label/key pairs
                        // overlapped each other horizontally (caught via a
                        // live screenshot: "Place Wall" visibly bled into
                        // the Global column's "F10" at the same row height).
                        CreateLabel(boxGo.transform, entry.Label, new Vector2(columnX[c] - 75f, y), 12, TextAlignmentOptions.Left, 150f);
                        TMP_Text keyLabel = CreateLabel(boxGo.transform, "", new Vector2(columnX[c] + 60f, y), 12, TextAlignmentOptions.Right, 80f);
                        var binding = keyLabel.gameObject.AddComponent<HotkeyOverlayKeyLabel>();
                        binding.ActionId = entry.ActionId;
                        binding.Default = entry.Default;
                        keyLabel.text = GameSettings.GetKey(entry.ActionId, entry.Default).ToString();
                        y -= 20f;
                    }

                    y -= 10f;
                }
            }

            CreateButton(boxGo.transform, "Close", new Vector2(0f, -290f), new Vector2(140f, 34f),
                () => _panel.SetActive(false));
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment, float width = 220f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
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

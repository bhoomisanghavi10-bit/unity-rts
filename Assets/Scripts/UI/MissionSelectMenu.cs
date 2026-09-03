using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;

namespace KingdomsOfBharat.UI
{
    // Item 50: the very first screen - choose a scripted mission, or
    // Skirmish to fall through to the existing CivPicker flow unchanged.
    // Code-generated at runtime (same pattern as SettingsMenu/
    // DiplomacyMenu) at a higher sort order than CivPicker's own scene-
    // authored Canvas, so it overlays on top rather than needing any
    // change to Main.unity - CivPicker itself is untouched, still active
    // by default, this just sits in front of it until a choice is made.
    public class MissionSelectMenu : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<MissionSelectMenu>() != null)
            {
                return;
            }

            var go = new GameObject("MissionSelectMenu");
            go.AddComponent<MissionSelectMenu>();
        }

        private void Awake()
        {
            BuildUi();
        }

        private void ChooseSkirmish()
        {
            // CivPicker is already active underneath (scene default) -
            // just remove this overlay and let it take input normally.
            Destroy(gameObject);
        }

        // Item 6 (Scenario Editor, heavy path session 1): opens the in-game
        // placements editor. Same self-destroying handoff ChooseScenario
        // already uses below (CivPicker isn't needed for editing - the
        // editor picks civ/map itself when a scenario is actually saved/
        // played), so this menu just gets out of the way.
        private void ChooseCreateScenario()
        {
            var civPicker = FindFirstObjectByType<CivPicker>();
            if (civPicker != null)
            {
                Destroy(civPicker.gameObject);
            }

            ScenarioEditorMenu.Open();
            Destroy(gameObject);
        }

        private void ChooseScenario(ScenarioDefinition scenario)
        {
            var civSetup = FindFirstObjectByType<CivilizationSetup>();
            if (civSetup == null)
            {
                Debug.LogWarning("[MissionSelectMenu] No CivilizationSetup in scene - can't start scenario.");
                return;
            }

            civSetup.BeginScenarioMatch(scenario);

            var civPicker = FindFirstObjectByType<CivPicker>();
            if (civPicker != null)
            {
                Destroy(civPicker.gameObject);
            }

            Destroy(gameObject);
        }

        // Item 6 (Scenario Editor, heavy path session 3): plays a saved
        // custom scenario directly from the browse list, without going
        // through the editor - mirrors ChooseScenario's own shutdown
        // sequence exactly, just calling BeginCustomScenarioMatch (same
        // entry point ScenarioEditorMenu's own Play button already uses)
        // instead of BeginScenarioMatch.
        private void ChooseCustomScenario(string name)
        {
            CustomScenarioData data = SavedScenarioLibrary.Load(name);
            if (data == null)
            {
                Debug.LogWarning("[MissionSelectMenu] Saved scenario not found: " + name);
                return;
            }

            var civSetup = FindFirstObjectByType<CivilizationSetup>();
            if (civSetup == null)
            {
                Debug.LogWarning("[MissionSelectMenu] No CivilizationSetup in scene - can't start scenario.");
                return;
            }

            civSetup.BeginCustomScenarioMatch(data);

            var civPicker = FindFirstObjectByType<CivPicker>();
            if (civPicker != null)
            {
                Destroy(civPicker.gameObject);
            }

            Destroy(gameObject);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("MissionSelectMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // above SettingsMenu (200)/DiplomacyMenu (190)/CivPicker
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var backdropImage = panel.AddComponent<Image>();
            backdropImage.color = UIStyleTheme.Current.PanelBackground;
            var backdropRect = panel.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            var boxGo = new GameObject("Box");
            boxGo.transform.SetParent(panel.transform, false);
            var boxRect = boxGo.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(640f, 640f);
            boxRect.anchoredPosition = Vector2.zero;

            float y = 280f;
            CreateLabel(boxGo.transform, "Kingdoms of Bharat", new Vector2(0f, y), 30, TextAlignmentOptions.Center);
            y -= 40f;
            CreateLabel(boxGo.transform, "Choose a Campaign or play Skirmish", new Vector2(0f, y), 16, TextAlignmentOptions.Center);
            y -= 60f;

            foreach (ScenarioDefinition scenario in ScenarioRegistry.All)
            {
                CreateMissionRow(boxGo.transform, scenario, ref y);
            }

            y -= 20f;
            CreateButton(boxGo.transform, "Skirmish (Free Play)", new Vector2(0f, y), new Vector2(400f, 44f), ChooseSkirmish);
            y -= 50f;
            CreateButton(boxGo.transform, "Create Scenario", new Vector2(0f, y), new Vector2(400f, 40f), ChooseCreateScenario);
            y -= 46f;

            BuildCustomScenariosSection(boxGo.transform, ref y);
        }

        // Item 6 (Scenario Editor, heavy path session 3): a saved-scenario
        // browse list, previously only reachable from inside the editor
        // itself (open editor -> Load -> Play). The list is unbounded (a
        // player can save arbitrarily many), so it can't use the fixed-y
        // row layout the mission list above uses - same ScrollRect/
        // Viewport(RectMask2D)/Content pattern SettingsMenu.cs's own Key
        // Bindings list and ScenarioEditorMenu's own Objectives tab
        // (session 2) already establish for an unbounded row list.
        private void BuildCustomScenariosSection(Transform parent, ref float y)
        {
            CreateLabel(parent, "Custom Scenarios", new Vector2(0f, y), 16, TextAlignmentOptions.Center);
            y -= 26f;

            List<string> names = SavedScenarioLibrary.ListSavedScenarioNames();
            if (names.Count == 0)
            {
                CreateLabel(parent, "No saved scenarios yet - use Create Scenario to make one.",
                    new Vector2(0f, y), 12, TextAlignmentOptions.Center, wrapWidth: 400f);
                y -= 40f;
                return;
            }

            const float rowHeight = 28f;
            const float scrollHeight = 160f;

            var scrollGo = new GameObject("CustomScenariosScroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.sizeDelta = new Vector2(420f, scrollHeight);
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
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, names.Count * rowHeight);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            float rowY = 0f;
            foreach (string name in names)
            {
                CreateButton(contentGo.transform, name, new Vector2(0f, rowY), new Vector2(380f, 24f), () => ChooseCustomScenario(name));
                rowY -= rowHeight;
            }

            y -= scrollHeight + 10f;
        }

        private void CreateMissionRow(Transform parent, ScenarioDefinition scenario, ref float y)
        {
            CreateButton(parent, scenario.Title, new Vector2(0f, y), new Vector2(400f, 40f), () => ChooseScenario(scenario));
            y -= 34f;
            CreateLabel(parent, scenario.FlavorText, new Vector2(0f, y), 12, TextAlignmentOptions.Center, wrapWidth: 560f);
            y -= 50f;
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment, float wrapWidth = 300f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(wrapWidth, 60f);
            rect.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = UIStyleTheme.Current.TextSecondary;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
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
            tmp.fontSize = 17;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}

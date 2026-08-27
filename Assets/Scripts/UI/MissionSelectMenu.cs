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
            boxRect.sizeDelta = new Vector2(640f, 520f);
            boxRect.anchoredPosition = Vector2.zero;

            float y = 220f;
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

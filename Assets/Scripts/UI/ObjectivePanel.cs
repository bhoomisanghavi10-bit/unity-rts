using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Match;

namespace KingdomsOfBharat.UI
{
    // Item 50 gap-close: the objective/trigger system (MissionObjective/
    // ScenarioManager) already exists and evaluates correctly, but nothing
    // displayed it during play - this is the missing always-visible HUD
    // piece. Code-generated at runtime, same pattern as SettingsMenu/
    // DiplomacyMenu/MissionSelectMenu, so it never touches Main.unity.
    // Only ever visible when ScenarioManager.ActiveScenario != null - a
    // skirmish match (no scenario) sees nothing new, matching how
    // ScenarioManager itself is a pure addition alongside MatchManager's
    // existing elimination-based flow rather than a replacement of it.
    public class ObjectivePanel : MonoBehaviour
    {
        private GameObject _panel;
        private Transform _listParent;
        private readonly List<TMP_Text> _rows = new List<TMP_Text>();
        private ScenarioDefinition _builtFor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ObjectivePanel>() != null)
            {
                return;
            }

            var go = new GameObject("ObjectivePanel");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ObjectivePanel>();
        }

        private void Awake()
        {
            BuildUi();
            _panel.SetActive(false);
        }

        private void Update()
        {
            ScenarioDefinition active = ScenarioManager.ActiveScenario;

            if (active == null)
            {
                if (_panel.activeSelf)
                {
                    _panel.SetActive(false);
                    _builtFor = null;
                }
                return;
            }

            if (!_panel.activeSelf)
            {
                _panel.SetActive(true);
            }

            if (active != _builtFor)
            {
                RebuildRows(active);
            }

            RefreshRowText();
        }

        // Rows are rebuilt only when the active scenario reference changes
        // (mission start, or back to null) rather than every frame - each
        // scenario's objective count/order is fixed for its duration, only
        // the IsComplete() text/color needs a per-frame refresh.
        private void RebuildRows(ScenarioDefinition scenario)
        {
            foreach (TMP_Text row in _rows)
            {
                Destroy(row.gameObject);
            }
            _rows.Clear();

            IReadOnlyList<MissionObjective> objectives = ScenarioManager.CurrentObjectives;
            if (objectives == null)
            {
                _builtFor = scenario;
                return;
            }

            float y = -34f;
            foreach (MissionObjective _ in objectives)
            {
                TMP_Text row = CreateRow(_listParent, y);
                _rows.Add(row);
                y -= 26f;
            }

            _builtFor = scenario;
        }

        private void RefreshRowText()
        {
            IReadOnlyList<MissionObjective> objectives = ScenarioManager.CurrentObjectives;
            if (objectives == null)
            {
                return;
            }

            for (int i = 0; i < _rows.Count && i < objectives.Count; i++)
            {
                MissionObjective objective = objectives[i];
                bool complete = objective.IsComplete();
                _rows[i].text = complete
                    ? $"<s>{objective.Description}</s>"
                    : $"• {objective.Description}";
                _rows[i].color = complete ? new Color(0.55f, 0.85f, 0.55f) : Color.white;
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("ObjectivePanelCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // above default gameplay HUD, well below the menu overlays (190+)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var boxImage = _panel.AddComponent<Image>();
            boxImage.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);
            var boxRect = _panel.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 1f);
            boxRect.anchorMax = new Vector2(0f, 1f);
            boxRect.pivot = new Vector2(0f, 1f);
            boxRect.sizeDelta = new Vector2(360f, 140f);
            boxRect.anchoredPosition = new Vector2(16f, -16f);

            CreateLabel(_panel.transform, "Objectives", new Vector2(12f, -8f), 18, TextAlignmentOptions.TopLeft);

            var listGo = new GameObject("List");
            listGo.transform.SetParent(_panel.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;
            _listParent = listGo.transform;
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(336f, 24f);
            rect.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            return tmp;
        }

        private static TMP_Text CreateRow(Transform parent, float y)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(336f, 24f);
            rect.anchoredPosition = new Vector2(12f, y);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 15;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }
    }
}

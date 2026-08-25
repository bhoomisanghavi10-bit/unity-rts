using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Match;

namespace KingdomsOfBharat.UI
{
    // Item 50 gap-close (2/2): the campaign's only narrative moments were a
    // one-paragraph FlavorText blurb on the mission-select screen
    // (ScenarioRegistry) and nothing further during the mission. This adds
    // three text-only beats, all reusing content that already exists or is
    // opt-in per mission - deliberately not a full cutscene/dialogue
    // system, per the user's chosen scope:
    //  - Mission start: re-shows the scenario's FlavorText as a toast.
    //  - Objective complete: shows MissionObjective.CompleteText (if the
    //    mission author set one) the moment IsComplete() first flips true.
    //  - Match end: shows ScenarioDefinition.VictoryText/DefeatText (if
    //    set) alongside the existing GameOverScreen.
    // Code-generated at runtime, same pattern as ObjectivePanel/
    // SettingsMenu/DiplomacyMenu/MissionSelectMenu - never touches
    // Main.unity. Polls ScenarioManager/MatchManager's existing public
    // static state rather than editing either of those files directly,
    // since MatchManager.cs was mid-edit by another session's work on
    // item 50's core scope at the time this was written.
    public class MissionToast : MonoBehaviour
    {
        private const float DisplaySeconds = 6f;

        private GameObject _panel;
        private TMP_Text _text;
        private float _hideAtUnscaledTime = -1f;

        private ScenarioDefinition _trackedScenario;
        private readonly List<bool> _objectiveWasComplete = new List<bool>();
        private bool _outcomeShown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<MissionToast>() != null)
            {
                return;
            }

            var go = new GameObject("MissionToast");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<MissionToast>();
        }

        private void Awake()
        {
            BuildUi();
            _panel.SetActive(false);
        }

        private void Update()
        {
            ScenarioDefinition active = ScenarioManager.ActiveScenario;

            if (active != _trackedScenario)
            {
                TrackNewScenario(active);
            }
            else if (active != null)
            {
                CheckObjectiveCompletions();
                CheckOutcome();
            }

            if (_hideAtUnscaledTime >= 0f && Time.unscaledTime >= _hideAtUnscaledTime)
            {
                _panel.SetActive(false);
                _hideAtUnscaledTime = -1f;
            }
        }

        private void TrackNewScenario(ScenarioDefinition active)
        {
            _trackedScenario = active;
            _objectiveWasComplete.Clear();
            _outcomeShown = false;

            if (active == null)
            {
                return;
            }

            IReadOnlyList<MissionObjective> objectives = ScenarioManager.CurrentObjectives;
            if (objectives != null)
            {
                foreach (MissionObjective objective in objectives)
                {
                    _objectiveWasComplete.Add(objective.IsComplete());
                }
            }

            Show(active.FlavorText);
        }

        private void CheckObjectiveCompletions()
        {
            IReadOnlyList<MissionObjective> objectives = ScenarioManager.CurrentObjectives;
            if (objectives == null)
            {
                return;
            }

            for (int i = 0; i < objectives.Count && i < _objectiveWasComplete.Count; i++)
            {
                bool isComplete = objectives[i].IsComplete();
                if (isComplete && !_objectiveWasComplete[i] && !string.IsNullOrEmpty(objectives[i].CompleteText))
                {
                    Show(objectives[i].CompleteText);
                }
                _objectiveWasComplete[i] = isComplete;
            }
        }

        // Uses unscaled time throughout (Show/Update both key off
        // Time.unscaledTime) specifically so this still works at the
        // moment of Victory/Defeat - MatchManager.Declare sets
        // Time.timeScale = 0 right when the outcome fires, which would
        // freeze a scaled-time countdown before the player ever saw it.
        private void CheckOutcome()
        {
            if (_outcomeShown || MatchManager.Outcome == MatchOutcome.Ongoing)
            {
                return;
            }

            _outcomeShown = true;
            string text = MatchManager.Outcome == MatchOutcome.Victory
                ? _trackedScenario.VictoryText
                : _trackedScenario.DefeatText;

            if (!string.IsNullOrEmpty(text))
            {
                Show(text);
            }
        }

        private void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _text.text = message;
            _panel.SetActive(true);
            _hideAtUnscaledTime = Time.unscaledTime + DisplaySeconds;
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("MissionToastCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150; // above gameplay HUD (ObjectivePanel=20), below menu overlays (190+)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var boxImage = _panel.AddComponent<Image>();
            boxImage.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            var boxRect = _panel.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 1f);
            boxRect.anchorMax = new Vector2(0.5f, 1f);
            boxRect.pivot = new Vector2(0.5f, 1f);
            boxRect.sizeDelta = new Vector2(760f, 90f);
            boxRect.anchoredPosition = new Vector2(0f, -90f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_panel.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 20;
            _text.color = Color.white;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Match
{
    // Item 50: runtime driver for a scripted mission's objectives/triggers.
    // Self-installs via RuntimeInitializeOnLoadMethod (same pattern
    // SaveManager already uses) so no scene changes are needed. A plain
    // skirmish match never calls Begin(), so ActiveScenario stays null and
    // MatchManager's own elimination-based Evaluate() keeps running
    // exactly as it always has - this class is a pure addition, not a
    // replacement.
    public class ScenarioManager : MonoBehaviour
    {
        public static ScenarioDefinition ActiveScenario { get; private set; }

        private static ScenarioManager _instance;
        private List<MissionObjective> _objectives;
        private List<MissionTrigger> _triggers;
        private readonly HashSet<string> _firedTriggers = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<ScenarioManager>() != null)
            {
                return;
            }

            var go = new GameObject("ScenarioManager");
            _instance = go.AddComponent<ScenarioManager>();
            DontDestroyOnLoad(go);
        }

        // Called by CivilizationSetup.BeginScenarioMatch, before the
        // gated match content activates - objectives that spawn a scripted
        // target (see ScenarioRegistry) run their setup here, so that
        // target already exists by the time anything else in the match
        // starts ticking.
        public static void Begin(ScenarioDefinition scenario)
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ScenarioManager>();
            }

            ActiveScenario = scenario;
            _instance._objectives = scenario.BuildObjectives();
            _instance._triggers = scenario.BuildTriggers != null ? scenario.BuildTriggers() : new List<MissionTrigger>();
            _instance._firedTriggers.Clear();
        }

        // Called by CivilizationSetup on scene teardown, same "reset
        // static state on OnDestroy" convention CivilizationSetup.
        // HasMatchStarted already uses - a stale scenario shouldn't
        // survive into whatever runs next.
        public static void EndScenario()
        {
            ActiveScenario = null;
        }

        public static IReadOnlyList<MissionObjective> CurrentObjectives =>
            _instance != null ? (IReadOnlyList<MissionObjective>)_instance._objectives : null;

        private void Update()
        {
            if (ActiveScenario == null || _triggers == null)
            {
                return;
            }

            foreach (MissionTrigger trigger in _triggers)
            {
                if (_firedTriggers.Contains(trigger.Id))
                {
                    continue;
                }

                if (trigger.Condition())
                {
                    trigger.Fire();
                    _firedTriggers.Add(trigger.Id);
                }
            }
        }

        // Called by MatchManager.Evaluate() instead of its own elimination
        // check whenever a scenario is active. Failure takes priority over
        // completion - if any objective explicitly reports failure, the
        // mission is lost regardless of how close the others are.
        // Objectives without an IsFailed check (most of them) simply never
        // trigger this branch. Victory requires every objective complete.
        public static MatchOutcome EvaluateOutcome()
        {
            if (ActiveScenario == null || _instance == null || _instance._objectives == null)
            {
                return MatchOutcome.Ongoing;
            }

            foreach (MissionObjective objective in _instance._objectives)
            {
                if (objective.IsFailed != null && objective.IsFailed())
                {
                    return MatchOutcome.Defeat;
                }
            }

            foreach (MissionObjective objective in _instance._objectives)
            {
                if (!objective.IsComplete())
                {
                    return MatchOutcome.Ongoing;
                }
            }

            return MatchOutcome.Victory;
        }
    }
}

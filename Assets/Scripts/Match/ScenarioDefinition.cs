using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Match
{
    // Item 50: one scripted mission's fixed configuration - which
    // civilizations/map it uses, and factory functions for its objectives/
    // triggers. Functions, not pre-built lists: objectives/triggers close
    // over live match state (Building.All, ResourceStockpile, elapsed
    // time), so they have to be constructed fresh each time a mission
    // actually starts, not once at game boot when none of that state
    // exists yet.
    public class ScenarioDefinition
    {
        public string Id;
        public string Title;
        public string FlavorText;
        public CivilizationId PlayerCivilization;
        public CivilizationId AiCivilization;
        public MapId Map;
        public System.Func<List<MissionObjective>> BuildObjectives;
        public System.Func<List<MissionTrigger>> BuildTriggers;

        // Item 50 gap-close: optional narrative beats shown as a toast (see
        // MissionToast) when the match ends while this scenario is active -
        // null means that outcome shows no extra text beyond the existing
        // GameOverScreen. Deliberately not required on every mission, same
        // as CompleteText on MissionObjective - a mission author opts in
        // only where the extra line is worth writing.
        public string VictoryText;
        public string DefeatText;
    }
}

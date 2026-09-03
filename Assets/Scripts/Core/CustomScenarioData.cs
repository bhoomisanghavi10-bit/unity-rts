using System;
using System.Collections.Generic;
using KingdomsOfBharat.Match;

namespace KingdomsOfBharat.Core
{
    // Item 6 (Scenario Editor, heavy path session 1): a player-authored
    // scenario's saved state - civ/map choice plus starting placements.
    // Plain JsonUtility-serializable data, same convention MatchSaveData
    // already establishes (no Dictionary - JsonUtility can't serialize one).
    // Reuses UnitSaveData/BuildingSaveData directly rather than inventing
    // parallel placement types - a starting placement and a saved-mid-match
    // unit/building are the same shape (type string, faction, position),
    // so ScenarioEditorMenu/EntitySpawner and SaveManager's own restore path
    // share the exact same data shape.
    //
    // Heavy path session 2: objectives/triggers, authored in-game via
    // ScenarioEditorMenu's Objectives tab, reusing MissionCsvLoader's own
    // ObjectiveRow/TriggerRow types and BuildObjectivesFromRows/
    // BuildTriggersFromRows interpreter - the exact same small fixed
    // vocabulary (ObjectiveKind/TriggerKind) the light (CSV) path uses, just
    // authored through the UI instead of a text file. Empty (the default)
    // means this scenario has no scripted objectives at all - see
    // CivilizationSetup.BeginCustomScenarioMatch, which only calls
    // ScenarioManager.Begin when objectives.Count > 0 (an empty objective
    // list would otherwise read as "already won").
    [Serializable]
    public class CustomScenarioData
    {
        public string id;
        public string title;
        public int mapId;
        public int playerCivilization;
        public int aiCivilization;
        public List<UnitSaveData> units = new List<UnitSaveData>();
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
        public List<ObjectiveRow> objectives = new List<ObjectiveRow>();
        public List<TriggerRow> triggers = new List<TriggerRow>();
        public string victoryText;
        public string defeatText;
    }
}

using System;
using System.Collections.Generic;

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
    // v1 deliberately carries no objectives/triggers - a custom scenario
    // with no ScenarioManager.ActiveScenario just falls through to
    // MatchManager's existing elimination-based Conquest evaluation, same
    // as a normal skirmish. Authoring custom objectives/triggers is an
    // explicit follow-on session, not silently dropped.
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
    }
}

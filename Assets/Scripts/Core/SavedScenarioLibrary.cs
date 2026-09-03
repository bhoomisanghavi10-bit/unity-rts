using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Item 6 (Scenario Editor, heavy path session 3): the single source of
    // truth for where saved custom scenarios live on disk and how they're
    // read - previously duplicated inline inside ScenarioEditorMenu's own
    // ScenarioFolder/RefreshFileList/LoadFile. Extracted so
    // MissionSelectMenu's new saved-scenario browse list can list/load the
    // exact same files the editor itself saves, without a second
    // implementation of the same Directory.GetFiles/JsonUtility.FromJson
    // logic drifting out of sync - same "extract shared logic once" reason
    // EntitySpawner.cs was pulled out of SaveManager in session 1.
    public static class SavedScenarioLibrary
    {
        private const string ScenarioFolderName = "Scenarios";

        public static string ScenarioFolder => Path.Combine(Application.persistentDataPath, ScenarioFolderName);

        public static List<string> ListSavedScenarioNames()
        {
            var names = new List<string>();
            if (!Directory.Exists(ScenarioFolder))
            {
                return names;
            }

            foreach (string path in Directory.GetFiles(ScenarioFolder, "*.json"))
            {
                names.Add(Path.GetFileNameWithoutExtension(path));
            }

            names.Sort();
            return names;
        }

        public static CustomScenarioData Load(string name)
        {
            string path = Path.Combine(ScenarioFolder, name + ".json");
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<CustomScenarioData>(File.ReadAllText(path));
        }
    }
}

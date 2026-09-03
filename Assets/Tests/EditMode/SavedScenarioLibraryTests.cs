using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Item 6 (Scenario Editor, heavy path session 3). SavedScenarioLibrary
    // is the single source of truth for saved-scenario file I/O, extracted
    // out of ScenarioEditorMenu so MissionSelectMenu's new browse list
    // reads the exact same files the editor itself writes. Writes real
    // small JSON files into the real Application.persistentDataPath/
    // Scenarios folder (same location the production code uses) and
    // cleans them up in TearDown - there's no sandboxed alternative
    // location this project's save/load code supports.
    public class SavedScenarioLibraryTests
    {
        private readonly List<string> _writtenNames = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (string name in _writtenNames)
            {
                string path = Path.Combine(SavedScenarioLibrary.ScenarioFolder, name + ".json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            _writtenNames.Clear();
        }

        private void WriteScenario(string name, string title)
        {
            Directory.CreateDirectory(SavedScenarioLibrary.ScenarioFolder);
            var data = new CustomScenarioData { id = name, title = title };
            File.WriteAllText(Path.Combine(SavedScenarioLibrary.ScenarioFolder, name + ".json"), JsonUtility.ToJson(data));
            _writtenNames.Add(name);
        }

        [Test]
        public void ListSavedScenarioNames_ReturnsWrittenFiles_Sorted()
        {
            WriteScenario("zzz_test_scenario", "Z");
            WriteScenario("aaa_test_scenario", "A");

            List<string> names = SavedScenarioLibrary.ListSavedScenarioNames();

            int aIndex = names.IndexOf("aaa_test_scenario");
            int zIndex = names.IndexOf("zzz_test_scenario");
            Assert.GreaterOrEqual(aIndex, 0, "aaa_test_scenario must be listed.");
            Assert.GreaterOrEqual(zIndex, 0, "zzz_test_scenario must be listed.");
            Assert.Less(aIndex, zIndex, "Names must be sorted.");
        }

        [Test]
        public void Load_ExistingScenario_ReturnsCorrectData()
        {
            WriteScenario("test_load_scenario", "Load Test");

            CustomScenarioData loaded = SavedScenarioLibrary.Load("test_load_scenario");

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Load Test", loaded.title);
        }

        [Test]
        public void Load_MissingScenario_ReturnsNull()
        {
            Assert.IsNull(SavedScenarioLibrary.Load("this_scenario_does_not_exist_at_all"));
        }
    }
}

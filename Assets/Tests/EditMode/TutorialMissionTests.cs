using System.Collections.Generic;
using NUnit.Framework;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 40 (Tutorial content): "The First Lesson" - a new
    // CSV-authored mission (Assets/Resources/Data/Missions/mission_*.csv),
    // riding the existing MissionObjective/MissionTrigger/MissionCsvLoader
    // system per the item's own roadmap text ("no new system needed").
    // Unlike MissionCsvLoaderTests.cs/MissionRowsTests.cs (which drive the
    // interpreter with small in-memory CSV strings), this exercises the
    // real Resources-loaded files directly - the one thing those tests
    // don't cover and the one place a real formatting mistake in the new
    // rows (a bad quote, a stray comma, a mis-typed enum value) would
    // actually surface.
    //
    // Deliberately does NOT call BuildObjectives() on the loaded mission -
    // its DestroyScriptedTarget objective calls BarracksFactory.Place,
    // which NREs outside Play mode (a pre-existing limitation
    // MissionCsvLoaderTests.cs's own
    // ObjectiveKind_DestroyScriptedTarget_ParsesIntoOneObjectiveWithGivenText
    // test already documents and works around the same way). Real
    // objective/trigger behavior is covered by live UnityMCP verification
    // instead (see docs/SESSION_LOG.md) - this test covers what EditMode
    // safely can: the real files parse into the right shape.
    public class TutorialMissionTests
    {
        [Test]
        public void LoadAll_IncludesTheFirstLesson_WithExpectedCivsAndMap()
        {
            List<ScenarioDefinition> missions = MissionCsvLoader.LoadAll();

            ScenarioDefinition tutorial = missions.Find(m => m.Id == "tutorial_basics");
            Assert.IsNotNull(tutorial, "The real mission_definitions.csv must contain a 'tutorial_basics' row.");
            Assert.AreEqual("The First Lesson", tutorial.Title);
            Assert.AreEqual(CivilizationId.Maurya, tutorial.PlayerCivilization);
            Assert.AreEqual(CivilizationId.Maratha, tutorial.AiCivilization);
            Assert.AreEqual(MapId.RiverValley, tutorial.Map);
            Assert.IsFalse(string.IsNullOrEmpty(tutorial.VictoryText));
            Assert.IsFalse(string.IsNullOrEmpty(tutorial.DefeatText));
        }

        // Reads the real objectives CSV row-by-row (bypassing
        // BuildObjectives entirely, for the reason explained on the class)
        // to confirm all 5 lesson rows are present with the exact Kind
        // vocabulary the item's design calls for - one of each existing
        // ObjectiveKind, teaching gather/build/grow/hold/attack.
        [Test]
        public void TutorialObjectiveRows_CoverOneOfEachObjectiveKind()
        {
            string csv = ReadRealMissionCsv("Data/Missions/mission_objectives");
            List<Dictionary<string, string>> rows = MissionCsvLoader.ReadCsv(csv);

            var tutorialKinds = new List<string>();
            foreach (Dictionary<string, string> row in rows)
            {
                if (row["MissionId"] == "tutorial_basics")
                {
                    tutorialKinds.Add(row["Kind"]);
                }
            }

            Assert.AreEqual(5, tutorialKinds.Count, "The First Lesson should have exactly 5 objectives.");
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "ResourceThreshold",
                    "BuildingCountThreshold",
                    "PopulationThreshold",
                    "SurviveSeconds",
                    "DestroyScriptedTarget",
                },
                tutorialKinds);
        }

        [Test]
        public void TutorialTriggerRow_GrantsGoldEarly()
        {
            string csv = ReadRealMissionCsv("Data/Missions/mission_triggers");
            List<Dictionary<string, string>> rows = MissionCsvLoader.ReadCsv(csv);

            Dictionary<string, string> trigger = rows.Find(r => r["MissionId"] == "tutorial_basics");
            Assert.IsNotNull(trigger, "The real mission_triggers.csv must contain a row for tutorial_basics.");
            Assert.AreEqual("GrantResourceAtTime", trigger["Kind"]);
            Assert.AreEqual("Gold", trigger["Param1"]);
        }

        private static string ReadRealMissionCsv(string resourcePath)
        {
            UnityEngine.TextAsset asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(resourcePath);
            Assert.IsNotNull(asset, $"Expected a real TextAsset at Resources/{resourcePath}.");
            return asset.text;
        }
    }
}

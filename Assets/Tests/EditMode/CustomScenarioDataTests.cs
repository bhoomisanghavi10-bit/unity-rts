using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Item 6 (Scenario Editor, heavy path session 1). CustomScenarioData is
    // the ScenarioEditorMenu's save-file shape - same JsonUtility-
    // serializable convention MatchSaveData already proves works
    // (Assets/Scripts/Core/SaveData.cs), reusing UnitSaveData/
    // BuildingSaveData directly rather than inventing parallel types.
    public class CustomScenarioDataTests
    {
        [Test]
        public void RoundTrips_ThroughJsonUtility_WithPlacementsIntact()
        {
            var data = new CustomScenarioData
            {
                id = "test_scenario",
                title = "Test Scenario",
                mapId = (int)MapId.Highlands,
                playerCivilization = (int)CivilizationId.Rajput,
                aiCivilization = (int)CivilizationId.Maurya,
            };
            data.buildings.Add(new BuildingSaveData
            {
                buildingType = "TownCenter",
                faction = (int)FactionId.Player,
                position = new Vector3(1f, 0f, 2f),
                health = 500f,
                isComplete = true,
            });
            data.units.Add(new UnitSaveData
            {
                unitType = "Worker",
                faction = (int)FactionId.Player,
                position = new Vector3(3f, 0f, 4f),
                health = 25f,
                stance = -1,
            });

            string json = JsonUtility.ToJson(data);
            CustomScenarioData restored = JsonUtility.FromJson<CustomScenarioData>(json);

            Assert.AreEqual(data.id, restored.id);
            Assert.AreEqual(data.title, restored.title);
            Assert.AreEqual(data.mapId, restored.mapId);
            Assert.AreEqual(data.playerCivilization, restored.playerCivilization);
            Assert.AreEqual(data.aiCivilization, restored.aiCivilization);

            Assert.AreEqual(1, restored.buildings.Count);
            Assert.AreEqual("TownCenter", restored.buildings[0].buildingType);
            Assert.AreEqual((int)FactionId.Player, restored.buildings[0].faction);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), restored.buildings[0].position);

            Assert.AreEqual(1, restored.units.Count);
            Assert.AreEqual("Worker", restored.units[0].unitType);
            Assert.AreEqual(new Vector3(3f, 0f, 4f), restored.units[0].position);
        }

        [Test]
        public void RoundTrips_WithNoPlacements_EmptyListsNotNull()
        {
            var data = new CustomScenarioData { id = "empty", title = "Empty" };

            string json = JsonUtility.ToJson(data);
            CustomScenarioData restored = JsonUtility.FromJson<CustomScenarioData>(json);

            Assert.IsNotNull(restored.units);
            Assert.IsNotNull(restored.buildings);
            Assert.AreEqual(0, restored.units.Count);
            Assert.AreEqual(0, restored.buildings.Count);
        }
    }
}

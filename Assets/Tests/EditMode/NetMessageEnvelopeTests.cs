using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Tests
{
    // Item 6 (Scenario Editor, heavy path session 5). NetMessageEnvelope.
    // scenarioJson carries a whole CustomScenarioData over the wire so two
    // LAN peers can play a custom scenario together (LanMatchMenu.cs) -
    // same "embed as its own JSON string" convention snapshotJson already
    // establishes for ResyncSnapshot. This proves the field round-trips
    // through JsonUtility with a real, non-trivial CustomScenarioData
    // (placements + an objective) intact, the same way
    // CustomScenarioDataTests.cs already proves the type itself round-trips
    // on its own.
    public class NetMessageEnvelopeTests
    {
        [Test]
        public void ScenarioJson_RoundTrips_WithPlacementsAndObjectiveIntact()
        {
            var scenario = new CustomScenarioData
            {
                id = "lan_test",
                title = "LAN Test Scenario",
                playerCivilization = (int)CivilizationId.Chola,
                aiCivilization = (int)CivilizationId.Vijayanagara,
            };
            scenario.buildings.Add(new BuildingSaveData
            {
                buildingType = "TownCenter",
                faction = (int)FactionId.Player,
                position = new Vector3(1f, 0f, 2f),
                health = 500f,
                isComplete = true,
            });
            scenario.objectives.Add(new ObjectiveRow
            {
                kind = ObjectiveKind.PopulationThreshold,
                param1 = "2",
                description = "Reach 2 population",
            });

            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.HostHello,
                hostCivilization = (int)CivilizationId.Chola,
                scenarioJson = JsonUtility.ToJson(scenario),
            };

            string wireJson = JsonUtility.ToJson(envelope);
            NetMessageEnvelope received = JsonUtility.FromJson<NetMessageEnvelope>(wireJson);

            Assert.IsFalse(string.IsNullOrEmpty(received.scenarioJson));
            CustomScenarioData restored = JsonUtility.FromJson<CustomScenarioData>(received.scenarioJson);

            Assert.AreEqual("LAN Test Scenario", restored.title);
            Assert.AreEqual(1, restored.buildings.Count);
            Assert.AreEqual("TownCenter", restored.buildings[0].buildingType);
            Assert.AreEqual(1, restored.objectives.Count);
            Assert.AreEqual(ObjectiveKind.PopulationThreshold, restored.objectives[0].kind);
        }

        [Test]
        public void ScenarioJson_DefaultsToEmpty_MeaningNormalSkirmish()
        {
            var envelope = new NetMessageEnvelope { kind = NetMessageKind.HostHello };

            NetMessageEnvelope received = JsonUtility.FromJson<NetMessageEnvelope>(JsonUtility.ToJson(envelope));

            Assert.IsTrue(string.IsNullOrEmpty(received.scenarioJson));
        }
    }
}

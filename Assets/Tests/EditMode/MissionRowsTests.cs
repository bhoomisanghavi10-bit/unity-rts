using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Item 6 (Scenario Editor, heavy path session 2). MissionCsvLoader.
    // BuildObjectivesFromRows/BuildTriggersFromRows is the interpreter the
    // CSV (light) path and ScenarioEditorMenu's in-game Objectives tab both
    // share - MissionCsvLoaderTests.cs already proves the CSV path builds
    // correct rows and hands them to this interpreter; these tests drive
    // the interpreter directly with hand-built ObjectiveRow/TriggerRow
    // objects (no CSV/Dictionary involved), proving it behaves identically
    // reached either way.
    public class MissionRowsTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }

                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }

                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private ResourceStockpile CreateStockpile(FactionId faction)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile_" + faction).AddComponent<ResourceStockpile>();
            stockpile.Configure(faction);
            return stockpile;
        }

        private Unit CreateUnit(FactionId faction)
        {
            GameObject go = CreateGameObject("Unit");
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            go.AddComponent<FactionMember>().Configure(faction);
            return unit;
        }

        [Test]
        public void BuildObjectivesFromRows_PopulationThreshold_IsCompleteReflectsRealPopulation()
        {
            CreateUnit(FactionId.Player);
            CreateUnit(FactionId.Player);

            var rows = new List<ObjectiveRow>
            {
                new ObjectiveRow { kind = ObjectiveKind.PopulationThreshold, param1 = "2", param2 = "Player", description = "Reach 2 population" },
            };

            List<MissionObjective> built = MissionCsvLoader.BuildObjectivesFromRows(rows);

            Assert.AreEqual(1, built.Count);
            Assert.IsTrue(built[0].IsComplete(), "2 real Player units exist - a PopulationThreshold of 2 must read complete.");
        }

        [Test]
        public void BuildObjectivesFromRows_ResourceThreshold_IsCompleteReflectsRealStockpile()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            stockpile.Add(ResourceType.Wood, 40f);

            var rows = new List<ObjectiveRow>
            {
                new ObjectiveRow { kind = ObjectiveKind.ResourceThreshold, param1 = "Wood", param2 = "50", param3 = "Player", description = "Gather 50 Wood" },
            };

            MissionObjective objective = MissionCsvLoader.BuildObjectivesFromRows(rows)[0];

            Assert.IsFalse(objective.IsComplete(), "40 Wood is below the 50 threshold.");
            stockpile.Add(ResourceType.Wood, 10f);
            Assert.IsTrue(objective.IsComplete(), "50 Wood must now satisfy the threshold.");
        }

        [Test]
        public void BuildObjectivesFromRows_SurviveSeconds_ProducesAWorkingClosure()
        {
            var rows = new List<ObjectiveRow>
            {
                new ObjectiveRow { kind = ObjectiveKind.SurviveSeconds, param1 = "999999", description = "Survive" },
            };

            MissionObjective objective = MissionCsvLoader.BuildObjectivesFromRows(rows)[0];

            Assert.IsFalse(objective.IsComplete(), "A 999999-second survive objective must not be complete immediately.");
        }

        [Test]
        public void BuildObjectivesFromRows_EmptyList_ProducesEmptyObjectiveList()
        {
            // The exact case CivilizationSetup.BeginCustomScenarioMatch's
            // own objectives.Count > 0 gate exists to avoid ever reaching:
            // an empty objective list means ScenarioManager.EvaluateOutcome
            // would read as "every objective complete" (an empty foreach
            // never hits its Ongoing branch) - proving BuildObjectivesFromRows
            // itself faithfully returns an empty list (not, say, a single
            // default objective) confirms the gate's premise holds.
            List<MissionObjective> built = MissionCsvLoader.BuildObjectivesFromRows(new List<ObjectiveRow>());
            Assert.AreEqual(0, built.Count);
        }

        [Test]
        public void BuildTriggersFromRows_GrantResourceAtTime_FiresRealResourceGrant()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            var rows = new List<TriggerRow>
            {
                new TriggerRow { triggerId = "grant1", kind = TriggerKind.GrantResourceAtTime, param1 = "Gold", param2 = "100", param3 = "0", param4 = "Player" },
            };

            MissionTrigger trigger = MissionCsvLoader.BuildTriggersFromRows(rows)[0];

            Assert.IsTrue(trigger.Condition(), "atSeconds=0 must already be satisfied.");
            trigger.Fire();
            Assert.AreEqual(100f, stockpile.GetTotal(ResourceType.Gold), 0.01f);
        }

        [Test]
        public void BuildTriggersFromRows_RepeatingGrantResource_ExpandsToOneTriggerPerRepeat()
        {
            var rows = new List<TriggerRow>
            {
                new TriggerRow { triggerId = "repeat1", kind = TriggerKind.RepeatingGrantResource, param1 = "Gold", param2 = "40", param3 = "30", param4 = "3", param5 = "Player" },
            };

            List<MissionTrigger> built = MissionCsvLoader.BuildTriggersFromRows(rows);

            Assert.AreEqual(3, built.Count, "count=3 must expand into 3 discrete one-shot triggers.");
            CollectionAssert.AllItemsAreUnique(built.ConvertAll(t => t.Id));
        }
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 33: Town Bell - one-click "send every Worker home"
    // convenience. Uses FactionId.Enemy2 to stay isolated from
    // Building.All/Unit.All entries any other test in the same run might
    // leave behind for Player/Enemy, same convention AgeUpRequirementTests
    // already establishes.
    public class TownBellTests
    {
        private const FactionId TestFaction = FactionId.Enemy2;
        private const FactionId OtherFaction = FactionId.Player;
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                // Building.All/Unit.All registration normally happens via
                // OnEnable, which isn't guaranteed to have fired
                // synchronously within a single EditMode test - same
                // gotcha AgeUpRequirementTests/BuildingAttackerTests
                // already document. Managed directly here instead.
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
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

        private GarrisonPoint CreateTownCenter(FactionId faction, Vector3 position, int capacity)
        {
            GameObject go = CreateGameObject("TownCenter");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            TownCenter tc = go.AddComponent<TownCenter>();
            if (!Building.All.Contains(tc))
            {
                Building.All.Add(tc);
            }
            GarrisonPoint garrisonPoint = go.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(capacity);
            return garrisonPoint;
        }

        private GameObject CreateWorker(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("Worker");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<Gatherer>();
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            return go;
        }

        // A non-Worker land unit (e.g. Soldier) - has GarrisonSeeker like
        // every land unit, but no Gatherer, so Town Bell must not touch it.
        private GameObject CreateSoldier(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("Soldier");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<GarrisonSeeker>();
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            return go;
        }

        [Test]
        public void Ring_ReturnsZero_WhenFactionHasNoTownCenter()
        {
            CreateWorker(TestFaction, Vector3.zero);

            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(0, sent);
        }

        [Test]
        public void Ring_ReturnsZero_WhenFactionHasNoWorkers()
        {
            CreateTownCenter(TestFaction, Vector3.zero, capacity: 4);

            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(0, sent);
        }

        [Test]
        public void Ring_SendsEachWorker_UpToTownCenterCapacity()
        {
            CreateTownCenter(TestFaction, Vector3.zero, capacity: 2);
            CreateWorker(TestFaction, new Vector3(5f, 0f, 0f));
            CreateWorker(TestFaction, new Vector3(-5f, 0f, 0f));

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(2, sent);
        }

        [Test]
        public void Ring_DoesNotExceedCombinedCapacityAcrossTownCenters()
        {
            CreateTownCenter(TestFaction, new Vector3(0f, 0f, 0f), capacity: 1);
            CreateTownCenter(TestFaction, new Vector3(100f, 0f, 0f), capacity: 1);
            CreateWorker(TestFaction, new Vector3(1f, 0f, 0f));
            CreateWorker(TestFaction, new Vector3(2f, 0f, 0f));
            CreateWorker(TestFaction, new Vector3(3f, 0f, 0f));

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(2, sent, "Only 2 total garrison slots exist across both TownCenters - the 3rd worker has nowhere to go.");
        }

        [Test]
        public void Ring_IgnoresUnitsWithoutGatherer()
        {
            CreateTownCenter(TestFaction, Vector3.zero, capacity: 4);
            CreateSoldier(TestFaction, new Vector3(1f, 0f, 0f));

            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(0, sent, "A Soldier has no Gatherer - Town Bell should never touch a combat unit.");
        }

        [Test]
        public void Ring_IgnoresOtherFactionsWorkersAndTownCenters()
        {
            CreateTownCenter(OtherFaction, Vector3.zero, capacity: 4);
            CreateWorker(OtherFaction, new Vector3(1f, 0f, 0f));
            CreateTownCenter(TestFaction, new Vector3(50f, 0f, 0f), capacity: 4);

            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(0, sent, "TestFaction has a TownCenter but no workers of its own - it must not pull in another faction's worker.");
        }

        [Test]
        public void Ring_CancelsInProgressGathering()
        {
            CreateTownCenter(TestFaction, new Vector3(20f, 0f, 0f), capacity: 1);
            GameObject workerGo = CreateWorker(TestFaction, Vector3.zero);
            Gatherer gatherer = workerGo.GetComponent<Gatherer>();
            GameObject nodeGo = CreateGameObject("Node");
            nodeGo.transform.position = new Vector3(3f, 0f, 0f);
            ResourceNode node = nodeGo.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Wood, 100f);

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            gatherer.GatherFrom(node);
            Assert.IsTrue(gatherer.IsWorking, "Sanity check: the worker should be mid-gather before Town Bell rings.");

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            int sent = TownBell.Ring(TestFaction);

            Assert.AreEqual(1, sent);
            Assert.IsFalse(gatherer.IsWorking, "Town Bell should cancel an in-progress gather task, not leave the worker gathering while also walking home.");
        }
    }
}

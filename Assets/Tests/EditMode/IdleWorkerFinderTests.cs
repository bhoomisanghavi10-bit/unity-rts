using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.UI;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 34: idle-worker indicator. Uses FactionId.Enemy2 to stay
    // isolated from Unit.All entries any other test in the same run might
    // leave behind for Player/Enemy, same convention TownBellTests already
    // establishes.
    public class IdleWorkerFinderTests
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
                // Unit.All registration normally happens via OnEnable, which
                // isn't guaranteed to have fired synchronously within a
                // single EditMode test - same gotcha TownBellTests/
                // BuildingAttackerTests already document.
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

        private GameObject CreateWorker(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("Worker");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Gatherer>();
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            return go;
        }

        // A non-Worker unit (e.g. Soldier) - no Gatherer, so it must never
        // count as an idle worker no matter how idle it actually is.
        private GameObject CreateSoldier(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("Soldier");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            return go;
        }

        [Test]
        public void FindAll_ReturnsEmpty_WhenNoUnitsExist()
        {
            List<Unit> idle = IdleWorkerFinder.FindAll(TestFaction);

            Assert.AreEqual(0, idle.Count);
        }

        [Test]
        public void FindAll_ReturnsWorker_WithNoCurrentTask()
        {
            GameObject workerGo = CreateWorker(TestFaction, Vector3.zero);

            List<Unit> idle = IdleWorkerFinder.FindAll(TestFaction);

            Assert.AreEqual(1, idle.Count);
            Assert.AreSame(workerGo.GetComponent<Unit>(), idle[0]);
        }

        [Test]
        public void FindAll_ExcludesWorker_MidGather()
        {
            GameObject workerGo = CreateWorker(TestFaction, Vector3.zero);
            Gatherer gatherer = workerGo.GetComponent<Gatherer>();
            GameObject nodeGo = CreateGameObject("Node");
            nodeGo.transform.position = new Vector3(3f, 0f, 0f);
            ResourceNode node = nodeGo.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Wood, 100f);

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            gatherer.GatherFrom(node);
            Assert.IsTrue(gatherer.IsWorking, "Sanity check: the worker should be mid-gather (walking to the node counts as working).");

            List<Unit> idle = IdleWorkerFinder.FindAll(TestFaction);

            Assert.AreEqual(0, idle.Count, "A worker walking to or harvesting a resource node is not idle.");
        }

        [Test]
        public void FindAll_ExcludesUnitsWithoutGatherer()
        {
            CreateSoldier(TestFaction, Vector3.zero);

            List<Unit> idle = IdleWorkerFinder.FindAll(TestFaction);

            Assert.AreEqual(0, idle.Count, "A Soldier has no Gatherer - it must never count as an idle worker.");
        }

        [Test]
        public void FindAll_ExcludesOtherFactionsWorkers()
        {
            CreateWorker(OtherFaction, Vector3.zero);

            List<Unit> idle = IdleWorkerFinder.FindAll(TestFaction);

            Assert.AreEqual(0, idle.Count);
        }

        [Test]
        public void FindAll_ReturnsMultipleIdleWorkers_ForTheSameFaction()
        {
            CreateWorker(TestFaction, new Vector3(1f, 0f, 0f));
            CreateWorker(TestFaction, new Vector3(2f, 0f, 0f));
            CreateWorker(OtherFaction, new Vector3(3f, 0f, 0f));

            List<Unit> idle = IdleWorkerFinder.FindAll(TestFaction);

            Assert.AreEqual(2, idle.Count);
        }
    }
}

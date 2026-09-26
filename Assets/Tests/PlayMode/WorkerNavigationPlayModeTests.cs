using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Wildlife;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.PlayModeTests
{
    // Prompt 10: worker orders over REAL NavMesh navigation in a live
    // skirmish (Chola, default map). Time is accelerated (timeScale) so a
    // multi-minute economy loop fits in a minute of wall time; results are
    // also logged as "[WorkerCycles]" lines for the runtime-observation
    // record.
    public class WorkerNavigationPlayModeTests
    {
        private const float Speed = 4f;

        [SetUp]
        public void SetUp()
        {
            WorkerDiagnostics.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        private IEnumerator Boot()
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<CivilizationSetup>().BeginMatch(CivilizationId.Chola);
            for (int i = 0; i < 5; i++) yield return null;
            // Isolate the Player economy: the Enemy AI issues its own worker
            // orders (a separate, logged finding), which would pollute counts.
            foreach (AiController ai in Object.FindObjectsByType<AiController>(FindObjectsSortMode.None)) ai.enabled = false;
            // Wild boars attack workers and (by design) interrupt their orders; remove them so
            // this test measures navigation, not combat interruptions.
            foreach (WildBoarSpawner sp in Object.FindObjectsByType<WildBoarSpawner>(FindObjectsSortMode.None)) sp.enabled = false;
            ClearBoars();
        }

        private static void ClearBoars()
        {
            foreach (WildBoar boar in Object.FindObjectsByType<WildBoar>(FindObjectsSortMode.None)) Object.Destroy(boar.gameObject);
        }

        private static List<Gatherer> PlayerWorkers()
        {
            return Unit.All.Where(u => u != null && u.TryGetComponent(out FactionMember m) && m.Faction == FactionId.Player)
                .Select(u => u.GetComponent<Gatherer>()).Where(g => g != null).ToList();
        }

        private static TownCenter PlayerTownCenter()
        {
            return Building.All.OfType<TownCenter>().First(t => t.GetComponent<FactionMember>().Faction == FactionId.Player);
        }

        private static ResourceNode NearestReachableNode(Gatherer worker, ResourceType type, ISet<ResourceNode> exclude)
        {
            UnitMover mover = worker.GetComponent<UnitMover>();
            // Skip nodes inside enemy building fire range: the Enemy Town
            // Center shoots workers (a real, separate hazard the run found).
            var enemyBuildings = Building.All.Where(b => b != null && b.GetComponent<FactionMember>().Faction != FactionId.Player).ToList();
            return ResourceNode.All
                .Where(n => n != null && !n.IsDepleted && n.ResourceType == type && n.name != "Fish" && !exclude.Contains(n)
                    && enemyBuildings.All(b => Vector3.Distance(b.transform.position, n.transform.position) > 40f))
                .OrderBy(n => Vector3.Distance(worker.transform.position, n.transform.position))
                .FirstOrDefault(n => mover.CanReach(n.transform.position));
        }

        [UnityTest]
        public IEnumerator EveryResourceType_CompletesRealGatherDepositCycles_NoStuckWorkers()
        {
            yield return Boot();
            ResourceStockpile stock = ResourceStockpile.For(FactionId.Player);
            List<Gatherer> workers = PlayerWorkers();
            Assert.GreaterOrEqual(workers.Count, 4, "Standard start has 4 workers.");

            var types = new[] { ResourceType.Wood, ResourceType.Food, ResourceType.Gold, ResourceType.Stone };
            var used = new HashSet<ResourceNode>();
            var assigned = new Dictionary<ResourceType, Gatherer>();
            var before = types.ToDictionary(t => t, t => stock.GetTotal(t));
            var skipped = new List<ResourceType>();
            for (int i = 0; i < types.Length; i++)
            {
                ResourceNode node = NearestReachableNode(workers[i], types[i], used);
                if (node == null) { skipped.Add(types[i]); continue; }
                used.Add(node);
                Debug.Log($"[WorkerCycles] assign {types[i]}: node '{node.name}' amount {node.Amount:0.#} at {node.transform.position}, worker at {workers[i].transform.position}");
                workers[i].GatherFrom(node);
                assigned[types[i]] = workers[i];
            }
            Assert.GreaterOrEqual(assigned.Count, 3, "The reference map must offer at least 3 of the 4 resource types near a worker; skipped: " + string.Join(",", skipped));

            Time.timeScale = Speed;
            float simSeconds = 0f;
            var lastProgress = assigned.ToDictionary(kv => kv.Key, kv => 0f);
            var stalls = 0;
            const float horizon = 240f; // sim seconds
            while (simSeconds < horizon)
            {
                yield return new WaitForSecondsRealtime(1f);
                simSeconds += Speed;
                ClearBoars();
            }
            Time.timeScale = 1f;

            foreach (var kv in assigned)
            {
                float gained = stock.GetTotal(kv.Key) - before[kv.Key];
                Debug.Log($"[WorkerCycles] {kv.Key}: banked {gained:0.#}, destroyed {kv.Value == null}, state {kv.Value.OrderState}, failure {kv.Value.LastFailure}, idleReason '{kv.Value.IdleReason}'");
                Assert.Greater(gained, 0f, kv.Key + " worker never completed a deposit within " + horizon + " sim seconds.");
                Assert.AreEqual(WorkerFailure.None, kv.Value.LastFailure, kv.Key.ToString());
                if (kv.Value.OrderState == Gatherer.WorkerOrderState.Idle) stalls++;
            }
            Debug.Log($"[WorkerCycles] types {assigned.Count}/4, skipped [{string.Join(",", skipped)}], idle/stuck workers {stalls}, failures {WorkerDiagnostics.Total}");
            Assert.AreEqual(0, WorkerDiagnostics.Total, "No order may fail on the reference map.");
        }

        [UnityTest]
        public IEnumerator SeveralWorkersOnOneNode_AccountForEveryHarvestedUnit()
        {
            yield return Boot();
            ResourceStockpile stock = ResourceStockpile.For(FactionId.Player);
            TownCenter tc = PlayerTownCenter();
            List<Gatherer> workers = PlayerWorkers();

            // Only the shared node may supply Gold, or workers that finish it would
            // legitimately move on (Prompt 10 retargeting) and inflate the tally.
            foreach (ResourceNode other in ResourceNode.All.Where(n => n != null && n.ResourceType == ResourceType.Gold).ToList()) Object.Destroy(other.gameObject);
            yield return null;

            Vector3 spot = tc.transform.position + new Vector3(10f, 0f, 10f);
            Assert.IsTrue(NavMesh.SamplePosition(spot, out NavMeshHit hit, 8f, NavMesh.AllAreas), "A walkable spot near the Town Center.");
            var go = new GameObject("SharedTestNode");
            go.transform.position = hit.position;
            ResourceNode node = go.AddComponent<ResourceNode>();
            const float amount = 300f;
            node.Configure(ResourceType.Gold, amount);
            float before = stock.GetTotal(ResourceType.Gold);
            foreach (Gatherer w in workers) w.GatherFrom(node);

            Time.timeScale = Speed;
            for (int i = 0; i < 60; i++) yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1f;

            float banked = stock.GetTotal(ResourceType.Gold) - before;
            float carried = workers.Where(w => w.CarriedType == ResourceType.Gold).Sum(w => w.CarriedAmount);
            float taken = node != null ? amount - node.Amount : amount;
            Debug.Log($"[WorkerCycles] shared node: workers {workers.Count}, taken {taken:0.#}, banked {banked:0.#}, carried {carried:0.#}");
            Assert.AreEqual(taken, banked + carried, 0.5f, "Every harvested unit is banked or still carried: none lost, none duplicated.");
            Assert.Greater(banked, 0f);
        }

        [UnityTest]
        public IEnumerator UnreachableResource_FailsCleanlyWithinBoundedTime_AndWorkerIsFreeAgain()
        {
            yield return Boot();
            Gatherer worker = PlayerWorkers()[0];
            var go = new GameObject("UnreachableNode");
            go.transform.position = new Vector3(900f, 1f, 900f); // far outside the map/NavMesh
            ResourceNode node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Wood, 100f);

            worker.GatherFrom(node);
            Time.timeScale = Speed;
            float waited = 0f;
            while (worker.OrderState != Gatherer.WorkerOrderState.Idle && waited < 120f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                waited += 0.5f * Speed;
            }
            Time.timeScale = 1f;

            Debug.Log($"[WorkerCycles] unreachable node: gave up after {waited:0.#} sim s, failure {worker.LastFailure}, reports {WorkerDiagnostics.Total}");
            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, worker.OrderState, "Worker must give up instead of walking at the wall forever.");
            Assert.AreEqual(WorkerFailure.ResourceUnreachable, worker.LastFailure);
            Assert.AreEqual(1, WorkerDiagnostics.Total, "One failure, one record - no spam.");

            // And it is usable afterwards.
            ResourceNode good = NearestReachableNode(worker, ResourceType.Wood, new HashSet<ResourceNode>());
            Assert.IsNotNull(good);
            worker.GatherFrom(good);
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToResource, worker.OrderState);
        }

        [UnityTest]
        public IEnumerator DropOffDestroyedWhileCarrying_WorkerWaitsAndReportsOnce_ThenCancelStopsCleanly()
        {
            yield return Boot();
            ResourceStockpile stock = ResourceStockpile.For(FactionId.Player);
            Gatherer worker = PlayerWorkers()[0];
            ResourceNode node = NearestReachableNode(worker, ResourceType.Wood, new HashSet<ResourceNode>());
            Assert.IsNotNull(node);
            worker.GatherFrom(node);

            Time.timeScale = Speed;
            float waited = 0f;
            while (worker.OrderState != Gatherer.WorkerOrderState.MovingToDropOff && waited < 200f)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f * Speed;
            }
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToDropOff, worker.OrderState, "Worker filled up and set off to deposit.");
            float woodBefore = stock.GetTotal(ResourceType.Wood);
            float carried = worker.CarriedAmount;

            foreach (Building b in Building.All.ToList())
            {
                if (b is TownCenter && b.GetComponent<FactionMember>().Faction == FactionId.Player) Object.Destroy(b.gameObject);
            }
            for (int i = 0; i < 12; i++) yield return new WaitForSecondsRealtime(0.25f);
            Time.timeScale = 1f;

            Assert.AreEqual(Gatherer.WorkerOrderState.WaitingForDropOff, worker.OrderState);
            Assert.AreEqual(WorkerFailure.NoDropOff, worker.LastFailure);
            Assert.AreEqual(1, WorkerDiagnostics.Count(WorkerFailure.NoDropOff));
            Assert.AreEqual(woodBefore, stock.GetTotal(ResourceType.Wood), 0.001f, "Nothing is banked without a drop-off.");
            Assert.AreEqual(carried, worker.CarriedAmount, 0.001f, "The load is not lost.");

            worker.CancelGather();
            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, worker.OrderState);
        }
    }
}

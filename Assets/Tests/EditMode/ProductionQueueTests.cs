using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Prompt 9: shared production queue - enqueue/spend-once, insufficient
    // resources, FIFO order, cancel/refund, population, destruction, reset,
    // and the Town Center/Barracks migrations. Enemy2 keeps registries
    // isolated from other fixtures, same convention as ScoreProgressTests.
    public class ProductionQueueTests
    {
        private const FactionId F = FactionId.Enemy2;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            AgeProgress.Initialize(F, AgeId.Ancient);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null) continue;
                if (go.TryGetComponent(out Unit unit)) Unit.All.Remove(unit);
                if (go.TryGetComponent(out Building building)) Building.All.Remove(building);
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private ResourceStockpile Stockpile(float each = 1000f)
        {
            GameObject go = new GameObject("Stockpile");
            _spawned.Add(go);
            ResourceStockpile s = go.AddComponent<ResourceStockpile>();
            s.Configure(F);
            foreach (ResourceType t in new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Gold, ResourceType.Stone })
            {
                s.SetTotal(t, each);
            }
            return s;
        }

        private Barracks NewBarracks()
        {
            GameObject go = new GameObject("Barracks");
            _spawned.Add(go);
            go.AddComponent<FactionMember>().Configure(F);
            return go.AddComponent<Barracks>();
        }

        [Test]
        public void Enqueue_DeductsCostExactlyOnce_AndRecordsItem()
        {
            ResourceStockpile s = Stockpile();
            var q = new ProductionQueue();

            Assert.IsTrue(q.TryEnqueue(F, 1, "A", 5f, (ResourceType.Food, 50f), (ResourceType.Gold, 20f)));

            Assert.AreEqual(950f, s.GetTotal(ResourceType.Food));
            Assert.AreEqual(980f, s.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1, q.Count);
            q.Tick(F, 1f, _ => { });
            Assert.AreEqual(950f, s.GetTotal(ResourceType.Food), "Progress must never charge again.");
        }

        [Test]
        public void Enqueue_InsufficientResources_SpendsNothingAndReportsWhy()
        {
            ResourceStockpile s = Stockpile(30f);
            var q = new ProductionQueue();

            Assert.IsFalse(q.TryEnqueue(F, 1, "A", 5f, (ResourceType.Gold, 20f), (ResourceType.Food, 50f)));

            Assert.AreEqual(30f, s.GetTotal(ResourceType.Gold), "Nothing may be spent on a refused request, even for the affordable part.");
            Assert.AreEqual(0, q.Count);
            StringAssert.Contains("Food", q.LastFailure);
        }

        [Test]
        public void Queue_RunsFifo_OnlyHeadCountsDown()
        {
            Stockpile();
            var q = new ProductionQueue();
            q.TryEnqueue(F, 1, "first", 2f);
            q.TryEnqueue(F, 2, "second", 2f);
            var done = new List<int>();

            q.Tick(F, 1f, i => done.Add(i.Kind));
            Assert.AreEqual(2f, q.Items[1].Remaining, "Second item must not count down while first is active.");
            q.Tick(F, 1f, i => done.Add(i.Kind));
            q.Tick(F, 2f, i => done.Add(i.Kind));

            CollectionAssert.AreEqual(new[] { 1, 2 }, done);
            Assert.IsTrue(q.IsEmpty);
        }

        [Test]
        public void Queue_RejectsWhenFull()
        {
            Stockpile();
            var q = new ProductionQueue(capacity: 2);
            q.TryEnqueue(F, 1, "a", 1f);
            q.TryEnqueue(F, 1, "b", 1f);

            Assert.IsFalse(q.TryEnqueue(F, 1, "c", 1f));
            StringAssert.Contains("full", q.LastFailure);
        }

        [Test]
        public void Cancel_ActiveAndQueued_RefundExactCostOfThatItem()
        {
            ResourceStockpile s = Stockpile();
            var q = new ProductionQueue();
            q.TryEnqueue(F, 1, "a", 5f, (ResourceType.Food, 50f));
            q.TryEnqueue(F, 2, "b", 5f, (ResourceType.Wood, 30f));
            q.Tick(F, 2f, _ => { });

            Assert.IsTrue(q.CancelLast(F));
            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Wood));
            Assert.IsTrue(q.Cancel(F, 0));
            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Food));
            Assert.IsTrue(q.IsEmpty);
            Assert.IsFalse(q.Cancel(F, 0), "Cancelling an empty queue must be a harmless no-op.");
        }

        [Test]
        public void Cancel_HonoursConfiguredRefundFraction()
        {
            ResourceStockpile s = Stockpile();
            var q = new ProductionQueue(refundFraction: 0.5f);
            q.TryEnqueue(F, 1, "a", 5f, (ResourceType.Food, 100f));

            q.Cancel(F, 0);

            Assert.AreEqual(950f, s.GetTotal(ResourceType.Food));
        }

        [Test]
        public void Cancel_ThenTickingDoesNotSpawn()
        {
            Stockpile();
            var q = new ProductionQueue();
            q.TryEnqueue(F, 1, "a", 1f);
            q.Cancel(F, 0);
            int spawned = 0;

            q.Tick(F, 10f, _ => spawned++);

            Assert.AreEqual(0, spawned);
        }

        [Test]
        public void Population_QueuedItemsCountAgainstTheCap()
        {
            Stockpile();
            int cap = Population.Cap(F);
            var q = new ProductionQueue(capacity: 50);
            for (int i = 0; i < cap; i++)
            {
                Assert.IsTrue(q.TryEnqueue(F, 1, "u", 1f), "item " + i);
            }

            Assert.IsFalse(q.TryEnqueue(F, 1, "u", 1f));
            StringAssert.Contains("Population", q.LastFailure);
        }

        [Test]
        public void FinishedItem_WaitsWhenPopulationFull_ThenSpawnsWhenRoomAppears()
        {
            Stockpile();
            var q = new ProductionQueue();
            q.TryEnqueue(F, 1, "u", 1f);
            // Fill the cap with real registered units (OnEnable is not
            // guaranteed synchronous in EditMode, so register directly).
            var fillers = new List<Unit>();
            for (int i = 0; i < Population.Cap(F); i++)
            {
                GameObject go = new GameObject("Filler");
                _spawned.Add(go);
                go.AddComponent<FactionMember>().Configure(F);
                Unit u = go.AddComponent<Unit>();
                if (!Unit.All.Contains(u)) Unit.All.Add(u);
                fillers.Add(u);
            }
            int spawned = 0;

            q.Tick(F, 5f, _ => spawned++);
            Assert.AreEqual(0, spawned);
            Assert.IsTrue(q.Blocked);
            Assert.AreEqual(1, q.Count, "Blocked item must stay queued, not vanish.");

            Unit.All.Remove(fillers[0]);
            q.Tick(F, 0.1f, _ => spawned++);

            Assert.AreEqual(1, spawned);
            Assert.IsFalse(q.Blocked);
        }

        [Test]
        public void Clear_DropsEverythingWithoutRefund()
        {
            ResourceStockpile s = Stockpile();
            var q = new ProductionQueue();
            q.TryEnqueue(F, 1, "a", 5f, (ResourceType.Food, 50f));

            q.Clear();

            Assert.IsTrue(q.IsEmpty);
            Assert.AreEqual(950f, s.GetTotal(ResourceType.Food), "Reset is not a cancel; it must not refund.");
        }

        [Test]
        public void Barracks_QueuesMultipleItems_ChargingEachOnce_AndCancelRestoresExactly()
        {
            ResourceStockpile s = Stockpile();
            Barracks b = NewBarracks();
            // IsComplete is true when there's no ConstructionSite.
            b.RequestTrain();
            b.RequestTrainArcher();
            float food = s.GetTotal(ResourceType.Food);
            float gold = s.GetTotal(ResourceType.Gold);
            Assert.AreEqual(2, b.Queue.Count);
            Assert.AreEqual(1000f - 50f - 40f, food);
            Assert.AreEqual(1000f - 20f - 35f, gold);

            b.Queue.CancelLast(F);
            b.Queue.CancelLast(F);

            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Gold));
            Assert.IsFalse(b.IsTraining);
        }

        [Test]
        public void Barracks_InsufficientResources_QueuesNothing()
        {
            Stockpile(10f);
            Barracks b = NewBarracks();

            b.RequestTrain();

            Assert.IsFalse(b.IsTraining);
            Assert.IsNotNull(b.LastTrainFailure);
        }

        [Test]
        public void Barracks_Destroyed_RefundsEverythingStillQueued()
        {
            ResourceStockpile s = Stockpile();
            Barracks b = NewBarracks();
            b.RequestTrain();
            b.RequestTrainCavalry();
            Assert.Less(s.GetTotal(ResourceType.Food), 1000f);

            b.RefundQueueForDestruction();

            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Gold));
            Assert.IsFalse(b.IsTraining);
        }

        [Test]
        public void TownCenter_QueuesWorkers_CancelRefundsFood()
        {
            ResourceStockpile s = Stockpile();
            GameObject go = new GameObject("TC");
            _spawned.Add(go);
            go.AddComponent<FactionMember>().Configure(F);
            TownCenter tc = go.AddComponent<TownCenter>();

            tc.RequestTrain();
            tc.RequestTrain();
            Assert.AreEqual(900f, s.GetTotal(ResourceType.Food));
            Assert.AreEqual(2, tc.Queue.Count);

            tc.Queue.CancelLast(F);
            tc.Queue.CancelLast(F);

            Assert.AreEqual(1000f, s.GetTotal(ResourceType.Food));
        }

        [Test]
        public void RestoreQueue_RebuildsItemsSoCancelStillRefunds()
        {
            ResourceStockpile s = Stockpile();
            Barracks b = NewBarracks();
            b.RestoreQueue(new[]
            {
                new ProductionItem { Kind = 0, Label = "Soldier", CostTypes = new[] { ResourceType.Food }, CostAmounts = new[] { 50f }, Total = 5f, Remaining = 2f },
            });

            Assert.IsTrue(b.IsTraining);
            Assert.AreEqual(2f, b.TrainingRemaining, 0.001f);
            b.Queue.Cancel(F, 0);
            Assert.AreEqual(1050f, s.GetTotal(ResourceType.Food));
        }
    }
}

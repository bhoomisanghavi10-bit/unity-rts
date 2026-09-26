using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Prompt 10: worker gather/deposit/construct state machine and its
    // failure recovery, driven with injected time (Gatherer.Step). Nothing
    // here needs a NavMesh; real navigation is covered by
    // WorkerNavigationPlayModeTests. Enemy2 isolates from other fixtures.
    public class WorkerOrderTests
    {
        private const FactionId F = FactionId.Enemy2;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private bool _priorIgnore;

        [SetUp]
        public void SetUp()
        {
            _priorIgnore = LogAssert.ignoreFailingMessages;
            UnitMover.SkipMovesOffNavMesh = true;
            WorkerDiagnostics.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null) continue;
                if (go.TryGetComponent(out Building b)) Building.All.Remove(b);
                if (go.TryGetComponent(out ResourceNode n)) ResourceNode.All.Remove(n);
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            UnitMover.SkipMovesOffNavMesh = false;
            LogAssert.ignoreFailingMessages = _priorIgnore;
        }

        private GameObject Make(string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            _spawned.Add(go);
            return go;
        }

        private ResourceStockpile Stockpile()
        {
            GameObject go = Make("Stockpile", Vector3.zero);
            ResourceStockpile s = go.AddComponent<ResourceStockpile>();
            s.Configure(F);
            return s;
        }

        private Gatherer Worker(Vector3 pos)
        {
            GameObject go = Make("Worker", pos);
            go.AddComponent<FactionMember>().Configure(F);
            return go.AddComponent<Gatherer>();
        }

        private ResourceNode Node(ResourceType type, float amount, Vector3 pos)
        {
            GameObject go = Make("Node", pos);
            ResourceNode n = go.AddComponent<ResourceNode>();
            n.Configure(type, amount);
            if (!ResourceNode.All.Contains(n)) ResourceNode.All.Add(n);
            return n;
        }

        private TownCenter DropOff(Vector3 pos)
        {
            GameObject go = Make("TC", pos);
            go.AddComponent<FactionMember>().Configure(F);
            TownCenter tc = go.AddComponent<TownCenter>();
            if (!Building.All.Contains(tc)) Building.All.Add(tc);
            return tc;
        }

        private static void Steps(Gatherer g, int n, float dt)
        {
            for (int i = 0; i < n; i++) g.Step(dt);
        }

        [TestCase(ResourceType.Food)]
        [TestCase(ResourceType.Wood)]
        [TestCase(ResourceType.Gold)]
        [TestCase(ResourceType.Stone)]
        public void FullCycle_EveryResourceType_DepositsExactlyOnceIntoMatchingStockpile(ResourceType type)
        {
            ResourceStockpile stock = Stockpile();
            Gatherer g = Worker(new Vector3(0, 0, 1));
            Node(type, 100f, Vector3.zero);
            DropOff(new Vector3(0, 0, 2));

            g.GatherFrom(g.GetComponent<Gatherer>() == null ? null : ResourceNode.All[ResourceNode.All.Count - 1]);
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToResource, g.OrderState);
            g.Step(0.01f);
            Assert.AreEqual(Gatherer.WorkerOrderState.Gathering, g.OrderState);

            Steps(g, 4, 0.5f); // 4 x 2.5 = 10 = carry capacity
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToDropOff, g.OrderState);
            g.Step(0.01f); // arrives (drop-off within range) and deposits
            Assert.AreEqual(10f, stock.GetTotal(type), 0.001f);
            Assert.AreEqual(0f, g.CarriedAmount);
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToResource, g.OrderState, "Resumes the same resource after depositing.");

            g.Step(0.01f);
            Assert.AreEqual(10f, stock.GetTotal(type), 0.001f, "A second step must not deposit again.");
        }

        [Test]
        public void SeveralWorkersSharingANode_DrawDownItWithoutOverHarvestingOrLosingResource()
        {
            ResourceStockpile stock = Stockpile();
            DropOff(new Vector3(0, 0, 2));
            ResourceNode node = Node(ResourceType.Wood, 30f, Vector3.zero);
            var workers = new List<Gatherer>();
            for (int i = 0; i < 3; i++) { Gatherer w = Worker(new Vector3(0.1f * i, 0, 1)); w.GatherFrom(node); workers.Add(w); }

            for (int i = 0; i < 200; i++) foreach (Gatherer w in workers) w.Step(0.25f);

            float held = 0f; foreach (Gatherer w in workers) held += w.CarriedAmount;
            Assert.AreEqual(30f, stock.GetTotal(ResourceType.Wood) + held, 0.01f, "Every unit harvested is either banked or still carried - none lost or duplicated.");
        }

        [Test]
        public void DepletedNode_WorkerContinuesOnNearbySameTypeNode()
        {
            ResourceStockpile stock = Stockpile();
            DropOff(new Vector3(0, 0, 2));
            Gatherer g = Worker(new Vector3(0, 0, 1));
            ResourceNode first = Node(ResourceType.Wood, 10f, Vector3.zero);
            Node(ResourceType.Wood, 40f, new Vector3(1, 0, 1)); // agents do not move in EditMode, so keep it in range
            Node(ResourceType.Gold, 40f, new Vector3(0.5f, 0, 1)); // wrong type: must never be chosen
            g.GatherFrom(first);

            for (int i = 0; i < 60; i++) g.Step(0.25f);

            Assert.Greater(stock.GetTotal(ResourceType.Wood), 10f, "Worker should have moved on to the second Wood node.");
            Assert.AreEqual(0f, stock.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void DepletedNode_NoReplacement_GoesIdleAfterBankingLoad()
        {
            ResourceStockpile stock = Stockpile();
            DropOff(new Vector3(0, 0, 2));
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 4f, Vector3.zero));

            for (int i = 0; i < 20; i++) g.Step(0.25f);

            Assert.AreEqual(4f, stock.GetTotal(ResourceType.Wood), 0.001f);
            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState);
            Assert.AreEqual(WorkerFailure.None, g.LastFailure, "Running out is normal, not a failure.");
        }

        [Test]
        public void Cancel_WhileCarrying_StopsCleanlyAndKeepsLoad()
        {
            ResourceStockpile stock = Stockpile();
            DropOff(new Vector3(0, 0, 2));
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            Steps(g, 3, 0.5f);
            Assert.Greater(g.CarriedAmount, 0f);

            g.CancelGather();
            Steps(g, 10, 0.5f);

            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState, "A cancelled worker must not walk on to a drop-off.");
            Assert.AreEqual(0f, stock.GetTotal(ResourceType.Wood));
            Assert.Greater(g.CarriedAmount, 0f);
        }

        [Test]
        public void Cancel_DuringDropOffTrip_DoesNotStrandWorker()
        {
            Stockpile();
            DropOff(new Vector3(0, 0, 40)); // far away: worker is mid-trip
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            Steps(g, 5, 0.5f);
            g.Step(0.01f);
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToDropOff, g.OrderState);

            g.CancelGather(); // e.g. the player right-clicked open ground

            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState);
            Assert.IsFalse(g.IsWorking);
        }

        [Test]
        public void NewOrderForDifferentResource_WhileCarrying_DeliversOldLoadFirst()
        {
            ResourceStockpile stock = Stockpile();
            DropOff(new Vector3(0, 0, 2));
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            Steps(g, 2, 0.5f);
            float wood = g.CarriedAmount;
            Assert.Greater(wood, 0f);
            g.CancelGather();
            ResourceNode gold = Node(ResourceType.Gold, 100f, new Vector3(1, 0, 0));

            g.GatherFrom(gold);
            g.Step(0.01f);

            Assert.AreEqual(wood, stock.GetTotal(ResourceType.Wood), 0.001f, "The wood load must be banked as wood, not relabelled as gold.");
            Assert.AreEqual(0f, stock.GetTotal(ResourceType.Gold));
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToResource, g.OrderState);
        }

        [Test]
        public void NoDropOff_WaitsExplicitly_ReportsOnce_AndResumesWhenOneAppears()
        {
            ResourceStockpile stock = Stockpile();
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            Steps(g, 5, 0.5f);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Worker\] NoDropOff"));

            Steps(g, 30, 0.5f);

            Assert.AreEqual(Gatherer.WorkerOrderState.WaitingForDropOff, g.OrderState);
            Assert.AreEqual(WorkerFailure.NoDropOff, g.LastFailure);
            Assert.AreEqual(1, WorkerDiagnostics.Count(WorkerFailure.NoDropOff), "Reported once, not every frame.");

            DropOff(new Vector3(0, 0, 2));
            Steps(g, 6, 0.5f);

            Assert.Greater(stock.GetTotal(ResourceType.Wood), 0f);
            Assert.AreEqual(WorkerFailure.None, g.LastFailure);
        }

        [Test]
        public void DropOffDestroyedMidTrip_RoutesToAnotherEligibleDropOff()
        {
            ResourceStockpile stock = Stockpile();
            TownCenter first = DropOff(new Vector3(0, 0, 30));
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            Steps(g, 6, 0.5f);
            g.Step(0.01f);
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToDropOff, g.OrderState);

            Building.All.Remove(first);
            Object.DestroyImmediate(first.gameObject);
            DropOff(new Vector3(0, 0, 2));
            g.Step(0.01f);
            g.Step(0.01f);

            Assert.Greater(stock.GetTotal(ResourceType.Wood), 0f, "Load must reach the surviving drop-off.");
        }

        [Test]
        public void UnreachableResource_RecoversThenFailsCleanly_WithoutSpam()
        {
            Stockpile();
            var workers = new List<Gatherer>();
            ResourceNode far = Node(ResourceType.Wood, 100f, new Vector3(0, 0, 500));
            for (int i = 0; i < 5; i++) { Gatherer w = Worker(Vector3.zero); w.GatherFrom(far); workers.Add(w); }
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Worker\] ResourceUnreachable"));

            // Workers never get closer (no agent movement in EditMode).
            for (int i = 0; i < 40; i++) foreach (Gatherer w in workers) w.Step(1f);

            foreach (Gatherer w in workers)
            {
                Assert.AreEqual(Gatherer.WorkerOrderState.Idle, w.OrderState);
                Assert.AreEqual(WorkerFailure.ResourceUnreachable, w.LastFailure);
            }
            Assert.AreEqual(5, WorkerDiagnostics.Count(WorkerFailure.ResourceUnreachable));
            Assert.AreEqual(5, WorkerDiagnostics.Total);
        }

        [Test]
        public void Unreachable_WithAlternativeNode_SwitchesInsteadOfFailing()
        {
            Stockpile();
            Gatherer g = Worker(Vector3.zero);
            ResourceNode bad = Node(ResourceType.Wood, 100f, new Vector3(0, 0, 500));
            g.GatherFrom(bad);
            // Alternative sits within retarget radius of the ORIGINAL target.
            Node(ResourceType.Wood, 100f, new Vector3(0, 0, 505));

            for (int i = 0; i < 6; i++) g.Step(1f);

            Assert.AreNotEqual(WorkerFailure.ResourceUnreachable, g.LastFailure);
            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToResource, g.OrderState);
        }

        [Test]
        public void LargeNode_IsReachedAtItsEdge_NotItsCentre()
        {
            Stockpile();
            Gatherer g = Worker(new Vector3(6f, 0, 0));
            ResourceNode n = Node(ResourceType.Gold, 100f, Vector3.zero);
            n.gameObject.AddComponent<BoxCollider>().size = new Vector3(10, 2, 10); // edge at x=5
            Physics.SyncTransforms();
            g.GatherFrom(n);

            g.Step(0.01f);

            Assert.AreEqual(Gatherer.WorkerOrderState.Gathering, g.OrderState, "1 unit from the edge is within interaction range even though the centre is 6 away.");
        }

        [Test]
        public void ResetOrder_ClearsCarriedTargetAndState()
        {
            Stockpile();
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            Steps(g, 3, 0.5f);

            g.ResetOrder();

            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState);
            Assert.AreEqual(0f, g.CarriedAmount);
            g.Step(0.5f);
            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState, "No stale target may revive the order.");
        }

        [Test]
        public void StuckWatchdog_FiresOnlyWithoutProgress()
        {
            var w = new StuckWatchdog();
            for (int i = 0; i < 20; i++)
            {
                Assert.IsFalse(w.Tick(1f, 20f - i), "Steady progress is never stuck.");
            }

            w.Reset();
            Assert.IsFalse(w.Tick(1f, 10f));
            Assert.IsFalse(w.Tick(1f, 10f));
            Assert.IsFalse(w.Tick(1f, 10f));
            Assert.IsTrue(w.Tick(1f, 10f));
        }

        [Test]
        public void Diagnostics_RateLimitsConsoleButCountsEveryFailure()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Worker\] ResourceUnreachable"));

            for (int i = 0; i < 25; i++)
            {
                WorkerDiagnostics.Report(WorkerFailure.ResourceUnreachable, "w" + i, "test");
            }

            Assert.AreEqual(25, WorkerDiagnostics.Count(WorkerFailure.ResourceUnreachable));
            Assert.LessOrEqual(WorkerDiagnostics.Recent.Count, 20);
        }

        [Test]
        public void Builder_SiteDestroyedWhileBuilding_ClearsBuildingFlag()
        {
            GameObject siteGo = Make("Site", Vector3.zero);
            ConstructionSite site = siteGo.AddComponent<ConstructionSite>();
            site.Configure(10f);
            GameObject go = Make("Builder", Vector3.zero);
            Builder builder = go.AddComponent<Builder>();
            builder.BuildAt(site);
            MethodInfo update = typeof(Builder).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(builder, null);
            Assert.IsTrue(builder.IsBuilding);

            Object.DestroyImmediate(siteGo);
            update.Invoke(builder, null);

            Assert.IsFalse(builder.IsBuilding, "A vanished site must not leave the builder flagged as building.");
        }

        [Test]
        public void AfterBeingAttacked_WorkerResumesItsResourceOnceThreatIsGone()
        {
            Stockpile();
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.SetCombatResponse(CombatResponse.Flee);
            g.gameObject.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            ResourceNode node = Node(ResourceType.Wood, 100f, Vector3.zero);
            g.GatherFrom(node);
            g.Step(0.01f);
            Attackable attacker = Make("Boar", new Vector3(0, 0, 2)).AddComponent<Attackable>();
            attacker.Configure(30f);

            g.HandleDamaged(attacker);
            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState);
            g.Step(0.1f);
            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState, "Stays out while the attacker is alive and near.");

            attacker.RestoreHealth(0f);
            g.Step(0.1f);

            Assert.AreEqual(Gatherer.WorkerOrderState.MovingToResource, g.OrderState, "Back to work after the threat is dead.");
        }

        [Test]
        public void ExplicitCancel_AfterAttack_PreventsResume()
        {
            Stockpile();
            Gatherer g = Worker(new Vector3(0, 0, 1));
            g.SetCombatResponse(CombatResponse.Flee);
            g.gameObject.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            g.GatherFrom(Node(ResourceType.Wood, 100f, Vector3.zero));
            g.Step(0.01f);
            Attackable attacker = Make("Boar", new Vector3(0, 0, 2)).AddComponent<Attackable>();
            attacker.Configure(30f);
            g.HandleDamaged(attacker);

            g.CancelGather(); // player gave another order
            attacker.RestoreHealth(0f);
            g.Step(0.1f);

            Assert.AreEqual(Gatherer.WorkerOrderState.Idle, g.OrderState);
        }
    }
}

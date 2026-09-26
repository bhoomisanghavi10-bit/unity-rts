using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.UI;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.PlayModeTests
{
    // Prompt 9: production queue through the real HUD in a live skirmish.
    // Buttons are the real scene BuildMenu buttons (onClick.Invoke, the same
    // path a mouse click takes, so orders go through CommandBus with its
    // input delay), the building is selected through the real
    // SelectionManager, and results are read from real Unit.All/stockpile.
    public class ProductionQueuePlayModeTests
    {
        private static object Field(object o, string name)
        {
            return o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(o);
        }

        private static int Workers(FactionId f)
        {
            int n = 0;
            foreach (Unit u in Unit.All)
            {
                if (u != null && u.TryGetComponent(out FactionMember m) && m.Faction == f && u.name.Contains("Worker")) n++;
            }
            return n;
        }

        private IEnumerator StartMatch(System.Action<TownCenter, BuildMenu, SelectionManager> body)
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<CivilizationSetup>().BeginMatch(CivilizationId.Chola);
            yield return null;
            yield return null;

            TownCenter tc = null;
            foreach (Building b in Building.All)
            {
                if (b is TownCenter t && t.TryGetComponent(out FactionMember m) && m.Faction == FactionId.Player) tc = t;
            }
            Assert.IsNotNull(tc, "Player Town Center must exist in a standard skirmish.");
            SelectionManager sel = Object.FindFirstObjectByType<SelectionManager>();
            typeof(SelectionManager).GetMethod("SelectBuilding", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sel, new object[] { tc });
            body(tc, Object.FindFirstObjectByType<BuildMenu>(), sel);
        }

        [UnityTest]
        public IEnumerator HudTrainQueueCancelRefund_NoLeakedSpendOrDuplicateUnits()
        {
            TownCenter tc = null; BuildMenu menu = null;
            yield return StartMatch((t, m, s) => { tc = t; menu = m; });
            ResourceStockpile stock = ResourceStockpile.For(FactionId.Player);
            stock.SetTotal(ResourceType.Food, 500f);
            int workersBefore = Workers(FactionId.Player);
            yield return null; yield return null; // let BuildMenu.Update react to the selection

            Button train = (Button)Field(menu, "workerButton");
            Button cancel = (Button)Field(menu, "_cancelQueueButton");
            Assert.IsTrue(cancel.gameObject.activeSelf == false, "Cancel must be hidden while nothing is queued.");

            for (int i = 0; i < 3; i++) train.onClick.Invoke();
            yield return new WaitForSeconds(1f); // > CommandBus input delay
            Assert.AreEqual(3, tc.Queue.Count, "Three clicks should queue three Workers.");
            Assert.AreEqual(350f, stock.GetTotal(ResourceType.Food), 0.01f, "Each Worker charged exactly once.");
            yield return null;
            Assert.IsTrue(cancel.gameObject.activeSelf, "Cancel must show once something is queued.");

            cancel.onClick.Invoke();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(2, tc.Queue.Count);
            Assert.AreEqual(400f, stock.GetTotal(ResourceType.Food), 0.01f, "Cancelling the last item refunds exactly its cost.");

            // Let the remaining two finish; exactly two Workers must appear.
            yield return new WaitForSeconds(20f);
            Assert.IsTrue(tc.Queue.IsEmpty);
            Assert.AreEqual(workersBefore + 2, Workers(FactionId.Player), "Only the two uncancelled items may produce units.");
            Assert.AreEqual(400f, stock.GetTotal(ResourceType.Food), 0.01f, "Nothing may be charged or refunded again.");
        }

        [UnityTest]
        public IEnumerator SaveLoad_RestoresQueuedAndActiveItems_AndCancelStillRefunds()
        {
            TownCenter tc = null;
            yield return StartMatch((t, m, s) => tc = t);
            ResourceStockpile stock = ResourceStockpile.For(FactionId.Player);
            stock.SetTotal(ResourceType.Food, 500f);
            tc.RequestTrain(); tc.RequestTrain(); tc.RequestTrain();
            Assert.AreEqual(3, tc.Queue.Count);
            Assert.IsTrue(Multiplayer.NetworkId.TryGetId(tc.GetComponent<Building>(), out int id));
            yield return new WaitForSeconds(2f);
            float activeRemaining = tc.Queue.Active.Remaining;
            float food = stock.GetTotal(ResourceType.Food);

            var capture = typeof(SaveManager).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
            var apply = typeof(SaveManager).GetMethod("ApplySnapshotToRunningMatch", BindingFlags.Static | BindingFlags.NonPublic);
            var data = JsonUtility.FromJson<MatchSaveData>(JsonUtility.ToJson((MatchSaveData)capture.Invoke(null, null)));
            apply.Invoke(null, new object[] { data });
            yield return null; yield return null;

            Assert.IsTrue(Multiplayer.NetworkId.TryResolveBuilding(id, out Building restored));
            TownCenter r = (TownCenter)restored;
            Assert.AreEqual(3, r.Queue.Count, "Active and queued items must both survive save/load.");
            Assert.AreEqual(activeRemaining, r.Queue.Active.Remaining, 0.6f, "Active progress must survive (within the frames since capture).");
            Assert.AreEqual(food, stock.GetTotal(ResourceType.Food), 0.01f, "Loading must not re-charge the queue.");
            r.Queue.CancelLast(FactionId.Player);
            Assert.AreEqual(food + 50f, stock.GetTotal(ResourceType.Food), 0.01f, "Cost is remembered across load, so cancel still refunds.");
        }

        [UnityTest]
        public IEnumerator DestroyedBuilding_RefundsQueue_AndRematchStartsEmpty()
        {
            TownCenter tc = null;
            yield return StartMatch((t, m, s) => tc = t);
            ResourceStockpile stock = ResourceStockpile.For(FactionId.Player);
            stock.SetTotal(ResourceType.Food, 500f);
            tc.RequestTrain(); tc.RequestTrain();
            Assert.AreEqual(400f, stock.GetTotal(ResourceType.Food), 0.01f);

            Object.Destroy(tc.gameObject);
            yield return null; yield return null;
            Assert.AreEqual(500f, stock.GetTotal(ResourceType.Food), 0.01f, "Queued Workers are refunded when the building is destroyed.");

            // Rematch: reload the scene like GameOverScreen's Play Again.
            yield return StartMatch((t, m, s) => tc = t);
            Assert.IsTrue(tc.Queue.IsEmpty, "A fresh match must start with empty queues.");
            Assert.IsFalse(tc.IsTraining);
        }
    }
}

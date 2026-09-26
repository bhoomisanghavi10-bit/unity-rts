using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.UI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.PlayModeTests
{
    // Prompt 12, code-level verification in ONE process: a live match is
    // started as two humans, the process is switched to the second slot's
    // perspective, and a "remote" command envelope for the first slot is fed
    // through the real receive path. This is NOT a two-process test.
    public class LanPerspectivePlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            NetworkMatch.End();
        }

        private IEnumerator StartTwoHumanMatch()
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<CivilizationSetup>().BeginMatch(
                MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha, seed: 21, secondSlotIsHuman: true));
            for (int i = 0; i < 6; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator SecondSlotPerspective_HudFogAndVictoryFollowTheLocalFaction()
        {
            yield return StartTwoHumanMatch();
            foreach (var ai in Object.FindObjectsByType<AiController>(FindObjectsSortMode.None)) Assert.IsFalse(ai.enabled);

            // This process now plays the SECOND slot (Enemy).
            typeof(NetworkMatch).GetMethod("SetForTests", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { FactionId.Enemy, FactionId.Player, true });
            NetworkMatch.OnRemoteTickSeen(100000); // never stall the sim clock
            ResourceStockpile.For(FactionId.Enemy).SetTotal(ResourceType.Food, 777f);
            ResourceStockpile.For(FactionId.Player).SetTotal(ResourceType.Food, 111f);
            yield return null; yield return null;

            ResourceHUD hud = Object.FindFirstObjectByType<ResourceHUD>();
            object foodLabel = typeof(ResourceHUD).GetField("foodLabel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            string shown = (string)foodLabel.GetType().GetProperty("text").GetValue(foodLabel);
            Assert.AreEqual("777", shown, "HUD must show the LOCAL (second-slot) player's stockpile.");

            Assert.IsFalse(FogOfWarManager.IsFogged(FactionId.Enemy));
            Assert.IsTrue(FogOfWarManager.IsFogged(FactionId.Player));
            Assert.AreEqual(MatchOutcome_Ongoing(), MatchOutcomeNow(), "Both sides have forces: still ongoing from the local view.");
        }

        private static object MatchOutcome_Ongoing() => KingdomsOfBharat.Match.MatchOutcome.Ongoing;
        private static object MatchOutcomeNow()
        {
            return typeof(KingdomsOfBharat.Match.MatchManager).GetMethod("EvaluateSkirmishOutcome", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { false });
        }

        [UnityTest]
        public IEnumerator RemoteGatherCommand_ResolvesRealIds_AndReachesExecution()
        {
            yield return StartTwoHumanMatch();
            Gatherer worker = Unit.All.Where(u => u != null && u.GetComponent<FactionMember>().Faction == FactionId.Player)
                .Select(u => u.GetComponent<Gatherer>()).First(g => g != null);
            ResourceNode node = ResourceNode.All.First(n => n != null && !n.IsDepleted);
            Assert.IsTrue(NetworkId.TryGetId(node, out _), "Resource nodes must carry a NetworkId in a live match.");
            Assert.IsTrue(NetworkId.TryGetId(worker.GetComponent<Unit>(), out _));

            // This process is the second slot; the Player slot is "remote".
            typeof(NetworkMatch).GetMethod("SetForTests", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { FactionId.Enemy, FactionId.Player, true });
            NetworkMatch.OnRemoteTickSeen(100000);

            NetMessageEnvelope wire = JsonUtility.FromJson<NetMessageEnvelope>(JsonUtility.ToJson(
                CommandSerializer.ForGather(SimClock.CurrentTick + CommandBus.InputDelayTicks, FactionId.Player, worker.GetComponent<Unit>(), node)));
            wire.faction = (int)FactionId.Player;
            wire.seq = 1;
            typeof(NetworkDriver).GetMethod("HandleCommand", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { wire });

            for (int i = 0; i < 20 && worker.OrderState == Gatherer.WorkerOrderState.Idle; i++) yield return new WaitForSeconds(0.25f);

            Assert.AreNotEqual(Gatherer.WorkerOrderState.Idle, worker.OrderState, "The received gather order must execute on this peer's worker.");
            Assert.AreEqual(0, NetworkDiagnostics.Count(NetworkIssue.UnresolvedTarget));
        }
    }
}

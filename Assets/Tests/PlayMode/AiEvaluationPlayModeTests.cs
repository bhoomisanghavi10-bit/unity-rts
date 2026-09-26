using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.AI;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.PlayModeTests
{
    // Prompt 13: seeded AI evaluation on ONE reference matchup - River
    // Valley, Player Chola (idle, human slot) vs AI Maratha, fixed seed.
    // Time is accelerated; wild boars are switched off so their random
    // attacks do not pollute the measurements. Every run logs "[AiEval]"
    // lines used for the before/after table in the audit.
    public class AiEvaluationPlayModeTests
    {
        private const int Seed = 20260926;
        private const float Speed = 5f;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        private static AiController Ai() =>
            Object.FindObjectsByType<AiController>(FindObjectsSortMode.None).First(a => a.enabled);

        private IEnumerator Boot()
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<CivilizationSetup>().BeginMatch(
                MatchConfiguration.Create(MapId.SkirmishMedium, CivilizationId.Chola, CivilizationId.Maratha, seed: Seed));
            for (int i = 0; i < 5; i++) yield return null;
            foreach (var sp in Object.FindObjectsByType<WildBoarSpawner>(FindObjectsSortMode.None)) sp.enabled = false;
            foreach (var b in Object.FindObjectsByType<WildBoar>(FindObjectsSortMode.None)) Object.Destroy(b.gameObject);
        }

        private static IEnumerator RunSim(float simSeconds)
        {
            Time.timeScale = Speed;
            float elapsed = 0f;
            while (elapsed < simSeconds)
            {
                yield return new WaitForSecondsRealtime(1f);
                elapsed += Speed;
            }
            Time.timeScale = 1f;
        }

        private static int Workers(FactionId f) => Unit.All.Count(u => u != null && u.GetComponent<FactionMember>().Faction == f && u.GetComponent<Gatherer>() != null);
        private static int Military(FactionId f) => Unit.All.Count(u => u != null && u.GetComponent<FactionMember>().Faction == f && u.GetComponent<Gatherer>() == null && u.GetComponent<MeleeAttacker>() != null);
        private static Building Find<T>(FactionId f) where T : Building => Building.All.OfType<T>().FirstOrDefault(b => b != null && b.GetComponent<FactionMember>().Faction == f);

        [UnityTest]
        public IEnumerator Opening_TenSimulatedMinutes_ReportsTelemetry()
        {
            yield return Boot();
            AiTelemetry t = AiTelemetry.For(FactionId.Enemy);
            Assert.IsNotNull(t);

            yield return RunSim(120f);
            Farm farm = Building.All.OfType<Farm>().FirstOrDefault(f => f.GetComponent<FactionMember>().Faction == FactionId.Enemy);
            if (farm != null)
            {
                var fws = Object.FindObjectsByType<FarmWorker>(FindObjectsSortMode.None).Where(w => w.Farm == farm).ToList();
                Debug.Log($"[AiEval] t=120 farm at {farm.transform.position}, remaining {farm.RemainingFood:0}, complete {farm.IsComplete}, farm workers {fws.Count} farming {fws.Count(w => w.IsFarming)}, distances {string.Join(",", fws.Select(w => Vector3.Distance(w.transform.position, farm.transform.position).ToString("0.0")))}, agents {string.Join(";", fws.Select(w => { var ag = w.GetComponent<UnityEngine.AI.NavMeshAgent>(); return $"pos {w.transform.position} dest {ag.destination} status {ag.pathStatus} vel {ag.velocity.magnitude:0.0} onMesh {ag.isOnNavMesh} stopped {ag.isStopped} pending {ag.pathPending} rem {ag.remainingDistance:0.0}"; }))}, tc {Building.All.OfType<TownCenter>().First(b => b.GetComponent<FactionMember>().Faction == FactionId.Enemy).transform.position}, food {ResourceStockpile.For(FactionId.Enemy).GetTotal(ResourceType.Food):0}");
            }
            else Debug.Log("[AiEval] t=120 no farm");
            yield return RunSim(480f);

            Debug.Log($"[AiEval] opening: {t.Report()}");
            Debug.Log($"[AiEval] opening end state: AI workers {Workers(FactionId.Enemy)}, military {Military(FactionId.Enemy)}, pop {Population.Current(FactionId.Enemy)}/{Population.Cap(FactionId.Enemy)}, food {ResourceStockpile.For(FactionId.Enemy).GetTotal(ResourceType.Food):0}, wood {ResourceStockpile.For(FactionId.Enemy).GetTotal(ResourceType.Wood):0}, gold {ResourceStockpile.For(FactionId.Enemy).GetTotal(ResourceType.Gold):0}, stone {ResourceStockpile.For(FactionId.Enemy).GetTotal(ResourceType.Stone):0}; nodes left {string.Join("/", new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Gold, ResourceType.Stone }.Select(r => ResourceNode.All.Count(n => n != null && n.ResourceType == r)))} (food/wood/gold/stone), age {AgeProgress.CurrentAge(FactionId.Enemy)}, buildings {string.Join(",", Building.All.Where(b => b != null && b.GetComponent<FactionMember>().Faction == FactionId.Enemy).Select(b => b.GetType().Name))}; player units {Unit.All.Count(u => u != null && u.GetComponent<FactionMember>().Faction == FactionId.Player)}, outcome {MatchManager_Outcome()}");
        }

        private static object MatchManager_Outcome()
        {
            return MatchManager.Outcome.ToString();
        }

        [UnityTest]
        public IEnumerator Recovery_AfterLosingWorkers_AndABarracks()
        {
            yield return Boot();
            AiTelemetry t = AiTelemetry.For(FactionId.Enemy);
            yield return RunSim(240f);

            int before = Workers(FactionId.Enemy);
            var victims = Unit.All.Where(u => u != null && u.GetComponent<FactionMember>().Faction == FactionId.Enemy && u.GetComponent<Gatherer>() != null).Take(3).ToList();
            foreach (var v in victims) v.GetComponent<Attackable>().TakeDamage(9999f);
            float lostAt = Time.time;
            yield return null;
            int afterLoss = Workers(FactionId.Enemy);

            float restored = -1f;
            Time.timeScale = Speed;
            for (float waited = 0f; waited < 180f; waited += Speed * 0.5f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (Workers(FactionId.Enemy) >= before) { restored = Time.time - lostAt; break; }
            }
            Time.timeScale = 1f;
            var stockR = ResourceStockpile.For(FactionId.Enemy);
            Debug.Log($"[AiEval] recovery detail: food {stockR.GetTotal(ResourceType.Food):0}, pop {Population.Current(FactionId.Enemy)}/{Population.Cap(FactionId.Enemy)}, tcTraining {Find<TownCenter>(FactionId.Enemy) is TownCenter tcR && tcR.IsTraining}, {t.Report()}");
            Debug.Log($"[AiEval] worker recovery: had {before}, killed to {afterLoss}, restored after {(restored < 0 ? "never (180s)" : restored.ToString("0") + "s")}");

            Building barracks = Find<Barracks>(FactionId.Enemy);
            if (barracks == null)
            {
                Debug.Log("[AiEval] barracks recovery: AI had no Barracks at t=240+180");
                yield break;
            }
            Object.Destroy(barracks.gameObject);
            float razedAt = Time.time;
            yield return null;
            float rebuilt = -1f;
            Time.timeScale = Speed;
            for (float waited = 0f; waited < 240f; waited += Speed * 0.5f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                var b = Find<Barracks>(FactionId.Enemy) as Barracks;
                if (b != null && b.IsComplete) { rebuilt = Time.time - razedAt; break; }
            }
            Time.timeScale = 1f;
            Debug.Log($"[AiEval] barracks recovery: rebuilt {(rebuilt < 0 ? "never (240s)" : rebuilt.ToString("0") + "s")}");
        }

        [UnityTest]
        public IEnumerator FinishingWeakenedOpponent()
        {
            yield return Boot();
            AiTelemetry t = AiTelemetry.For(FactionId.Enemy);
            yield return RunSim(360f);
            int militaryAtStart = Military(FactionId.Enemy);

            // Weaken the Player to a single damaged Town Center.
            foreach (var u in Unit.All.Where(u => u != null && u.GetComponent<FactionMember>().Faction == FactionId.Player).ToList())
                u.GetComponent<Attackable>().TakeDamage(9999f);
            foreach (var b in Building.All.Where(b => b != null && b.GetComponent<FactionMember>().Faction == FactionId.Player && !(b is TownCenter)).ToList())
                Object.Destroy(b.gameObject);
            var tc = Find<TownCenter>(FactionId.Player);
            tc.GetComponent<Attackable>().RestoreHealth(40f);
            float start = Time.time;
            yield return null;

            float finished = -1f;
            Time.timeScale = Speed;
            for (float waited = 0f; waited < 300f; waited += Speed * 0.5f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (MatchManager.Outcome != MatchOutcome.Ongoing) { finished = Time.time - start; break; }
            }
            Time.timeScale = 1f;
            Debug.Log($"[AiEval] finish weakened: AI military {militaryAtStart}, outcome {MatchManager.Outcome} after {(finished < 0 ? ">300s" : finished.ToString("0") + "s")}; telemetry {t.Report()}");
        }

        // Regression for a defect the evaluation found: the AI built its Farm,
        // House and Barracks around the map centre (an unset Inspector default)
        // instead of around its own Town Center.
        [UnityTest]
        public IEnumerator Ai_BuildsAroundItsOwnTownCenter_AndStartsFreshAfterRematch()
        {
            for (int match = 0; match < 2; match++)
            {
                yield return Boot();
                AiTelemetry t = AiTelemetry.For(FactionId.Enemy);
                Assert.IsNotNull(t);
                Assert.AreEqual(1, AiTelemetry.All.Count, "Exactly one AI, no leftover telemetry from the previous match.");
                Assert.AreEqual(-1f, t.FirstWorkerProduced, "Fresh telemetry each match.");

                yield return RunSim(60f);

                Building tc = Find<TownCenter>(FactionId.Enemy);
                Building farm = Find<Farm>(FactionId.Enemy);
                Assert.IsNotNull(farm, "The AI should have placed a Farm within a minute.");
                Assert.Less(Vector3.Distance(tc.transform.position, farm.transform.position), 20f, "Match " + match + ": Farm must be beside the AI's own Town Center.");
            }
        }

        [UnityTest]
        public IEnumerator Ai_IdleAndBusyFarmers_AreNotReassignedAsIdleWorkers()
        {
            yield return Boot();
            yield return RunSim(90f);
            var farmers = Object.FindObjectsByType<FarmWorker>(FindObjectsSortMode.None)
                .Where(w => w.Farm != null && w.GetComponent<FactionMember>().Faction == FactionId.Enemy).ToList();
            Assert.GreaterOrEqual(farmers.Count, 1, "The Farm must be staffed and stay staffed (not pulled off by idle-worker assignment).");
        }
    }
}

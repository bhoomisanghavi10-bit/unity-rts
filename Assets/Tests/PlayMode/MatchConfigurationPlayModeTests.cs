using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.AI;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.PlayModeTests
{
    // Prompt 11: real launches of real matches through the production
    // bootstrap. Each match reloads the scene (like Play Again) and starts
    // from a MatchConfiguration; the strategic world (resource nodes and
    // wildlife) is fingerprinted and compared across launches.
    public class MatchConfigurationPlayModeTests
    {
        private IEnumerator Launch(System.Action<CivilizationSetup> begin)
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            begin(Object.FindFirstObjectByType<CivilizationSetup>());
            for (int i = 0; i < 6; i++) yield return null; // spawners' Start()
        }

        private static string Fingerprint()
        {
            var sb = new StringBuilder();
            foreach (string line in ResourceNode.All.Where(n => n != null)
                .Select(n => $"{n.ResourceType}|{n.transform.position.x:0.00}|{n.transform.position.z:0.00}|{n.Amount:0.#}")
                .OrderBy(x => x)) sb.AppendLine(line);
            foreach (string line in Object.FindObjectsByType<WildBoar>(FindObjectsSortMode.None)
                .Select(b => $"boar|{b.transform.position.x:0.00}|{b.transform.position.z:0.00}").OrderBy(x => x)) sb.AppendLine(line);
            return sb.ToString();
        }

        private static int Hash(string s)
        {
            unchecked { int h = 17; foreach (char c in s) h = h * 31 + c; return h; }
        }

        [UnityTest]
        public IEnumerator SameSeed_ReproducesWorld_DifferentSeed_Varies_AndNextMatchIsNotStale()
        {
            string first = null, second = null, other = null;
            int nodes = 0;

            yield return Launch(s => { s.SetSeed(4242); s.BeginMatch(CivilizationId.Chola); });
            first = Fingerprint(); nodes = ResourceNode.All.Count;
            int hash1 = MatchConfiguration.Current.ComputeHash();
            Assert.AreEqual(4242, MatchConfiguration.Current.Seed);

            yield return Launch(s => { s.SetSeed(4242); s.BeginMatch(CivilizationId.Chola); });
            second = Fingerprint();
            int hash2 = MatchConfiguration.Current.ComputeHash();

            yield return Launch(s => { s.SetSeed(999); s.BeginMatch(CivilizationId.Chola); });
            other = Fingerprint();
            int hash3 = MatchConfiguration.Current.ComputeHash();

            Debug.Log($"[MatchConfig] run A seed 4242: nodes {nodes}, world hash {Hash(first)}, config hash {hash1}");
            Debug.Log($"[MatchConfig] run B seed 4242: nodes {ResourceNode.All.Count}, world hash {Hash(second)}, config hash {hash2}");
            Debug.Log($"[MatchConfig] run C seed 999 : world hash {Hash(other)}, config hash {hash3}");

            Assert.Greater(nodes, 10, "The match must actually have generated resources.");
            Assert.AreEqual(first, second, "Same configuration must reproduce the same strategic world.");
            Assert.AreEqual(hash1, hash2);
            Assert.AreNotEqual(first, other, "A different seed must change the world.");
            Assert.AreNotEqual(hash1, hash3);

            // Next match, no SetSeed: must not reuse 999 or the old civ.
            yield return Launch(s => s.BeginMatch(CivilizationId.Rajput));
            Assert.AreNotEqual(999, MatchConfiguration.Current.Seed, "A fixed seed applies to one match only.");
            Assert.AreEqual(CivilizationId.Rajput, MatchConfiguration.Current.SlotFor(FactionId.Player).Civilization);
            Assert.AreEqual(CivilizationId.Rajput, CivilizationRegistry.For(FactionId.Player));
            Debug.Log($"[MatchConfig] run D unseeded: seed {MatchConfiguration.Current.Seed}, world hash {Hash(Fingerprint())}");
        }

        [UnityTest]
        public IEnumerator HumanAndAiSlots_ReceiveEquivalentStartingForcesAtTheirMapStarts()
        {
            yield return Launch(s => s.BeginMatch(CivilizationId.Chola));
            MapDefinitionData map = MapRegistry.Current;

            foreach (var (faction, start) in new[] { (FactionId.Player, map.PlayerTownCenter), (FactionId.Enemy, map.EnemyTownCenter) })
            {
                var centres = Building.All.OfType<TownCenter>().Where(t => t.GetComponent<FactionMember>().Faction == faction).ToList();
                Assert.AreEqual(1, centres.Count, faction + " Town Centers");
                Assert.Less(Vector3.Distance(new Vector3(centres[0].transform.position.x, 0, centres[0].transform.position.z), new Vector3(start.x, 0, start.z)), 1.5f, faction + " Town Center at its configured start");
                var workers = Unit.All.Where(u => u != null && u.GetComponent<FactionMember>().Faction == faction).ToList();
                Assert.AreEqual(StartingForces.WorkerCount, workers.Count, faction + " starting workers");
                foreach (Unit w in workers)
                {
                    Assert.Less(Vector3.Distance(w.transform.position, centres[0].transform.position), 8f, faction + " workers start beside their own Town Center, not at the map centre");
                }
            }
            Assert.AreEqual(0, Unit.All.Count(u => u != null && u.GetComponent<FactionMember>().Faction == FactionId.Enemy2), "Closed slot spawns nothing.");
            Assert.IsTrue(Object.FindObjectsByType<AiController>(FindObjectsSortMode.None).Any(a => a.enabled), "Enemy AI slot activates its controller.");
        }

        [UnityTest]
        public IEnumerator HumanSecondSlot_GetsStartingForces_AndNoAiController()
        {
            yield return Launch(s => s.BeginMatch(MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha,
                seed: 11, secondSlotIsHuman: true)));

            var enemyWorkers = Unit.All.Count(u => u != null && u.GetComponent<FactionMember>().Faction == FactionId.Enemy);
            Assert.AreEqual(StartingForces.WorkerCount, enemyWorkers, "A human in the second slot starts with the same force an AI would.");
            Assert.IsTrue(Building.All.OfType<TownCenter>().Any(t => t.GetComponent<FactionMember>().Faction == FactionId.Enemy));
            Assert.IsFalse(Object.FindObjectsByType<AiController>(FindObjectsSortMode.None).Any(a => a.enabled), "No AI may drive a human slot.");
        }

        [UnityTest]
        public IEnumerator StartingResourceRule_AndPopulationRule_AreApplied()
        {
            MatchConfiguration config = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha, seed: 3);
            config.StartingResources = new StartingResourceRule { Override = true, Food = 321f, Wood = 222f, Gold = 123f, Stone = 45f };
            config.PopulationBase = 40;

            yield return Launch(s => s.BeginMatch(config));

            foreach (FactionId f in new[] { FactionId.Player, FactionId.Enemy })
            {
                ResourceStockpile stock = ResourceStockpile.For(f);
                Assert.AreEqual(321f, stock.GetTotal(ResourceType.Food), 25f, f + " food (allowing early gathering)");
                Assert.AreEqual(45f, stock.GetTotal(ResourceType.Stone), 25f);
            }
            Assert.AreEqual(40, Population.Cap(FactionId.Player));
        }
    }
}

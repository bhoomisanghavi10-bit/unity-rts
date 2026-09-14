using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 36: Score system. Uses FactionId.Enemy2 to stay isolated
    // from Unit.All/Building.All entries any other test in the same run
    // might leave behind for Player/Enemy, same convention TownBellTests/
    // IdleWorkerFinderTests/AgeUpRequirementTests already establish.
    public class ScoreProgressTests
    {
        private const FactionId TestFaction = FactionId.Enemy2;
        private const FactionId OtherFaction = FactionId.Player;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            ScoreProgress.ResetForTests();
            UpgradeProgress.ResetForTests();
            AgeProgress.Initialize(TestFaction, AgeId.Ancient);
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
                // Unit.All/Building.All registration normally happens via
                // OnEnable, which isn't guaranteed to have fired
                // synchronously within a single EditMode test - same
                // gotcha every sibling test file already documents.
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            ScoreProgress.ResetForTests();
            UpgradeProgress.ResetForTests();
            AgeProgress.Initialize(TestFaction, AgeId.Ancient);
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private GameObject CreateUnit(FactionId faction, UnitClass unitClass, bool hasGatherer)
        {
            GameObject go = CreateGameObject("Unit");
            go.AddComponent<FactionMember>().Configure(faction);
            Attackable attackable = go.AddComponent<Attackable>();
            attackable.Configure(30f);
            attackable.ConfigureClass(unitClass);
            if (hasGatherer)
            {
                go.AddComponent<Gatherer>();
            }
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            return go;
        }

        private void CreateBuilding(FactionId faction, bool complete)
        {
            GameObject go = CreateGameObject("Building");
            go.AddComponent<FactionMember>().Configure(faction);
            Building building = go.AddComponent<Building>();
            if (!Building.All.Contains(building))
            {
                Building.All.Add(building);
            }
            ConstructionSite site = go.AddComponent<ConstructionSite>();
            if (complete)
            {
                site.CompleteImmediately();
            }
            else
            {
                site.EnsureInitialized();
            }
        }

        private ResourceStockpile CreateStockpile(FactionId faction)
        {
            GameObject go = CreateGameObject("Stockpile");
            ResourceStockpile stockpile = go.AddComponent<ResourceStockpile>();
            stockpile.Configure(faction);
            return stockpile;
        }

        // --- Kill/razing credit ---

        [Test]
        public void KillCount_IsZero_ByDefault()
        {
            Assert.AreEqual(0, ScoreProgress.KillCount(TestFaction));
            Assert.AreEqual(0, ScoreProgress.BuildingsRazedCount(TestFaction));
        }

        [Test]
        public void RecordKill_IncrementsKillCount_ForUnitVictim()
        {
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: false);
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: false);

            Assert.AreEqual(2, ScoreProgress.KillCount(TestFaction));
            Assert.AreEqual(0, ScoreProgress.BuildingsRazedCount(TestFaction));
        }

        [Test]
        public void RecordKill_IncrementsBuildingsRazedCount_ForBuildingVictim()
        {
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: true);

            Assert.AreEqual(0, ScoreProgress.KillCount(TestFaction));
            Assert.AreEqual(1, ScoreProgress.BuildingsRazedCount(TestFaction));
        }

        [Test]
        public void RecordKill_DoesNotAffectOtherFactions()
        {
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: false);

            Assert.AreEqual(0, ScoreProgress.KillCount(OtherFaction));
        }

        [Test]
        public void Reset_ClearsKillsAndRazings()
        {
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: false);
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: true);

            ScoreProgress.Reset();

            Assert.AreEqual(0, ScoreProgress.KillCount(TestFaction));
            Assert.AreEqual(0, ScoreProgress.BuildingsRazedCount(TestFaction));
        }

        // --- Military ---

        [Test]
        public void Military_CountsLivingCombatUnit_AsOne()
        {
            CreateUnit(TestFaction, UnitClass.Infantry, hasGatherer: false);

            Assert.AreEqual(1, ScoreProgress.Compute(TestFaction).Military);
        }

        [Test]
        public void Military_ExcludesWorkers_EvenThoughTheyAreInfantryClass()
        {
            // WorkerFactory stamps UnitClass.Infantry too - Gatherer
            // presence is the only reliable "this is a Worker" marker.
            CreateUnit(TestFaction, UnitClass.Infantry, hasGatherer: true);

            Assert.AreEqual(0, ScoreProgress.Compute(TestFaction).Military);
        }

        [Test]
        public void Military_ExcludesSupportClassUnits()
        {
            CreateUnit(TestFaction, UnitClass.Support, hasGatherer: false);

            Assert.AreEqual(0, ScoreProgress.Compute(TestFaction).Military);
        }

        [Test]
        public void Military_ExcludesBuildingClassAttackables()
        {
            CreateUnit(TestFaction, UnitClass.Building, hasGatherer: false);

            Assert.AreEqual(0, ScoreProgress.Compute(TestFaction).Military);
        }

        [Test]
        public void Military_WeightsKillsAtThreeAndRazingsAtFive()
        {
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: false);
            ScoreProgress.RecordKill(TestFaction, victimWasBuilding: true);

            Assert.AreEqual(3 + 5, ScoreProgress.Compute(TestFaction).Military);
        }

        // --- Economy ---

        [Test]
        public void Economy_WeightsStockpileTotal_AtOneTwentiethPerPoint()
        {
            ResourceStockpile stockpile = CreateStockpile(TestFaction);
            stockpile.Add(ResourceType.Wood, 100f);
            stockpile.Add(ResourceType.Food, 100f);

            Assert.AreEqual(Mathf.RoundToInt(200f * 0.05f), ScoreProgress.Compute(TestFaction).Economy);
        }

        [Test]
        public void Economy_WeightsEachWorkerAtThree()
        {
            CreateUnit(TestFaction, UnitClass.Infantry, hasGatherer: true);
            CreateUnit(TestFaction, UnitClass.Infantry, hasGatherer: true);

            Assert.AreEqual(6, ScoreProgress.Compute(TestFaction).Economy);
        }

        [Test]
        public void Economy_IsZero_WithNoStockpileAndNoWorkers()
        {
            Assert.AreEqual(0, ScoreProgress.Compute(TestFaction).Economy);
        }

        // --- Technology ---

        // Delta-based, not absolute: UniqueTechProgress.MarkResearched has
        // no unmark/reset anywhere in this project, so a faction's
        // "researched" flag can leak across tests within the same domain
        // regardless of NUnit's (undefined) execution order - comparing
        // before/after the one change under test keeps these three
        // Technology tests independent of that leak.
        [Test]
        public void Technology_WeightsAgeReached_AtThirtyPerOrdinal()
        {
            int before = ScoreProgress.Compute(TestFaction).Technology;

            AgeProgress.Initialize(TestFaction, AgeId.Imperial);

            int after = ScoreProgress.Compute(TestFaction).Technology;
            Assert.AreEqual(3 * 30, after - before);
        }

        [Test]
        public void Technology_WeightsFlatUpgradeTiers_AtFifteenEach()
        {
            int before = ScoreProgress.Compute(TestFaction).Technology;

            UpgradeProgress.AdvanceAttack(TestFaction);
            UpgradeProgress.AdvanceArmor(TestFaction);

            int after = ScoreProgress.Compute(TestFaction).Technology;
            Assert.AreEqual(2 * 15, after - before);
        }

        [Test]
        public void Technology_AddsFiftyPoints_OnceUniqueTechResearched()
        {
            int before = ScoreProgress.Compute(TestFaction).Technology;

            UniqueTechProgress.MarkResearched(TestFaction);

            int after = ScoreProgress.Compute(TestFaction).Technology;
            Assert.AreEqual(50, after - before);
        }

        // --- Society ---

        [Test]
        public void Society_WeightsPopulation_AtFourPerUnit()
        {
            CreateUnit(TestFaction, UnitClass.Infantry, hasGatherer: false);
            CreateUnit(TestFaction, UnitClass.Infantry, hasGatherer: true);

            Assert.AreEqual(4 * 2, ScoreProgress.Compute(TestFaction).Society);
        }

        [Test]
        public void Society_WeightsCompleteBuildings_AtThreeEach()
        {
            CreateBuilding(TestFaction, complete: true);
            CreateBuilding(TestFaction, complete: true);

            Assert.AreEqual(3 * 2, ScoreProgress.Compute(TestFaction).Society);
        }

        [Test]
        public void Society_ExcludesUnderConstructionBuildings()
        {
            CreateBuilding(TestFaction, complete: false);

            Assert.AreEqual(0, ScoreProgress.Compute(TestFaction).Society);
        }

        // --- Total ---

        [Test]
        public void Breakdown_Total_SumsAllFourCategories()
        {
            var breakdown = new ScoreProgress.Breakdown(1, 2, 3, 4);

            Assert.AreEqual(10, breakdown.Total);
        }

        // --- Integration: Attackable.TakeDamage credits the attacker ---

        [Test]
        public void TakeDamage_LethalHit_CreditsAttackerFaction_WithAKill()
        {
            GameObject victimGo = CreateGameObject("Victim");
            victimGo.AddComponent<FactionMember>().Configure(OtherFaction);
            Attackable victim = victimGo.AddComponent<Attackable>();
            victim.Configure(10f);
            victim.ConfigureClass(UnitClass.Infantry);

            GameObject attackerGo = CreateGameObject("Attacker");
            attackerGo.AddComponent<FactionMember>().Configure(TestFaction);
            Attackable attacker = attackerGo.AddComponent<Attackable>();
            attacker.Configure(10f);

            TakeDamageIgnoringVfxLogs(victim, attacker);

            Assert.IsTrue(victim.IsDead);
            Assert.AreEqual(1, ScoreProgress.KillCount(TestFaction));
            Assert.AreEqual(0, ScoreProgress.BuildingsRazedCount(TestFaction));
        }

        [Test]
        public void TakeDamage_LethalHitOnBuilding_CreditsAttackerFaction_WithARazing()
        {
            GameObject victimGo = CreateGameObject("VictimBuilding");
            victimGo.AddComponent<FactionMember>().Configure(OtherFaction);
            Attackable victim = victimGo.AddComponent<Attackable>();
            victim.Configure(10f);
            victim.ConfigureClass(UnitClass.Building);

            GameObject attackerGo = CreateGameObject("Attacker");
            attackerGo.AddComponent<FactionMember>().Configure(TestFaction);
            Attackable attacker = attackerGo.AddComponent<Attackable>();
            attacker.Configure(10f);

            TakeDamageIgnoringVfxLogs(victim, attacker);

            Assert.AreEqual(0, ScoreProgress.KillCount(TestFaction));
            Assert.AreEqual(1, ScoreProgress.BuildingsRazedCount(TestFaction));
        }

        // Attackable.TakeDamage unconditionally spawns a VfxFactory particle
        // burst (stopAction: Destroy) - harmless at real runtime, but Unity's
        // Editor logs "Destroy may not be called from edit mode!" once that
        // particle system's internal cleanup fires outside Play mode, same
        // situation BuildingAttackerTests' own TickIgnoringVfxLogs documents.
        private static void TakeDamageIgnoringVfxLogs(Attackable victim, Attackable attacker)
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                victim.TakeDamage(999f, DamageType.Melee, attacker);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}

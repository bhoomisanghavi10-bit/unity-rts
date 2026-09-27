using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.PlayModeTests
{
    // Real factories require Awake (selection rings) and deferred Destroy (weapons).
    // Keep these tests synchronous so units never start movement on a missing NavMesh.
    public class DefinitionCatalogSpawnTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();
        private CivilizationId previousCivilization;

        [SetUp]
        public void SetUp()
        {
            previousCivilization = CivilizationRegistry.For(FactionId.Player);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
            ProgressionRegistry.ResetAllForNewMatch();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            spawned.Clear();
            ProgressionRegistry.ResetAllForNewMatch();
            CivilizationRegistry.Assign(FactionId.Player, previousCivilization);
        }

        private GameObject Track(GameObject go) { spawned.Add(go); return go; }

        [TestCase(DefinitionCatalog.Worker)]
        [TestCase(DefinitionCatalog.CholaPadati)]
        [TestCase(DefinitionCatalog.CholaDhanurdhara)]
        [TestCase(DefinitionCatalog.TownCenter)]
        [TestCase(DefinitionCatalog.Barracks)]
        public void CatalogAndScenarioSpawn_AttachIdentityAndPreservePlacement(string id)
        {
            var definition = DefinitionCatalog.Default.Get(id);
            var point = new Vector3(300f, 12f, 300f);
            var go = Track(definition.Kind == DefinitionKind.Unit
                ? EntitySpawner.SpawnUnit(id, FactionId.Player, point)
                : EntitySpawner.SpawnBuilding(id, FactionId.Player, point));
            Assert.IsNotNull(go);
            Assert.AreEqual(id, go.GetComponent<DefinitionId>().Value);
            Assert.AreEqual(1, go.GetComponents<DefinitionId>().Length);
            Assert.AreEqual(FactionId.Player, go.GetComponent<FactionMember>().Faction);
            Assert.IsNotNull(go.GetComponent<Attackable>());
            Assert.AreEqual(point.x, go.transform.position.x);
            Assert.AreEqual(point.z, go.transform.position.z);
            if (definition.Kind == DefinitionKind.Unit) Assert.IsNotNull(go.GetComponent<Unit>());
            else Assert.IsNotNull(go.GetComponent<Building>());
        }

        [Test]
        public void PublicFactories_AreCatalogAdaptersForReferenceEntities()
        {
            Assert.AreEqual(DefinitionCatalog.Worker, Track(WorkerFactory.Spawn(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>().Value);
            Assert.AreEqual(DefinitionCatalog.CholaPadati, Track(SoldierFactory.Spawn(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>().Value);
            Assert.AreEqual(DefinitionCatalog.CholaDhanurdhara, Track(ArcherFactory.Spawn(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>().Value);
            Assert.AreEqual(DefinitionCatalog.TownCenter, Track(TownCenterFactory.Place(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>().Value);
            var barracks = Track(BarracksFactory.Place(Vector3.zero, FactionId.Player, 42f));
            Assert.AreEqual(DefinitionCatalog.Barracks, barracks.GetComponent<DefinitionId>().Value);
            Assert.IsFalse(barracks.GetComponent<ConstructionSite>().IsComplete);
        }

        [Test]
        public void ScenarioPlacement_UsesCatalogIdsAndCompletesStartingBuildings()
        {
            var data = new CustomScenarioData();
            data.units.Add(new UnitSaveData { unitType = DefinitionCatalog.Worker, faction = (int)FactionId.Player, position = new Vector3(41f, 0f, 0f) });
            data.units.Add(new UnitSaveData { unitType = DefinitionCatalog.CholaPadati, faction = (int)FactionId.Player, position = new Vector3(42f, 0f, 0f) });
            data.units.Add(new UnitSaveData { unitType = DefinitionCatalog.CholaDhanurdhara, faction = (int)FactionId.Player, position = new Vector3(43f, 0f, 0f) });
            data.buildings.Add(new BuildingSaveData { buildingType = DefinitionCatalog.TownCenter, faction = (int)FactionId.Player, position = new Vector3(44f, 0f, 0f) });
            data.buildings.Add(new BuildingSaveData { buildingType = DefinitionCatalog.Barracks, faction = (int)FactionId.Player, position = new Vector3(45f, 0f, 0f) });

            MethodInfo spawnPlacements = typeof(CivilizationSetup).GetMethod("SpawnPlacements", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(spawnPlacements, "Scenario placement should keep one shared EntitySpawner entry point.");
            spawnPlacements.Invoke(null, new object[] { data });

            DefinitionId[] identities = UnityEngine.Object.FindObjectsByType<DefinitionId>(FindObjectsSortMode.None);
            string[] ids = { DefinitionCatalog.Worker, DefinitionCatalog.CholaPadati, DefinitionCatalog.CholaDhanurdhara,
                DefinitionCatalog.TownCenter, DefinitionCatalog.Barracks };
            for (int index = 0; index < ids.Length; index++)
            {
                string id = ids[index];
                DefinitionId identity = identities.SingleOrDefault(candidate => candidate.Value == id
                    && Mathf.Approximately(candidate.transform.position.x, 41f + index));
                Assert.IsNotNull(identity, $"Scenario placement should create '{id}' through the catalog at its authored position.");
                Track(identity.gameObject);
            }

            DefinitionId barracksIdentity = identities.Single(candidate => candidate.Value == DefinitionCatalog.Barracks && candidate.transform.position.x == 45f);
            Assert.IsTrue(barracksIdentity.GetComponent<ConstructionSite>().IsComplete,
                "Scenario starting buildings should retain the scenario placement path's completed state.");
        }

        [UnityTest]
        public System.Collections.IEnumerator ReferenceTraining_CompletesThroughCatalogAdapters()
        {
            var townCenter = Track(TownCenterFactory.Place(new Vector3(151f, 0f, 0f), FactionId.Player)).GetComponent<TownCenter>();
            townCenter.RestoreTraining(0f);

            var barracksGo = Track(BarracksFactory.Place(new Vector3(152f, 0f, 0f), FactionId.Player, 0.01f));
            barracksGo.GetComponent<ConstructionSite>().CompleteImmediately();
            var barracks = barracksGo.GetComponent<Barracks>();
            barracks.RestoreTraining(DefinitionCatalog.CholaDhanurdhara, 0f);

            // Both production paths apply their normal rally point. This
            // focused catalog test intentionally runs outside a baked
            // NavMesh, so account for the two expected SetDestination
            // errors while still exercising the real training completion.
            LogAssert.Expect(LogType.Error, "\"SetDestination\" can only be called on an active agent that has been placed on a NavMesh.");
            LogAssert.Expect(LogType.Error, "\"SetDestination\" can only be called on an active agent that has been placed on a NavMesh.");
            yield return null;

            Unit trainedWorker = Unit.All.Single(unit => unit.GetComponent<DefinitionId>()?.Value == DefinitionCatalog.Worker
                && unit.transform.position.x > 145f);
            Unit trainedArcher = Unit.All.Single(unit => unit.GetComponent<DefinitionId>()?.Value == DefinitionCatalog.CholaDhanurdhara
                && unit.transform.position.x > 150f);
            Track(trainedWorker.gameObject);
            Track(trainedArcher.gameObject);
            Assert.IsNotNull(trainedWorker,
                "Town Center Worker training should preserve the catalog identity.");
            Assert.IsNotNull(trainedArcher,
                "Barracks Dhanurdhara training should preserve the catalog identity.");
        }

        [Test]
        public void ExplicitBaseTier_DoesNotFollowResearch_ButLegacyFactoriesStillDo()
        {
            InfantryLineProgress.AdvanceTier(FactionId.Player);
            ArcherLineProgress.AdvanceTier(FactionId.Player);
            var baseSoldier = Track(DefinitionCatalog.Default.Spawn(DefinitionCatalog.CholaPadati, Vector3.zero, FactionId.Player));
            var baseArcher = Track(DefinitionCatalog.Default.Spawn(DefinitionCatalog.CholaDhanurdhara, Vector3.zero, FactionId.Player));
            var soldier = Track(SoldierFactory.Spawn(Vector3.zero, FactionId.Player));
            var archer = Track(ArcherFactory.Spawn(Vector3.zero, FactionId.Player));
            StringAssert.Contains("Padati", baseSoldier.name);
            StringAssert.Contains("Senani", soldier.name);
            StringAssert.Contains("Yantra Dhanurdhara", archer.name);
            Assert.Greater(soldier.GetComponent<Attackable>().MaxHealth, baseSoldier.GetComponent<Attackable>().MaxHealth);
            Assert.Greater(archer.GetComponent<Attackable>().MaxHealth, baseArcher.GetComponent<Attackable>().MaxHealth);
            Assert.IsNull(soldier.GetComponent<DefinitionId>());
            Assert.IsNull(archer.GetComponent<DefinitionId>());
        }

        [Test]
        public void NonCholaFactories_KeepLegacyBehavior_ExplicitCholaIdRejectsMismatch()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            Assert.IsNull(Track(SoldierFactory.Spawn(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>());
            Assert.IsNull(Track(ArcherFactory.Spawn(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>());
            Assert.Throws<ArgumentException>(() => DefinitionCatalog.Default.Spawn(DefinitionCatalog.CholaPadati, Vector3.zero, FactionId.Player));
            Assert.AreEqual(DefinitionCatalog.Worker, Track(WorkerFactory.Spawn(Vector3.zero, FactionId.Player)).GetComponent<DefinitionId>().Value);
        }

        [Test]
        public void SaveCapture_UsesIdentityDespiteRename_AndRestoresBaseTierAndBuildingPosition()
        {
            var worker = Track(WorkerFactory.Spawn(Vector3.zero, FactionId.Player));
            var soldier = Track(SoldierFactory.Spawn(Vector3.right * 5, FactionId.Player));
            var archer = Track(ArcherFactory.Spawn(Vector3.right * 10, FactionId.Player));
            var town = Track(TownCenterFactory.Place(Vector3.right * 15, FactionId.Player));
            var barracks = Track(BarracksFactory.Place(new Vector3(20, 7, 0), FactionId.Player, 42f));
            foreach (var go in spawned) go.name = "Renamed entity";
            var capture = typeof(SaveManager).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
            var data = JsonUtility.FromJson<MatchSaveData>(JsonUtility.ToJson((MatchSaveData)capture.Invoke(null, null)));
            CollectionAssert.IsSubsetOf(new[] { DefinitionCatalog.Worker, DefinitionCatalog.CholaPadati, DefinitionCatalog.CholaDhanurdhara }, data.units.ConvertAll(u => u.unitType));
            CollectionAssert.IsSubsetOf(new[] { DefinitionCatalog.TownCenter, DefinitionCatalog.Barracks }, data.buildings.ConvertAll(b => b.buildingType));
            var saved = data.buildings.Find(b => b.buildingType == DefinitionCatalog.Barracks && b.position.x == 20);
            Assert.AreEqual(7f, saved.position.y);
            var restored = Track(EntitySpawner.SpawnBuilding(saved.buildingType, FactionId.Player, saved.position));
            Assert.AreEqual(barracks.transform.position, restored.transform.position);
            InfantryLineProgress.AdvanceTier(FactionId.Player);
            var savedSoldier = data.units.Find(u => u.unitType == DefinitionCatalog.CholaPadati);
            var restoredSoldier = Track(EntitySpawner.SpawnUnit(savedSoldier.unitType, FactionId.Player, savedSoldier.position));
            Assert.AreEqual(soldier.GetComponent<Attackable>().MaxHealth, restoredSoldier.GetComponent<Attackable>().MaxHealth);
        }
    }
}

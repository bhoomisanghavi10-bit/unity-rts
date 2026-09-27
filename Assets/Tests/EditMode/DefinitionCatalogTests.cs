using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class DefinitionCatalogTests
    {
        [Test]
        public void ReferenceIds_AreUniqueAndFrozen()
        {
            string[] ids = DefinitionCatalog.Default.Definitions.Select(d => d.Id).ToArray();
            CollectionAssert.AreEquivalent(new[] { "unit.common.worker", "unit.chola.padati.t1",
                "unit.chola.dhanurdhara.t1", "building.common.town_center", "building.common.barracks" }, ids);
            Assert.AreEqual(ids.Length, ids.Distinct().Count());
        }

        [Test]
        public void DuplicateRegistration_IsRejectedAtInitialization()
        {
            var entry = DefinitionCatalog.Default.Get(DefinitionCatalog.Worker);
            StringAssert.Contains(entry.Id, Assert.Throws<ArgumentException>(() =>
                new DefinitionCatalog(new[] { entry, entry })).Message);
        }

        [TestCase("unit.unknown.missing")]
        [TestCase("Worker")]
        [TestCase("UNIT.COMMON.WORKER")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownId_ThrowsClearError(string id)
        {
            StringAssert.Contains("Unknown definition ID", Assert.Throws<ArgumentException>(() =>
                DefinitionCatalog.Default.Spawn(id, Vector3.zero, FactionId.Player)).Message);
        }

        [Test]
        public void ScenarioSpawner_RejectsUnknownAndWrongKindCatalogIds()
        {
            Assert.Throws<ArgumentException>(() => EntitySpawner.SpawnUnit("unit.unknown.missing", FactionId.Player, Vector3.zero));
            Assert.Throws<ArgumentException>(() => EntitySpawner.SpawnBuilding(DefinitionCatalog.Worker, FactionId.Player, Vector3.zero));
            Assert.Throws<ArgumentException>(() => EntitySpawner.SpawnUnit(DefinitionCatalog.TownCenter, FactionId.Player, Vector3.zero));
        }

        [Test]
        public void UnitMetadata_UsesExistingGeneratedCsvAssets()
        {
            foreach (var definition in DefinitionCatalog.Default.Definitions.Where(d => d.Kind == DefinitionKind.Unit))
            {
                Assert.IsNotNull(definition.UnitData, definition.Id);
                Assert.AreEqual(definition.DataId, definition.UnitData.unitId);
                Assert.IsNotEmpty(definition.IconKey);
                Assert.Greater(definition.UnitData.trainTimeSeconds, 0);
            }
        }

        [Test]
        public void SavePreflight_RejectsUnknownDefinitionBeforeRestore()
        {
            var data = new MatchSaveData();
            data.units.Add(new UnitSaveData { unitType = "unit.future.missing" });
            StringAssert.Contains("unit.future.missing", Assert.Throws<ArgumentException>(() =>
                SaveManager.ValidateCatalogEntries(data)).Message);
        }

        [Test]
        public void LegacySaveNames_RemainAccepted()
        {
            var data = new MatchSaveData();
            data.units.Add(new UnitSaveData { unitType = "Soldier" });
            data.buildings.Add(new BuildingSaveData { buildingType = "TownCenter" });
            Assert.DoesNotThrow(() => SaveManager.ValidateCatalogEntries(data));
        }

        [Test]
        public void CatalogProductionDefinitions_RejectUnknownWrongKindAndWrongBuilding()
        {
            var data = new MatchSaveData();
            data.buildings.Add(new BuildingSaveData
            {
                buildingType = DefinitionCatalog.Barracks,
                trainingDefinitionId = DefinitionCatalog.Worker,
            });
            StringAssert.Contains("cannot train", Assert.Throws<ArgumentException>(() =>
                SaveManager.ValidateCatalogEntries(data)).Message);

            data.buildings[0].trainingDefinitionId = DefinitionCatalog.TownCenter;
            StringAssert.Contains("not a unit", Assert.Throws<ArgumentException>(() =>
                SaveManager.ValidateCatalogEntries(data)).Message);

            data.buildings[0].trainingDefinitionId = "unit.unknown.missing";
            StringAssert.Contains("unit.unknown.missing", Assert.Throws<ArgumentException>(() =>
                SaveManager.ValidateCatalogEntries(data)).Message);
        }

        [Test]
        public void DuplicateSavedRuntimeIds_AreRejectedBeforeRestore()
        {
            var data = new MatchSaveData();
            data.units.Add(new UnitSaveData { networkId = 7 });
            data.units.Add(new UnitSaveData { networkId = 7 });
            StringAssert.Contains("Duplicate saved unit network ID '7'", Assert.Throws<ArgumentException>(() =>
                SaveManager.ValidateRuntimeIds(data)).Message);
        }

        [Test]
        public void EmptySerializedTrainingId_IsAbsentOnlyWhenNoCountdownExists()
        {
            var data = new MatchSaveData();
            data.buildings.Add(new BuildingSaveData
            {
                buildingType = DefinitionCatalog.Barracks,
                trainingDefinitionId = string.Empty,
                trainingRemaining = -1f,
            });
            Assert.DoesNotThrow(() => SaveManager.ValidateCatalogEntries(data));

            data.buildings[0].trainingRemaining = 1f;
            StringAssert.Contains("no training definition ID", Assert.Throws<ArgumentException>(() =>
                SaveManager.ValidateCatalogEntries(data)).Message);
        }
    }
}

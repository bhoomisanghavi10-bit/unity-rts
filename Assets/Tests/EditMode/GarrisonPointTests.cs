using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // General garrisoning system (worker mechanics audit, 2026-08-29 /
    // Roadmap Section 1, closed 2026-09-01). Covers GarrisonPoint's pooled
    // capacity, durgOnly gating (Wall's unchanged single-slot Durg-only
    // behavior), and UngarrisonAll's ejection - see UniqueUnitsTests.cs
    // for the 3 tests specifically covering the Maratha Durg Garrison
    // unique unit's siege-immunity mechanic through this same class.
    public class GarrisonPointTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private GameObject CreateUnit(string name, bool grantsSiegeImmunity = false)
        {
            GameObject go = CreateGameObject(name);
            go.AddComponent<GarrisonSeeker>().Configure(grantsSiegeImmunity);
            return go;
        }

        private GarrisonPoint CreateGarrisonPoint(int capacity, bool durgOnly = false)
        {
            GameObject buildingGo = CreateGameObject("Building");
            buildingGo.AddComponent<Attackable>().ConfigureClass(UnitClass.Building);
            GarrisonPoint garrisonPoint = buildingGo.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(capacity, durgOnly);
            return garrisonPoint;
        }

        [Test]
        public void TryGarrison_AcceptsAnyUnitWhenNotDurgOnly()
        {
            GarrisonPoint garrisonPoint = CreateGarrisonPoint(capacity: 4, durgOnly: false);
            GameObject worker = CreateUnit("Worker");

            bool result = garrisonPoint.TryGarrison(worker);

            Assert.IsTrue(result);
            Assert.AreEqual(1, garrisonPoint.Count);
            Assert.IsFalse(worker.activeSelf);
        }

        [Test]
        public void TryGarrison_RejectsPastCapacity()
        {
            GarrisonPoint garrisonPoint = CreateGarrisonPoint(capacity: 2, durgOnly: false);
            garrisonPoint.TryGarrison(CreateUnit("First"));
            garrisonPoint.TryGarrison(CreateUnit("Second"));
            GameObject third = CreateUnit("Third");

            bool result = garrisonPoint.TryGarrison(third);

            Assert.IsFalse(result);
            Assert.AreEqual(2, garrisonPoint.Count);
            Assert.IsTrue(third.activeSelf);
        }

        [Test]
        public void TryGarrison_DurgOnly_RejectsUnitThatDoesNotGrantSiegeImmunity()
        {
            GarrisonPoint garrisonPoint = CreateGarrisonPoint(capacity: 1, durgOnly: true);
            GameObject soldier = CreateUnit("Soldier", grantsSiegeImmunity: false);

            bool result = garrisonPoint.TryGarrison(soldier);

            Assert.IsFalse(result);
            Assert.AreEqual(0, garrisonPoint.Count);
            Assert.IsTrue(soldier.activeSelf);
        }

        [Test]
        public void TryGarrison_DurgOnly_AcceptsUnitThatGrantsSiegeImmunity()
        {
            GarrisonPoint garrisonPoint = CreateGarrisonPoint(capacity: 1, durgOnly: true);
            GameObject durgUnit = CreateUnit("DurgGarrisonUnit", grantsSiegeImmunity: true);

            bool result = garrisonPoint.TryGarrison(durgUnit);

            Assert.IsTrue(result);
            Assert.AreEqual(1, garrisonPoint.Count);
        }

        [Test]
        public void SiegeImmune_ClearsOnlyAfterLastImmunityGrantingOccupantLeaves()
        {
            GarrisonPoint garrisonPoint = CreateGarrisonPoint(capacity: 2, durgOnly: false);
            Attackable buildingAttackable = garrisonPoint.GetComponent<Attackable>();
            GameObject durgUnit = CreateUnit("DurgGarrisonUnit", grantsSiegeImmunity: true);
            GameObject worker = CreateUnit("Worker", grantsSiegeImmunity: false);
            garrisonPoint.TryGarrison(durgUnit);
            garrisonPoint.TryGarrison(worker);

            Assert.IsTrue(buildingAttackable.SiegeImmune);

            garrisonPoint.UngarrisonAll();

            Assert.IsFalse(buildingAttackable.SiegeImmune);
        }

        [Test]
        public void UngarrisonAll_ReactivatesEveryOccupantAndClearsCount()
        {
            GarrisonPoint garrisonPoint = CreateGarrisonPoint(capacity: 3, durgOnly: false);
            GameObject first = CreateUnit("First");
            GameObject second = CreateUnit("Second");
            garrisonPoint.TryGarrison(first);
            garrisonPoint.TryGarrison(second);

            garrisonPoint.UngarrisonAll();

            Assert.AreEqual(0, garrisonPoint.Count);
            Assert.IsTrue(first.activeSelf);
            Assert.IsTrue(second.activeSelf);
        }
    }
}

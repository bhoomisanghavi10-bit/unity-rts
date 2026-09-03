using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Item 6 (Scenario Editor, heavy path session 1). EntitySpawner is a
    // pure extraction of SaveManager's own RestoreUnits/RestoreBuildings
    // dispatch - these tests confirm every unit type string still resolves
    // to the correct real entity, proving the refactor is behavior-
    // preserving. Units have no dedicated per-type component (Worker/
    // Soldier/Archer/Cavalry/Siege are all just Unit+MeleeAttacker with a
    // different UnitClass/component set - confirmed by reading the
    // factories directly, not assumed) - identified here by
    // Attackable.Class plus Gatherer presence (the one thing that actually
    // distinguishes Worker from Soldier, both UnitClass.Infantry).
    //
    // Two things are NOT covered here, both confirmed (not guessed) to be
    // EditMode-only limitations pre-existing in this codebase, unrelated to
    // this session's own changes:
    //  - SpawnBuilding: every building factory calls SelectionIndicator.
    //    Configure() immediately after AddComponent (unlike units, which
    //    never call Configure - "Units keep the smaller default sizing" per
    //    SelectionIndicator's own comment), and that Configure call NREs
    //    outside Play mode.
    //  - SpawnUnit("Soldier"): WeaponAttachment.KeepOnlyFirstMesh calls the
    //    real (non-Immediate) Object.Destroy to trim its weapon prop's
    //    extra mesh renderers - harmless at real runtime, but Unity's
    //    Editor logs a hard error for it outside Play mode that neither
    //    LogAssert.ignoreFailingMessages nor disabling Debug.unityLogger
    //    suppresses (both tried directly, confirmed still failing) - unlike
    //    every other EditMode-only log this project's tests already
    //    document and work around.
    // Both are live-verified via UnityMCP instead (see docs/SESSION_LOG.md).
    public class EntitySpawnerTests
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

        [Test]
        public void SpawnBuilding_UnknownType_ReturnsNull()
        {
            Assert.IsNull(EntitySpawner.SpawnBuilding("NotARealType", FactionId.Player, Vector3.zero));
        }

        [Test]
        public void SpawnUnit_Worker_HasInfantryClassAndGatherer()
        {
            GameObject go = SpawnUnitTracked("Worker");
            Assert.AreEqual(UnitClass.Infantry, go.GetComponent<Attackable>().Class);
            Assert.IsNotNull(go.GetComponent<Gatherer>(), "Worker must have Gatherer - the one thing distinguishing it from Soldier.");
        }

        [Test]
        public void SpawnUnit_Archer_HasArcherClass()
        {
            GameObject go = SpawnUnitTracked("Archer");
            Assert.AreEqual(UnitClass.Archer, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void SpawnUnit_Cavalry_HasCavalryClass()
        {
            GameObject go = SpawnUnitTracked("Cavalry");
            Assert.AreEqual(UnitClass.Cavalry, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void SpawnUnit_Siege_HasSiegeClass()
        {
            GameObject go = SpawnUnitTracked("Siege");
            Assert.AreEqual(UnitClass.Siege, go.GetComponent<Attackable>().Class);
        }

        [Test]
        public void SpawnUnit_UnknownType_ReturnsNull()
        {
            Assert.IsNull(EntitySpawner.SpawnUnit("NotARealType", FactionId.Player, Vector3.zero));
        }

        [Test]
        public void UnitTypes_ListsSoldierAlongsideTheOtherFourEditModeSafeTypes()
        {
            // Confirms the roster itself (not a spawn) - Soldier's own spawn
            // behavior is covered by live verification instead, see the
            // class-level comment above.
            CollectionAssert.Contains(EntitySpawner.UnitTypes, "Soldier");
            Assert.AreEqual(5, EntitySpawner.UnitTypes.Length);
        }

        [Test]
        public void UnitTypes_EveryTypeExceptSoldier_ActuallySpawnsViaSpawnUnit()
        {
            foreach (string type in EntitySpawner.UnitTypes)
            {
                if (type == "Soldier")
                {
                    continue;
                }

                GameObject go = SpawnUnitTracked(type);
                Assert.IsNotNull(go, $"UnitTypes lists '{type}' but SpawnUnit doesn't handle it.");
            }
        }

        private GameObject SpawnUnitTracked(string type)
        {
            GameObject go = EntitySpawner.SpawnUnit(type, FactionId.Player, Vector3.zero);
            Assert.IsNotNull(go, $"SpawnUnit('{type}') returned null.");
            _spawned.Add(go);
            return go;
        }
    }
}

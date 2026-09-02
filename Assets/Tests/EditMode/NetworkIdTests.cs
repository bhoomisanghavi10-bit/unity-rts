using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Phase 5 LAN transport MVP: NetworkId is the one new piece of shared
    // mutable state a real transport needs (a stable integer identity for
    // units/buildings) - this proves assignment is sequential/stable and
    // TryResolve/TryGetId round-trip correctly, the concrete claim
    // CommandSerializer's receive-side resolution depends on.
    public class NetworkIdTests
    {
        [SetUp]
        public void SetUp()
        {
            NetworkId.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            NetworkId.Reset();
        }

        // Unit.OnEnable doesn't fire synchronously right after
        // AddComponent<Unit>() in EditMode (the same gotcha
        // CommandBusDeterminismTests/DesyncRecoveryTests already document),
        // so NetworkId.Assign is called directly here rather than relying
        // on the real OnEnable hook - this test is about Assign/TryResolve/
        // TryGetId's own contract, not about proving OnEnable wiring (that
        // needs a live Play mode session, same as StateHash's own unit
        // registration).
        [Test]
        public void Assign_ReturnsSequentialIds_StartingFromZeroAfterReset()
        {
            var unitA = new GameObject("A").AddComponent<Unit>();
            var unitB = new GameObject("B").AddComponent<Unit>();

            try
            {
                int idA = NetworkId.Assign(unitA);
                int idB = NetworkId.Assign(unitB);

                Assert.AreEqual(0, idA);
                Assert.AreEqual(1, idB);
            }
            finally
            {
                Object.DestroyImmediate(unitA.gameObject);
                Object.DestroyImmediate(unitB.gameObject);
            }
        }

        [Test]
        public void TryResolveUnit_RoundTripsAnAssignedId()
        {
            var unit = new GameObject("A").AddComponent<Unit>();

            try
            {
                int id = NetworkId.Assign(unit);

                bool resolved = NetworkId.TryResolveUnit(id, out Unit found);

                Assert.IsTrue(resolved);
                Assert.AreSame(unit, found);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void TryResolveUnit_UnknownId_ReturnsFalse()
        {
            bool resolved = NetworkId.TryResolveUnit(999, out Unit found);

            Assert.IsFalse(resolved);
            Assert.IsNull(found);
        }

        [Test]
        public void TryResolveUnit_DestroyedUnit_ReturnsFalse()
        {
            var unit = new GameObject("A").AddComponent<Unit>();
            int id = NetworkId.Assign(unit);

            Object.DestroyImmediate(unit.gameObject);

            // Unity overloads == so a destroyed object still compares equal
            // to null - TryResolveUnit's own null-check (not a plain
            // dictionary lookup) is what makes this the correct "stale
            // reference" behavior CommandSerializer's receive-side code
            // relies on, the same tolerance every Command subtype's own
            // Execute() guard already has for a locally-originated order.
            bool resolved = NetworkId.TryResolveUnit(id, out Unit found);

            Assert.IsFalse(resolved);
        }

        [Test]
        public void TryGetId_RoundTripsBackToTheSameId()
        {
            var unit = new GameObject("A").AddComponent<Unit>();

            try
            {
                int assigned = NetworkId.Assign(unit);

                bool found = NetworkId.TryGetId(unit, out int retrieved);

                Assert.IsTrue(found);
                Assert.AreEqual(assigned, retrieved);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void Assign_BuildingsAndUnits_TrackSeparateIdSequences()
        {
            var unit = new GameObject("Unit").AddComponent<Unit>();
            var building = new GameObject("Building").AddComponent<Building>();

            try
            {
                int unitId = NetworkId.Assign(unit);
                int buildingId = NetworkId.Assign(building);

                Assert.AreEqual(0, unitId, "Unit and Building id sequences must be independent - both should start at 0.");
                Assert.AreEqual(0, buildingId);
                Assert.IsTrue(NetworkId.TryResolveBuilding(buildingId, out Building foundBuilding));
                Assert.AreSame(building, foundBuilding);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(building.gameObject);
            }
        }

        [Test]
        public void Reset_ClearsPriorAssignmentsAndRestartsCounters()
        {
            var unitA = new GameObject("A").AddComponent<Unit>();
            NetworkId.Assign(unitA);

            NetworkId.Reset();

            var unitB = new GameObject("B").AddComponent<Unit>();
            try
            {
                int idAfterReset = NetworkId.Assign(unitB);

                Assert.AreEqual(0, idAfterReset, "Reset must restart the counter from 0.");
                Assert.IsFalse(NetworkId.TryGetId(unitA, out _), "Reset must forget prior assignments entirely.");
            }
            finally
            {
                Object.DestroyImmediate(unitA.gameObject);
                Object.DestroyImmediate(unitB.gameObject);
            }
        }
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // AoE-parity Phase 4.2 (worker self-defense/cross-awareness, Roadmap
    // Section 1): Gatherer.HandleDamaged is exercised directly (not via
    // Attackable.OnDamaged's real subscription timing) - see
    // AssemblyInfo.cs's InternalsVisibleTo grant, same convention as
    // GathererDropOffTests' direct AcceptsDropOff calls.
    public class GathererCombatResponseTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // GatherFrom/HandleDamaged's Fight-and-Flee branches both call
            // UnitMover.MoveTo -> NavMeshAgent.SetDestination, which logs an
            // Editor error here (no baked NavMesh in an EditMode test scene)
            // even though the call itself doesn't throw - same
            // expected-not-a-regression situation BuildingAttackerTests
            // documents for its own Editor-only VFX log. Unlike that log,
            // this one is a native-engine error that ignoreFailingMessages
            // alone doesn't suppress in this Unity Test Framework version -
            // it still fails the test as an "Unhandled log message" unless
            // explicitly consumed via LogAssert.Expect, so each test that
            // triggers a SetDestination call does that itself.
            LogAssert.ignoreFailingMessages = true;
        }

        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");

        private static void ExpectSetDestinationError()
        {
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private GameObject CreateGameObject(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _spawned.Add(go);
            return go;
        }

        private Gatherer CreateWorker(CombatResponse response)
        {
            GameObject go = CreateGameObject("Worker", Vector3.zero);
            go.AddComponent<UnitMover>();
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(2f);
            Gatherer gatherer = go.AddComponent<Gatherer>();
            gatherer.SetCombatResponse(response);
            return gatherer;
        }

        private ResourceNode CreateNode(Vector3 position)
        {
            GameObject go = CreateGameObject("Node", position);
            var node = go.AddComponent<ResourceNode>();
            return node;
        }

        private Attackable CreateAttacker(Vector3 position)
        {
            GameObject go = CreateGameObject("Attacker", position);
            return go.AddComponent<Attackable>();
        }

        [Test]
        public void HandleDamaged_WhileGathering_Fight_InterruptsAndAttacksBack()
        {
            ExpectSetDestinationError(); // GatherFrom
            Gatherer gatherer = CreateWorker(CombatResponse.Fight);
            gatherer.GatherFrom(CreateNode(Vector3.zero));
            Attackable attacker = CreateAttacker(new Vector3(5f, 0f, 0f));

            ExpectSetDestinationError(); // AttackMove
            gatherer.HandleDamaged(attacker);

            Assert.IsFalse(gatherer.IsWorking, "Fight response should cancel the gather task.");
            MeleeAttacker meleeAttacker = gatherer.GetComponent<MeleeAttacker>();
            Assert.IsTrue(meleeAttacker.IsAttacking, "Fight response should turn the worker's own MeleeAttacker on the attacker.");
        }

        [Test]
        public void HandleDamaged_WhileGathering_Flee_InterruptsWithoutAttacking()
        {
            ExpectSetDestinationError(); // GatherFrom
            Gatherer gatherer = CreateWorker(CombatResponse.Flee);
            gatherer.GatherFrom(CreateNode(Vector3.zero));
            Attackable attacker = CreateAttacker(new Vector3(5f, 0f, 0f));

            ExpectSetDestinationError(); // flee MoveTo
            gatherer.HandleDamaged(attacker);

            Assert.IsFalse(gatherer.IsWorking, "Flee response should cancel the gather task.");
            MeleeAttacker meleeAttacker = gatherer.GetComponent<MeleeAttacker>();
            Assert.IsFalse(meleeAttacker.IsAttacking, "Flee response should not turn the worker's MeleeAttacker on anything.");
        }

        [Test]
        public void HandleDamaged_WhileIdle_IsANoOp()
        {
            Gatherer gatherer = CreateWorker(CombatResponse.Fight);
            Attackable attacker = CreateAttacker(new Vector3(5f, 0f, 0f));

            gatherer.HandleDamaged(attacker);

            Assert.IsFalse(gatherer.IsWorking);
            Assert.IsFalse(gatherer.GetComponent<MeleeAttacker>().IsAttacking);
        }

        [Test]
        public void HandleDamaged_NullAttacker_IsANoOp()
        {
            ExpectSetDestinationError(); // GatherFrom
            Gatherer gatherer = CreateWorker(CombatResponse.Fight);
            gatherer.GatherFrom(CreateNode(Vector3.zero));

            gatherer.HandleDamaged(null);

            Assert.IsTrue(gatherer.IsWorking, "A null attacker shouldn't interrupt an in-progress gather task.");
        }

        [TestCase(5f, 0f, 0f, -1f, 0f, 0f)]
        [TestCase(0f, 0f, -5f, 0f, 0f, 1f)]
        public void ComputeFleeDestination_PointsAwayFromAttacker(
            float attackerX, float attackerY, float attackerZ,
            float expectedDirX, float expectedDirY, float expectedDirZ)
        {
            Vector3 self = Vector3.zero;
            Vector3 attacker = new Vector3(attackerX, attackerY, attackerZ);

            Vector3 destination = Gatherer.ComputeFleeDestination(self, attacker, distance: 6f);

            Vector3 expectedDirection = new Vector3(expectedDirX, expectedDirY, expectedDirZ);
            Vector3 expectedDestination = self + expectedDirection * 6f;
            Assert.Less(Vector3.Distance(destination, expectedDestination), 0.001f);
        }

        [Test]
        public void ComputeFleeDestination_SamePositionAsAttacker_FallsBackToAFixedDirection()
        {
            Vector3 destination = Gatherer.ComputeFleeDestination(Vector3.zero, Vector3.zero, distance: 6f);

            Assert.AreEqual(6f, destination.magnitude, 0.001f);
        }

        [TestCase(CivilizationId.Chola, CombatResponse.Fight)]
        [TestCase(CivilizationId.Vijayanagara, CombatResponse.Fight)]
        [TestCase(CivilizationId.Rajput, CombatResponse.Fight)]
        [TestCase(CivilizationId.Maurya, CombatResponse.Fight)]
        [TestCase(CivilizationId.Maratha, CombatResponse.Flee)]
        public void WorkerCombatResponseDefaults_MatchesDesignedPerCivDefaults(CivilizationId civ, CombatResponse expected)
        {
            Assert.AreEqual(expected, WorkerCombatResponseDefaults.For(civ));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // AoE-Parity Phase 5 (multiplayer determinism): CommandBus/StateHash had
    // zero test coverage before this - this is the actual "same inputs ->
    // same state" claim the whole lockstep design rests on, so it's worth
    // proving directly rather than only via the (also new) live BuildingPlacer
    // wiring. Exercises CommandBus.EnqueueAt/ExecuteTick directly rather than
    // through SimClock's real Update() loop, which never ticks in EditMode
    // (gated on CivilizationSetup.HasMatchStarted, always false there) - see
    // AssemblyInfo.cs's InternalsVisibleTo grant.
    public class CommandBusDeterminismTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                // Unit.All registration/deregistration normally happens via
                // OnEnable/OnDisable, but Unity's Editor doesn't guarantee
                // those fire synchronously within a single test method (same
                // gotcha BuildingAttackerTests documents) - removed directly
                // here instead.
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        // Mutates a Transform by a fixed delta on Execute() - deliberately
        // not MoveCommand/UnitMover here, to keep this test about
        // CommandBus/StateHash's own ordering/folding guarantee rather than
        // NavMesh (which EditMode can't bake anyway).
        private class DeterministicTestCommand : Command
        {
            private readonly Transform _transform;
            private readonly Vector3 _delta;

            public DeterministicTestCommand(Transform transform, Vector3 delta) : base(FactionId.Player)
            {
                _transform = transform;
                _delta = delta;
            }

            public override void Execute()
            {
                _transform.position += _delta;
            }
        }

        private class RecordingCommand : Command
        {
            private readonly List<int> _log;
            private readonly int _id;

            public RecordingCommand(List<int> log, int id) : base(FactionId.Player)
            {
                _log = log;
                _id = id;
            }

            public override void Execute()
            {
                _log.Add(_id);
            }
        }

        [Test]
        public void ExecuteTick_RunsCommandsInFixedEnqueueOrder()
        {
            var log = new List<int>();
            const int tick = 1000;
            CommandBus.EnqueueAt(tick, new RecordingCommand(log, 1));
            CommandBus.EnqueueAt(tick, new RecordingCommand(log, 2));
            CommandBus.EnqueueAt(tick, new RecordingCommand(log, 3));

            CommandBus.ExecuteTick(tick);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, log,
                "CommandBus must execute a tick's commands in fixed enqueue order - this is what 'same inputs -> same state' depends on.");
        }

        [Test]
        public void ExecuteTick_UnscheduledTick_IsANoOp()
        {
            // No assertion beyond "doesn't throw" - an empty/absent tick is
            // the common case for every tick where no player issued an order.
            CommandBus.ExecuteTick(-999999);
        }

        private GameObject SpawnWorld(out Transform a, out Transform b)
        {
            GameObject unitA = CreateUnit("A", new Vector3(0f, 0f, 0f), FactionId.Player);
            GameObject unitB = CreateUnit("B", new Vector3(10f, 0f, 5f), FactionId.Enemy);
            a = unitA.transform;
            b = unitB.transform;
            return unitA;
        }

        private GameObject CreateUnit(string name, Vector3 position, FactionId faction)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            // Registered directly rather than relying on Unit.OnEnable -
            // see TearDown's comment for why. Without this, StateHash.Compute()
            // silently folds over zero units (Unit.All stays empty within a
            // single synchronous test method), which is exactly what let the
            // first draft of this test pass with a hash that never actually
            // depended on either spawned unit's state.
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            go.AddComponent<Attackable>().Configure(30f);
            go.AddComponent<FactionMember>().Configure(faction);
            _spawned.Add(go);
            return go;
        }

        // The actual self-consistency proof: replaying the identical command
        // stream against two independently-built, identical starting worlds
        // must fold into the identical StateHash - the concrete form of
        // "same inputs -> same state" this whole phase is about.
        [Test]
        public void ReplayingIdenticalCommandStream_ProducesIdenticalStateHash()
        {
            uint hashA = RunScenario(deltaForSecondCommand: new Vector3(3f, 0f, -1f));

            uint hashB = RunScenario(deltaForSecondCommand: new Vector3(3f, 0f, -1f));

            Assert.AreEqual(hashA, hashB,
                "Replaying the identical command stream against an identically-built starting world must produce the identical StateHash.");
        }

        // Guards against the test passing trivially (both hashes wrong in
        // the same way) - a genuinely different input must fold differently.
        [Test]
        public void ReplayingADifferentCommandStream_ProducesADifferentStateHash()
        {
            uint hashA = RunScenario(deltaForSecondCommand: new Vector3(3f, 0f, -1f));

            uint hashB = RunScenario(deltaForSecondCommand: new Vector3(-7f, 0f, 2f));

            Assert.AreNotEqual(hashA, hashB,
                "A different command stream must produce a different StateHash, or this test's positive case would be meaningless.");
        }

        private uint RunScenario(Vector3 deltaForSecondCommand)
        {
            GameObject unitA = SpawnWorld(out Transform transformA, out Transform transformB);

            const int tick1 = 5000;
            const int tick2 = 5001;
            CommandBus.EnqueueAt(tick1, new DeterministicTestCommand(transformA, new Vector3(1f, 0f, 2f)));
            CommandBus.EnqueueAt(tick1, new DeterministicTestCommand(transformB, new Vector3(0f, 0f, -4f)));
            CommandBus.EnqueueAt(tick2, new DeterministicTestCommand(transformA, deltaForSecondCommand));

            CommandBus.ExecuteTick(tick1);
            CommandBus.ExecuteTick(tick2);

            uint hash = StateHash.Compute();

            TearDown();
            return hash;
        }
    }
}

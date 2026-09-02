using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Phase 5 LAN transport MVP: Command (Move/Train/Build/Attack) carries
    // live object references and closures, not data - CommandSerializer is
    // the one place that converts to/from the wire-format NetMessageEnvelope
    // (see Command.cs's own comment on why this had to be a separate class
    // rather than a method on Command itself). This proves the receive-side
    // half: given an envelope whose NetworkIds resolve to real live
    // objects, ToCommand reconstructs the correct concrete Command type
    // tagged with the correct Faction - and given an envelope referencing a
    // NetworkId that no longer resolves (the same staleness every Command
    // subtype's own Execute() already tolerates), returns null rather than
    // throwing.
    public class CommandSerializerTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            NetworkId.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            NetworkId.Reset();
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private GameObject Spawn(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void ToCommand_Move_ResolvesToMoveCommandWithCorrectFaction()
        {
            GameObject go = Spawn("Unit");
            Unit unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            int unitId = NetworkId.Assign(unit);

            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.Move,
                faction = (int)FactionId.Enemy,
                unitNetId = unitId,
                destination = new Vector3(3f, 0f, 4f),
            };

            Command command = CommandSerializer.ToCommand(envelope);

            Assert.IsInstanceOf<MoveCommand>(command);
            Assert.AreEqual(FactionId.Enemy, command.Faction);
        }

        [Test]
        public void ToCommand_Move_UnresolvableUnit_ReturnsNull()
        {
            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.Move,
                faction = (int)FactionId.Enemy,
                unitNetId = 12345,
                destination = Vector3.zero,
            };

            Command command = CommandSerializer.ToCommand(envelope);

            Assert.IsNull(command, "A NetworkId that resolves to nothing (destroyed/never existed) must fail closed, same as every Command's own stale-reference guard.");
        }

        [Test]
        public void ToCommand_Attack_ResolvesToAttackCommand()
        {
            GameObject attackerGo = Spawn("Attacker");
            Unit attackerUnit = attackerGo.AddComponent<Unit>();
            attackerGo.AddComponent<MeleeAttacker>();
            int attackerId = NetworkId.Assign(attackerUnit);

            GameObject targetGo = Spawn("Target");
            Unit targetUnit = targetGo.AddComponent<Unit>();
            targetGo.AddComponent<Attackable>().Configure(30f);
            int targetId = NetworkId.Assign(targetUnit);

            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.Attack,
                faction = (int)FactionId.Player,
                attackerNetId = attackerId,
                targetNetId = targetId,
            };

            Command command = CommandSerializer.ToCommand(envelope);

            Assert.IsInstanceOf<AttackCommand>(command);
            Assert.AreEqual(FactionId.Player, command.Faction);
        }

        [Test]
        public void ToCommand_Attack_DeadTarget_StillResolves_ExecuteIsWhatGuardsStaleness()
        {
            // Mirrors AttackCommand.Execute's own convention: resolution
            // (this class's job) and the IsDead guard (Execute's job) are
            // deliberately separate concerns - ToCommand only needs to
            // confirm both NetworkIds resolve to something real right now.
            GameObject attackerGo = Spawn("Attacker");
            Unit attackerUnit = attackerGo.AddComponent<Unit>();
            attackerGo.AddComponent<MeleeAttacker>();
            int attackerId = NetworkId.Assign(attackerUnit);

            GameObject targetGo = Spawn("Target");
            Unit targetUnit = targetGo.AddComponent<Unit>();
            targetGo.AddComponent<Attackable>();
            int targetId = NetworkId.Assign(targetUnit);

            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.Attack,
                faction = (int)FactionId.Player,
                attackerNetId = attackerId,
                targetNetId = targetId,
            };

            Command command = CommandSerializer.ToCommand(envelope);

            Assert.IsInstanceOf<AttackCommand>(command);
        }

        [Test]
        public void ToCommand_Train_Barracks_ResolvesToTrainCommand()
        {
            GameObject barracksGo = Spawn("Barracks");
            Barracks barracks = barracksGo.AddComponent<Barracks>();
            int buildingId = NetworkId.Assign(barracks);

            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.Train,
                faction = (int)FactionId.Enemy,
                sourceBuildingNetId = buildingId,
                trainKind = NetTrainKind.Archer,
            };

            Command command = CommandSerializer.ToCommand(envelope);

            Assert.IsInstanceOf<TrainCommand>(command);
            Assert.AreEqual(FactionId.Enemy, command.Faction);
        }

        [Test]
        public void ToCommand_Train_UnresolvableBuilding_ReturnsNull()
        {
            var envelope = new NetMessageEnvelope
            {
                kind = NetMessageKind.Train,
                faction = (int)FactionId.Player,
                sourceBuildingNetId = 999,
                trainKind = NetTrainKind.Soldier,
            };

            Command command = CommandSerializer.ToCommand(envelope);

            Assert.IsNull(command);
        }

        // --- Wire round-trip: JsonUtility serialization must preserve
        // every field CommandSerializer/LanTransport actually read,
        // including enums and Vector3 - this is what LanTransport.Send/
        // ReceiveLoop rely on. ---
        [Test]
        public void NetMessageEnvelope_JsonRoundTrip_PreservesAllFields()
        {
            var original = new NetMessageEnvelope
            {
                kind = NetMessageKind.Attack,
                tick = 12345,
                faction = (int)FactionId.Enemy,
                attackerNetId = 7,
                targetNetId = 9,
                destination = new Vector3(1.5f, 2.5f, -3.5f),
                buildKind = NetBuildKind.Tower,
                trainKind = NetTrainKind.Cavalry,
                hash = 4242u,
            };

            string json = JsonUtility.ToJson(original);
            NetMessageEnvelope roundTripped = JsonUtility.FromJson<NetMessageEnvelope>(json);

            Assert.AreEqual(original.kind, roundTripped.kind);
            Assert.AreEqual(original.tick, roundTripped.tick);
            Assert.AreEqual(original.faction, roundTripped.faction);
            Assert.AreEqual(original.attackerNetId, roundTripped.attackerNetId);
            Assert.AreEqual(original.targetNetId, roundTripped.targetNetId);
            Assert.AreEqual(original.destination, roundTripped.destination);
            Assert.AreEqual(original.buildKind, roundTripped.buildKind);
            Assert.AreEqual(original.trainKind, roundTripped.trainKind);
            Assert.AreEqual(original.hash, roundTripped.hash);
        }
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Prompt 12: LAN command/state foundation - wire round trips for every
    // command kind, sender binding, duplicate/late rejection, canonical
    // ordering, config verification, and local-perspective rules.
    // Code-level verification only: nothing here exercises two processes.
    public class LanCommandFoundationTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            NetworkId.Reset();
            NetworkDiagnostics.Reset();
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, false);
        }

        [TearDown]
        public void TearDown()
        {
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, false);
            MatchConfiguration.End();
            NetworkId.Reset();
            foreach (GameObject go in _spawned)
            {
                if (go == null) continue;
                if (go.TryGetComponent(out Unit u)) Unit.All.Remove(u);
                if (go.TryGetComponent(out Building b)) Building.All.Remove(b);
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private GameObject Go(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Unit MakeUnit(System.Action<GameObject> configure = null)
        {
            GameObject go = Go("Unit");
            Unit unit = go.AddComponent<Unit>();
            configure?.Invoke(go);
            NetworkId.Assign(unit);
            return unit;
        }

        private T MakeBuilding<T>() where T : Building
        {
            GameObject go = Go(typeof(T).Name);
            T b = go.AddComponent<T>();
            NetworkId.Assign(b);
            return b;
        }

        // Send-side envelope -> JSON -> receive-side envelope, exactly what
        // LanTransport does.
        private static NetMessageEnvelope OverTheWire(NetMessageEnvelope e)
        {
            return JsonUtility.FromJson<NetMessageEnvelope>(JsonUtility.ToJson(e));
        }

        [Test]
        public void RoundTrip_Move()
        {
            Unit u = MakeUnit(g => g.AddComponent<UnitMover>());
            NetMessageEnvelope e = OverTheWire(CommandSerializer.ForMove(9, FactionId.Enemy, u, new Vector3(1, 2, 3)));
            Assert.AreEqual(new Vector3(1, 2, 3), e.destination);
            Assert.IsInstanceOf<MoveCommand>(CommandSerializer.ToCommand(e));
        }

        [Test]
        public void RoundTrip_Train_EveryProducingBuilding()
        {
            var cases = new (Building building, NetTrainKind kind)[]
            {
                (MakeBuilding<TownCenter>(), NetTrainKind.Soldier),
                (MakeBuilding<Barracks>(), NetTrainKind.Archer),
                (MakeBuilding<Durg>(), NetTrainKind.Hero),
                (MakeBuilding<Dock>(), NetTrainKind.WarGalley),
                (MakeBuilding<Market>(), NetTrainKind.Vanik),
                (MakeBuilding<Monastery>(), NetTrainKind.Vaidya),
            };
            foreach ((Building building, NetTrainKind kind) in cases)
            {
                NetMessageEnvelope e = OverTheWire(CommandSerializer.ForTrain(4, FactionId.Player, building, kind));
                Assert.AreEqual(kind, e.trainKind);
                Assert.IsInstanceOf<TrainCommand>(CommandSerializer.ToCommand(e), building.GetType().Name);
            }
        }

        [Test]
        public void RoundTrip_Build_KindPointAndRotation()
        {
            Go("Placer").AddComponent<BuildingPlacer>();
            NetMessageEnvelope e = OverTheWire(CommandSerializer.ForBuild(4, FactionId.Player, NetBuildKind.Wall, new Vector3(5, 0, 6), 45f));

            Assert.AreEqual(NetBuildKind.Wall, e.buildKind);
            Assert.AreEqual(new Vector3(5, 0, 6), e.point);
            Assert.AreEqual(45f, e.buildRotationY);
            Assert.IsInstanceOf<BuildCommand>(CommandSerializer.ToCommand(e));
        }

        [Test]
        public void RoundTrip_Attack_UnitTarget_AndBuildingTarget()
        {
            Unit attacker = MakeUnit(g => g.AddComponent<MeleeAttacker>());
            Unit targetUnit = MakeUnit(g => g.AddComponent<Attackable>().Configure(30f));
            Barracks targetBuilding = MakeBuilding<Barracks>();
            targetBuilding.gameObject.AddComponent<Attackable>().Configure(100f);

            NetMessageEnvelope unitEnv = OverTheWire(CommandSerializer.ForAttack(3, FactionId.Player, attacker, targetUnit.GetComponent<Attackable>()));
            NetMessageEnvelope buildingEnv = OverTheWire(CommandSerializer.ForAttack(3, FactionId.Player, attacker, targetBuilding.GetComponent<Attackable>()));

            Assert.AreEqual(0, unitEnv.targetKind);
            Assert.AreEqual(1, buildingEnv.targetKind, "A building target must be typed as a building, not silently dropped.");
            Assert.IsInstanceOf<AttackCommand>(CommandSerializer.ToCommand(unitEnv));
            Assert.IsInstanceOf<AttackCommand>(CommandSerializer.ToCommand(buildingEnv));
        }

        [Test]
        public void RoundTrip_Gather_LandWorker_TypedByResourceNodeId()
        {
            Unit worker = MakeUnit(g => g.AddComponent<Gatherer>());
            ResourceNode node = Go("Node").AddComponent<ResourceNode>();
            node.Configure(ResourceType.Gold, 50f);
            NetworkId.Assign(node);

            NetMessageEnvelope e = OverTheWire(CommandSerializer.ForGather(6, FactionId.Player, worker, node));

            Assert.IsTrue(NetworkId.TryGetId(node, out int nodeId));
            Assert.AreEqual(nodeId, e.resourceNetId);
            Assert.IsInstanceOf<AbilityCommand>(CommandSerializer.ToCommand(e));
        }

        [Test]
        public void RoundTrip_TradeRoute_Heal_Convert()
        {
            Unit trader = MakeUnit(g => g.AddComponent<Trader>());
            Market market = MakeBuilding<Market>();
            Assert.IsInstanceOf<TradeRouteCommand>(CommandSerializer.ToCommand(OverTheWire(CommandSerializer.ForTradeRoute(2, FactionId.Player, trader, market))));

            Unit healer = MakeUnit(g => g.AddComponent<VaidyaHealer>());
            Unit hurt = MakeUnit(g => g.AddComponent<Attackable>().Configure(30f));
            Assert.IsInstanceOf<AbilityCommand>(CommandSerializer.ToCommand(OverTheWire(CommandSerializer.ForHeal(2, FactionId.Player, healer, hurt.GetComponent<Attackable>()))));

            Unit priest = MakeUnit(g => g.AddComponent<PurohitaConverter>());
            Assert.IsInstanceOf<AbilityCommand>(CommandSerializer.ToCommand(OverTheWire(CommandSerializer.ForConvert(2, FactionId.Player, priest, hurt.GetComponent<Attackable>()))));
        }

        [Test]
        public void Envelope_RoundTrip_KeepsSequenceConfigAndTypedFields()
        {
            var e = new NetMessageEnvelope { kind = NetMessageKind.ConfigCheck, seq = 17, configHash = -5, timeLimitMinutes = 30, regicide = 1, targetKind = 1, resourceNetId = 8 };
            NetMessageEnvelope r = OverTheWire(e);
            Assert.AreEqual((17, -5, 30, 1, 1, 8), (r.seq, r.configHash, r.timeLimitMinutes, r.regicide, r.targetKind, r.resourceNetId));
            Assert.AreEqual(NetMessageKind.ConfigCheck, r.kind);
        }

        [Test]
        public void UnresolvedTarget_IsCountedAndReported_NotSilentlyDropped()
        {
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, true);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Network\] UnresolvedTarget"));

            NetworkDriver.HandleCommand(new NetMessageEnvelope { kind = NetMessageKind.Move, faction = (int)FactionId.Enemy, seq = 1, tick = 5, unitNetId = 4321 });
            NetworkDriver.HandleCommand(new NetMessageEnvelope { kind = NetMessageKind.Gather, faction = (int)FactionId.Enemy, seq = 2, tick = 5, unitNetId = 4321, resourceNetId = 77 });

            Assert.AreEqual(2, NetworkDiagnostics.Count(NetworkIssue.UnresolvedTarget));
        }

        [Test]
        public void SenderBinding_RejectsCommandsClaimingTheLocalOrAnotherFaction()
        {
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, true);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Network\] WrongSender"));

            Assert.IsFalse(NetworkMatch.AcceptRemoteCommand(new NetMessageEnvelope { kind = NetMessageKind.Move, faction = (int)FactionId.Player, seq = 1 }),
                "The remote peer must not be able to issue orders for my faction.");
            Assert.IsTrue(NetworkMatch.AcceptRemoteCommand(new NetMessageEnvelope { kind = NetMessageKind.Move, faction = (int)FactionId.Enemy, seq = 1 }));
            Assert.AreEqual(1, NetworkDiagnostics.Count(NetworkIssue.WrongSender));
        }

        [Test]
        public void Duplicates_AndReplays_AreRejected()
        {
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, true);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Network\] DuplicateCommand"));
            var e1 = new NetMessageEnvelope { kind = NetMessageKind.Move, faction = (int)FactionId.Enemy, seq = 5 };

            Assert.IsTrue(NetworkMatch.AcceptRemoteCommand(e1));
            Assert.IsFalse(NetworkMatch.AcceptRemoteCommand(e1), "Exact duplicate.");
            Assert.IsFalse(NetworkMatch.AcceptRemoteCommand(new NetMessageEnvelope { kind = NetMessageKind.Move, faction = (int)FactionId.Enemy, seq = 3 }), "Older sequence (replay).");
            Assert.IsTrue(NetworkMatch.AcceptRemoteCommand(new NetMessageEnvelope { kind = NetMessageKind.Move, faction = (int)FactionId.Enemy, seq = 6 }));
        }

        private sealed class Recorder : Command
        {
            private readonly List<string> _log;
            private readonly string _name;
            public Recorder(FactionId faction, int seq, List<string> log) : base(faction)
            {
                Sequence = seq;
                _log = log;
                _name = faction + ":" + seq;
            }
            public override void Execute() => _log.Add(_name);
        }

        [Test]
        public void CommandOrder_IsCanonical_RegardlessOfArrivalOrder()
        {
            var a = new List<string>();
            var b = new List<string>();
            const int tickA = 900001, tickB = 900002;

            // Peer 1 hears its own command first; peer 2 hears the remote one first.
            CommandBus.EnqueueAt(tickA, new Recorder(FactionId.Player, 2, a));
            CommandBus.EnqueueAt(tickA, new Recorder(FactionId.Enemy, 1, a));
            CommandBus.EnqueueAt(tickA, new Recorder(FactionId.Player, 1, a));
            CommandBus.EnqueueAt(tickB, new Recorder(FactionId.Player, 1, b));
            CommandBus.EnqueueAt(tickB, new Recorder(FactionId.Player, 2, b));
            CommandBus.EnqueueAt(tickB, new Recorder(FactionId.Enemy, 1, b));
            CommandBus.ExecuteTick(tickA);
            CommandBus.ExecuteTick(tickB);

            CollectionAssert.AreEqual(new[] { "Player:1", "Player:2", "Enemy:1" }, a);
            CollectionAssert.AreEqual(a, b, "Both arrival orders must execute identically.");
        }

        [Test]
        public void LateNetworkCommand_IsRejectedAndReported()
        {
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, true);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Network\] LateCommand"));
            var log = new List<string>();
            CommandBus.ExecuteTick(950010);

            CommandBus.EnqueueAt(950005, new Recorder(FactionId.Enemy, 1, log));
            CommandBus.ExecuteTick(950005);

            Assert.IsEmpty(log);
            Assert.AreEqual(1, NetworkDiagnostics.Count(NetworkIssue.LateCommand));
        }

        [Test]
        public void ConfigCheck_MismatchRaisesFault_MatchDoesNot()
        {
            MatchConfiguration mine = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha, seed: 7, secondSlotIsHuman: true);
            MatchConfiguration.Begin(mine);

            NetworkDriver.HandleConfigCheck(new NetMessageEnvelope { kind = NetMessageKind.ConfigCheck, configHash = mine.ComputeHash() });
            Assert.IsNull(NetworkMatch.Fault);

            LogAssert.Expect(LogType.Warning, new Regex(@"ConfigMismatch"));
            LogAssert.Expect(LogType.Error, new Regex(@"FAULT"));
            NetworkDriver.HandleConfigCheck(new NetMessageEnvelope { kind = NetMessageKind.ConfigCheck, configHash = mine.ComputeHash() + 1 });
            Assert.IsNotNull(NetworkMatch.Fault);
        }

        [Test]
        public void ConfigHash_IsIdenticalOnBothPeers_DespiteDifferentLocalFaction()
        {
            NetworkMatch.SetForTests(FactionId.Player, FactionId.Enemy, true);
            int host = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha, seed: 7, secondSlotIsHuman: true).ComputeHash();
            NetworkMatch.SetForTests(FactionId.Enemy, FactionId.Player, true);
            MatchConfiguration joiner = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha, seed: 7, secondSlotIsHuman: true);

            Assert.AreEqual(FactionId.Enemy, joiner.LocalFaction);
            Assert.AreEqual(host, joiner.ComputeHash());
        }

        [Test]
        public void ResourceNodes_GetStableNetworkIds_AssignIsIdempotent()
        {
            ResourceNode n = Go("Node").AddComponent<ResourceNode>();
            int a = NetworkId.Assign(n);
            int b = NetworkId.Assign(n);

            Assert.AreEqual(a, b);
            Assert.IsTrue(NetworkId.TryResolveNode(a, out ResourceNode back));
            Assert.AreSame(n, back);
        }

        [Test]
        public void Fog_TreatsTheLocalFactionAsVisible_AndTheRemoteAsFogged()
        {
            NetworkMatch.SetForTests(FactionId.Enemy, FactionId.Player, true);

            Assert.IsFalse(FogOfWarManager.IsFogged(FactionId.Enemy), "The joiner must see its own units.");
            Assert.IsTrue(FogOfWarManager.IsFogged(FactionId.Player));
            Assert.IsTrue(VisionSource.IsTracked(FactionId.Enemy), "Both peers' units carry vision in a LAN match.");
        }

        [Test]
        public void Victory_IsEvaluatedFromTheLocalPlayersPerspective()
        {
            // Local = Enemy2 (isolated from other fixtures), opponent = Player.
            NetworkMatch.SetForTests(FactionId.Enemy2, FactionId.Player, true);
            var localUnit = Go("Mine");
            localUnit.AddComponent<FactionMember>().Configure(FactionId.Enemy2);
            Unit lu = localUnit.AddComponent<Unit>();
            if (!Unit.All.Contains(lu)) Unit.All.Add(lu);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.EvaluateSkirmishOutcome(false), "I still have forces and nobody hostile does.");

            Unit.All.Remove(lu);
            Object.DestroyImmediate(localUnit);
            Assert.AreEqual(MatchOutcome.Defeat, MatchManager.EvaluateSkirmishOutcome(false), "I lost everything: defeat for ME, not judged from Player's side.");
        }
    }
}

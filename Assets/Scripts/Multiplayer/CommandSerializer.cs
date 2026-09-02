using System;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer.Wire;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: the one place that knows how to go
    // between a real (unserializable) Command and its wire-format
    // NetMessageEnvelope. Deliberately NOT a method on Command itself -
    // Move/Train/Build/Attack carry live refs/closures precisely because
    // there's no shared "trainable"/"placeable"/"attacker" interface across
    // Barracks/TownCenter/BuildingPlacer/MeleeAttacker/BoatAttacker (see
    // each Command subtype's own comment) - this class is where that same
    // per-kind knowledge lives for the network direction instead.
    public static class CommandSerializer
    {
        // --- Build an outgoing envelope from data already at hand at each
        // origination call site (SelectionManager/BuildMenu/BuildingPlacer)
        // - avoids needing to reflect into Command's private fields just to
        // recover what the caller already knew a moment earlier. ---

        public static NetMessageEnvelope ForMove(int tick, FactionId faction, Unit unit, Vector3 destination)
        {
            NetworkId.TryGetId(unit, out int unitId);
            return new NetMessageEnvelope
            {
                kind = NetMessageKind.Move,
                tick = tick,
                faction = (int)faction,
                unitNetId = unitId,
                destination = destination,
            };
        }

        public static NetMessageEnvelope ForTrain(int tick, FactionId faction, Building source, NetTrainKind trainKind)
        {
            NetworkId.TryGetId(source, out int buildingId);
            return new NetMessageEnvelope
            {
                kind = NetMessageKind.Train,
                tick = tick,
                faction = (int)faction,
                sourceBuildingNetId = buildingId,
                trainKind = trainKind,
            };
        }

        public static NetMessageEnvelope ForBuild(int tick, FactionId faction, NetBuildKind buildKind, Vector3 point)
        {
            return new NetMessageEnvelope
            {
                kind = NetMessageKind.Build,
                tick = tick,
                faction = (int)faction,
                buildKind = buildKind,
                point = point,
            };
        }

        public static NetMessageEnvelope ForAttack(int tick, FactionId faction, Unit attackerUnit, Attackable target)
        {
            NetworkId.TryGetId(attackerUnit, out int attackerId);
            int targetId = -1;
            if (target.TryGetComponent(out Unit targetUnit))
            {
                NetworkId.TryGetId(targetUnit, out targetId);
            }

            return new NetMessageEnvelope
            {
                kind = NetMessageKind.Attack,
                tick = tick,
                faction = (int)faction,
                attackerNetId = attackerId,
                targetNetId = targetId,
            };
        }

        // --- Reconstruct a real, executable Command from a received
        // envelope, resolving NetworkIds back to live objects. Returns null
        // (caller must no-op) if a referenced object no longer exists - the
        // exact same staleness a locally-originated Command already
        // tolerates (see each Command subtype's own null guard), just
        // resolved at receive time instead of at Execute() time. ---

        public static Command ToCommand(NetMessageEnvelope envelope)
        {
            FactionId faction = (FactionId)envelope.faction;

            switch (envelope.kind)
            {
                case NetMessageKind.Move:
                    return ToMoveCommand(envelope, faction);
                case NetMessageKind.Train:
                    return ToTrainCommand(envelope, faction);
                case NetMessageKind.Build:
                    return ToBuildCommand(envelope, faction);
                case NetMessageKind.Attack:
                    return ToAttackCommand(envelope, faction);
                default:
                    return null;
            }
        }

        private static Command ToMoveCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            if (!NetworkId.TryResolveUnit(envelope.unitNetId, out Unit unit) || !unit.TryGetComponent(out UnitMover mover))
            {
                return null;
            }

            return new MoveCommand(faction, mover, envelope.destination);
        }

        private static Command ToTrainCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            if (!NetworkId.TryResolveBuilding(envelope.sourceBuildingNetId, out Building building))
            {
                return null;
            }

            // Barracks/TownCenter/Dock share no common "trainable" interface
            // (see TrainCommand.cs's own comment) - this switch is the
            // network-side equivalent of BuildMenu's own per-button
            // RequestTrain* call sites.
            if (building is Barracks barracks)
            {
                Action requestTrain = envelope.trainKind switch
                {
                    NetTrainKind.Soldier => barracks.RequestTrain,
                    NetTrainKind.Archer => barracks.RequestTrainArcher,
                    NetTrainKind.Cavalry => barracks.RequestTrainCavalry,
                    NetTrainKind.Siege => barracks.RequestTrainSiege,
                    NetTrainKind.Spearman => barracks.RequestTrainSpearman,
                    NetTrainKind.UniqueUnit => barracks.RequestTrainUniqueUnit,
                    NetTrainKind.UniqueUnitSlot0 => () => barracks.RequestTrainUniqueUnit(0),
                    NetTrainKind.UniqueUnitSlot1 => () => barracks.RequestTrainUniqueUnit(1),
                    _ => null,
                };
                return requestTrain == null ? null : new TrainCommand(faction, barracks, requestTrain);
            }

            if (building is TownCenter townCenter && envelope.trainKind == NetTrainKind.Soldier)
            {
                return new TrainCommand(faction, townCenter, townCenter.RequestTrain);
            }

            if (building is Dock dock)
            {
                Action requestTrain = envelope.trainKind switch
                {
                    NetTrainKind.FishingBoat => dock.RequestTrainFishingBoat,
                    NetTrainKind.WarGalley => dock.RequestTrainWarGalley,
                    _ => null,
                };
                return requestTrain == null ? null : new TrainCommand(faction, dock, requestTrain);
            }

            return null;
        }

        private static Command ToBuildCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            BuildingPlacer placer = UnityEngine.Object.FindFirstObjectByType<BuildingPlacer>();
            if (placer == null)
            {
                return null;
            }

            return new BuildCommand(faction, placer, () => placer.ExecuteBuildFromNetwork(envelope.buildKind, envelope.point));
        }

        private static Command ToAttackCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            // Both sides can have gone stale in the delay window between
            // the remote peer originating this order and it arriving here -
            // same tolerance AttackCommand.Execute's own guard already has,
            // just checked at receive time instead.
            if (!NetworkId.TryResolveUnit(envelope.attackerNetId, out Unit attackerUnit))
            {
                return null;
            }

            if (!NetworkId.TryResolveUnit(envelope.targetNetId, out Unit targetUnit) || !targetUnit.TryGetComponent(out Attackable target))
            {
                return null;
            }

            if (attackerUnit.TryGetComponent(out MeleeAttacker melee))
            {
                return new AttackCommand(faction, melee, target, melee.AttackMove);
            }

            if (attackerUnit.TryGetComponent(out BoatAttacker boat))
            {
                return new AttackCommand(faction, boat, target, boat.AttackMove);
            }

            return null;
        }
    }
}

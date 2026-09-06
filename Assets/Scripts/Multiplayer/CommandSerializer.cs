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

        // Wave 4 item 26: reuses Attack's own field shape (one unit id, one
        // building id) rather than adding new envelope fields - see
        // NetMessageEnvelope's own comment on attackerNetId/targetNetId.
        public static NetMessageEnvelope ForTradeRoute(int tick, FactionId faction, Unit trader, Building destination)
        {
            NetworkId.TryGetId(trader, out int traderId);
            NetworkId.TryGetId(destination, out int destinationId);

            return new NetMessageEnvelope
            {
                kind = NetMessageKind.TradeRoute,
                tick = tick,
                faction = (int)faction,
                attackerNetId = traderId,
                targetNetId = destinationId,
            };
        }

        // Wave 4 item 27: reuses Attack's own field shape, same reasoning
        // as ForTradeRoute above - both sides here are already Units, so
        // ForAttack's own attackerNetId/targetNetId resolve them directly.
        public static NetMessageEnvelope ForHeal(int tick, FactionId faction, Unit healer, Attackable target)
        {
            NetworkId.TryGetId(healer, out int healerId);
            int targetId = -1;
            if (target.TryGetComponent(out Unit targetUnit))
            {
                NetworkId.TryGetId(targetUnit, out targetId);
            }

            return new NetMessageEnvelope
            {
                kind = NetMessageKind.Heal,
                tick = tick,
                faction = (int)faction,
                attackerNetId = healerId,
                targetNetId = targetId,
            };
        }

        public static NetMessageEnvelope ForConvert(int tick, FactionId faction, Unit converter, Attackable target)
        {
            NetworkId.TryGetId(converter, out int converterId);
            int targetId = -1;
            if (target.TryGetComponent(out Unit targetUnit))
            {
                NetworkId.TryGetId(targetUnit, out targetId);
            }

            return new NetMessageEnvelope
            {
                kind = NetMessageKind.Convert,
                tick = tick,
                faction = (int)faction,
                attackerNetId = converterId,
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
                case NetMessageKind.TradeRoute:
                    return ToTradeRouteCommand(envelope, faction);
                case NetMessageKind.Heal:
                    return ToHealCommand(envelope, faction);
                case NetMessageKind.Convert:
                    return ToConvertCommand(envelope, faction);
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
                    NetTrainKind.Chara => barracks.RequestTrainChara,
                    NetTrainKind.Skirmisher => barracks.RequestTrainSkirmisher,
                    NetTrainKind.BatteringRam => barracks.RequestTrainBatteringRam,
                    NetTrainKind.CavalryArcher => barracks.RequestTrainCavalryArcher,
                    NetTrainKind.CamelRider => barracks.RequestTrainCamelRider,
                    NetTrainKind.Scorpion => barracks.RequestTrainScorpion,
                    _ => null,
                };
                return requestTrain == null ? null : new TrainCommand(faction, barracks, requestTrain);
            }

            // Wave 2 item 7: unique-unit training moved off Barracks onto
            // Durg this session - same NetTrainKind values, just resolved
            // against a different building type from here on.
            if (building is Durg durg)
            {
                Action requestTrain = envelope.trainKind switch
                {
                    NetTrainKind.UniqueUnit => durg.RequestTrainUniqueUnit,
                    NetTrainKind.UniqueUnitSlot0 => () => durg.RequestTrainUniqueUnit(0),
                    NetTrainKind.UniqueUnitSlot1 => () => durg.RequestTrainUniqueUnit(1),
                    NetTrainKind.Hero => durg.RequestTrainHero,
                    _ => null,
                };
                return requestTrain == null ? null : new TrainCommand(faction, durg, requestTrain);
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
                    NetTrainKind.FireShip => dock.RequestTrainFireShip,
                    NetTrainKind.TradeShip => dock.RequestTrainTradeShip,
                    _ => null,
                };
                return requestTrain == null ? null : new TrainCommand(faction, dock, requestTrain);
            }

            // Wave 4 item 26: Vanik trains here, not Barracks - Market's
            // own unit.
            if (building is Market market && envelope.trainKind == NetTrainKind.Vanik)
            {
                return new TrainCommand(faction, market, market.RequestTrainVanik);
            }

            // Wave 4 item 27: Vaidya/Purohita train here.
            if (building is Monastery monastery)
            {
                Action requestTrain = envelope.trainKind switch
                {
                    NetTrainKind.Vaidya => monastery.RequestTrainVaidya,
                    NetTrainKind.Purohita => monastery.RequestTrainPurohita,
                    _ => null,
                };
                return requestTrain == null ? null : new TrainCommand(faction, monastery, requestTrain);
            }

            return null;
        }

        // Wave 4 item 27: resolves a received heal order back to a real
        // VaidyaHealer call - same resolution shape as ToAttackCommand
        // (both sides are Units), but wraps VaidyaHealer.HealAt in an
        // AbilityCommand instead of an AttackCommand.
        private static Command ToHealCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            if (!NetworkId.TryResolveUnit(envelope.attackerNetId, out Unit healerUnit)
                || !healerUnit.TryGetComponent(out KingdomsOfBharat.ResourceGathering.VaidyaHealer healer))
            {
                return null;
            }

            if (!NetworkId.TryResolveUnit(envelope.targetNetId, out Unit targetUnit) || !targetUnit.TryGetComponent(out Attackable target))
            {
                return null;
            }

            return new AbilityCommand(faction, healerUnit, () => healer.HealAt(target));
        }

        // Wave 4 item 27: resolves a received convert order back to a real
        // PurohitaConverter call.
        private static Command ToConvertCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            if (!NetworkId.TryResolveUnit(envelope.attackerNetId, out Unit converterUnit)
                || !converterUnit.TryGetComponent(out KingdomsOfBharat.ResourceGathering.PurohitaConverter converter))
            {
                return null;
            }

            if (!NetworkId.TryResolveUnit(envelope.targetNetId, out Unit targetUnit) || !targetUnit.TryGetComponent(out Attackable target))
            {
                return null;
            }

            return new AbilityCommand(faction, converterUnit, () => converter.ConvertAt(target));
        }

        // Wave 4 item 26: resolves a received trade-route order back to a
        // real Trader/BoatTrader call - checks both possible trader
        // component types against both possible destination building
        // types, same "no shared interface" reasoning as ToTrainCommand's
        // per-building-type switch above.
        private static Command ToTradeRouteCommand(NetMessageEnvelope envelope, FactionId faction)
        {
            if (!NetworkId.TryResolveUnit(envelope.attackerNetId, out Unit traderUnit))
            {
                return null;
            }

            if (!NetworkId.TryResolveBuilding(envelope.targetNetId, out Building destination))
            {
                return null;
            }

            if (traderUnit.TryGetComponent(out KingdomsOfBharat.ResourceGathering.Trader trader)
                && destination is Market market)
            {
                return new TradeRouteCommand(faction, traderUnit, () => trader.SetTradeRoute(market));
            }

            if (traderUnit.TryGetComponent(out KingdomsOfBharat.ResourceGathering.BoatTrader boatTrader)
                && destination is Dock dock)
            {
                return new TradeRouteCommand(faction, traderUnit, () => boatTrader.SetTradeRoute(dock));
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

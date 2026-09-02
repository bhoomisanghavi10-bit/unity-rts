using System.Collections.Generic;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: a stable, wire-friendly integer identity
    // for units/buildings. Unit.All/Building.All (see Unit.cs/Building.cs)
    // already track every live instance in spawn order, and StateHash
    // already relies on that order being deterministic given a
    // deterministic command stream - this piggybacks on the exact same
    // guarantee rather than introducing a second one. Assignment happens
    // once, centrally, from Unit.OnEnable/Building.OnEnable, so no factory
    // needed to change to get a NetworkId - every spawn path already funnels
    // through one of those two OnEnable methods.
    //
    // Because both peers execute the identical CommandBus-scheduled spawn
    // sequence in the identical order once tick-gated (see SimClock.cs),
    // both peers assign the same NetworkId to "the same" unit/building with
    // no handshake required - this is what lets a received NetTrainCommand/
    // NetBuildCommand/NetAttackCommand/NetMoveCommand's integer IDs resolve
    // to the correct local object on the receiving peer.
    public static class NetworkId
    {
        private static int _nextUnitId;
        private static int _nextBuildingId;
        private static readonly Dictionary<int, Unit> _units = new Dictionary<int, Unit>();
        private static readonly Dictionary<int, Building> _buildings = new Dictionary<int, Building>();
        private static readonly Dictionary<Unit, int> _unitIds = new Dictionary<Unit, int>();
        private static readonly Dictionary<Building, int> _buildingIds = new Dictionary<Building, int>();

        // Called once at the very start of CivilizationSetup.BeginMatchCore,
        // before any gated spawner activates - a match's initial units/
        // buildings are spawned synchronously during that activation, before
        // SimClock/CommandBus even exist as a running tick stream, so IDs
        // must start counting from a clean slate there rather than at
        // SimClock's own match-start point (which fires one frame later).
        public static void Reset()
        {
            _nextUnitId = 0;
            _nextBuildingId = 0;
            _units.Clear();
            _buildings.Clear();
            _unitIds.Clear();
            _buildingIds.Clear();
        }

        public static int Assign(Unit unit)
        {
            int id = _nextUnitId++;
            _units[id] = unit;
            _unitIds[unit] = id;
            return id;
        }

        public static int Assign(Building building)
        {
            int id = _nextBuildingId++;
            _buildings[id] = building;
            _buildingIds[building] = id;
            return id;
        }

        public static bool TryResolveUnit(int id, out Unit unit)
        {
            if (_units.TryGetValue(id, out unit) && unit != null)
            {
                return true;
            }

            unit = null;
            return false;
        }

        public static bool TryResolveBuilding(int id, out Building building)
        {
            if (_buildings.TryGetValue(id, out building) && building != null)
            {
                return true;
            }

            building = null;
            return false;
        }

        public static bool TryGetId(Unit unit, out int id)
        {
            return _unitIds.TryGetValue(unit, out id);
        }

        public static bool TryGetId(Building building, out int id)
        {
            return _buildingIds.TryGetValue(building, out id);
        }
    }
}

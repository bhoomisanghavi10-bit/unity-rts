using System.Collections.Generic;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;

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
        private static int _nextNodeId;
        private static readonly Dictionary<int, ResourceNode> _nodes = new Dictionary<int, ResourceNode>();
        private static readonly Dictionary<ResourceNode, int> _nodeIds = new Dictionary<ResourceNode, int>();
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
            _nextNodeId = 0;
            _nodes.Clear();
            _nodeIds.Clear();
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

        // Resource nodes: assigned in creation order, which is identical on
        // both peers because the world is generated from the shared match
        // seed (MatchConfiguration substream "resources").
        public static int Assign(ResourceNode node)
        {
            if (_nodeIds.TryGetValue(node, out int existing))
            {
                return existing;
            }

            int id = _nextNodeId++;
            _nodes[id] = node;
            _nodeIds[node] = id;
            return id;
        }

        public static bool TryResolveNode(int id, out ResourceNode node)
        {
            if (_nodes.TryGetValue(id, out node) && node != null)
            {
                return true;
            }

            node = null;
            return false;
        }

        public static bool TryGetId(ResourceNode node, out int id)
        {
            return _nodeIds.TryGetValue(node, out id);
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

        // Save/load only: reassigns a restored unit/building back to its
        // ORIGINAL saved id, overriding whatever fresh sequential id
        // Unit.OnEnable/Building.OnEnable already auto-assigned it moments
        // earlier when EntitySpawner created it during restore. Keeps a
        // restored entity's stable runtime identity stable across a
        // save/load round trip - separate from and independent of its
        // (string) DefinitionId, which identifies WHAT it is, not WHICH
        // one. Bumps the next-id counter past whatever was explicitly
        // reassigned so a later, genuinely new spawn in the same
        // (resumed) match can never collide with a restored id.
        public static void Reassign(Unit unit, int id)
        {
            if (_unitIds.TryGetValue(unit, out int oldId))
            {
                _units.Remove(oldId);
            }

            _units[id] = unit;
            _unitIds[unit] = id;
            if (id >= _nextUnitId)
            {
                _nextUnitId = id + 1;
            }
        }

        public static void Reassign(Building building, int id)
        {
            if (_buildingIds.TryGetValue(building, out int oldId))
            {
                _buildings.Remove(oldId);
            }

            _buildings[id] = building;
            _buildingIds[building] = id;
            if (id >= _nextBuildingId)
            {
                _nextBuildingId = id + 1;
            }
        }
    }
}

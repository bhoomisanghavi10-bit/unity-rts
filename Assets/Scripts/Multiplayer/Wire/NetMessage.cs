using UnityEngine;

namespace KingdomsOfBharat.Multiplayer.Wire
{
    // Phase 5 LAN transport MVP: pure-data wire DTOs for everything a real
    // transport needs to carry. Command (see Command.cs) deliberately
    // captures live object references and closures - fine for a
    // single-process input-delay queue, unserializable as-is. Each DTO
    // below mirrors one Command subtype's semantic intent using only
    // NetworkIds/enums/primitives, and CommandSerializer.cs converts
    // between the two directions.
    //
    // One JSON object per message, chosen over a binary format because
    // MatchSaveData already round-trips via JsonUtility for the F5/F9
    // quicksave feature (see SaveManager.cs) - reusing that convention
    // means no new serialization library for a LAN-only MVP where message
    // size/parse speed isn't a real constraint. Kind discriminates which
    // payload field is populated (JsonUtility can't serialize polymorphic
    // types or a C# union, so every payload field exists on every message
    // and only the one matching Kind is meaningful) - see
    // NetMessageEnvelope.
    public enum NetMessageKind
    {
        Move,
        Train,
        Build,
        Attack,
        StateHash,
        ResyncSnapshot,
        // Sent every tick even when the local player issues no order that
        // tick - a real lockstep peer can't otherwise tell "no message has
        // arrived yet" apart from "no command was issued this tick" (see
        // SimClock.cs's tick-gating).
        Heartbeat,
        // Handshake-only messages exchanged before either side calls
        // CivilizationSetup.BeginNetworkMatch - see LanMatchMenu.cs.
        HostHello,
        JoinHello,
    }

    public enum NetTrainKind
    {
        Soldier,
        Archer,
        Cavalry,
        Siege,
        Spearman,
        UniqueUnit,
        UniqueUnitSlot0,
        UniqueUnitSlot1,
        FishingBoat,
        WarGalley,
    }

    // Mirrors BuildingPlacer.BuildingKind (internal enum nested in that
    // class) one-for-one - duplicated rather than referenced directly so
    // this file has no dependency on KingdomsOfBharat.Buildings, keeping
    // the wire layer's own namespace dependency shallow. CommandSerializer
    // is the single place that maps between the two.
    public enum NetBuildKind
    {
        Barracks,
        Farm,
        House,
        Wall,
        Gate,
        Tower,
        Market,
        Dock,
        LumberCamp,
        MiningCamp,
        Mill,
        Durg,
        Karmashala,
    }

    [System.Serializable]
    public class NetMessageEnvelope
    {
        public NetMessageKind kind;
        public int tick;
        public int faction;

        // Move
        public int unitNetId;
        public Vector3 destination;

        // Train
        public int sourceBuildingNetId;
        public NetTrainKind trainKind;

        // Build
        public NetBuildKind buildKind;
        public Vector3 point;

        // Attack
        public int attackerNetId;
        public int targetNetId;

        // StateHash
        public uint hash;

        // ResyncSnapshot - MatchSaveData is already [Serializable]/
        // JsonUtility-round-tripped for F5/F9 (see SaveManager.cs); embedded
        // as its own JSON string rather than a nested object so
        // JsonUtility.FromJson<NetMessageEnvelope> doesn't need to know
        // MatchSaveData's shape up front (it's assembled from Core, which
        // Multiplayer.Wire otherwise has no reason to depend on).
        public string snapshotJson;

        // HostHello / JoinHello
        public int hostCivilization;
        public int remoteCivilization;
        public int mapId;
        public int seed;

        // HostHello, optional - item 6 (Scenario Editor, heavy path
        // session 5): non-empty means the host chose to play a saved
        // custom scenario instead of a plain skirmish. Same "embed as its
        // own JSON string" convention snapshotJson already establishes
        // above, for the same reason (CustomScenarioData is assembled
        // from Core, which Multiplayer.Wire otherwise has no reason to
        // depend on). Empty/null (the default) means "normal skirmish" -
        // this codebase's established convention for optional fields
        // elsewhere (e.g. ScenarioDefinition.VictoryText).
        public string scenarioJson;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Plain JsonUtility-serializable data - no Dictionary (JsonUtility can't
    // serialize one), so per-faction/per-class lookups are flattened into
    // lists of small entry structs instead. Every field here is a snapshot
    // taken by SaveManager.Capture(), not a live reference to anything -
    // once captured, changing the running game state doesn't change a
    // MatchSaveData already written to disk.
    [Serializable]
    public class MatchSaveData
    {
        public int mapId;
        public List<FactionSaveData> factions = new List<FactionSaveData>();
        public List<UnitSaveData> units = new List<UnitSaveData>();
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
        // Item 48: only ever contains Allied pairs - War is DiplomacyRegistry's
        // default for any pair never explicitly set, so a save from before
        // diplomacy existed (or a match that never touched it) loads with
        // an empty list and every faction defaults back to War, matching
        // the pre-diplomacy save format exactly.
        public List<AllianceEntry> alliances = new List<AllianceEntry>();
    }

    [Serializable]
    public class AllianceEntry
    {
        public int factionA;
        public int factionB;
    }

    [Serializable]
    public class FactionSaveData
    {
        public int faction;
        public int civilization;
        public int currentAge;
        public int attackTier;
        public int armorTier;
        public List<ClassTierEntry> classAttackTiers = new List<ClassTierEntry>();
        public List<ClassTierEntry> classArmorTiers = new List<ClassTierEntry>();
        public List<ResourceEntry> resources = new List<ResourceEntry>();
    }

    [Serializable]
    public class ClassTierEntry
    {
        public int unitClass;
        public int tier;
    }

    [Serializable]
    public class ResourceEntry
    {
        public int resourceType;
        public float amount;
    }

    // unitType is the Factory name ("Worker","Soldier","Archer","Cavalry",
    // "Siege") rather than a UnitClass - UnitClass is a coarser combat
    // category (Worker and Soldier are both Infantry) and can't tell a
    // save/load rebuild which Factory.Spawn to call.
    [Serializable]
    public class UnitSaveData
    {
        public string unitType;
        public int faction;
        public Vector3 position;
        public float health;
        public int stance = -1; // -1 = no StanceController (Worker)
    }

    // buildingType mirrors unitType's role, one per Building subclass
    // ("TownCenter","Barracks","Farm","House","Wall","Gate","Tower",
    // "Market").
    [Serializable]
    public class BuildingSaveData
    {
        public string buildingType;
        public int faction;
        public Vector3 position;
        public float health;
        public bool isComplete;
    }
}

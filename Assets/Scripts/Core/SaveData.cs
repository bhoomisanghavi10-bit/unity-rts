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
        // 0 (the implicit default when this field is absent from the JSON
        // entirely, i.e. any save written before this field existed) means
        // the original, pre-catalog-migration schema - still fully
        // supported: every new field below defaults to its own "not
        // present" sentinel for exactly that case, so the same restore
        // code path handles both without a separate legacy branch. See
        // SaveManager.CurrentSaveVersion/ValidateSaveVersion.
        public int version;
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

        // DefinitionCatalog reference-entity tier ladders (InfantryLineProgress/
        // ArcherLineProgress - Padati's and Dhanurdhara's own progression),
        // NOT this project's other ~12 unit-tier ladders - scoped to the
        // catalog's reference entities only. -1 ("not present") for any
        // save from before this field existed; the restore path skips
        // advancing a tier whenever it reads that sentinel, so an old save
        // is unaffected.
        public int infantryTier = -1;
        public int archerTier = -1;
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

    // Catalog entities store their stable definition ID in unitType. Older saves
    // and unmigrated entities retain the legacy Factory name ("Worker","Soldier","Archer","Cavalry",
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

        // Stable RUNTIME identity (Multiplayer.NetworkId) - separate from
        // and independent of unitType's own stable DEFINITION identity
        // above. unitType says WHAT this is; networkId says WHICH one, so
        // a future save-data extension that references a specific entity
        // (an order, a carried resource, a garrison slot) has a stable
        // handle to point at that survives a save/load round trip. -1 =
        // not present (any save from before this field existed) - the
        // restored entity simply keeps whatever fresh id it was assigned
        // on spawn, matching this project's pre-existing, documented
        // "reconstructed objects receive fresh IDs" behavior.
        public int networkId = -1;
    }

    // buildingType mirrors unitType: stable catalog ID or legacy Building name
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

        // Same stable runtime identity as UnitSaveData.networkId above.
        public int networkId = -1;

        // 0..1 exact construction progress (1f for a building with no
        // ConstructionSite at all, e.g. Town Center - same "no site =
        // always complete" convention isComplete above already uses).
        // -1 = not present (any save from before this field existed) -
        // the restore path falls back to isComplete's own coarser
        // complete-or-freshly-placed behavior in that case, unchanged
        // from before this field existed.
        public float constructionProgress = -1f;

        // Reference-catalog production/research queue example: which
        // catalog unit (if any) this building is training, and Barracks'
        // own Infantry tier-research countdown (Padati's tier ladder).
        // Null/-1 = nothing queued, or a save from before these fields
        // existed - either way, nothing is restored for them.
        public string trainingDefinitionId;
        public float trainingRemaining = -1f;
        public float infantryTierResearchRemaining = -1f;

        // Full production queue (Prompt 9): every queued item, active first,
        // with the cost already paid so a cancel after load still refunds.
        // Empty = nothing queued or a pre-queue save (legacy fields above
        // are then used instead).
        public List<ProductionItemSaveData> productionQueue = new List<ProductionItemSaveData>();
    }

    [Serializable]
    public class ProductionItemSaveData
    {
        public int kind;
        public string label;
        public int[] costTypes;
        public float[] costAmounts;
        public float total;
        public float remaining;
    }
}

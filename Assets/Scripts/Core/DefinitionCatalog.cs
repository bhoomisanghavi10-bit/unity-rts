using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Core
{
    public enum DefinitionKind { Unit, Building }

    // Code-only runtime binding. Never serialized into a ScriptableObject or scene.
    public sealed class EntityDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public DefinitionKind Kind { get; }
        public string DataId { get; }
        public string IconKey { get; }
        public CivilizationId? Civilization { get; }
        public float PlacementHeightOffset { get; }
        public IReadOnlyCollection<string> TrainableDefinitionIds => trainableDefinitionIds;
        public UnitDefinition UnitData => DataId == null ? null : DataRegistry.GetUnit(DataId);
        internal Func<Vector3, FactionId, float, GameObject> Create { get; }
        private readonly HashSet<string> trainableDefinitionIds;

        public EntityDefinition(string id, string displayName, DefinitionKind kind,
            Func<Vector3, FactionId, float, GameObject> create, string dataId = null,
            string iconKey = null, CivilizationId? civilization = null, float placementHeightOffset = 0f,
            IEnumerable<string> trainableDefinitionIds = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id != id.ToLowerInvariant())
                throw new ArgumentException("Definition IDs must be nonempty, lowercase and have no surrounding whitespace.", nameof(id));
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            Create = create ?? throw new ArgumentNullException(nameof(create));
            DataId = dataId;
            IconKey = iconKey;
            Civilization = civilization;
            PlacementHeightOffset = placementHeightOffset;
            this.trainableDefinitionIds = new HashSet<string>(trainableDefinitionIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public bool CanTrain(string definitionId) => trainableDefinitionIds.Contains(definitionId);
    }

    public sealed class DefinitionCatalog
    {
        // Persistence contract: never rename/reuse an ID when display names or code change.
        // t1 means the base roster tier, not the save schema or current researched tier.
        public const string Worker = "unit.common.worker";
        public const string CholaPadati = "unit.chola.padati.t1";
        public const string CholaDhanurdhara = "unit.chola.dhanurdhara.t1";
        public const string TownCenter = "building.common.town_center";
        public const string Barracks = "building.common.barracks";

        public static DefinitionCatalog Default { get; } = new DefinitionCatalog(new[]
        {
            new EntityDefinition(Worker, "Worker", DefinitionKind.Unit,
                (p, f, _) => WorkerFactory.SpawnCore(p, f), "worker", "train_worker"),
            new EntityDefinition(CholaPadati, "Chola Padati", DefinitionKind.Unit,
                (p, f, _) => SoldierFactory.SpawnCore(p, f, InfantryLineProgress.Tiers[0]),
                "soldier", "train_soldier", CivilizationId.Chola),
            new EntityDefinition(CholaDhanurdhara, "Chola Dhanurdhara", DefinitionKind.Unit,
                (p, f, _) => ArcherFactory.SpawnCore(p, f, ArcherLineProgress.Tiers[0]),
                "archer", "train_archer", CivilizationId.Chola),
            new EntityDefinition(TownCenter, "Town Center", DefinitionKind.Building,
                (p, f, _) => TownCenterFactory.PlaceCore(p, f),
                trainableDefinitionIds: new[] { Worker }),
            new EntityDefinition(Barracks, "Barracks", DefinitionKind.Building,
                (p, f, t) => BarracksFactory.PlaceCore(p, f, t), placementHeightOffset: 1f,
                trainableDefinitionIds: new[] { CholaPadati, CholaDhanurdhara }),
        });

        private readonly Dictionary<string, EntityDefinition> definitions =
            new Dictionary<string, EntityDefinition>(StringComparer.Ordinal);
        public IReadOnlyCollection<EntityDefinition> Definitions => definitions.Values;

        public DefinitionCatalog(IEnumerable<EntityDefinition> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            foreach (EntityDefinition entry in entries)
            {
                if (entry == null) throw new ArgumentException("Null definition entry.", nameof(entries));
                if (definitions.ContainsKey(entry.Id))
                    throw new ArgumentException($"Duplicate definition ID '{entry.Id}'.", nameof(entries));
                definitions.Add(entry.Id, entry);
            }
        }

        public bool TryGet(string id, out EntityDefinition definition)
        {
            definition = null;
            return id != null && definitions.TryGetValue(id, out definition);
        }

        public EntityDefinition Get(string id)
        {
            if (!TryGet(id, out EntityDefinition definition))
                throw new ArgumentException($"Unknown definition ID '{id ?? "<null>"}'.", nameof(id));
            return definition;
        }

        public GameObject Spawn(string id, Vector3 position, FactionId faction, float buildTime = 0.01f)
        {
            EntityDefinition definition = Get(id);
            if (definition.Civilization.HasValue && definition.Civilization.Value != CivilizationRegistry.For(faction))
                throw new ArgumentException($"Definition '{id}' requires civilization {definition.Civilization.Value}.", nameof(faction));
            GameObject entity = definition.Create(position, faction, buildTime);
            if (entity == null) throw new InvalidOperationException($"Definition '{id}' returned no entity.");
            var identity = entity.GetComponent<DefinitionId>() ?? entity.AddComponent<DefinitionId>();
            identity.Initialize(id);
            return entity;
        }
    }
}

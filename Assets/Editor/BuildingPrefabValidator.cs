using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Editor
{
    /// <summary>
    /// Lightweight static checks for shipped building prefabs and the definition catalog.
    /// Errors fail the EditMode test and the batch entry point; warnings list known debt.
    /// A building moves from "warning: no LODs" to "error" by being added to LodRequired,
    /// which makes a converted building a ratchet: it cannot silently lose its LODs.
    /// </summary>
    public static class BuildingPrefabValidator
    {
        public const int LodTriangleThreshold = 20000;

        // Buildings whose LOD setup is finished and must stay so. Path relative to Resources/Buildings.
        public static readonly HashSet<string> LodRequired = new HashSet<string> { "Chola/House" };

        private static readonly HashSet<string> KnownBuildingNames = new HashSet<string>
        {
            "TownCenter", "Barracks", "Tower", "Market", "Farm", "House", "Wall", "Gate", "Dock",
            "Durg", "Karmashala", "LumberCamp", "MiningCamp", "Mill", "Monastery",
        };

        public struct Issue
        {
            public bool IsError;
            public string Asset;
            public string Message;
            public override string ToString() => (IsError ? "ERROR " : "warn  ") + Asset + ": " + Message;
        }

        [MenuItem("BharatRTS/Art/Validate Building Prefabs")]
        public static void ValidateMenu()
        {
            List<Issue> issues = ValidateAll();
            foreach (Issue i in issues) (i.IsError ? (System.Action<string>)Debug.LogError : Debug.LogWarning)(i.ToString());
            Debug.Log($"BuildingPrefabValidator: {issues.Count(i => i.IsError)} errors, {issues.Count(i => !i.IsError)} warnings");
        }

        // Batch entry point: -executeMethod KingdomsOfBharat.Editor.BuildingPrefabValidator.RunBatch
        public static void RunBatch()
        {
            List<Issue> issues = ValidateAll();
            foreach (Issue i in issues) Debug.Log(i.ToString());
            EditorApplication.Exit(issues.Any(i => i.IsError) ? 1 : 0);
        }

        public static List<Issue> ValidateAll()
        {
            var issues = new List<Issue>();
            const string root = "Assets/Resources/Buildings";
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/_Source/") || path.Contains("/_Decimated/")) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { issues.Add(new Issue { IsError = true, Asset = path, Message = "prefab failed to load" }); continue; }
                ValidatePrefab(prefab, path.Substring(root.Length + 1).Replace(".prefab", ""), issues);
            }
            ValidateCatalog(issues);
            return issues;
        }

        public static void ValidatePrefab(GameObject prefab, string key, List<Issue> issues)
        {
            void Add(bool error, string msg) => issues.Add(new Issue { IsError = error, Asset = key, Message = msg });

            string baseName = System.IO.Path.GetFileNameWithoutExtension(key).Split('_')[0];
            if (!KnownBuildingNames.Contains(baseName))
                Add(true, $"'{baseName}' is not a building name any factory spawns (identifier typo or orphan prefab)");

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { Add(true, "no renderers"); return; }

            foreach (Renderer r in renderers)
            {
                if (r is MeshRenderer && (r.GetComponent<MeshFilter>() == null || r.GetComponent<MeshFilter>().sharedMesh == null))
                    Add(true, $"renderer '{r.name}' has no mesh");
                if (r.sharedMaterials.Length == 0 || r.sharedMaterials.Any(m => m == null))
                    Add(true, $"renderer '{r.name}' has a missing material");
                else if (r.sharedMaterials.Any(m => m.shader == null || m.shader.name == "Hidden/InternalErrorShader"))
                    Add(true, $"renderer '{r.name}' has a broken shader");
            }
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Any(b => b == null))
                Add(true, "missing script reference on the prefab");

            LODGroup group = prefab.GetComponentInChildren<LODGroup>(true);
            int triangles = renderers.OfType<MeshRenderer>().Where(r => IsLod0(r, group))
                .Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);
            bool required = LodRequired.Contains(key);

            if (group == null)
            {
                if (required) Add(true, "LODs are required for this building but the prefab has no LODGroup");
                else if (triangles > LodTriangleThreshold) Add(false, $"{triangles} tris and no LODGroup");
                return;
            }
            ValidateLods(group, Add);
        }

        private static bool IsLod0(Renderer r, LODGroup group)
        {
            if (group == null) return true;
            LOD[] lods = group.GetLODs();
            return lods.Length > 0 && lods[0].renderers.Contains(r);
        }

        private static void ValidateLods(LODGroup group, System.Action<bool, string> add)
        {
            LOD[] lods = group.GetLODs();
            if (lods.Length < 2) { add(true, "LODGroup has fewer than 2 levels"); return; }
            int previousTriangles = int.MaxValue;
            for (int i = 0; i < lods.Length; i++)
            {
                if (lods[i].renderers == null || lods[i].renderers.Length == 0 || lods[i].renderers.Any(r => r == null))
                { add(true, $"LOD{i} has no renderer or a null renderer"); continue; }
                if (i > 0 && lods[i].screenRelativeTransitionHeight >= lods[i - 1].screenRelativeTransitionHeight)
                    add(true, $"LOD{i} transition {lods[i].screenRelativeTransitionHeight} is not below LOD{i - 1}");
                int tris = lods[i].renderers.OfType<MeshRenderer>().Sum(r => r.GetComponent<MeshFilter>().sharedMesh?.triangles.Length / 3 ?? 0);
                if (tris >= previousTriangles) add(true, $"LOD{i} has {tris} tris, not fewer than the level above ({previousTriangles})");
                previousTriangles = tris;
                // Levels must share materials or the tinted instance count multiplies with LOD count.
                if (i > 0 && !lods[i].renderers[0].sharedMaterials.SequenceEqual(lods[0].renderers[0].sharedMaterials))
                    add(true, $"LOD{i} uses different materials than LOD0");
            }
        }

        private static void ValidateCatalog(List<Issue> issues)
        {
            var seen = new HashSet<string>();
            foreach (EntityDefinition d in DefinitionCatalog.Default.Definitions)
            {
                string expectedPrefix = d.Kind == DefinitionKind.Unit ? "unit." : "building.";
                if (string.IsNullOrEmpty(d.Id) || !d.Id.StartsWith(expectedPrefix))
                    issues.Add(new Issue { IsError = true, Asset = "catalog:" + d.Id, Message = $"{d.Kind} id must start with '{expectedPrefix}'" });
                if (!seen.Add(d.Id))
                    issues.Add(new Issue { IsError = true, Asset = "catalog:" + d.Id, Message = "duplicate id" });
                if (d.Kind == DefinitionKind.Unit && d.DataId != null && d.UnitData == null)
                    issues.Add(new Issue { IsError = true, Asset = "catalog:" + d.Id, Message = $"data id '{d.DataId}' has no UnitDefinition" });
            }
        }
    }
}

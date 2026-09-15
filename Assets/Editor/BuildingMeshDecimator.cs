using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;

namespace KingdomsOfBharat.Editor
{
    /// <summary>
    /// Decimates a civ-specific building's imported mesh (raw Meshy AI exports ship at
    /// ~1.7-2.0M un-decimated triangles, see Roadmap Section 1 item 17) down to a target
    /// triangle count via UnityMeshSimplifier, saving the result as a new Mesh asset under
    /// Buildings/&lt;civId&gt;/_Decimated and repointing the prefab's MeshFilter(s) at it.
    ///
    /// Always re-derives from the original _Source/&lt;Name&gt;/&lt;Name&gt;_model.fbx rather than
    /// the prefab's current (possibly already-decimated) mesh, so re-running with a different
    /// target triangle count is idempotent and never compounds simplification.
    ///
    /// Edits the prefab via LoadPrefabContents -> modify components -> SaveAsPrefabAsset at
    /// the same path (Unity's standard scripted-prefab-editing pattern) rather than manually
    /// unpacking the nested &lt;Name&gt;_model PrefabInstance MeshyBuildingImporter created -
    /// this records the mesh swap as an instance override and requires no unpacking.
    /// </summary>
    public static class BuildingMeshDecimator
    {
        private static readonly string[] BuildingNames =
            { "TownCenter", "Barracks", "Tower", "Market", "Farm", "House", "Wall", "Gate", "Dock" };

        private static readonly string[] CivIds =
            { "Chola", "Vijayanagara", "Rajput", "Maurya", "Maratha" };

        [MenuItem("Tools/Building Pipeline/Decimate Chola TownCenter (POC, 15000 tris)")]
        private static void DecimateCholaTownCenterPocTight()
        {
            DecimateBuilding("Chola", "TownCenter", 15000);
        }

        [MenuItem("Tools/Building Pipeline/Decimate Chola TownCenter (POC, 45000 tris)")]
        private static void DecimateCholaTownCenterPocConservative()
        {
            DecimateBuilding("Chola", "TownCenter", 45000);
        }

        // Age-tiered variants (Ancient/Classical/Durg - see
        // AgeTieredBuildingVisual/BuildingModelFactory's own age-suffixed
        // Resources paths) are a wholly separate system from the 45
        // Imperial-tier prefabs above and were never in this decimator's
        // original scope, so they shipped un-decimated (~1.9-3.0M tris each)
        // even after the Imperial pass landed. TownCenter/Tower/Wall each
        // have Ancient+Classical (shared across all 5 civs) plus a Durg tier
        // - civ-specific for TownCenter (5 separate models), shared for
        // Tower/Wall (same Durg fortification art regardless of civ) - 13
        // prefabs total.
        private static readonly (string civId, string buildingName, string age)[] AgeTieredTargets =
        {
            (null, "TownCenter", "Ancient"),
            (null, "TownCenter", "Classical"),
            ("Chola", "TownCenter", "Durg"),
            ("Vijayanagara", "TownCenter", "Durg"),
            ("Rajput", "TownCenter", "Durg"),
            ("Maurya", "TownCenter", "Durg"),
            ("Maratha", "TownCenter", "Durg"),
            (null, "Tower", "Ancient"),
            (null, "Tower", "Classical"),
            (null, "Tower", "Durg"),
            (null, "Wall", "Ancient"),
            (null, "Wall", "Classical"),
            (null, "Wall", "Durg"),
        };

        [MenuItem("Tools/Building Pipeline/Decimate All Civ Buildings (500000 tris)")]
        private static void DecimateAllMenuConservative()
        {
            DecimateAll(500000);
        }

        [MenuItem("Tools/Building Pipeline/Decimate All Age-Tiered Buildings (500000 tris)")]
        private static void DecimateAllAgeTieredMenuConservative()
        {
            DecimateAllAgeTiered(500000);
        }

        public static void DecimateAll(int targetTriangleCount)
        {
            foreach (string civId in CivIds)
            {
                foreach (string buildingName in BuildingNames)
                {
                    DecimateBuilding(civId, buildingName, targetTriangleCount);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("BuildingMeshDecimator: DecimateAll complete");
            AssetDatabase.Refresh();
        }

        public static void DecimateAllAgeTiered(int targetTriangleCount)
        {
            foreach (var (civId, buildingName, age) in AgeTieredTargets)
            {
                string suffixedName = $"{buildingName}_{age}";
                string fbxFolder = civId == null
                    ? $"Assets/Resources/Buildings/_Source/{suffixedName}"
                    : $"Assets/Resources/Buildings/{civId}/_Source/{suffixedName}";
                string prefabPath = civId == null
                    ? $"Assets/Resources/Buildings/{suffixedName}.prefab"
                    : $"Assets/Resources/Buildings/{civId}/{suffixedName}.prefab";
                string decimatedFolder = civId == null
                    ? "Assets/Resources/Buildings/_Decimated"
                    : $"Assets/Resources/Buildings/{civId}/_Decimated";

                DecimateBuildingAt(fbxFolder, prefabPath, decimatedFolder, suffixedName, targetTriangleCount);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("BuildingMeshDecimator: DecimateAllAgeTiered complete");
            AssetDatabase.Refresh();
        }

        public static void DecimateBuilding(string civId, string buildingName, int targetTriangleCount)
        {
            string fbxFolder = $"Assets/Resources/Buildings/{civId}/_Source/{buildingName}";
            string prefabPath = $"Assets/Resources/Buildings/{civId}/{buildingName}.prefab";
            string decimatedFolder = $"Assets/Resources/Buildings/{civId}/_Decimated";
            DecimateBuildingAt(fbxFolder, prefabPath, decimatedFolder, buildingName, targetTriangleCount);
        }

        // Core, path-agnostic implementation shared by both the Imperial-tier
        // (civ-rooted) and age-tiered (civ-rooted-or-shared) call sites above.
        // Locates the source FBX by scanning the given folder rather than
        // assuming an exact "<name>_model.fbx" filename, since several
        // age-tiered sources were renamed to "<name>_model_v2.fbx" during
        // the September 2026 import-cache-corruption fix (see CLAUDE.md) -
        // hardcoding the old name would silently miss those.
        private static void DecimateBuildingAt(string fbxFolder, string prefabPath, string decimatedFolder, string assetBaseName, int targetTriangleCount)
        {
            string absoluteFbxFolder = Path.GetFullPath(fbxFolder);
            if (!Directory.Exists(absoluteFbxFolder))
            {
                Debug.LogError($"BuildingMeshDecimator: no source folder at {fbxFolder}");
                return;
            }

            string[] fbxFiles = Directory.GetFiles(absoluteFbxFolder, "*.fbx")
                .Where(f => !Path.GetFileName(f).StartsWith("._"))
                .ToArray();
            if (fbxFiles.Length == 0)
            {
                Debug.LogError($"BuildingMeshDecimator: no .fbx file found under {fbxFolder}");
                return;
            }

            string fbxPath = fbxFolder + "/" + Path.GetFileName(fbxFiles[0]);
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogError($"BuildingMeshDecimator: could not load source FBX at {fbxPath}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError($"BuildingMeshDecimator: no prefab at {prefabPath}");
                return;
            }

            string buildingName = assetBaseName;
            GameObject sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            MeshFilter[] sourceFilters = sourceInstance.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.sharedMesh != null).ToArray();

            int originalTotal = sourceFilters.Sum(mf => mf.sharedMesh.triangles.Length / 3);
            if (originalTotal == 0)
            {
                Debug.LogError($"BuildingMeshDecimator: {prefabPath} source mesh has 0 triangles");
                Object.DestroyImmediate(sourceInstance);
                return;
            }

            // Some sources report a genuinely corrupted mesh - a real,
            // non-Unity-cache-related defect found live on Wall_Durg's
            // shared source FBX: vertexCount/bounds both read 0 while
            // triangles.Length still reports millions of stale indices,
            // reproduced identically on a byte-identical fresh-GUID copy
            // and under forced isReadable=true/indexFormat=UInt32, ruling
            // out every import-setting/cache explanation - so this is a
            // content-level defect in the source file itself, not
            // something a reimport or rename can fix. Caught here (rather
            // than left to crash inside MeshSimplifier.Initialize, which
            // silently aborted the rest of DecimateAllAgeTiered's batch
            // loop the first time this was hit) so one bad source degrades
            // gracefully - this building keeps its current mesh untouched
            // and every other target in the same batch still runs.
            bool anyCorrupted = sourceFilters.Any(mf => mf.sharedMesh.vertexCount == 0);
            if (anyCorrupted)
            {
                Debug.LogError($"BuildingMeshDecimator: {prefabPath} source mesh reports 0 vertices with " +
                    $"{originalTotal} stale triangles - a corrupted source FBX (not a cache issue, confirmed " +
                    "via a fresh-GUID copy) - skipping, needs the source re-exported/re-sourced");
                Object.DestroyImmediate(sourceInstance);
                return;
            }

            float quality = Mathf.Clamp01((float)targetTriangleCount / originalTotal);

            Directory.CreateDirectory(Path.GetFullPath(decimatedFolder));

            var decimatedMeshes = new List<Mesh>(sourceFilters.Length);
            try
            {
                for (int i = 0; i < sourceFilters.Length; i++)
                {
                    Mesh sourceMesh = sourceFilters[i].sharedMesh;
                    var simplifier = new MeshSimplifier();
                    simplifier.Initialize(sourceMesh);
                    simplifier.SimplifyMesh(quality);
                    Mesh decimated = simplifier.ToMesh();
                    decimated.name = sourceFilters.Length == 1 ? $"{buildingName}_decimated" : $"{buildingName}_decimated_{i}";

                    string meshAssetPath = $"{decimatedFolder}/{decimated.name}.asset";
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath) != null)
                    {
                        AssetDatabase.DeleteAsset(meshAssetPath);
                    }
                    AssetDatabase.CreateAsset(decimated, meshAssetPath);
                    decimatedMeshes.Add(decimated);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BuildingMeshDecimator: {prefabPath} simplification failed ({ex.GetType().Name}: " +
                    $"{ex.Message}) - skipping, source may be corrupted");
                Object.DestroyImmediate(sourceInstance);
                return;
            }

            Object.DestroyImmediate(sourceInstance);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            MeshFilter[] targetFilters = prefabRoot.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.sharedMesh != null).ToArray();

            if (targetFilters.Length != decimatedMeshes.Count)
            {
                Debug.LogError($"BuildingMeshDecimator: {prefabPath} MeshFilter count mismatch " +
                    $"(prefab has {targetFilters.Length}, source has {decimatedMeshes.Count}) - skipping");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            int decimatedTotal = 0;
            for (int i = 0; i < targetFilters.Length; i++)
            {
                targetFilters[i].sharedMesh = decimatedMeshes[i];
                decimatedTotal += decimatedMeshes[i].triangles.Length / 3;

                MeshCollider collider = targetFilters[i].GetComponent<MeshCollider>();
                if (collider != null) collider.sharedMesh = decimatedMeshes[i];
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            AssetDatabase.SaveAssets();

            // AssetDatabase/Resources.Load can otherwise keep serving a cached prefab
            // graph from before this mesh swap within the same Editor session (found
            // live: AssetDatabase.LoadAssetAtPath and Resources.Load both returned a
            // null MeshFilter.sharedMesh after a re-run, even though the saved .prefab
            // and mesh asset were both correct on disk and PrefabUtility.LoadPrefabContents
            // - which always re-reads from disk - showed the right mesh). Forcing a
            // reimport of just this prefab keeps that cache in sync.
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"BuildingMeshDecimator: {prefabPath} {originalTotal} -> {decimatedTotal} tris " +
                $"(quality={quality:F4}, target={targetTriangleCount})");
        }
    }
}

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

        [MenuItem("Tools/Building Pipeline/Decimate All Civ Buildings (500000 tris)")]
        private static void DecimateAllMenuConservative()
        {
            DecimateAll(500000);
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

        public static void DecimateBuilding(string civId, string buildingName, int targetTriangleCount)
        {
            string fbxPath = $"Assets/Resources/Buildings/{civId}/_Source/{buildingName}/{buildingName}_model.fbx";
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogError($"BuildingMeshDecimator: no source FBX at {fbxPath}");
                return;
            }

            string prefabPath = $"Assets/Resources/Buildings/{civId}/{buildingName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError($"BuildingMeshDecimator: no prefab at {prefabPath}");
                return;
            }

            GameObject sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            MeshFilter[] sourceFilters = sourceInstance.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.sharedMesh != null).ToArray();

            int originalTotal = sourceFilters.Sum(mf => mf.sharedMesh.triangles.Length / 3);
            if (originalTotal == 0)
            {
                Debug.LogError($"BuildingMeshDecimator: {civId}/{buildingName} source mesh has 0 triangles");
                Object.DestroyImmediate(sourceInstance);
                return;
            }

            float quality = Mathf.Clamp01((float)targetTriangleCount / originalTotal);

            string decimatedFolder = $"Assets/Resources/Buildings/{civId}/_Decimated";
            Directory.CreateDirectory(Path.GetFullPath(decimatedFolder));

            var decimatedMeshes = new List<Mesh>(sourceFilters.Length);
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

            Object.DestroyImmediate(sourceInstance);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            MeshFilter[] targetFilters = prefabRoot.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.sharedMesh != null).ToArray();

            if (targetFilters.Length != decimatedMeshes.Count)
            {
                Debug.LogError($"BuildingMeshDecimator: {civId}/{buildingName} MeshFilter count mismatch " +
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

            Debug.Log($"BuildingMeshDecimator: {civId}/{buildingName} {originalTotal} -> {decimatedTotal} tris " +
                $"(quality={quality:F4}, target={targetTriangleCount})");
        }
    }
}

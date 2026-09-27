using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

namespace KingdomsOfBharat.Editor
{
    /// <summary>
    /// Builds a three-level LODGroup for one civ building prefab from its original _Source FBX
    /// (never from an already-decimated mesh, so re-running is idempotent). The prefab keeps its
    /// single "model" renderer as LOD0; LOD1/LOD2 are sibling copies with lower-triangle meshes
    /// saved under the civ's _Decimated folder. All levels share the same materials, so only one
    /// material set exists per building regardless of LOD.
    ///
    /// Also caps the building's albedo/normal at 1024 with compression; a 2048 map on a house that
    /// covers a few dozen screen pixels at gameplay zoom is wasted memory (see docs/art).
    /// </summary>
    public static class BuildingLodBuilder
    {
        // Screen-relative transition heights (fraction of screen height covered by the bounds).
        // LOD1 starts where the building is still large on screen, LOD2 where it is small; culling
        // stays off because a building silhouette must never vanish at any zoom.
        public static readonly float[] Transitions = { 0.30f, 0.10f, 0.0f };

        [MenuItem("BharatRTS/Art/Build LODs - Chola House")]
        public static void BuildCholaHouseMenu() => BuildCholaHouse();

        // Batch entry point: Unity -batchmode -executeMethod KingdomsOfBharat.Editor.BuildingLodBuilder.BuildCholaHouse
        public static void BuildCholaHouse()
        {
            Build("Chola", "House", new[] { 24000, 6000, 1500 }, 1024);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void Build(string civId, string name, int[] lodTriangleTargets, int textureMaxSize)
        {
            string sourceFolder = $"Assets/Resources/Buildings/{civId}/_Source/{name}";
            string prefabPath = $"Assets/Resources/Buildings/{civId}/{name}.prefab";
            string meshFolder = $"Assets/Resources/Buildings/{civId}/_Decimated";
            string fbxPath = Directory.GetFiles(Path.GetFullPath(sourceFolder), "*.fbx")
                .Where(f => !Path.GetFileName(f).StartsWith("._"))
                .Select(f => sourceFolder + "/" + Path.GetFileName(f)).First();

            ConfigureTextures(sourceFolder, textureMaxSize);

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            Mesh source = fbx.GetComponentsInChildren<MeshFilter>(true).First(m => m.sharedMesh != null).sharedMesh;
            int sourceTris = source.triangles.Length / 3;

            Mesh[] lodMeshes = new Mesh[lodTriangleTargets.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                var simplifier = new MeshSimplifier();
                simplifier.Initialize(source);
                simplifier.SimplifyMesh(Mathf.Clamp01((float)lodTriangleTargets[i] / sourceTris));
                Mesh m = simplifier.ToMesh();
                m.name = i == 0 ? $"{name}_decimated" : $"{name}_decimated_lod{i}";
                m.RecalculateBounds();
                string path = $"{meshFolder}/{m.name}.asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(m, path);
                lodMeshes[i] = m;
                Debug.Log($"BuildingLodBuilder: {name} LOD{i} {m.triangles.Length / 3} tris (source {sourceTris})");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (LODGroup old in root.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(old);
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("_LOD")).ToArray())
                    Object.DestroyImmediate(t.gameObject);

                MeshFilter lod0Filter = root.GetComponentsInChildren<MeshFilter>(true).First(m => m.sharedMesh != null);
                MeshRenderer lod0Renderer = lod0Filter.GetComponent<MeshRenderer>();
                lod0Filter.sharedMesh = lodMeshes[0];
                MeshCollider stale = lod0Filter.GetComponent<MeshCollider>();
                if (stale != null) Object.DestroyImmediate(stale); // factory adds one tight BoxCollider

                Renderer[][] levelRenderers = new Renderer[lodMeshes.Length][];
                levelRenderers[0] = new Renderer[] { lod0Renderer };
                for (int i = 1; i < lodMeshes.Length; i++)
                {
                    GameObject copy = new GameObject($"{lod0Filter.gameObject.name}_LOD{i}");
                    copy.transform.SetParent(lod0Filter.transform.parent, false);
                    copy.transform.localPosition = lod0Filter.transform.localPosition;
                    copy.transform.localRotation = lod0Filter.transform.localRotation;
                    copy.transform.localScale = lod0Filter.transform.localScale;
                    copy.AddComponent<MeshFilter>().sharedMesh = lodMeshes[i];
                    MeshRenderer r = copy.AddComponent<MeshRenderer>();
                    EditorUtility.CopySerialized(lod0Renderer, r);
                    // Shadows: every level casts, so the shadow silhouette follows the visible
                    // mesh. Distant LOD2 stops receiving them (cost with no readable benefit).
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.receiveShadows = i < 2;
                    levelRenderers[i] = new Renderer[] { r };
                }

                LODGroup group = root.AddComponent<LODGroup>();
                LOD[] lods = new LOD[lodMeshes.Length];
                for (int i = 0; i < lods.Length; i++)
                    lods[i] = new LOD(Transitions[Mathf.Min(i, Transitions.Length - 1)], levelRenderers[i]);
                group.SetLODs(lods);
                group.fadeMode = LODFadeMode.None; // cross-fade needs dithering the URP shaders here do not all support
                group.RecalculateBounds();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureTextures(string folder, int maxSize)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.crunchedCompression = false;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = false;
                importer.SaveAndReimport();
            }
        }
    }
}

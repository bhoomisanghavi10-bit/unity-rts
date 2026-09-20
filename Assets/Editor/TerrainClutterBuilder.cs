using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.EditorTools
{
    // Builds everything the terrain clutter scatter (Core/TerrainClutter.cs)
    // needs, reproducibly: a crossed-quad grass tuft mesh + alpha-clipped URP
    // Lit material + prefab (from Resources/Terrain/Detail/Tuft.png), a
    // decimated rock mesh from the owned PolishedSurfaces rock set on a
    // plain instancing-enabled URP Lit material (the set's own Shader Graph
    // material isn't suitable for instanced detail meshes), and the
    // TerrainClutterSet asset tying them together. Safe to re-run: it
    // overwrites its own outputs only.
    public static class TerrainClutterBuilder
    {
        private const string Folder = "Assets/Resources/Terrain/Detail";
        private const string RockSource = "Assets/PolishedSurfaces/System_RockSet_Sample/Art";

        [MenuItem("BharatRTS/Build Terrain Clutter Set")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);

            // --- Tuft texture import settings (alpha-tested cards).
            string tuftTex = Folder + "/Tuft.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(tuftTex);
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.mipMapsPreserveCoverage = true;
            ti.alphaTestReferenceValue = 0.5f;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();

            Mesh tuftMesh = SaveMesh(BuildTuftMesh(), Folder + "/TuftMesh.asset");
            Material tuftMat = SaveMaterial(Folder + "/TuftMat.mat", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tuftTex));
                m.SetColor("_BaseColor", new Color(0.78f, 0.86f, 0.52f)); // muted so tufts sit in the terrain grass
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.SetFloat("_Cull", 0f);
                m.SetFloat("_Smoothness", 0.05f);
                m.SetFloat("_Metallic", 0f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = 2450;
            });
            GameObject tuftPrefab = SavePrefab("Tuft", tuftMesh, tuftMat);

            // --- Rock / pebble: decimated LOD from the owned rock set.
            Mesh rockMesh = BuildRockMesh();
            Material rockMat = SaveMaterial(Folder + "/RockClutterMat.mat", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RockSource + "/Textures/1. Small/T_RockSet_01_Small_01_A.png"));
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RockSource + "/Textures/1. Small/T_RockSet_01_Small_01_N.png"));
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Smoothness", 0.25f);
                m.SetFloat("_Metallic", 0f);
            });
            Material pebbleMat = SaveMaterial(Folder + "/PebbleClutterMat.mat", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RockSource + "/Textures/1. Small/T_RockSet_01_Small_01_A.png"));
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RockSource + "/Textures/1. Small/T_RockSet_01_Small_01_N.png"));
                m.EnableKeyword("_NORMALMAP");
                m.SetColor("_BaseColor", new Color(0.62f, 0.58f, 0.52f)); // wet: darker
                m.SetFloat("_Smoothness", 0.55f);
                m.SetFloat("_Metallic", 0f);
            });
            GameObject rockPrefab = SavePrefab("RockClutter", rockMesh, rockMat);
            GameObject pebblePrefab = SavePrefab("PebbleClutter", rockMesh, pebbleMat);

            string setPath = "Assets/Resources/Terrain/ClutterSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<TerrainClutterSet>(setPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<TerrainClutterSet>();
                AssetDatabase.CreateAsset(set, setPath);
            }

            // Only seed the entries on first build. After that the entries are
            // hand-tuned in the Inspector (e.g. swapping the placeholder rock
            // for a purpose-made rock asset), so a re-run must not clobber them.
            if (set.entries == null || set.entries.Length == 0)
            {
                set.entries = new[]
                {
                    new TerrainClutterSet.Entry { name = "Grass tuft", prefab = tuftPrefab, kind = TerrainClutterSet.ClutterKind.Grass, minScale = 0.28f, maxScale = 0.6f, density = 1.3f },
                    new TerrainClutterSet.Entry { name = "Small rock", prefab = rockPrefab, kind = TerrainClutterSet.ClutterKind.Rock, minScale = 0.5f, maxScale = 1.1f, density = 1f },
                    new TerrainClutterSet.Entry { name = "Shore pebble", prefab = pebblePrefab, kind = TerrainClutterSet.ClutterKind.Pebble, minScale = 0.15f, maxScale = 0.4f, density = 1f },
                };
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Terrain clutter set built: " + setPath);
        }

        // Three vertical cards crossed at 60 degrees. Normals point up so the
        // tuft shades like the ground under it instead of flickering per card.
        private static Mesh BuildTuftMesh()
        {
            const float width = 1.0f, height = 0.7f;
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 3; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 60f, 0f);
                int b = verts.Count;
                verts.Add(q * new Vector3(-width * 0.5f, 0f, 0f));
                verts.Add(q * new Vector3(width * 0.5f, 0f, 0f));
                verts.Add(q * new Vector3(-width * 0.5f, height, 0f));
                verts.Add(q * new Vector3(width * 0.5f, height, 0f));
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1));
                tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
            }

            var mesh = new Mesh { name = "TuftMesh" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildRockMesh()
        {
            string fbx = RockSource + "/Meshes/1. Small/SM_Small_01_Sample.fbx";
            Mesh source = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
            {
                if (o is Mesh m && m.name == "Small_01_LOD2") source = m;
            }

            var simplifier = new MeshSimplifier(source);
            simplifier.SimplifyMesh(0.3f);
            Mesh result = simplifier.ToMesh();
            result.name = "RockClutterMesh";
            result.RecalculateBounds();
            return SaveMesh(result, Folder + "/RockClutterMesh.asset");
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                return existing;
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material SaveMaterial(string path, System.Action<Material> configure)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            configure(mat);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject SavePrefab(string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            string path = Folder + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private const string MossFbx = "Assets/importedmodels/RockMossSet/rock_moss_set_02_2k.fbx";
        private const string MossFolder = Folder + "/RockMoss";

        // Poly Haven rock_moss_set_02: 7 mossy rocks sharing one atlas
        // material. Each is rotated Z-up -> Y-up (the FBX mesh data comes in
        // 90 degrees about X), re-based so the pivot is bottom-centre (the
        // originals aren't consistently bottom-aligned), decimated from ~8k
        // triangles to a few hundred (there are tens of thousands of
        // instances) and given a plain instancing-enabled URP Lit material.
        // The seven prefabs become the variants of the "Small rock" entry.
        [MenuItem("BharatRTS/Build Moss Rock Clutter")]
        public static void BuildMossRocks()
        {
            Directory.CreateDirectory(MossFolder);

            Material mat = SaveMaterial(MossFolder + "/MossRockMat.mat", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MossFolder + "/Albedo.png"));
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MossFolder + "/Normal.png"));
                m.EnableKeyword("_NORMALMAP");
                m.SetColor("_BaseColor", new Color(1.25f, 1.25f, 1.2f)); // atlas is dark
                m.SetFloat("_Smoothness", 0.3f);
                m.SetFloat("_Metallic", 0f);
            });

            var prefabs = new System.Collections.Generic.List<GameObject>();
            var report = new System.Text.StringBuilder();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(MossFbx))
            {
                if (!(o is Mesh source) || !source.name.Contains("_rock"))
                {
                    continue;
                }

                Mesh mesh = ProcessMossRock(source);
                string shortName = "MossRock_" + source.name.Substring(source.name.LastIndexOf("_rock") + 5);
                Mesh saved = SaveMesh(mesh, MossFolder + "/" + shortName + ".asset");
                prefabs.Add(SavePrefabAt(MossFolder, shortName, saved, mat));
                report.Append(shortName + " tris=" + saved.triangles.Length / 3 + " size=" + saved.bounds.size.ToString("F2") + "; ");
            }

            var set = AssetDatabase.LoadAssetAtPath<TerrainClutterSet>("Assets/Resources/Terrain/ClutterSet.asset");
            foreach (var e in set.entries)
            {
                if (e != null && e.kind == TerrainClutterSet.ClutterKind.Rock)
                {
                    e.variants = prefabs.ToArray();
                    e.prefab = prefabs.Count > 0 ? prefabs[0] : e.prefab;
                    // Originals are 1.2-2.5 m wide; scatter them as small rocks.
                    e.minScale = 0.3f;
                    e.maxScale = 0.55f;
                    e.density = 1f;
                }
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("Moss rock clutter built: " + report);
        }

        private static Mesh ProcessMossRock(Mesh source)
        {
            Quaternion toYUp = Quaternion.Euler(90f, 0f, 0f);
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = toYUp * v[i];
            }

            for (int i = 0; i < n.Length; i++)
            {
                n[i] = toYUp * n[i];
            }

            // Re-base: bottom at y = 0, footprint centred on x/z.
            Vector3 min = v[0], max = v[0];
            foreach (Vector3 p in v)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Vector3 shift = new Vector3(-(min.x + max.x) * 0.5f, -min.y, -(min.z + max.z) * 0.5f);
            for (int i = 0; i < v.Length; i++)
            {
                v[i] += shift;
            }

            var rotated = new Mesh { name = source.name };
            rotated.indexFormat = source.indexFormat;
            rotated.vertices = v;
            rotated.normals = n;
            rotated.uv = source.uv;
            rotated.triangles = source.triangles;
            rotated.RecalculateTangents();
            rotated.RecalculateBounds();

            var simplifier = new MeshSimplifier(rotated);
            simplifier.SimplificationOptions = new SimplificationOptions
            {
                PreserveBorderEdges = true,
                PreserveUVSeamEdges = true,
                PreserveUVFoldoverEdges = true,
                PreserveSurfaceCurvature = false,
                EnableSmartLink = true,
                VertexLinkDistance = double.Epsilon,
                MaxIterationCount = 100,
                Agressiveness = 7.0,
                ManualUVComponentCount = false,
                UVComponentCount = 2,
            };
            simplifier.SimplifyMesh(0.06f);
            Mesh result = simplifier.ToMesh();
            result.name = source.name;
            result.RecalculateTangents();
            result.RecalculateBounds();
            return result;
        }

        private static GameObject SavePrefabAt(string folder, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}

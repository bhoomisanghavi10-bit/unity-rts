using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.EditorTools
{
    // Builds everything the terrain clutter scatter (Core/TerrainClutter.cs)
    // needs, reproducibly: a crossed-quad grass tuft mesh + alpha-clipped URP
    // Lit material + prefab (from Resources/Terrain/Detail/Tuft.png) and the
    // TerrainClutterSet asset tying everything together. Rocks and pebbles
    // have their own menus below (Build Moss Rock Clutter / Build Pebble
    // Clutter), which fill in those entries. Safe to re-run: it overwrites
    // its own outputs only.
    public static class TerrainClutterBuilder
    {
        private const string Folder = "Assets/Resources/Terrain/Detail";

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
                m.SetFloat("_Cutoff", 0.5f);
                m.SetFloat("_SwayStrength", 0.10f);
                m.SetFloat("_SwaySpeed", 1.6f);
                m.SetFloat("_BaseDarken", 0.4f);
                m.SetFloat("_FadeStart", 45f);
                m.SetFloat("_FadeEnd", 66f);
                m.renderQueue = 2450;
            }, "KingdomsOfBharat/Foliage");
            GameObject tuftPrefab = SavePrefab("Tuft", tuftMesh, tuftMat);

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
                    // Filled in by "Build Moss Rock Clutter" / "Build Pebble Clutter";
                    // entries without a usable prefab are simply skipped at runtime.
                    new TerrainClutterSet.Entry { name = "Small rock", kind = TerrainClutterSet.ClutterKind.Rock, minScale = 0.3f, maxScale = 0.55f, density = 1f },
                    new TerrainClutterSet.Entry { name = "Shore pebble", kind = TerrainClutterSet.ClutterKind.Pebble, minScale = 1.2f, maxScale = 2.6f, density = 2.4f },
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

        private static Material SaveMaterial(string path, System.Action<Material> configure, string shaderName = "Universal Render Pipeline/Lit")
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader.name != shaderName)
            {
                mat.shader = Shader.Find(shaderName);
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

        private const string PebbleFbx = "Assets/importedmodels/PebbleSet/pebble_set.fbx";
        private const string PebbleFolder = Folder + "/Pebbles";
        private const int PebbleVariants = 8;

        // User-supplied pebble bed (FBX exported from a 3ds Max scene): four
        // mirrored groups of ~39 stones, each a UV sphere (960 tris) squashed
        // into an oval by its node scale, 7-16 cm wide. This bakes each node's
        // transform into its mesh, keeps a spread of PebbleVariants distinct
        // shapes (by size and flatness), re-bases the pivot to bottom-centre,
        // decimates the spheres (they simplify well) and gives them plain
        // stone-colour materials (white / grey / dark, as in the source scene;
        // the sphere UVs would wrap a pebble photo badly). Assigned as the
        // variants of the Pebble entry in ClutterSet.asset.
        [MenuItem("BharatRTS/Build Pebble Clutter")]
        public static void BuildPebbles()
        {
            Directory.CreateDirectory(PebbleFolder);

            var importer = (ModelImporter)AssetImporter.GetAtPath(PebbleFbx);
            if (importer != null && (!importer.isReadable || importer.materialImportMode != ModelImporterMaterialImportMode.None))
            {
                importer.isReadable = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PebbleFbx);
            var candidates = new System.Collections.Generic.List<MeshFilter>();
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.StartsWith("Group_") || mf.sharedMesh == null)
                {
                    continue;
                }

                candidates.Add(mf);
            }

            // Spread by width (major footprint axis), then take evenly spaced ones.
            candidates.Sort((a, b) => WorldWidth(root, a).CompareTo(WorldWidth(root, b)));
            var chosen = new System.Collections.Generic.List<MeshFilter>();
            for (int i = 0; i < PebbleVariants && candidates.Count > 0; i++)
            {
                chosen.Add(candidates[Mathf.Min(candidates.Count - 1, (int)((i + 0.5f) / PebbleVariants * candidates.Count))]);
            }

            Color[] tints = { new Color(0.56f, 0.53f, 0.48f), new Color(0.48f, 0.45f, 0.41f), new Color(0.42f, 0.38f, 0.34f) };
            var mats = new Material[tints.Length];
            string[] matNames = { "PebbleWhite", "PebbleGrey", "PebbleDark" };
            for (int i = 0; i < tints.Length; i++)
            {
                Color tint = tints[i];
                mats[i] = SaveMaterial(PebbleFolder + "/" + matNames[i] + ".mat", m =>
                {
                    m.SetColor("_BaseColor", tint);
                    m.SetFloat("_Smoothness", 0.55f); // wet
                    m.SetFloat("_Metallic", 0f);
                });
            }

            var prefabs = new System.Collections.Generic.List<GameObject>();
            var report = new System.Text.StringBuilder();
            for (int i = 0; i < chosen.Count; i++)
            {
                Mesh mesh = ProcessPebble(root, chosen[i]);
                string name = "Pebble_" + (i + 1).ToString("00");
                Mesh saved = SaveMesh(mesh, PebbleFolder + "/" + name + ".asset");
                prefabs.Add(SavePrefabAt(PebbleFolder, name, saved, mats[i % mats.Length]));
                report.Append(name + " tris=" + saved.triangles.Length / 3 + " size=" + saved.bounds.size.ToString("F3") + "; ");
            }

            var set = AssetDatabase.LoadAssetAtPath<TerrainClutterSet>("Assets/Resources/Terrain/ClutterSet.asset");
            foreach (var e in set.entries)
            {
                if (e != null && e.kind == TerrainClutterSet.ClutterKind.Pebble)
                {
                    e.variants = prefabs.ToArray();
                    e.prefab = prefabs.Count > 0 ? prefabs[0] : e.prefab;
                    // Source stones are 7-16 cm; scaled up so they read from the RTS camera.
                    e.minScale = 1.2f;
                    e.maxScale = 2.6f;
                    e.density = 2.4f;
                }
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("Pebble clutter built: " + report);
        }

        private static float WorldWidth(GameObject root, MeshFilter mf)
        {
            Vector3 size = mf.GetComponent<Renderer>().bounds.size;
            return Mathf.Max(size.x, size.z);
        }

        private static Mesh ProcessPebble(GameObject root, MeshFilter mf)
        {
            // Node -> root space, baking the oval-making scale and rotation.
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Mesh source = mf.sharedMesh;
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            Matrix4x4 normalMatrix = toRoot.inverse.transpose;
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = toRoot.MultiplyPoint3x4(v[i]);
                n[i] = normalMatrix.MultiplyVector(n[i]).normalized;
            }

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

            var baked = new Mesh { name = source.name, indexFormat = source.indexFormat };
            baked.vertices = v;
            baked.normals = n;
            baked.uv = source.uv;
            baked.triangles = source.triangles;
            // A non-uniform scale can flip winding; fix if the transform is mirrored.
            if (toRoot.determinant < 0f)
            {
                int[] t = baked.triangles;
                for (int i = 0; i < t.Length; i += 3)
                {
                    (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
                }

                baked.triangles = t;
            }

            baked.RecalculateBounds();

            var simplifier = new MeshSimplifier(baked);
            simplifier.SimplifyMesh(0.16f);
            Mesh result = simplifier.ToMesh();
            result.name = source.name;
            result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }

        private const string WetFolder = Folder + "/Shore";

        // Procedural shoreline props (no sourced assets exist yet, so these
        // are generated placeholders that any real reed / log prefab can
        // replace via the Inspector): a reed card (Reed.png on the foliage
        // shader, sways more than grass) and three driftwood logs (a tapered,
        // bent, noisy cylinder with a couple of branch stubs, Driftwood.png
        // on plain URP Lit). Adds "Reed clump" and "Driftwood" entries to
        // the clutter set if they aren't there yet.
        [MenuItem("BharatRTS/Build Reed and Driftwood Clutter")]
        public static void BuildShoreProps()
        {
            Directory.CreateDirectory(WetFolder);

            string reedTex = Folder + "/Reed.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(reedTex);
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.mipMapsPreserveCoverage = true;
            ti.alphaTestReferenceValue = 0.5f;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();

            Mesh reedMesh = SaveMesh(BuildCardMesh("ReedMesh", 0.8f, 1.6f), WetFolder + "/ReedMesh.asset");
            Material reedMat = SaveMaterial(WetFolder + "/ReedMat.mat", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(reedTex));
                m.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.85f));
                m.SetFloat("_Cutoff", 0.5f);
                m.SetFloat("_SwayStrength", 0.22f);
                m.SetFloat("_SwaySpeed", 1.2f);
                m.SetFloat("_BaseDarken", 0.3f);
                m.SetFloat("_FadeStart", 45f);
                m.SetFloat("_FadeEnd", 66f);
                m.renderQueue = 2450;
            }, "KingdomsOfBharat/Foliage");
            GameObject reedPrefab = SavePrefabAt(WetFolder, "Reed", reedMesh, reedMat);

            Material woodMat = SaveMaterial(WetFolder + "/DriftwoodMat.mat", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Driftwood.png"));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.2f);
                m.SetFloat("_Metallic", 0f);
            });
            var logs = new System.Collections.Generic.List<GameObject>();
            float[] lengths = { 1.9f, 1.4f, 2.2f };
            float[] radii = { 0.10f, 0.08f, 0.12f };
            for (int i = 0; i < 3; i++)
            {
                Mesh log = SaveMesh(BuildLogMesh("DriftwoodMesh_" + (i + 1), 100 + i * 17, lengths[i], radii[i]), WetFolder + "/DriftwoodMesh_" + (i + 1) + ".asset");
                logs.Add(SavePrefabAt(WetFolder, "Driftwood_" + (i + 1), log, woodMat));
            }

            var set = AssetDatabase.LoadAssetAtPath<TerrainClutterSet>("Assets/Resources/Terrain/ClutterSet.asset");
            var entries = new System.Collections.Generic.List<TerrainClutterSet.Entry>(set.entries);
            UpsertEntry(entries, TerrainClutterSet.ClutterKind.Reed, "Reed clump", new[] { reedPrefab }, 0.5f, 0.95f, 1.3f);
            UpsertEntry(entries, TerrainClutterSet.ClutterKind.Driftwood, "Driftwood", logs.ToArray(), 0.8f, 1.4f, 1f);
            set.entries = entries.ToArray();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("Shore props built: 1 reed, 3 driftwood logs.");
        }

        private static void UpsertEntry(System.Collections.Generic.List<TerrainClutterSet.Entry> entries, TerrainClutterSet.ClutterKind kind, string name, GameObject[] prefabs, float minScale, float maxScale, float density)
        {
            var entry = entries.Find(e => e != null && e.kind == kind);
            if (entry == null)
            {
                entry = new TerrainClutterSet.Entry { name = name, kind = kind, minScale = minScale, maxScale = maxScale, density = density };
                entries.Add(entry);
            }

            entry.prefab = prefabs[0];
            entry.variants = prefabs;
        }

        // Three vertical cards crossed at 60 degrees, up-facing normals.
        private static Mesh BuildCardMesh(string name, float width, float height)
        {
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

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        // A bent, tapered, lumpy log along X with capped ends and a couple
        // of branch stubs; pivot bottom-centre, sunk slightly so it beds in.
        private static Mesh BuildLogMesh(string name, int seed, float length, float baseRadius)
        {
            var rng = new System.Random(seed);
            const int rings = 14, sides = 10;
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            float bendY = 0.04f + (float)rng.NextDouble() * 0.05f;
            float bendZ = 0.05f + (float)rng.NextDouble() * 0.08f;
            float phase = (float)rng.NextDouble() * 6f;

            Vector3 Center(float t) => new Vector3((t - 0.5f) * length, Mathf.Sin(t * Mathf.PI) * bendY, Mathf.Sin(t * 2.4f + phase) * bendZ);
            float Radius(float t) => baseRadius * Mathf.Lerp(1f, 0.55f, t) * (1f + 0.14f * Mathf.Sin(t * 9f + phase));

            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                Vector3 c = Center(t);
                float rad = Radius(t);
                for (int sIdx = 0; sIdx <= sides; sIdx++)
                {
                    float a = sIdx / (float)sides * Mathf.PI * 2f;
                    float lump = 1f + 0.10f * ((float)rng.NextDouble() - 0.5f) * 2f;
                    verts.Add(c + new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a)) * rad * lump);
                    uvs.Add(new Vector2(t * length / 0.7f, sIdx / (float)sides));
                }
            }

            int stride = sides + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int sIdx = 0; sIdx < sides; sIdx++)
                {
                    int i0 = r * stride + sIdx, i1 = i0 + 1, i2 = i0 + stride, i3 = i2 + 1;
                    tris.AddRange(new[] { i0, i1, i2, i1, i3, i2 });
                }
            }

            // End caps (flat fans).
            for (int end = 0; end < 2; end++)
            {
                int ringStart = end == 0 ? 0 : rings * stride;
                Vector3 c = Center(end == 0 ? 0f : 1f);
                int centre = verts.Count;
                verts.Add(c);
                uvs.Add(new Vector2(end == 0 ? 0f : length / 0.7f, 0.5f));
                for (int sIdx = 0; sIdx < sides; sIdx++)
                {
                    int a = ringStart + sIdx, b = ringStart + sIdx + 1;
                    if (end == 0) tris.AddRange(new[] { centre, b, a }); else tris.AddRange(new[] { centre, a, b });
                }
            }

            // Branch stubs.
            int stubs = 1 + rng.Next(2);
            for (int i = 0; i < stubs; i++)
            {
                float t = 0.25f + (float)rng.NextDouble() * 0.5f;
                Vector3 origin = Center(t);
                float angle = (float)rng.NextDouble() * Mathf.PI;
                Vector3 dir = new Vector3(0.3f, Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
                float len = 0.22f + (float)rng.NextDouble() * 0.2f;
                float r0 = Radius(t) * 0.45f;
                Vector3 side = Vector3.Cross(dir, Vector3.right).normalized;
                Vector3 up = Vector3.Cross(dir, side).normalized;
                int baseIndex = verts.Count;
                const int stubSides = 6;
                for (int k = 0; k <= stubSides; k++)
                {
                    float a = k / (float)stubSides * Mathf.PI * 2f;
                    Vector3 ring = (side * Mathf.Cos(a) + up * Mathf.Sin(a));
                    verts.Add(origin + ring * r0);
                    uvs.Add(new Vector2(0f, k / (float)stubSides));
                    verts.Add(origin + dir * len + ring * r0 * 0.4f);
                    uvs.Add(new Vector2(0.3f, k / (float)stubSides));
                }

                for (int k = 0; k < stubSides; k++)
                {
                    int a0 = baseIndex + k * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                    tris.AddRange(new[] { a0, b0, a1, b0, b1, a1 });
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Re-base: footprint centred, bottom slightly below the ground.
            Bounds b2 = mesh.bounds;
            Vector3[] v = mesh.vertices;
            Vector3 shift = new Vector3(-b2.center.x, -b2.min.y - 0.02f, -b2.center.z);
            for (int i = 0; i < v.Length; i++) v[i] += shift;
            mesh.vertices = v;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

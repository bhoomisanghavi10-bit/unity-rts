using System.IO;
using UnityEditor;
using UnityEngine;

namespace KingdomsOfBharat.EditorTools
{
    /// <summary>
    /// Builds a standalone (non-variant) building prefab from an already
    /// low-poly glb plus its three extracted PBR PNGs. The mesh is copied
    /// into its own .asset so the glb (which embeds 17MB of textures) can
    /// be deleted afterwards and the prefab has no dependency on a raw FBX.
    /// The source folder is a transient staging area: copy the glb (plus its
    /// extracted albedo/normal/metallicSmoothness PNGs) in, run the menu item,
    /// then delete the folder. The Maratha House original is ~/Downloads/maratha house.glb.
    /// </summary>
    public static class LowpolyBuildingImporter
    {
        [MenuItem("BharatRTS/Import Lowpoly/Maratha House")]
        public static void ImportMarathaHouse()
        {
            Import("Assets/importedmodels/MarathaHouseLowpoly", "MarathaHouse",
                   "Assets/Resources/buildings/Maratha/_Lowpoly/House", "House",
                   "Assets/Resources/buildings/Maratha/House_Lowpoly.prefab");
        }

        [MenuItem("BharatRTS/Import Lowpoly/Chola TownCenter")]
        public static void ImportCholaTownCenter() => ImportTownCenter("Chola");

        // Staging folder Assets/importedmodels/<Civ>TownCenterLowpoly holds <Civ>TownCenter.glb + PNGs
        // (made by Tools/glb_extract_pbr.py); the result overwrites <Civ>/TownCenter.prefab in place.
        public static void ImportTownCenter(string civ)
        {
            Import($"Assets/importedmodels/{civ}TownCenterLowpoly", $"{civ}TownCenter",
                   $"Assets/Resources/buildings/{civ}/_Lowpoly/TownCenter", "TownCenter",
                   $"Assets/Resources/buildings/{civ}/TownCenter.prefab");
        }

        static void Import(string srcFolder, string srcName, string destFolder, string buildingName, string prefabPath)
        {
            Directory.CreateDirectory(Path.GetFullPath(destFolder));
            string albedo = Move(srcFolder, srcName + "_albedo.png", destFolder, buildingName + "_albedo.png");
            string normal = Move(srcFolder, srcName + "_normal.png", destFolder, buildingName + "_normal.png");
            string packed = Move(srcFolder, srcName + "_metallicSmoothness.png", destFolder, buildingName + "_metallicSmoothness.png");
            AssetDatabase.Refresh();
            SetTex(albedo, false, true);
            SetTex(normal, true, false);
            SetTex(packed, false, false);

            Mesh src = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath($"{srcFolder}/{srcName}.glb"))
                if (o is Mesh m) { src = m; break; }
            if (src == null) { Debug.LogError("LowpolyBuildingImporter: no mesh in glb"); return; }
            Mesh mesh = Object.Instantiate(src);
            mesh.name = buildingName + "_lowpoly";
            string meshPath = $"{destFolder}/{buildingName}_mesh.asset";
            AssetDatabase.CreateAsset(mesh, meshPath);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.color = Color.white;
            string matPath = $"{destFolder}/{buildingName}.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();

            GameObject root = new GameObject(buildingName);
            GameObject model = new GameObject(buildingName + "_model");
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = mat;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"LowpolyBuildingImporter: {prefabPath} tris={mesh.triangles.Length / 3} bounds={mesh.bounds}");
        }

        static string Move(string sf, string sn, string df, string dn)
        {
            string dest = $"{df}/{dn}";
            File.Copy(Path.GetFullPath($"{sf}/{sn}"), Path.GetFullPath(dest), true);
            return dest;
        }

        static void SetTex(string path, bool normalMap, bool srgb)
        {
            AssetDatabase.ImportAsset(path);
            var t = (TextureImporter)AssetImporter.GetAtPath(path);
            if (t == null) return;
            t.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (!normalMap) t.sRGBTexture = srgb;
            t.SaveAndReimport();
        }
    }
}

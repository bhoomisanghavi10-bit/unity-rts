using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KingdomsOfBharat.Editor
{
    /// <summary>
    /// One-shot pipeline for wiring a raw Meshy AI building export (FBX + separate
    /// albedo/metallic/normal/roughness PNGs, plus a pre-packed metallic+smoothness PNG)
    /// into a building prefab at either
    /// Assets/Resources/Buildings/&lt;civId&gt;/&lt;buildingName&gt;.prefab (civ-specific,
    /// via ImportBuilding) or Assets/Resources/Buildings/&lt;buildingName&gt;.prefab
    /// (shared/non-civ, via ImportSharedBuilding) - both paths BuildingModelFactory.Spawn
    /// probes. Age-aware building visuals: pass buildingName with an "_{AgeId}" suffix
    /// (e.g. "TownCenter_Durg", "Tower_Ancient") for an age-tiered import - see
    /// BuildingModelFactory's own resource-path-convention comment.
    ///
    /// The metallic(R)+smoothness(A) packing itself is done outside Unity (see
    /// scratchpad/pack_metallic_smoothness.py) — an in-Editor Texture2D.GetPixels/
    /// SetPixels pass on 2048x2048 maps crashed the Unity Editor process outright, so
    /// this class only ever consumes an already-packed "*_metallicSmoothness.png".
    /// </summary>
    public static class MeshyBuildingImporter
    {
        public static void ImportBuilding(string civId, string buildingName, string sourceFolder, float extraScale)
        {
            ImportBuilding(civId, buildingName, sourceFolder, extraScale, Quaternion.identity);
        }

        /// <summary>
        /// modelRotationCorrection is applied to the instantiated model *inside* the
        /// wrapper prefab, one level below where BuildingModelFactory.Spawn overwrites
        /// the wrapper root's localRotation for resourceName=="Tower" (a correction
        /// authored for a different shared Tower asset, keyed by name only, not civ).
        /// Use this to counter-rotate a civ-specific Tower that's already upright.
        /// </summary>
        public static void ImportBuilding(string civId, string buildingName, string sourceFolder, float extraScale, Quaternion modelRotationCorrection)
        {
            Import($"Assets/Resources/Buildings/{civId}/_Source/{buildingName}",
                $"Assets/Resources/Buildings/{civId}/{buildingName}.prefab",
                $"{civId}/{buildingName}", buildingName, sourceFolder, extraScale, modelRotationCorrection);
        }

        public static void ImportSharedBuilding(string buildingName, string sourceFolder, float extraScale)
        {
            ImportSharedBuilding(buildingName, sourceFolder, extraScale, Quaternion.identity);
        }

        /// <summary>
        /// Same pipeline as ImportBuilding, minus the civ folder segment - for a
        /// shared/non-civ age tier (TownCenter Ancient/Classical, Tower/Wall
        /// Ancient/Classical/Durg) or a non-age-tiered shared building (Lumber
        /// Camp/Mining Camp/Mill).
        /// </summary>
        public static void ImportSharedBuilding(string buildingName, string sourceFolder, float extraScale, Quaternion modelRotationCorrection)
        {
            Import($"Assets/Resources/Buildings/_Source/{buildingName}",
                $"Assets/Resources/Buildings/{buildingName}.prefab",
                buildingName, buildingName, sourceFolder, extraScale, modelRotationCorrection);
        }

        private static void Import(string destFolder, string prefabPath, string logLabel, string buildingName, string sourceFolder, float extraScale, Quaternion modelRotationCorrection)
        {
            // A source folder on an external volume ships a macOS
            // AppleDouble shadow file next to every real file
            // ("._<name>", same extension, ~4KB resource-fork stub) -
            // Directory.GetFiles' unspecified ordering let FirstOrDefault
            // silently pick the shadow instead of the real asset once
            // (a 76MB FBX import silently produced a 4096-byte, 0-mesh
            // prefab with zero compile/console errors), so every glob
            // below explicitly excludes filenames starting with "._".
            string absSourceFolder = Path.GetFullPath(sourceFolder);
            string fbxSrc = GetRealFiles(absSourceFolder, "*.fbx").FirstOrDefault();
            string normalSrc = GetRealFiles(absSourceFolder, "*_normal.png").FirstOrDefault();
            string packedSrc = GetRealFiles(absSourceFolder, "*_metallicSmoothness.png").FirstOrDefault();
            string metallicSrc = GetRealFiles(absSourceFolder, "*_metallic.png").FirstOrDefault();
            string roughnessSrc = GetRealFiles(absSourceFolder, "*_roughness.png").FirstOrDefault();
            string albedoSrc = GetRealFiles(absSourceFolder, "*.png")
                .FirstOrDefault(p => p != metallicSrc && p != normalSrc && p != roughnessSrc && p != packedSrc);

            if (fbxSrc == null || normalSrc == null || packedSrc == null || albedoSrc == null)
            {
                Debug.LogError($"MeshyBuildingImporter: missing source file(s) in {sourceFolder}");
                return;
            }

            Directory.CreateDirectory(Path.GetFullPath(destFolder));

            string fbxDest = CopyInto(fbxSrc, destFolder, $"{buildingName}_model.fbx");
            string albedoDest = CopyInto(albedoSrc, destFolder, $"{buildingName}_albedo.png");
            string normalDest = CopyInto(normalSrc, destFolder, $"{buildingName}_normal.png");
            string packedDest = CopyInto(packedSrc, destFolder, $"{buildingName}_metallicSmoothness.png");
            AssetDatabase.Refresh();

            SetTextureSettings(albedoDest, isNormalMap: false, sRGB: true);
            SetTextureSettings(normalDest, isNormalMap: true, sRGB: false);
            SetTextureSettings(packedDest, isNormalMap: false, sRGB: false);
            AssetDatabase.Refresh();

            Material mat = BuildMaterial(albedoDest, normalDest, packedDest);
            string matPath = $"{destFolder}/{buildingName}.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();

            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxDest);
            GameObject root = new GameObject(buildingName);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = modelRotationCorrection;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            root.transform.localScale = Vector3.one * extraScale;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"MeshyBuildingImporter: wired {logLabel} -> {prefabPath} (extraScale={extraScale})");
        }

        private static System.Collections.Generic.IEnumerable<string> GetRealFiles(string folder, string pattern)
        {
            return Directory.GetFiles(folder, pattern)
                .Where(p => !Path.GetFileName(p).StartsWith("._"));
        }

        private static string CopyInto(string absSrc, string destFolder, string destFileName)
        {
            string destPath = $"{destFolder}/{destFileName}";
            string absDest = Path.GetFullPath(destPath);
            File.Copy(absSrc, absDest, overwrite: true);
            return destPath;
        }

        private static void SetTextureSettings(string path, bool isNormalMap, bool sRGB)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (!isNormalMap) importer.sRGBTexture = sRGB;
            importer.SaveAndReimport();
        }

        private static Material BuildMaterial(string albedoPath, string normalPath, string packedPath)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(shader);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.color = Color.white;
            return mat;
        }
    }
}

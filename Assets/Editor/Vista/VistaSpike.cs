using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pinwheel.Vista;
using Pinwheel.Vista.UnityTerrain;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.EditorTools
{
    // Drives Vista from code to generate each of the 5 named skirmish
    // layout styles (docs/SKIRMISH_MAP_SPEC.md) in a throwaway additive
    // scene, then bakes the result into the Resources map assets
    // ProceduralTerrain loads. Generation is async (GPU compute); call
    // Create(recipe), then poll Task.
    //
    // Every style's signature terrain feature (Mountain Pass's ridge and
    // corridor, Highland Foothills' terraces) is a deterministic C#
    // post-process on the sampled heightmap (LayoutRecipe.HeightPostProcess),
    // not a hand-authored Vista graph node - see SkirmishTerrainCarving.
    // Vista only ever supplies the base noise.
    public static class VistaSpike
    {
        public const float MapSize = 158f;
        public const float HeightScale = 20f;
        private const string TemplateRoot = "Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/";
        private const string ResourceRoot = "Assets/Resources/Maps/";

        public static VistaManager Manager { get; private set; }
        public static GenerationTask Task { get; private set; }
        public static Scene SpikeScene { get; private set; }
        public static GameObject TerrainObject { get; private set; }
        public static string Log { get; private set; } = "";

        public struct LayoutRecipe
        {
            public string DisplayName;
            // Relative to TemplateRoot, e.g. "Mountain/Mountain_BiomeTemplate.asset".
            public string TemplatePath;
            // Resources/Maps/<ResourceFolder>/height.bytes.
            public string ResourceFolder;
            // Optional: (heights01, mapSize, heightScaleWorld) -> heights01.
            public System.Func<float[,], float, float, float[,]> HeightPostProcess;
        }

        // Crossroad Valleys: open/balanced ground (Dunes) with 4 flat-topped
        // mesa plateaus at the contested zone's diagonal corners. The
        // template's own paired "Mesa" biome (Mesa/DunesAndMesa.asset) was
        // tried first but its two sibling LocalProceduralBiomes cancelled
        // each other out to a completely flat bake (a real Vista paired-
        // biome quirk, not chased further) - this reuses the same reliable
        // single-biome-plus-code-carve approach as every other style
        // instead. Kept on the existing SkirmishMedium resource path/enum
        // member.
        public static LayoutRecipe CrossroadValleys => new LayoutRecipe
        {
            DisplayName = "Crossroad Valleys",
            TemplatePath = "Dunes/Dunes_BiomeTemplate.asset",
            ResourceFolder = "SkirmishMedium",
            HeightPostProcess = (heights, mapSize, heightScale) => SkirmishTerrainCarving.LimitSlope(
                SkirmishTerrainCarving.ApplyCornerMesas(
                    heights, mapSize, heightScale,
                    mesaCentersXZ: new[] { new Vector2(25f, 25f), new Vector2(-25f, 25f), new Vector2(25f, -25f), new Vector2(-25f, -25f) },
                    mesaRadius: 10f, mesaFalloff: 6f, mesaRaiseWorld: 6f),
                mapSize, heightScale, MaxWalkableSlopeDegrees, SlopeLimitIterations),
        };

        // Divided Riverbed: the river itself is carved entirely at runtime
        // by ProceduralTerrain from MapDefinitionData.WaterCenter/
        // WaterHalfExtents/FordCentersX - Vista only supplies gentle base
        // relief for the banks.
        public static LayoutRecipe DividedRiverbed => new LayoutRecipe
        {
            DisplayName = "Divided Riverbed",
            TemplatePath = "Dunes/Dunes_BiomeTemplate.asset",
            ResourceFolder = "SkirmishDividedRiverbed",
            HeightPostProcess = (heights, mapSize, heightScale) => SkirmishTerrainCarving.LimitSlope(
                heights, mapSize, heightScale, MaxWalkableSlopeDegrees, SlopeLimitIterations),
        };

        // The raw Mountain template's own noise can exceed the NavMesh's
        // walkable slope limit in a random patch anywhere on the map, not
        // just wherever a feature-specific carve looks - always finish
        // with a general slope-safety pass (docs/SKIRMISH_MAP_SPEC.md rule
        // 5). A no-op on terrain that's already gentle (Crossroad Valleys/
        // Divided Riverbed/Clearing's Dunes base), so it's applied to every
        // recipe uniformly rather than only the two Mountain-based ones.
        private const float MaxWalkableSlopeDegrees = 35f;
        private const int SlopeLimitIterations = 30;

        // Mountain Pass: a mountain chain across Z, with one corridor at
        // X=0 (directly between Player and Enemy, who both sit at x=0).
        public static LayoutRecipe MountainPass => new LayoutRecipe
        {
            DisplayName = "Mountain Pass",
            TemplatePath = "Mountain/Mountain_BiomeTemplate.asset",
            ResourceFolder = "SkirmishMountainPass",
            HeightPostProcess = (heights, mapSize, heightScale) => SkirmishTerrainCarving.LimitSlope(
                SkirmishTerrainCarving.ApplyRidgeWithPass(
                    heights, mapSize, heightScale,
                    ridgeCenterZ: 0f, ridgeHalfWidth: 12f, ridgeFalloff: 10f, ridgeRaiseWorld: 6f,
                    passCenterX: 0f, passHalfWidth: 6f, passFalloff: 10f, passFloorWorld: 2f),
                mapSize, heightScale, MaxWalkableSlopeDegrees, SlopeLimitIterations),
        };

        // Highland Foothills: step-quantize the Mountain template's relief
        // into terraces (partial strength keeps some natural undulation).
        public static LayoutRecipe HighlandFoothills => new LayoutRecipe
        {
            DisplayName = "Highland Foothills",
            TemplatePath = "Mountain/Mountain_BiomeTemplate.asset",
            ResourceFolder = "SkirmishHighlandFoothills",
            HeightPostProcess = (heights, mapSize, heightScale) => SkirmishTerrainCarving.LimitSlope(
                SkirmishTerrainCarving.ApplyTerracing(heights, stepSizeNormalized: 0.05f, strength: 0.7f),
                mapSize, heightScale, MaxWalkableSlopeDegrees, SlopeLimitIterations),
        };

        // Clearing: mostly flat/gentle - the dense-forest-with-lanes
        // identity comes entirely from ResourceNodeSpawner's tile
        // classification (MapDefinitionData.ForestThreshold/
        // ForestLaneWidth), not the heightmap.
        public static LayoutRecipe Clearing => new LayoutRecipe
        {
            DisplayName = "Clearing",
            TemplatePath = "Dunes/Dunes_BiomeTemplate.asset",
            ResourceFolder = "SkirmishClearing",
            HeightPostProcess = (heights, mapSize, heightScale) => SkirmishTerrainCarving.LimitSlope(
                heights, mapSize, heightScale, MaxWalkableSlopeDegrees, SlopeLimitIterations),
        };

        private static LayoutRecipe _activeRecipe;

        public static string Create(LayoutRecipe recipe)
        {
            _activeRecipe = recipe;
            Log = "";
            if (SpikeScene.IsValid() && SpikeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(SpikeScene, true);
            }

            SpikeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(SpikeScene);

            Manager = VistaManager.CreateInstanceInScene();
            InitManager(Manager);
            var root = new GameObject("TerrainRoot");
            root.transform.position = new Vector3(-MapSize * 0.5f, 0f, -MapSize * 0.5f);

            var system = VistaManager.GetTerrainSystem<UnityTerrainSystem>();
            GameObject[,] grid = system.CreateTerrainGrid(new TerrainGridCreationContext(new Vector2Int(1, 1), new Vector3(MapSize, HeightScale, MapSize), root.transform));
            TerrainObject = grid[0, 0];
            var tile = system.SetupTile(Manager, TerrainObject) as TerrainTile;
            // CreateTerrainGrid's fresh TerrainData keeps Unity's factory
            // default heightmap resolution (33) - neither it nor SetupTile
            // ever raises it, so without this every bake is far too coarse
            // (a sample every ~5 world units on a 158 m map) regardless of
            // recipe. 513 matches the granularity the original spike used.
            if (tile != null)
            {
                tile.heightMapResolution = 513;
            }

            var template = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(TemplateRoot + recipe.TemplatePath);
            if (template == null)
            {
                return "template not found: " + recipe.TemplatePath;
            }

            var context = new BiomeTemplateSpawnContext(Manager);
            GameObject biome = BiomeTemplateSpawner.Spawn(template, context);
            // Templates are authored for a 1000 m terrain; fit the biome to our tile.
            // Some templates (e.g. Mesa's "DunesAndMesa") spawn a root with no
            // LocalProceduralBiome of its own and two paired biome children
            // instead - fit every biome found anywhere under the root, not
            // just a root-level one.
            biome.transform.position = Vector3.zero;
            biome.transform.localScale = Vector3.one;
            float h = MapSize * 0.5f;
            var anchors = new[] { new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h) };
            LocalProceduralBiome[] localBiomes = biome.GetComponentsInChildren<LocalProceduralBiome>(true);
            foreach (var localBiome in localBiomes)
            {
                localBiome.anchors = anchors;
                ScaleNoiseToMap(localBiome);
            }
            Log += "recipe=" + recipe.DisplayName + " biome=" + biome.name + " biomeCount=" + localBiomes.Length + " tiles=" + Manager.GetTiles().Count + "; ";

            Task = Manager.Generate(Manager.GetTiles());
            Log += "generation started";
            return Log;
        }

        // A manager created in code (not through the Inspector) has null
        // UnityEvent fields, which Generate() invokes; give it fresh ones and
        // our terrain height scale (Vista's default max height is 500 m).
        private static void InitManager(VistaManager manager)
        {
            foreach (var field in typeof(VistaManager).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                if (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(field.FieldType) && field.GetValue(manager) == null)
                {
                    field.SetValue(manager, System.Activator.CreateInstance(field.FieldType));
                }
            }

            manager.terrainMaxHeight = HeightScale;
        }

        // Vista templates are authored for a 1000 m terrain: every Noise node's
        // scale is a world-space wavelength, so shrink them in proportion or the
        // 158 m map comes out nearly flat. A template with an empty graph (e.g.
        // Blank) has zero NoiseNodes and is simply left alone.
        private static void ScaleNoiseToMap(LocalProceduralBiome biome)
        {
            float factor = MapSize / 1000f;
            int scaled = 0;
            foreach (var node in biome.terrainGraph.GetNodes())
            {
                if (node.GetType().Name != "NoiseNode")
                {
                    continue;
                }

                var field = node.GetType().GetField("m_scale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field.SetValue(node, System.Convert.ToSingle(field.GetValue(node)) * factor);
                scaled++;
            }

            Log += "noise nodes scaled=" + scaled + "; ";
        }

        // Reads the generated heights, applies the active recipe's
        // post-process (if any), and writes the result to Resources as a
        // BakedHeightmap.
        public static string BakeHeightmap()
        {
            var data = TerrainObject.GetComponent<Terrain>().terrainData;
            int n = data.heightmapResolution;
            float[,] heights01 = data.GetHeights(0, 0, n, n);
            if (_activeRecipe.HeightPostProcess != null)
            {
                heights01 = _activeRecipe.HeightPostProcess(heights01, MapSize, data.size.y);
            }

            var samples = new ushort[n * n];
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    samples[z * n + x] = (ushort)Mathf.RoundToInt(Mathf.Clamp01(heights01[z, x]) * 65535f);
                }
            }

            string bakedPath = ResourceRoot + _activeRecipe.ResourceFolder + "/height.bytes";
            Directory.CreateDirectory(Path.GetDirectoryName(bakedPath));
            File.WriteAllBytes(bakedPath, BakedHeightmap.ToBytes(n, data.size.y, samples));
            AssetDatabase.ImportAsset(bakedPath);
            return "baked " + n + "x" + n + " heightScale=" + data.size.y + " -> " + bakedPath;
        }

        [MenuItem("BharatRTS/Vista Skirmish Layouts/Generate And Bake Crossroad Valleys")]
        public static void GenerateAndBakeCrossroadValleys() => GenerateAndBake(CrossroadValleys);

        [MenuItem("BharatRTS/Vista Skirmish Layouts/Generate And Bake Divided Riverbed")]
        public static void GenerateAndBakeDividedRiverbed() => GenerateAndBake(DividedRiverbed);

        [MenuItem("BharatRTS/Vista Skirmish Layouts/Generate And Bake Mountain Pass")]
        public static void GenerateAndBakeMountainPass() => GenerateAndBake(MountainPass);

        [MenuItem("BharatRTS/Vista Skirmish Layouts/Generate And Bake Highland Foothills")]
        public static void GenerateAndBakeHighlandFoothills() => GenerateAndBake(HighlandFoothills);

        [MenuItem("BharatRTS/Vista Skirmish Layouts/Generate And Bake Clearing")]
        public static void GenerateAndBakeClearing() => GenerateAndBake(Clearing);

        public static void GenerateAndBake(LayoutRecipe recipe)
        {
            Debug.Log("Vista spike: " + Create(recipe));
            EditorApplication.update += PollBake;
        }

        private static void PollBake()
        {
            if (Task == null || !Task.isCompleted)
            {
                return;
            }

            EditorApplication.update -= PollBake;
            Debug.Log("Vista spike: " + BakeHeightmap());
            if (SpikeScene.IsValid() && SpikeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(SpikeScene, true);
            }
        }

        public static string Status()
        {
            if (Task == null)
            {
                return "no task";
            }

            return "status=" + Task.status + " completed=" + Task.isCompleted;
        }
    }
}

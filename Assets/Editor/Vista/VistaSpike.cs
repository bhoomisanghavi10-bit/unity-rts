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
    // Spike: drive Vista from code to generate one medium (158 x 158) skirmish
    // terrain in a throwaway additive scene, so the result can be baked into
    // map assets that ProceduralTerrain loads (see docs/SKIRMISH_MAP_SPEC.md).
    // Generation is async (GPU compute); call Create(), then poll Task.
    public static class VistaSpike
    {
        public const float MapSize = 158f;
        public const float HeightScale = 20f;
        private const string BakedHeightPath = "Assets/Resources/Maps/SkirmishMedium/height.bytes";
        private const string DefaultTemplate = "Mountain/Mountain_BiomeTemplate.asset";
        private const string TemplateRoot = "Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/";

        public static VistaManager Manager { get; private set; }
        public static GenerationTask Task { get; private set; }
        public static Scene SpikeScene { get; private set; }
        public static GameObject TerrainObject { get; private set; }
        public static string Log { get; private set; } = "";

        public static string Create(string templatePath)
        {
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
            system.SetupTile(Manager, TerrainObject);

            var template = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(TemplateRoot + templatePath);
            if (template == null)
            {
                return "template not found: " + templatePath;
            }

            var context = new BiomeTemplateSpawnContext(Manager);
            GameObject biome = BiomeTemplateSpawner.Spawn(template, context);
            // Templates are authored for a 1000 m terrain; fit the biome to our tile.
            var localBiome = biome.GetComponent<LocalProceduralBiome>();
            biome.transform.position = Vector3.zero;
            biome.transform.localScale = Vector3.one;
            float h = MapSize * 0.5f;
            localBiome.anchors = new[] { new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h) };
            Log += "biome=" + biome.name + " tiles=" + Manager.GetTiles().Count + "; ";

            ScaleNoiseToMap(localBiome);
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
        // 158 m map comes out nearly flat.
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

        // Writes the generated heightmap to Resources as a BakedHeightmap.
        public static string BakeHeightmap()
        {
            var data = TerrainObject.GetComponent<Terrain>().terrainData;
            int n = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, n, n);
            var samples = new ushort[n * n];
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    samples[z * n + x] = (ushort)Mathf.RoundToInt(Mathf.Clamp01(heights[z, x]) * 65535f);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BakedHeightPath));
            File.WriteAllBytes(BakedHeightPath, BakedHeightmap.ToBytes(n, data.size.y, samples));
            AssetDatabase.ImportAsset(BakedHeightPath);
            return "baked " + n + "x" + n + " heightScale=" + data.size.y + " -> " + BakedHeightPath;
        }

        [MenuItem("BharatRTS/Vista Spike/Generate And Bake Medium Map")]
        public static void GenerateAndBake()
        {
            Debug.Log("Vista spike: " + Create(DefaultTemplate));
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

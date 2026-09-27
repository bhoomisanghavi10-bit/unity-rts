using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Camera;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace KingdomsOfBharat.Performance
{
    // A deliberately small, deterministic workload for comparing Development-player
    // performance. It is opt-in through -representative-benchmark so normal matches
    // and tests never receive benchmark-only placements or commands.
    public static class RepresentativeBenchmarkBootstrap
    {
        public const string CommandLineFlag = "-representative-benchmark";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (!HasArgument(CommandLineFlag))
                return;

            var runner = new GameObject("RepresentativeBenchmarkRunner");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<RepresentativeBenchmarkRunner>();
        }

        internal static bool HasArgument(string argument)
        {
            foreach (string value in Environment.GetCommandLineArgs())
            {
                if (string.Equals(value, argument, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    public sealed class RepresentativeBenchmarkRunner : MonoBehaviour
    {
        public const string ScenarioId = "representative_battle_v1";
        public const int Seed = 20260926;
        public const int WarmupSeconds = 15;
        public const int SampleFrameCount = 900;
        private const int OutputSchemaVersion = 1;
        private const float BattleHealth = 500f;

        private readonly List<Attackable> _playerCombatants = new List<Attackable>();
        private readonly List<Attackable> _enemyCombatants = new List<Attackable>();
        private readonly List<double> _cpuFrameMs = new List<double>(SampleFrameCount);
        private readonly List<double> _gpuFrameMs = new List<double>(SampleFrameCount);
        private readonly List<double> _totalMemoryBytes = new List<double>(SampleFrameCount);
        private readonly List<double> _textureMemoryBytes = new List<double>(SampleFrameCount);
        private readonly List<double> _gcAllocBytes = new List<double>(SampleFrameCount);
        private readonly List<double> _batches = new List<double>(SampleFrameCount);
        private readonly List<double> _triangles = new List<double>(SampleFrameCount);
        private readonly List<double> _visibleSkinnedMeshes = new List<double>(SampleFrameCount);
        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];

        private ProfilerRecorder _gcAllocRecorder;
        private ProfilerRecorder _textureMemoryRecorder;
        private ProfilerRecorder _batchesRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private UnityEngine.Camera _camera;
        private float _captureStartedAt;
        private float _cameraPathStartedAt;
        private float _nextOrderAt;
        private bool _capturing;

        private void Start()
        {
            UnityEngine.Random.InitState(Seed);
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            StartCoroutine(PrepareScenario());
        }

        private IEnumerator PrepareScenario()
        {
            // CivPicker has already run Awake by AfterSceneLoad. Yield once so its
            // Start-time UI initialization cannot race the explicit match setup.
            yield return null;

            CivilizationSetup setup = FindFirstObjectByType<CivilizationSetup>();
            if (setup == null)
            {
                Debug.LogError("Representative benchmark could not find CivilizationSetup.");
                FinishWithError("CivilizationSetup was not present in the build scene.");
                yield break;
            }

            setup.BeginCustomScenarioMatch(BuildScenario());
            // RiverValley normally asks ResourceNodeSpawner for a fresh seed. Pin
            // the selected map before its Start method runs so surrounding resource
            // placement is part of the repeatable workload as well.
            MapRegistry.Current.ResourceSeed = Seed;
            foreach (var ai in FindObjectsByType<KingdomsOfBharat.AI.AiController>(FindObjectsSortMode.None))
                ai.enabled = false;

            // Terrain/NavMesh rebuild and model loading occur before the warm-up.
            yield return null;
            yield return null;
            ConfigureCamera();
            ConfigureBattle();
            BeginWarmup();
        }

        internal static CustomScenarioData BuildScenario()
        {
            var data = new CustomScenarioData
            {
                id = ScenarioId,
                title = "Representative Settlement and Mixed Battle",
                mapId = (int)MapId.RiverValley,
                playerCivilization = (int)CivilizationId.Chola,
                aiCivilization = (int)CivilizationId.Vijayanagara,
            };

            AddSettlement(data, FactionId.Player, 20f);
            AddSettlement(data, FactionId.Enemy, -20f);
            AddMixedForce(data, FactionId.Player, 8f);
            AddMixedForce(data, FactionId.Enemy, -8f);
            return data;
        }

        private static void AddSettlement(CustomScenarioData data, FactionId faction, float z)
        {
            AddBuilding(data, "building.common.town_center", faction, new Vector3(0f, 1f, z));
            AddBuilding(data, "building.common.barracks", faction, new Vector3(-8f, 1f, z));
            AddBuilding(data, "House", faction, new Vector3(8f, 1f, z));
            AddBuilding(data, "Farm", faction, new Vector3(12f, 1f, z + 4f));
            AddBuilding(data, "Market", faction, new Vector3(-12f, 1f, z + 4f));
        }

        private static void AddMixedForce(CustomScenarioData data, FactionId faction, float z)
        {
            // 12 units per side: 2 Workers, 3 Padati/Soldiers, 3 Dhanurdhara/
            // Archers, 2 Cavalry, and 2 Siege. The catalog IDs exercise the
            // reference slice while unmigrated factories stay in the workload.
            AddUnit(data, "unit.common.worker", faction, new Vector3(-5f, 1f, z));
            AddUnit(data, "unit.common.worker", faction, new Vector3(5f, 1f, z));
            AddUnit(data, "unit.chola.padati.t1", faction, new Vector3(-3f, 1f, z + 1.5f));
            AddUnit(data, "unit.chola.padati.t1", faction, new Vector3(0f, 1f, z + 1.5f));
            AddUnit(data, "unit.chola.padati.t1", faction, new Vector3(3f, 1f, z + 1.5f));
            AddUnit(data, "unit.chola.dhanurdhara.t1", faction, new Vector3(-3f, 1f, z - 1.5f));
            AddUnit(data, "unit.chola.dhanurdhara.t1", faction, new Vector3(0f, 1f, z - 1.5f));
            AddUnit(data, "unit.chola.dhanurdhara.t1", faction, new Vector3(3f, 1f, z - 1.5f));
            AddUnit(data, "Cavalry", faction, new Vector3(-6f, 1f, z + 3f));
            AddUnit(data, "Cavalry", faction, new Vector3(6f, 1f, z + 3f));
            AddUnit(data, "Siege", faction, new Vector3(-6f, 1f, z - 3f));
            AddUnit(data, "Siege", faction, new Vector3(6f, 1f, z - 3f));
        }

        private static void AddBuilding(CustomScenarioData data, string type, FactionId faction, Vector3 position)
        {
            data.buildings.Add(new BuildingSaveData { buildingType = type, faction = (int)faction, position = position });
        }

        private static void AddUnit(CustomScenarioData data, string type, FactionId faction, Vector3 position)
        {
            data.units.Add(new UnitSaveData { unitType = type, faction = (int)faction, position = position });
        }

        private void ConfigureCamera()
        {
            _camera = UnityEngine.Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<UnityEngine.Camera>();
            if (_camera == null)
                return;

            RTSCameraController controller = _camera.GetComponent<RTSCameraController>();
            if (controller != null)
                controller.enabled = false;
            _cameraPathStartedAt = Time.unscaledTime;
            SetCameraPose(0f);
        }

        private void ConfigureBattle()
        {
            foreach (Attackable attackable in FindObjectsByType<Attackable>(FindObjectsSortMode.None))
            {
                if (!attackable.TryGetComponent(out FactionMember faction))
                    continue;
                if (!attackable.TryGetComponent(out MeleeAttacker _))
                    continue;

                // Keeps every composition member present through the bounded sample
                // while retaining the normal movement, animation, projectile, and
                // attack update paths.
                attackable.Configure(BattleHealth);
                if (faction.Faction == FactionId.Player)
                    _playerCombatants.Add(attackable);
                else if (faction.Faction == FactionId.Enemy)
                    _enemyCombatants.Add(attackable);
            }

            IssueBattleOrders();
        }

        private void BeginWarmup()
        {
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 1);
            _textureMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory", 1);
            _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            _captureStartedAt = Time.unscaledTime;
            _nextOrderAt = _captureStartedAt + 1f;
            Debug.Log($"Representative benchmark warm-up: {WarmupSeconds}s, then {SampleFrameCount} bounded frames.");
        }

        private void Update()
        {
            if (_camera != null)
                SetCameraPose(Time.unscaledTime - _cameraPathStartedAt);

            if (_captureStartedAt <= 0f)
                return;
            if (Time.unscaledTime >= _nextOrderAt)
            {
                IssueBattleOrders();
                _nextOrderAt += 1f;
            }

            if (!_capturing && Time.unscaledTime - _captureStartedAt >= WarmupSeconds)
            {
                _capturing = true;
                Debug.Log("Representative benchmark steady-state capture started.");
            }
        }

        private void LateUpdate()
        {
            if (!_capturing || _cpuFrameMs.Count >= SampleFrameCount)
                return;

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
            {
                _cpuFrameMs.Add(_frameTimings[0].cpuFrameTime);
                if (_frameTimings[0].gpuFrameTime > 0d)
                    _gpuFrameMs.Add(_frameTimings[0].gpuFrameTime);
            }
            else
            {
                _cpuFrameMs.Add(Time.unscaledDeltaTime * 1000d);
            }

            _totalMemoryBytes.Add(Profiler.GetTotalAllocatedMemoryLong());
            _textureMemoryBytes.Add(RecorderValueOrNaN(_textureMemoryRecorder));
            _gcAllocBytes.Add(RecorderValueOrNaN(_gcAllocRecorder));
            _batches.Add(RecorderValueOrNaN(_batchesRecorder));
            _triangles.Add(RecorderValueOrNaN(_trianglesRecorder));
            _visibleSkinnedMeshes.Add(VisibleSkinnedMeshCount());

            if (_cpuFrameMs.Count == SampleFrameCount)
                Finish();
        }

        private void IssueBattleOrders()
        {
            OrderSide(_playerCombatants, _enemyCombatants);
            OrderSide(_enemyCombatants, _playerCombatants);
        }

        private static void OrderSide(List<Attackable> attackers, List<Attackable> targets)
        {
            if (targets.Count == 0)
                return;
            for (int i = 0; i < attackers.Count; i++)
            {
                Attackable attacker = attackers[i];
                Attackable target = targets[i % targets.Count];
                if (attacker != null && !attacker.IsDead && target != null && !target.IsDead &&
                    attacker.TryGetComponent(out MeleeAttacker combat))
                    combat.AttackMove(target);
            }
        }

        private void SetCameraPose(float elapsedSeconds)
        {
            // A 24-second looping, deterministic orbit keeps both settlements and
            // the central engagement visible without input-driven camera variance.
            float phase = (elapsedSeconds % 24f) / 24f * Mathf.PI * 2f;
            Vector3 position = new Vector3(Mathf.Sin(phase) * 22f, 30f + Mathf.Cos(phase * 2f) * 3f, Mathf.Cos(phase) * 30f);
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-position.normalized, Vector3.up));
        }

        private static double RecorderValueOrNaN(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : double.NaN;
        }

        private static double VisibleSkinnedMeshCount()
        {
            int count = 0;
            foreach (SkinnedMeshRenderer renderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.isVisible)
                    count++;
            }
            return count;
        }

        private void Finish()
        {
            _capturing = false;
            var result = new BenchmarkResult
            {
                schemaVersion = OutputSchemaVersion,
                scenarioId = ScenarioId,
                seed = Seed,
                map = MapId.RiverValley.ToString(),
                warmupSeconds = WarmupSeconds,
                sampledFrames = _cpuFrameMs.Count,
                gpuSampledFrames = _gpuFrameMs.Count,
                qualityPreset = QualitySettings.names[QualitySettings.GetQualityLevel()],
                resolution = Screen.width + "x" + Screen.height,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                cpuFrameMs = Summary.From(_cpuFrameMs, "FrameTimingManager.cpuFrameTime"),
                gpuFrameMs = Summary.From(_gpuFrameMs, "FrameTimingManager.gpuFrameTime"),
                totalMemoryBytes = Summary.From(_totalMemoryBytes, "Profiler.GetTotalAllocatedMemoryLong"),
                textureMemoryBytes = Summary.From(_textureMemoryBytes, "ProfilerRecorder: Memory/Texture Memory"),
                gcAllocBytes = Summary.From(_gcAllocBytes, "ProfilerRecorder: Memory/GC.Alloc"),
                batches = Summary.From(_batches, "ProfilerRecorder: Render/Draw Calls Count"),
                triangles = Summary.From(_triangles, "ProfilerRecorder: Render/Triangles Count"),
                visibleSkinnedMeshes = Summary.From(_visibleSkinnedMeshes, "visible SkinnedMeshRenderer count"),
            };

            string outputPath = OutputPath();
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
            Debug.Log("Representative benchmark result written to " + outputPath);
            DisposeRecorders();
            Application.Quit(0);
        }

        private void FinishWithError(string error)
        {
            Debug.LogError("Representative benchmark failed: " + error);
            Application.Quit(1);
        }

        private void OnDestroy()
        {
            DisposeRecorders();
        }

        private void DisposeRecorders()
        {
            if (_gcAllocRecorder.Valid) _gcAllocRecorder.Dispose();
            if (_textureMemoryRecorder.Valid) _textureMemoryRecorder.Dispose();
            if (_batchesRecorder.Valid) _batchesRecorder.Dispose();
            if (_trianglesRecorder.Valid) _trianglesRecorder.Dispose();
        }

        private static string OutputPath()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-benchmark-output")
                    return args[i + 1];
            }
            return Path.Combine(Application.persistentDataPath, "representative-benchmark-result.json");
        }

        [Serializable]
        private sealed class BenchmarkResult
        {
            public int schemaVersion;
            public string scenarioId;
            public int seed;
            public string map;
            public int warmupSeconds;
            public int sampledFrames;
            public int gpuSampledFrames;
            public string qualityPreset;
            public string resolution;
            public string unityVersion;
            public string platform;
            public Summary cpuFrameMs;
            public Summary gpuFrameMs;
            public Summary totalMemoryBytes;
            public Summary textureMemoryBytes;
            public Summary gcAllocBytes;
            public Summary batches;
            public Summary triangles;
            public Summary visibleSkinnedMeshes;
        }

        [Serializable]
        private sealed class Summary
        {
            public string source;
            public bool available;
            public int samples;
            public double median;
            public double p95;
            public double p99;

            public static Summary From(List<double> values, string source)
            {
                var usable = new List<double>(values.Count);
                foreach (double value in values)
                    if (!double.IsNaN(value) && !double.IsInfinity(value)) usable.Add(value);
                usable.Sort();
                if (usable.Count == 0)
                    return new Summary { source = source, available = false };
                return new Summary
                {
                    source = source,
                    available = true,
                    samples = usable.Count,
                    median = Percentile(usable, 0.50),
                    p95 = Percentile(usable, 0.95),
                    p99 = Percentile(usable, 0.99),
                };
            }

            private static double Percentile(List<double> sorted, double percentile)
            {
                int index = Mathf.Clamp(Mathf.CeilToInt((float)(percentile * sorted.Count)) - 1, 0, sorted.Count - 1);
                return sorted[index];
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Camera;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace KingdomsOfBharat.Performance
{
    // Opt-in (-art-reference) reference scene for judging art and readability in the real
    // production spawn path. It lays out one representative of each asset class on the
    // Coastal map (terrain + water), then photographs and measures each at gameplay zoom and
    // close-inspection zoom, in the default quality preset and the lowest one. It also runs a
    // House-specific pipeline audit and a LOD-transition sweep. Output: JSON + PNGs in the
    // -art-output directory. Run it under a Development player, never as a shipping benchmark.
    public static class ArtReferenceBootstrap
    {
        public const string Flag = "-art-reference";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (!RepresentativeBenchmarkBootstrap.HasArgument(Flag)) return;
            var go = new GameObject("ArtReferenceRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ArtReferenceRunner>();
        }
    }

    public sealed class ArtReferenceRunner : MonoBehaviour
    {
        private const float GameplayDistance = 38f;
        private const int SettleFrames = 20;
        private const int SampleFrames = 90;

        private struct Station
        {
            public string name;
            public Vector3 focus;
            public float closeDistance;
        }

        // World layout (Coastal map: land x < 37, water x >= 37.5).
        private static readonly Station[] Stations =
        {
            new Station { name = "worker_and_units", focus = new Vector3(-14f, 1f, 1f), closeDistance = 7f },
            new Station { name = "house_cluster",    focus = new Vector3(-8f, 1f, -14f), closeDistance = 9f },
            new Station { name = "barracks",         focus = new Vector3(3f, 1f, -14f), closeDistance = 12f },
            new Station { name = "town_center",      focus = new Vector3(16f, 1f, -14f), closeDistance = 17f },
            new Station { name = "wall_and_gate",    focus = new Vector3(20f, 1f, 4f), closeDistance = 9f },
            new Station { name = "terrain_water",    focus = new Vector3(37f, 0f, 0f), closeDistance = 14f },
            new Station { name = "ranged_combat",    focus = new Vector3(-14f, 1f, 16f), closeDistance = 14f },
        };

        private readonly List<Attackable> _players = new List<Attackable>();
        private readonly List<Attackable> _enemies = new List<Attackable>();
        private readonly List<CaptureRecord> _captures = new List<CaptureRecord>();
        private readonly List<LodTransitionRecord> _transitions = new List<LodTransitionRecord>();
        private GameObject _auditHouse;
        private UnityEngine.Camera _camera;
        private Quaternion _rotation;
        private string _outDir;
        private string _label;
        private ProfilerRecorder _batches, _tris, _setPass;
        private HouseAudit _houseAudit;

        private void Start()
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            _outDir = Arg("-art-output") ?? Path.Combine(Application.persistentDataPath, "art-reference");
            _label = Arg("-art-label") ?? "run";
            Directory.CreateDirectory(_outDir);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return null;
            CivilizationSetup setup = FindFirstObjectByType<CivilizationSetup>();
            if (setup == null) { Debug.LogError("ArtReference: no CivilizationSetup"); Application.Quit(1); yield break; }

            setup.BeginCustomScenarioMatch(BuildScenario());
            MapRegistry.Current.ResourceSeed = 20260926;
            foreach (var ai in FindObjectsByType<KingdomsOfBharat.AI.AiController>(FindObjectsSortMode.None)) ai.enabled = false;
            yield return null;
            yield return null;
            yield return null;

            foreach (ConstructionSite site in FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None)) site.CompleteImmediately();
            if (!FogOfWarManager.ToggleRevealAll()) FogOfWarManager.ToggleRevealAll();
            foreach (var fog in FindObjectsByType<FogOfWarManager>(FindObjectsSortMode.None))
                if (fog.TryGetComponent(out MeshRenderer fogRenderer)) fogRenderer.enabled = false;

            _camera = UnityEngine.Camera.main ?? FindFirstObjectByType<UnityEngine.Camera>();
            RTSCameraController controller = _camera.GetComponent<RTSCameraController>();
            if (controller != null) controller.enabled = false;
            _rotation = Quaternion.Euler(52f, 0f, 0f);

            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);

            // Combat for the ranged/impact station: keep everyone alive, order a fight.
            foreach (Attackable a in FindObjectsByType<Attackable>(FindObjectsSortMode.None))
            {
                if (!a.TryGetComponent(out FactionMember f) || !a.TryGetComponent(out MeleeAttacker _)) continue;
                a.Configure(800f);
                (f.Faction == FactionId.Player ? _players : _enemies).Add(a);
            }
            _auditHouse = FindAuditHouse();
            _houseAudit = AuditHouse(_auditHouse);

            int defaultQuality = QualitySettings.GetQualityLevel();
            int[] qualities = { defaultQuality, 0 };
            foreach (int quality in qualities)
            {
                QualitySettings.SetQualityLevel(quality, true);
                yield return null;
                foreach (Station station in Stations)
                {
                    foreach (bool close in new[] { false, true })
                    {
                        if (station.name == "ranged_combat") IssueOrders();
                        yield return CaptureStation(station, close, QualitySettings.names[quality]);
                    }
                }
            }
            QualitySettings.SetQualityLevel(defaultQuality, true);
            yield return LodSweep();
            Write();
            Application.Quit(0);
        }

        private static CustomScenarioData BuildScenario()
        {
            var d = new CustomScenarioData
            {
                id = "art_reference_v1", title = "Art and readability reference",
                mapId = (int)MapId.Coastal,
                playerCivilization = (int)CivilizationId.Chola,
                aiCivilization = (int)CivilizationId.Vijayanagara,
            };
            // Units: identical rows for both teams so team color is compared like for like.
            string[] units = { "unit.common.worker", "unit.chola.padati.t1", "unit.chola.dhanurdhara.t1", "Cavalry", "Siege" };
            for (int i = 0; i < units.Length; i++)
            {
                d.units.Add(U(units[i], FactionId.Player, new Vector3(-18f + i * 2.4f, 1f, 0f)));
                d.units.Add(U(units[i], FactionId.Enemy, new Vector3(-18f + i * 2.4f, 1f, 3.6f)));
            }
            // Ranged/impact station: archers firing at infantry 9 units away.
            for (int i = 0; i < 3; i++)
            {
                d.units.Add(U("unit.chola.dhanurdhara.t1", FactionId.Player, new Vector3(-19f + i * 2f, 1f, 12f)));
                d.units.Add(U("unit.chola.padati.t1", FactionId.Enemy, new Vector3(-19f + i * 2f, 1f, 21f)));
            }
            // Buildings: four Player Houses (the audited asset), one Enemy House, then the rest.
            for (int i = 0; i < 4; i++) d.buildings.Add(B("House", FactionId.Player, new Vector3(-14f + i * 4f, 1f, -14f)));
            d.buildings.Add(B("House", FactionId.Enemy, new Vector3(-14f, 1f, -22f)));
            d.buildings.Add(B("building.common.barracks", FactionId.Player, new Vector3(3f, 1f, -14f)));
            d.buildings.Add(B("building.common.town_center", FactionId.Player, new Vector3(16f, 1f, -14f)));
            d.buildings.Add(B("building.common.town_center", FactionId.Enemy, new Vector3(16f, 1f, -32f)));
            for (int i = 0; i < 4; i++) d.buildings.Add(B("Wall", FactionId.Player, new Vector3(14f + i * 2.4f, 1f, 4f)));
            d.buildings.Add(B("Gate", FactionId.Player, new Vector3(23.6f, 1f, 4f)));
            return d;
        }

        private static UnitSaveData U(string t, FactionId f, Vector3 p) => new UnitSaveData { unitType = t, faction = (int)f, position = p };
        private static BuildingSaveData B(string t, FactionId f, Vector3 p) => new BuildingSaveData { buildingType = t, faction = (int)f, position = p };

        private void IssueOrders()
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_enemies.Count == 0) return;
                Attackable a = _players[i]; Attackable t = _enemies[i % _enemies.Count];
                if (a != null && a.transform.position.z > 10f && t != null && a.TryGetComponent(out MeleeAttacker m)) m.AttackMove(t);
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                Attackable a = _enemies[i]; Attackable t = _players[i % _players.Count];
                if (a != null && a.transform.position.z > 10f && t != null && a.TryGetComponent(out MeleeAttacker m)) m.AttackMove(t);
            }
        }

        private void Place(Station s, float distance)
        {
            _camera.transform.SetPositionAndRotation(s.focus - _rotation * Vector3.forward * distance, _rotation);
        }

        private IEnumerator CaptureStation(Station s, bool close, string quality)
        {
            Place(s, close ? s.closeDistance : GameplayDistance);
            for (int i = 0; i < SettleFrames; i++) yield return null;

            var frameMs = new List<double>(SampleFrames);
            for (int i = 0; i < SampleFrames; i++) { yield return null; frameMs.Add(Time.unscaledDeltaTime * 1000.0); }
            frameMs.Sort();

            var rec = new CaptureRecord
            {
                station = s.name, view = close ? "close" : "gameplay", quality = quality,
                cpuFrameMsMedian = frameMs[frameMs.Count / 2], cpuFrameMsP95 = frameMs[(int)(frameMs.Count * 0.95)],
                batches = Val(_batches), setPassCalls = Val(_setPass), trianglesRendered = Val(_tris),
            };
            Census(rec);
            yield return new WaitForEndOfFrame();
            string file = $"{_label}_{s.name}_{rec.view}_{quality.Replace(' ', '-')}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, file));
            rec.screenshot = file;
            _captures.Add(rec);
        }

        // Counts what the camera actually renders now: LOD-hidden renderers are not visible.
        private static void Census(CaptureRecord rec)
        {
            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture>();
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.isVisible) continue;
                rec.visibleRenderers++;
                Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.TryGetComponent(out MeshFilter mf) ? mf.sharedMesh : null;
                if (mesh != null) rec.visibleMeshTriangles += mesh.triangles.Length / 3;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null || !materials.Add(m)) continue;
                    foreach (int id in m.GetTexturePropertyNameIDs())
                        if (m.GetTexture(id) is Texture t) textures.Add(t);
                }
            }
            rec.uniqueMaterials = materials.Count;
            rec.uniqueTextures = textures.Count;
            long bytes = 0;
            foreach (Texture t in textures) bytes += Profiler.GetRuntimeMemorySizeLong(t);
            rec.textureMemoryMB = bytes / 1048576.0;
        }

        private static GameObject FindAuditHouse()
        {
            foreach (House h in FindObjectsByType<House>(FindObjectsSortMode.None))
                if (h.TryGetComponent(out FactionMember f) && f.Faction == FactionId.Player) return h.gameObject;
            return null;
        }

        private HouseAudit AuditHouse(GameObject house)
        {
            var a = new HouseAudit();
            if (house == null) { a.found = false; return a; }
            a.found = true;
            a.rootName = house.name;
            Transform visual = house.transform.Find("Visual");
            a.hasVisualChild = visual != null;
            LODGroup group = house.GetComponentInChildren<LODGroup>(true);
            a.lodLevels = group != null ? group.lodCount : 0;
            var tris = new List<int>();
            if (group != null)
                foreach (LOD lod in group.GetLODs())
                    tris.Add(lod.renderers.OfType<MeshRenderer>().Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3));
            else
                tris.Add(house.GetComponentsInChildren<MeshFilter>(true).Sum(m => m.sharedMesh != null ? m.sharedMesh.triangles.Length / 3 : 0));
            a.trianglesPerLod = tris.ToArray();
            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture>();
            foreach (Renderer r in house.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    materials.Add(m);
                    foreach (int id in m.GetTexturePropertyNameIDs()) if (m.GetTexture(id) is Texture t) textures.Add(t);
                }
            }
            a.materialInstances = materials.Count;
            a.textures = textures.Select(t => $"{t.name} {t.width}x{t.height}").ToArray();
            a.textureMemoryMB = textures.Sum(t => Profiler.GetRuntimeMemorySizeLong(t)) / 1048576.0;
            MeshRenderer[] meshRenderers = house.GetComponentsInChildren<MeshRenderer>(true);
            a.rendererShadowModes = meshRenderers.Select(r => $"{r.gameObject.name}:cast={r.shadowCastingMode},receive={r.receiveShadows}").ToArray();
            int modelRenderers = group != null ? group.GetLODs().Sum(l => l.renderers.Length) : 1;
            a.teamAccentRenderers = meshRenderers.Length - modelRenderers;
            BoxCollider[] boxes = house.GetComponents<BoxCollider>();
            a.boxColliders = boxes.Length;
            a.hasNavMeshObstacle = house.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null;
            a.hasConstructionSiteComplete = house.TryGetComponent(out ConstructionSite cs) && cs.IsComplete;
            a.hasRequired = new[] { typeof(Building), typeof(House), typeof(FactionMember), typeof(Attackable) }
                .All(t => house.GetComponent(t) != null);
            a.hasBuildingFootprint = house.GetComponent<BuildingFootprintTag>() != null;
            if (boxes.Length == 1)
            {
                Bounds renderBounds = default; bool first = true;
                foreach (Renderer r in house.GetComponentsInChildren<Renderer>(true))
                {
                    if (group != null && !group.GetLODs()[0].renderers.Contains(r)) continue;
                    if (first) { renderBounds = r.bounds; first = false; } else renderBounds.Encapsulate(r.bounds);
                }
                Bounds c = boxes[0].bounds;
                a.colliderToRenderBoundsVolumeRatio = (c.size.x * c.size.y * c.size.z) / Mathf.Max(0.0001f, renderBounds.size.x * renderBounds.size.y * renderBounds.size.z);
            }
            return a;
        }

        // Dollies the camera at the audit house and records which LOD each step renders, then
        // photographs adjacent levels forced at the same camera to measure how different they look.
        private IEnumerator LodSweep()
        {
            if (_auditHouse == null) yield break;
            LODGroup group = _auditHouse.GetComponentInChildren<LODGroup>(true);
            Vector3 focus = _auditHouse.transform.position;
            if (group == null) { _houseAudit.lodSweepNote = "no LODGroup"; yield break; }
            var renderersByLevel = group.GetLODs().Select(l => l.renderers).ToArray();
            int previous = -1;
            for (float d = 90f; d >= 4f; d -= 0.75f)
            {
                _camera.transform.SetPositionAndRotation(focus - _rotation * Vector3.forward * d, _rotation);
                yield return null; yield return null;
                int level = -1;
                for (int i = 0; i < renderersByLevel.Length; i++)
                    if (renderersByLevel[i].Any(r => r != null && r.isVisible)) { level = i; break; }
                if (level != previous)
                {
                    Bounds b = renderersByLevel[Mathf.Max(0, level)][0].bounds;
                    float height = ScreenHeightFraction(b);
                    _transitions.Add(new LodTransitionRecord { cameraDistance = d, level = level, screenHeightFraction = height, screenHeightPixels = height * Screen.height });
                    previous = level;
                }
            }
            // Pairs: force level i and i+1 at each transition distance and compare the pixels.
            for (int i = 0; i < group.lodCount - 1; i++)
            {
                LodTransitionRecord t = _transitions.FirstOrDefault(x => x.level == i + 1);
                if (t == null) continue;
                _camera.transform.SetPositionAndRotation(focus - _rotation * Vector3.forward * t.cameraDistance, _rotation);
                group.ForceLOD(i); yield return null; yield return null; yield return new WaitForEndOfFrame();
                Texture2D a = ScreenCapture.CaptureScreenshotAsTexture();
                group.ForceLOD(i + 1); yield return null; yield return null; yield return new WaitForEndOfFrame();
                Texture2D b = ScreenCapture.CaptureScreenshotAsTexture();
                group.ForceLOD(-1);
                Rect rect = ScreenRect(group.GetLODs()[0].renderers[0].bounds, a.width, a.height);
                t.changedPixelPercent = ChangedPercent(a, b, rect);
                File.WriteAllBytes(Path.Combine(_outDir, $"{_label}_lod{i}_vs_lod{i + 1}_a.png"), a.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(_outDir, $"{_label}_lod{i}_vs_lod{i + 1}_b.png"), b.EncodeToPNG());
                Destroy(a); Destroy(b);
            }
        }

        private float ScreenHeightFraction(Bounds b)
        {
            Rect r = ScreenRect(b, Screen.width, Screen.height);
            return r.height / Screen.height;
        }

        private Rect ScreenRect(Bounds b, int w, int h)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue), max = new Vector3(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 s = _camera.WorldToScreenPoint(c);
                min = Vector3.Min(min, s); max = Vector3.Max(max, s);
            }
            float sx = w / (float)Screen.width, sy = h / (float)Screen.height;
            return Rect.MinMaxRect(Mathf.Clamp(min.x * sx, 0, w), Mathf.Clamp(min.y * sy, 0, h), Mathf.Clamp(max.x * sx, 0, w), Mathf.Clamp(max.y * sy, 0, h));
        }

        // Share of pixels inside the building's screen rect that differ visibly between two frames.
        private static double ChangedPercent(Texture2D a, Texture2D b, Rect r)
        {
            Color32[] pa = a.GetPixels32(), pb = b.GetPixels32();
            int changed = 0, total = 0;
            for (int y = (int)r.yMin; y < (int)r.yMax; y++)
                for (int x = (int)r.xMin; x < (int)r.xMax; x++)
                {
                    int i = y * a.width + x; total++;
                    if (Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g) + Mathf.Abs(pa[i].b - pb[i].b) > 48) changed++;
                }
            return total == 0 ? 0 : 100.0 * changed / total;
        }

        private static double Val(ProfilerRecorder r) => r.Valid ? r.LastValue : double.NaN;

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        private void Write()
        {
            var result = new Result
            {
                label = _label, unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                resolution = Screen.width + "x" + Screen.height, gpu = SystemInfo.graphicsDeviceName,
                defaultQuality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                house = _houseAudit, captures = _captures.ToArray(), lodTransitions = _transitions.ToArray(),
            };
            File.WriteAllText(Path.Combine(_outDir, $"{_label}_result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("ArtReference: wrote " + Path.Combine(_outDir, $"{_label}_result.json"));
        }

        [Serializable] private sealed class Result
        {
            public string label, unityVersion, platform, resolution, gpu, defaultQuality;
            public HouseAudit house; public CaptureRecord[] captures; public LodTransitionRecord[] lodTransitions;
        }

        [Serializable] private sealed class CaptureRecord
        {
            public string station, view, quality, screenshot;
            public double cpuFrameMsMedian, cpuFrameMsP95, batches, setPassCalls, trianglesRendered;
            public int visibleRenderers, visibleMeshTriangles, uniqueMaterials, uniqueTextures;
            public double textureMemoryMB;
        }

        [Serializable] private sealed class LodTransitionRecord
        {
            public float cameraDistance, screenHeightFraction, screenHeightPixels;
            public int level;
            public double changedPixelPercent;
        }

        [Serializable] private sealed class HouseAudit
        {
            public bool found, hasVisualChild, hasNavMeshObstacle, hasConstructionSiteComplete, hasRequired, hasBuildingFootprint;
            public string rootName, lodSweepNote;
            public int lodLevels, materialInstances, boxColliders, teamAccentRenderers;
            public int[] trianglesPerLod; public string[] textures, rendererShadowModes;
            public double textureMemoryMB, colliderToRenderBoundsVolumeRatio;
        }
    }
}

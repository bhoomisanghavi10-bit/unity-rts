using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.UI
{
    // Item 6 (Scenario Editor, heavy path session 1): the in-game runtime
    // editor for placements (starting units/buildings per faction) - real
    // UGC/modding, not a Unity EditorWindow (user-confirmed). Opened via a
    // new "Create Scenario" button on MissionSelectMenu.cs, same self-
    // destroying handoff MissionSelectMenu.ChooseScenario already uses.
    //
    // ProceduralGround/RTSCameraController are always-active from scene
    // load (confirmed by reading ProceduralGround.cs directly, not gated
    // like TownCenterSpawner) - editing needs no match-start machinery at
    // all, only Play does (CivilizationSetup.BeginCustomScenarioMatch).
    //
    // v1 deliberately keeps placement markers lightweight (a plain tinted
    // primitive, no model/NavMeshObstacle/VisionSource/construction site) -
    // the real heavyweight factory output only ever gets spawned once the
    // scenario is actually played (EntitySpawner), so free repositioning
    // during editing doesn't fight construction-site/NavMesh side effects.
    // Explicitly deferred, not silently dropped: objective/trigger
    // authoring (v1 defaults to standard Conquest), richer palette icons,
    // a saved-scenario browse list back on MissionSelectMenu, floating
    // per-marker labels, multiplayer/LAN play of a custom scenario.
    public class ScenarioEditorMenu : MonoBehaviour
    {
        private class PlacedMarker
        {
            public GameObject Visual;
            public string EntityType;
            public bool IsBuilding;
            public FactionId Faction;
        }

        // No existing faction-color convention anywhere in this project
        // (checked - MinimapController/etc. don't tint by faction either) -
        // this is a fresh, editor-only convention, not reused from
        // elsewhere.
        private static readonly Dictionary<FactionId, Color> FactionColors = new Dictionary<FactionId, Color>
        {
            { FactionId.Player, new Color(0.25f, 0.45f, 0.95f) },
            { FactionId.Enemy, new Color(0.9f, 0.25f, 0.25f) },
            { FactionId.Enemy2, new Color(0.3f, 0.8f, 0.35f) },
        };

        private const float MarkerPickRadius = 1.5f;
        private const string ScenarioFolderName = "Scenarios";

        private UnityEngine.Camera _camera;
        private readonly List<PlacedMarker> _markers = new List<PlacedMarker>();

        private FactionId _selectedFaction = FactionId.Player;
        private string _selectedType; // null = no placement type selected (drag/delete only)
        private bool _selectedIsBuilding;

        private PlacedMarker _dragging;

        private TMP_InputField _nameField;
        private TMP_Text _statusLabel;
        private Transform _fileListContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Not self-bootstrapping like SettingsMenu/DiplomacyMenu - this
            // only exists while actively editing, created by
            // MissionSelectMenu's "Create Scenario" button and destroyed by
            // its own Back/Play buttons, same lifecycle
            // MissionSelectMenu.ChooseScenario already establishes for
            // itself.
        }

        public static void Open()
        {
            var go = new GameObject("ScenarioEditorMenu");
            go.AddComponent<ScenarioEditorMenu>();
        }

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
            BuildUi();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                OnLeftClickDown();
            }
            else if (Input.GetMouseButton(0) && _dragging != null)
            {
                if (TryGetGroundPoint(out Vector3 point))
                {
                    _dragging.Visual.transform.position = point + Vector3.up * 0.5f;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _dragging = null;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TryDeleteMarkerUnderCursor();
            }
        }

        private void OnLeftClickDown()
        {
            // Clicking an existing marker always starts a drag, regardless
            // of the current palette selection - matches a normal "click
            // and drag what's under the cursor" editor convention rather
            // than requiring a separate "move mode."
            PlacedMarker hit = FindMarkerUnderCursor();
            if (hit != null)
            {
                _dragging = hit;
                return;
            }

            if (_selectedType == null)
            {
                return;
            }

            if (!TryGetGroundPoint(out Vector3 groundPoint))
            {
                return;
            }

            PlaceMarker(_selectedType, _selectedIsBuilding, _selectedFaction, groundPoint);
        }

        private void TryDeleteMarkerUnderCursor()
        {
            PlacedMarker hit = FindMarkerUnderCursor();
            if (hit == null)
            {
                return;
            }

            _markers.Remove(hit);
            Destroy(hit.Visual);
            if (_dragging == hit)
            {
                _dragging = null;
            }
        }

        private PlacedMarker FindMarkerUnderCursor()
        {
            if (!TryGetGroundPoint(out Vector3 point))
            {
                return null;
            }

            PlacedMarker closest = null;
            float closestDistance = MarkerPickRadius;
            foreach (PlacedMarker marker in _markers)
            {
                float distance = Vector3.Distance(marker.Visual.transform.position, point);
                if (distance <= closestDistance)
                {
                    closest = marker;
                    closestDistance = distance;
                }
            }
            return closest;
        }

        // Same ground-raycast pattern BuildingPlacer.TryGetGroundPoint
        // already establishes.
        private bool TryGetGroundPoint(out Vector3 point)
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }

            if (_camera != null)
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                {
                    point = hit.point;
                    return true;
                }
            }

            point = Vector3.zero;
            return false;
        }

        private void PlaceMarker(string entityType, bool isBuilding, FactionId faction, Vector3 position)
        {
            GameObject visual = GameObject.CreatePrimitive(isBuilding ? PrimitiveType.Cube : PrimitiveType.Capsule);
            visual.name = "Marker_" + entityType;
            visual.transform.SetParent(transform, false);
            visual.transform.position = position + Vector3.up * 0.5f;
            visual.transform.localScale = isBuilding ? new Vector3(1.5f, 1f, 1.5f) : new Vector3(0.6f, 1f, 0.6f);
            visual.GetComponent<Renderer>().sharedMaterial = GameplayMaterial.CreateOpaque(FactionColors[faction]);

            _markers.Add(new PlacedMarker
            {
                Visual = visual,
                EntityType = entityType,
                IsBuilding = isBuilding,
                Faction = faction,
            });
        }

        private void ClearMarkers()
        {
            foreach (PlacedMarker marker in _markers)
            {
                Destroy(marker.Visual);
            }
            _markers.Clear();
        }

        // --- Save / Load / Play ---

        private static string ScenarioFolder => Path.Combine(Application.persistentDataPath, ScenarioFolderName);

        private void Save()
        {
            string name = _nameField.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Enter a scenario name before saving.");
                return;
            }

            var data = new CustomScenarioData
            {
                id = name,
                title = name,
                mapId = (int)MapId.RiverValley,
                playerCivilization = (int)CivilizationId.Chola,
                aiCivilization = (int)CivilizationId.Vijayanagara,
            };

            foreach (PlacedMarker marker in _markers)
            {
                if (marker.IsBuilding)
                {
                    data.buildings.Add(new BuildingSaveData
                    {
                        buildingType = marker.EntityType,
                        faction = (int)marker.Faction,
                        position = marker.Visual.transform.position - Vector3.up * 0.5f,
                        health = 1f,
                        isComplete = true,
                    });
                }
                else
                {
                    data.units.Add(new UnitSaveData
                    {
                        unitType = marker.EntityType,
                        faction = (int)marker.Faction,
                        position = marker.Visual.transform.position - Vector3.up * 0.5f,
                        health = 1f,
                        stance = -1,
                    });
                }
            }

            Directory.CreateDirectory(ScenarioFolder);
            string path = Path.Combine(ScenarioFolder, name + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            SetStatus("Saved to " + path);
            RefreshFileList();
        }

        private void LoadFile(string name)
        {
            string path = Path.Combine(ScenarioFolder, name + ".json");
            if (!File.Exists(path))
            {
                SetStatus("Not found: " + name);
                return;
            }

            CustomScenarioData data = JsonUtility.FromJson<CustomScenarioData>(File.ReadAllText(path));
            ClearMarkers();
            _nameField.text = data.title;

            foreach (BuildingSaveData building in data.buildings)
            {
                PlaceMarker(building.buildingType, true, (FactionId)building.faction, building.position);
            }
            foreach (UnitSaveData unit in data.units)
            {
                PlaceMarker(unit.unitType, false, (FactionId)unit.faction, unit.position);
            }

            SetStatus("Loaded " + name);
        }

        private void Play()
        {
            string name = string.IsNullOrEmpty(_nameField.text.Trim()) ? "untitled" : _nameField.text.Trim();
            var data = new CustomScenarioData
            {
                id = name,
                title = name,
                mapId = (int)MapId.RiverValley,
                playerCivilization = (int)CivilizationId.Chola,
                aiCivilization = (int)CivilizationId.Vijayanagara,
            };

            foreach (PlacedMarker marker in _markers)
            {
                if (marker.IsBuilding)
                {
                    data.buildings.Add(new BuildingSaveData
                    {
                        buildingType = marker.EntityType,
                        faction = (int)marker.Faction,
                        position = marker.Visual.transform.position - Vector3.up * 0.5f,
                        health = 1f,
                        isComplete = true,
                    });
                }
                else
                {
                    data.units.Add(new UnitSaveData
                    {
                        unitType = marker.EntityType,
                        faction = (int)marker.Faction,
                        position = marker.Visual.transform.position - Vector3.up * 0.5f,
                        health = 1f,
                        stance = -1,
                    });
                }
            }

            var civSetup = FindFirstObjectByType<CivilizationSetup>();
            if (civSetup == null)
            {
                SetStatus("No CivilizationSetup in scene - can't play.");
                return;
            }

            civSetup.BeginCustomScenarioMatch(data);
            Close();
        }

        private void Close()
        {
            ClearMarkers();
            Destroy(gameObject);
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = text;
            }
        }

        // --- UI ---

        private void BuildUi()
        {
            var canvasGo = new GameObject("ScenarioEditorCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // same top-level tier as MissionSelectMenu
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            UIStyleTheme.Current.ApplyPanel(panelImage);
            var panelRect = panelGo.GetComponent<RectTransform>();
            // Left-anchored side panel, not full-screen - the rest of the
            // screen stays the real game world/camera so placement clicks
            // can raycast onto it.
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.sizeDelta = new Vector2(300f, 0f);
            panelRect.anchoredPosition = Vector2.zero;

            float y = 500f;
            CreateLabel(panelGo.transform, "Scenario Editor", new Vector2(150f, y), 20, TextAlignmentOptions.Center);
            y -= 40f;

            CreateLabel(panelGo.transform, "Faction", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 26f;
            float fx = 60f;
            foreach (FactionId faction in new[] { FactionId.Player, FactionId.Enemy, FactionId.Enemy2 })
            {
                CreateButton(panelGo.transform, faction.ToString(), new Vector2(fx, y), new Vector2(85f, 26f),
                    () => { _selectedFaction = faction; SetStatus("Faction: " + faction); });
                fx += 90f;
            }
            y -= 40f;

            CreateLabel(panelGo.transform, "Buildings", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 24f;
            foreach (string type in EntitySpawner.BuildingTypes)
            {
                CreatePaletteButton(panelGo.transform, type, isBuilding: true, ref y);
            }
            y -= 10f;

            CreateLabel(panelGo.transform, "Units", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 24f;
            foreach (string type in EntitySpawner.UnitTypes)
            {
                CreatePaletteButton(panelGo.transform, type, isBuilding: false, ref y);
            }
            y -= 16f;

            var nameFieldGo = CreateInputField(panelGo.transform, new Vector2(150f, y));
            _nameField = nameFieldGo;
            y -= 40f;

            CreateButton(panelGo.transform, "Save", new Vector2(75f, y), new Vector2(130f, 30f), Save);
            CreateButton(panelGo.transform, "Play", new Vector2(220f, y), new Vector2(130f, 30f), Play);
            y -= 36f;
            CreateButton(panelGo.transform, "Back", new Vector2(150f, y), new Vector2(130f, 30f), Close);
            y -= 36f;

            CreateLabel(panelGo.transform, "Saved Scenarios", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 22f;
            var fileListGo = new GameObject("FileList");
            fileListGo.transform.SetParent(panelGo.transform, false);
            var fileListRect = fileListGo.AddComponent<RectTransform>();
            fileListRect.anchorMin = new Vector2(0.5f, 0.5f);
            fileListRect.anchorMax = new Vector2(0.5f, 0.5f);
            fileListRect.sizeDelta = Vector2.zero;
            fileListRect.anchoredPosition = new Vector2(0f, y);
            _fileListContainer = fileListGo.transform;
            RefreshFileList();

            _statusLabel = CreateLabel(panelGo.transform, "Click the ground to place; drag to move; right-click to delete.",
                new Vector2(150f, -520f), 12, TextAlignmentOptions.Center, wrapWidth: 280f);
        }

        private void RefreshFileList()
        {
            for (int i = _fileListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_fileListContainer.GetChild(i).gameObject);
            }

            if (!Directory.Exists(ScenarioFolder))
            {
                return;
            }

            float y = 0f;
            foreach (string path in Directory.GetFiles(ScenarioFolder, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                CreateButton(_fileListContainer, name, new Vector2(0f, y), new Vector2(260f, 24f), () => LoadFile(name));
                y -= 26f;
            }
        }

        private void CreatePaletteButton(Transform parent, string type, bool isBuilding, ref float y)
        {
            TMP_Text label = CreateButton(parent, type, new Vector2(150f, y), new Vector2(260f, 24f), () =>
            {
                _selectedType = type;
                _selectedIsBuilding = isBuilding;
                SetStatus("Placing: " + type + " (" + _selectedFaction + ")");
            });
            label.fontSize = 13;
            y -= 26f;
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment, float wrapWidth = 260f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(wrapWidth, 30f);
            rect.anchoredPosition = position;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static TMP_Text CreateButton(Transform parent, string text, Vector2 position, Vector2 size, System.Action onClick)
        {
            var go = new GameObject("Button_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            UIStyleTheme.Current.ApplyButton(image);

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        // No existing TMP_InputField precedent anywhere in this project
        // (checked directly) - a new, standard, low-risk TMP component for
        // the one piece of free-text input this feature genuinely needs
        // (the scenario's save name).
        private static TMP_InputField CreateInputField(Transform parent, Vector2 position)
        {
            var go = new GameObject("NameField");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 30f);
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            UIStyleTheme.Current.ApplyButton(image);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.color = UIStyleTheme.Current.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(8f, 2f);
            placeholderRect.offsetMax = new Vector2(-8f, -2f);
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "Scenario name...";
            placeholderTmp.fontSize = 14;
            placeholderTmp.color = UIStyleTheme.Current.TextSecondary;
            placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderTmp.fontStyle = FontStyles.Italic;

            var field = go.AddComponent<TMP_InputField>();
            field.textViewport = rect;
            field.textComponent = tmp;
            field.placeholder = placeholderTmp;
            return field;
        }
    }
}

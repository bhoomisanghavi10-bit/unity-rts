using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;

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
    //
    // Heavy path session 2 added the Objectives tab: authoring objectives/
    // triggers in-game via MissionCsvLoader's own ObjectiveRow/TriggerRow
    // types and shared interpreter (BuildObjectivesFromRows/
    // BuildTriggersFromRows) - the same small fixed vocabulary the CSV
    // (light) path uses. Still explicitly deferred, not silently dropped:
    // per-kind bespoke input widgets (Param fields stay generic text, same
    // as the CSV path), richer palette icons, a saved-scenario browse list
    // back on MissionSelectMenu, floating per-marker labels, multiplayer/
    // LAN play of a custom scenario.
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

        // Heavy path session 2: Objective/Trigger authoring. Two tabs share
        // the same side panel - Placements (the palette/faction rows
        // session 1 already built) and Objectives (this session's new
        // section) - only one is visible at a time, toggled by 2 buttons at
        // the top of the panel rather than 2 separate panels, keeping
        // Save/Play/Back/name field/file list at the bottom common to both.
        private enum Tab { Placements, Objectives }
        private Tab _activeTab = Tab.Placements;
        private Transform _placementsSection;
        private Transform _objectivesSection;
        private Transform _objectivesScrollContent;
        private TMP_InputField _victoryTextField;
        private TMP_InputField _defeatTextField;
        // Authoritative current value, independent of _victoryTextField/
        // _defeatTextField's own lifetime - RefreshObjectivesSection()
        // destroys and recreates those TMP_InputFields on every Add/
        // Remove/Kind-change, so reading .text back off a stale/destroyed
        // reference would lose whatever the author typed. onValueChanged
        // keeps these in sync live; RefreshObjectivesSection re-seeds the
        // freshly created fields from these, not the other way around.
        private string _victoryText = string.Empty;
        private string _defeatText = string.Empty;

        private readonly List<ObjectiveRow> _objectiveRows = new List<ObjectiveRow>();
        private readonly List<TriggerRow> _triggerRows = new List<TriggerRow>();

        // Short param hints so the generic Param1-3/Param1-4 text slots are
        // self-documenting in the UI - same small fixed vocabulary
        // MissionCsvLoader.cs's own class comment already discloses ("not a
        // general expression language"), just surfaced live instead of
        // left to a CSV author's own memory of the column meanings.
        private static readonly Dictionary<ObjectiveKind, string> ObjectiveHints = new Dictionary<ObjectiveKind, string>
        {
            { ObjectiveKind.SurviveSeconds, "Param1 = seconds to survive" },
            { ObjectiveKind.BuildingCountThreshold, "Param1 = building type (e.g. Barracks), Param2 = count, Param3 = faction (optional, default Player)" },
            { ObjectiveKind.ResourceThreshold, "Param1 = resource (Food/Wood/Gold/Stone), Param2 = amount, Param3 = faction (optional)" },
            { ObjectiveKind.PopulationThreshold, "Param1 = population amount, Param2 = faction (optional, default Player)" },
            { ObjectiveKind.DestroyScriptedTarget, "Param1 = building type (Barracks/TownCenter), Param2 = target faction, Param3 = position as \"x,y,z\"" },
        };

        private static readonly Dictionary<TriggerKind, string> TriggerHints = new Dictionary<TriggerKind, string>
        {
            { TriggerKind.GrantResourceAtTime, "Param1 = resource, Param2 = amount, Param3 = seconds, Param4 = faction (optional)" },
            { TriggerKind.RepeatingGrantResource, "Param1 = resource, Param2 = amount, Param3 = interval seconds, Param4 = repeat count, Param5 = faction (optional)" },
        };

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

            CustomScenarioData data = BuildScenarioData(name);

            Directory.CreateDirectory(ScenarioFolder);
            string path = Path.Combine(ScenarioFolder, name + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            SetStatus("Saved to " + path);
            RefreshFileList();
        }

        // Shared by Save() and Play() - both need the exact same
        // CustomScenarioData built from the current editing session's
        // markers/objectives/triggers, just written to different places
        // (a JSON file vs. straight into a live match).
        private CustomScenarioData BuildScenarioData(string name)
        {
            var data = new CustomScenarioData
            {
                id = name,
                title = name,
                mapId = (int)MapId.RiverValley,
                playerCivilization = (int)CivilizationId.Chola,
                aiCivilization = (int)CivilizationId.Vijayanagara,
                objectives = new List<ObjectiveRow>(_objectiveRows),
                triggers = new List<TriggerRow>(_triggerRows),
                victoryText = _victoryText,
                defeatText = _defeatText,
            };

            // Trigger Ids are auto-assigned here (not authored) - one field
            // fewer for the author to fill in, same auto-suffixing
            // convention MissionCsvLoader.BuildTriggersFromRows already
            // uses to expand a single RepeatingGrantResource row into many
            // one-shot triggers.
            for (int i = 0; i < data.triggers.Count; i++)
            {
                data.triggers[i].triggerId = "trigger_" + i;
            }

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

            return data;
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

            _objectiveRows.Clear();
            _objectiveRows.AddRange(data.objectives ?? new List<ObjectiveRow>());
            _triggerRows.Clear();
            _triggerRows.AddRange(data.triggers ?? new List<TriggerRow>());
            _victoryText = data.victoryText ?? string.Empty;
            _defeatText = data.defeatText ?? string.Empty;
            RefreshObjectivesSection();

            SetStatus("Loaded " + name);
        }

        private void Play()
        {
            string name = string.IsNullOrEmpty(_nameField.text.Trim()) ? "untitled" : _nameField.text.Trim();
            CustomScenarioData data = BuildScenarioData(name);

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

            // Heavy path session 2: tab toggle - Placements (session 1's
            // existing palette) vs. Objectives (this session's new
            // authoring UI). Both sections are built once here and shown/
            // hidden via SetActiveTab, not rebuilt on every switch - only
            // the Objectives list content itself rebuilds, on Add/Remove/
            // Kind-change (RefreshObjectivesSection).
            CreateButton(panelGo.transform, "Placements", new Vector2(85f, y), new Vector2(130f, 28f), () => SetActiveTab(Tab.Placements));
            CreateButton(panelGo.transform, "Objectives", new Vector2(215f, y), new Vector2(130f, 28f), () => SetActiveTab(Tab.Objectives));
            y -= 36f;

            float sectionTop = y;
            float sectionBottom = -40f; // where the common Save/Play/Back area begins below

            _placementsSection = CreateSectionContainer(panelGo.transform, "PlacementsSection");
            BuildPlacementsSection(_placementsSection, sectionTop);

            _objectivesSection = CreateSectionContainer(panelGo.transform, "ObjectivesSection");
            BuildObjectivesScaffold(_objectivesSection, sectionTop, sectionTop - sectionBottom);

            y = sectionBottom;
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

            RefreshObjectivesSection();
            SetActiveTab(Tab.Placements);
        }

        private void SetActiveTab(Tab tab)
        {
            _activeTab = tab;
            _placementsSection.gameObject.SetActive(tab == Tab.Placements);
            _objectivesSection.gameObject.SetActive(tab == Tab.Objectives);
            SetStatus(tab == Tab.Placements
                ? "Click the ground to place; drag to move; right-click to delete."
                : "Author objectives/triggers below. Empty objectives = standard Conquest (elimination).");
        }

        private static Transform CreateSectionContainer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.transform;
        }

        private void BuildPlacementsSection(Transform parent, float startY)
        {
            float y = startY;
            CreateLabel(parent, "Faction", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 26f;
            float fx = 60f;
            foreach (FactionId faction in new[] { FactionId.Player, FactionId.Enemy, FactionId.Enemy2 })
            {
                CreateButton(parent, faction.ToString(), new Vector2(fx, y), new Vector2(85f, 26f),
                    () => { _selectedFaction = faction; SetStatus("Faction: " + faction); });
                fx += 90f;
            }
            y -= 40f;

            CreateLabel(parent, "Buildings", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 24f;
            foreach (string type in EntitySpawner.BuildingTypes)
            {
                CreatePaletteButton(parent, type, isBuilding: true, ref y);
            }
            y -= 10f;

            CreateLabel(parent, "Units", new Vector2(150f, y), 14, TextAlignmentOptions.Center);
            y -= 24f;
            foreach (string type in EntitySpawner.UnitTypes)
            {
                CreatePaletteButton(parent, type, isBuilding: false, ref y);
            }
        }

        // Heavy path session 2: scaffolding only (ScrollRect/Viewport/
        // Content), same pattern SettingsMenu.cs's own Key Bindings list
        // already establishes for an unbounded, dynamically-sized row list.
        // RefreshObjectivesSection() populates/resizes Content on every
        // Add/Remove/Kind-change - not built here, since the row count
        // starts at zero and grows only once the author clicks "+ Add".
        private void BuildObjectivesScaffold(Transform parent, float topY, float height)
        {
            var scrollGo = new GameObject("ObjectivesScroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.sizeDelta = new Vector2(300f, height);
            scrollRect.anchoredPosition = new Vector2(150f, topY);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            _objectivesScrollContent = contentGo.transform;
        }

        // Rebuilds the entire Objectives-tab content column: Objective
        // rows, "+ Add Objective", Trigger rows, "+ Add Trigger", then
        // Victory/Defeat text fields at the bottom. Called after every
        // Add/Remove/Kind-change (NOT on every keystroke in a Param/
        // Description field - those write straight into the row object via
        // onValueChanged, no rebuild needed, so typing doesn't lose focus).
        private void RefreshObjectivesSection()
        {
            if (_objectivesScrollContent == null)
            {
                return;
            }

            for (int i = _objectivesScrollContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_objectivesScrollContent.GetChild(i).gameObject);
            }

            float y = 0f;
            CreateLabel(_objectivesScrollContent, "Objectives", new Vector2(150f, y), 16, TextAlignmentOptions.Center);
            y -= 24f;

            foreach (ObjectiveRow row in _objectiveRows)
            {
                BuildObjectiveRowUi(_objectivesScrollContent, row, ref y);
            }

            CreateButton(_objectivesScrollContent, "+ Add Objective", new Vector2(150f, y), new Vector2(240f, 26f), () =>
            {
                _objectiveRows.Add(new ObjectiveRow { kind = ObjectiveKind.SurviveSeconds, param1 = string.Empty, param2 = string.Empty, param3 = string.Empty, description = string.Empty });
                RefreshObjectivesSection();
            });
            y -= 36f;

            CreateLabel(_objectivesScrollContent, "Triggers", new Vector2(150f, y), 16, TextAlignmentOptions.Center);
            y -= 24f;

            foreach (TriggerRow row in _triggerRows)
            {
                BuildTriggerRowUi(_objectivesScrollContent, row, ref y);
            }

            CreateButton(_objectivesScrollContent, "+ Add Trigger", new Vector2(150f, y), new Vector2(240f, 26f), () =>
            {
                _triggerRows.Add(new TriggerRow { kind = TriggerKind.GrantResourceAtTime, param1 = string.Empty, param2 = string.Empty, param3 = string.Empty, param4 = string.Empty, param5 = string.Empty });
                RefreshObjectivesSection();
            });
            y -= 36f;

            CreateLabel(_objectivesScrollContent, "Victory Text (optional)", new Vector2(150f, y), 12, TextAlignmentOptions.Center);
            y -= 20f;
            _victoryTextField = CreateInputField(_objectivesScrollContent, new Vector2(150f, y), "Victory message...", height: 26f);
            _victoryTextField.text = _victoryText;
            _victoryTextField.onValueChanged.AddListener(v => _victoryText = v);
            y -= 32f;

            CreateLabel(_objectivesScrollContent, "Defeat Text (optional)", new Vector2(150f, y), 12, TextAlignmentOptions.Center);
            y -= 20f;
            _defeatTextField = CreateInputField(_objectivesScrollContent, new Vector2(150f, y), "Defeat message...", height: 26f);
            _defeatTextField.text = _defeatText;
            _defeatTextField.onValueChanged.AddListener(v => _defeatText = v);
            y -= 26f;

            ((RectTransform)_objectivesScrollContent).sizeDelta = new Vector2(0f, -y);
        }

        private void BuildObjectiveRowUi(Transform parent, ObjectiveRow row, ref float y)
        {
            CreateButton(parent, "Kind: " + row.kind, new Vector2(150f, y), new Vector2(260f, 24f), () =>
            {
                row.kind = NextEnum(row.kind);
                RefreshObjectivesSection();
            });
            y -= 26f;

            string hint = ObjectiveHints.TryGetValue(row.kind, out string h) ? h : string.Empty;
            CreateLabel(parent, hint, new Vector2(150f, y), 10, TextAlignmentOptions.Center, wrapWidth: 250f);
            y -= 32f;

            BindTextField(parent, ref y, "Param1", row.param1, v => row.param1 = v);
            BindTextField(parent, ref y, "Param2", row.param2, v => row.param2 = v);
            BindTextField(parent, ref y, "Param3", row.param3, v => row.param3 = v);
            BindTextField(parent, ref y, "Description", row.description, v => row.description = v);

            CreateButton(parent, "Remove Objective", new Vector2(150f, y), new Vector2(240f, 22f), () =>
            {
                _objectiveRows.Remove(row);
                RefreshObjectivesSection();
            });
            y -= 32f;
        }

        private void BuildTriggerRowUi(Transform parent, TriggerRow row, ref float y)
        {
            CreateButton(parent, "Kind: " + row.kind, new Vector2(150f, y), new Vector2(260f, 24f), () =>
            {
                row.kind = NextEnum(row.kind);
                RefreshObjectivesSection();
            });
            y -= 26f;

            string hint = TriggerHints.TryGetValue(row.kind, out string h) ? h : string.Empty;
            CreateLabel(parent, hint, new Vector2(150f, y), 10, TextAlignmentOptions.Center, wrapWidth: 250f);
            y -= 32f;

            BindTextField(parent, ref y, "Param1", row.param1, v => row.param1 = v);
            BindTextField(parent, ref y, "Param2", row.param2, v => row.param2 = v);
            BindTextField(parent, ref y, "Param3", row.param3, v => row.param3 = v);
            BindTextField(parent, ref y, "Param4", row.param4, v => row.param4 = v);
            BindTextField(parent, ref y, "Param5 (RepeatingGrantResource only)", row.param5, v => row.param5 = v);

            CreateButton(parent, "Remove Trigger", new Vector2(150f, y), new Vector2(240f, 22f), () =>
            {
                _triggerRows.Remove(row);
                RefreshObjectivesSection();
            });
            y -= 32f;
        }

        private void BindTextField(Transform parent, ref float y, string placeholder, string initialValue, Action<string> onChanged)
        {
            TMP_InputField field = CreateInputField(parent, new Vector2(150f, y), placeholder, height: 26f);
            field.text = initialValue ?? string.Empty;
            field.onValueChanged.AddListener(v => onChanged(v));
            y -= 30f;
        }

        private static T NextEnum<T>(T current) where T : struct, Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            int index = Array.IndexOf(values, current);
            return values[(index + 1) % values.Length];
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
        // (checked directly) - a new, standard, low-risk TMP component,
        // originally added for the scenario's save name. Heavy path
        // session 2 reuses it for every objective/trigger Param/
        // Description/Victory/Defeat field too, hence the placeholder/size
        // parameters (both defaulted to the original name-field values so
        // the one pre-existing call site is unaffected).
        private static TMP_InputField CreateInputField(Transform parent, Vector2 position, string placeholder = "Scenario name...", float width = 260f, float height = 30f)
        {
            var go = new GameObject("InputField_" + placeholder);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
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
            placeholderTmp.text = placeholder;
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

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

        // Heavy path session 6 ("per-kind bespoke input widgets"): every
        // objective/trigger param is actually drawn from a small, fixed,
        // already-known vocabulary, not free text - a per-Kind field-spec
        // table drives which widget renders for each param slot (index 0 =
        // param1, index 1 = param2, ...), replacing the old generic
        // "Param1"/"Param2" text-field + hint-label pair session 2 shipped.
        // Numeric/free-text slots (seconds, counts, amounts, the "x,y,z"
        // position string, Description) still use a plain TMP_InputField -
        // they aren't a fixed-vocabulary problem the enum-valued slots are.
        private enum ParamFieldKind { Text, Faction, Resource, BuildingType, DestroyTargetBuildingType }

        private readonly struct ParamFieldSpec
        {
            public readonly string Label;
            public readonly ParamFieldKind Kind;
            public ParamFieldSpec(string label, ParamFieldKind kind) { Label = label; Kind = kind; }
        }

        // "" is always the first Faction option - every optional-Faction
        // param falls back to Player when blank (BuildObjectivesFromRows/
        // BuildTriggersFromRows's own string.IsNullOrEmpty(...) ? "Player"
        // : ... convention), so blank reads as "(default: Player)" rather
        // than requiring every row to explicitly spell out "Player".
        private static readonly string[] FactionOptions = { "", "Player", "Enemy", "Enemy2" };
        private static readonly string[] ResourceOptions = { "Food", "Wood", "Gold", "Stone" };
        // Deliberately narrower than EntitySpawner.BuildingTypes - matches
        // MissionCsvLoader.SpawnScriptedTarget's own real supported switch
        // exactly, so this widget can never offer a value that would
        // silently no-op (with only a console warning) at play time.
        private static readonly string[] DestroyTargetBuildingOptions = { "Barracks", "TownCenter" };

        private static readonly Dictionary<ObjectiveKind, ParamFieldSpec[]> ObjectiveFieldSpecs = new Dictionary<ObjectiveKind, ParamFieldSpec[]>
        {
            { ObjectiveKind.SurviveSeconds, new[] {
                new ParamFieldSpec("Seconds", ParamFieldKind.Text) } },
            { ObjectiveKind.BuildingCountThreshold, new[] {
                new ParamFieldSpec("Building Type", ParamFieldKind.BuildingType),
                new ParamFieldSpec("Count", ParamFieldKind.Text),
                new ParamFieldSpec("Faction (optional)", ParamFieldKind.Faction) } },
            { ObjectiveKind.ResourceThreshold, new[] {
                new ParamFieldSpec("Resource", ParamFieldKind.Resource),
                new ParamFieldSpec("Amount", ParamFieldKind.Text),
                new ParamFieldSpec("Faction (optional)", ParamFieldKind.Faction) } },
            { ObjectiveKind.PopulationThreshold, new[] {
                new ParamFieldSpec("Population", ParamFieldKind.Text),
                new ParamFieldSpec("Faction (optional)", ParamFieldKind.Faction) } },
            { ObjectiveKind.DestroyScriptedTarget, new[] {
                new ParamFieldSpec("Target Building", ParamFieldKind.DestroyTargetBuildingType),
                new ParamFieldSpec("Target Faction", ParamFieldKind.Faction),
                new ParamFieldSpec("Position (x,y,z)", ParamFieldKind.Text) } },
        };

        private static readonly Dictionary<TriggerKind, ParamFieldSpec[]> TriggerFieldSpecs = new Dictionary<TriggerKind, ParamFieldSpec[]>
        {
            { TriggerKind.GrantResourceAtTime, new[] {
                new ParamFieldSpec("Resource", ParamFieldKind.Resource),
                new ParamFieldSpec("Amount", ParamFieldKind.Text),
                new ParamFieldSpec("At Seconds", ParamFieldKind.Text),
                new ParamFieldSpec("Faction (optional)", ParamFieldKind.Faction) } },
            { TriggerKind.RepeatingGrantResource, new[] {
                new ParamFieldSpec("Resource", ParamFieldKind.Resource),
                new ParamFieldSpec("Amount", ParamFieldKind.Text),
                new ParamFieldSpec("Interval Seconds", ParamFieldKind.Text),
                new ParamFieldSpec("Repeat Count", ParamFieldKind.Text),
                new ParamFieldSpec("Faction (optional)", ParamFieldKind.Faction) } },
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

        private void Save()
        {
            string name = _nameField.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Enter a scenario name before saving.");
                return;
            }

            CustomScenarioData data = BuildScenarioData(name);

            Directory.CreateDirectory(SavedScenarioLibrary.ScenarioFolder);
            string path = Path.Combine(SavedScenarioLibrary.ScenarioFolder, name + ".json");
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
            CustomScenarioData data = SavedScenarioLibrary.Load(name);
            if (data == null)
            {
                SetStatus("Not found: " + name);
                return;
            }

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
            CreateLabel(_objectivesScrollContent, "Objectives", new Vector2(150f, y), 16, TextAlignmentOptions.Center, anchorTop: true);
            y -= 24f;

            foreach (ObjectiveRow row in _objectiveRows)
            {
                BuildObjectiveRowUi(_objectivesScrollContent, row, ref y);
            }

            CreateButton(_objectivesScrollContent, "+ Add Objective", new Vector2(150f, y), new Vector2(240f, 26f), () =>
            {
                _objectiveRows.Add(new ObjectiveRow { kind = ObjectiveKind.SurviveSeconds, param1 = string.Empty, param2 = string.Empty, param3 = string.Empty, description = string.Empty });
                RefreshObjectivesSection();
            }, anchorTop: true);
            y -= 36f;

            CreateLabel(_objectivesScrollContent, "Triggers", new Vector2(150f, y), 16, TextAlignmentOptions.Center, anchorTop: true);
            y -= 24f;

            foreach (TriggerRow row in _triggerRows)
            {
                BuildTriggerRowUi(_objectivesScrollContent, row, ref y);
            }

            CreateButton(_objectivesScrollContent, "+ Add Trigger", new Vector2(150f, y), new Vector2(240f, 26f), () =>
            {
                _triggerRows.Add(new TriggerRow { kind = TriggerKind.GrantResourceAtTime, param1 = string.Empty, param2 = string.Empty, param3 = string.Empty, param4 = string.Empty, param5 = string.Empty });
                RefreshObjectivesSection();
            }, anchorTop: true);
            y -= 36f;

            CreateLabel(_objectivesScrollContent, "Victory Text (optional)", new Vector2(150f, y), 12, TextAlignmentOptions.Center, anchorTop: true);
            y -= 20f;
            _victoryTextField = CreateInputField(_objectivesScrollContent, new Vector2(150f, y), "Victory message...", height: 26f, anchorTop: true);
            _victoryTextField.text = _victoryText;
            _victoryTextField.onValueChanged.AddListener(v => _victoryText = v);
            y -= 32f;

            CreateLabel(_objectivesScrollContent, "Defeat Text (optional)", new Vector2(150f, y), 12, TextAlignmentOptions.Center, anchorTop: true);
            y -= 20f;
            _defeatTextField = CreateInputField(_objectivesScrollContent, new Vector2(150f, y), "Defeat message...", height: 26f, anchorTop: true);
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
            }, anchorTop: true);
            y -= 26f;

            ParamFieldSpec[] specs = ObjectiveFieldSpecs[row.kind];
            string[] values = { row.param1, row.param2, row.param3 };
            Action<string>[] setters = { v => row.param1 = v, v => row.param2 = v, v => row.param3 = v };
            for (int i = 0; i < specs.Length; i++)
            {
                BindParamField(parent, ref y, specs[i], values[i], setters[i]);
            }

            BindTextField(parent, ref y, "Description", row.description, v => row.description = v);

            CreateButton(parent, "Remove Objective", new Vector2(150f, y), new Vector2(240f, 22f), () =>
            {
                _objectiveRows.Remove(row);
                RefreshObjectivesSection();
            }, anchorTop: true);
            y -= 32f;
        }

        private void BuildTriggerRowUi(Transform parent, TriggerRow row, ref float y)
        {
            CreateButton(parent, "Kind: " + row.kind, new Vector2(150f, y), new Vector2(260f, 24f), () =>
            {
                row.kind = NextEnum(row.kind);
                RefreshObjectivesSection();
            }, anchorTop: true);
            y -= 26f;

            ParamFieldSpec[] specs = TriggerFieldSpecs[row.kind];
            string[] values = { row.param1, row.param2, row.param3, row.param4, row.param5 };
            Action<string>[] setters = { v => row.param1 = v, v => row.param2 = v, v => row.param3 = v, v => row.param4 = v, v => row.param5 = v };
            for (int i = 0; i < specs.Length; i++)
            {
                BindParamField(parent, ref y, specs[i], values[i], setters[i]);
            }

            CreateButton(parent, "Remove Trigger", new Vector2(150f, y), new Vector2(240f, 22f), () =>
            {
                _triggerRows.Remove(row);
                RefreshObjectivesSection();
            }, anchorTop: true);
            y -= 32f;
        }

        private void BindParamField(Transform parent, ref float y, ParamFieldSpec spec, string initialValue, Action<string> onChanged)
        {
            switch (spec.Kind)
            {
                case ParamFieldKind.Faction:
                    BindEnumCycleField(parent, ref y, spec.Label, FactionOptions, initialValue, onChanged);
                    break;
                case ParamFieldKind.Resource:
                    BindEnumCycleField(parent, ref y, spec.Label, ResourceOptions, initialValue, onChanged);
                    break;
                case ParamFieldKind.BuildingType:
                    BindEnumCycleField(parent, ref y, spec.Label, EntitySpawner.BuildingTypes, initialValue, onChanged);
                    break;
                case ParamFieldKind.DestroyTargetBuildingType:
                    BindEnumCycleField(parent, ref y, spec.Label, DestroyTargetBuildingOptions, initialValue, onChanged);
                    break;
                default:
                    BindTextField(parent, ref y, spec.Label, initialValue, onChanged);
                    break;
            }
        }

        private void BindTextField(Transform parent, ref float y, string placeholder, string initialValue, Action<string> onChanged)
        {
            TMP_InputField field = CreateInputField(parent, new Vector2(150f, y), placeholder, height: 26f, anchorTop: true);
            field.text = initialValue ?? string.Empty;
            field.onValueChanged.AddListener(v => onChanged(v));
            y -= 30f;
        }

        // Cycle-on-click button for a param drawn from a small fixed
        // vocabulary (Faction/Resource/BuildingType/...) - same "click
        // cycles to the next value, full RefreshObjectivesSection() rebuild"
        // convention the Kind button itself already uses, rather than
        // mutating the button's own label text in place. A value that
        // doesn't match any option (a legacy hand-typed string from before
        // this session, or an unrecognized value) normalizes to options[0]
        // (blank for Faction, a real value for the others) rather than
        // erroring - the row is also written back immediately so a stale/
        // invalid stored value doesn't linger unseen.
        private void BindEnumCycleField(Transform parent, ref float y, string label, string[] options, string initialValue, Action<string> onChanged)
        {
            int index = Array.IndexOf(options, initialValue ?? string.Empty);
            if (index < 0)
            {
                index = 0;
            }
            string current = options[index];
            if (current != initialValue)
            {
                onChanged(current);
            }

            string displayValue = string.IsNullOrEmpty(current) ? "(default: Player)" : current;
            CreateButton(parent, label + ": " + displayValue, new Vector2(150f, y), new Vector2(260f, 24f), () =>
            {
                string next = options[(index + 1) % options.Length];
                onChanged(next);
                RefreshObjectivesSection();
            }, anchorTop: true);
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

            float y = 0f;
            foreach (string name in SavedScenarioLibrary.ListSavedScenarioNames())
            {
                CreateButton(_fileListContainer, name, new Vector2(0f, y), new Vector2(260f, 24f), () => LoadFile(name));
                y -= 26f;
            }
        }

        // Heavy path session 4 ("richer palette art"): every EntitySpawner
        // type name that has a matching command-card icon already wired
        // elsewhere (BuildMenu.cs's own build_*/train_* icons under
        // Resources/UI/Icons/). TownCenter deliberately has no entry - no
        // build_towncenter.png exists anywhere in the project (confirmed
        // by directory listing, not assumed) since TownCenter is normally
        // auto-spawned rather than player-built through a menu elsewhere -
        // it stays text-only in the palette, the same disclosed fallback
        // BuildMenu.cs itself already uses for Dock/LumberCamp/MiningCamp/
        // Mill ("no matching icon asset yet, stay text-only").
        private static readonly Dictionary<string, string> PaletteIconNames = new Dictionary<string, string>
        {
            { "Barracks", "build_barracks" },
            { "Farm", "build_farm" },
            { "House", "build_house" },
            { "Wall", "build_wall" },
            { "Gate", "build_gate" },
            { "Tower", "build_tower" },
            { "Market", "build_market" },
            { "Worker", "train_worker" },
            { "Soldier", "train_soldier" },
            { "Archer", "train_archer" },
            { "Cavalry", "train_cavalry" },
            { "Siege", "train_siege" },
        };

        private void CreatePaletteButton(Transform parent, string type, bool isBuilding, ref float y)
        {
            TMP_Text label = CreateButton(parent, type, new Vector2(150f, y), new Vector2(260f, 24f), () =>
            {
                _selectedType = type;
                _selectedIsBuilding = isBuilding;
                SetStatus("Placing: " + type + " (" + _selectedFaction + ")");
            });
            label.fontSize = 13;

            if (PaletteIconNames.TryGetValue(type, out string iconName))
            {
                AddPaletteIcon(label, iconName);
            }

            y -= 26f;
        }

        // Adapted from BuildMenu.AddCommandIcon's own "icon + inset label"
        // shape, retuned for this file's smaller 260x24 palette rows
        // (BuildMenu's own command cards are 204x28) rather than shared
        // directly - the two button geometries differ enough that reusing
        // one method would need extra size/offset parameters, not a clean
        // 1:1 call.
        private static void AddPaletteIcon(TMP_Text label, string iconName)
        {
            Sprite icon = Resources.Load<Sprite>("UI/Icons/" + iconName);
            if (icon == null)
            {
                return;
            }

            Transform button = label.transform.parent;
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(16f, 16f);
            iconRect.anchoredPosition = new Vector2(4f, 0f);
            iconGo.GetComponent<Image>().sprite = icon;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(22f, labelRect.offsetMin.y);
        }

        // anchorTop: true anchors this element to its parent's fixed TOP
        // edge (0.5, 1) instead of the default (0.5, 0.5) center. Needed
        // for any child of a RectTransform whose own sizeDelta.y grows
        // over time (e.g. _objectivesScrollContent, resized every
        // RefreshObjectivesSection() so ScrollRect knows its scroll
        // extent) - a center anchor point drifts downward as that parent
        // grows, silently pulling every "position.y" (which assumes a
        // fixed top-origin, matching the accumulating `y` variable callers
        // use) further from where it's supposed to land, eventually past
        // the scroll viewport's clip rect entirely. See the RectMask2D
        // over-culling bug this was written to fix (heavy path session 6
        // follow-up, flagged via spawn_task task_545a0590). Fixed-size
        // parents (every other call site) are unaffected either way, since
        // a fixed-size rect's center and top are both constant points.
        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment, float wrapWidth = 260f, bool anchorTop = false)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorTop ? 1f : 0.5f);
            rect.anchorMax = new Vector2(0.5f, anchorTop ? 1f : 0.5f);
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

        private static TMP_Text CreateButton(Transform parent, string text, Vector2 position, Vector2 size, System.Action onClick, bool anchorTop = false)
        {
            var go = new GameObject("Button_" + text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorTop ? 1f : 0.5f);
            rect.anchorMax = new Vector2(0.5f, anchorTop ? 1f : 0.5f);
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
        private static TMP_InputField CreateInputField(Transform parent, Vector2 position, string placeholder = "Scenario name...", float width = 260f, float height = 30f, bool anchorTop = false)
        {
            var go = new GameObject("InputField_" + placeholder);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorTop ? 1f : 0.5f);
            rect.anchorMax = new Vector2(0.5f, anchorTop ? 1f : 0.5f);
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

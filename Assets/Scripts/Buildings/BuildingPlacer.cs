using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Placement flow for the Player's constructible buildings (Barracks,
    // Farm): triggered by the B/F hotkeys or BuildMenu's buttons, both
    // funnel through BeginPlacementBarracks()/BeginPlacementFarm(). Move
    // the mouse to preview it (green if affordable and clear, red
    // otherwise), left-click to confirm, right-click/Escape to cancel.
    // Always places for FactionId.Player - it's an inherently
    // player-driven tool, not a spawned/faction-tagged entity itself, so
    // it hardcodes that rather than trying to derive it. Actual
    // GameObject creation is BarracksFactory's/FarmFactory's job (shared
    // with AiController's programmatic placement, for Barracks).
    public class BuildingPlacer : MonoBehaviour
    {
        private enum BuildingKind { Barracks, Farm }

        [Header("Barracks")]
        [SerializeField] private KeyCode placeBarracksKey = KeyCode.B;
        [SerializeField] private float barracksWoodCost = 100f;
        [SerializeField] private float barracksStoneCost = 50f;
        [SerializeField] private float barracksBuildTime = 8f;
        [SerializeField] private Vector3 barracksSize = new Vector3(3f, 2f, 3f);

        [Header("Farm")]
        [SerializeField] private KeyCode placeFarmKey = KeyCode.F;
        [SerializeField] private float farmWoodCost = 60f;
        [SerializeField] private float farmBuildTime = 5f;
        [SerializeField] private Vector3 farmSize = new Vector3(2f, 0.6f, 2f);

        [SerializeField] private float minClearance = 3f;

        // SelectionManager checks this so a click meant to place/cancel a
        // building doesn't also register as a select/move/gather command.
        public static bool IsPlacing { get; private set; }

        private UnityEngine.Camera _camera;
        private GameObject _ghost;
        private bool _placing;
        private BuildingKind _kind;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
        }

        public void BeginPlacementBarracks()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Barracks);
            }
        }

        public void BeginPlacementFarm()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Farm);
            }
        }

        private void Update()
        {
            if (!_placing)
            {
                if (Input.GetKeyDown(placeBarracksKey))
                {
                    BeginPlacementBarracks();
                }
                else if (Input.GetKeyDown(placeFarmKey))
                {
                    BeginPlacementFarm();
                }
            }

            if (!_placing)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacing();
                return;
            }

            UpdateGhost();

            if (Input.GetMouseButtonDown(0))
            {
                TryConfirmPlacement();
            }
        }

        private void StartPlacing(BuildingKind kind)
        {
            _kind = kind;
            _placing = true;
            IsPlacing = true;
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "PlacementGhost";
            _ghost.transform.localScale = CurrentSize();
            Destroy(_ghost.GetComponent<Collider>());

            var renderer = _ghost.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader());
        }

        private void CancelPlacing()
        {
            _placing = false;
            IsPlacing = false;
            Destroy(_ghost);
        }

        private void UpdateGhost()
        {
            if (!TryGetGroundPoint(out Vector3 point))
            {
                return;
            }

            Vector3 size = CurrentSize();
            _ghost.transform.position = point + Vector3.up * (size.y * 0.5f);

            bool affordable = CanAfford();
            bool clear = BarracksFactory.IsClear(point, minClearance);
            var renderer = _ghost.GetComponent<MeshRenderer>();
            renderer.sharedMaterial.color = affordable && clear
                ? new Color(0.3f, 1f, 0.3f, 0.5f)
                : new Color(1f, 0.3f, 0.3f, 0.5f);
        }

        private void TryConfirmPlacement()
        {
            if (!TryGetGroundPoint(out Vector3 point) || !BarracksFactory.IsClear(point, minClearance))
            {
                return;
            }

            if (!CanAfford())
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);

            if (_kind == BuildingKind.Barracks)
            {
                stockpile.Add(ResourceType.Wood, -barracksWoodCost);
                stockpile.Add(ResourceType.Stone, -barracksStoneCost);
                BarracksFactory.Place(point, FactionId.Player, barracksBuildTime);
            }
            else
            {
                stockpile.Add(ResourceType.Wood, -farmWoodCost);
                FarmFactory.Place(point, FactionId.Player, farmBuildTime);
            }

            CancelPlacing();
        }

        private bool CanAfford()
        {
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);

            if (_kind == BuildingKind.Barracks)
            {
                return stockpile.GetTotal(ResourceType.Wood) >= barracksWoodCost
                    && stockpile.GetTotal(ResourceType.Stone) >= barracksStoneCost;
            }

            return stockpile.GetTotal(ResourceType.Wood) >= farmWoodCost;
        }

        private Vector3 CurrentSize()
        {
            return _kind == BuildingKind.Barracks ? barracksSize : farmSize;
        }

        private bool TryGetGroundPoint(out Vector3 point)
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                point = hit.point;
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}

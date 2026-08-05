using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Placement flow for the Player's Barracks foundation: triggered by the
    // B hotkey or BuildMenu's Build Barracks button (UI, milestone 7), both
    // funnel through BeginPlacement(). Move the mouse to preview it (green
    // if affordable and clear, red otherwise), left-click to confirm,
    // right-click/Escape to cancel. Always places for FactionId.Player -
    // it's an inherently player-driven tool, not a spawned/faction-tagged
    // entity itself, so it hardcodes that rather than trying to derive it.
    // Actual GameObject creation is BarracksFactory's job (shared with
    // AiController's programmatic placement).
    public class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private KeyCode placeBarracksKey = KeyCode.B;
        [SerializeField] private float barracksWoodCost = 100f;
        [SerializeField] private float barracksStoneCost = 50f;
        [SerializeField] private float barracksBuildTime = 8f;
        [SerializeField] private Vector3 barracksSize = new Vector3(3f, 2f, 3f);
        [SerializeField] private float minClearance = 3f;

        // SelectionManager checks this so a click meant to place/cancel a
        // building doesn't also register as a select/move/gather command.
        public static bool IsPlacing { get; private set; }

        private UnityEngine.Camera _camera;
        private GameObject _ghost;
        private bool _placing;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
        }

        public void BeginPlacement()
        {
            if (!_placing)
            {
                StartPlacing();
            }
        }

        private void Update()
        {
            if (!_placing && Input.GetKeyDown(placeBarracksKey))
            {
                BeginPlacement();
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

        private void StartPlacing()
        {
            _placing = true;
            IsPlacing = true;
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "BarracksGhost";
            _ghost.transform.localScale = barracksSize;
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

            _ghost.transform.position = point + Vector3.up * (barracksSize.y * 0.5f);

            bool affordable = CanAffordBarracks();
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

            if (!CanAffordBarracks())
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);
            stockpile.Add(ResourceType.Wood, -barracksWoodCost);
            stockpile.Add(ResourceType.Stone, -barracksStoneCost);
            BarracksFactory.Place(point, FactionId.Player, barracksBuildTime);
            CancelPlacing();
        }

        private bool CanAffordBarracks()
        {
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);
            return stockpile.GetTotal(ResourceType.Wood) >= barracksWoodCost
                && stockpile.GetTotal(ResourceType.Stone) >= barracksStoneCost;
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

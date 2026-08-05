using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Buildings
{
    // Placement flow for a Barracks foundation: triggered by the B hotkey
    // or BuildMenu's Build Barracks button (UI, milestone 7), both funnel
    // through BeginPlacement(). Move the mouse to preview it (green if
    // affordable and clear, red otherwise), left-click to confirm,
    // right-click/Escape to cancel.
    public class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private KeyCode placeBarracksKey = KeyCode.B;
        [SerializeField] private float barracksWoodCost = 100f;
        [SerializeField] private float barracksStoneCost = 50f;
        [SerializeField] private float barracksBuildTime = 8f;
        [SerializeField] private Vector3 barracksSize = new Vector3(3f, 2f, 3f);
        [SerializeField] private Color barracksColor = new Color(0.5f, 0.3f, 0.2f);
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
            bool clear = IsClear(point);
            var renderer = _ghost.GetComponent<MeshRenderer>();
            renderer.sharedMaterial.color = affordable && clear
                ? new Color(0.3f, 1f, 0.3f, 0.5f)
                : new Color(1f, 0.3f, 0.3f, 0.5f);
        }

        private void TryConfirmPlacement()
        {
            if (!TryGetGroundPoint(out Vector3 point) || !IsClear(point))
            {
                return;
            }

            if (!CanAffordBarracks())
            {
                return;
            }

            ResourceStockpile.Instance.Add(ResourceType.Wood, -barracksWoodCost);
            ResourceStockpile.Instance.Add(ResourceType.Stone, -barracksStoneCost);
            PlaceBarracks(point);
            CancelPlacing();
        }

        private bool CanAffordBarracks()
        {
            return ResourceStockpile.Instance.GetTotal(ResourceType.Wood) >= barracksWoodCost
                && ResourceStockpile.Instance.GetTotal(ResourceType.Stone) >= barracksStoneCost;
        }

        private void PlaceBarracks(Vector3 point)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Barracks";
            go.transform.position = point + Vector3.up * (barracksSize.y * 0.5f);
            go.transform.localScale = barracksSize;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader()) { color = barracksColor };

            go.AddComponent<Barracks>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(barracksBuildTime);
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            go.AddComponent<VisionSource>().Configure(10f);
        }

        private bool IsClear(Vector3 point)
        {
            foreach (Building building in Building.All)
            {
                if (Vector3.Distance(building.transform.position, point) < minClearance)
                {
                    return false;
                }
            }

            return true;
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

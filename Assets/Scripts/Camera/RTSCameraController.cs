using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Camera
{
    // Drives an RTS-style camera rig: WASD/arrow pan, screen-edge pan, and
    // scroll-wheel zoom (dolly along the camera's own height). Movement is
    // applied in world space so panning stays level regardless of the
    // camera's downward tilt. Input accumulates into a target position,
    // then the actual transform eases toward it (SmoothDamp) each frame -
    // gives pan/zoom a bit of weight/deceleration instead of snapping
    // instantly, and lets MinimapController.JumpTo() fly the camera to a
    // clicked point the same eased way rather than teleporting.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private bool edgeScrollEnabled = true;
        [SerializeField] private float edgeScrollBorder = 12f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 400f;
        [SerializeField] private float minHeight = 8f;
        [SerializeField] private float maxHeight = 35f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.12f;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 mapMin = new Vector2(-20f, -20f);
        [SerializeField] private Vector2 mapMax = new Vector2(20f, 20f);

        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private bool _wasMatchStarted;

        private void Start()
        {
            _targetPosition = transform.position;
        }

        // Called by MinimapController when the player clicks/drags on the
        // minimap - re-centers the target XZ, keeping current zoom height,
        // and lets the existing SmoothDamp ease the camera there.
        public void JumpTo(float worldX, float worldZ)
        {
            _targetPosition.x = worldX;
            _targetPosition.z = worldZ;
        }

        // Phase 5 map-awareness fix: mapMin/mapMax were previously fixed at
        // half of RiverValley's 40-unit ground regardless of which map was
        // actually picked - on Highlands (52)/Coastal (50) the pan clamp
        // was tighter than the real ground, making the true map edges
        // unreachable by camera. This component is always-active from
        // scene load (not gated like NavMeshBaker), so the fix can't just
        // be an Awake()-time MapRegistry.Current read the way NavMeshBaker
        // does it - the player hasn't picked a map through CivPicker at
        // that point yet. Corrected once, on the same HasMatchStarted
        // false->true transition FogOfWarManager/MinimapController/
        // SimClock all key off of.
        private void Update()
        {
            if (CivilizationSetup.HasMatchStarted)
            {
                if (!_wasMatchStarted)
                {
                    _wasMatchStarted = true;
                    float half = MapRegistry.Current.GroundSize * 0.5f;
                    mapMin = new Vector2(-half, -half);
                    mapMax = new Vector2(half, half);
                }
            }
            else
            {
                _wasMatchStarted = false;
            }

            Vector3 move = GetKeyboardInput() + GetEdgeScrollInput();
            _targetPosition += move * panSpeed * Time.deltaTime;

            HandleZoom();
            ClampTargetPosition();

            transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, positionSmoothTime);
        }

        private static Vector3 GetKeyboardInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            return new Vector3(x, 0f, z);
        }

        private Vector3 GetEdgeScrollInput()
        {
            if (!edgeScrollEnabled)
            {
                return Vector3.zero;
            }

            Vector3 mousePos = Input.mousePosition;
            Vector3 move = Vector3.zero;

            if (mousePos.x <= edgeScrollBorder)
            {
                move.x -= 1f;
            }
            else if (mousePos.x >= Screen.width - edgeScrollBorder)
            {
                move.x += 1f;
            }

            if (mousePos.y <= edgeScrollBorder)
            {
                move.z -= 1f;
            }
            else if (mousePos.y >= Screen.height - edgeScrollBorder)
            {
                move.z += 1f;
            }

            return move;
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            _targetPosition.y = Mathf.Clamp(_targetPosition.y - scroll * zoomSpeed * Time.deltaTime, minHeight, maxHeight);
        }

        private void ClampTargetPosition()
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, mapMin.x, mapMax.x);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, mapMin.y, mapMax.y);
        }
    }
}

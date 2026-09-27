using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Camera
{
    // Drives an RTS-style camera rig: WASD/arrow pan, screen-edge pan, and
    // scroll-wheel zoom (Camera.orthographicSize when the camera is
    // Orthographic - this project's own AoE-style fixed isometric rig; a
    // height dolly otherwise, for a Perspective camera). Movement is
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
        // Phase 5 map-scale-up: bumped from 20 alongside the x2.5 map size
        // increase (not a full x2.5 - panSpeed is a UX/feel number, not a
        // distance being scaled 1:1, so this is a judgment call landing
        // partway between "unchanged" and "scales with the map") so
        // crossing a bigger map by keyboard/edge-scroll doesn't feel
        // glacial compared to before.
        [SerializeField] private float panSpeed = 32f;
        [SerializeField] private bool edgeScrollEnabled = true;
        [SerializeField] private float edgeScrollBorder = 12f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 400f;
        [SerializeField] private float minHeight = 8f;
        // Phase 5 map-scale-up: bumped from 35 so the player can actually
        // zoom out far enough to see a meaningful fraction of the now-
        // bigger maps - same "partial, not full x2.5" judgment call as
        // panSpeed above (a fully proportional 87.5 would be an extreme
        // top-down view).
        [SerializeField] private float maxHeight = 60f;

        // AoE-style fixed isometric camera: the scene's Main Camera is now
        // Orthographic (fixed 30 deg pitch/45 deg yaw, matching the classic
        // AoE II dimetric look, never rotated by this controller). Under an
        // orthographic projection, dollying the camera - moving it along
        // anything other than its own exact forward axis - PANS the framed
        // ground point instead of scaling it; the minHeight/maxHeight dolly
        // above only actually zooms a Perspective camera. Confirmed live:
        // moving this rig's height from 20 to 60 while orthographic didn't
        // shrink anything on screen, it panned the view off into
        // unrendered terrain. So scroll zoom instead drives
        // Camera.orthographicSize directly when orthographic is on -
        // height/minHeight/maxHeight stay untouched by zoom in that case
        // and the Perspective branch below is kept for backward
        // compatibility if the project ever switches back.
        [SerializeField] private float minOrthographicSize = 4f;
        [SerializeField] private float maxOrthographicSize = 20f;
        [SerializeField] private float orthoZoomSpeed = 100f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.12f;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 mapMin = new Vector2(-20f, -20f);
        [SerializeField] private Vector2 mapMax = new Vector2(20f, 20f);

        private UnityEngine.Camera _camera;
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private bool _wasMatchStarted;

        private void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
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

        // Repository-audit reproduction: at match start the camera sat
        // wherever the scene/CivPicker screen last left it - nowhere near
        // the local player's actual starting base - and with fog enabled
        // that reads as an almost entirely black Game view. Explicit
        // match-start camera-focus API: called once by
        // CivilizationSetup.BeginMatchCore right after the match's map/
        // faction configuration is known, so it always has a real ground
        // target to aim at.
        //
        // Deliberately NOT `transform.position = groundTarget` (what
        // JumpTo effectively does, and what the naive fix would do) - this
        // camera is permanently pitched downward (see the scene's own
        // Main Camera transform), so its own XZ position is not the same
        // point as where its forward ray actually meets the ground.
        // ComputeGroundFocusPosition solves for the camera position whose
        // forward ray - at the given height - lands exactly on
        // groundTarget, so groundTarget ends up centered in the viewport
        // instead of the camera's own footprint sitting near it.
        //
        // Snaps `transform.position` directly (bypassing the usual
        // SmoothDamp ease) rather than merely setting `_targetPosition`
        // and letting the next several frames ease toward it - an eased
        // multi-second pan from wherever the camera happened to be
        // (possibly far off-map) would still show the same black,
        // unexplored fog the bug report describes while it catches up.
        // `_velocity` is reset alongside it so no leftover pan/zoom
        // momentum causes the very next frame's SmoothDamp step to
        // overshoot away from the snapped position. Every other camera
        // behavior (WASD/edge-scroll pan, scroll zoom, minimap JumpTo,
        // and the SmoothDamp easing they all still use) is untouched.
        public void FocusOnMatchStart(Vector3 groundTarget)
        {
            float height = Mathf.Clamp(transform.position.y, minHeight, maxHeight);
            Vector3 focusPosition = ComputeGroundFocusPosition(groundTarget, height, transform.forward);

            transform.position = focusPosition;
            _targetPosition = focusPosition;
            _velocity = Vector3.zero;
        }

        // Pure ground-point math, deliberately free of any MonoBehaviour/
        // scene dependency so it's directly unit-testable: given the
        // camera's actual forward direction and a desired height, returns
        // the camera position whose forward ray intersects the
        // groundTarget's own elevation exactly at groundTarget's XZ.
        //
        // Derivation: starting from camera position P looking along unit
        // vector f, the ray P + t*f reaches groundTarget's elevation when
        // t = (height - groundTarget.y) / -f.y (solving P.y + t*f.y =
        // groundTarget.y with P.y = height). The hit point's XZ must equal
        // groundTarget's XZ, i.e. P.xz + t*f.xz = groundTarget.xz - so
        // P.xz = groundTarget.xz - t*f.xz. This holds for any yaw, not
        // just the project's current fixed-yaw rig, and reduces to
        // groundTarget.xz unchanged only in the degenerate case where the
        // camera isn't tilted at all (f.y == 0, guarded below since the
        // camera would never reach the ground and t would be undefined).
        internal static Vector3 ComputeGroundFocusPosition(Vector3 groundTarget, float height, Vector3 cameraForward)
        {
            if (Mathf.Approximately(cameraForward.y, 0f))
            {
                return new Vector3(groundTarget.x, height, groundTarget.z);
            }

            float t = (height - groundTarget.y) / -cameraForward.y;
            return new Vector3(
                groundTarget.x - t * cameraForward.x,
                height,
                groundTarget.z - t * cameraForward.z);
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

            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize - scroll * orthoZoomSpeed * Time.deltaTime,
                    minOrthographicSize, maxOrthographicSize);
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

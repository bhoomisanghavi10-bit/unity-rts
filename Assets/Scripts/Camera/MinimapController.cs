using UnityEngine;

namespace KingdomsOfBharat.Camera
{
    // A second, top-down orthographic camera rendered into a small
    // render-texture in the screen corner - literally the same scene the
    // main camera sees (including the fog-of-war quad), so fog/units/
    // buildings all show up on the minimap automatically with zero extra
    // compositing code, guaranteed to match FogOfWarManager's own visuals
    // since it's the same geometry. Click/drag on the minimap re-centers
    // the main camera there.
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private float mapSize = 44f;
        [SerializeField] private int textureSize = 256;
        [SerializeField] private float cameraHeight = 60f;
        [SerializeField] private Vector2 screenSize = new Vector2(220f, 220f);
        [SerializeField] private float screenMargin = 10f;

        // SelectionManager checks this so a click meant to jump the camera
        // via the minimap doesn't also register as a select/move command
        // on the game world underneath it - same pattern as
        // BuildingPlacer.IsPlacing.
        public static bool IsPointerOverMinimap { get; private set; }

        private RTSCameraController _mainCameraController;
        private RenderTexture _renderTexture;
        private Rect _screenRect;

        private void Awake()
        {
            _mainCameraController = UnityEngine.Camera.main.GetComponent<RTSCameraController>();

            _renderTexture = new RenderTexture(textureSize, textureSize, 16);

            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0f, cameraHeight, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var minimapCamera = camGo.AddComponent<UnityEngine.Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = mapSize * 0.5f;
            minimapCamera.targetTexture = _renderTexture;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
            minimapCamera.cullingMask = ~0;
            minimapCamera.nearClipPlane = 0.3f;
            minimapCamera.farClipPlane = cameraHeight + 20f;
            minimapCamera.depth = -10f;
        }

        private void OnGUI()
        {
            _screenRect = new Rect(
                Screen.width - screenSize.x - screenMargin,
                Screen.height - screenSize.y - screenMargin,
                screenSize.x, screenSize.y);

            GUI.DrawTexture(_screenRect, _renderTexture, ScaleMode.StretchToFill, false);

            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            IsPointerOverMinimap = _screenRect.Contains(mouse);

            if (!IsPointerOverMinimap || !Input.GetMouseButton(0))
            {
                return;
            }

            Vector2 local = mouse - new Vector2(_screenRect.x, _screenRect.y);
            float normX = local.x / _screenRect.width;
            float normZ = 1f - local.y / _screenRect.height;

            float half = mapSize * 0.5f;
            float worldX = Mathf.Lerp(-half, half, normX);
            float worldZ = Mathf.Lerp(-half, half, normZ);

            _mainCameraController.JumpTo(worldX, worldZ);
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }
        }
    }
}

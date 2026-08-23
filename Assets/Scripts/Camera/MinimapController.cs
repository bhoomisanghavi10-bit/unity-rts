using UnityEngine;
using UnityEngine.UI;

namespace KingdomsOfBharat.Camera
{
    // A second, top-down orthographic camera rendered into a small
    // render-texture in the screen corner - literally the same scene the
    // main camera sees (including the fog-of-war quad), so fog/units/
    // buildings all show up on the minimap automatically with zero extra
    // compositing code, guaranteed to match FogOfWarManager's own visuals
    // since it's the same geometry. Click/drag on the minimap re-centers
    // the main camera there. uGUI replacement for the original OnGUI
    // version - the render texture is displayed via a RawImage Canvas
    // child (wired up in the Inspector) instead of GUI.DrawTexture, and
    // input is tracked against that RawImage's RectTransform instead of a
    // manually computed screen Rect.
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private float mapSize = 44f;
        [SerializeField] private int textureSize = 256;
        [SerializeField] private float cameraHeight = 60f;
        [SerializeField] private RawImage display;

        // SelectionManager checks this so a click meant to jump the camera
        // via the minimap doesn't also register as a select/move command
        // on the game world underneath it - same pattern as
        // BuildingPlacer.IsPlacing.
        public static bool IsPointerOverMinimap { get; private set; }

        private RTSCameraController _mainCameraController;
        private RenderTexture _renderTexture;

        private void Awake()
        {
            _mainCameraController = UnityEngine.Camera.main.GetComponent<RTSCameraController>();

            _renderTexture = new RenderTexture(textureSize, textureSize, 16);
            display.texture = _renderTexture;

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

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            RectTransform rt = display.rectTransform;
            IsPointerOverMinimap = RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);

            if (!IsPointerOverMinimap || !Input.GetMouseButton(0))
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, null, out Vector2 local);

            float normX = (local.x - rt.rect.x) / rt.rect.width;
            float normZ = (local.y - rt.rect.y) / rt.rect.height;

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

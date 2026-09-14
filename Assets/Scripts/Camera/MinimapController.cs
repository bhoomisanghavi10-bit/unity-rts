using UnityEngine;
using UnityEngine.UI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.UI;

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
        private UnityEngine.Camera _minimapCamera;
        private bool _wasMatchStarted;

        private void Awake()
        {
            _mainCameraController = UnityEngine.Camera.main.GetComponent<RTSCameraController>();

            _renderTexture = new RenderTexture(textureSize, textureSize, 16);
            display.texture = _renderTexture;

            // Wave 6 item 34: snapshot this panel's own bottom-right
            // anchor/position/size BEFORE ApplyDiamondFrame() reparents and
            // re-stretches this same RectTransform to fill its new mask
            // parent - see IdleWorkerIndicator.Configure's own comment for
            // why reading `transform` after that call would be wrong.
            RectTransform selfRect = GetComponent<RectTransform>();
            Transform originalParent = selfRect.parent;
            Vector2 originalAnchorMin = selfRect.anchorMin;
            Vector2 originalAnchorMax = selfRect.anchorMax;
            Vector2 originalPivot = selfRect.pivot;
            Vector2 originalAnchoredPosition = selfRect.anchoredPosition;
            Vector2 originalSizeDelta = selfRect.sizeDelta;

            ApplyDiamondFrame();

            // Self-attached rather than scene-wired (see
            // IdleWorkerIndicator's own header comment) - positioned via
            // the snapshot above, directly above the minimap.
            gameObject.AddComponent<IdleWorkerIndicator>().Configure(
                originalParent, originalAnchorMin, originalAnchorMax, originalPivot, originalAnchoredPosition, originalSizeDelta);

            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0f, cameraHeight, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _minimapCamera = camGo.AddComponent<UnityEngine.Camera>();
            _minimapCamera.orthographic = true;
            // Placeholder using the Inspector default (RiverValley-sized) -
            // corrected in Update() once HasMatchStarted flips true. This
            // component is always-active from scene load (not gated like
            // NavMeshBaker/ResourceNodeSpawner), so mapSize can't be read
            // from MapRegistry.Current here yet - the player hasn't picked
            // a map through CivPicker at this point.
            _minimapCamera.orthographicSize = mapSize * 0.5f;
            _minimapCamera.targetTexture = _renderTexture;
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
            _minimapCamera.cullingMask = ~0;
            _minimapCamera.nearClipPlane = 0.3f;
            _minimapCamera.farClipPlane = cameraHeight + 20f;
            _minimapCamera.depth = -10f;
        }

        // Phase 5 map-awareness fix: mapSize was previously a fixed 44
        // (roughly RiverValley's 40-unit ground plus a small margin,
        // mirroring FogOfWarManager's own quadSize margin) regardless of
        // which map was actually picked - on Highlands (52)/Coastal (50)
        // the minimap camera's orthographicSize was smaller than the map's
        // real half-extent, cropping the true edges, and HandleInput's
        // click-to-jump math used the same wrong half, so a click near the
        // minimap's edge on those maps could jump the camera to the wrong
        // world position. Corrected once, on the same HasMatchStarted
        // false->true transition FogOfWarManager/SimClock key off of.
        private void Update()
        {
            if (CivilizationSetup.HasMatchStarted)
            {
                if (!_wasMatchStarted)
                {
                    _wasMatchStarted = true;
                    mapSize = MapRegistry.Current.GroundSize;
                    _minimapCamera.orthographicSize = mapSize * 0.5f;
                }
            }
            else
            {
                _wasMatchStarted = false;
            }

            HandleInput();
        }

        // 2026-09-12 ornate HUD reskin: `display`'s render-texture feed is a
        // plain square, but the sourced frame art (`docs/UI_ART_BRIEF.md`'s
        // "Ornate HUD reskin" spec) is a diamond viewport - clips `display`
        // to that diamond via a `Mask` (using the interior-opaque frame
        // sprite as the mask shape, the same sprite `Mask` reads alpha from
        // for any masked child) rather than reshaping the camera's own
        // render texture, then layers a second, interior-punched-out sprite
        // of the same art on top as the visible ornate border - two crops
        // of one source image, not two separate assets.
        private void ApplyDiamondFrame()
        {
            Sprite maskShape = Resources.Load<Sprite>("UI/Panels/panel_minimap_frame");
            Sprite borderOnly = Resources.Load<Sprite>("UI/Panels/panel_minimap_frame_border");
            if (maskShape == null || borderOnly == null)
            {
                return;
            }

            RectTransform displayRect = display.rectTransform;
            Transform parent = displayRect.parent;
            int siblingIndex = displayRect.GetSiblingIndex();

            GameObject maskGo = new GameObject("MinimapDiamondMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            RectTransform maskRect = maskGo.GetComponent<RectTransform>();
            maskRect.SetParent(parent, false);
            maskRect.anchorMin = displayRect.anchorMin;
            maskRect.anchorMax = displayRect.anchorMax;
            maskRect.pivot = displayRect.pivot;
            maskRect.anchoredPosition = displayRect.anchoredPosition;
            maskRect.sizeDelta = displayRect.sizeDelta;
            maskRect.SetSiblingIndex(siblingIndex);

            Image maskImage = maskGo.GetComponent<Image>();
            maskImage.sprite = maskShape;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;

            displayRect.SetParent(maskRect, false);
            displayRect.anchorMin = Vector2.zero;
            displayRect.anchorMax = Vector2.one;
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;

            GameObject borderGo = new GameObject("MinimapDiamondBorder", typeof(RectTransform), typeof(Image));
            RectTransform borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.SetParent(parent, false);
            borderRect.anchorMin = maskRect.anchorMin;
            borderRect.anchorMax = maskRect.anchorMax;
            borderRect.pivot = maskRect.pivot;
            borderRect.anchoredPosition = maskRect.anchoredPosition;
            borderRect.sizeDelta = maskRect.sizeDelta;
            borderRect.SetSiblingIndex(maskRect.GetSiblingIndex() + 1);
            borderGo.GetComponent<Image>().sprite = borderOnly;
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

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KingdomsOfBharat.Core
{
    // Inside the namespace so it wins over the KingdomsOfBharat.Camera namespace.
    using Camera = UnityEngine.Camera;

    // Real-time planar reflection for the water: a second camera renders the
    // scene mirrored about the water plane (oblique near-clip plane so nothing
    // below the surface leaks in) into a reduced-resolution RenderTexture,
    // which KobWater samples by screen position (bent by the ripples). The
    // texture's alpha is 0 wherever the reflection camera saw only sky, so the
    // shader falls back to its analytic sky gradient there. Cost control: it
    // only renders when the main camera can see the water, at a fraction of
    // screen resolution, with shadows, post-processing and SSAO off (the
    // camera is typed Reflection, which URP exempts from both) and a short
    // far clip. Lives on the Water object, so it is rebuilt/destroyed with it.
    [RequireComponent(typeof(MeshRenderer))]
    public class PlanarReflection : MonoBehaviour
    {
        // Master switch (e.g. for a low-quality tier or a settings toggle).
        public static bool Enabled = true;
        public static float ResolutionScale = 0.4f;
        public static float FarClip = 60f;

        private static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");
        private static readonly int PlanarStrengthId = Shader.PropertyToID("_PlanarStrength");

        private MeshRenderer _water;
        private Camera _reflectionCamera;
        private RenderTexture _texture;
        private readonly Plane[] _planes = new Plane[6];
        private bool _rendering;

        public bool HasTexture => _texture != null;
        public int RenderedFrames { get; private set; }

        private void Awake()
        {
            _water = GetComponent<MeshRenderer>();
        }

        private void OnDestroy()
        {
            Release();
        }

        private void OnDisable()
        {
            SetStrength(0f);
        }

        // Pure math (unit-testable): the 4x4 matrix reflecting a point across
        // the plane (nx, ny, nz, d) where n.p + d = 0.
        public static Matrix4x4 ReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 m = default;
            m.m00 = 1f - 2f * plane.x * plane.x;
            m.m01 = -2f * plane.x * plane.y;
            m.m02 = -2f * plane.x * plane.z;
            m.m03 = -2f * plane.w * plane.x;
            m.m10 = -2f * plane.y * plane.x;
            m.m11 = 1f - 2f * plane.y * plane.y;
            m.m12 = -2f * plane.y * plane.z;
            m.m13 = -2f * plane.w * plane.y;
            m.m20 = -2f * plane.z * plane.x;
            m.m21 = -2f * plane.z * plane.y;
            m.m22 = 1f - 2f * plane.z * plane.z;
            m.m23 = -2f * plane.w * plane.z;
            m.m30 = 0f;
            m.m31 = 0f;
            m.m32 = 0f;
            m.m33 = 1f;
            return m;
        }

        private void LateUpdate()
        {
            Camera main = Camera.main;
            if (!Enabled || _rendering || main == null || _water == null || !_water.enabled || GraphicsSettings.currentRenderPipeline == null)
            {
                SetStrength(0f);
                return;
            }

            // Skip when the water isn't in view.
            GeometryUtility.CalculateFrustumPlanes(main, _planes);
            if (!GeometryUtility.TestPlanesAABB(_planes, _water.bounds))
            {
                return;
            }

            EnsureResources(main);
            if (_reflectionCamera == null || _texture == null)
            {
                return;
            }

            _rendering = true;
            bool fog = RenderSettings.fog;
            try
            {
                float y = transform.position.y;
                ConfigureCamera(main, y);

                // The water quad must not reflect itself.
                _water.enabled = false;
                GL.invertCulling = true;

                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = _texture };
                if (RenderPipeline.SupportsRenderRequest(_reflectionCamera, request))
                {
                    RenderPipeline.SubmitRenderRequest(_reflectionCamera, request);
                    RenderedFrames++;
                }
            }
            finally
            {
                GL.invertCulling = false;
                _water.enabled = true;
                RenderSettings.fog = fog;
                _rendering = false;
            }

            _water.sharedMaterial.SetTexture(ReflectionTexId, _texture);
            SetStrength(1f);
        }

        private void ConfigureCamera(Camera main, float waterY)
        {
            Camera r = _reflectionCamera;
            r.CopyFrom(main);
            r.enabled = false;
            r.cameraType = CameraType.Reflection;
            r.clearFlags = CameraClearFlags.SolidColor;
            r.backgroundColor = new Color(0f, 0f, 0f, 0f); // alpha 0 = sky, shader falls back
            r.allowHDR = false;
            r.allowMSAA = false;
            r.farClipPlane = Mathf.Min(main.farClipPlane, FarClip);
            // Ground clutter (tufts, pebbles...) and the water itself aren't worth
            // reflecting - skipping them is most of the cost saving.
            int mask = main.cullingMask;
            int clutter = LayerMask.NameToLayer("Clutter");
            if (clutter >= 0)
            {
                mask &= ~(1 << clutter);
            }

            int water = LayerMask.NameToLayer("Water");
            if (water >= 0)
            {
                mask &= ~(1 << water);
            }

            r.cullingMask = mask;
            r.targetTexture = null;

            var data = r.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;

            Vector4 plane = new Vector4(0f, 1f, 0f, -waterY);
            Matrix4x4 reflection = ReflectionMatrix(plane);

            r.worldToCameraMatrix = main.worldToCameraMatrix * reflection;
            Vector3 p = main.transform.position;
            r.transform.position = new Vector3(p.x, 2f * waterY - p.y, p.z);
            Vector3 f = main.transform.forward;
            Vector3 u = main.transform.up;
            r.transform.rotation = Quaternion.LookRotation(new Vector3(f.x, -f.y, f.z), new Vector3(-u.x, u.y, -u.z));

            // Oblique near plane at (just above) the water surface.
            Vector3 planePoint = new Vector3(0f, waterY + 0.03f, 0f);
            Matrix4x4 view = r.worldToCameraMatrix;
            Vector3 cpos = view.MultiplyPoint(planePoint);
            Vector3 cnormal = view.MultiplyVector(Vector3.up).normalized;
            var clip = new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
            r.projectionMatrix = main.projectionMatrix;
            r.projectionMatrix = r.CalculateObliqueMatrix(clip);
        }

        private void EnsureResources(Camera main)
        {
            int w = Mathf.Max(64, Mathf.RoundToInt(main.pixelWidth * ResolutionScale));
            int h = Mathf.Max(64, Mathf.RoundToInt(main.pixelHeight * ResolutionScale));

            if (_texture == null || _texture.width != w || _texture.height != h)
            {
                if (_texture != null)
                {
                    _texture.Release();
                    Destroy(_texture);
                }

                _texture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "WaterPlanarReflection",
                    useMipMap = false,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                _texture.Create();
            }

            if (_reflectionCamera == null)
            {
                var go = new GameObject("WaterReflectionCamera") { hideFlags = HideFlags.HideAndDontSave };
                _reflectionCamera = go.AddComponent<Camera>();
                _reflectionCamera.enabled = false;
            }
        }

        private void SetStrength(float value)
        {
            if (_water != null && _water.sharedMaterial != null)
            {
                _water.sharedMaterial.SetFloat(PlanarStrengthId, value);
            }
        }

        private void Release()
        {
            if (_reflectionCamera != null)
            {
                Destroy(_reflectionCamera.gameObject);
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}

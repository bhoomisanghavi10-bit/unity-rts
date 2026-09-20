using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // A foam wake behind a boat: flat blobs left along its path that swell
    // and fade (a stern trail), plus a small splash at the bow. Emission is
    // driven by distance travelled (rateOverDistance), so a boat that stops
    // stops leaving foam and the trail simply fades out. Built entirely in
    // code with a runtime-generated soft-blob texture, like VfxFactory's
    // bursts (no hand-authored prefab). Particles live in world space at the
    // water surface (WaterProximity.SurfaceY) and are drawn after the water
    // (queue 3100) so the translucent water doesn't tint them away.
    // FogOfWarManager hides a unit by disabling every Renderer under it,
    // which includes these particle renderers - so an unseen enemy boat's
    // wake is hidden with it.
    [DisallowMultipleComponent]
    public class BoatWake : MonoBehaviour
    {
        private const float SurfaceLift = 0.03f;
        private const float DefaultLength = 3f;

        private static Texture2D _foamTexture;
        private static Material _material;

        private ParticleSystem _sternWake;
        private ParticleSystem _bowSplash;
        private float _halfLength = DefaultLength * 0.5f;

        // Pure helper (unit-testable): wake size multiplier for a hull length.
        public static float ScaleForLength(float hullLength)
        {
            return Mathf.Clamp(hullLength / DefaultLength, 0.6f, 2f);
        }

        private void Start()
        {
            float length = MeasureHullLength();
            _halfLength = length * 0.5f;
            float k = ScaleForLength(length);

            _sternWake = BuildSystem("Wake", k, sternTrail: true);
            _bowSplash = BuildSystem("BowSplash", k, sternTrail: false);
        }

        private void LateUpdate()
        {
            if (_sternWake == null)
            {
                return;
            }

            Vector3 p = transform.position;
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            float y = WaterProximity.SurfaceY + SurfaceLift;

            _sternWake.transform.position = new Vector3(p.x, y, p.z) - fwd * (_halfLength * 0.85f);
            _bowSplash.transform.position = new Vector3(p.x, y, p.z) + fwd * (_halfLength * 0.9f);
        }

        private float MeasureHullLength()
        {
            var bounds = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer)
                {
                    continue;
                }

                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return any ? Mathf.Max(bounds.size.x, bounds.size.z) : DefaultLength;
        }

        private ParticleSystem BuildSystem(string name, float k, bool sternTrail)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            // AddComponent auto-plays; stop before editing main (see VfxFactory).
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = sternTrail ? new ParticleSystem.MinMaxCurve(2.6f, 3.8f) : new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = sternTrail ? 0.3f : 0.5f;
            main.startSize = sternTrail
                ? new ParticleSystem.MinMaxCurve(0.6f * k, 1.0f * k)
                : new ParticleSystem.MinMaxCurve(0.25f * k, 0.45f * k);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 1f, 1f, sternTrail ? 0.5f : 0.75f);
            main.gravityModifier = 0f;
            main.maxParticles = sternTrail ? 500 : 100;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = sternTrail ? 7f / Mathf.Max(k, 0.5f) : 2.5f / Mathf.Max(k, 0.5f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = (sternTrail ? 0.35f : 0.1f) * k;
            shape.radiusThickness = 1f;
            // Lay the emission circle flat on the water so drift is horizontal.
            shape.rotation = new Vector3(90f, 0f, 0f);

            // Foam blobs swell as they age...
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, sternTrail ? 3.2f : 1.8f));

            // ...and fade in fast (no pop) then out slowly.
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.03f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.sharedMaterial = GetMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }

        private static Material GetMaterial()
        {
            if (_material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? GameplayMaterial.FindUnlitShader();
                _material = new Material(shader) { name = "BoatWakeFoam" };
                if (_material.HasProperty("_BaseMap"))
                {
                    _material.SetTexture("_BaseMap", GetFoamTexture());
                }

                GameplayMaterial.ForceTransparent(_material);
                // After the (transparent) water so it isn't tinted over.
                _material.renderQueue = 3100;
            }

            return _material;
        }

        // Soft, slightly noisy round blob.
        private static Texture2D GetFoamTexture()
        {
            if (_foamTexture == null)
            {
                const int n = 64;
                _foamTexture = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    name = "BoatWakeFoamBlob",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                var pixels = new Color32[n * n];
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f;
                        float dy = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - r);
                        a = a * a * (3f - 2f * a);
                        float noise = 0.65f + 0.35f * Mathf.PerlinNoise(x * 0.18f + 3f, y * 0.18f + 9f);
                        pixels[y * n + x] = new Color32(255, 255, 255, (byte)(255f * a * noise));
                    }
                }

                _foamTexture.SetPixels32(pixels);
                _foamTexture.Apply(false);
            }

            return _foamTexture;
        }
    }
}

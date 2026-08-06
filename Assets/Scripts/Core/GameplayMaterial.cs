using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Shared helper for every runtime-created gameplay material, so PBR
    // tuning and the URP opaque/transparent surface-type distinction (see
    // ForceTransparent) live in exactly one place instead of being
    // copy-pasted across every factory/spawner. Matte-leaning defaults
    // (low smoothness, zero metallic) replace the shader's own defaults,
    // which read as shiny plastic on flat-color primitives.
    public static class GameplayMaterial
    {
        public static Material CreateOpaque(Color color, float smoothness = 0.15f, float metallic = 0f)
        {
            var material = new Material(FindOpaqueShader()) { color = color };
            ApplyPbrDefaults(material, smoothness, metallic);
            return material;
        }

        public static Material CreateTransparent(Color color, float smoothness = 0.1f)
        {
            var material = new Material(FindOpaqueShader()) { color = color };
            ApplyPbrDefaults(material, smoothness, 0f);
            ForceTransparent(material);
            return material;
        }

        public static Shader FindOpaqueShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }

        // Separate from FindOpaqueShader: URP's Lit shader is meant for
        // shaded/lit opaque surfaces; an always-visible overlay like fog
        // needs something unlit instead.
        public static Shader FindUnlitShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Unlit/Color");
        }

        private static void ApplyPbrDefaults(Material material, float smoothness, float metallic)
        {
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }
        }

        // URP's Lit/Unlit shaders default to an OPAQUE surface from a bare
        // `new Material(shader)` - setting color alpha alone does nothing.
        // The "Surface Type" Inspector dropdown has to be flipped to
        // Transparent explicitly, which for a runtime-created material
        // means setting these properties/keywords by hand (this is what
        // fixed the fog-of-war quad and the building-placement ghost both
        // rendering solid opaque once URP became the active pipeline).
        // Guarded by HasProperty so shaders without it (the legacy
        // Unlit/Transparent fallback, already transparent by design) are
        // left untouched.
        public static void ForceTransparent(Material material)
        {
            if (!material.HasProperty("_Surface"))
            {
                return;
            }

            material.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            material.SetFloat("_Blend", 0f); // Alpha blend
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}

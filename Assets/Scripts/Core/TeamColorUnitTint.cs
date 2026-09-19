using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Unit-side counterpart to TeamColorBuildingTint - see
    // docs/TEAM_COLOR_ART_BRIEF.md's "Units - still needs new art
    // (Blender, not Canva)" section. Buildings reuse an existing PBR
    // metallic map as their tint mask for free; units carry no such map,
    // so this reads a dedicated authored mask resource instead (a white/
    // black texture painted directly on the 3D model in Blender, one per
    // unit body, at the same UV layout as that unit's albedo). Reuses the
    // exact same offscreen blit shader (Shaders/TeamColorTrimBlit) - the
    // compositing mechanism is generic, only the mask source differs.
    //
    // A unit with no authored mask yet (Resources.Load returns null)
    // silently no-ops, same convention TryApplyMetallicTrimTint already
    // uses for a building with no metallic map - this is additive on top
    // of whatever civ-palette/custom-texture material the unit's own
    // factory already applied, and on top of the existing
    // TeamColorAccent banner, which stays as the only team indicator for
    // any unit this can't reach yet.
    public static class TeamColorUnitTint
    {
        private const string BlitShaderPath = "Shaders/TeamColorTrimBlit";
        private const int BakedResolution = 1024;

        private static readonly Dictionary<(Texture, Texture, FactionId), RenderTexture> Cache =
            new Dictionary<(Texture, Texture, FactionId), RenderTexture>();

        private static Material _blitMaterial;

        public static void TryApplyTeamMask(Renderer renderer, string maskResourcePath, FactionId faction)
        {
            if (renderer == null || string.IsNullOrEmpty(maskResourcePath))
            {
                return;
            }

            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            Texture albedo = ResolveAlbedo(material);
            if (albedo == null)
            {
                return;
            }

            Texture mask = Resources.Load<Texture2D>(maskResourcePath);
            if (mask == null)
            {
                return;
            }

            RenderTexture baked = GetOrBake(albedo, mask, faction);
            if (baked == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetTexture("_BaseMap", baked);
            block.SetTexture("_MainTex", baked);
            renderer.SetPropertyBlock(block);
        }

        // Called by CivilizationSetup.BeginMatch alongside
        // TeamColorBuildingTint.Reset()/DiplomacyRegistry.Reset() - same
        // per-match static-registry-reset convention, applied here to
        // release the cached RenderTextures rather than leaking them
        // across matches.
        public static void Reset()
        {
            foreach (RenderTexture rt in Cache.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                }
            }

            Cache.Clear();
        }

        private static Texture ResolveAlbedo(Material material)
        {
            if (material.HasProperty("_BaseMap"))
            {
                Texture baseMap = material.GetTexture("_BaseMap");
                if (baseMap != null)
                {
                    return baseMap;
                }
            }

            return material.mainTexture;
        }

        private static RenderTexture GetOrBake(Texture albedo, Texture mask, FactionId faction)
        {
            var key = (albedo, mask, faction);
            if (Cache.TryGetValue(key, out RenderTexture cached) && cached != null)
            {
                return cached;
            }

            if (_blitMaterial == null)
            {
                Shader shader = Resources.Load<Shader>(BlitShaderPath);
                if (shader == null)
                {
                    return null;
                }

                _blitMaterial = new Material(shader);
            }

            var rt = new RenderTexture(BakedResolution, BakedResolution, 0, RenderTextureFormat.ARGB32)
            {
                name = $"TeamColorUnit_{albedo.name}_{faction}",
            };
            rt.Create();

            _blitMaterial.SetTexture("_MaskTex", mask);
            _blitMaterial.SetColor("_TeamColor", TeamColor.For(faction));
            Graphics.Blit(albedo, rt, _blitMaterial);

            Cache[key] = rt;
            return rt;
        }
    }
}

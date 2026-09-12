using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Wave 5 item 29 follow-up: recolors a building's gilded/metal trim
    // toward its faction's TeamColor, using the metallic map every
    // MeshyBuildingImporter-sourced building already carries as a PBR
    // input - bright pixels there mark gilt/trim, dark pixels mark plain
    // stone, at the exact same UV layout as the albedo (confirmed by
    // direct visual inspection of TownCenter_albedo.png/
    // TownCenter_metallicSmoothness.png before writing this). No new art
    // needed, unlike the unit side of this item (see
    // docs/TEAM_COLOR_ART_BRIEF.md) - units have no equivalent map.
    //
    // Buildings with no metallic map (procedural-fallback shapes, or any
    // future import that doesn't generate one) are left completely
    // untouched by TryApplyMetallicTrimTint - it's purely additive on top
    // of BuildingModelFactory.TintMaterials's existing civ-color blend
    // (that mutates Material.color, a separate multiplier from whatever
    // texture is sampled, so the two compose without conflict) and the
    // existing TeamColorAccent banner, which stays as the only team
    // indicator for buildings this can't reach.
    public static class TeamColorBuildingTint
    {
        private const string BlitShaderPath = "Shaders/TeamColorTrimBlit";
        private const int BakedResolution = 1024;

        private static readonly Dictionary<(Texture, Texture, FactionId), RenderTexture> Cache =
            new Dictionary<(Texture, Texture, FactionId), RenderTexture>();

        private static Material _blitMaterial;

        public static void TryApplyMetallicTrimTint(Renderer renderer, FactionId faction)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            Texture albedo = ResolveAlbedo(material);
            if (albedo == null || !material.HasProperty("_MetallicGlossMap"))
            {
                return;
            }

            Texture metallicMask = material.GetTexture("_MetallicGlossMap");
            if (metallicMask == null)
            {
                return;
            }

            RenderTexture baked = GetOrBake(albedo, metallicMask, faction);
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
        // DiplomacyRegistry.Reset() - same "fresh start, no leaked
        // per-match state" convention that file already establishes,
        // applied here to release the cached RenderTextures rather than
        // leaking them across matches.
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

        private static RenderTexture GetOrBake(Texture albedo, Texture metallicMask, FactionId faction)
        {
            var key = (albedo, metallicMask, faction);
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
                name = $"TeamColorTrim_{albedo.name}_{faction}",
            };
            rt.Create();

            _blitMaterial.SetTexture("_MaskTex", metallicMask);
            _blitMaterial.SetColor("_TeamColor", TeamColor.For(faction));
            Graphics.Blit(albedo, rt, _blitMaterial);

            Cache[key] = rt;
            return rt;
        }
    }
}

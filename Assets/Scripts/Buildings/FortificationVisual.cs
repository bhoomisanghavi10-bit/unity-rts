using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Shared age-branching visual builder for Wall (every WallPieceKind
    // variant) and Gate. Ancient/Imperial always used BuildingModelFactory's
    // real 3D mesh pipeline (via its own Refresh, given an already-created
    // root) - Refresh's own age-suffixed lookup chain already degrades
    // correctly for ages with no dedicated art (Imperial resolves to the
    // per-civ model, an Ancient-only junction piece resolves to its literal
    // resourceName with no suffix). Classical/Durg (2026-09-28, "classic age
    // wall and gate kit") originally used a flat isometric sprite-billboard
    // kit (WallSpriteVisual) - a genuine ChatGPT-rendered 2D delivery, not a
    // 3D model. That delivery was superseded the same day by a real Meshy
    // glb kit (straight/corner/end post/T/X/gate, matching the Ancient kit's
    // own shape), so Classical/Durg now go through the exact same
    // BuildingModelFactory.Refresh mesh path as Ancient/Imperial - the age
    // branch below is gone entirely. Durg still reuses Classical's exact
    // same 6 meshes (Buildings/{name}_Classical resolves for both ages,
    // since Refresh always passes AgeId.Classical for either - see
    // WallFactory/GateFactory's own ClassicalMeshResourceName), darkened via
    // a material-color multiply (DarkenMaterials below, see its own
    // DurgTint comment), applied after the normal civ tint.
    internal static class FortificationVisual
    {
        // 0.7x (the sprite kit's original "30% darker" value) measured
        // correct via direct material-color sampling but read as barely
        // distinguishable at normal in-game viewing distance/lighting -
        // the exact same "reads correctly in RGB, easy to miss by eye"
        // issue this project's history already documented once for the
        // sprite kit itself. Deepened to 0.5x (50% darker), user-confirmed
        // 2026-09-28 after a live side-by-side spawn still looked too
        // subtle at 0.7x.
        private static readonly Color DurgTint = new Color(0.5f, 0.5f, 0.5f, 1f);

        public static void Build(GameObject root, string meshResourceName, AgeId age, CivilizationId civ, Color civColor, FactionId? faction, Vector3 size)
        {
            DestroyExistingVisualAndColliders(root);

            BuildingModelFactory.Refresh(root, meshResourceName, civ, age, size, civColor, faction);

            if (age == AgeId.Durg)
            {
                DarkenMaterials(root);
            }
        }

        private static void DestroyExistingVisualAndColliders(GameObject root)
        {
            Transform existing = root.transform.Find("Visual");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            foreach (BoxCollider collider in root.GetComponents<BoxCollider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        // BuildingModelFactory.TintMaterials already instantiates a unique
        // Material per renderer (see its own comment) - safe to further
        // darken those instances here without affecting any other building
        // sharing the same source texture/material. Mirrors TintMaterials'
        // own if/else-if property-priority chain exactly (never checking
        // more than one property per material) so this can't double-darken
        // a shader that happens to expose more than one of these names.
        private static void DarkenMaterials(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.color *= DurgTint;
                    }
                    else if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", material.GetColor("_BaseColor") * DurgTint);
                    }
                    else if (material.HasProperty("baseColorFactor"))
                    {
                        material.SetColor("baseColorFactor", material.GetColor("baseColorFactor") * DurgTint);
                    }
                    else if (material.HasProperty("diffuseFactor"))
                    {
                        material.SetColor("diffuseFactor", material.GetColor("diffuseFactor") * DurgTint);
                    }
                }
            }
        }
    }
}

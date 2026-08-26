using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Shared building-mesh spawn path, mirroring HumanModelFactory: looks
    // for a real model under Resources/Buildings/<resourceName> first, and
    // falls back to a hand-assembled procedural silhouette
    // (ProceduralBuildingFactory - stepped/spired/towered shapes, not just
    // a flat cube) whenever nothing's been imported yet - so every
    // Building factory keeps working, just with a more building-like
    // placeholder shape than a single box, until a real pack exists to
    // source model names from.
    //
    // Every gameplay component (Barracks/Farm/House/TownCenter,
    // ConstructionSite, FactionMember) still gets added by each factory to
    // the returned ROOT object, same as before the model swap -
    // HoverTooltip/SelectionManager read components straight off
    // hit.collider.gameObject with no GetComponentInParent fallback, so
    // the root is where the Collider has to live too, with the actual
    // visual mesh one level deeper as a child (same split HumanModelFactory
    // already uses for units, for the same reason).
    public static class BuildingModelFactory
    {
        private const float GroundClearance = 0.02f;

        // Full visual sweep (2026-08-26, following the ship/Dock bug
        // report): Tower's world bounds at identity rotation were
        // (25.88, 3.78, 7.01) - wider than a highway, barely taller than
        // a House. A tower's height should dominate its footprint, not
        // the other way around. -90 on Z gives (3.78, 25.88, 7.01) - tall
        // and narrow, confirmed visually via scene-view screenshot
        // (upright crenellated tower, not on its side) before committing.
        // Every other building's bounds were sane (footprint > height, or
        // legitimately tall-and-narrow like TownCenter's tiered-temple
        // design, which was already visually confirmed correct in an
        // earlier live Play mode screenshot this session) - Tower was the
        // only real rotation bug among the 9 sourced building models.
        private static readonly System.Collections.Generic.Dictionary<string, Quaternion> ImportRotationCorrections =
            new System.Collections.Generic.Dictionary<string, Quaternion>
            {
                { "Tower", Quaternion.Euler(0f, 0f, -90f) },
            };

        // rootPosition is the same "vertical center of the building"
        // convention every factory already computed for its primitive
        // cube (point + Vector3.up * (size.y * 0.5f), or TownCenterFactory's
        // pre-centered spawn position) - kept as the root's own transform
        // so every existing distance/rally-offset call site (IsClear,
        // rallyOffset, etc.) keeps reading the same position it always
        // has, unaffected by whichever visual (real model or procedural
        // shape) ends up under it.
        public static GameObject Spawn(string resourceName, Vector3 rootPosition, Vector3 fallbackSize, Color civColor)
        {
            // TownCenter/Barracks/Farm/House are flat .prefab assets right
            // under Resources/Buildings/ (hand-placed there), but models
            // sourced via the Sketchfab import pipeline land nested as
            // Buildings/<name>/<name>/scene.gltf (the importer's own
            // subfolder-per-model convention, same shape HumanModelFactory
            // already accounts for under Resources/human/) - try the flat
            // path first so nothing about the 4 original buildings changes,
            // then fall back to the nested one.
            // Item 49: a third fallback for Dock specifically - it was
            // sourced alongside the ship models into Assets/Resources/
            // Ships/ (a flat extracted prefab, not the nested Buildings/
            // <name>/<name>/scene convention the other imports use) rather
            // than Assets/Resources/Buildings/, since it's naturally part
            // of the same asset-sourcing pass as the boats.
            // Phase 5 gap-close (Wall/Gate): a fourth fallback - these two
            // landed one level shallower (Buildings/<name>/scene.gltf) than
            // the item-47 imports' Buildings/<name>/<name>/scene.gltf, an
            // import-tool convention difference rather than anything this
            // project chose - so a bare Buildings/<name>/scene path is
            // tried too, after the more specific patterns above.
            GameObject prefab = Resources.Load<GameObject>($"Buildings/{resourceName}")
                ?? Resources.Load<GameObject>($"Buildings/{resourceName}/{resourceName}/scene")
                ?? Resources.Load<GameObject>($"Buildings/{resourceName}/scene")
                ?? Resources.Load<GameObject>($"Ships/{resourceName}");

            GameObject root = new GameObject(resourceName);
            root.transform.position = rootPosition;

            GameObject model;
            if (prefab != null)
            {
                model = Object.Instantiate(prefab, root.transform);
                // Only a real imported pack gets the partial Lerp tint -
                // it has its own diffuse texture/material to partially
                // preserve. Procedural parts already get the civ color
                // outright at creation time (see ProceduralBuildingFactory).
                TintMaterials(model, civColor);
            }
            else
            {
                model = ProceduralBuildingFactory.Build(resourceName, civColor, root.transform);
            }

            model.transform.localPosition = Vector3.zero;
            // Only a real imported model can need an import-orientation
            // correction - a procedural fallback shape is already built
            // correctly oriented, so applying a correction meant for a
            // specific sourced model to it (if that model's Resources
            // asset were ever removed) would wrongly rotate a perfectly
            // fine primitive shape instead.
            model.transform.localRotation = prefab != null && ImportRotationCorrections.TryGetValue(resourceName, out Quaternion correction)
                ? correction
                : Quaternion.identity;

            // Imported packs (TownCenter in particular) ship their own
            // Collider baked into the model, sized by whoever authored the
            // pack rather than this project's units - TownCenter's is
            // roughly 13x22x12 world units once its x2 scale is applied,
            // dwarfing the actual building footprint and catching
            // raycasts/clicks on ground far outside the visible silhouette.
            // Stripped so the only Collider left is the tight one added
            // below, sized from the model's actual rendered bounds - same
            // reasoning as ProceduralBuildingFactory's primitive parts.
            foreach (Collider leftover in model.GetComponentsInChildren<Collider>(true))
            {
                Object.Destroy(leftover);
            }

            // TownCenter.prefab was found to have TownCenter/FactionMember/
            // VisionSource baked into it - its root was even still named
            // "TownCenter(Clone)" inside the saved asset, meaning it was
            // created by saving an already-live, already-Instantiate()'d
            // runtime object as a prefab instead of just its visual mesh.
            // Every one of those components only makes sense on the root
            // this method returns (added moments later, once - Faction,
            // VisionSource.Configure, etc. all assume exactly one), never
            // on the purely-visual child under it - a second copy here
            // means two TownCenters/FactionMembers/etc coexisting under
            // one root, contesting the same faction/rally logic. Stripped
            // generally (any MonoBehaviour, not just this specific asset's
            // known offenders) so the same mistake in a future imported
            // pack fails safe instead of silently duplicating gameplay
            // state again.
            // Plain Destroy() on all of them in one pass left TownCenter
            // itself behind (confirmed live) - RallyPoint's [RequireComponent
            // (typeof(Building))] is checked synchronously against the
            // object's CURRENT components, and Destroy() only defers the
            // actual removal to end-of-frame, so at the moment this loop
            // reached TownCenter, RallyPoint (queued for destruction a few
            // iterations earlier, not yet actually gone) still counted as
            // depending on it and blocked the destroy. DestroyImmediate in
            // dependency-safe passes (destroy whatever's still there each
            // pass, stop once nothing changes) removes dependents like
            // RallyPoint before their requirement is ever checked again.
            for (int pass = 0; pass < 4; pass++)
            {
                MonoBehaviour[] remaining = model.GetComponentsInChildren<MonoBehaviour>(true);
                if (remaining.Length == 0)
                {
                    break;
                }

                foreach (MonoBehaviour leftover in remaining)
                {
                    Object.DestroyImmediate(leftover);
                }
            }

            // Ground level under a center-pivoted building of this size -
            // approximate for TownCenter specifically (its spawn Y
            // predates milestone 14's terrain height variation, same
            // latent flat-Y issue unit spawns had before ResolveGroundHeight
            // was added), exact for Barracks/Farm/House (their point is
            // already ground-raycast-resolved by the caller).
            float groundY = rootPosition.y - fallbackSize.y * 0.5f;
            Bounds bounds = AlignBaseToGround(model, groundY);
            AddBoundsCollider(root, bounds);

            return root;
        }

        private static Bounds AlignBaseToGround(GameObject model, float groundY)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(model.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Some Sketchfab imports (House/Medieval Tavern in particular)
            // ship with their mesh's local origin nowhere near the mesh
            // itself - one baked-in offset was ~13 world units away once
            // scaled. Only correcting Y here left the root (and every
            // system that reads it - Collider, click detection, rally,
            // camera framing) pointing at the model's origin while the
            // visible mesh rendered a dozen units off to the side, which
            // reads as "walked inside broken giant geometry" up close.
            // Recentering X/Z on the root too, not just aligning Y to the
            // ground, makes root position and visible silhouette agree
            // regardless of how a given pack's pivot was authored.
            Vector3 rootPosition = model.transform.parent.position;
            Vector3 correction = new Vector3(
                rootPosition.x - bounds.center.x,
                groundY + GroundClearance - bounds.min.y,
                rootPosition.z - bounds.center.z);
            model.transform.position += correction;
            bounds.center += correction;

            return bounds;
        }

        // Sized/centered from the model's actual rendered bounds rather
        // than the fallback cube's Size, so clicking/hovering the real
        // model (once one exists) hits roughly its true silhouette instead
        // of an arbitrary placeholder box.
        private static void AddBoundsCollider(GameObject root, Bounds worldBounds)
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(worldBounds.center);
            collider.size = worldBounds.size;
        }

        // Runtime-tint placeholder: nudges the pack's own material color
        // toward the civ color at partial strength so an unfamiliar pack
        // still reads through underneath. HumanModelFactory instead swaps
        // in the Kevin Iglesias pack's own pre-baked Red/Yellow/Blue
        // palette materials, because that pack shipped color variants
        // already - worth the same upgrade here once a building pack's
        // exact material setup is known, since a runtime Lerp tint is a
        // weaker substitute for hand-authored palette variants.
        // Phase 5 gap-close: the Gate model's glTFast-imported Shader
        // Graph material has no "_Color" property at all (Unity's
        // Material.color is a convenience wrapper around exactly that
        // name) - every earlier imported pack happened to use a shader
        // that has one, so this went unnoticed until Gate's import spammed
        // a "doesn't have a color property '_Color'" console error on
        // every spawn and silently skipped tinting. glTFast names its own
        // color property "baseColorFactor" (glTF-spec metallic-roughness
        // workflow) instead - tried as a fallback. A third variant showed
        // up on the Wall model's material: glTFast's specular-glossiness
        // workflow shader instead names it "diffuseFactor" - same problem,
        // same fix, one more fallback before giving up on a given material
        // rather than assuming every pack's shader matches.
        private static void TintMaterials(GameObject go, Color civColor)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.color = Color.Lerp(material.color, civColor, 0.35f);
                    }
                    else if (material.HasProperty("baseColorFactor"))
                    {
                        Color baseColor = material.GetColor("baseColorFactor");
                        material.SetColor("baseColorFactor", Color.Lerp(baseColor, civColor, 0.35f));
                    }
                    else if (material.HasProperty("diffuseFactor"))
                    {
                        Color diffuseColor = material.GetColor("diffuseFactor");
                        material.SetColor("diffuseFactor", Color.Lerp(diffuseColor, civColor, 0.35f));
                    }
                }
            }
        }
    }
}

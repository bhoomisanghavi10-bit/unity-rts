using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Shared building-mesh spawn path, mirroring HumanModelFactory: looks
    // for a real model under Resources/Buildings/<resourceName> first, and
    // only falls back to today's flat-color primitive cube if nothing's
    // been imported yet - so every Building factory keeps working exactly
    // as before, completely unchanged visually, until a real building pack
    // exists to source model names from.
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

        // rootPosition is the same "vertical center of the building"
        // convention every factory already computed for its primitive
        // cube (point + Vector3.up * (size.y * 0.5f), or TownCenterFactory's
        // pre-centered spawn position) - kept as the root's own transform
        // so every existing distance/rally-offset call site (IsClear,
        // rallyOffset, etc.) keeps reading the same position it always
        // has, unaffected by whichever visual (primitive or real model)
        // ends up under it.
        public static GameObject Spawn(string resourceName, Vector3 rootPosition, Vector3 fallbackSize, Color civColor)
        {
            GameObject prefab = Resources.Load<GameObject>($"Buildings/{resourceName}");
            if (prefab == null)
            {
                return SpawnFallback(rootPosition, fallbackSize, civColor);
            }

            GameObject root = new GameObject(resourceName);
            root.transform.position = rootPosition;

            GameObject model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            TintMaterials(model, civColor);

            // Ground level under a center-pivoted fallback cube of this
            // size - approximate for TownCenter specifically (its spawn
            // Y predates milestone 14's terrain height variation, same
            // latent flat-Y issue unit spawns had before ResolveGroundHeight
            // was added), exact for Barracks/Farm/House (their point is
            // already ground-raycast-resolved by the caller).
            float groundY = rootPosition.y - fallbackSize.y * 0.5f;
            Bounds bounds = AlignBaseToGround(model, groundY);
            AddBoundsCollider(root, bounds);

            return root;
        }

        private static GameObject SpawnFallback(Vector3 rootPosition, Vector3 size, Color civColor)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = rootPosition;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(civColor);

            return go;
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

            float correction = groundY + GroundClearance - bounds.min.y;
            model.transform.position += new Vector3(0f, correction, 0f);
            bounds.center += new Vector3(0f, correction, 0f);

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
        private static void TintMaterials(GameObject go, Color civColor)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    material.color = Color.Lerp(material.color, civColor, 0.35f);
                }
            }
        }
    }
}

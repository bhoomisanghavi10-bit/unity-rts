using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    // Loads EVERY prefab under Resources/Environment/<category>/ (via
    // LoadAll, not a single fixed name like BuildingModelFactory) and
    // picks one at random per spawn - built this way specifically because
    // the user is dropping in multiple Asset Store tree/bush variants
    // rather than one exact model, so adding or removing a variant is
    // just a drag-and-drop into that folder, no code change needed here.
    // Returns null if the category folder is empty/missing so callers
    // (ResourceNodeSpawner) can fall back to their existing primitive -
    // same graceful-degradation shape as BuildingModelFactory.
    public static class EnvironmentPropFactory
    {
        private const float GroundClearance = 0.02f;

        public static GameObject TrySpawn(string category, Vector3 groundPoint)
        {
            GameObject[] variants = Resources.LoadAll<GameObject>($"Environment/{category}");
            if (variants == null || variants.Length == 0)
            {
                return null;
            }

            GameObject prefab = variants[Random.Range(0, variants.Length)];

            GameObject root = new GameObject(prefab.name);
            root.transform.position = groundPoint;
            // Left at identity rotation until AFTER the collider is sized
            // below - see AddBoundsCollider for why.

            GameObject model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            AlignBaseToGround(model, groundPoint.y);
            AddBoundsCollider(root, model);

            // Random facing so a cluster of the same variant doesn't all
            // stare the same direction - purely cosmetic, props have no
            // gameplay-relevant orientation. Applied AFTER the collider is
            // sized (not before) so the box - defined in local space - just
            // rotates along with root from here on, staying correctly
            // fitted at any facing.
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            return root;
        }

        private static void AlignBaseToGround(GameObject model, float groundY)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float correction = groundY + GroundClearance - bounds.min.y;
            model.transform.position += new Vector3(0f, correction, 0f);
        }

        // ResourceNode is added by the caller onto the ROOT, and
        // SelectionManager/HoverTooltip read components straight off
        // hit.collider.gameObject with no GetComponentInParent fallback -
        // same reasoning as BuildingModelFactory's collider placement.
        private static void AddBoundsCollider(GameObject root, GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // root is still at identity rotation here (TrySpawn applies the
            // random facing only after this runs), so world-space bounds
            // and root-local bounds are identical - a genuinely tight fit.
            // A prior version of this tried to correct for rotation by
            // transforming the world bounds' 8 corners into local space
            // AFTER rotating - that's provably never smaller than the true
            // footprint and often much larger for elongated/asymmetric
            // models (a rotated-then-rebounded box always over-estimates),
            // which produced oversized colliders that swallowed clicks
            // meant for neighboring objects map-wide.
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
        }
    }
}

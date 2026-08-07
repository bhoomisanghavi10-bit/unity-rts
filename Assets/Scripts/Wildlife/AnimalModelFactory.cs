using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Wildlife
{
    // Mirrors EnvironmentPropFactory's "load every prefab under
    // Resources/Environment/<category>/, pick one at random" approach, but
    // for animals rather than static props: WildBoar/Livestock move via
    // NavMeshAgent after spawning (the props never do), so this splits
    // root from visual the same way HumanModelFactory does for units -
    // the root carries the gameplay components (Attackable, WildBoar/
    // Livestock, the Collider raycast code depends on) and stays under
    // NavMeshAgent's control untouched, while the visual model is a child
    // kept aligned to the ground every frame via GroundFollower (reused
    // as-is from Units - it's already generic, not Human-specific).
    // Returns null if the category folder is empty so callers
    // (WildBoarSpawner/LivestockSpawner) can fall back to their existing
    // primitive capsule.
    public static class AnimalModelFactory
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
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // GetComponentInChildren, not TryGetComponent directly on
            // model: some packs (RedCambala's boar) put the Animator on a
            // nested child rather than the model's own top-level
            // GameObject, so a direct check silently misses it.
            Animator animator = model.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                // Same fix HumanModelFactory needed: a Generic-rig Animator
                // auto-attached on import still applies baked-in root motion
                // by default, which fights NavMeshAgent's own position control
                // and produces the "gliding" symptom already seen once this
                // project. Disabled preemptively here instead of waiting to
                // rediscover it.
                animator.applyRootMotion = false;

                // Some packs (again, RedCambala's boar) ship their own
                // Animator Controller asset. AnimationDriver/BoarAnimation-
                // Driver/CowAnimationDriver drive the Animator directly via
                // a hand-built PlayableGraph and never touch this
                // controller's parameters, so if left assigned it keeps
                // driving the Animator's default state (uncontrolled,
                // parameter-less) in parallel with - and fighting - the
                // custom graph. Clearing it hands the Animator over to the
                // custom graph completely, same as packs that never shipped
                // a controller in the first place.
                animator.runtimeAnimatorController = null;
            }

            AlignBaseToGround(model, groundPoint.y);
            AddBoundsCollider(root, model);
            root.AddComponent<GroundFollower>().Configure(model.transform);

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

        // ResourceNode/Attackable/WildBoar/Livestock is added by the
        // caller onto the ROOT, and SelectionManager/HoverTooltip read
        // components straight off hit.collider.gameObject with no
        // GetComponentInParent fallback - same reasoning as
        // BuildingModelFactory/EnvironmentPropFactory's collider placement.
        private static void AddBoundsCollider(GameObject root, GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // Animals spawn at a random Y rotation (unlike static props,
            // that rotation also changes at runtime as they turn to walk).
            // worldBounds.size is a WORLD-axis-aligned extent - assigning
            // it directly as the BoxCollider's LOCAL size only happens to
            // line up when root's rotation is near 0/180 degrees. At any
            // other rotation the box ends up narrower along one local axis
            // and wider along the other than the actual (elongated,
            // non-square) animal body, so right-click raycasts
            // (SelectionManager reads hit.collider directly, single
            // Physics.Raycast, no fallback) miss the visible model more
            // often than not - the direct cause of milking/attack commands
            // silently not registering. Transforming the world bounds'
            // corners into root-local space instead gives a box that
            // actually wraps the model regardless of root rotation.
            Bounds localBounds = new Bounds(root.transform.InverseTransformPoint(worldBounds.center), Vector3.zero);
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                localBounds.Encapsulate(root.transform.InverseTransformPoint(corner));
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }
    }
}

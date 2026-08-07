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

            // Same fix HumanModelFactory needed: a Generic-rig Animator
            // auto-attached on import still applies baked-in root motion
            // by default, which fights NavMeshAgent's own position control
            // and produces the "gliding" symptom already seen once this
            // project. Disabled preemptively here instead of waiting to
            // rediscover it.
            if (model.TryGetComponent(out Animator animator))
            {
                animator.applyRootMotion = false;
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

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
        }
    }
}

using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // Instantiates the pack-ox model used by Vanik, the land Trader (see
    // VanikFactory.cs) - a user-supplied, pre-rigged Meshy AI GLB (an ox
    // with a laden cargo saddle, matching AoE's Trade Cart being pulled by
    // draft animals rather than a person carrying goods on foot). Mirrors
    // HumanModelFactory's root/child split (gameplay components live on
    // the root, the visual model is a ground-aligned child), but this rig
    // is a Generic (non-humanoid) quadruped skeleton with its own bone
    // names (Bone_000..Bone_046, a Meshy "UniRig" auto-rig) - there is no
    // Avatar retargeting here the way HumanAnimationSet's shared clips get
    // reused across every human unit.
    //
    // The model shipped with no animation at all. Its Walk cycle was
    // produced by retargeting the existing Shepherd Valley cow pack's own
    // A_Cow_Walk_01 clip (see CowAnimationSet/CowAnimationDriver) onto this
    // rig's 4 leg chains and tail in Blender - a world-space delta-rotation
    // transfer per mapped bone pair (source bone's rotation offset from its
    // own bind pose, reapplied to the target bone's bind pose), since the
    // two rigs have unrelated bone names/counts and can't share a clip
    // directly the way two Cow-pack assets can. The spine/head/ears/horn
    // were deliberately left unretargeted (kept at rest) to avoid a twisted
    // look from any roll/axis mismatch between the two independently-
    // authored skeletons - only the legs and tail actually animate. Idle is
    // a static single-frame pose (the model's own rest pose) - no idle
    // clip existed to retarget. Both clips were baked as an FBX from the
    // same Blender scene the retarget ran in, then had their curve paths
    // rewritten (see the .anim assets in Resources/AnimalAnimations/Ox) to
    // match this glTF-imported prefab's flatter hierarchy (glTFast doesn't
    // preserve the FBX exporter's extra "Armature" node the way Unity's own
    // FBX importer does), and the prefab's Animator/Avatar were both built
    // by hand (AvatarBuilder.BuildGenericAvatar) since glTFast doesn't
    // auto-attach either for a mesh with no embedded animation.
    public static class OxModelFactory
    {
        private const string PrefabPath = "UniqueUnits/Vanik/Vanik";
        private const float GroundClearance = 0.02f;

        public static GameObject Spawn(Vector3 position)
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabPath);

            GameObject root = new GameObject(prefab.name);
            root.transform.position = position;

            GameObject model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            if (model.TryGetComponent(out Animator animator))
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
            }

            float groundY = ResolveGroundHeight(position, fallback: position.y - 1f);
            AlignBaseToGround(model, groundY);
            AddBoundsCollider(root, model);

            root.AddComponent<GroundFollower>().Configure(model.transform);

            return root;
        }

        private static float ResolveGroundHeight(Vector3 position, float fallback)
        {
            return GroundReference.TryGetHeight(position, out float height) ? height : fallback;
        }

        private static void AlignBaseToGround(GameObject model, float groundY)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            groundY += GroundClearance;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float correction = groundY - bounds.min.y;
            model.transform.position += new Vector3(0f, correction, 0f);
        }

        // A quadruped's footprint is wider/longer than it is tall - a
        // CapsuleCollider (every biped unit's own choice, via
        // HumanModelFactory) would be a poor fit here, so this follows
        // AnimalModelFactory's own box-fit-to-rendered-bounds approach
        // instead. Root is still at identity rotation here (facing is
        // never randomized for a player-ordered unit like Vanik, unlike
        // AnimalModelFactory's wandering wildlife), so world-space and
        // root-local bounds are identical.
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

using UnityEngine;

namespace KingdomsOfBharat.Combat
{
    // Attaches a downloaded weapon/mount prop to a Humanoid rig's bone (or
    // stands one beside the unit root, for Cavalry's horse - see
    // AttachBeside), auto-normalizing scale and re-centering the mesh from
    // its own measured bounds instead of hand-tuned per-model numbers -
    // same "measure, don't guess" convention HumanModelFactory's
    // AlignFeetToGround already uses for foot placement. Needed because
    // downloaded packs come in whatever real-world scale/pivot the artist
    // modeled them at (e.g. the sword pack's swords are ~100-300 units
    // "long" with a pivot nowhere near the mesh), not this project's own
    // ~1-unit-per-meter convention.
    public static class WeaponAttachment
    {
        // Some downloaded packs bundle several weapon variants as sibling
        // meshes in one file (see the sword pack) - trimToFirstMesh keeps
        // only the first renderer found and discards the rest, since only
        // one variant is wanted per attachment.
        public static GameObject AttachToBone(
            GameObject unitRoot, HumanBodyBones bone, string resourcePath,
            float targetSize, Vector3 localPositionOffset, Vector3 localEulerOffset,
            bool trimToFirstMesh = false)
        {
            Animator animator = unitRoot.GetComponentInChildren<Animator>();
            Transform socket = animator != null ? animator.GetBoneTransform(bone) : null;
            if (socket == null)
            {
                return null;
            }

            return AttachToTransform(socket, resourcePath, targetSize, localPositionOffset, localEulerOffset, trimToFirstMesh);
        }

        // For a prop that isn't hand-held (Cavalry's horse) - stands it
        // beside the unit root instead of on a rig bone, same normalize/
        // recenter treatment.
        public static GameObject AttachBeside(
            GameObject unitRoot, string resourcePath,
            float targetSize, Vector3 localPositionOffset, Vector3 localEulerOffset)
        {
            return AttachToTransform(unitRoot.transform, resourcePath, targetSize, localPositionOffset, localEulerOffset, trimToFirstMesh: false);
        }

        private static GameObject AttachToTransform(
            Transform socket, string resourcePath, float targetSize,
            Vector3 localPositionOffset, Vector3 localEulerOffset, bool trimToFirstMesh)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return null;
            }

            GameObject wrapper = new GameObject(resourcePath + "_Socket");
            wrapper.transform.SetParent(socket, false);
            wrapper.transform.localPosition = localPositionOffset;
            wrapper.transform.localRotation = Quaternion.Euler(localEulerOffset);

            GameObject prop = Object.Instantiate(prefab, wrapper.transform, false);

            if (trimToFirstMesh)
            {
                KeepOnlyFirstMesh(prop);
            }

            NormalizeAndCenter(prop, wrapper, targetSize);

            return wrapper;
        }

        // Renderer, not MeshFilter: some downloaded packs' individual mesh
        // nodes don't carry a MeshFilter/MeshRenderer pair in a form
        // GetComponentsInChildren<MeshFilter>() actually finds (varies by
        // importer), but every visible mesh always has a Renderer, so
        // trimming against that instead is reliable regardless of exactly
        // which renderer subtype the pack uses.
        private static void KeepOnlyFirstMesh(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            for (int i = 1; i < renderers.Length; i++)
            {
                Object.Destroy(renderers[i].gameObject);
            }
        }

        // Scales the WRAPPER to hit targetSize, then re-measures the
        // renderers' world bounds (now reflecting that scale) and shifts
        // the wrapper by the resulting world-space delta so the mesh's
        // geometric center lands exactly on the wrapper's original
        // position. Deliberately empirical (measure again after scaling)
        // rather than derived through the parent chain's rotation/scale
        // algebraically - some of these packs bake a pivot thousands of
        // units from the actual geometry (see the sword pack), and an
        // algebraic derivation left a large residual offset in practice;
        // re-measuring sidesteps that entirely, same "measure, don't
        // guess" convention HumanModelFactory's AlignFeetToGround uses.
        private static void NormalizeAndCenter(GameObject prop, GameObject wrapper, float targetSize)
        {
            Renderer[] renderers = prop.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestDimension <= 0f)
            {
                return;
            }

            float scale = targetSize / largestDimension;
            wrapper.transform.localScale = Vector3.one * scale;

            Bounds scaledBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                scaledBounds.Encapsulate(renderers[i].bounds);
            }

            wrapper.transform.position += wrapper.transform.position - scaledBounds.center;
        }
    }
}

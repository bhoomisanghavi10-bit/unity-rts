using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // Instantiates the shared "Human Character Dummy" body (Kevin Iglesias
    // pack, Assets/Resources/Kevin Iglesias/) used by every human unit type
    // - Worker, Soldier, any future human role - reskinned per civ via the
    // pack's own color-palette materials rather than a hand-picked flat
    // tint, since the palette is a trim-sheet texture (same _MainTex
    // across every color variant, differentiated only by
    // mainTextureOffset) - copying that offset onto a URP-safe material
    // (the pack's own materials use a legacy Built-in shader) preserves
    // the palette split while sidestepping the shader-compatibility
    // question entirely, same approach as SoldierFactory's earlier
    // Axe Warrior integration.
    public static class HumanModelFactory
    {
        public enum Gender { Male, Female }

        // The returned root sits at "center height" - the same convention
        // every other unit/building spawn position in this project already
        // uses (e.g. AiController's townCenterPosition.y = 1, matching a
        // ~2-unit-tall object's vertical center resting on the ground).
        // The dummy model's own pivot is at its feet (standard for a
        // Humanoid rig, unlike a CreatePrimitive capsule's center pivot),
        // so it's parented as a visual-only child offset down by half the
        // body height instead of being instantiated directly at the root -
        // keeps every other system's position-based math (VisionSource
        // range, MeleeAttacker range, NavMeshAgent, the Collider) working
        // unchanged, since they all read the root's transform, not the
        // visual child's.
        public static GameObject Spawn(Gender gender, Vector3 position, CivilizationId civilization)
        {
            string genderTag = gender == Gender.Male ? "M" : "F";
            GameObject prefab = Resources.Load<GameObject>($"Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_{genderTag} White");

            GameObject root = new GameObject(prefab.name);
            root.transform.position = position;

            GameObject model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            // No facing correction applied (a guessed 180-degree flip made
            // it worse per real-Editor testing, so it's removed rather
            // than compounding the guess) - if it still faces the wrong
            // way, the exact correction needs to come from testing
            // feedback, not another blind guess.
            model.transform.localRotation = Quaternion.identity;
            // Forced explicitly rather than trusting Instantiate to carry
            // over (1,1,1): Instantiate(prefab, parent) preserves whatever
            // local scale is saved on the PREFAB ASSET itself, not a fresh
            // default - so if that shared asset's own Transform scale ever
            // gets edited by accident (e.g. selecting the wrong asset in
            // the Project window while scaling something else entirely),
            // every worker/soldier spawned afterward silently inherits the
            // bad scale, while ones already alive keep their original one -
            // exactly the "some units giant, some fine, root reads 1,1,1"
            // symptom seen in testing. Forcing it here makes every spawn
            // correct regardless of what the shared asset's own scale is.
            model.transform.localScale = Vector3.one;

            // Humanoid FBX models get an Animator+Avatar auto-attached by
            // Unity on import - AnimationDriver needs exactly this (it
            // drives the Avatar directly via the Playables API, with no
            // AnimatorController assigned), so it's kept as-is rather than
            // removed. applyRootMotion is disabled: the walk clip's own
            // baked-in forward translation would otherwise fight
            // NavMeshAgent's independent control of the root's position,
            // producing exactly the "gliding" symptom seen in testing
            // (visual root motion and actual navigation motion doubling
            // up / cancelling out).
            if (model.TryGetComponent(out Animator animator))
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
            }

            ApplyPaletteMaterial(model, PaletteNameFor(civilization));

            // Two independent problems were compounding here: the model's
            // pivot convention wasn't known (fixed by measuring rendered
            // bounds instead of guessing an offset), and the caller's
            // spawn position uses a flat, hardcoded Y that doesn't account
            // for ProceduralGround's height variation (milestone 14) at
            // that particular XZ - invisible on a plain capsule, obvious
            // on a detailed model. Resolving actual ground height via
            // raycast (same technique AiController/BuildingPlacer already
            // use for buildings) fixes the second half; falls back to the
            // caller's own Y if the raycast somehow misses.
            float groundY = ResolveGroundHeight(position, fallback: position.y - 1f);
            AlignFeetToGround(model, groundY);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.4f;
            collider.height = 2f;

            return root;
        }

        private static float ResolveGroundHeight(Vector3 position, float fallback)
        {
            Vector3 origin = new Vector3(position.x, position.y + 20f, position.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f))
            {
                return hit.point.y;
            }

            return fallback;
        }

        // Small upward safety margin: bounds are measured here before the
        // Idle animation (set moments later by AnimationDriver) has been
        // evaluated even once, so this reads the mesh's rest/bind pose,
        // not the actual idle-sway pose (typically slightly lower, knees
        // microbent) - per real-Editor testing, that gap was enough for
        // feet to visibly clip into the ground. A small constant margin
        // is simpler and safer than trying to re-measure bounds every
        // frame against a moving animated pose.
        private const float GroundClearance = 0.08f;

        private static void AlignFeetToGround(GameObject model, float groundY)
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

        private static string PaletteNameFor(CivilizationId civilization)
        {
            switch (civilization)
            {
                case CivilizationId.Chola: return "Red";
                case CivilizationId.Vijayanagara: return "Yellow";
                case CivilizationId.Rajput: return "Blue";
                default: return null;
            }
        }

        private static void ApplyPaletteMaterial(GameObject go, string paletteName)
        {
            string materialPath = paletteName == null
                ? "Kevin Iglesias/Human Character Dummy/Materials/HumanDummy"
                : $"Kevin Iglesias/Human Character Dummy/Materials/HumanDummy_{paletteName}";
            Material source = Resources.Load<Material>(materialPath);

            Material material = GameplayMaterial.CreateOpaque(Color.white);
            if (source != null)
            {
                material.mainTexture = source.mainTexture;
                material.mainTextureOffset = source.mainTextureOffset;
                material.mainTextureScale = source.mainTextureScale;
            }

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}

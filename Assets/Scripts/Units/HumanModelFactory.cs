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
            model.transform.localPosition = new Vector3(0f, -1f, 0f);
            // Best-effort correction for the model's forward axis not
            // matching Unity's +Z convention - flag to the user if the
            // model still faces the wrong way after testing so this can
            // be tuned precisely.
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Humanoid FBX models get an Animator+Avatar auto-attached by
            // Unity on import - AnimationDriver needs exactly this (it
            // drives the Avatar directly via the Playables API, with no
            // AnimatorController assigned), so it's kept as-is rather than
            // removed.
            if (model.TryGetComponent(out Animator animator))
            {
                animator.runtimeAnimatorController = null;
            }

            ApplyPaletteMaterial(model, PaletteNameFor(civilization));

            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.4f;
            collider.height = 2f;

            return root;
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

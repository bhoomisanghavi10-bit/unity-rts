using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Combat
{
    // Creates a "Soldier" unit with combat components. Used by Barracks
    // when training finishes, for whichever faction owns the training
    // Barracks. Milestone 19: uses the real "Axe Warrior" model (Humanoid-
    // rigged, Assets/Resources/Axe Warrior/) instead of a capsule primitive
    // - loaded via Resources.Load since this is a static factory (no
    // MonoBehaviour to hold an Inspector-assigned prefab reference). Still
    // has no walk/idle/attack animation wired up yet (the pack ships one
    // clip; a proper Animator Controller is milestone 20's job) - the model
    // will render in its rest pose while moving/fighting for now.
    public static class SoldierFactory
    {
        private const string ModelResourcePath = "Axe Warrior/Prefab/Axe_Warrior_Complete_set";
        private const string DiffuseTextureResourcePath = "Axe Warrior/Materials/Texture/Axe Warrior_Bake1_PBR_Diffuse";

        // No default faction value, deliberately: a future call site that
        // forgets to pass one should fail to compile, not silently spawn a
        // Player-owned soldier from an AI Barracks.
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject prefab = Resources.Load<GameObject>(ModelResourcePath);
            GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Soldier"
                : $"Enemy {profile.DisplayName} Soldier";

            ApplyModelMaterial(go, profile.PrimaryColor);

            // The model has no collider of its own (FBX imports don't add
            // one) - selection/raycasting elsewhere (SelectionManager,
            // BuildingPlacer) expects to find gameplay components via
            // TryGetComponent directly on whatever the raycast hit, so the
            // collider has to live on this same root GameObject, not
            // wherever the mesh happens to sit in the model's hierarchy.
            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.4f;
            collider.height = 2f;
            collider.center = new Vector3(0f, 1f, 0f);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 4f;

            go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<Attackable>().Configure(30f * profile.MaxHealthMultiplier);
            go.AddComponent<MeleeAttacker>().SetDamageMultiplier(profile.SoldierDamageMultiplier);
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }

        // Reassigns every renderer's material through GameplayMaterial
        // (with the pack's own diffuse texture) rather than trusting the
        // imported material/shader as-is - sidesteps needing to know
        // whether the pack's original shader is URP-compatible at all.
        // Tints at half strength so the model's painted texture detail
        // still reads through the civ color rather than being flattened.
        private static void ApplyModelMaterial(GameObject go, Color civColor)
        {
            Texture2D diffuse = Resources.Load<Texture2D>(DiffuseTextureResourcePath);
            Material material = GameplayMaterial.CreateOpaque(Color.Lerp(Color.white, civColor, 0.5f));
            if (diffuse != null)
            {
                material.mainTexture = diffuse;
            }

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Combat
{
    // Creates a placeholder capsule "Soldier" unit with combat components.
    // Used by Barracks when training finishes, for whichever faction owns
    // the training Barracks.
    public static class SoldierFactory
    {
        // No default faction value, deliberately: a future call site that
        // forgets to pass one should fail to compile, not silently spawn a
        // Player-owned soldier from an AI Barracks.
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Soldier"
                : $"Enemy {profile.DisplayName} Soldier";
            go.transform.position = position;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(profile.PrimaryColor);

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

    }
}

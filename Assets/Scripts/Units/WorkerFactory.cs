using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.Units
{
    // Creates a placeholder capsule "Worker" unit with movement, selection,
    // gathering, and building components. Used by both UnitSpawner (the
    // Player's starting workers) and AiController (the AI's own workers).
    // Workers carry Attackable (fragile, no MeleeAttacker of their own) so
    // wild boars and enemy soldiers can actually threaten them - AoE-style,
    // unarmed villagers are vulnerable, not invincible.
    public static class WorkerFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Worker"
                : $"Enemy {profile.DisplayName} Worker";
            go.transform.position = position;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader())
            {
                color = profile.PrimaryColor,
            };

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 3.5f;

            go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<Gatherer>().SetRateMultiplier(profile.GatherRateMultiplier);
            go.AddComponent<Builder>();
            go.AddComponent<FarmWorker>();
            go.AddComponent<LivestockWorker>();
            go.AddComponent<Attackable>().Configure(20f * profile.MaxHealthMultiplier);
            go.AddComponent<FactionMember>().Configure(faction);

            // Only the Player's own vision feeds FogOfWarManager; the AI
            // has full internal knowledge and never queries fog itself, so
            // giving Enemy units a VisionSource too would incorrectly
            // reveal fog around the AI's own base to the player.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}

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
    // Barracks. Milestone 19b: uses the shared Human Character Dummy body
    // (Male) instead of the earlier single-pose Axe Warrior model - gives
    // Idle/Walk/Attack animation via AnimationDriver, and Workers/Soldiers
    // now share one consistent body style (see HumanModelFactory).
    public static class SoldierFactory
    {
        // No default faction value, deliberately: a future call site that
        // forgets to pass one should fail to compile, not silently spawn a
        // Player-owned soldier from an AI Barracks.
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Soldier"
                : $"Enemy {profile.DisplayName} Soldier";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 4f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<Attackable>().Configure(30f * profile.MaxHealthMultiplier);
            go.AddComponent<MeleeAttacker>().SetDamageMultiplier(profile.SoldierDamageMultiplier);
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}

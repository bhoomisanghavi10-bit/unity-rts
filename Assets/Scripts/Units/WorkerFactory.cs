using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Wildlife;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Units
{
    // Creates a "Worker" unit with movement, selection, gathering, and
    // building components. Used by both UnitSpawner (the Player's
    // starting workers) and AiController (the AI's own workers). Workers
    // carry Attackable so wild boars and enemy soldiers can actually
    // threaten them - AoE-style, unarmed villagers are vulnerable, not
    // invincible - but also a weak MeleeAttacker of their own (well below
    // a Soldier's damage) so they can fight back or hunt wild boars for
    // Food, same as AoE villagers can. Milestone 19b: uses the shared
    // Human Character Dummy body (Female) instead of a capsule - Soldiers
    // use the Male variant, giving Workers and Soldiers distinct
    // silhouettes for free.
    public static class WorkerFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            // Baked in at spawn time, same as the civ bonus - a later Age-up
            // doesn't retroactively boost units that already exist, only
            // ones trained from then on, matching how CivilizationProfile's
            // own bonuses already work here.
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Female, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Worker"
                : $"Enemy {profile.DisplayName} Worker";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 3.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // Phase 6 gap-close: EconomyTechProgress's ImprovedTools/
            // PackMules techs layer on top of the civ/age multipliers
            // already here, same "multiply everything relevant together"
            // convention every other bonus in this project uses.
            float gatherRateMultiplier = profile.GatherRateMultiplier * age.GatherRateMultiplier
                * (EconomyTechProgress.HasResearched(faction, EconomyTech.ImprovedTools) ? EconomyTechDefinition.For(EconomyTech.ImprovedTools).Bonus : 1f);
            float carryCapacityMultiplier = EconomyTechProgress.HasResearched(faction, EconomyTech.PackMules)
                ? EconomyTechDefinition.For(EconomyTech.PackMules).Bonus
                : 1f;
            var gatherer = go.AddComponent<Gatherer>();
            gatherer.SetRateMultiplier(gatherRateMultiplier);
            gatherer.SetCarryCapacityMultiplier(carryCapacityMultiplier);
            go.AddComponent<Builder>();
            go.AddComponent<FarmWorker>();
            go.AddComponent<LivestockWorker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(20f * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Infantry);
            go.AddComponent<HealthBar>();
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(2f);
            attacker.SetUnitClass(UnitClass.Infantry);
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Female), agent, unit);

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

    }
}

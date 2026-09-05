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
    // Food, same as AoE villagers can. Uses the sourced "Harvest Guardian"
    // villager models (a male/female pair, randomly picked per worker) -
    // see the comment at the spawn call below.
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
            // Phase 2 migration: base combat/movement stats now come from
            // the CSV-generated UnitDefinition rather than literals here -
            // see DataRegistry. Falls back to the pre-migration hardcoded
            // values (matching what shipped before this pass) if the
            // generated asset is ever missing, so a stale/un-regenerated
            // project degrades instead of breaking.
            UnitDefinition def = DataRegistry.GetUnit("worker");
            if (def == null)
            {
                Debug.LogWarning("WorkerFactory: no generated UnitDefinition for 'worker' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Uses the sourced "Harvest Guardian" villager models instead of
            // the shared Human Character Dummy body - real, bespoke rigged
            // assets (a matched male/female pair, same concept-art family:
            // sickle + basket) rather than the generic reskinned mannequin.
            // Each worker randomly gets one body or the other, purely for
            // crowd variety - AoE villager crowds mix genders, and there's
            // no gameplay distinction between them (identical stats either
            // way). Each one's own painted texture is kept as-is, same
            // applyPaletteMaterial:false convention as the 3 Meshy-sourced
            // unique units - these are single sourced assets shared across
            // all civs, not a trim-sheet to retint per civ. Only Worker
            // uses this random pick; every other human unit stays on the
            // dummy body's fixed Gender.Male.
            HumanModelFactory.Gender villagerGender = Random.value < 0.5f
                ? HumanModelFactory.Gender.Female
                : HumanModelFactory.Gender.Male;
            string villagerPrefabPath = villagerGender == HumanModelFactory.Gender.Female
                ? "human/FemaleVillager/FemaleVillager"
                : "human/MaleVillager/MaleVillager";
            GameObject go = HumanModelFactory.Spawn(
                villagerGender, position, civilization,
                prefabPathOverride: villagerPrefabPath, applyPaletteMaterial: false);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Worker"
                : $"Enemy {profile.DisplayName} Worker";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.5f;
            // Phase 6 gap-close: Maurya's Worker-only move-speed bonus - see
            // CivilizationProfile.FindCategoryMultiplier for why this isn't
            // one of CivilizationProfile's named fields.
            agent.speed *= CivilizationProfile.FindCategoryMultiplier(civilization, StatType.MoveSpeed, UnitClass.Support);

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit be
            // ordered to walk to and enter a friendly GarrisonPoint - see
            // GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
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
            // Roadmap Section 1 (worker self-defense/cross-awareness,
            // AoE-parity Phase 4.2): per-civ default for how this worker
            // reacts to being attacked mid-gather - see
            // WorkerCombatResponseDefaults.
            gatherer.SetCombatResponse(WorkerCombatResponseDefaults.For(civilization));
            go.AddComponent<Builder>();
            go.AddComponent<Repairer>();
            go.AddComponent<FarmWorker>();
            go.AddComponent<LivestockWorker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 20f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Infantry);
            go.AddComponent<HealthBar>();
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 2f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetUnitClass(UnitClass.Infantry);
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(villagerGender), agent, unit);

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
